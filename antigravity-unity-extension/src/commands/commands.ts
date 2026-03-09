import * as vscode from 'vscode';

export function registerCommands(context: vscode.ExtensionContext) {
    // Attach Unity Debugger (uses DotRush's "unity" debugger type)
    context.subscriptions.push(
        vscode.commands.registerCommand('antigravity-unity.attachDebugger', async () => {
            const config: vscode.DebugConfiguration = {
                type: 'unity',
                name: 'Unity Debugger',
                request: 'attach',
            };

            const success = await vscode.debug.startDebugging(undefined, config);
            if (success) {
                vscode.window.showInformationMessage('Attached to Unity Editor via DotRush');
            } else {
                vscode.window.showWarningMessage(
                    'Failed to attach. Make sure DotRush extension is installed and Unity Editor is running.'
                );
            }
        })
    );

    // Unity API Reference
    context.subscriptions.push(
        vscode.commands.registerCommand('antigravity-unity.openApiReference', async () => {
            const editor = vscode.window.activeTextEditor;
            let searchTerm = '';

            if (editor) {
                const selection = editor.selection;
                if (!selection.isEmpty) {
                    searchTerm = editor.document.getText(selection);
                } else {
                    const wordRange = editor.document.getWordRangeAtPosition(selection.active);
                    if (wordRange) {
                        searchTerm = editor.document.getText(wordRange);
                    }
                }
            }

            if (!searchTerm) {
                searchTerm = await vscode.window.showInputBox({
                    prompt: 'Enter Unity API class or method name',
                    placeHolder: 'e.g., Transform, Rigidbody, Vector3'
                }) || '';
            }

            if (searchTerm) {
                const url = `https://docs.unity3d.com/ScriptReference/30_search.html?q=${encodeURIComponent(searchTerm)}`;
                vscode.env.openExternal(vscode.Uri.parse(url));
            }
        })
    );

    // Regenerate Project Files
    context.subscriptions.push(
        vscode.commands.registerCommand('antigravity-unity.regenerateProjectFiles', async () => {
            vscode.window.showInformationMessage(
                'Please regenerate project files from Unity Editor: Edit > Preferences > External Tools > Regenerate project files'
            );
        })
    );
}
