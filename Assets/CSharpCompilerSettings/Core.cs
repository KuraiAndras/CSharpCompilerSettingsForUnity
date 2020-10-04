using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEditor.Compilation;
using Assembly = System.Reflection.Assembly;
using Debug = UnityEngine.Debug;

namespace Coffee.CSharpCompilerSettings
{
    [InitializeOnLoad]
    internal static class Core
    {
        private static string k_LogHeader = "<b><color=#aa2222>[CscSettings]</color></b> ";
        static Dictionary<string, bool> s_EnableAsmdefs = new Dictionary<string, bool>();
        static Dictionary<string, string> s_AssemblyNames = new Dictionary<string, string>();
        private static bool IsGlobal => typeof(Core).Assembly.GetName().Name == "CSharpCompilerSettings";
        private static bool EnableDebugLog => CscSettingsAsset.instance.EnableDebugLog;

        public static void LogDebug(string format, params object[] args)
        {
            if (CscSettingsAsset.instance.EnableDebugLog)
                LogInfo(format, args);
        }

        public static void LogInfo(string format, params object[] args)
        {
            if (args == null || args.Length == 0)
                UnityEngine.Debug.Log(k_LogHeader + format);
            else
                UnityEngine.Debug.LogFormat(k_LogHeader + format, args);
        }

        public static void LogExeption(Exception e)
        {
            UnityEngine.Debug.LogException(new Exception(k_LogHeader + e.Message, e.InnerException));
        }

        public static void RequestScriptCompilation()
        {
            Type.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface, UnityEditor")
                .Call("DirtyAllScripts");
        }

        private static string GetAssemblyName(string asmdefPath)
        {
            if (string.IsNullOrEmpty(asmdefPath)) return null;

            string assemblyName;
            if (s_AssemblyNames.TryGetValue(asmdefPath, out assemblyName)) return assemblyName;

            var m = Regex.Match(File.ReadAllText(asmdefPath), "\"name\":\\s*\"([^\"]*)\"");
            assemblyName = m.Success ? m.Groups[1].Value : "";
            s_AssemblyNames[asmdefPath] = assemblyName;

            return assemblyName;
        }

        public static bool HasPortableDll(string asmdefPath)
        {
            if (string.IsNullOrEmpty(asmdefPath)) return false;
            bool enabled;
            if (s_EnableAsmdefs.TryGetValue(asmdefPath, out enabled)) return enabled;

            enabled = Directory.GetFiles(Path.GetDirectoryName(asmdefPath))
                .Any(x => Regex.IsMatch(x, "CSharpCompilerSettings_[0-9a-zA-Z]{32}.dll"));

            s_EnableAsmdefs[asmdefPath] = enabled;

            return enabled;
        }

        public static bool IsInSameDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            var dir = Path.GetFullPath(Path.GetDirectoryName(path));
            var coreAssemblyLocationDir = Path.GetFullPath(Path.GetDirectoryName(typeof(Core).Assembly.Location));
            return dir == coreAssemblyLocationDir;
        }

        static string FindAsmdef()
        {
            return Directory.GetFiles(Path.GetDirectoryName(typeof(Core).Assembly.Location), "*.asmdef")
                .FirstOrDefault(x => x.EndsWith(".asmdef"));
        }

        static CscSettingsAsset GetSettings()
        {
            return IsGlobal
                ? CscSettingsAsset.instance
                : (CscSettingsAsset.GetAtPath(FindAsmdef()) ?? ScriptableObject.CreateInstance<CscSettingsAsset>());
        }

        public static string[] ModifyDefineSymbols(IEnumerable<string> defines, string modifySymbols)
        {
            var symbols = modifySymbols.Split(';', ',');
            var add = symbols.Where(x => 0 < x.Length && !x.StartsWith("!"));
            var remove = symbols.Where(x => 1 < x.Length && x.StartsWith("!")).Select(x => x.Substring(1));
            return defines
                .Union(add)
                .Except(remove)
                .Distinct()
                .ToArray();
        }

