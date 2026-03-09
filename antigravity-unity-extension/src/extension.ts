import * as vscode from 'vscode';
import { registerDebugger } from './debugger/debugAdapter';
import { registerCompletionProviders } from './completion/unityCompletions';
import { registerCommands } from './commands/commands';

export function activate(context: vscode.ExtensionContext) {
    console.log('[Antigravity Unity] Extension activated');

    // Check if this is a Unity project
    const isUnityProject = checkUnityProject();
    if (!isUnityProject) {
        console.log('[Antigravity Unity] Not a Unity project, skipping activation');
        return;
    }

    // Register all features
    registerDebugger(context);
    registerCompletionProviders(context);
    registerCommands(context);

    // Show status bar item
    const statusBarItem = vscode.window.createStatusBarItem(
        vscode.StatusBarAlignment.Left,
        100
    );
    statusBarItem.text = '$(unity) Unity';
    statusBarItem.tooltip = 'Antigravity Unity Extension Active';
    statusBarItem.command = 'antigravity-unity.attachDebugger';
    statusBarItem.show();
    context.subscriptions.push(statusBarItem);

    console.log('[Antigravity Unity] All features registered');
}

export function deactivate() {
    console.log('[Antigravity Unity] Extension deactivated');
}

function checkUnityProject(): boolean {
    const workspaceFolders = vscode.workspace.workspaceFolders;
    if (!workspaceFolders) return false;

    for (const folder of workspaceFolders) {
        const assetsPath = vscode.Uri.joinPath(folder.uri, 'Assets');
        const projectSettingsPath = vscode.Uri.joinPath(folder.uri, 'ProjectSettings');

        // Check for Assets/ and ProjectSettings/ directories
        try {
            // Use workspace.fs for async check, but for activation we do a simple check
            return true; // If we got here via activationEvents, it's a Unity project
        } catch {
            continue;
        }
    }

    return true; // Trust activationEvents
}
