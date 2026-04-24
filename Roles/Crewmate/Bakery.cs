using System;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class Bakery : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Bakery),
            player => new Bakery(player),
            CustomRoles.Bakery,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            10200,
            SetupOptionItem,
            "bak",
            "#8f6121",
            (4, 2),
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );

    private static OptionItem PoisonedBakeryChanceOption;

    public Bakery(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        RareRoute = false;
        RouteNumber = 0;
        IsOnlyNomalMessage = false;
    }

    bool RareRoute;
    int RouteNumber;
    static bool IsOnlyNomalMessage;

    public static void SetupOptionItem()
    {
        PoisonedBakeryChanceOption = FloatOptionItem.Create(
            RoleInfo,
            12,
            "PoisonedBakeryRole77",
            new(0f, 100f, 1f),
            25f,
            false
        ).SetValueFormat(OptionFormat.Percent);
    }

    public override void Add()
    {
        var bakerycount = 0;
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (pc.GetCustomRole() is CustomRoles.Bakery) bakerycount++;
            if (pc.GetCustomRole() is CustomRoles.AllArounder && AllArounder.RandomBakery.GetBool())
            {
                IsOnlyNomalMessage = true;
                break;
            }
        }
        if (bakerycount > 1) IsOnlyNomalMessage = true;
    }

    public override string MeetingAddMessage()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return "";
        if (Player.IsAlive())
        {
            string BakeryTitle = $"<size=90%><color=#8f6121>{GetString("Message.BakeryTitle")}</size></color>";
            return BakeryTitle + "\n<size=70%>" + BakeryMeg() + "</size>\n";//, title: "<color=#8f6121>" + BakeryTitle);
        }
        return "";
    }

    string BakeryMeg()
    {
        if (IsOnlyNomalMessage)
            return GetString("Message.Bakery");
        int rect = IRandom.Instance.Next(1, 101);
        int dore = IRandom.Instance.Next(1, 101);
        int meg = IRandom.Instance.Next(1, 4);
        var kisetu = "";
        if (DateTime.Now.Month is 1 or 2 or 12) kisetu = "winter";
        if (DateTime.Now.Month is 3 or 4 or 5) kisetu = "spring";
        if (DateTime.Now.Month is 6 or 7 or 8) kisetu = "summer";
        if (DateTime.Now.Month is 9 or 10 or 11) kisetu = "fall";
        if (RareRoute is false)
        {
            if (rect <= 15)//15%以下なら分岐
            {
                RareRoute = true;
                if (dore <= 15)//15%
                {
                    RouteNumber = 1;
                    return GetString("Message.Bakery1");
                }
                else
                    if (dore <= 35)
                    {
                        RouteNumber = 2;
                        return string.Format(GetString("Message.Bakery2"), GetString($"{kisetu}"));
                    }
                    else
                        if (dore <= 65)
                        {
                            RouteNumber = 3;
                            return string.Format(GetString("Message.Bakery3"), (MapNames)Main.NormalOptions.MapId, GetString($"{kisetu}.Ba"));
                        }
                        else//35%
                        {
                            RouteNumber = 4;
                            return GetString($"Message.Bakery4.{meg}");
                        }
            }
            return GetString("Message.Bakery");
        }
        else
        {
            switch (RouteNumber)
            {
                case 1:
                    int sns = IRandom.Instance.Next(1, 11);
                    int Like = IRandom.Instance.Next(0, 126);
                    if (Like <= 25) Like = 0;
                    else Like -= 25;
                    int Ripo = IRandom.Instance.Next(0, Like + 5 + 26);
                    if (Ripo <= 25) Ripo = 0;
                    else Ripo -= 25;


                    if (sns is 9) return string.Format(GetString($"Message.Bakery1.9"), $"{IRandom.Instance.Next((UtilsGameLog.day - 1) * 5, UtilsGameLog.day * 5) * 10}") + string.Format("\n　<color=#ff69b4>♥</color>{0}　<color=#7cfc00>Θ</color>{1}", Like, Ripo);
                    if (sns is 8) return GetString("Message.Bakery1.8");
                    return GetString($"Message.Bakery1.{sns}") + string.Format("\n　<color=#ff69b4>♥</color>{0}　<color=#7cfc00>Θ</color>{1}", Like, Ripo);
                case 2:
                    return GetString($"Message.Bakery2.{meg}");
                case 3:
                    return string.Format(GetString($"Message.Bakery3.{meg}"), GetString($"{kisetu}.Ba"));
                case 4:
                    if (rect <= 50) return GetString("Message.Bakery");
                    else return GetString($"Message.Bakery4.{meg}");
            }
        }
        //ここまで来たらバグじゃ!!
        return "なんかエラー起きてるよ(´-ω-`)\nホストさんログ取って提出して☆";
    }

    private RoleTypes ConvertCustomRoleToRoleType(CustomRoles customRole)
    {
        return customRole switch
        {
            CustomRoles.PoisonedBakery => RoleTypes.Crewmate,
            _ => RoleTypes.Crewmate
        };
    }

    public override void AfterMeetingTasks()
    {
        Logger.Info("AfterMeetingTasks called for Bakery", nameof(Bakery));
        try
        {
            base.AfterMeetingTasks();

            if (Player == null)
            {
                Logger.Warn("Player is null in Bakery.AfterMeetingTasks", nameof(Bakery));
                return;
            }

            if (!Player.IsAlive()) return;

            if (PoisonedBakeryChanceOption == null)
            {
                Logger.Warn("PoisonedBakeryChanceOption is null", nameof(Bakery));
                return;
            }

            var chance = PoisonedBakeryChanceOption.GetFloat();
            var rand = IRandom.Instance.Next(1, 101);

            Logger.Info($"Chance: {chance}, Random: {rand}", nameof(Bakery));

            if (rand <= chance)
            {
                Logger.Info("Schedule transforming into PoisonedBakery", nameof(Bakery));
                _ = new LateTask(() =>
                {
                    try
                    {
                        if (Player == null)
                        {
                            Logger.Warn("Player is null when executing delayed Bakery transform", nameof(Bakery));
                            return;
                        }

                        if (!AmongUsClient.Instance.AmHost)
                        {
                            Logger.Info("Not host - skipping role change", nameof(Bakery));
                            return;
                        }

                        Player.RpcSetCustomRole(CustomRoles.PoisonedBakery);
                        Utils.SendMessage("<color=#a83232>パン屋が<b><size=120%>パン屋(N)</b></size>に変化しました。</color>", Player.PlayerId);
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex, nameof(Bakery));
                    }
                }, 0.05f, "BakeryTransform", true);
            }
        }
        catch (Exception e)
        {
            Logger.Exception(e, nameof(Bakery));
        }
    }

    public static string BakeryMark()
    {
        var bakerys = PlayerCatch.AllAlivePlayerControls.Where(pc =>
        {
            if (pc.GetRoleClass() is AllArounder allArounder)
            {
                return allArounder.NowRole is AllArounder.NowMode.Bakery && allArounder.CanUseAbility();
            }
            return pc.GetCustomRole() is CustomRoles.Bakery;
        });
        if (bakerys.Count() <= 0) return "";

        return $" <#8f6121><rotate=-20>§</rotate></color>{(bakerys.Count() > 1 ? $"×{bakerys.Count()}" : "")}";
        //非クライアントに§が表示されないのでそこをいつか直します
    }


}