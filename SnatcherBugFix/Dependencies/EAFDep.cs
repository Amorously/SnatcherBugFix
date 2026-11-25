using BepInEx.Unity.IL2CPP;

namespace SnatcherBugFix.Dependencies
{
    internal static class EAFDep
    {
        public const string PLUGIN_GUID = "Dinorush.EnemyAnimationFix";
        public static readonly bool Installed;

        static EAFDep()
        {
            Installed = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }
    }
}
