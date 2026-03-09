using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AntigravityScriptEditor : IExternalCodeEditor
{
    const string EditorName = "Antigravity";
    const string PrefKey_DebugPort = "Antigravity_DebugPort";
    const string PrefKey_ReuseWindow = "Antigravity_ReuseWindow";
    const string PrefKey_GenerateLaunchJson = "Antigravity_GenerateLaunchJson";
    const string PrefKey_AnalyzerLevel = "Antigravity_AnalyzerLevel";

    private static string[] KnownPaths
    {
        get
        {
            var paths = new List<string>();

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                paths.Add("/Applications/Antigravity.app");
                paths.Add("/Applications/Antigravity.app/Contents/MacOS/Antigravity");
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                paths.Add(Path.Combine(localAppData, "Programs", "Antigravity", "Antigravity.exe"));

                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                paths.Add(Path.Combine(programFiles, "Antigravity", "Antigravity.exe"));
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                paths.Add("/opt/Antigravity/antigravity");
                paths.Add("/usr/bin/antigravity");
                paths.Add("/usr/local/bin/antigravity");

                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                paths.Add(Path.Combine(userProfile, ".local", "bin", "antigravity"));
            }

            return paths.ToArray();
        }
    }

    static AntigravityScriptEditor()
    {
        CodeEditor.Register(new AntigravityScriptEditor());

        string current = EditorPrefs.GetString("kScriptsDefaultApp");
        if (IsAntigravityInstalled() && !current.Contains(EditorName))
        {
            // Registration handles availability; user preference is respected unless explicitly changed.
        }
    }

    private static bool IsAntigravityInstalled()
    {
        return KnownPaths.Any(p => File.Exists(p) || Directory.Exists(p));
    }

    private static string GetExecutablePath(string path)
    {
        if (path.EndsWith(".app"))
        {
            string executable = Path.Combine(path, "Contents", "MacOS", "Antigravity");
            return File.Exists(executable) ? executable : path;
        }
        return path;
    }

    public CodeEditor.Installation[] Installations
    {
        get
        {
            var installations = new List<CodeEditor.Installation>();
            foreach (var path in KnownPaths)
            {
                if (File.Exists(path) || Directory.Exists(path))
                {
                    installations.Add(new CodeEditor.Installation
                    {
                        Name = EditorName,
                        Path = path
                    });
                }
            }
            return installations.ToArray();
        }
    }

    public void Initialize(string editorInstallationPath)
    {
        // Ensure project files are generated on initialization
        ProjectGeneration.Sync();
    }

    public void OnGUI()
    {
        GUILayout.Label("Antigravity IDE Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // Reuse window preference
        bool reuseWindow = EditorPrefs.GetBool(PrefKey_ReuseWindow, true);
        bool newReuseWindow = EditorGUILayout.Toggle(
            new GUIContent("Reuse Window", "Open files in existing Antigravity window instead of launching a new one"),
            reuseWindow);
        if (newReuseWindow != reuseWindow)
            EditorPrefs.SetBool(PrefKey_ReuseWindow, newReuseWindow);

        EditorGUILayout.Space(2);

        // Debug port
        int debugPort = EditorPrefs.GetInt(PrefKey_DebugPort, 56000);
        int newDebugPort = EditorGUILayout.IntField(
            new GUIContent("Debug Port", "TCP port for Unity debugger attachment"),
            debugPort);
        if (newDebugPort != debugPort)
            EditorPrefs.SetInt(PrefKey_DebugPort, newDebugPort);

        EditorGUILayout.Space(2);

        // Launch.json generation
        bool genLaunchJson = EditorPrefs.GetBool(PrefKey_GenerateLaunchJson, true);
        bool newGenLaunchJson = EditorGUILayout.Toggle(
            new GUIContent("Generate launch.json", "Auto-generate .vscode/launch.json for Unity debugging"),
            genLaunchJson);
        if (newGenLaunchJson != genLaunchJson)
            EditorPrefs.SetBool(PrefKey_GenerateLaunchJson, newGenLaunchJson);

        EditorGUILayout.Space(2);

        // Analyzer level
        string[] analyzerOptions = { "None", "Default", "Recommended", "All" };
        int analyzerLevel = EditorPrefs.GetInt(PrefKey_AnalyzerLevel, 1);
        int newAnalyzerLevel = EditorGUILayout.Popup(
            new GUIContent("Analyzer Level", "Configure Roslyn analyzer severity level"),
            analyzerLevel, analyzerOptions);
        if (newAnalyzerLevel != analyzerLevel)
            EditorPrefs.SetInt(PrefKey_AnalyzerLevel, newAnalyzerLevel);

        EditorGUILayout.Space(8);

        // Action buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Regenerate Project Files", GUILayout.Height(24)))
        {
            ProjectGeneration.Sync();
            UnityEngine.Debug.Log("[Antigravity] Project files regenerated.");
        }

        if (GUILayout.Button("Reset Settings", GUILayout.Height(24)))
        {
            EditorPrefs.DeleteKey(PrefKey_DebugPort);
            EditorPrefs.DeleteKey(PrefKey_ReuseWindow);
            EditorPrefs.DeleteKey(PrefKey_GenerateLaunchJson);
            EditorPrefs.DeleteKey(PrefKey_AnalyzerLevel);
            UnityEngine.Debug.Log("[Antigravity] Settings reset to defaults.");
        }
        EditorGUILayout.EndHorizontal();
    }

    public bool OpenProject(string filePath, int line, int column)
    {
        string installation = CodeEditor.CurrentEditorInstallation;
        string projectDir = Directory.GetCurrentDirectory();

        // If no specific file, just open the project folder
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = projectDir;
        }

        bool reuseWindow = EditorPrefs.GetBool(PrefKey_ReuseWindow, true);

        // Build arguments: always pass workspace folder first
        var args = new List<string>();

        if (reuseWindow)
        {
            args.Add("--reuse-window");
        }

        if (Directory.Exists(filePath))
        {
            // Opening a folder
            args.Add($"\"{filePath}\"");
        }
        else
        {
            // Opening a file — pass workspace folder first, then file with goto
            args.Add($"\"{projectDir}\"");
            args.Add("--goto");
            args.Add($"\"{filePath}:{line}:{column}\"");
        }

        string arguments = string.Join(" ", args);

        try
        {
            Process process = new Process();

            // Handle macOS .app bundles specifically
            if (installation.EndsWith(".app") && Application.platform == RuntimePlatform.OSXEditor)
            {
                process.StartInfo.FileName = "/usr/bin/open";
                process.StartInfo.Arguments = $"-a \"{installation}\" -n --args {arguments}";
            }
            else
            {
                process.StartInfo.FileName = GetExecutablePath(installation);
                process.StartInfo.Arguments = arguments;
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            return true;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[Antigravity] Failed to open editor: {e.Message}");
            return false;
        }
    }

    public void SyncAll()
    {
        ProjectGeneration.Sync();
    }

    public void SyncIfNeeded(string[] addedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, string[] importedAssets)
    {
        ProjectGeneration.SyncIfNeeded(addedAssets, deletedAssets, movedAssets, movedFromAssetPaths, importedAssets);
    }

    public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
    {
        if (editorPath.Contains("Antigravity"))
        {
            installation = new CodeEditor.Installation
            {
                Name = EditorName,
                Path = editorPath
            };
            return true;
        }

        installation = default;
        return false;
    }
}
