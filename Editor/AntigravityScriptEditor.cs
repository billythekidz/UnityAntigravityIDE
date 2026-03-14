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
    const string PrefKey_Arguments = "Antigravity_Arguments";
    const string PrefKey_Extensions = "Antigravity_UserExtensions";

    // ✅ LEARN: Proper filename-based detection like com.unity.ide.vscode
    // NOTE: All names here must be lowercase, with NO spaces or dashes.
    // Detection normalizes filenames by lowercasing and stripping spaces/dashes.
    static readonly string[] k_SupportedFileNames =
    {
        // Windows
        "antigravity.exe",
        "antigravityide.exe",
        // macOS (.app bundles and inner binaries)
        "antigravity.app",
        "antigravityide.app",
        "antigravity",
        "antigravityide",
        // Linux
        "antigravityide",
    };

    static readonly string DefaultArgument = "\"$(ProjectPath)\" -g \"$(File)\":$(Line):$(Column)";

    string m_Arguments;
    string Arguments
    {
        get => m_Arguments ?? (m_Arguments = EditorPrefs.GetString(PrefKey_Arguments, DefaultArgument));
        set
        {
            m_Arguments = value;
            EditorPrefs.SetString(PrefKey_Arguments, value);
        }
    }

    // ✅ LEARN: HandledExtensions from com.unity.ide.vscode
    static string[] DefaultExtensions
    {
        get
        {
            var customExtensions = new[] { "json", "asmdef", "asmref", "log", "shader", "compute", "hlsl", "cginc", "uss", "uxml" };
            return EditorSettings.projectGenerationBuiltinExtensions
                .Concat(EditorSettings.projectGenerationUserExtensions)
                .Concat(customExtensions)
                .Distinct().ToArray();
        }
    }

    static string HandledExtensionsString
    {
        get => EditorPrefs.GetString(PrefKey_Extensions, string.Join(";", DefaultExtensions));
        set => EditorPrefs.SetString(PrefKey_Extensions, value);
    }

    static string[] HandledExtensions => HandledExtensionsString
        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(s => s.TrimStart('.', '*'))
        .ToArray();

    /// <summary>
    /// Normalizes a filename for comparison by lowercasing and stripping spaces and dashes.
    /// e.g. "Antigravity IDE.app" → "antigravityide.app", "antigravity-ide" → "antigravityide"
    /// </summary>
    private static string NormalizeFileName(string filename)
    {
        return filename.ToLower().Replace(" ", "").Replace("-", "");
    }

    private static string[] KnownPaths
    {
        get
        {
            var paths = new List<string>();

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                // System Applications
                paths.Add("/Applications/Antigravity.app");
                paths.Add("/Applications/Antigravity IDE.app");
                // User Applications
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                paths.Add(Path.Combine(userProfile, "Applications", "Antigravity.app"));
                paths.Add(Path.Combine(userProfile, "Applications", "Antigravity IDE.app"));
                // Homebrew
                paths.Add("/opt/homebrew/bin/antigravity");
                paths.Add("/usr/local/bin/antigravity");
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                paths.Add(Path.Combine(localAppData, "Programs", "Antigravity", "Antigravity.exe"));
                paths.Add(Path.Combine(localAppData, "Programs", "Antigravity IDE", "Antigravity IDE.exe"));

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
        var editor = new AntigravityScriptEditor();
        CodeEditor.Register(editor);

        if (IsAntigravityInstallation(CodeEditor.CurrentEditorInstallation))
        {
            editor.CreateIfDoesntExist();
        }
    }

    // ✅ LEARN: CreateIfDoesntExist pattern from com.unity.ide.vscode
    public void CreateIfDoesntExist()
    {
        if (!File.Exists(GetSolutionPath()))
        {
            ProjectGeneration.Sync();
        }
    }

    private static string GetSolutionPath()
    {
        string projectName = Path.GetFileName(Directory.GetCurrentDirectory());
        return Path.Combine(Directory.GetCurrentDirectory(), $"{projectName}.sln");
    }

    private static bool IsAntigravityInstalled()
    {
        return KnownPaths.Any(p => File.Exists(p) || Directory.Exists(p));
    }

    // ✅ LEARN: Filename-based check like IsVSCodeInstallation
    private static bool IsAntigravityInstallation(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        // Check filename directly
        var filename = Path.GetFileName(path);
        var normalized = NormalizeFileName(filename);
        if (k_SupportedFileNames.Contains(normalized))
            return true;

        // On macOS, the inner binary might be "Electron" inside "Antigravity.app"
        // Check if any parent directory is an Antigravity .app bundle
        if (path.IndexOf("Antigravity", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        return false;
    }

    private static string GetExecutablePath(string path)
    {
        // .app bundle resolution — macOS only
        if (Application.platform == RuntimePlatform.OSXEditor && path.EndsWith(".app"))
        {
            // Try the CLI binary first (reliable for --goto args)
            string cliBinary = Path.Combine(path, "Contents", "Resources", "app", "bin", "antigravity");
            if (File.Exists(cliBinary)) return cliBinary;

            // Fallback: inner binary in MacOS/
            string macosDir = Path.Combine(path, "Contents", "MacOS");
            if (Directory.Exists(macosDir))
            {
                string appName = Path.GetFileNameWithoutExtension(path);
                string executable = Path.Combine(macosDir, appName);
                if (File.Exists(executable)) return executable;

                foreach (var name in new[] { "Antigravity", "Antigravity IDE", "antigravity", "Electron" })
                {
                    executable = Path.Combine(macosDir, name);
                    if (File.Exists(executable)) return executable;
                }

                try
                {
                    var files = Directory.GetFiles(macosDir);
                    if (files.Length > 0) return files[0];
                }
                catch (Exception) { }
            }
            return path;
        }
        return path;
    }

    public CodeEditor.Installation[] Installations
    {
        get
        {
            var installations = new List<CodeEditor.Installation>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in KnownPaths)
            {
                if (File.Exists(path) || Directory.Exists(path))
                {
                    // Prefer .app bundle paths on macOS — skip inner binaries if the .app is already listed
                    string canonicalPath = path;
                    if (!path.EndsWith(".app") && Application.platform == RuntimePlatform.OSXEditor)
                    {
                        int appIdx = path.IndexOf(".app/", StringComparison.OrdinalIgnoreCase);
                        if (appIdx >= 0)
                        {
                            canonicalPath = path.Substring(0, appIdx + 4);
                        }
                    }

                    if (seenPaths.Add(canonicalPath))
                    {
                        installations.Add(new CodeEditor.Installation
                        {
                            Name = EditorName,
                            Path = canonicalPath
                        });
                    }
                }
            }
            return installations.ToArray();
        }
    }

    public void Initialize(string editorInstallationPath)
    {
        ProjectGeneration.Sync();
    }

    public void OnGUI()
    {
        GUILayout.Label("Antigravity IDE Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // Arguments
        Arguments = EditorGUILayout.TextField("External Script Editor Args", Arguments);
        if (GUILayout.Button("Reset argument", GUILayout.Width(120)))
        {
            Arguments = DefaultArgument;
        }

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
            new GUIContent("Debug Port", "TCP port for Unity debugger attachment (used in launch.json)"),
            debugPort);
        if (newDebugPort != debugPort)
            EditorPrefs.SetInt(PrefKey_DebugPort, newDebugPort);

        EditorGUILayout.Space(2);

        // Launch.json generation
        bool genLaunchJson = EditorPrefs.GetBool(PrefKey_GenerateLaunchJson, true);
        bool newGenLaunchJson = EditorGUILayout.Toggle(
            new GUIContent("Generate launch.json", "Auto-generate .vscode/launch.json for Unity debugging via DotRush"),
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

        EditorGUILayout.Space(4);

        // Generate .csproj flags (always enabled — we sync all packages)
        GUILayout.Label("Generate .csproj files for:", EditorStyles.label);
        EditorGUI.indentLevel++;
        EditorGUILayout.Toggle(new GUIContent("Embedded packages"), true);
        EditorGUILayout.Toggle(new GUIContent("Local packages"), true);
        EditorGUILayout.Toggle(new GUIContent("Registry packages"), true);
        EditorGUILayout.Toggle(new GUIContent("Git packages"), true);
        EditorGUILayout.Toggle(new GUIContent("Built-in packages"), true);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(4);

        // ✅ LEARN: HandledExtensions UI from com.unity.ide.vscode
        HandledExtensionsString = EditorGUILayout.TextField(
            new GUIContent("Extensions handled:"), HandledExtensionsString);

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
            EditorPrefs.DeleteKey(PrefKey_Arguments);
            EditorPrefs.DeleteKey(PrefKey_Extensions);
            UnityEngine.Debug.Log("[Antigravity] Settings reset to defaults.");
        }
        EditorGUILayout.EndHorizontal();
    }


    public bool OpenProject(string filePath, int line, int column)
    {
        if (filePath != "" && (!SupportsExtension(filePath) || !File.Exists(filePath)))
        {
            return false;
        }

        if (line == -1) line = 1;
        if (column == -1) column = 0;

        string installation = CodeEditor.CurrentEditorInstallation;
        string projectDir = Directory.GetCurrentDirectory();

        if (string.IsNullOrEmpty(filePath))
            filePath = projectDir;

        bool reuseWindow = EditorPrefs.GetBool(PrefKey_ReuseWindow, true);

        try
        {
            var process = new Process();

            if (Application.platform == RuntimePlatform.OSXEditor && installation.EndsWith(".app"))
            {
                process.StartInfo.FileName = "/usr/bin/open";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                if (Directory.Exists(filePath))
                {
                    // Opening a folder — use `open -a` (native macOS, reuses existing app)
                    process.StartInfo.Arguments = $"-a \"{installation}\" \"{filePath}\"";
                }
                else
                {
                    // Opening a file at line:col — use antigravity:// URL scheme
                    // macOS routes this to the existing app instance (no new dock icon)
                    string uri = $"antigravity://file{filePath}:{line}:{column}";
                    process.StartInfo.Arguments = $"\"{uri}\"";
                }
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                // Direct binary path (e.g. Homebrew symlink)
                process.StartInfo.FileName = installation;

                var args = new List<string>();
                if (reuseWindow) args.Add("--reuse-window");

                if (Directory.Exists(filePath))
                {
                    args.Add($"\"{filePath}\"");
                }
                else
                {
                    args.Add("--goto");
                    args.Add($"\"{filePath}:{line}:{column}\"");
                }

                process.StartInfo.Arguments = string.Join(" ", args);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
            }
            else
            {
                // Windows / Linux
                process.StartInfo.FileName = GetExecutablePath(installation);

                var args = new List<string>();
                if (reuseWindow) args.Add("--reuse-window");

                if (Directory.Exists(filePath))
                {
                    args.Add($"\"{filePath}\"");
                }
                else
                {
                    args.Add("--goto");
                    args.Add($"\"{filePath}:{line}:{column}\"");
                }

                process.StartInfo.Arguments = string.Join(" ", args);
                process.StartInfo.WindowStyle = installation.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)
                    ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = true;
            }

            process.Start();
            return true;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[Antigravity] Failed to open editor: {e.Message}");
            return false;
        }
    }

    // ✅ LEARN: SupportsExtension check from com.unity.ide.vscode
    static bool SupportsExtension(string path)
    {
        var extension = Path.GetExtension(path);
        if (string.IsNullOrEmpty(extension)) return false;
        return HandledExtensions.Contains(extension.TrimStart('.'));
    }

    public void SyncAll()
    {
        // ✅ LEARN: ResetPackageInfoCache before sync
        AssetDatabase.Refresh();
        ProjectGeneration.Sync();
    }

    public void SyncIfNeeded(string[] addedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, string[] importedAssets)
    {
        ProjectGeneration.SyncIfNeeded(addedAssets, deletedAssets, movedAssets, movedFromAssetPaths, importedAssets);
    }

    public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
    {
        var filename = Path.GetFileName(editorPath);
        var normalized = NormalizeFileName(filename);
        bool filenameMatch = k_SupportedFileNames.Contains(normalized);
        bool pathMatch = editorPath.IndexOf("antigravity", StringComparison.OrdinalIgnoreCase) >= 0;

        if (!filenameMatch && !pathMatch)
        {
            installation = default;
            return false;
        }

        // If the path points to an inner binary of a .app, use the .app path instead
        string installPath = editorPath;
        if (Application.platform == RuntimePlatform.OSXEditor)
        {
            int appIdx = editorPath.IndexOf(".app", StringComparison.OrdinalIgnoreCase);
            if (appIdx >= 0)
            {
                installPath = editorPath.Substring(0, appIdx + 4);
            }
        }

        installation = new CodeEditor.Installation
        {
            Name = EditorName,
            Path = installPath
        };
        return true;
    }
}
