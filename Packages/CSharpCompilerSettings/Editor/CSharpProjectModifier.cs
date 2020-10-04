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

            var defines = Regex.Match(content, "<DefineConstants>(.*)</DefineConstants>").Groups[1].Value.Split(';', ',');
            defines = Core.ModifyDefineSymbols(defines, setting.AdditionalSymbols);
            var defineText = string.Join(";", defines);
            content = Regex.Replace(content, "<DefineConstants>(.*)</DefineConstants>", string.Format("<DefineConstants>{0}</DefineConstants>", defineText), RegexOptions.Multiline);

            // Language version.
            content = Regex.Replace(content, "<LangVersion>.*</LangVersion>", "<LangVersion>" + setting.LanguageVersion + "</LangVersion>", RegexOptions.Multiline);

            return content;
        }
    }
}
