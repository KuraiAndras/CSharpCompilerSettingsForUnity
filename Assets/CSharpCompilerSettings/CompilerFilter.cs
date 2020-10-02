namespace Coffee.CSharpCompilerSettings
{
    internal readonly struct CompilerFilter
    {
        public readonly CompilerType Type;
        public readonly string RelatedPath;

        public CompilerFilter(CompilerType type, string relatedPath)
        {
            Type = type;
            RelatedPath = relatedPath;
        }
    }
}
