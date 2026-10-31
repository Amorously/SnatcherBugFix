using Enemies;
using GTFO.API;
using HarmonyLib;
using LevelGeneration;
using Player;
using SnatcherBugFix.Utils;
using SNetwork;
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

    private static void OnEnterLevel() // add LODGroups to hide dimension gameobjects
    {
        bool flag = false;
        foreach (var dim in Builder.CurrentFloor.m_dimensions)
        {
            if (!dim.IsArenaDimension)
                continue;

            flag |= true;
            for (int i = 0; i < dim.DimensionLevel.transform.childCount; i++)
            {
                Transform child = dim.DimensionLevel.transform.GetChild(i);
                if (!child.name.Contains("Cube", StringComparison.OrdinalIgnoreCase))
                    continue;

                var lodGroup = child.gameObject.AddOrGetComponent<LODGroup>();
                lodGroup.fadeMode = LODFadeMode.None;
                var mr = child.GetComponent<MeshRenderer>();
                if (mr == null) continue;
                lodGroup.SetLODs(new[]
                {
                    new LOD
                    {
                        screenRelativeTransitionHeight = 0.70f,
                        fadeTransitionWidth = 0f,
                        renderers = new[] { mr }
                    }
                });
                lodGroup.RecalculateBounds();
            }
        }
        _handler = flag ? PlayerManager.GetLocalPlayerAgent().gameObject.GetComponent<SnatcherHandler>() : null;
    }

    [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.Setup))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_PlayerAgent_Setup(PlayerAgent __instance)
    {
        if (__instance.IsLocallyOwned)
            __instance.gameObject.AddOrGetComponent<SnatcherHandler>();
    }

    [HarmonyPatch(typeof(SurvivalWave), nameof(SurvivalWave.OnSpawn))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_SurvivalWave_OnSpawn(SurvivalWave __instance)
    {
        if (__instance.m_courseNode?.m_dimension.IsArenaDimension == true)
            __instance.m_courseNode = _handler?.GoodNode ?? __instance.m_courseNode;
    }

    [HarmonyPatch(typeof(EnemySync), nameof(EnemySync.OnSpawn))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.High)]
    [HarmonyWrapSafe]
    private static void Post_EnemySync_OnSpawn(EnemySync __instance)
    {
        var enemy = __instance.m_agent;
        if (!enemy.IsSetup)
            return;

        if (enemy.IsArenaDimensionEnemy)
            enemy.AddOnDeadOnce(() => _handler?.OnDead(enemy)); // force spit out captured player on death
    }

    [HarmonyPatch(typeof(PouncerBehaviour), nameof(PouncerBehaviour.OnConsumeRequestReceived))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_Consume(PouncerBehaviour __instance, pEB_PouncerTargetInfoPacket data)
    {
        if (!PlayerManager.TryGetPlayerAgent(ref data.PlayerSlot, out var agent)) 
            return;

        if (agent.IsLocallyOwned)
            _handler?.OnConsumed(__instance.m_ai.m_enemyAgent, __instance.Data);
    }

    [HarmonyPatch(typeof(PouncerBehaviour), nameof(PouncerBehaviour.RequestConsume))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_Consume(PouncerBehaviour __instance, int playerSlotIndex)
    {
        if (!SNet.IsMaster || !PlayerManager.TryGetPlayerAgent(ref playerSlotIndex, out var agent)) 
            return;

        if (agent.IsLocallyOwned)
            _handler?.OnConsumed(__instance.m_ai.m_enemyAgent, __instance.Data);
    }

    [HarmonyPatch(typeof(PouncerScreenFX), nameof(PouncerScreenFX.SetCovered), new Type[] { typeof(bool) })]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    private static void Pre_CoverScreen()
    {
        if (GameStateManager.CurrentStateName == eGameStateName.InLevel)
            FocusStateManager.ChangeState(eFocusState.FPS, true); // force exit menu pages
    }
}
