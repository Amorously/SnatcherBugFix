using Enemies;
using GTFO.API;
using HarmonyLib;
using Player;
using SnatcherBugFix.Utils;

namespace SnatcherBugFix;

[HarmonyPatch]
internal static class SnatcherPatches // auri ur epic
{
    private static PlayerAgent? _player;
    private static SnatcherHandler? _handler;

    static SnatcherPatches()
    {
        LevelAPI.OnEnterLevel += OnEnterLevel;
    }

    private static void OnEnterLevel()
    {
        _player = PlayerManager.GetLocalPlayerAgent();
        _handler = _player.gameObject.AddOrGetComponent<SnatcherHandler>();
        _handler.enabled = true;
    }

    [HarmonyPatch(typeof(EnemyAgent), nameof(EnemyAgent.Setup))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_EnemySpawned(EnemyAgent __instance)
    {
        if (__instance.IsSetup && __instance.IsArenaDimensionEnemy)
        {
            __instance.AddOnDeadOnce(() => _handler?.OnSpitOut(__instance));
        }
    }

    [HarmonyPatch(typeof(PouncerBehaviour), nameof(PouncerBehaviour.OnConsumeRequestReceived))]
    [HarmonyPatch(typeof(PouncerBehaviour), nameof(PouncerBehaviour.RequestConsume))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_Consume(PouncerBehaviour __instance)
    {
        _handler?.OnConsumed(__instance.GetComponentInParent<EnemyAgent>());
    }

    [HarmonyPatch(typeof(PouncerScreenFX), nameof(PouncerScreenFX.SetCovered), new Type[] { typeof(bool) })]
    [HarmonyPrefix]
    private static void Pre_CoverScreen()
    {
        FocusStateManager.ChangeState(eFocusState.FPS, true); // force exit menus
    }

    [HarmonyPatch(typeof(PouncerScreenFX), nameof(PouncerScreenFX.SetCovered), new Type[] { typeof(bool) })]
    [HarmonyPostfix]
    private static void Post_CoverScreen(bool value)
    {
        if (value && _handler != null)
        {
            _handler?.UncoverCallback.Start();
        }
    }
}
