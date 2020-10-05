using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEditor.Compilation;
using System;
using System.Security.Cryptography;
using System.Linq;
using Object = UnityEngine.Object;

namespace Coffee.CSharpCompilerSettings
{
    [InitializeOnLoad]
    internal static class InspectorGUI
    {
        static string s_AssemblyNameToPublish;
        private static string s_AsmdefPathToPublish;
        static GUIContent s_IgnoreAccessCheckText;
        static GUIContent s_EnableText;
        static GUIContent s_ModifySymbolsText;
        static GUIContent s_SettingsText;
        static GUIContent s_PublishText;
        static GUIContent s_ReloadText;
        static GUIContent s_HelpText;
        static bool s_OpenSettings = false;
        static Dictionary<string, bool> s_EnableAsmdefs = new Dictionary<string, bool>();

        static Dictionary<string, string> s_AssemblyNames = new Dictionary<string, string>();
        // private static CscSettingsAsset s_Instance;

        static void OnAssemblyCompilationFinished(string name, CompilerMessage[] messages)
        {
            try
            {
                // This assembly is requested to publish?
                var assemblyName = Path.GetFileNameWithoutExtension(name);
                if (s_AssemblyNameToPublish != assemblyName)
                    return;

                s_AssemblyNameToPublish = null;
                Core.LogInfo("Assembly compilation finished: <b>{0} is requested to publish.</b>", assemblyName);

                // No compilation error?
                if (messages.Any(x => x.type == CompilerMessageType.Error))
                    return;

                // Publish a dll to parent directory.
                var dst = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(s_AssemblyNameToPublish)), assemblyName + ".dll");
                var src = "Library/ScriptAssemblies/" + Path.GetFileName(dst);
                Core.LogInfo("<b>Publish assembly as dll:</b> " + dst);
                CopyFileIfUpdated(Path.GetFullPath(src), Path.GetFullPath(dst));

