using BepInEx.Unity.IL2CPP.Utils;
using Enemies;
using GTFO.API;
using HarmonyLib;
using Player;
using SnatcherBugFix.Utils;
using SNetwork;
using System.Collections;
using UnityEngine;

namespace SnatcherBugFix;

[HarmonyPatch]
internal static class SnatcherPatches // auri ur epic
{
    private static SnatcherHandler? _handler;

    static SnatcherPatches()
    {
        LevelAPI.OnEnterLevel += OnEnterLevel;
    }

    private static void OnEnterLevel()
    {
        var player = PlayerManager.GetLocalPlayerAgent();
        _handler = player.gameObject.AddOrGetComponent<SnatcherHandler>();
        if (!_handler.enabled) _handler = null;
    }

    [HarmonyPatch(typeof(EnemyAgent), nameof(EnemyAgent.Setup))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    private static void Pre_EnemySetup(EnemyAgent __instance)
    {
        if (!__instance.IsSetup) return;

        if (__instance.IsArenaDimensionEnemy)
        {
            __instance.AddOnDeadOnce(() => _handler?.OnSpitOut(__instance)); // force spit out captured player on death
        }
        if (SNet.IsMaster && __instance.Dimension.IsArenaDimension)
        {
            __instance.StartCoroutine(DespawnFromArena(__instance)); // host despawn enemies in arena dim
        }
    }

    private static IEnumerator DespawnFromArena(EnemyAgent enemy)
    {
        yield return new WaitForSeconds(2.0f);
        enemy.m_replicator.Despawn();
    }

    [HarmonyPatch(typeof(PouncerBehaviour), nameof(PouncerBehaviour.RequestConsume))]
    [HarmonyPatch(typeof(PouncerBehaviour), nameof(PouncerBehaviour.OnConsumeRequestReceived))]
    [HarmonyPostfix]
    private static void Post_Consume(PouncerBehaviour __instance)
    {
        _handler?.OnConsumed(__instance.GetComponentInParent<EnemyAgent>());
    }

    [HarmonyPatch(typeof(PouncerScreenFX), nameof(PouncerScreenFX.SetCovered), new Type[] { typeof(bool) })]
    [HarmonyPrefix]
    private static void Pre_CoverScreen()
    {
        FocusStateManager.ChangeState(eFocusState.FPS, true); // force exit menu pages
    }

    [HarmonyPatch(typeof(PouncerScreenFX), nameof(PouncerScreenFX.SetCovered), new Type[] { typeof(bool) })]
    [HarmonyPostfix]
    private static void Post_CoverScreen(bool value)
    {
        if (!value) return;
        _handler?.UncoverCallback?.Start();
    }
}
