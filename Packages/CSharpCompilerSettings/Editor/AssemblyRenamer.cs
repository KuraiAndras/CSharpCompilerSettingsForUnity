using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace Coffee.CSharpCompilerSettings
{
    internal class AssemblyRenamer
    {
        public static void Rename(string dll, string assemblyName)
        {
            const string exe = "Packages/CSharpCompilerSettings/.exe/ChangeAssemblyName.exe";
            const string cecilDll = "Packages/CSharpCompilerSettings/.exe/Unity.Cecil.dll";
            var contentsPath = EditorApplication.applicationContentsPath;
            var sep = Path.DirectorySeparatorChar;

            // Create compilation process.
            var psi = new ProcessStartInfo
            {
                Arguments = string.Format("\"{0}\" {1}", dll, assemblyName),
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            if (!File.Exists(cecilDll))
            {
                File.Copy((contentsPath + "/Managed/Unity.Cecil.dll").Replace('/', sep), cecilDll.Replace('/', sep));
            }

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                psi.FileName = Path.GetFullPath(exe);
            }
            else
            {
                psi.FileName = Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/bin/mono");
                psi.Arguments = exe + " " + psi.Arguments;
            }

            // Start compilation process.
            Debug.LogFormat("Rename: Change assembly name\n  command={0} {1}\n", psi.FileName, psi.Arguments);
            var p = Process.Start(psi);
            p.Exited += (_, __) =>
            {
                if (p.ExitCode == 0)
                    Debug.Log("Rename: success.\n" + p.StandardOutput.ReadToEnd());
                else
                    Debug.LogError("Rename: failure.\n" + p.StandardError.ReadToEnd());
            };
            p.EnableRaisingEvents = true;

            p.WaitForExit();
        }
    }
}
