using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates analyzer configuration files that integrate Unity Roslyn analyzers
/// with Antigravity IDE's C# language server. Produces .editorconfig and 
/// Directory.Build.props files for proper Unity-specific diagnostics.
/// </summary>
public static class UnityAnalyzerConfig
{
    private const string PrefKey_AnalyzerLevel = "Antigravity_AnalyzerLevel";

    // Unity API message methods that should not trigger "unused" warnings
    private static readonly string[] UnityMessages = new[]
    {
        "Awake", "Start", "Update", "LateUpdate", "FixedUpdate",
        "OnEnable", "OnDisable", "OnDestroy",
        "OnCollisionEnter", "OnCollisionExit", "OnCollisionStay",
        "OnCollisionEnter2D", "OnCollisionExit2D", "OnCollisionStay2D",
        "OnTriggerEnter", "OnTriggerExit", "OnTriggerStay",
        "OnTriggerEnter2D", "OnTriggerExit2D", "OnTriggerStay2D",
        "OnMouseDown", "OnMouseUp", "OnMouseEnter", "OnMouseExit",
        "OnMouseOver", "OnMouseDrag", "OnMouseUpAsButton",
        "OnBecameVisible", "OnBecameInvisible",
        "OnApplicationFocus", "OnApplicationPause", "OnApplicationQuit",
        "OnDrawGizmos", "OnDrawGizmosSelected",
        "OnGUI", "OnValidate", "Reset",
        "OnAnimatorIK", "OnAnimatorMove",
        "OnAudioFilterRead",
        "OnParticleCollision", "OnParticleSystemStopped", "OnParticleTrigger",
        "OnRenderImage", "OnRenderObject",
        "OnPreCull", "OnPreRender", "OnPostRender",
        "OnTransformParentChanged", "OnTransformChildrenChanged",
        "OnJointBreak", "OnJointBreak2D",
        "OnControllerColliderHit",
        "OnServerInitialized", "OnConnectedToServer",
        "OnPlayerConnected", "OnPlayerDisconnected"
    };

    [MenuItem("Antigravity/Generate Analyzer Config", false, 200)]
    public static void GenerateConfig()
    {
        GenerateEditorConfig();
        GenerateGlobalSuppression();
        Debug.Log("[Antigravity] Analyzer configuration generated.");
    }

