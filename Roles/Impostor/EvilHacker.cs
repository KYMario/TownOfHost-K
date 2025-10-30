using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using UnityEngine;
using HarmonyLib;
using Hazel;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class EvilHacker : RoleBase, IImpostor, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilHacker),
            player => new EvilHacker(player),
            CustomRoles.EvilHacker,
            () => OptionShapeshiftAdmin.GetBool() ? RoleTypes.Shapeshifter : RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3100,
            SetupOptionItems,
            "eh",
            OptionSort: (2, 0),
            from: From.TheOtherRoles
        );
    public EvilHacker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        canSeeDeadMark = OptionCanSeeDeadMark.GetBool();
        canSeeImpostorMark = OptionCanSeeImpostorMark.GetBool();
        canSeeKillFlash = OptionCanSeeKillFlash.GetBool();
        canSeeMurderRoom = OptionCanSeeMurderRoom.GetBool();
        CanseeTaskTurn = OptionShapeshiftAdmin.GetBool();

        CustomRoleManager.OnMurderPlayerOthers.Add(HandleMurderRoomNotify);
        instances.Add(this);
        NameAddmin.Clear();
    }
    public override void OnDestroy()
    {
        instances.Remove(this);
    }

    private static OptionItem OptionCanSeeDeadMark;
    private static OptionItem OptionCanSeeImpostorMark;
    private static OptionItem OptionCanSeeKillFlash;
    private static OptionItem OptionCanSeeMurderRoom;
    private static OptionItem OptionShapeshiftAdmin;
    private enum OptionName
    {
        EvilHackerCanSeeDeadMark,
        EvilHackerCanSeeImpostorMark,
        EvilHackerCanSeeKillFlash,
        EvilHackerCanSeeMurderRoom,
        EvilHackerShapeshiftAdmin
    }
    private static bool canSeeDeadMark;
    private static bool canSeeImpostorMark;
    private static bool canSeeKillFlash;
    private static bool canSeeMurderRoom;
    private static bool CanseeTaskTurn;
    static Dictionary<byte, string> NameAddmin = new();

    private static HashSet<EvilHacker> instances = new(1);

    private HashSet<MurderNotify> activeNotifies = new(2);

    private static void SetupOptionItems()
    {
        OptionCanSeeDeadMark = BooleanOptionItem.Create(RoleInfo, 10, OptionName.EvilHackerCanSeeDeadMark, true, false);
        OptionCanSeeImpostorMark = BooleanOptionItem.Create(RoleInfo, 11, OptionName.EvilHackerCanSeeImpostorMark, true, false);
        OptionCanSeeKillFlash = BooleanOptionItem.Create(RoleInfo, 12, OptionName.EvilHackerCanSeeKillFlash, false, false);
        OptionCanSeeMurderRoom = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EvilHackerCanSeeMurderRoom, false, false, OptionCanSeeKillFlash);
        OptionShapeshiftAdmin = BooleanOptionItem.Create(RoleInfo, 14, OptionName.EvilHackerShapeshiftAdmin, true, false);
    }
    /// <summary>相方がキルした部屋を通知する設定がオンなら各プレイヤーに通知を行う</summary>
    private static void HandleMurderRoomNotify(MurderInfo info)
    {
        if (canSeeMurderRoom)
        {
            foreach (var evilHacker in instances)
            {
                evilHacker.OnMurderPlayer(info);
            }
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        NameAddmin.Clear();
        if (!Player.IsAlive())
        {
            return;
        }
        var admins = AdminProvider.CalculateAdmin();
        var builder = new StringBuilder(512);
        var messagebuilder = new StringBuilder(512);
        var index = 0;
        var aliveplayers = PlayerCatch.AllAlivePlayerControls.OrderBy(x => x.PlayerId).ToArray();
        var deadplayers = PlayerCatch.AllPlayerControls.Where(x => !x.IsAlive()).OrderBy(x => x.PlayerId).ToArray();
        var list = aliveplayers.AddRangeToArray(deadplayers);
        // 送信するメッセージを生成
        foreach (var admin in admins)
        {
            var entry = admin.Value;
            if (entry.TotalPlayers <= 0)
            {
                continue;
            }
            // インポスターがいるなら星マークを付ける
            if (canSeeImpostorMark && entry.NumImpostors > 0)
            {
                builder.Append(ImpostorMark);
            }
            // 部屋名と合計プレイヤー数を表記
            builder.Append(DestroyableSingleton<TranslationController>.Instance.GetString(entry.Room));
            builder.Append(": ");
            builder.Append(entry.TotalPlayers);
            // 死体があったら死体の数を書く
            if (canSeeDeadMark && entry.NumDeadBodies > 0)
            {
                builder.Append('(').Append(GetString("Deadbody"));
                builder.Append('×').Append(entry.NumDeadBodies).Append(')');
            }
            messagebuilder.Append(builder);
            messagebuilder.Append('\n');
            NameAddmin.Add(list[index].PlayerId, builder.ToString());

            builder.Clear();
            index++;
        }
        var message = messagebuilder.ToString();
        var title = Utils.ColorString(Color.green, GetString("LastAdminInfo"));

        _ = new LateTask(() =>
        {
            if (GameStates.IsInGame)
            {
                Utils.SendMessage(message, Player.PlayerId, title, false);
            }
        }, 4f, "EvilHacker Admin Message");
        return;
    }
    private void OnMurderPlayer(MurderInfo info)
    {
        // 生きてる間に相方のキルでキルフラが鳴った場合に通知を出す
        if (!Player.IsAlive() || !(CheckKillFlash(info) == true) || info.AttemptKiller == Player)
        {
            return;
        }
        RpcCreateMurderNotify(info.AttemptTarget.GetPlainShipRoom()?.RoomId ?? SystemTypes.Hallway);
    }
    private void RpcCreateMurderNotify(SystemTypes room)
    {
        CreateMurderNotify(room);
        if (AmongUsClient.Instance.AmHost)
        {
            using var sender = CreateSender();
            sender.Writer.Write((byte)room);
        }
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        CreateMurderNotify((SystemTypes)reader.ReadByte());
    }
    /// <summary>
    /// 名前の下にキル発生通知を出す
    /// </summary>
    /// <param name="room">キルが起きた部屋</param>
    private void CreateMurderNotify(SystemTypes room)
    {
        activeNotifies.Add(new()
        {
            CreatedAt = DateTime.Now,
            Room = room,
        });
        if (AmongUsClient.Instance.AmHost)
        {
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        }
    }
    public override bool NotifyRolesCheckOtherName => true;
    string text = "";
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (GameStates.Intro || GameStates.CalledMeeting) return;
        {
            if (CanseeTaskTurn && player.IsAlive())
            {
                var oldtext = text;
                text = "";
                NameAddmin.Clear();
                var admins = AdminProvider.CalculateAdmin();
                var builder = new StringBuilder(512);

                var index = 0;
                var aliveplayers = PlayerCatch.AllAlivePlayerControls.OrderBy(x => x.PlayerId).ToArray();
                var deadplayers = PlayerCatch.AllPlayerControls.Where(x => !x.IsAlive()).OrderBy(x => x.PlayerId).ToArray();
                var list = aliveplayers.AddRangeToArray(deadplayers);
                foreach (var admin in admins)
                {
                    var entry = admin.Value;
                    if (entry.TotalPlayers <= 0)
                    {
                        continue;
                    }
                    // インポスターがいるなら星マークを付ける
                    if (canSeeImpostorMark && entry.NumImpostors > 0)
                    {
                        builder.Append(ImpostorMark);
                    }
                    builder.Append(DestroyableSingleton<TranslationController>.Instance.GetString(entry.Room));
                    builder.Append(": ");
                    builder.Append(entry.TotalPlayers);
                    if (canSeeDeadMark && entry.NumDeadBodies > 0)
                    {
                        builder.Append('(').Append(GetString("Deadbody"));
                        builder.Append('×').Append(entry.NumDeadBodies).Append(')');
                    }
                    NameAddmin.Add(list[index].PlayerId, builder.ToString());
                    foreach (var data in NameAddmin)
                    {
                        text += $"{data.Key} : {data.Value}";
                    }

                    builder.Clear();
                    index++;
                }
                foreach (var data in NameAddmin)
                {
                    text += $"{data.Key} : {data.Value}";
                }
                if (oldtext != text && AmongUsClient.Instance.AmHost) UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
            }
        }
        // 古い通知の削除処理 Mod入りは自分でやる
        if (!AmongUsClient.Instance.AmHost && Player != PlayerControl.LocalPlayer)
        {
            return;
        }
        if (activeNotifies.Count <= 0)
        {
            return;
        }
        // NotifyRolesを実行するかどうかのフラグ
        var doNotifyRoles = false;
        // 古い通知があれば削除
        foreach (var notify in activeNotifies)
        {
            if (DateTime.Now - notify.CreatedAt > NotifyDuration)
            {
                activeNotifies.Remove(notify);
                doNotifyRoles = true;
            }
        }
        if (doNotifyRoles && AmongUsClient.Instance.AmHost)
        {
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        }
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        var adminInfoText = "";
        if (CanseeTaskTurn || isForMeeting)
        {
            if (!NameAddmin.TryGetValue(seen.PlayerId, out var Admin)) return "";
            adminInfoText = "<color=#8cffff><size=1.5>" + Admin + "</color></size>";
        }
        if (!canSeeMurderRoom || seer != Player || seen != Player || activeNotifies.Count <= 0)
        {
            return adminInfoText += base.GetSuffix(seer, seen, isForMeeting);
        }
        var roomNames = activeNotifies.Select(notify => DestroyableSingleton<TranslationController>.Instance.GetString(notify.Room));
        return adminInfoText += Utils.ColorString(Color.green, $"{GetString("MurderNotify")}: {string.Join(", ", roomNames)}");
    }
    public bool? CheckKillFlash(MurderInfo info) =>
        canSeeKillFlash && !info.IsSuicide && !info.IsAccident && info.AttemptKiller.Is(CustomRoleTypes.Impostor);

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = 1f;
    }
    public override bool CheckShapeshift(PlayerControl target, ref bool shouldAnimate)
    {
        shouldAnimate = false;
        return false;
    }
    public override string GetAbilityButtonText() => GetString("EvilHackerAbility");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "EvilHacker_ability";
        return true;
    }
    public static readonly string ImpostorMark = "★".Color(Palette.ImpostorRed);
    /// <summary>相方がキルしたときに名前の下に通知を表示する長さ</summary>
    private static readonly TimeSpan NotifyDuration = TimeSpan.FromSeconds(10);

    public readonly struct MurderNotify
    {
        /// <summary>通知が作成された時間</summary>
        public DateTime CreatedAt { get; init; }
        /// <summary>キルが起きた部屋</summary>
        public SystemTypes Room { get; init; }
    }
}