        private static void ChangeCompilerProcess(object compiler, object scriptAssembly, CscSettingsAsset setting)
        {
            var tProgram = Type.GetType("UnityEditor.Utils.Program, UnityEditor");
            var tScriptCompilerBase = Type.GetType("UnityEditor.Scripting.Compilers.ScriptCompilerBase, UnityEditor");
            var fiProcess = tScriptCompilerBase.GetField("process", BindingFlags.NonPublic | BindingFlags.Instance);
            var psi = compiler.Get("process", fiProcess).Call("GetProcessStartInfo") as ProcessStartInfo;
            LogDebug("current process: {0}", (psi.FileName + " " + psi.Arguments));

            var command = (psi.FileName + " " + psi.Arguments).Replace('\\', '/')
                .Replace(EditorApplication.applicationContentsPath.Replace('\\', '/'), "@APP_CONTENTS@");
            var isDefaultCsc = Regex.IsMatch(command, "@APP_CONTENTS@/[^ ]*(mcs|csc)");

            // csc is not Unity default. It is already modified.
            if (!isDefaultCsc)
            {
                LogDebug("  <color=#bbbb44><Skipped> current csc is not Unity default. It is already modified.</color>");
                return;
            }

            var compilerInfo = CustomCompiler.GetInstalledPath(setting.PackageId);

            // csc is not installed.
            if (!compilerInfo.HasValue)
            {
                LogDebug("  <color=#bbbb44><Skipped> custom csc is not installed.</color>");
                return;
            }

            // Kill current process.
            compiler.Call("Dispose");

            var responseFile = Regex.Replace(psi.Arguments, "^.*@(.+)$", "$1");
            var text = File.ReadAllText(responseFile);
            text = Regex.Replace(text, "[\r\n]+", "\n");
            text = Regex.Replace(text, "^-", "/");
            text = Regex.Replace(text, "\n/langversion:[^\n]+\n", "\n/langversion:" + setting.LanguageVersion + "\n");
            text = Regex.Replace(text, "\n/debug\n", "\n/debug:portable\n");
            text += "\n/preferreduilang:en-US";


            // Modify scripting define symbols.
            LogDebug("Modify scripting define symbols: {0}", responseFile);
            var defines = Regex.Matches(text, "^/define:(.*)$", RegexOptions.Multiline)
                .Cast<Match>()
                .Select(x => x.Groups[1].Value);

            text = Regex.Replace(text, "[\r\n]+/define:[^\r\n]+", "");
            foreach (var d in ModifyDefineSymbols(defines, setting.AdditionalSymbols))
                text += "\n/define:" + d;

            // Change exe file path.
            LogDebug("Change csc to {0}", compilerInfo);
            if (compilerInfo.Value.Type == CompilerType.NetFramework)
            {
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    psi.FileName = Path.GetFullPath(compilerInfo.Value.Path);
                    psi.Arguments = "/shared /noconfig @" + responseFile;
                }
                else
                {
                    psi.FileName = Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/bin/mono");
                    psi.Arguments = compilerInfo.Value.Path + " /noconfig @" + responseFile;
                }
            }
            else
            {
                psi.FileName = Path.GetFullPath(DotnetRuntime.GetInstalledPath());
                psi.Arguments = compilerInfo.Value.Path + " /noconfig @" + responseFile;
            }

            text = Regex.Replace(text, "\n", Environment.NewLine);
            File.WriteAllText(responseFile, text);

