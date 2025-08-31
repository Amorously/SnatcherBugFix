using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System.Runtime.CompilerServices;

namespace SnatcherBugFix;

[BepInPlugin("Amor.SnatcherBugFix", "SnatcherBugFix", "0.4.1")]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
internal sealed class EntryPoint : BasePlugin
{
    public override void Load()
    {
        ClassInjector.RegisterTypeInIl2Cpp<SnatcherHandler>();
        new Harmony("Amor.SnatcherBugFix").PatchAll();
        RuntimeHelpers.RunClassConstructor(typeof(SnatcherPatches).TypeHandle); // idfk why but harmony doesn't run the ctor 
        Logger.Info("SnatcherBugFix is done loading!");        
    }
}