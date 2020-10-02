using UnityEditor;

namespace Coffee.CSharpCompilerSettings
{
    internal class CSharpCompilerSettingsProvider
    {
        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            var serializedObject = CscSettings.GetSerializedObject();
            var keywords = SettingsProvider.GetSearchKeywordsFromSerializedObject(serializedObject);
            return new SettingsProvider("Project/C# Compiler", SettingsScope.Project)
            {
                label = "C# Compiler",
                keywords = keywords,
                guiHandler = OnGUI,
            };
        }

        private static void OnGUI(string searchContext)
        {
            var serializedObject = CscSettings.GetSerializedObject();
            var spUseDefaultCompiler = serializedObject.FindProperty("m_UseDefaultCompiler");
            var spPackageName = serializedObject.FindProperty("m_PackageName");
            var spPackageVersion = serializedObject.FindProperty("m_PackageVersion");
            var spLanguageVersion = serializedObject.FindProperty("m_LanguageVersion");
            var spEnableDebugLog = serializedObject.FindProperty("m_EnableDebugLog");

            using (var ccs = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(spUseDefaultCompiler);
                if (ccs.changed)
                    Core.RequestScriptCompilation();
            }

            EditorGUILayout.PropertyField(spPackageName);
            EditorGUILayout.PropertyField(spPackageVersion);
            EditorGUILayout.PropertyField(spLanguageVersion);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(spEnableDebugLog);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
