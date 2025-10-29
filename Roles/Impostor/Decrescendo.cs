using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Impostor;

public sealed class Decrescendo : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Decrescendo),
            player => new Decrescendo(player),
            CustomRoles.Decrescendo,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            21000,
            SetupOptionItem,
            "De",
            OptionSort: (8, 2),
            Desc: () =>
            {
                var killcool = OptionNomalKillCool.GetFloat();
                var killx = OptionKillCoolx.GetFloat();
                var visiondesc = "";
                if (OptionDecVision.GetBool())
                {
                    var vision = Main.NormalOptions.ImpostorLightMod;
                    var visionx = OptionVisionx.GetFloat();
                    visiondesc = string.Format(GetString("DecrescendDescVision"), vision, (vision * visionx).Round(0.01f), (vision * visionx * visionx).Round(0.01f), OptionMinVision.GetFloat());
                }

                return string.Format(GetString("DecrescendDesc"), OptionDecKillcount.GetInt(), killcool, (killcool * killx).Round(0.1f), (killcool * killx * killx).Round(0.1f), OptionMaxKillCool.GetFloat(), visiondesc);
            }
);
    public Decrescendo(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCount = 0;
        Decrescending = false;
        NowKillCool = OptionNomalKillCool.GetFloat();
        NowVision = Main.DefaultImpostorVision;
    }
    public bool CanBeLastImpostor { get; } = false;
    static OptionItem OptionNomalKillCool;//通常キルク
    static OptionItem OptionMaxKillCool;//最大キルク
    static OptionItem OptionKillCoolx;//倍率
    static OptionItem OptionDecVision;//視野変更するか
    static OptionItem OptionVisionx;//視野変更の倍率
    static OptionItem OptionMinVision;//最低視野
    static OptionItem OptionDecCantVent;//ベント使えるか
    static OptionItem OptionDecKillcount;//弱化開始キル数
    enum OptionName
    {
        DecrescendoDecKillCount,
        DecrescendoMaxKillcooldown,
        DecrescendoKillCoolx,
        DecrescendoDecVision,
        DecrescendoVisionx,
        DecrescendoMinVision,
        DecrescendoCanUseVent
    }
    /// <summary>弱化中か</summary>
    bool Decrescending;
    int KillCount;
    float NowKillCool;
    float NowVision;
    static void SetupOptionItem()
    {
        OptionDecKillcount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.DecrescendoDecKillCount, new(0, 15, 1), 3, false);
        OptionNomalKillCool = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.KillCooldown, new(0, 180, 0.5f), 25, false).SetValueFormat(OptionFormat.Seconds);
        OptionKillCoolx = FloatOptionItem.Create(RoleInfo, 12, OptionName.DecrescendoKillCoolx, new(1, 2, 0.05f), 1.25f, false).SetValueFormat(OptionFormat.Multiplier);
        OptionMaxKillCool = FloatOptionItem.Create(RoleInfo, 13, OptionName.DecrescendoMaxKillcooldown, new(0, 180, 0.5f), 60, false).SetValueFormat(OptionFormat.Seconds);
        OptionDecVision = BooleanOptionItem.Create(RoleInfo, 14, OptionName.DecrescendoDecVision, true, false);
        OptionVisionx = FloatOptionItem.Create(RoleInfo, 15, OptionName.DecrescendoVisionx, new(0, 1, 0.05f), 0.75f, false, OptionDecVision).SetValueFormat(OptionFormat.Multiplier);
        OptionMinVision = FloatOptionItem.Create(RoleInfo, 16, OptionName.DecrescendoMinVision, new(0, 1, 0.05f), 0.1f, false, OptionDecVision).SetValueFormat(OptionFormat.Multiplier);
        OptionDecCantVent = BooleanOptionItem.Create(RoleInfo, 17, OptionName.DecrescendoCanUseVent, true, false);
    }
    public float CalculateKillCooldown()
    {
        //弱化前なら通常
        if (!Decrescending) return OptionNomalKillCool.GetFloat();
        return NowKillCool;
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (!info.DoKill || !info.CanKill) return;
        var (killer, target) = info.AppearanceTuple;
        if (killer.PlayerId != Player.PlayerId) return;

        Logger.Info($"{KillCount} / {OptionDecKillcount.GetInt()}", "Decrescend");
        KillCount++;
        if (OptionDecKillcount.GetInt() <= KillCount)
        {
            Decrescending = true;

            NowKillCool = NowKillCool * OptionKillCoolx.GetFloat();
            NowVision = NowVision * OptionVisionx.GetFloat();

            if (OptionMaxKillCool.GetFloat() <= NowKillCool) NowKillCool = OptionMaxKillCool.GetFloat();
            if (NowVision <= OptionMinVision.GetFloat()) NowVision = OptionMinVision.GetFloat();
            _ = new LateTask(() =>
            {
                killer.ResetKillCooldown();
                killer.SetKillCooldown(delay: true);
            }, 0.2f, "Decrecend SyncSetting");
        }
        SendRPC();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        //弱化中じゃない or 視界は変更しないなら返す
        if (!Decrescending || !OptionDecVision.GetBool()) return;
        if (!AmongUsClient.Instance.AmHost) return;
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, NowVision);
    }
    public bool CanUseImpostorVentButton()
    {
        //弱化中かつ設定が有効の場合のみ返す
        if (Decrescending && !OptionDecCantVent.GetBool()) return false;
        else return true;
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Decrescending ? Utils.ColorString(ModColors.NeutralGray, "(´・ω・｀)") : Utils.ColorString(Palette.ImpostorRed, $"({KillCount}/{OptionDecKillcount.GetInt()})");

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(KillCount);
        sender.Writer.Write(Decrescending);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        KillCount = reader.ReadInt32();
        Decrescending = reader.ReadBoolean();
    }
}