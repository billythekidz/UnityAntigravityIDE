import * as vscode from 'vscode';
import * as fs from 'fs';
import * as path from 'path';

const DEBOUNCE_MS = 5000; // Wait 5s after last diagnostic change before checking
const MISSING_TYPE_CODES = ['CS0246', 'CS0103']; // Missing type/name errors

let debounceTimer: NodeJS.Timeout | undefined;
let lastFixedSet = new Set<string>(); // Track what we've already fixed to avoid loops

/**
 * Watches DotRush diagnostics for missing assembly errors (CS0246, CS0103).
 * When detected, scans Library/ScriptAssemblies for DLLs not referenced
 * in .csproj and patches them in as a fallback fix.
 */
export function registerCsprojFixer(context: vscode.ExtensionContext) {
    const disposable = vscode.languages.onDidChangeDiagnostics((e) => {
        // Debounce: wait for diagnostics to stabilize
        if (debounceTimer) {
            clearTimeout(debounceTimer);
        }
        debounceTimer = setTimeout(() => checkAndFixMissingAssemblies(), DEBOUNCE_MS);
    });
    context.subscriptions.push(disposable);
    console.log('[Antigravity Unity] .csproj fixer registered');
}

async function checkAndFixMissingAssemblies() {
    const workspaceFolder = vscode.workspace.workspaceFolders?.[0];
    if (!workspaceFolder) return;

    const projectRoot = workspaceFolder.uri.fsPath;
    const scriptAssembliesDir = path.join(projectRoot, 'Library', 'ScriptAssemblies');

    // Only run in Unity projects
    if (!fs.existsSync(scriptAssembliesDir)) return;

    // Check if there are CS0246/CS0103 errors from DotRush
    const hasMissingTypeErrors = checkForMissingTypeErrors();
    if (!hasMissingTypeErrors) return;

    // Find all .csproj files in the project root
    const csprojFiles = fs.readdirSync(projectRoot)
        .filter(f => f.endsWith('.csproj'))
        .map(f => path.join(projectRoot, f));

    if (csprojFiles.length === 0) return;

    // Get all DLLs in Library/ScriptAssemblies
    const allDlls = fs.readdirSync(scriptAssembliesDir)
        .filter(f => f.endsWith('.dll'))
        .map(f => ({
            name: path.basename(f, '.dll'),
            fullPath: path.join(scriptAssembliesDir, f)
        }));

    let totalPatched = 0;

    for (const csprojPath of csprojFiles) {
        const patched = patchCsproj(csprojPath, allDlls);
        totalPatched += patched;
    }

    if (totalPatched > 0) {
        const action = await vscode.window.showInformationMessage(
            `[Antigravity] Fixed ${totalPatched} missing assembly reference(s) in .csproj. Reload to apply.`,
            'Reload Window'
        );
        if (action === 'Reload Window') {
            vscode.commands.executeCommand('workbench.action.reloadWindow');
        }
    }
}

function checkForMissingTypeErrors(): boolean {
    for (const [uri, diagnostics] of vscode.languages.getDiagnostics()) {
        // Only check .cs files
        if (!uri.fsPath.endsWith('.cs')) continue;

        for (const diag of diagnostics) {
            if (diag.source !== 'Assembly-CSharp' && 
                diag.source !== 'Assembly-CSharp-Editor') continue;
            
            const code = typeof diag.code === 'object' ? String(diag.code.value) : String(diag.code);
            if (MISSING_TYPE_CODES.includes(code)) {
                return true;
            }
        }
    }
    return false;
}

/**
 * Patches a .csproj file by adding missing DLL references from Library/ScriptAssemblies.
 * Uses the same "sibling matching" strategy as ProjectGeneration.cs:
 * only adds DLLs whose parent namespace is already referenced.
 * Returns the number of references added.
 */
function patchCsproj(csprojPath: string, allDlls: { name: string; fullPath: string }[]): number {
    let content = fs.readFileSync(csprojPath, 'utf-8');

    // Extract existing reference names
    const existingRefs = new Set<string>();
    const refRegex = /<Reference Include="([^"]+)"/g;
    let match;
    while ((match = refRegex.exec(content)) !== null) {
        existingRefs.add(match[1].toLowerCase());
    }

    // Build root set (parent namespaces of existing refs)
    const roots = new Set<string>();
    for (const ref of existingRefs) {
        let lastDot = ref.lastIndexOf('.');
        while (lastDot > 0) {
            roots.add(ref.substring(0, lastDot));
            lastDot = ref.substring(0, lastDot).lastIndexOf('.');
        }
    }

    // Find missing sibling DLLs
    const missing: { name: string; fullPath: string }[] = [];
    for (const dll of allDlls) {
        const lowerName = dll.name.toLowerCase();
        if (existingRefs.has(lowerName)) continue;

        const lastDot = lowerName.lastIndexOf('.');
        if (lastDot > 0) {
            const parent = lowerName.substring(0, lastDot);
            if (roots.has(parent) || existingRefs.has(parent)) {
                // Check we haven't fixed this exact ref before in this session
                const fixKey = `${path.basename(csprojPath)}:${dll.name}`;
                if (lastFixedSet.has(fixKey)) continue;

                missing.push(dll);
                lastFixedSet.add(fixKey);
            }
        }
    }

    if (missing.length === 0) return 0;

    // Build the XML block to insert
    const lines = ['  <ItemGroup>'];
    for (const dll of missing) {
        lines.push(`    <Reference Include="${dll.name}">`);
        lines.push(`        <HintPath>${dll.fullPath.replace(/\\/g, '/')}</HintPath>`);
        lines.push('        <Private>false</Private>');
        lines.push('    </Reference>');
    }
    lines.push('  </ItemGroup>');
    const block = lines.join('\n');

    // Insert before the closing </Project> tag
    const insertPoint = content.lastIndexOf('</Project>');
    if (insertPoint === -1) return 0;

    content = content.substring(0, insertPoint) + block + '\n' + content.substring(insertPoint);
    fs.writeFileSync(csprojPath, content, 'utf-8');

    console.log(`[Antigravity Unity] Patched ${path.basename(csprojPath)}: added ${missing.length} missing refs (${missing.map(m => m.name).join(', ')})`);
    return missing.length;
}
