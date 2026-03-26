using AIGraph;
using Enemies;
using ExteriorRendering;
using GTFO.API.Extensions;
using LevelGeneration;
using Player;
using UnityEngine;

namespace SnatcherBugFix;

public class SnatcherHandler : MonoBehaviour
{
    public PlayerAgent Player = null!;
    public EnemyAgent? Captor;
    public AIG_CourseNode? LastNode;
    public eDimensionIndex LastDimension;
    public Vector3 LastPosition;
    private float _warpTime;
    private const float DeadBuffer = 0.5f;

    public AIG_CourseNode? GoodNode => Captor?.CourseNode ?? LastNode;
    public eDimensionIndex GoodDimension => Captor?.DimensionIndex ?? LastDimension;
    public Vector3 GoodPosition => Captor?.Position ?? LastPosition;
    public bool IsInArenaDim => Player?.Dimension?.IsArenaDimension ?? false;

    public void Awake()
    {
        Player = GetComponent<PlayerAgent>();
        
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

        bool inArenaDim = IsInArenaDim;
        if (_warpTime > 0 && Clock.Time > _warpTime)
        {
            if (inArenaDim)
            {
                Player.RequestWarpToSync(GoodDimension, GoodPosition, Player.FPSCamera.CameraRayDir, PlayerAgent.WarpOptions.None);
                if (Player.Alive)
                    Player.Locomotion.GrabbedByPouncer.StandUp();
                FixWarpFX();
                Player.FPSCamera.PouncerScreenFX.SetCovered(false);
            }
            else if (Player.FPSCamera.PouncerScreenFX.covered)
            {
                Player.FPSCamera.PouncerScreenFX.SetCovered(false);
            }

            Captor = null;
            _warpTime = 0;
            return;
        }

        if (!inArenaDim)
        {
            LastNode = Player.CourseNode;
            LastDimension = Player.DimensionIndex;
            LastPosition = Player.Position;
        }
    }

    public void OnConsumed(EnemyAgent pouncer, PouncerDataContainer data)
    {
        Logger.Debug($"SnatcherHandler OnConsumed, Enemy {pouncer.GetInstanceID()}");
        Captor = pouncer;
        var heldData = data.HeldStateData;
        _warpTime = Clock.Time + data.ConsumeDuration + heldData.HeldStartAnimationDuration + heldData.MaxHeldDuration + heldData.SpitOutStateDuration;
    }
        
    public void OnDead(EnemyAgent pouncer)
    {
        if (Captor == null || Captor.GetInstanceID() != pouncer.GetInstanceID()) return;
        Logger.Debug("SnatcherHandler OnDead");
        Captor = null;
        _warpTime = Clock.Time + DeadBuffer;
    }

    private void FixWarpFX()
    {
        var dimension = GoodNode!.m_dimension;
        ExteriorCamera.GlobalSwitch = dimension.DimensionData.IsOutside;
        if ((bool)dimension.DimensionRootTemp.SkyOcclusionVolume)
        {
            dimension.DimensionRootTemp.SkyOcclusionVolume.Upload();
        }
        if (dimension.DimensionData.IsOutside)
        {
            Lighting.ReflectionsDirty = true;
        }
        if (dimension.IsMainDimension)
        {
            Dimension.SetRealitySoundEnvironment();
        }
        Player.UpdateSoundScape();
    }
}
