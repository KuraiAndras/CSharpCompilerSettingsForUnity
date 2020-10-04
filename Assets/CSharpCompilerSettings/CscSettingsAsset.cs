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

        public string PackageName
        {
            get { return m_PackageName; }
            set { m_PackageName = value; }
        }


        public string PackageVersion
        {
            get { return m_PackageVersion; }
            set { m_PackageVersion = value; }
        }

        public CSharpLanguageVersion CSharpLanguageVersion
        {
            get { return m_LanguageVersion; }
            set { m_LanguageVersion = value; }
        }

        public string PackageId
        {
            get { return m_PackageName + "." + m_PackageVersion; }
        }

        public bool UseDefaultCompiler
        {
            get { return m_UseDefaultCompiler; }
            set { m_UseDefaultCompiler = value; }
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

                StringBuilder sb = new StringBuilder();
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

        private static void LanguageVersionCheck(ref string[] symbols, LVersion current, LVersion version, string symbol)
        {
            symbols = version <= current
                ? symbols.Union(new[] {symbol}).ToArray()
                : symbols.Except(new[] {symbol}).ToArray();
        }

        public static CscSettingsAsset GetAtPath(string path)
        {
            try
            {
                return CreateFromJson(AssetImporter.GetAtPath(path).userData);
            }
            catch
            {
                return null;
            }
        }

        public static CscSettingsAsset CreateFromJson(string json = "")
        {
            var setting = CreateInstance<CscSettingsAsset>();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(k_SettingsPath), setting);
            return setting;
        }
    }
}