            LogDebug("Restart compiler process: {0} {1}", psi.FileName, psi.Arguments);
            var program = tProgram.New(psi);
            program.Call("Start");
            compiler.Set("process", program, fiProcess);
        }

        private static void OnAssemblyCompilationStarted(string name)
        {
            try
            {
                var assemblyName = Path.GetFileNameWithoutExtension(name);
                LogDebug("Assembly compilation started: {0}", assemblyName);
                if (assemblyName == typeof(Core).Assembly.GetName().Name)
                {
                    LogDebug("  <color=#bbbb44><Skipped> Assembly '{0}' requires default csc.</color>", assemblyName);
                    return;
                }

                var tEditorCompilationInterface = Type.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface, UnityEditor");
                var compilerTasks = tEditorCompilationInterface.Get("Instance").Get("compilationTask").Get("compilerTasks") as IDictionary;
                var scriptAssembly = compilerTasks.Keys.Cast<object>().FirstOrDefault(x => (x.Get("Filename") as string) == assemblyName + ".dll");
                if (scriptAssembly == null)
                    return;

                var asmdefPath = scriptAssembly.Get("OriginPath") as string;
                if (IsGlobal && HasPortableDll(asmdefPath))
                {
                    LogDebug("  <color=#bbbb44><Skipped> Local CSharpCompilerSettings assembly is found.</color>");
                    return;
                }
                if (!IsGlobal && IsInSameDirectory(asmdefPath))
                {
                    LogDebug("  <color=#bbbb44><Skipped> '{0}' is not target assembly.</color>", assemblyName);
                    return;
                }

                var settings = GetSettings();;
                if (!settings || settings.UseDefaultCompiler)
                    return;

                // Create new compiler to recompile.
                LogDebug("Assembly compilation started: <b>{0} should be recompiled.</b>\n{1}", assemblyName, JsonUtility.ToJson(settings));
                ChangeCompilerProcess(compilerTasks[scriptAssembly], scriptAssembly, settings);
            }
            catch (Exception e)
            {
                LogExeption(e);
            }
        }

        static Core()
        {
            if (!IsGlobal)
            {
                var targetAssemblyName = GetAssemblyName(FindAsmdef());
                if (string.IsNullOrEmpty(targetAssemblyName))
                {
                    LogExeption(new Exception(string.Format("Target assembly is not found. {0}", typeof(Core).Assembly.Location.Replace(Environment.CurrentDirectory, "."))));
                }
                k_LogHeader = string.Format("<b><color=#aa2222>[CscSettings ({0})]</color></b> ", targetAssemblyName);
            }

            if (CscSettingsAsset.instance.EnableDebugLog)
            {
                var sb = new StringBuilder("<b>InitializeOnLoad</b>. Loaded assemblies:\n");
                foreach (var asm in Type.GetType("UnityEditor.EditorAssemblies, UnityEditor").Get("loadedAssemblies") as Assembly[])
                {
                    var name = asm.GetName().Name;
                    var path = asm.Location;
                    if (path.Contains(Path.GetDirectoryName(EditorApplication.applicationPath)))
                        sb.AppendFormat("  > {0}:\t{1}\n", name, "APP_PATH/.../" + Path.GetFileName(path));
                    else
                        sb.AppendFormat("  > <color=#22aa22><b>{0}</b></color>:\t{1}\n", name, path.Replace(Environment.CurrentDirectory, "."));
                }

                LogDebug(sb.ToString());
            }

            var assembly = typeof(Core).Assembly;
            var assemblyName = assembly.GetName().Name;
            var location = assembly.Location.Replace(Environment.CurrentDirectory, ".");
            LogInfo("Start watching assembly compilation: assembly = {0} [Global: {2}] ({1})", assemblyName, location, assembly.GetName().Name == "CSharpCompilerSettings");
            CompilationPipeline.assemblyCompilationStarted += OnAssemblyCompilationStarted;

            var settings = GetSettings();
            if (!settings || settings.UseDefaultCompiler) return;

            // csc is not installed.
            var compilerInfo = CustomCompiler.GetInstalledPath(settings.PackageId);
            if (!compilerInfo.HasValue)
            {
                LogExeption(new Exception(string.Format("Custom csc is not installed. {0}", settings.PackageId)));
            }
        }
    }
}
