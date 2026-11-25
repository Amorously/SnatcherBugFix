using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using SnatcherBugFix.Dependencies;

namespace SnatcherBugFix;

[BepInPlugin("Amor.SnatcherBugFix", "SnatcherBugFix", "0.6.1")]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(EAFDep.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
internal sealed class EntryPoint : BasePlugin
{
    public override void Load()
    {
        Logger.MLS = Log;
        ClassInjector.RegisterTypeInIl2Cpp<SnatcherHandler>();
        if (!EAFDep.Installed)
            SnatcherNativePatches.Init();
        new Harmony("Amor.SnatcherBugFix").PatchAll();
        Logger.Message("SnatcherBugFix is done loading!");        
    }
}