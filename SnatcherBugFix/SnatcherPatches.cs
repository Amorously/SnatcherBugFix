using Enemies;
using GTFO.API;
using HarmonyLib;
using Player;
using SnatcherBugFix.Utils;
using SNetwork;

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
            enemy.AddOnDeadOnce(() => _handler?.OnDead(enemy)); // force spit out captured player on death
    }

    [HarmonyPatch(typeof(PouncerBehaviour), nameof(PouncerBehaviour.OnConsumeRequestReceived))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_Consume(PouncerBehaviour __instance, pEB_PouncerTargetInfoPacket data)
    {
        if (!PlayerManager.TryGetPlayerAgent(ref data.PlayerSlot, out var agent)) return;

        if (agent.IsLocallyOwned == true)
            _handler?.OnConsumed(__instance.m_ai.m_enemyAgent, __instance.Data);
    }

    [HarmonyPatch(typeof(PouncerBehaviour), nameof(PouncerBehaviour.RequestConsume))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void Post_Consume(PouncerBehaviour __instance, int playerSlotIndex)
    {
        if (!SNet.IsMaster || !PlayerManager.TryGetPlayerAgent(ref playerSlotIndex, out var agent)) return;

        if (agent.IsLocallyOwned == true)
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
