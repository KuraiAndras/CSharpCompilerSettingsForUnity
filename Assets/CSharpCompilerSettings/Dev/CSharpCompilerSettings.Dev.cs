using System.Linq;
using UnityEditor;

namespace CSharpCompilierSettings
{
    internal static class Dev
    {
        private const string k_DebugModeText = "Csc Settings/Debug Mode";
        private const string k_DebugModeSymbol = "CSC_SETTINGS_DEBUG";

        private const string k_DevelopModeText = "Csc Settings/Develop Mode";
        private const string k_DevelopModeSymbol = "CSC_SETTINGS_DEVELOP";

        [MenuItem(k_DebugModeText, false)]
        private static void DebugMode()
        {
            SwitchSymbol(k_DebugModeSymbol);
        }

        [MenuItem(k_DebugModeText, true)]
        private static bool DebugMode_Valid()
        {
            Menu.SetChecked(k_DebugModeText, HasSymbol(k_DebugModeSymbol));
            return true;
        }

        [MenuItem(k_DevelopModeText, false)]
        private static void DevelopMode()
        {
            SwitchSymbol(k_DevelopModeSymbol);
        }

        [MenuItem(k_DevelopModeText, true)]
        private static bool DevelopMode_Valid()
        {
            Menu.SetChecked(k_DevelopModeText, HasSymbol(k_DevelopModeSymbol));
            return true;
        }

        private static string[] GetSymbols()
        {
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';', ',');
        }

        private static void SetSymbols(string[] symbols)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", symbols));
        }

        private static bool HasSymbol(string symbol)
        {
            return GetSymbols().Any(x => x == symbol);
        }

        private static void SwitchSymbol(string symbol)
        {
            var symbols = GetSymbols();
            SetSymbols(symbols.Any(x => x == symbol)
                ? symbols.Where(x => x != symbol).ToArray()
                : symbols.Concat(new[] {symbol}).ToArray()
            );
        }
    }
}
