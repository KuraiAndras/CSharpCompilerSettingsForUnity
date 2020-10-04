using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Coffee.CSharpCompilerSettings
{
    internal class CscSettingsProvider
    {
        private static SerializedObject serializedObject;

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            serializedObject = new SerializedObject(CscSettingsAsset.instance);
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
            if (serializedObject == null)
                serializedObject = new SerializedObject(CscSettingsAsset.instance);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_UseDefaultCompiler"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PackageName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PackageVersion"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_LanguageVersion"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_EnableDebugLog"));

            if (GUILayout.Button("Revert"))
            {
                serializedObject = new SerializedObject(CscSettingsAsset.instance);
            }
            if (GUILayout.Button("Apply"))
            {
                serializedObject.ApplyModifiedProperties();
                File.WriteAllText(CscSettingsAsset.k_SettingsPath, JsonUtility.ToJson(serializedObject.targetObject, true));
                RequestScriptCompilation();
            }
        }

        public static void RequestScriptCompilation()
        {
            Type.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface, UnityEditor")
                .Call("DirtyAllScripts");
        }
    }
}
