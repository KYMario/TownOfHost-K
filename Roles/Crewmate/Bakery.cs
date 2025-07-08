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
            20700,
            null,
            "bak",
            "#8f6121",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Bakery(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ch = false;
        bunki = 0;
    }
    bool ch;
    int bunki;
    public override string MeetingMeg()
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
        int rect = IRandom.Instance.Next(1, 101);
        int dore = IRandom.Instance.Next(1, 101);
        int meg = IRandom.Instance.Next(1, 4);
        var kisetu = "";
        if (DateTime.Now.Month is 1 or 2 or 12) kisetu = "winter";
        if (DateTime.Now.Month is 3 or 4 or 5) kisetu = "spring";
        if (DateTime.Now.Month is 6 or 7 or 8) kisetu = "summer";
        if (DateTime.Now.Month is 9 or 10 or 11) kisetu = "fall";
        if (!ch)
        {
            if (rect <= 15)//15%以下なら分岐
            {
                ch = true;
                if (dore <= 15)//15%
                {
                    bunki = 1;
                    return GetString("Message.Bakery1");
                }
                else
                if (dore <= 35)//20%
                {
                    bunki = 2;
                    return string.Format(GetString("Message.Bakery2"), GetString($"{kisetu}"));
                }
                else
                if (dore <= 65)//30%
                {
                    bunki = 3;
                    return string.Format(GetString("Message.Bakery3"), (MapNames)Main.NormalOptions.MapId, GetString($"{kisetu}.Ba"));
                }
                else//35%
                {
                    bunki = 4;
                    return GetString($"Message.Bakery4.{meg}");
                }
            }
            return GetString("Message.Bakery");
        }
        else
        {
            switch (bunki)
            {
                case 1:
                    int sns = IRandom.Instance.Next(1, 11);
                    int Like = IRandom.Instance.Next(0, 126);
                    if (Like <= 25) Like = 0;
                    else Like -= 25;
                    int Ripo = IRandom.Instance.Next(0, Like + 5 + 26);
                    if (Ripo <= 25) Ripo = 0;
                    else Ripo -= 25;
                    //26を足したり引いたりしてるのはいいね,リポが0の場合を多くするため。

                    if (sns is 9) return string.Format(GetString($"Message.Bakery1.9"), $"{IRandom.Instance.Next((UtilsGameLog.day - 1) * 5, UtilsGameLog.day * 5) * 10}") + string.Format("\n　<color=#ff69b4>♥</color>{0}　<color=#7cfc00>Θ</color>{1}", Like, Ripo); ;
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
    }
}