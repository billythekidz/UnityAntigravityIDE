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

    // Non-script asset extensions to include as <None Include> for IDE navigation
    private static readonly string[] k_NonScriptAssetExtensions =
    {
        ".uxml", ".uss", ".shader", ".cginc", ".hlsl", ".compute",
        ".asmdef", ".asmref", ".json", ".xml", ".yaml", ".txt",
        ".md", ".inputactions"
    };

    public static void Sync()
    {
        Profiler.BeginSample("AntigravityProjectSync");
        // Get ALL assemblies: Player (includes tests) + Editor
        var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
        var editorAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
        var allAssemblies = playerAssemblies
            .Concat(editorAssemblies)
            .GroupBy(a => a.name)
            .Select(g => g.First())
            .ToArray();

        // PERF: Only generate .csproj for user-editable assemblies
        // Package assemblies (Library/PackageCache) are resolved via HintPath references.
        // This drops load from ~155 projects to ~10-15, dramatically speeding up Roslyn.
        var userAssemblies = FilterUserAssemblies(allAssemblies);

        Debug.Log($"[Antigravity] Generating {userAssemblies.Length} project files (filtered from {allAssemblies.Length} total assemblies)");

        // Clean up orphaned .csproj files
        CleanOrphanedProjectFiles(userAssemblies);

        foreach (var assembly in userAssemblies)
        {
            GenerateCsproj(assembly);
        }
        GenerateSolution(userAssemblies);
        WriteVSCodeSettingsFiles();
        GenerateDirectoryBuildProps();

        OnGeneratedCSProjectFiles();

        Profiler.EndSample();
        Debug.Log("[Antigravity] Project files synchronized.");
    }

    /// <summary>
    /// Filters assemblies to only include user-editable ones.
    /// An assembly is user-editable if ANY of its source files are under:
    /// - Assets/ (user scripts)
    /// - Packages/ local packages (not in Library/PackageCache)
    /// Package assemblies from Library/PackageCache are excluded — their types
    /// are already resolved via compiledAssemblyReferences with HintPath.
    /// </summary>
    private static Assembly[] FilterUserAssemblies(Assembly[] allAssemblies)
    {
        string projectDir = Directory.GetCurrentDirectory().Replace("\\", "/");
        string packageCachePath = (projectDir + "/Library/PackageCache").ToLowerInvariant();

        var result = new List<Assembly>();
        foreach (var assembly in allAssemblies)
        {
            if (assembly.sourceFiles.Length == 0) continue;

            // Check if any source file is outside Library/PackageCache
            bool isUserEditable = assembly.sourceFiles.Any(f =>
            {
                string fullPath = Path.GetFullPath(f).Replace("\\", "/").ToLowerInvariant();
                return !fullPath.StartsWith(packageCachePath);
            });

            if (isUserEditable)
            {
                result.Add(assembly);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Removes .csproj files that no longer correspond to any active user assembly.
    /// Prevents stale project files from confusing IDEs after asmdef renames/deletes.
    /// </summary>
    private static void CleanOrphanedProjectFiles(Assembly[] activeAssemblies)
    {
        string projectDir = Directory.GetCurrentDirectory();
        var activeNames = new HashSet<string>(activeAssemblies.Select(a => a.name), StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var csproj in Directory.GetFiles(projectDir, "*.csproj"))
            {
                string name = Path.GetFileNameWithoutExtension(csproj);
                if (!activeNames.Contains(name))
                {
                    File.Delete(csproj);
                    Debug.Log($"[Antigravity] Removed orphaned project file: {name}.csproj");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Antigravity] Failed to clean orphaned files: {ex.Message}");
        }
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

        // Non-script assets: .uxml, .uss, .shader, .asmdef, etc.
        // These let IDEs and DotRush navigate/index non-C# Unity files
        AppendNonScriptAssets(sb, assembly);

        // Response file extra references/defines
        AppendResponseFileReferences(sb, assembly);

        // Project references + DLL HintPath for DotRush compatibility
        // DotRush (Roslyn-based) needs <Reference> with <HintPath> to resolve types.
        // Pure <ProjectReference> alone is not enough — Roslyn can't build Unity .csproj.
        // We emit BOTH: Reference (for IDE type resolution) + ProjectReference (for navigation).
        if (assembly.assemblyReferences.Length > 0)
        {
            string projectDir = Directory.GetCurrentDirectory();
            string scriptAssembliesDir = Path.Combine(projectDir, "Library", "ScriptAssemblies");

            // First: add Reference+HintPath for assemblies that have compiled DLLs
            var refsWithDll = new List<(string name, string dllPath)>();
            foreach (var refAssembly in assembly.assemblyReferences)
            {
                string dllPath = Path.Combine(scriptAssembliesDir, $"{refAssembly.name}.dll");
                if (File.Exists(dllPath))
                {
                    refsWithDll.Add((refAssembly.name, dllPath));
                }
            }

            if (refsWithDll.Count > 0)
            {
                sb.AppendLine("  <ItemGroup>");
                foreach (var (name, dllPath) in refsWithDll)
                {
                    sb.AppendLine($"    <Reference Include=\"{name}\">");
                    sb.AppendLine($"        <HintPath>{dllPath.Replace("\\", "/")}</HintPath>");
                    sb.AppendLine("        <Private>false</Private>");
                    sb.AppendLine("    </Reference>");
                }
                sb.AppendLine("  </ItemGroup>");
            }

            // Second: keep ProjectReference for IDE navigation (Go to Definition across projects)
            sb.AppendLine("  <ItemGroup>");
            foreach (var refAssembly in assembly.assemblyReferences)
            {
                sb.AppendLine($"    <ProjectReference Include=\"{refAssembly.name}.csproj\">");
                sb.AppendLine($"      <Project>{{{GenerateGuid(refAssembly.name)}}}</Project>");
                sb.AppendLine($"      <Name>{refAssembly.name}</Name>");
                sb.AppendLine($"      <ReferenceOutputAssembly>false</ReferenceOutputAssembly>");
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

        // Merge any extra defines from response files
        var rspData = ParseResponseFiles(assembly);
        defines.AddRange(rspData.Defines);

        return string.Join(";", new[] { "DEBUG", "TRACE" }
            .Concat(defines)
            .Distinct());
    }

    // -- Response File Parsing ------------------------------------------

    private struct ResponseFileData
    {
        public List<string> Defines;
        public List<string> References;
        public bool Unsafe;
    }

    /// <summary>
    /// Parses .rsp response files listed in assembly.compilerOptions.ResponseFiles.
    /// Extracts -define:, -r:, and -unsafe flags exactly like the official
    /// com.unity.ide.vscode ProjectGeneration does.
    /// </summary>
    private static ResponseFileData ParseResponseFiles(Assembly assembly)
    {
        var result = new ResponseFileData
        {
            Defines = new List<string>(),
            References = new List<string>(),
            Unsafe = false
        };

        string projectDir = Directory.GetCurrentDirectory();

        // Unity stores response file names in compilerOptions.ResponseFiles
        // Falls back to scanning Assets/ for csc.rsp / mcs.rsp
        var rspFiles = new List<string>();

        if (assembly.compilerOptions.ResponseFiles != null)
        {
            foreach (var rsp in assembly.compilerOptions.ResponseFiles)
            {
                string fullPath = Path.IsPathRooted(rsp)
                    ? rsp
                    : Path.GetFullPath(Path.Combine(projectDir, rsp));
                if (File.Exists(fullPath))
                    rspFiles.Add(fullPath);
            }
        }

        // Legacy: always check Assets/csc.rsp and Assets/mcs.rsp
        foreach (var legacyName in new[] { "csc.rsp", "mcs.rsp" })
        {
            string legacyPath = Path.Combine(projectDir, "Assets", legacyName);
            if (File.Exists(legacyPath) && !rspFiles.Contains(legacyPath))
                rspFiles.Add(legacyPath);
        }

        foreach (var rspFile in rspFiles)
        {
            try
            {
                foreach (var rawLine in File.ReadAllLines(rspFile))
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                        continue;

                    // -define:SYMBOL1;SYMBOL2 or -d:SYMBOL
                    if (line.StartsWith("-define:") || line.StartsWith("-d:"))
                    {
                        string value = line.Substring(line.IndexOf(':') + 1);
                        result.Defines.AddRange(value.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                    // -r:path/to/assembly.dll or -reference:...
                    else if (line.StartsWith("-r:") || line.StartsWith("-reference:"))
                    {
                        string value = line.Substring(line.IndexOf(':') + 1).Trim('"');
                        result.References.Add(value);
                    }
                    // -unsafe
                    else if (line == "-unsafe" || line == "/unsafe")
                    {
                        result.Unsafe = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Antigravity] Failed to parse response file {rspFile}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Appends extra DLL references found in response files that are not
    /// already in assembly.compiledAssemblyReferences.
    /// </summary>
    private static void AppendResponseFileReferences(StringBuilder sb, Assembly assembly)
    {
        var rspData = ParseResponseFiles(assembly);
        if (rspData.References.Count == 0) return;

        var existingRefs = new HashSet<string>(
            assembly.compiledAssemblyReferences.Select(Path.GetFileNameWithoutExtension),
            StringComparer.OrdinalIgnoreCase);

        var extraRefs = rspData.References
            .Where(r => !existingRefs.Contains(Path.GetFileNameWithoutExtension(r)))
            .ToList();

        if (extraRefs.Count == 0) return;

        sb.AppendLine("  <ItemGroup>");
        foreach (var refPath in extraRefs)
        {
            sb.AppendLine($"    <Reference Include=\"{Path.GetFileNameWithoutExtension(refPath)}\">");
            sb.AppendLine($"        <HintPath>{refPath.Replace("\\", "/")}</HintPath>");
            sb.AppendLine("    </Reference>");
        }
        sb.AppendLine("  </ItemGroup>");
    }

    // -- Non-script Asset Inclusion -------------------------------------

    /// <summary>
    /// Adds non-script Unity assets (shaders, uxml, uss, asmdef, etc.) as
    /// &lt;None Include&gt; items so IDEs can navigate them in the project tree
    /// and DotRush can syntax-check them.
    /// Only adds assets that belong to the same output folder as the assembly.
    /// </summary>
    private static void AppendNonScriptAssets(StringBuilder sb, Assembly assembly)
    {
        // Determine the root folders this assembly covers
        // (inferred from where its source files live)
        var assemblyRoots = assembly.sourceFiles
            .Select(f => GetTopLevelFolder(f))
            .Where(f => !string.IsNullOrEmpty(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (assemblyRoots.Count == 0) return;

        var nonScriptItems = new List<string>();

        try
        {
            // Use AssetDatabase to enumerate all project assets
            foreach (var assetPath in AssetDatabase.GetAllAssetPaths())
            {
                string ext = Path.GetExtension(assetPath);
                if (string.IsNullOrEmpty(ext)) continue;
                if (!k_NonScriptAssetExtensions.Any(e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase))) continue;

                // Only include assets under one of this assembly's root folders
                string topFolder = GetTopLevelFolder(assetPath);
                if (!assemblyRoots.Contains(topFolder)) continue;

                string fullPath = Path.GetFullPath(assetPath).Replace("\\", "/");
                nonScriptItems.Add(fullPath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Antigravity] Failed to enumerate non-script assets: {ex.Message}");
        }

        if (nonScriptItems.Count == 0) return;

        sb.AppendLine("  <ItemGroup>");
        foreach (var item in nonScriptItems)
        {
            sb.AppendLine($"    <None Include=\"{item}\" />");
        }
        sb.AppendLine("  </ItemGroup>");
    }

    /// <summary>Returns the top-level folder segment of an asset path (e.g. "Assets" or "Packages").</summary>
    private static string GetTopLevelFolder(string path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        // path is like "Assets/MyFolder/..." or "Packages/com.x/..."
        int slash = path.IndexOfAny(new[] { '/', '\\' });
        return slash >= 0 ? path.Substring(0, slash) : path;
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
