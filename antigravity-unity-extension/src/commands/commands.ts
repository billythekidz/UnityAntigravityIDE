import * as vscode from 'vscode';
import { discoverUnityInstances, sendBridgeCommand } from '../debugger/debugAdapter';

export function registerCommands(context: vscode.ExtensionContext) {
    // Attach Unity Debugger
    context.subscriptions.push(
        vscode.commands.registerCommand('antigravity-unity.attachDebugger', async () => {
            const statusMessage = vscode.window.setStatusBarMessage('$(sync~spin) Discovering Unity instances...');

            try {
                const instances = await discoverUnityInstances();

                if (instances.length === 0) {
                    const action = await vscode.window.showWarningMessage(
                        'No Unity instances found. Make sure the Debug Bridge is running in Unity (Antigravity > Start Debug Bridge).',
                        'Enter Manually'
                    );

                    if (action === 'Enter Manually') {
                        const endPoint = await vscode.window.showInputBox({
                            prompt: 'Enter Unity debug endpoint (IP:Port)',
                            value: '127.0.0.1:56000',
                            placeHolder: '127.0.0.1:56000'
                        });

                        if (endPoint) {
                            const [host, portStr] = endPoint.split(':');
                            const port = parseInt(portStr, 10);
                            await startDebugSession(host, port, 'Unity Player');
                        }
                    }
                    return;
                }

                if (instances.length === 1) {
                    const inst = instances[0];
                    await startDebugSession(inst.host, inst.port, inst.name);
                    return;
                }

                // Multiple instances — let user pick
                const items = instances.map(inst => ({
                    label: inst.name,
                    description: `${inst.host}:${inst.port}`,
                    detail: `Unity ${inst.unityVersion || 'Unknown'} ${inst.isPlaying ? '▶ Playing' : '⏸ Not Playing'}`,
                    instance: inst
                }));

                const selected = await vscode.window.showQuickPick(items, {
                    placeHolder: 'Select a Unity instance to debug',
                    title: 'Attach Unity Debugger'
                });

                if (selected) {
                    await startDebugSession(
                        selected.instance.host,
                        selected.instance.port,
                        selected.instance.name
                    );
                }
            } finally {
                statusMessage.dispose();
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
            // Try to send command to Unity via debug bridge
            const port = vscode.workspace.getConfiguration('antigravity-unity').get<number>('bridgePort', 56000);

            const result = await sendBridgeCommand('127.0.0.1', port, { type: 'regenerate_projects' });

            if (result) {
                vscode.window.showInformationMessage('Project files regenerated via Unity.');
            } else {
                // Fallback — Unity isn't connected, just inform user
                const action = await vscode.window.showWarningMessage(
                    'Could not connect to Unity. Please regenerate project files from Unity Editor (Antigravity > Regenerate Project Files in Preferences).',
                    'Open Terminal'
                );

                if (action === 'Open Terminal') {
                    const terminal = vscode.window.createTerminal('Unity Project');
                    terminal.show();
                    terminal.sendText('echo "Please regenerate project files from Unity Editor"');
                }
            }
        })
    );
}

async function startDebugSession(host: string, port: number, name: string): Promise<void> {
    const config: vscode.DebugConfiguration = {
        type: 'antigravity-unity',
        name: `Attach to ${name}`,
        request: 'attach',
        host: host,
        port: port,
    };

    const success = await vscode.debug.startDebugging(undefined, config);
    if (success) {
        vscode.window.showInformationMessage(`Attached to ${name} (${host}:${port})`);
    } else {
        vscode.window.showErrorMessage(`Failed to attach to ${name}`);
    }
}
