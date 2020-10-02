using System.Text.RegularExpressions;
using UnityEditor;

namespace Coffee.CSharpCompilerSettings
{
    internal class CSharpProjectModifier : AssetPostprocessor
    {
        private static string OnGeneratedCSProject(string path, string content)
        {
            var setting = CscSettingsAsset.instance;
            if (setting.UseDefaultCompiler) return content;

            // Language version.
            content = Regex.Replace(content, "<LangVersion>.*</LangVersion>", "<LangVersion>" + setting.LanguageVersion + "</LangVersion>", RegexOptions.Multiline);

            return content;
        }
    }
}
