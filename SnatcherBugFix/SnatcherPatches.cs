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
    private static SnatcherHandler? _handler = null;
    private static bool _prepared = false; // idfk why but harmony wouldn't load the cctor so whatever ig

    [HarmonyPrepare]
    private static void Prepare()
    {
        if (_prepared) return;
        LevelAPI.OnEnterLevel += OnEnterLevel;
        _prepared = true;
    }

    private static void OnEnterLevel()
    {
        var player = PlayerManager.GetLocalPlayerAgent();
        _handler = player.gameObject.AddOrGetComponent<SnatcherHandler>();
        if (!_handler.enabled) _handler = null;
    }

    [HarmonyPatch(typeof(SurvivalWave), nameof(SurvivalWave.OnSpawn))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_Wave_OnSpawn(SurvivalWave __instance)
    {
        if (__instance.m_courseNode?.m_dimension.IsArenaDimension == true)
            __instance.m_courseNode = _handler?.GoodNode ?? __instance.m_courseNode;
    }

    [HarmonyPatch(typeof(EnemySync), nameof(EnemySync.OnSpawn))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.High)]
    [HarmonyWrapSafe]
    private static void Post_Enemy_OnSpawn(EnemySync __instance)
    {
        var enemy = __instance.m_agent;
        if (!enemy.IsSetup)
            return;
        if (enemy.IsArenaDimensionEnemy)
            enemy.AddOnDeadOnce(() => _handler?.OnSpitOut(enemy)); // force spit out captured player on death
        if (SNet.IsMaster && enemy.Dimension.IsArenaDimension)
            enemy.StartCoroutine(DespawnFromArena(enemy)); // host despawn enemies in arena dim
    }

    private static IEnumerator DespawnFromArena(EnemyAgent enemy)
    {
        yield return new WaitForSeconds(0.75f);
        enemy.m_replicator.Despawn();
    }

    [HarmonyPatch(typeof(PouncerBehaviour), nameof(PouncerBehaviour.RequestConsume))]
    [HarmonyPatch(typeof(PouncerBehaviour), nameof(PouncerBehaviour.OnConsumeRequestReceived))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_Consume(PouncerBehaviour __instance)
    {
        if (__instance.CapturedPlayer?.IsLocallyOwned == true)
            _handler?.OnConsumed(__instance.GetComponentInParent<EnemyAgent>());
    }

    [HarmonyPatch(typeof(PouncerScreenFX), nameof(PouncerScreenFX.SetCovered), new Type[] { typeof(bool) })]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    private static void Pre_CoverScreen()
    {
        FocusStateManager.ChangeState(eFocusState.FPS, true); // force exit menu pages
    }

    [HarmonyPatch(typeof(PouncerScreenFX), nameof(PouncerScreenFX.SetCovered), new Type[] { typeof(bool) })]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_CoverScreen(bool value)
    {
        if (!value) return;
        _handler?.UncoverCallback?.Start();
    }
}
