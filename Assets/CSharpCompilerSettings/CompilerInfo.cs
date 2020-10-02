namespace Coffee.CSharpCompilerSettings
{
    internal readonly struct CompilerInfo
    {
        public readonly CompilerType Type;
        public readonly string Path;

        public CompilerInfo(CompilerType type, string path)
        {
            Type = type;
            Path = path;
        }
    }
}