                EditorApplication.delayCall += () => AssetDatabase.ImportAsset(dst);
            }
            catch (Exception e)
            {
                Core.LogException(e);
            }
        }

        public static void CopyFileIfUpdated(string src, string dst)
        {
            src = Path.GetFullPath(src);
            if (!File.Exists(src))
                return;

            dst = Path.GetFullPath(dst);
            if (File.Exists(dst))
            {
                using (var srcFs = new FileStream(src, FileMode.Open))
                using (var dstFs = new FileStream(dst, FileMode.Open))
                using (var md5 = new MD5CryptoServiceProvider())
                {
                    if (md5.ComputeHash(srcFs).SequenceEqual(md5.ComputeHash(dstFs)))
                        return;
                }
            }

            var dir = Path.GetDirectoryName(dst);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.Copy(src, dst, true);
        }

        static InspectorGUI()
        {
            s_IgnoreAccessCheckText = new GUIContent("Ignore Access Checks", "Ignore accessibility checks on compiling to allow access to internals and privates in other assemblies.");
            s_ModifySymbolsText = new GUIContent("Modify Symbols",
                "When compiling this assembly, add or remove specific symbols separated with semicolons (;) or commas (,).\nSymbols starting with '!' will be removed.\n\ne.g. 'SYMBOL_TO_ADD;!SYMBOL_TO_REMOVE;...'");
            s_EnableText = new GUIContent("Enable Asmdef Extension", "Enable asmdef extension for this assembly.");
            s_SettingsText = new GUIContent("Asmdef Extension", "Show extension settings for this assembly definition file.");
            s_PublishText = new GUIContent("Publish as dll", "Publish this assembly as dll to the parent directory.");
            s_ReloadText = new GUIContent("Reload AsmdefEx.cs", "Reload AsmdefEx.cs for this assembly.");
            s_HelpText = new GUIContent("Help", "Open AsmdefEx help page on browser.");

            Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
            s_OpenSettings = EditorPrefs.GetBool("Coffee.AsmdefEx.InspectorGUI_OpenSettings", false);
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        static SerializedObject _serializedObject;
        static Object _targetObject;
        private static bool _hasPortableDll = false;
        private static bool _changed = false;
        private static string _assetPath;

        private static void OnPostHeaderGUI(Editor editor)
        {
            var importer = editor.target as AssemblyDefinitionImporter;
            if (!importer || 1 < editor.targets.Length)
                return;

            if (_assetPath == null || _assetPath != importer.assetPath)
            {
                _assetPath = importer.assetPath;
                _serializedObject = new SerializedObject(CscSettingsAsset.CreateFromJson(importer.userData));
                _hasPortableDll = Core.HasPortableDll(importer.assetPath);
                _changed = false;
                EditorGUIUtility.hotControl = -1;

                Core.LogDebug("Targets Changed!" + importer.userData);
            }

            GUILayout.Space(-EditorGUIUtility.singleLineHeight);


            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
            {
                GUILayout.Space(30);

                // Open settings.
                using (var ccs = new EditorGUI.ChangeCheckScope())
                {
                    s_OpenSettings = GUILayout.Toggle(s_OpenSettings, s_SettingsText, EditorStyles.miniButtonLeft, GUILayout.ExpandWidth(false));
                    if (ccs.changed)
                        EditorPrefs.SetBool("Coffee.AsmdefEx.InspectorGUI_OpenSettings", s_OpenSettings);
                }

                // Open help.
                if (GUILayout.Button(s_HelpText, EditorStyles.miniButtonRight, GUILayout.ExpandWidth(false)))
                {
                    Application.OpenURL("https://github.com/mob-sakai/OpenSesameCompilerForUnity");
                }
            }

            if (!s_OpenSettings) return;

            _serializedObject.Update();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();

            // Enable.
            _hasPortableDll = EditorGUILayout.ToggleLeft(s_EnableText, _hasPortableDll);

            EditorGUILayout.PropertyField(_serializedObject.FindProperty("m_UseDefaultCompiler"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("m_PackageName"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("m_PackageVersion"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("m_LanguageVersion"));

            _changed |= EditorGUI.EndChangeCheck();

            var needToSaveSettings = false;
            if (_changed)
            {
                if (GUILayout.Button("Revert"))
                {
                    _assetPath = null;
                }

                if (GUILayout.Button("Apply"))
                {
                    needToSaveSettings = true;
                    _assetPath = null;
                }

                if (GUILayout.Button("Reload"))
                {
                    EnablePortableDll(importer.assetPath, true);
                    needToSaveSettings = true;
                    _assetPath = null;
                }

                if (GUILayout.Button(s_PublishText))
                {
                    _assetPath = null;
                    s_AsmdefPathToPublish = importer.assetPath;
                    s_AssemblyNameToPublish = Core.GetAssemblyName(importer.assetPath);
                    Core.LogInfo("<b><color=#22aa22>Request to publish dll:</color> {0}</b>", s_AssemblyNameToPublish);

                    importer.SaveAndReimport();
                }
            }

            GUILayout.EndVertical();

            _serializedObject.ApplyModifiedProperties();

            if (needToSaveSettings)
            {
                if (_hasPortableDll != Core.HasPortableDll(importer.assetPath))
                {
                    EnablePortableDll(importer.assetPath, _hasPortableDll);
                }

                importer.userData = _hasPortableDll
                    ? JsonUtility.ToJson(_serializedObject.targetObject)
                    : null;
                Debug.Log(importer.userData);

                importer.SaveAndReimport();
                AssetDatabase.Refresh();
            }
        }

        private static void EnablePortableDll(string asmdefPath, bool enabled)
        {
            if (enabled)
            {
                var src = "Packages/com.coffee.csharp-compiler-settings/Plugins/CSharpCompilerSettings.dll";
                var guid = Directory.GetFiles(Path.GetDirectoryName(asmdefPath))
                    .Select(x => Regex.Match(x, "CSharpCompilerSettings_([0-9a-zA-Z]{32}).dll").Groups[1].Value)
                    .FirstOrDefault(x => !string.IsNullOrEmpty(x));

                Debug.Log("EnablePortableDll exist ? " + guid);
                if (string.IsNullOrEmpty(guid))
                    guid = Guid.NewGuid().ToString().Replace("-", "");
                var tmpDst = "Temp/" + Path.GetFileName(Path.GetTempFileName());
                var dst = Path.GetDirectoryName(asmdefPath) + "/CSharpCompilerSettings_" + guid + ".dll";

                // Copy dll with renaming assembly name.
                File.Copy(src, tmpDst, true);
                AssemblyRenamer.Rename(tmpDst, "CSharpCompilerSettings_" + guid);
                CopyFileIfNeeded(tmpDst, dst);

                // Copy meta.
                File.Copy(src + ".meta~", tmpDst + ".meta", true);
                var meta = File.ReadAllText(tmpDst + ".meta");
                meta = Regex.Replace(meta, "${GUID}", guid);
                File.WriteAllText(tmpDst + ".meta", meta);
                CopyFileIfNeeded(tmpDst + ".meta", dst + ".meta");

                // Request to compile.
                EditorApplication.delayCall += AssetDatabase.Refresh;
            }
            else
            {
                var dir = Path.GetDirectoryName(asmdefPath);
                foreach (var path in Directory.GetFiles(dir, "*.dll")
                    .Where(x => Regex.IsMatch(x, "CSharpCompilerSettings_[0-9a-zA-Z]{32}.dll"))
                )
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        static void CopyFileIfNeeded(string src, string dst)
        {
            if (File.Exists(dst))
            {
                using (var md5 = MD5.Create())
                using (var srcStream = File.OpenRead(src))
                using (var dstStream = File.OpenRead(dst))
                    if (md5.ComputeHash(srcStream).SequenceEqual(md5.ComputeHash(dstStream)))
                        return;
            }

            File.Copy(src, dst, true);
        }
    }
}
