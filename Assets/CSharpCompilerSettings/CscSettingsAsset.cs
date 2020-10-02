using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LVersion = Coffee.CSharpCompilerSettings.CSharpLanguageVersion;

namespace Coffee.CSharpCompilerSettings
{
    internal class CscSettingsAsset : ScriptableObject
    {
        public const string k_SettingsPath = "ProjectSettings/CSharpCompilerSettings.asset";

        [SerializeField] private bool m_UseDefaultCompiler = true;
        [SerializeField] private string m_PackageName = "Microsoft.Net.Compilers";
        [SerializeField] private string m_PackageVersion = "3.5.0";
        [SerializeField] private CSharpLanguageVersion m_LanguageVersion = CSharpLanguageVersion.Latest;
        [SerializeField] private bool m_EnableDebugLog = false;

        internal static SerializedObject GetSerializedObject()
        {
            return new SerializedObject(instance);
        }

        private static CscSettingsAsset Create()
        {
            s_Instance = CreateInstance<CscSettingsAsset>();
            if (File.Exists(k_SettingsPath))
                JsonUtility.FromJsonOverwrite(File.ReadAllText(k_SettingsPath), s_Instance);
            s_Instance.OnValidate();
            return s_Instance;
        }

        private static CscSettingsAsset s_Instance;

        public static CscSettingsAsset instance
        {
            get { return s_Instance ? s_Instance : s_Instance = Create(); }
        }

        public string PackageId
        {
            get { return m_PackageName + "." + m_PackageVersion; }
        }

        public bool UseDefaultCompiler
        {
            get { return m_UseDefaultCompiler; }
        }

        public string LanguageVersion
        {
            get
            {
                switch (m_LanguageVersion)
                {
                    case CSharpLanguageVersion.CSharp7: return "7";
                    case CSharpLanguageVersion.CSharp7_1: return "7.1";
                    case CSharpLanguageVersion.CSharp7_2: return "7.2";
                    case CSharpLanguageVersion.CSharp7_3: return "7.3";
                    case CSharpLanguageVersion.CSharp8: return "8";
                    case CSharpLanguageVersion.CSharp9: return "9";
                    case CSharpLanguageVersion.Preview: return "preview";
                    default: return "latest";
                }
            }
        }

        public bool EnableDebugLog
        {
            get { return m_EnableDebugLog; }
        }

        private void OnValidate()
        {
            if (s_Instance != this) return;
            Core.LogDebug("OnValidate => " + JsonUtility.ToJson(this));
            File.WriteAllText(k_SettingsPath, JsonUtility.ToJson(this, true));

            var current = s_Instance.m_LanguageVersion;
            current = s_Instance.UseDefaultCompiler
                ? 0
                : current == LVersion.Preview
                    ? LVersion.CSharp9
                    : current == LVersion.Latest
                        ? LVersion.CSharp8
                        : current;

            foreach (var group in (BuildTargetGroup[]) Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if ((int) group <= 0 || typeof(BuildTargetGroup).GetMember(group.ToString())[0].GetCustomAttributes(typeof(ObsoleteAttribute), false).Length != 0) continue;

                try
                {
                    var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';', ',');
                    var oldSymbols = symbols;
                    LanguageVersionCheck(ref symbols, current, LVersion.CSharp7, "CSHARP_7_OR_LATER");
                    LanguageVersionCheck(ref symbols, current, LVersion.CSharp7_1, "CSHARP_7_1_OR_LATER");
                    LanguageVersionCheck(ref symbols, current, LVersion.CSharp7_2, "CSHARP_7_2_OR_LATER");
                    LanguageVersionCheck(ref symbols, current, LVersion.CSharp7_3, "CSHARP_7_3_OR_LATER");
                    LanguageVersionCheck(ref symbols, current, LVersion.CSharp8, "CSHARP_8_OR_LATER");
                    LanguageVersionCheck(ref symbols, current, LVersion.CSharp9, "CSHARP_9_OR_LATER");
                    var newSymbols = symbols;

                    if (oldSymbols.SequenceEqual(newSymbols)) continue;

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", newSymbols));
                }
                catch
                {
                }
            }
        }

        private static void LanguageVersionCheck(ref string[] symbols, LVersion current, LVersion version, string symbol)
        {
            symbols = version <= current
                ? symbols.Union(new[] {symbol}).ToArray()
                : symbols.Except(new[] {symbol}).ToArray();
        }
    }
}
