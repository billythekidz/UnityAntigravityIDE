import * as vscode from 'vscode';
import * as net from 'net';

interface UnityInstance {
    name: string;
    host: string;
    port: number;
    projectName?: string;
    unityVersion?: string;
    isPlaying?: boolean;
}

interface DebugInfo {
    type: string;
    unity_version: string;
    project_name: string;
    project_path: string;
    mono_debugger_port: number;
    is_playing: boolean;
    is_paused: boolean;
    process_id: number;
}

export function registerDebugger(context: vscode.ExtensionContext) {
    // Register debug configuration provider
    const provider = new UnityDebugConfigurationProvider();
    context.subscriptions.push(
        vscode.debug.registerDebugConfigurationProvider('antigravity-unity', provider)
    );

    // Register inline debug adapter factory
    const factory = new UnityDebugAdapterFactory();
    context.subscriptions.push(
        vscode.debug.registerDebugAdapterDescriptorFactory('antigravity-unity', factory)
    );
}

class UnityDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
    resolveDebugConfiguration(
        folder: vscode.WorkspaceFolder | undefined,
        config: vscode.DebugConfiguration,
        token?: vscode.CancellationToken
    ): vscode.ProviderResult<vscode.DebugConfiguration> {
        // If no config, provide default
        if (!config.type && !config.request && !config.name) {
            config.type = 'antigravity-unity';
            config.name = 'Attach to Unity Editor';
            config.request = 'attach';
        }

        if (!config.port) {
            config.port = 56000;
        }

        if (!config.endPoint) {
            config.endPoint = `127.0.0.1:${config.port}`;
        }

        return config;
    }
}

class UnityDebugAdapterFactory implements vscode.DebugAdapterDescriptorFactory {
    createDebugAdapterDescriptor(
        session: vscode.DebugSession,
        executable: vscode.DebugAdapterExecutable | undefined
    ): vscode.ProviderResult<vscode.DebugAdapterDescriptor> {
        // Connect to the Unity debug bridge
        const config = session.configuration;
        const port = config.port || 56000;
        const host = config.host || '127.0.0.1';

        // Use a server-based adapter that connects to Unity's Mono debugger
        return new vscode.DebugAdapterServer(port, host);
    }
}

/**
 * Discovers Unity instances on localhost by scanning common debug ports.
 */
export async function discoverUnityInstances(): Promise<UnityInstance[]> {
    const instances: UnityInstance[] = [];
    const portsToScan = [56000, 56001, 56002, 56003, 56004];

    const scanPromises = portsToScan.map(async (port) => {
        try {
            const info = await queryUnityBridge('127.0.0.1', port);
            if (info) {
                instances.push({
                    name: info.project_name || `Unity Editor (${port})`,
                    host: '127.0.0.1',
                    port: port,
                    projectName: info.project_name,
                    unityVersion: info.unity_version,
                    isPlaying: info.is_playing,
                });
            }
        } catch {
            // Port not responding, skip
        }
    });

    await Promise.all(scanPromises);
    return instances;
}

/**
 * Queries a Unity Debug Bridge instance for debug information.
 */
function queryUnityBridge(host: string, port: number): Promise<DebugInfo | null> {
    return new Promise((resolve) => {
        const client = new net.Socket();
        let data = '';

        client.setTimeout(2000);

        client.connect(port, host, () => {
            // Bridge sends debug info on connect
        });

        client.on('data', (chunk) => {
            data += chunk.toString();
            try {
                const info = JSON.parse(data) as DebugInfo;
                if (info.type === 'debug_info') {
                    resolve(info);
                    client.destroy();
                }
            } catch {
                // Wait for more data
            }
        });

        client.on('timeout', () => {
            resolve(null);
            client.destroy();
        });

        client.on('error', () => {
            resolve(null);
            client.destroy();
        });
    });
}

/**
 * Sends a command to the Unity Debug Bridge.
 */
export function sendBridgeCommand(
    host: string,
    port: number,
    command: object
): Promise<object | null> {
    return new Promise((resolve) => {
        const client = new net.Socket();
        let data = '';
        let initialInfoReceived = false;

        client.setTimeout(3000);

        client.connect(port, host, () => {
            // Wait for initial debug info, then send command
        });

        client.on('data', (chunk) => {
            data += chunk.toString();
            const lines = data.split('\n');

            for (const line of lines) {
                if (!line.trim()) continue;
                try {
                    const parsed = JSON.parse(line);
                    if (!initialInfoReceived) {
                        initialInfoReceived = true;
                        // Send our command after receiving initial info
                        client.write(JSON.stringify(command) + '\n');
                        data = '';
                    } else {
                        resolve(parsed);
                        client.destroy();
                        return;
                    }
                } catch {
                    // Partial data
                }
            }
        });

        client.on('timeout', () => {
            resolve(null);
            client.destroy();
        });

        client.on('error', () => {
            resolve(null);
            client.destroy();
        });
    });
}
