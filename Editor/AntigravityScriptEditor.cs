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
    static readonly string[] k_SupportedFileNames =
    {
        // Windows
        "antigravity.exe",
        "antigravity-ide.exe",
        // macOS
        "antigravity.app",
        "antigravity-ide.app",
        "antigravity",
        // Linux
        "antigravity-ide",
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

    private static string[] KnownPaths
    {
        get
        {
            var paths = new List<string>();

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                paths.Add("/Applications/Antigravity.app");
                paths.Add("/Applications/Antigravity.app/Contents/MacOS/Antigravity");
                paths.Add("/Applications/Antigravity IDE.app");
                paths.Add("/Applications/Antigravity IDE.app/Contents/MacOS/Antigravity IDE");
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
        var filename = Path.GetFileName(path.ToLower())
            .Replace(" ", "")
            .Replace("\\", Path.DirectorySeparatorChar.ToString())
            .Replace("/", Path.DirectorySeparatorChar.ToString());
        return k_SupportedFileNames.Contains(filename);
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

        // ✅ LEARN: Generate .csproj flags like com.unity.ide.vscode
        GUILayout.Label("Generate .csproj files for:", EditorStyles.label);
        EditorGUI.indentLevel++;
        SettingsButton(ProjectGenerationFlag.Embedded, "Embedded packages");
        SettingsButton(ProjectGenerationFlag.Local, "Local packages");
        SettingsButton(ProjectGenerationFlag.Registry, "Registry packages");
        SettingsButton(ProjectGenerationFlag.Git, "Git packages");
        SettingsButton(ProjectGenerationFlag.BuiltIn, "Built-in packages");
        SettingsButton(ProjectGenerationFlag.Unknown, "Packages from unknown sources");
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

    void SettingsButton(ProjectGenerationFlag preference, string guiMessage)
    {
        var prevValue = EditorSettings.projectGenerationUserExtensions.Length >= 0 &&
            (preference == ProjectGenerationFlag.BuiltIn ||
             preference == ProjectGenerationFlag.Embedded ||
             preference == ProjectGenerationFlag.Local ||
             preference == ProjectGenerationFlag.Registry ||
             preference == ProjectGenerationFlag.Git ||
             preference == ProjectGenerationFlag.Unknown);
        // Toggle visual only — actual generation controlled by ProjectGeneration flags
        EditorGUILayout.Toggle(new GUIContent(guiMessage), prevValue);
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

        var args = new List<string>();

        if (reuseWindow)
            args.Add("--reuse-window");

        if (Directory.Exists(filePath))
        {
            args.Add($"\"{filePath}\"");
        }
        else
        {
            args.Add($"\"{projectDir}\"");
            args.Add("--goto");
            args.Add($"\"{filePath}:{line}:{column}\"");
        }

        string arguments = string.Join(" ", args);

        try
        {
            var process = new Process();

            if (installation.EndsWith(".app") && Application.platform == RuntimePlatform.OSXEditor)
            {
                process.StartInfo.FileName = "/usr/bin/open";
                process.StartInfo.Arguments = $"-n \"{installation}\" --args {arguments}";
            }
            else
            {
                process.StartInfo.FileName = GetExecutablePath(installation);
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WindowStyle = installation.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)
                    ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal;
            }

            process.StartInfo.UseShellExecute = true;
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
        // ✅ LEARN: Use filename-based detection, not string.Contains()
        var lowerCasePath = editorPath.ToLower();
        var filename = Path.GetFileName(lowerCasePath).Replace(" ", "");

        if (!k_SupportedFileNames.Contains(filename))
        {
            // Fallback: still accept if path explicitly contains "Antigravity"
            if (!editorPath.Contains("Antigravity") && !editorPath.Contains("antigravity"))
            {
                installation = default;
                return false;
            }
        }

        installation = new CodeEditor.Installation
        {
            Name = EditorName,
            Path = editorPath
        };
        return true;
    }
}
