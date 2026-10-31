using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace SnatcherBugFix;

[BepInPlugin("Amor.SnatcherBugFix", "SnatcherBugFix", "0.5.1")]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
internal sealed class EntryPoint : BasePlugin
{
    public override void Load()
    {
        Logger.MLS = Log;
        ClassInjector.RegisterTypeInIl2Cpp<SnatcherHandler>();
        SnatcherNativePatches.Init();
        new Harmony("Amor.SnatcherBugFix").PatchAll();
        Logger.Message("SnatcherBugFix is done loading!");        
    }
}