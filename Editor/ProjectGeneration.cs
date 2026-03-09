using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

public static class ProjectGeneration
{
    private const string SettingsJsonTemplate = @"{
    ""omnisharp.useModernNet"": true,
    ""omnisharp.enableRoslynAnalyzers"": true,
    ""dotnet.defaultSolution"": ""{0}"",
    ""files.exclude"": {{
        ""**/.git"": true,
        ""**/.DS_Store"": true,
        ""**/*.meta"": true,
        ""**/Library"": true,
        ""**/Temp"": true,
        ""**/obj"": true,
        ""**/Logs"": true
    }}
}}";

    private const string LaunchJsonTemplate = @"{{
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
            ""endPoint"": ""127.0.0.1:{0}""
        }}
    ]
}}";

    public static void Sync()
    {
        var assemblies = CompilationPipeline.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            GenerateCsproj(assembly);
        }
        GenerateSolution(assemblies);
        GenerateEditorConfigs();
        GenerateDirectoryBuildProps();
        Debug.Log("[Antigravity] Project files synchronized.");
    }

    public static void SyncIfNeeded(string[] addedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, string[] importedAssets)
    {
        // Only regenerate if C# scripts or asmdef files changed
        var allChanged = addedAssets
            .Concat(deletedAssets)
            .Concat(movedAssets)
            .Concat(importedAssets);

        bool needsSync = allChanged.Any(path =>
            path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".asmref", StringComparison.OrdinalIgnoreCase));

        if (needsSync)
        {
            Sync();
        }
    }

    private static void GenerateCsproj(Assembly assembly)
    {
        string projectPath = Path.Combine(Directory.GetCurrentDirectory(), $"{assembly.name}.csproj");

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<Project ToolsVersion=\"4.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");

        // Property Group
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>");
        sb.AppendLine("    <Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>");
        sb.AppendLine("    <ProductVersion>10.0.20506</ProductVersion>");
        sb.AppendLine("    <SchemaVersion>2.0</SchemaVersion>");
        sb.AppendLine($"    <ProjectGuid>{{{GenerateGuid(assembly.name)}}}</ProjectGuid>");
        sb.AppendLine("    <OutputType>Library</OutputType>");
        sb.AppendLine($"    <AssemblyName>{assembly.name}</AssemblyName>");
        sb.AppendLine("    <TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>");
        sb.AppendLine("    <FileAlignment>512</FileAlignment>");
        sb.AppendLine("    <BaseDirectory>.</BaseDirectory>");

        // Language version — Unity 2021+ uses C# 9, Unity 2022+ uses C# 10
        string langVersion = GetLangVersion();
        sb.AppendLine($"    <LangVersion>{langVersion}</LangVersion>");

        // Suppress specific warnings for Unity
        sb.AppendLine("    <NoWarn>0169;0649</NoWarn>");

        // Unity define constants
        string defines = GetDefineConstants(assembly);
        if (!string.IsNullOrEmpty(defines))
        {
            sb.AppendLine($"    <DefineConstants>{defines}</DefineConstants>");
        }

        // Allow unsafe code if any assembly reference uses it
        if (assembly.compilerOptions.AllowUnsafeCode)
        {
            sb.AppendLine("    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>");
        }

        sb.AppendLine("  </PropertyGroup>");

        // Assembly references
        sb.AppendLine("  <ItemGroup>");
        foreach (var reference in assembly.compiledAssemblyReferences)
        {
            sb.AppendLine($"    <Reference Include=\"{Path.GetFileNameWithoutExtension(reference)}\">");
            sb.AppendLine($"      <HintPath>{reference}</HintPath>");
            sb.AppendLine("    </Reference>");
        }
        sb.AppendLine("  </ItemGroup>");

        // Source files
        sb.AppendLine("  <ItemGroup>");
        foreach (var sourceFile in assembly.sourceFiles)
        {
            sb.AppendLine($"    <Compile Include=\"{sourceFile}\" />");
        }
        sb.AppendLine("  </ItemGroup>");

        // Project references
        sb.AppendLine("  <ItemGroup>");
        foreach (var refAssembly in assembly.assemblyReferences)
        {
            sb.AppendLine($"    <ProjectReference Include=\"{refAssembly.name}.csproj\">");
            sb.AppendLine($"      <Project>{{{GenerateGuid(refAssembly.name)}}}</Project>");
            sb.AppendLine($"      <Name>{refAssembly.name}</Name>");
            sb.AppendLine("    </ProjectReference>");
        }
        sb.AppendLine("  </ItemGroup>");

        // Roslyn Analyzers
        var analyzerPaths = GetAnalyzerPaths();
        if (analyzerPaths.Count > 0)
        {
            sb.AppendLine("  <ItemGroup>");
            foreach (var analyzerPath in analyzerPaths)
            {
                sb.AppendLine($"    <Analyzer Include=\"{analyzerPath}\" />");
            }
            sb.AppendLine("  </ItemGroup>");
        }

        sb.AppendLine("  <Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />");
        sb.AppendLine("</Project>");

        File.WriteAllText(projectPath, sb.ToString());
    }

    private static void GenerateSolution(Assembly[] assemblies)
    {
        string solutionName = Path.GetFileName(Directory.GetCurrentDirectory());
        string solutionPath = Path.Combine(Directory.GetCurrentDirectory(), $"{solutionName}.sln");
        var sb = new StringBuilder();
        sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        sb.AppendLine("# Visual Studio 15");
        sb.AppendLine("VisualStudioVersion = 15.0.26228.4");
        sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

        foreach (var assembly in assemblies)
        {
            string guid = GenerateGuid(assembly.name);
            sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{assembly.name}\", \"{assembly.name}.csproj\", \"{{{guid}}}\"");
            sb.AppendLine("EndProject");
        }

        // Global section
        sb.AppendLine("Global");
        sb.AppendLine("  GlobalSection(SolutionConfigurationPlatforms) = preSolution");
        sb.AppendLine("    Debug|Any CPU = Debug|Any CPU");
        sb.AppendLine("    Release|Any CPU = Release|Any CPU");
        sb.AppendLine("  EndGlobalSection");

        sb.AppendLine("  GlobalSection(ProjectConfigurationPlatforms) = postSolution");
        foreach (var assembly in assemblies)
        {
            string guid = GenerateGuid(assembly.name);
            sb.AppendLine($"    {{{guid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            sb.AppendLine($"    {{{guid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
            sb.AppendLine($"    {{{guid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
            sb.AppendLine($"    {{{guid}}}.Release|Any CPU.Build.0 = Release|Any CPU");
        }
        sb.AppendLine("  EndGlobalSection");
        sb.AppendLine("EndGlobal");

        File.WriteAllText(solutionPath, sb.ToString());
    }

    private static void GenerateEditorConfigs()
    {
        string projectDir = Directory.GetCurrentDirectory();
        string vscodeDir = Path.Combine(projectDir, ".vscode");

        if (!Directory.Exists(vscodeDir))
        {
            Directory.CreateDirectory(vscodeDir);
        }

        // settings.json
        string settingsPath = Path.Combine(vscodeDir, "settings.json");
        if (!File.Exists(settingsPath))
        {
            string solutionName = Path.GetFileName(projectDir);
            string settingsContent = string.Format(SettingsJsonTemplate, $"{solutionName}.sln");
            File.WriteAllText(settingsPath, settingsContent);
        }

        // launch.json
        string launchPath = Path.Combine(vscodeDir, "launch.json");
        if (!File.Exists(launchPath))
        {
            int debugPort = EditorPrefs.GetInt("Antigravity_DebugPort", 56000);
            string launchContent = string.Format(LaunchJsonTemplate, debugPort);
            File.WriteAllText(launchPath, launchContent);
        }
    }

    private static void GenerateDirectoryBuildProps()
    {
        string projectDir = Directory.GetCurrentDirectory();
        string propsPath = Path.Combine(projectDir, "Directory.Build.props");

        // Only generate if not already present (user may customize)
        if (File.Exists(propsPath)) return;

        var sb = new StringBuilder();
        sb.AppendLine("<Project>");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>");
        sb.AppendLine("    <!-- Suppress Unity-specific false positives -->");
        sb.AppendLine("    <!-- IDE0051: Remove unused private members (Unity messages like Start, Update) -->");
        sb.AppendLine("    <!-- IDE0044: Add readonly modifier (serialized fields) -->");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("  <ItemGroup>");

        var analyzerPaths = GetAnalyzerPaths();
        foreach (var path in analyzerPaths)
        {
            sb.AppendLine($"    <Analyzer Include=\"{path}\" />");
        }

        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine("</Project>");

        File.WriteAllText(propsPath, sb.ToString());
    }

    private static string GetLangVersion()
    {
#if UNITY_2022_1_OR_NEWER
        return "10.0";
#elif UNITY_2021_2_OR_NEWER
        return "9.0";
#else
        return "8.0";
#endif
    }

    private static string GetDefineConstants(Assembly assembly)
    {
        var defines = new List<string>();

        // Add Unity scripting defines
        defines.AddRange(assembly.defines);

        // Add standard Unity platform defines
        defines.Add("UNITY_5_3_OR_NEWER");

#if UNITY_EDITOR
        defines.Add("UNITY_EDITOR");
#endif
#if UNITY_EDITOR_WIN
        defines.Add("UNITY_EDITOR_WIN");
#elif UNITY_EDITOR_OSX
        defines.Add("UNITY_EDITOR_OSX");
#elif UNITY_EDITOR_LINUX
        defines.Add("UNITY_EDITOR_LINUX");
#endif

        return string.Join(";", defines.Distinct());
    }

    private static List<string> GetAnalyzerPaths()
    {
        var analyzers = new List<string>();
        string projectDir = Directory.GetCurrentDirectory();

        // Look for analyzers in Packages
        string packagesDir = Path.Combine(projectDir, "Packages");
        if (Directory.Exists(packagesDir))
        {
            // Check for Unity's built-in analyzers
            string[] analyzerSearchDirs = new[]
            {
                Path.Combine(projectDir, "Library", "PackageCache"),
                packagesDir
            };

            foreach (var searchDir in analyzerSearchDirs)
            {
                if (!Directory.Exists(searchDir)) continue;

                try
                {
                    var dlls = Directory.GetFiles(searchDir, "*.dll", SearchOption.AllDirectories)
                        .Where(f => f.Contains("analyzers") || f.Contains("Analyzers"))
                        .Where(f => !f.Contains("test") && !f.Contains("Test"));

                    analyzers.AddRange(dlls);
                }
                catch (Exception)
                {
                    // Silently handle permission errors on package cache
                }
            }
        }

        return analyzers;
    }

    private static string GenerateGuid(string input)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(input));
            return new Guid(hash).ToString().ToUpper();
        }
    }
}
