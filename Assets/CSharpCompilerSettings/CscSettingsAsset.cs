using System;
using System.IO;
using System.Linq;
using System.Text;
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

        private static CscSettingsAsset CreateFromProjectSettings()
        {
            s_Instance = CreateInstance<CscSettingsAsset>();
            if (File.Exists(k_SettingsPath))
                JsonUtility.FromJsonOverwrite(File.ReadAllText(k_SettingsPath), s_Instance);
            return s_Instance;
        }

        private static CscSettingsAsset s_Instance;

        public static CscSettingsAsset instance => s_Instance ? s_Instance : s_Instance = CreateFromProjectSettings();

        public string PackageId => m_PackageName + "." + m_PackageVersion;

        public bool UseDefaultCompiler => m_UseDefaultCompiler;

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

        public bool EnableDebugLog => m_EnableDebugLog;

        public string AdditionalSymbols
        {
            get
            {
                var current = s_Instance.m_LanguageVersion;
                current = s_Instance.UseDefaultCompiler
                    ? 0
                    : current == LVersion.Preview
                        ? LVersion.CSharp9
                        : current == LVersion.Latest
                            ? LVersion.CSharp8
                            : current;

                var sb = new StringBuilder();
                if (LVersion.CSharp7 <= current) sb.Append("CSHARP_7_OR_NEWER;");
                if (LVersion.CSharp7_1 <= current) sb.Append("CSHARP_7_1_OR_NEWER;");
                if (LVersion.CSharp7_2 <= current) sb.Append("CSHARP_7_2_OR_NEWER;");
                if (LVersion.CSharp7_3 <= current) sb.Append("CSHARP_7_3_OR_NEWER;");
                if (LVersion.CSharp8 <= current) sb.Append("CSHARP_8_OR_NEWER;");
                if (LVersion.CSharp9 <= current) sb.Append("CSHARP_9_OR_NEWER;");
                if (LVersion.CSharp7 <= current) sb.Append("CSHARP_7_OR_NEWER;");
                return sb.ToString();
            }
        }

        public static CscSettingsAsset GetAtPath(string path)
        {
            try
            {
                return string.IsNullOrEmpty(path)
                    ? null
                    : CreateFromJson(AssetImporter.GetAtPath(path).userData);
            }
            catch
            {
                return null;
            }
        }

        public static CscSettingsAsset CreateFromJson(string json = "")
        {
            var setting = CreateInstance<CscSettingsAsset>();
            JsonUtility.FromJsonOverwrite(json, setting);
            return setting;
        }
    }
}
