using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Provides a TCP-based debug bridge that enables Antigravity IDE to communicate
/// with the Unity Editor for debugging purposes. This bridge acts as a relay between
/// the IDE's debug adapter and Unity's Mono debugger.
/// </summary>
[InitializeOnLoad]
public static class UnityDebugBridge
{
    private static TcpListener _listener;
    private static Thread _listenerThread;
    private static bool _isRunning;
    private static readonly object _lock = new object();

    private const string PrefKey_DebugPort = "Antigravity_DebugPort";
    private const string PrefKey_AutoStartBridge = "Antigravity_AutoStartBridge";
    private const int DefaultPort = 56000;

    static UnityDebugBridge()
    {
        if (EditorPrefs.GetBool(PrefKey_AutoStartBridge, false))
        {
            StartBridge();
        }

        EditorApplication.quitting += StopBridge;
        AssemblyReloadEvents.beforeAssemblyReload += StopBridge;
    }

    [MenuItem("Antigravity/Start Debug Bridge", false, 100)]
    public static void StartBridge()
    {
        lock (_lock)
        {
            if (_isRunning) return;

            int port = EditorPrefs.GetInt(PrefKey_DebugPort, DefaultPort);

            try
            {
                _listener = new TcpListener(IPAddress.Loopback, port);
                _listener.Start();
                _isRunning = true;

                _listenerThread = new Thread(ListenForConnections)
                {
                    IsBackground = true,
                    Name = "AntigravityDebugBridge"
                };
                _listenerThread.Start();

                Debug.Log($"[Antigravity] Debug bridge started on port {port}");
                GenerateDebugInfo(port);
            }
            catch (SocketException ex)
            {
                Debug.LogError($"[Antigravity] Failed to start debug bridge on port {port}: {ex.Message}");
                _isRunning = false;
            }
        }
    }

    [MenuItem("Antigravity/Stop Debug Bridge", false, 101)]
    public static void StopBridge()
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            _isRunning = false;

            try
            {
                _listener?.Stop();
                _listenerThread?.Join(1000);
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
            finally
            {
                _listener = null;
                _listenerThread = null;
                Debug.Log("[Antigravity] Debug bridge stopped.");
            }
        }
    }

    [MenuItem("Antigravity/Stop Debug Bridge", true)]
    private static bool ValidateStopBridge()
    {
        return _isRunning;
    }

    [MenuItem("Antigravity/Start Debug Bridge", true)]
    private static bool ValidateStartBridge()
    {
        return !_isRunning;
    }

    private static void ListenForConnections()
    {
        while (_isRunning)
        {
            try
            {
                if (_listener != null && _listener.Pending())
                {
                    var client = _listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            catch (SocketException)
            {
                if (_isRunning)
                {
                    Debug.LogWarning("[Antigravity] Debug bridge listener encountered an error.");
                }
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private static void HandleClient(object state)
    {
        var client = (TcpClient)state;
        try
        {
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                // Respond with debug info
                var debugInfo = GetDebugInfoJson();
                writer.WriteLine(debugInfo);

                // Keep connection alive for debug commands
                while (client.Connected && _isRunning)
                {
                    if (stream.DataAvailable)
                    {
                        string command = reader.ReadLine();
                        if (string.IsNullOrEmpty(command)) break;

                        string response = ProcessCommand(command);
                        writer.WriteLine(response);
                    }
                    else
                    {
                        Thread.Sleep(50);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                Debug.LogWarning($"[Antigravity] Debug client disconnected: {ex.Message}");
            }
        }
        finally
        {
            client.Close();
        }
    }

    private static string ProcessCommand(string command)
    {
        try
        {
            // Simple JSON-based command protocol
            if (command.Contains("\"type\":\"ping\""))
            {
                return "{\"type\":\"pong\",\"status\":\"ok\"}";
            }
            else if (command.Contains("\"type\":\"info\""))
            {
                return GetDebugInfoJson();
            }
            else if (command.Contains("\"type\":\"pause\""))
            {
                EditorApplication.delayCall += () => EditorApplication.isPaused = true;
                return "{\"type\":\"response\",\"status\":\"paused\"}";
            }
            else if (command.Contains("\"type\":\"resume\""))
            {
                EditorApplication.delayCall += () => EditorApplication.isPaused = false;
                return "{\"type\":\"response\",\"status\":\"resumed\"}";
            }
            else if (command.Contains("\"type\":\"play\""))
            {
                EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
                return "{\"type\":\"response\",\"status\":\"playing\"}";
            }
            else if (command.Contains("\"type\":\"stop\""))
            {
                EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
                return "{\"type\":\"response\",\"status\":\"stopped\"}";
            }
            else
            {
                return "{\"type\":\"error\",\"message\":\"unknown command\"}";
            }
        }
        catch (Exception ex)
        {
            return $"{{\"type\":\"error\",\"message\":\"{ex.Message.Replace("\"", "\\\"")}\"}}";
        }
    }

    private static string GetDebugInfoJson()
    {
        int monoDebuggerPort = GetMonoDebuggerPort();
        return $"{{" +
               $"\"type\":\"debug_info\"," +
               $"\"unity_version\":\"{Application.unityVersion}\"," +
               $"\"project_name\":\"{Path.GetFileName(Directory.GetCurrentDirectory())}\"," +
               $"\"project_path\":\"{Directory.GetCurrentDirectory().Replace("\\", "\\\\")}\"," +
               $"\"mono_debugger_port\":{monoDebuggerPort}," +
               $"\"is_playing\":{(EditorApplication.isPlaying ? "true" : "false")}," +
               $"\"is_paused\":{(EditorApplication.isPaused ? "true" : "false")}," +
               $"\"process_id\":{System.Diagnostics.Process.GetCurrentProcess().Id}" +
               $"}}";
    }

    private static int GetMonoDebuggerPort()
    {
        // Unity's Mono debugger typically listens on port 56000 + offset
        // The actual port can be found in the EditorUserBuildSettings
        try
        {
            string projectDir = Directory.GetCurrentDirectory();
            string debugInfoPath = Path.Combine(projectDir, "Library", "EditorInstance.json");

            if (File.Exists(debugInfoPath))
            {
                string content = File.ReadAllText(debugInfoPath);
                // Simple parsing for process_id to help locate debugger port
                // The actual Mono debugger port is determined at runtime
            }
        }
        catch (Exception)
        {
            // Fallback
        }

        return 56000;
    }

    private static void GenerateDebugInfo(int bridgePort)
    {
        string projectDir = Directory.GetCurrentDirectory();
        string vscodeDir = Path.Combine(projectDir, ".vscode");

        if (!Directory.Exists(vscodeDir))
        {
            Directory.CreateDirectory(vscodeDir);
        }

        // Update launch.json with active bridge port
        string launchPath = Path.Combine(vscodeDir, "launch.json");
        string launchContent = $@"{{
    ""version"": ""0.2.0"",
    ""configurations"": [
        {{
            ""name"": ""Attach to Unity Editor"",
            ""type"": ""vstuc"",
            ""request"": ""attach""
        }},
        {{
            ""name"": ""Attach to Unity Player"",
            ""type"": ""vstuc"",
            ""request"": ""attach"",
            ""endPoint"": ""127.0.0.1:{bridgePort}""
        }},
        {{
            ""name"": ""Antigravity: Unity Debug Bridge"",
            ""type"": ""antigravity-unity"",
            ""request"": ""attach"",
            ""port"": {bridgePort}
        }}
    ]
}}";
        File.WriteAllText(launchPath, launchContent);
    }
}
