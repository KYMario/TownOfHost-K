using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class DummySpawner : RoleBase, IImpostor, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(DummySpawner),
            player => new DummySpawner(player),
            CustomRoles.DummySpawner,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            27000,
            SetupOptionItem,
            "ds",
            "#ff4444",
            from: From.SuperNewRoles
        );

    public DummySpawner(PlayerControl player)
        : base(RoleInfo, player)
    {
        KillCooldownValue = OptionKillCooldown.GetFloat();
        SpawnedDummies = new();
    }

    static OptionItem OptionKillCooldown;
    static float KillCooldownValue;
    private readonly List<RandomDummy> SpawnedDummies;

    static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown,
            new(0f, 60f, 0.5f), 10f, false).SetValueFormat(OptionFormat.Seconds);
    }

    public float CalculateKillCooldown() => KillCooldownValue;
    public bool CanUseKillButton() => Player.IsAlive();
    public bool CanUseSabotageButton() => true;
    public bool CanUseImpostorVentButton() => true;

    // ★ キルボタンを押したとき: 通常キルの前に近くのダミーを優先してキル
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var killable = CustomNetObject.GetKillableTarget(Player, 1.5f);
        if (killable is IKillableDummy kd)
        {
            info.DoKill = false; // 通常キルをキャンセル
            kd.OnKilled(Player);
            Player.ResetKillCooldown();
            Player.SetKillCooldown();
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
        }
        // killableがnullなら通常キル（info.DoKill = trueのまま）
    }

    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive()) return;

        // 前のターンのダミーを削除
        foreach (var d in SpawnedDummies) d?.Despawn();
        SpawnedDummies.Clear();

        // ★ 100個をキューに積む（0.05秒間隔で処理される）
        for (int i = 0; i < 100; i++)
        {
            int index = i;
            _ = new LateTask(() =>
            {
                if (!Player.IsAlive()) return;
                var pos = GetRandomMapPosition();
                var dummy = new RandomDummy(pos);
                SpawnedDummies.Add(dummy);
            }, i * 0.02f, $"DS.Queue{index}", true);
        }
    }

    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        foreach (var d in SpawnedDummies) d?.Despawn();
        SpawnedDummies.Clear();
    }

    private static Vector2 GetRandomMapPosition()
    {
        var rng = IRandom.Instance;
        int mapId = Main.NormalOptions?.MapId ?? 0;
        return mapId switch
        {
            0 => new Vector2(rng.Next(-25, 20), rng.Next(-10, 5)),
            1 => new Vector2(rng.Next(-5, 20), rng.Next(-5, 15)),
            2 => new Vector2(rng.Next(-20, 25), rng.Next(-25, 5)),
            3 => new Vector2(rng.Next(-20, 30), rng.Next(-15, 15)),
            4 => new Vector2(rng.Next(-20, 20), rng.Next(-15, 10)),
            _ => new Vector2(rng.Next(-20, 20), rng.Next(-10, 10)),
        };
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (!Player.IsAlive()) return "";
        return SpawnedDummies.Count > 0
            ? $"<color=#ff4444>(ダミー:{SpawnedDummies.Count})</color>"
            : "";
    }
}

// ★ キル可能なランダムダミー
public sealed class RandomDummy : CustomNetObject, IKillableDummy
{
    private static readonly string[] SkinIds =
    {
        "skin_Astronaut", "skin_BlackSuit", "skin_CaptainA",
        "skin_Hazmat", "skin_Military", "skin_Police",
        "skin_Science", "skin_SuitB", "skin_Wall",
        "skin_Winter", "",
    };

    private static readonly string[] HatIds =
    {
        "hat_PaperHat", "hat_Fedora", "hat_TopHat",
        "hat_Antenna", "hat_Crown", "hat_FloppyHat",
        "hat_Eyebrows", "hat_Captain", "hat_Goggles",
        "hat_HardHat", "hat_Halo", "hat_Beanie", "",
    };

    private static readonly string[] VisorIds =
    {
        "visor_Visor", "visor_AngryEyes", "visor_ArcticGoggles",
        "visor_BubbleVisor", "visor_CandyCorns", "visor_CoolVisor",
        "visor_FlameVisor", "visor_GreenVisor", "visor_HalfVisor",
        "visor_HorrorVisor", "visor_LobsterVisor", "visor_Mira", "",
    };

    private static readonly string[] PetIds =
    {
        "pet_Alien", "pet_Bedcrab", "pet_Bushfriend",
        "pet_Charles", "pet_Clank", "pet_Crewmate",
        "pet_Doggy", "pet_Ellie", "pet_Hamster",
        "pet_Limegreen", "pet_Mini", "pet_Norbert",
        "pet_Squig", "pet_Stickmin", "pet_UFO", "",
    };

    private readonly int _colorId;
    private readonly string _skinId;
    private readonly string _hatId;
    private readonly string _visorId;
    private readonly string _petId;
    private readonly Vector2 _spawnPos;

    public RandomDummy(Vector2 position)
    {
        var rng = IRandom.Instance;
        _colorId = rng.Next(0, 18);
        _skinId = SkinIds[rng.Next(0, SkinIds.Length)];
        _hatId = HatIds[rng.Next(0, HatIds.Length)];
        _visorId = VisorIds[rng.Next(0, VisorIds.Length)];
        _petId = PetIds[rng.Next(0, PetIds.Length)];
        _spawnPos = position;

        CreateNetObject(position);
    }

    protected override void OnCreated()
    {
        SetAppearance(_colorId, _skinId, _hatId, _petId, _visorId);
        SetName("Dummy");
        SnapToPosition(_spawnPos);
    }

    // ★ キルされたときの処理
    public void OnKilled(PlayerControl killer)
    {
        Logger.Info($"Dummy killed by {killer?.Data?.GetLogPlayerName()}", "RandomDummy");
        // キルエフェクト的にフラッシュさせてから削除
        Utils.AllPlayerKillFlash();
        Despawn();
    }

    public override void OnMeeting()
    {
        Despawn();
    }
}