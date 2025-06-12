namespace UMA
{
    public struct PartialLibraryUnloadingArgs
    {
        public string[] AssetsToKeep;
        public string[] BundlesToKeep;
        public bool KeepGlobalUmaAssets;
        public string[] GlobalUmaAssets;
    }
}