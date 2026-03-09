using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SR = System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Profiling;

public static class ProjectGeneration
{
    // ✅ LEARN: Full exclude list from com.unity.ide.vscode (30+ patterns)
    private const string SettingsJsonTemplate = @"{{
    ""dotnet.defaultSolution"": ""{0}"",
    ""dotrush.msbuildProperties"": {{
        ""DefineConstants"": ""UNITY_EDITOR""
    }},
    ""files.exclude"": {{
        ""**/.DS_Store"": true,
        ""**/.git"": true,
        ""**/.gitmodules"": true,
        ""**/*.booproj"": true,
        ""**/*.pidb"": true,
        ""**/*.suo"": true,
        ""**/*.user"": true,
        ""**/*.userprefs"": true,
        ""**/*.unityproj"": true,
        ""**/*.dll"": true,
        ""**/*.exe"": true,
        ""**/*.pdf"": true,
        ""**/*.mid"": true,
        ""**/*.midi"": true,
        ""**/*.wav"": true,
        ""**/*.gif"": true,
        ""**/*.ico"": true,
        ""**/*.jpg"": true,
        ""**/*.jpeg"": true,
        ""**/*.png"": true,
        ""**/*.psd"": true,
        ""**/*.tga"": true,
        ""**/*.tif"": true,
        ""**/*.tiff"": true,
        ""**/*.3ds"": true,
        ""**/*.3DS"": true,
        ""**/*.fbx"": true,
        ""**/*.FBX"": true,
        ""**/*.lxo"": true,
        ""**/*.LXO"": true,
        ""**/*.ma"": true,
        ""**/*.MA"": true,
        ""**/*.obj"": true,
        ""**/*.OBJ"": true,
        ""**/*.asset"": true,
        ""**/*.cubemap"": true,
        ""**/*.flare"": true,
        ""**/*.mat"": true,
        ""**/*.meta"": true,
        ""**/*.prefab"": true,
        ""**/*.unity"": true,
        ""build/"": true,
        ""Build/"": true,
        ""Library/"": true,
        ""library/"": true,
        ""obj/"": true,
        ""Obj/"": true,
        ""ProjectSettings/"": true,
        ""temp/"": true,
        ""Temp/"": true,
        ""Logs/"": true
    }}
}}";

    // ✅ LEARN: Updated launch.json to use DotRush "unity" type
    private const string LaunchJsonTemplate = @"{{
    ""version"": ""0.2.0"",
    ""configurations"": [
        {{
            ""name"": ""Attach to Unity Editor"",
            ""type"": ""unity"",
            ""request"": ""attach""
        }},
        {{
            ""name"": ""Attach to Unity Player"",
            ""type"": ""unity"",
            ""request"": ""attach"",
            ""transportArgs"": {{
                ""port"": {0}
            }}
        }}
    ]
}}";

    private static readonly string[] k_ReimportSyncExtensions = { ".dll", ".asmdef", ".asmref" };

    public static void Sync()
    {
        Profiler.BeginSample("AntigravityProjectSync");
        var assemblies = CompilationPipeline.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            GenerateCsproj(assembly);
        }
        GenerateSolution(assemblies);
        WriteVSCodeSettingsFiles();
        GenerateDirectoryBuildProps();

        // ✅ LEARN: AssetPostprocessor callback from com.unity.ide.vscode
        OnGeneratedCSProjectFiles();

        Profiler.EndSample();
        Debug.Log("[Antigravity] Project files synchronized.");
    }

    public static void SyncIfNeeded(string[] addedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, string[] importedAssets)
    {
        Profiler.BeginSample("AntigravityProjectSyncIfNeeded");

        var allChanged = addedAssets
            .Concat(deletedAssets)
            .Concat(movedAssets)
            .Concat(importedAssets);

        bool needsSync = allChanged.Any(path =>
            path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
            k_ReimportSyncExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));

        if (needsSync)
        {
            Sync();
        }

        Profiler.EndSample();
    }

    private static void GenerateCsproj(Assembly assembly)
    {
        string projectPath = Path.Combine(Directory.GetCurrentDirectory(), $"{assembly.name}.csproj");

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<Project ToolsVersion=\"4.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");

        // LangVersion first (as in com.unity.ide.vscode)
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine($"    <LangVersion>{GetLangVersion(assembly)}</LangVersion>");
        sb.AppendLine("  </PropertyGroup>");

        // Main PropertyGroup
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>");
        sb.AppendLine("    <Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>");
        sb.AppendLine("    <ProductVersion>10.0.20506</ProductVersion>");
        sb.AppendLine("    <SchemaVersion>2.0</SchemaVersion>");
        sb.AppendLine($"    <RootNamespace>{EditorSettings.projectGenerationRootNamespace}</RootNamespace>");
        sb.AppendLine($"    <ProjectGuid>{{{GenerateGuid(assembly.name)}}}</ProjectGuid>");
        sb.AppendLine("    <OutputType>Library</OutputType>");
        sb.AppendLine("    <AppDesignerFolder>Properties</AppDesignerFolder>");
        sb.AppendLine($"    <AssemblyName>{assembly.name}</AssemblyName>");
        sb.AppendLine("    <TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>");
        sb.AppendLine("    <FileAlignment>512</FileAlignment>");
        sb.AppendLine("    <BaseDirectory>.</BaseDirectory>");
        sb.AppendLine("  </PropertyGroup>");

        // Debug configuration
        sb.AppendLine("  <PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' \">");
        sb.AppendLine("    <DebugSymbols>true</DebugSymbols>");
        sb.AppendLine("    <DebugType>full</DebugType>");
        sb.AppendLine("    <Optimize>false</Optimize>");
        sb.AppendLine("    <OutputPath>Temp\\bin\\Debug\\</OutputPath>");

        string defines = GetDefineConstants(assembly);
        sb.AppendLine($"    <DefineConstants>{defines}</DefineConstants>");
        sb.AppendLine("    <ErrorReport>prompt</ErrorReport>");
        sb.AppendLine("    <WarningLevel>4</WarningLevel>");
        sb.AppendLine("    <NoWarn>0169</NoWarn>");

        if (assembly.compilerOptions.AllowUnsafeCode)
            sb.AppendLine("    <AllowUnsafeBlocks>True</AllowUnsafeBlocks>");

        sb.AppendLine("  </PropertyGroup>");

        // MSBuild flags (no implicit references — Unity controls this)
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <NoConfig>true</NoConfig>");
        sb.AppendLine("    <NoStdLib>true</NoStdLib>");
        sb.AppendLine("    <AddAdditionalExplicitAssemblyReferences>false</AddAdditionalExplicitAssemblyReferences>");
        sb.AppendLine("    <ImplicitlyExpandNETStandardFacades>false</ImplicitlyExpandNETStandardFacades>");
        sb.AppendLine("    <ImplicitlyExpandDesignTimeFacades>false</ImplicitlyExpandDesignTimeFacades>");
        sb.AppendLine("  </PropertyGroup>");

        // ✅ LEARN: Roslyn Analyzers ItemGroup
        var analyzerPaths = GetAnalyzerPaths(assembly);
        if (analyzerPaths.Count > 0)
        {
            sb.AppendLine("  <ItemGroup>");
            foreach (var analyzerPath in analyzerPaths)
            {
                sb.AppendLine($"    <Analyzer Include=\"{analyzerPath.Replace("\\", "/")}\" />");
            }
            sb.AppendLine("  </ItemGroup>");
        }

        // Assembly references
        sb.AppendLine("  <ItemGroup>");
        foreach (var reference in assembly.compiledAssemblyReferences)
        {
            sb.AppendLine($"    <Reference Include=\"{Path.GetFileNameWithoutExtension(reference)}\">");
            sb.AppendLine($"        <HintPath>{reference.Replace("\\", "/")}</HintPath>");
            sb.AppendLine("    </Reference>");
        }
        sb.AppendLine("  </ItemGroup>");

        // Source files
        sb.AppendLine("  <ItemGroup>");
        foreach (var sourceFile in assembly.sourceFiles)
        {
            sb.AppendLine($"    <Compile Include=\"{sourceFile.Replace("\\", "/")}\" />");
        }
        sb.AppendLine("  </ItemGroup>");

        // Project references
        if (assembly.assemblyReferences.Length > 0)
        {
            sb.AppendLine("  <ItemGroup>");
            foreach (var refAssembly in assembly.assemblyReferences)
            {
                sb.AppendLine($"    <ProjectReference Include=\"{refAssembly.name}.csproj\">");
                sb.AppendLine($"      <Project>{{{GenerateGuid(refAssembly.name)}}}</Project>");
                sb.AppendLine($"      <Name>{refAssembly.name}</Name>");
                sb.AppendLine("    </ProjectReference>");
            }
            sb.AppendLine("  </ItemGroup>");
        }

        sb.AppendLine("  <Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />");
        sb.AppendLine("</Project>");

        // ✅ LEARN: AssetPostprocessor chain from com.unity.ide.vscode
        string content = OnGeneratedCSProject(projectPath, sb.ToString());
        WriteFileIfChanged(projectPath, content);
    }

    private static void GenerateSolution(Assembly[] assemblies)
    {
        string solutionName = Path.GetFileName(Directory.GetCurrentDirectory());
        string solutionPath = Path.Combine(Directory.GetCurrentDirectory(), $"{solutionName}.sln");

        var sb = new StringBuilder();
        sb.AppendLine("\r\nMicrosoft Visual Studio Solution File, Format Version 11.00");
        sb.AppendLine("# Visual Studio 2010");

        foreach (var assembly in assemblies)
        {
            string guid = GenerateGuid(assembly.name);
            sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{assembly.name}\", \"{assembly.name}.csproj\", \"{{{guid}}}\"");
            sb.AppendLine("EndProject");
        }

        sb.AppendLine("Global");
        sb.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
        sb.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
        foreach (var assembly in assemblies)
        {
            string guid = GenerateGuid(assembly.name);
            sb.AppendLine($"\t\t{{{guid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            sb.AppendLine($"\t\t{{{guid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
        }
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
        sb.AppendLine("\t\tHideSolutionNode = FALSE");
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("EndGlobal");

        // ✅ LEARN: AssetPostprocessor chain
        string content = OnGeneratedSlnSolution(solutionPath, sb.ToString());
        WriteFileIfChanged(solutionPath, content);
    }

    // ✅ LEARN: WriteVSCodeSettingsFiles pattern from com.unity.ide.vscode
    private static void WriteVSCodeSettingsFiles()
    {
        string projectDir = Directory.GetCurrentDirectory();
        string vscodeDir = Path.Combine(projectDir, ".vscode");

        if (!Directory.Exists(vscodeDir))
            Directory.CreateDirectory(vscodeDir);

        // settings.json — always regenerate to keep up to date
        string settingsPath = Path.Combine(vscodeDir, "settings.json");
        string solutionName = Path.GetFileName(projectDir);
        string settingsContent = string.Format(SettingsJsonTemplate, $"{solutionName}.sln");
        WriteFileIfChanged(settingsPath, settingsContent);

        // launch.json — only create if not present (user may customize)
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

        if (File.Exists(propsPath)) return;

        var sb = new StringBuilder();
        sb.AppendLine("<Project>");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>");
        sb.AppendLine("    <!-- Suppress Unity-specific false positives -->");
        sb.AppendLine("    <!-- IDE0051: Remove unused private members (Unity messages like Start, Update) -->");
        sb.AppendLine("    <!-- IDE0044: Add readonly modifier (serialized fields) -->");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("</Project>");

        File.WriteAllText(propsPath, sb.ToString());
    }

    // ✅ LEARN: WriteFileIfChanged — only write if content changed (avoids hot-reload)
    private static void WriteFileIfChanged(string path, string newContents)
    {
        try
        {
            if (File.Exists(path) && newContents == File.ReadAllText(path))
                return;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        File.WriteAllText(path, newContents);
    }

    // ✅ LEARN: AssetPostprocessor callbacks from com.unity.ide.vscode
    private static IEnumerable<SR.MethodInfo> GetPostProcessorCallbacks(string name)
    {
        return TypeCache
            .GetTypesDerivedFrom<AssetPostprocessor>()
            .Select(t => t.GetMethod(name, SR.BindingFlags.Public | SR.BindingFlags.NonPublic | SR.BindingFlags.Static))
            .Where(m => m != null);
    }

    private static void OnGeneratedCSProjectFiles()
    {
        foreach (var method in GetPostProcessorCallbacks(nameof(OnGeneratedCSProjectFiles)))
        {
            method.Invoke(null, Array.Empty<object>());
        }
    }

    private static string InvokePostProcessorCallback(string name, string path, string content)
    {
        foreach (var method in GetPostProcessorCallbacks(name))
        {
            var args = new object[] { path, content };
            var returnValue = method.Invoke(null, args);
            if (method.ReturnType == typeof(string))
                content = (string)returnValue;
        }
        return content;
    }

    private static string OnGeneratedCSProject(string path, string content)
        => InvokePostProcessorCallback(nameof(OnGeneratedCSProject), path, content);

    private static string OnGeneratedSlnSolution(string path, string content)
        => InvokePostProcessorCallback(nameof(OnGeneratedSlnSolution), path, content);

    // ✅ LEARN: LangVersion now reads from assembly.compilerOptions when available
    private static string GetLangVersion(Assembly assembly)
    {
#if UNITY_2022_2_OR_NEWER
        if (!string.IsNullOrEmpty(assembly.compilerOptions.LanguageVersion))
            return assembly.compilerOptions.LanguageVersion;
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
        defines.AddRange(assembly.defines);

        // Add active build target scripting defines
        defines.AddRange(EditorUserBuildSettings.activeScriptCompilationDefines);

        return string.Join(";", new[] { "DEBUG", "TRACE" }
            .Concat(defines)
            .Distinct());
    }

    private static List<string> GetAnalyzerPaths(Assembly assembly)
    {
        var analyzers = new List<string>();

#if UNITY_2020_2_OR_NEWER
        // Use Roslyn analyzer DLL paths from assembly compiler options
        if (assembly.compilerOptions.RoslynAnalyzerDllPaths != null)
        {
            analyzers.AddRange(assembly.compilerOptions.RoslynAnalyzerDllPaths
                .Select(p => Path.IsPathRooted(p) ? p : Path.GetFullPath(p)));
        }
#endif

        // Also scan PackageCache for analyzers
        string projectDir = Directory.GetCurrentDirectory();
        string[] searchDirs = {
            Path.Combine(projectDir, "Library", "PackageCache"),
            Path.Combine(projectDir, "Packages")
        };

        foreach (var searchDir in searchDirs)
        {
            if (!Directory.Exists(searchDir)) continue;
            try
            {
                var dlls = Directory.GetFiles(searchDir, "*.dll", SearchOption.AllDirectories)
                    .Where(f => (f.Contains("analyzers") || f.Contains("Analyzers"))
                             && !f.Contains("test") && !f.Contains("Test"));
                analyzers.AddRange(dlls);
            }
            catch (Exception) { }
        }

        return analyzers.Distinct().ToList();
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
