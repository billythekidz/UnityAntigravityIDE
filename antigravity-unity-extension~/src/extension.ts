import * as vscode from 'vscode';
import * as fs from 'fs';
import * as path from 'path';
import { registerCompletionProviders } from './completion/unityCompletions';
import { registerCommands } from './commands/commands';
// import { registerCsprojFixer } from './csproj/csprojFixer'; // Disabled: interferes with DotRush compilation

const DOTRUSH_EXTENSION_ID = 'nromanov.dotrush';

export async function activate(context: vscode.ExtensionContext) {
    // MUST be first: inject dotnet into PATH before DotRush tries to spawn it.
    // GUI apps on macOS/Linux don't inherit shell PATH, causing 'spawn dotnet ENOENT'.
    injectDotnetPath();

    console.log('[Antigravity Unity] Extension activated');

    // Auto-install DotRush if not present
    await ensureDotRushInstalled();

    // Register all features (debugging handled by DotRush)
    registerCompletionProviders(context);
    registerCommands(context);
    // registerCsprojFixer(context); // Disabled: interferes with DotRush compilation

    // Show status bar item
    const statusBarItem = vscode.window.createStatusBarItem(
        vscode.StatusBarAlignment.Left,
        100
    );
    statusBarItem.text = '$(unity) Unity';
    statusBarItem.tooltip = 'Antigravity Unity Extension Active — C# powered by DotRush';
    statusBarItem.command = 'antigravity-unity.openApiReference';
    statusBarItem.show();
    context.subscriptions.push(statusBarItem);

    console.log('[Antigravity Unity] All features registered');
}

async function ensureDotRushInstalled(): Promise<void> {
    const dotrush = vscode.extensions.getExtension(DOTRUSH_EXTENSION_ID);
    if (dotrush) {
        console.log('[Antigravity Unity] DotRush is already installed');
        return;
    }

    const choice = await vscode.window.showInformationMessage(
        'Antigravity Unity requires DotRush for C# IntelliSense and debugging. Install now?',
        'Install DotRush',
        'Later'
    );

    if (choice === 'Install DotRush') {
        try {
            await vscode.window.withProgress(
                {
                    location: vscode.ProgressLocation.Notification,
                    title: 'Installing DotRush...',
                    cancellable: false
                },
                async () => {
                    await vscode.commands.executeCommand(
                        'workbench.extensions.installExtension',
                        DOTRUSH_EXTENSION_ID
                    );
                }
            );
            vscode.window.showInformationMessage(
                'DotRush installed! Reload window for full C# support.',
                'Reload Now'
            ).then(action => {
                if (action === 'Reload Now') {
                    vscode.commands.executeCommand('workbench.action.reloadWindow');
                }
            });
        } catch (error) {
            vscode.window.showWarningMessage(
                `Failed to install DotRush automatically. Please install it manually from the extensions marketplace: ${DOTRUSH_EXTENSION_ID}`
            );
        }
    }
}

/**
 * Detects dotnet SDK installation and injects its directory into process.env.PATH.
 * All VS Code extensions share the same extension host process, so modifying
 * process.env.PATH here makes `dotnet` available to DotRush's spawn() calls.
 */
function injectDotnetPath(): void {
    // Check if dotnet is already in PATH
    const currentPath = process.env.PATH || '';
    const pathSep = process.platform === 'win32' ? ';' : ':';
    const dotnetExe = process.platform === 'win32' ? 'dotnet.exe' : 'dotnet';

    // Check if dotnet is already reachable
    const dirs = currentPath.split(pathSep);
    for (const dir of dirs) {
        if (dir && fs.existsSync(path.join(dir, dotnetExe))) {
            console.log(`[Antigravity Unity] dotnet found in PATH: ${dir}`);
            return; // Already accessible, no injection needed
        }
    }

    // Candidate directories per platform
    let candidates: string[];
    if (process.platform === 'darwin') {
        candidates = [
            '/usr/local/share/dotnet',
            '/opt/homebrew/bin',
        ];
    } else if (process.platform === 'win32') {
        const pf = process.env['ProgramFiles'] || 'C:\\Program Files';
        candidates = [
            path.join(pf, 'dotnet'),
        ];
    } else {
        // Linux
        const home = process.env['HOME'] || '';
        candidates = [
            '/usr/share/dotnet',
            '/usr/bin',
            '/snap/bin',
            path.join(home, '.dotnet'),
        ];
    }

    for (const dir of candidates) {
        if (fs.existsSync(path.join(dir, dotnetExe))) {
            process.env.PATH = dir + pathSep + currentPath;
            console.log(`[Antigravity Unity] Injected dotnet PATH: ${dir}`);
            return;
        }
    }

    console.warn('[Antigravity Unity] dotnet not found. DotRush may not work correctly.');
}

export function deactivate() {
    console.log('[Antigravity Unity] Extension deactivated');
}
