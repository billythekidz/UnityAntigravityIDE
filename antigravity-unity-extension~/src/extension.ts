import * as vscode from 'vscode';
import { registerCompletionProviders } from './completion/unityCompletions';
import { registerCommands } from './commands/commands';
// import { registerCsprojFixer } from './csproj/csprojFixer'; // Disabled: interferes with DotRush compilation

const DOTRUSH_EXTENSION_ID = 'nromanov.dotrush';

export async function activate(context: vscode.ExtensionContext) {
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

export function deactivate() {
    console.log('[Antigravity Unity] Extension deactivated');
}
