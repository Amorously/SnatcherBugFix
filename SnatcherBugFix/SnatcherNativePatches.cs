using AIGraph;
using BepInEx.Unity.IL2CPP.Hook;
using GTFO.API;
using Il2CppInterop.Runtime.Runtime;
using Player;
using PlayerCoverage;

namespace SnatcherBugFix
{
    internal static class SnatcherNativePatches
    {
        internal static void Init()
        {
            ApplyNativePatch();
        }

        private static INativeDetour _detour = null!;
        private static d_TryGetClosestAlivePlayerAgent orig_TryGetClosestAlivePlayerAgent = null!;
        private unsafe delegate bool d_TryGetClosestAlivePlayerAgent(IntPtr node, out IntPtr playerAgent, Il2CppMethodInfo* methodInfo);

        // Can't harmony patch the function due to out parameter so need a native detour
        private unsafe static void ApplyNativePatch()
        {
            _detour = INativeDetour.CreateAndApply(
                (nint)Il2CppAPI.GetIl2CppMethod<PlayerManager>(
                    nameof(PlayerManager.TryGetClosestAlivePlayerAgent),
                    typeof(bool).FullName,
                    false,
                    new[] {
                        typeof(AIG_CourseNode).FullName,
                        typeof(PlayerAgent).MakeByRefType().FullName
                    }),
                TryGetClosestAlivePlayerAgentPatch,
                out orig_TryGetClosestAlivePlayerAgent
                );
        }

        // Copy/paste but excludes players in the snatcher dimension.
        // Used mainly by wave spawn system with a few other minor cases.
        private unsafe static bool TryGetClosestAlivePlayerAgentPatch(IntPtr node, out IntPtr playerAgent, Il2CppMethodInfo* methodInfo)
        {
            AIG_CourseNode courseNode = new(node);
            PlayerAgent? best = null;
            PlayerAgent? recoveryPlayer = null;
            int num = int.MaxValue;
            for (int i = 0; i < PlayerManager.PlayerAgentsInLevel.Count; i++)
            {
                PlayerAgent player = PlayerManager.PlayerAgentsInLevel[i];
                if (!(player != null) || !player.Alive) continue;
                if (player.Dimension?.IsArenaDimension == true) continue;

                recoveryPlayer = player;
                if (i < courseNode.m_playerCoverage.m_coverageDatas.Length)
                {
                    PlayerCoverageSystem.PlayerCoverageData playerCoverageData = courseNode.m_playerCoverage.m_coverageDatas[i];
                    if (playerCoverageData.IsValidNodeDistance && num > playerCoverageData.m_nodeDistance)
                    {
                        num = playerCoverageData.m_nodeDistance;
                        best = player;
                    }
                }
            }

            if (best == null)
                best = recoveryPlayer;

            playerAgent = best?.Pointer ?? IntPtr.Zero;
            return playerAgent != IntPtr.Zero;
        }
    }
}