    /// <summary>
    /// Generates .editorconfig with Unity-appropriate diagnostic severities.
    /// </summary>
    public static void GenerateEditorConfig()
    {
        string projectDir = Directory.GetCurrentDirectory();
        string editorConfigPath = Path.Combine(projectDir, ".editorconfig");

        int level = EditorPrefs.GetInt(PrefKey_AnalyzerLevel, 1);

        var sb = new StringBuilder();
        sb.AppendLine("# Antigravity IDE — Unity Analyzer Configuration");
        sb.AppendLine("# Auto-generated. Safe to customize.");
        sb.AppendLine();
        sb.AppendLine("root = true");
        sb.AppendLine();
        sb.AppendLine("[*.cs]");
        sb.AppendLine();

        // Basic formatting
        sb.AppendLine("# Formatting");
        sb.AppendLine("indent_style = space");
        sb.AppendLine("indent_size = 4");
        sb.AppendLine("end_of_line = lf");
        sb.AppendLine("charset = utf-8");
        sb.AppendLine("trim_trailing_whitespace = true");
        sb.AppendLine("insert_final_newline = true");
        sb.AppendLine();

        // Unity-specific suppressions
        sb.AppendLine("# Unity-specific diagnostic suppressions");
        sb.AppendLine("# IDE0051: Remove unused private members — false positive for Unity messages");
        sb.AppendLine("dotnet_diagnostic.IDE0051.severity = none");
        sb.AppendLine();
        sb.AppendLine("# IDE0044: Add readonly modifier — false positive for serialized fields");
        sb.AppendLine("dotnet_diagnostic.IDE0044.severity = none");
        sb.AppendLine();
        sb.AppendLine("# CS0649: Field is never assigned — false positive for [SerializeField]");
        sb.AppendLine("dotnet_diagnostic.CS0649.severity = none");
        sb.AppendLine();

        if (level >= 2) // Recommended
        {
            sb.AppendLine("# Code quality");
            sb.AppendLine("dotnet_diagnostic.CA1822.severity = suggestion");
            sb.AppendLine("dotnet_diagnostic.CA2235.severity = warning");
            sb.AppendLine();
            sb.AppendLine("# Naming conventions");
            sb.AppendLine("dotnet_naming_rule.private_fields_should_be_camel_case.severity = suggestion");
            sb.AppendLine("dotnet_naming_rule.private_fields_should_be_camel_case.symbols = private_fields");
            sb.AppendLine("dotnet_naming_rule.private_fields_should_be_camel_case.style = camel_case_style");
            sb.AppendLine();
            sb.AppendLine("dotnet_naming_symbols.private_fields.applicable_kinds = field");
            sb.AppendLine("dotnet_naming_symbols.private_fields.applicable_accessibilities = private");
            sb.AppendLine();
            sb.AppendLine("dotnet_naming_style.camel_case_style.capitalization = camel_case");
            sb.AppendLine();
        }

        if (level >= 3) // All
        {
            sb.AppendLine("# Strict analysis");
            sb.AppendLine("dotnet_analyzer_diagnostic.severity = warning");
            sb.AppendLine();
        }

        // Shader/USS/UXML files
        sb.AppendLine("[*.shader]");
        sb.AppendLine("indent_style = tab");
        sb.AppendLine();
        sb.AppendLine("[*.uss]");
        sb.AppendLine("indent_style = space");
        sb.AppendLine("indent_size = 4");
        sb.AppendLine();
        sb.AppendLine("[*.uxml]");
        sb.AppendLine("indent_style = space");
        sb.AppendLine("indent_size = 4");

        File.WriteAllText(editorConfigPath, sb.ToString());
    }

    /// <summary>
    /// Generates a GlobalSuppressions.cs file that suppresses false-positive
    /// diagnostics for Unity API message methods.
    /// </summary>
    public static void GenerateGlobalSuppression()
    {
        string projectDir = Directory.GetCurrentDirectory();
        string suppressionPath = Path.Combine(projectDir, "Assets", "GlobalSuppressions.cs");

        // Don't overwrite existing file
        if (File.Exists(suppressionPath)) return;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Antigravity IDE — Unity analyzer suppressions");
        sb.AppendLine("// This file suppresses false-positive diagnostics for Unity API messages.");
        sb.AppendLine("// Safe to delete if not using Roslyn analyzers.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        sb.AppendLine("using System.Diagnostics.CodeAnalysis;");
        sb.AppendLine();
        sb.AppendLine("// Suppress warnings for Unity message methods that appear unused to static analysis");
        sb.AppendLine("[assembly: SuppressMessage(\"CodeQuality\", \"IDE0051:Remove unused private members\",");
        sb.AppendLine("    Justification = \"Unity message methods are called by the engine via reflection\")]");

        File.WriteAllText(suppressionPath, sb.ToString());
    }

    /// <summary>
    /// Gets the list of discovered Roslyn analyzer DLL paths in the project.
    /// </summary>
    public static List<string> GetDiscoveredAnalyzers()
    {
        var analyzers = new List<string>();
        string projectDir = Directory.GetCurrentDirectory();

        string[] searchDirs = new[]
        {
            Path.Combine(projectDir, "Library", "PackageCache"),
            Path.Combine(projectDir, "Packages")
        };

        foreach (var dir in searchDirs)
        {
            if (!Directory.Exists(dir)) continue;
            try
            {
                analyzers.AddRange(
                    Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories)
                        .Where(f => f.Replace("\\", "/").Contains("/analyzers/") ||
                                    f.Replace("\\", "/").Contains("/Analyzers/"))
                        .Where(f => !f.Replace("\\", "/").Contains("/test/") &&
                                    !f.Replace("\\", "/").Contains("/Test/"))
                );
            }
            catch (Exception)
            {
                // Silently handle permission errors
            }
        }

        return analyzers;
    }
}
