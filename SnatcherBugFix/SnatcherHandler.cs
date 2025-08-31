using Enemies;
using GTFO.API.Extensions;
using LevelGeneration;
using Player;
using SnatcherBugFix.Utils;
using UnityEngine;

namespace SnatcherBugFix;

public class SnatcherHandler : MonoBehaviour
{
    public PlayerAgent Player;
    public DelayedCallback UncoverCallback, UnwarpCallback;
    public eDimensionIndex LastDimension;
    public Vector3 LastPosition;

    public void Awake()
    {
        Player = GetComponent<PlayerAgent>();
        UncoverCallback = new(() => 2.5f, () => UncoverScreen());
        UnwarpCallback = new(() => 8.0f, () => ArenaUnwarp());
    }

    public void OnEnable()
    {
        if (!Builder.CurrentFloor.m_dimensions.ToManaged().Any(dim => dim.IsArenaDimension))
        {
            Logger.Error("No arena dimension present in level!!");
            enabled = false;
            return;
        }
        Logger.Debug("SnatcherHandler is setup and enabled");
    }

    public void OnConsumed(EnemyAgent pouncer)
    {
        Logger.Debug("SnatcherHandler OnConsumed");

        Vector3 pos = pouncer.Position;
        if (pos != Vector3.zero)
        {
            LastPosition = pos;
            LastDimension = Dimension.GetDimensionFromPos(pos).DimensionIndex;
        }

        UncoverCallback.Start();
        UnwarpCallback.Start();
    }

    public void OnSpitOut(EnemyAgent pouncer)
    {
        Logger.Debug("SnatcherHandler OnSpitOut (dead)");

        Vector3 pos = pouncer.Position;
        if (pos != Vector3.zero)
        {
            LastPosition = pos;
            LastDimension = Dimension.GetDimensionFromPos(pos).DimensionIndex;
        }

        UncoverCallback.Stop();
        UnwarpCallback.Stop();
    }

    public void UncoverScreen()
    {
        if (!Player.FPSCamera.PouncerScreenFX.covered) return;
        else if (Player.DimensionIndex < eDimensionIndex.Dimension_17)
        {
            Player.FPSCamera.PouncerScreenFX.SetCovered(false);
            Logger.Warn("Force uncovering local player's screen");
        }
        else UncoverCallback?.Start();
    }

    public void ArenaUnwarp()
    {
        if (Player.DimensionIndex >= eDimensionIndex.Dimension_17)
        {
            Player.RequestWarpToSync(LastDimension, LastPosition, Player.FPSCamera.CameraRayDir, PlayerAgent.WarpOptions.None);
            Logger.Warn("Force teleporting local player out from arena dimension");
        }
        else UnwarpCallback?.Start();
    }
}
