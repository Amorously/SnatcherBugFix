using AIGraph;
using Enemies;
using GTFO.API.Extensions;
using LevelGeneration;
using Player;
using SnatcherBugFix.Utils;
using UnityEngine;

namespace SnatcherBugFix;

public class SnatcherHandler : MonoBehaviour
{
    public PlayerAgent Player = null!;
    public EnemyAgent? Captor;
    public DelayedCallback? UncoverCallback, UnwarpCallback;
    public AIG_CourseNode? LastNode;
    public eDimensionIndex LastDimension;
    public Vector3 LastPosition;
    private bool _warpFlag;

    public AIG_CourseNode? GoodNode => Captor?.CourseNode ?? LastNode;
    public eDimensionIndex GoodDimension => Captor?.DimensionIndex ?? LastDimension;
    public Vector3 GoodPosition => Captor?.Position ?? LastPosition;
    public bool IsInArenaDim => Player?.Dimension?.IsArenaDimension ?? false;

    public void Awake()
    {
        Player = GetComponent<PlayerAgent>();
        UncoverCallback = new(() => 2.5f, () => UncoverScreen());
        UnwarpCallback = new(() => 8.0f, () => ArenaUnwarp());
        
        if (!Builder.CurrentFloor.m_dimensions.ToManaged().Any(dim => dim.IsArenaDimension))
        {
            Logger.Error("No arena dimension present in level");
            enabled = false;
            return;
        }
        Logger.Debug("SnatcherHandler is setup and enabled");
    }

    public void Update()
    {
        if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;
        if (Player == null) return;

        if (IsInArenaDim)
        {
            if (!_warpFlag)
            {
                _warpFlag = true;
                UnwarpCallback?.Start();
            }
            return;
        }

        LastNode = Player.CourseNode;
        LastDimension = Player.DimensionIndex;
        LastPosition = Player.Position;
        _warpFlag = false;
    }
    
    public void OnDestroy()
    {
        UncoverCallback?.Cancel();
        UnwarpCallback?.Cancel();
    }   

    public void OnConsumed(EnemyAgent pouncer)
    {
        Logger.Debug($"SnatcherHandler OnConsumed, Enemy {pouncer.GetInstanceID()}");
        Captor = pouncer;        
        UncoverCallback?.Start();
        UnwarpCallback?.Start();
    }
        
    public void OnSpitOut(EnemyAgent pouncer)
    {
        if (Captor == null || Captor.GetInstanceID() != pouncer.GetInstanceID()) return;
        Logger.Debug("SnatcherHandler OnSpitOut (dead)");
        UncoverCallback?.Stop();
        UnwarpCallback?.Stop();
        Captor = null;
    }

    public void UncoverScreen()
    {
        if (!Player.FPSCamera.PouncerScreenFX.covered) return;
        else if (!IsInArenaDim)
        {
            Player.FPSCamera.PouncerScreenFX.SetCovered(false);
            Logger.Warn("Force uncovering local player's screen");
        }
        else UncoverCallback?.Start();
    }

    public void ArenaUnwarp()
    {
        if (!IsInArenaDim)
        {
            _warpFlag = false;
            return;
        }        
        Player.RequestWarpToSync(GoodDimension, GoodPosition, Player.FPSCamera.CameraRayDir, PlayerAgent.WarpOptions.None);
        Logger.Warn("Force teleporting local player out from arena dimension");
    }

    public void DelayedCallbackDebug(float uncoverTime, float unwarpTime)
    {
        UncoverCallback = new(() => uncoverTime, () => UncoverScreen());
        UnwarpCallback = new(() => unwarpTime, () => ArenaUnwarp());
        Logger.Warn("Changed delayed callbacks");
    }
}
