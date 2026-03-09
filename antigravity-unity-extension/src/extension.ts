import * as vscode from 'vscode';
import { registerCompletionProviders } from './completion/unityCompletions';
import { registerCommands } from './commands/commands';

export function activate(context: vscode.ExtensionContext) {
    console.log('[Antigravity Unity] Extension activated');

    // Register all features (debugging handled by DotRush)
    registerCompletionProviders(context);
    registerCommands(context);

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

export function deactivate() {
    console.log('[Antigravity Unity] Extension deactivated');
}
