using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

using TownOfHost.Attributes;
using TownOfHost.Modules;
using TownOfHost.Modules.ChatManager;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.Madmate;
using TownOfHost.Roles.Crewmate;

using static TownOfHost.Translator;
using TownOfHost.Patches;

namespace TownOfHost;

//参考→https://github.com/0xDrMoe/TownofHost-Enhanced/releases/tag/v1.0.1
//上手くいかない部分はこっちも参考にしました→https://github.com/AsumuAkaguma/TownOfHost_ForE/releases/tag/vBOMB!
public static class GuessManager
{

    public static Dictionary<byte, int> GuesserGuessed = new();
    public static Dictionary<byte, int> OneMeetingGuessed = new();

    [GameModuleInitializer]
    public static void Guessreset()
    {
        GuesserGuessed.Clear();
        OneMeetingGuessed.Clear();//ゲスでターン終了したらリセットされないので
    }
    public static void Reset()
    {
        OneMeetingGuessed.Clear();
    }

    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Length; i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }

    private static bool ComfirmIncludeMsg(string msg, string key)
    {
        var keys = key.Split('|');
        for (int i = 0; i < keys.Length; i++)
        {
            if (msg.Contains(keys[i])) return true;
        }
        return false;
    }
    public static bool GuesserMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;

        int operate = 0;
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "bt", false)) operate = 2;
        else return false;

        if (!pc.IsAlive()) return true;//本当は悪あがきはやめなって処理入れたかった(´・ω・｀)←入れてもいいけどめんどくs(((!(?)
        if (operate == 2)
        {
            var RoleTypes = pc.GetCustomRole().GetCustomRoleTypes();
            if (!Options.ExHideChatCommand.GetBool())
            {
                if (pc.IsAlive()) ChatManager.SendPreviousMessagesToAll();
            }
            //Notゲッサーはここで処理を止める。
            if (!pc.Is(CustomRoles.Guesser)
                && !(pc.Is(CustomRoles.LastImpostor) && LastImpostor.giveguesser)
                && !(pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool())
                && !(RoleAddAddons.GetRoleAddon(pc.GetCustomRole(), out var op, pc, subrole: CustomRoles.Guesser) && op.GiveGuesser.GetBool())
                )
            {
                Utils.SendMessage(GetString("NoOneMeetingGuessederError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("NoOneMeetingGuessederErrortitle")));
                return true;
            }
            if (!MsgToPlayerAndRole(msg, out byte targetId, out CustomRoles role, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId, "<#e6b422>" + GetString("GuessErrortitle") + "</color>");
                return true;
            }
            if (MadAvenger.Skill)//マッドアベンジャー中は処理しないぜ★
            {
                Utils.SendMessage(GetString("GuessErrorMadAvenger"), pc.PlayerId, $"<#ff1919>{GetString("GuessErrorMadAvengerTitle")}</color>");
                return true;
            }
            if (!SelfVoteManager.Canuseability())
            {
                return true;
            }
            if (GameStates.ExiledAnimate)
            {
                Utils.SendMessage(GetString("GuessErrorTuiho"), pc.PlayerId, $"<#ab80c2>{GetString("GuessErrorTuihoTitle")}</color>");
                return true;
            }
            if (Balancer.Id != 255 && !(Balancer.target1 == targetId || Balancer.target2 == targetId) && Balancer.OptionCanMeetingAbility.GetBool()) return true;

            var target = PlayerCatch.GetPlayerById(targetId);
            if (target != null)
            {
                bool guesserSuicide = false;
                if (!GuesserGuessed.ContainsKey(pc.PlayerId)) GuesserGuessed.Add(pc.PlayerId, 0);
                if (!OneMeetingGuessed.ContainsKey(pc.PlayerId)) OneMeetingGuessed.Add(pc.PlayerId, 0);

                var targetroleclass = target.GetRoleClass();
                if (targetroleclass?.CheckGuess(pc) == null && targetroleclass != null) return true;

                //陣営事に区別する
                switch (RoleTypes)
                {
                    case CustomRoleTypes.Impostor:
                        if (GuessCountImp(pc) || role is CustomRoles.Egoist) return true;
                        break;
                    case CustomRoleTypes.Neutral:
                        if (GuessCountNeu(pc)) return true;
                        break;
                    //ｸﾙｰとﾏｯﾄﾞは処理一緒だからまとめる。
                    case CustomRoleTypes.Crewmate:
                    case CustomRoleTypes.Madmate:
                        if (GuessCountCrewandMad(pc)) return true;
                        break;
                }

                switch (RoleTypes)
                {
                    case CustomRoleTypes.Impostor:
                        if (GetImpostorGuessResult(pc, target, role)) return true;
                        break;
                    case CustomRoleTypes.Crewmate:
                        if (GetCrewmateGuessResult(pc, target, role)) return true;
                        break;
                    case CustomRoleTypes.Madmate:
                        if (GetMadmateGuessResult(pc, target, role)) return true;
                        break;
                    case CustomRoleTypes.Neutral:
                        if (GetNeutralGuessResult(pc, target, role)) return true;
                        break;
                }

                if (role == CustomRoles.GM || target.Is(CustomRoles.GM) || role.IsAddOn() || role.IsGhostRole())
                    return true;

                if (pc.Is(CustomRoles.Egoist) && target.Is(CustomRoleTypes.Impostor) && Guesser.ICanGuessNakama.GetBool())
                    return true;

                //↓ここから成功/失敗の処理。
                if (pc.PlayerId == target.PlayerId)
                {
                    Utils.SendMessage(GetString("Guesssuicide"), pc.PlayerId, Utils.ColorString(Color.cyan, GetString("Guesssuicidetitle")));
                    guesserSuicide = true;
                }
                else if (role.IsLovers(false) && target.IsLovers(false))
                {//ラバーズって打っててどれかのラバーズなら破滅★
                    guesserSuicide = false;
                }
                else if (role is CustomRoles.OneLove && target.Is(CustomRoles.OneLove))
                {
                    guesserSuicide = false;
                }
                else
                if (targetroleclass?.CheckGuess(pc) == false)
                {
                    guesserSuicide = true;
                }

                //自殺が決まってないなら処理
                if (CheckTargetRoles(target, role) && !guesserSuicide)
                {
                    guesserSuicide = true;
                }
                Logger.Info($"{pc.GetNameWithRole().RemoveHtmlTags()} が {target.GetNameWithRole().RemoveHtmlTags()} をゲス", "Guesser");

                var dp = guesserSuicide ? pc : target;
                var tempDeathReason = CustomDeathReason.Guess;
                tempDeathReason = guesserSuicide ? CustomDeathReason.Misfire : CustomDeathReason.Guess;
                var dareda = target;
                target = dp;

                Logger.Info($"ゲッサー：{target.GetNameWithRole().RemoveHtmlTags()} ", "Guesser");

                string Name = dp.GetRealName();

                GuesserGuessed[pc.PlayerId]++;
                OneMeetingGuessed[pc.PlayerId]++;

                _ = new LateTask(() =>
                {
                    var playerState = PlayerState.GetByPlayerId(dp.PlayerId);
                    playerState.DeathReason = tempDeathReason;
                    dp.SetRealKiller(pc);
                    MeetingVoteManager.ResetVoteManager(dp.PlayerId);

                    //死者检查
                    if (!dp.IsModClient() && !dp.AmOwner) pc.RpcMeetingKill(dp);
                    CustomRoleManager.OnMurderPlayer(pc, target);

                    _ = new LateTask(() =>
                    {
                        foreach (var pl in PlayerCatch.AllPlayerControls)
                        {
                            Utils.SendMessage(UtilsName.GetPlayerColor(dp, true) + GetString("Meetingkill"), pl.PlayerId, GetString("MSKillTitle"));
                        }
                        ChatManager.OnDisconnectOrDeadPlayer(dp.PlayerId);
                    }, 0.1f, "Guess Msg");
                }, Main.LagTime, "Guesser Kill");

                if (pc == dp)
                {
                    UtilsGameLog.AddGameLog("Guess", string.Format(GetString("guessfall"), UtilsName.GetPlayerColor(pc, true) + $"(<b>{UtilsRoleText.GetTrueRoleName(pc.PlayerId, false)}</b>)", UtilsName.GetPlayerColor(dareda, true), GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role))));
                }
                else
                {
                    UtilsGameLog.AddGameLog("Guess", string.Format(GetString("guesssuccess"), UtilsName.GetPlayerColor(pc, true) + $"(<b>{UtilsRoleText.GetTrueRoleName(pc.PlayerId, false)}</b>)", UtilsName.GetPlayerColor(dp, true) + $"(<b>{UtilsRoleText.GetTrueRoleName(dp.PlayerId, false)}</b>)"));
                }

                _ = new LateTask(() =>
                {
                    string send = "";
                    if (pc == dp)
                    {
                        send = string.Format(GetString("Rgobaku"), UtilsName.GetPlayerColor(pc, true), UtilsName.GetPlayerColor(dareda, true), GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role)));
                    }
                    else
                    {
                        send = string.Format(GetString("RMeetingKill"), UtilsName.GetPlayerColor(pc, true), UtilsName.GetPlayerColor(dp, true));
                    }
                    foreach (var spl in PlayerCatch.AllPlayerControls.Where(pc => !pc.IsAlive())) Utils.SendMessage(send, spl.PlayerId, GetString("RMSKillTitle"));
                }, 2.0f, "Reikai Guess Msg");
            }
        }
        return true;
    }

    private static bool CheckTargetRoles(PlayerControl target, CustomRoles role)
    {
        bool result = !target.Is(role);

        switch (target.GetCustomRole())
        {
            case CustomRoles.Engineer:
                if (role == CustomRoles.Engineer) result = false;
                break;
            case CustomRoles.Scientist:
                if (role == CustomRoles.Scientist) result = false;
                break;
            case CustomRoles.Tracker:
                if (role == CustomRoles.Tracker) result = false;
                break;
            case CustomRoles.Noisemaker:
                if (role == CustomRoles.Noisemaker) result = false;
                break;
            case CustomRoles.Detective:
                if (role == CustomRoles.Detective) result = false;
                break;
            case CustomRoles.Impostor:
                if (role == CustomRoles.Impostor) result = false;
                break;
            case CustomRoles.Shapeshifter:
                if (role == CustomRoles.Shapeshifter) result = false;
                break;
            case CustomRoles.Phantom:
                if (role == CustomRoles.Phantom) result = false;
                break;
            case CustomRoles.Viper:
                if (role == CustomRoles.Viper) result = false;
                break;
        }

        return result;
    }

    public static TextMeshPro NameText(this PlayerControl p) => p.cosmetics.nameText;
    public static TextMeshPro NameText(this PoolablePlayer p) => p.cosmetics.nameText;

    private static bool MsgToPlayerAndRole(string msg, out byte id, out CustomRoles role, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        id = byte.MaxValue;
        string[] args = msg.Split(' ');
        var result = args.Length < 2 ? "" : args[1];
        msg = args.Length < 3 ? "" : args[2];

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else if (result.Contains("レッド") || result.Contains("赤") || result.Contains("red"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Red)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("ブルー") || result.Contains("青") || result.Contains("blue"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Blue)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("ライム") || (result.Contains("黄") && result.Contains("緑")) || result.Contains("lime"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Lime)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("グリーン") || result.Contains("緑") || result.Contains("Green"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Green)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("ピンク") || result.Contains("桃") || result.Contains("pink"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Pink)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("オレンジ") || result.Contains("橙") || result.Contains("orange"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Orange)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("イエロー") || result.Contains("黄") || result.Contains("yellow"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Yellow)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("ブラック") || result.Contains("黒") || result.Contains("black"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Black)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("ホワイト") || result.Contains("白") || result.Contains("white"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.white)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("紫") || result.Contains("Purple") || result.Contains("パープル"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Purple)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("ブラウン") || result.Contains("茶") || result.Contains("brown"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Brown)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("シアン") || result.Contains("水") || result.Contains("cyan"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Cyan)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("小豆") || result.Contains("マルーン") || result.Contains("maroon"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Maroon)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("薄桃") || result.Contains("ローズ") || result.Contains("Rose"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Rose)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("ばなーぬ") || result.Contains("banana") || result.Contains("バナナ"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Banana)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("灰") || result.Contains("グレ－") || result.Contains("gray"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Gray)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("tan") || result.Contains("タン"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Tan)
                    id = pc.PlayerId;
            }
        }
        else if (result.Contains("coral") || result.Contains("珊瑚") || result.Contains("コーラル"))
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.cosmetics.ColorId == (int)ModColors.PlayerColor.Coral)
                    id = pc.PlayerId;
            }
        }
        else
        {
            id = byte.MaxValue;
            error = GetString("GuessError1");
            role = new();
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = PlayerCatch.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("GuessError2");
            role = new();
            return false;
        }

        if (!UtilsRoleInfo.GetRoleByInputName(msg, out role, true))
        {
            error = GetString("GuessError1");
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static string ChangeNormal2Vanilla(CustomRoles role)
    {
        switch (role)
        {
            case CustomRoles.Engineer:
                role = CustomRoles.Engineer;
                break;
            case CustomRoles.Scientist:
                role = CustomRoles.Scientist;
                break;
            case CustomRoles.Tracker:
                role = CustomRoles.Tracker;
                break;
            case CustomRoles.Noisemaker:
                role = CustomRoles.Noisemaker;
                break;
            case CustomRoles.Detective:
                role = CustomRoles.Detective;
                break;
            case CustomRoles.Impostor:
                role = CustomRoles.Impostor;
                break;
            case CustomRoles.Shapeshifter:
                role = CustomRoles.Shapeshifter;
                break;
            case CustomRoles.Phantom:
                role = CustomRoles.Phantom;
                break;
            case CustomRoles.Viper:
                role = CustomRoles.Viper;
                break;
        }

        return GetString(Enum.GetName(typeof(CustomRoles), role)).TrimStart('*').ToLower().Trim().Replace(" ", string.Empty).RemoveHtmlTags();
    }
    public static bool GuessCountImp(PlayerControl pc)
    {
        CustomRoles? RoleNullable = pc?.GetCustomRole();
        CustomRoles role = RoleNullable.Value;
        var roleaddon = RoleAddAddons.GetRoleAddon(role, out var data, pc, subrole: CustomRoles.Guesser);

        //全体
        var ShotLimit = 0;
        if (pc.Is(CustomRoles.Guesser)) ShotLimit = Guesser.CanGuessTime.GetInt();
        if (roleaddon && data.GiveGuesser.GetBool())
        {
            if (data.AddShotLimit.GetBool()) ShotLimit += data.CanGuessTime.GetInt();
            else ShotLimit = data.CanGuessTime.GetInt();
        }
        if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.AddShotLimit.GetBool() && pc.Is(CustomRoles.Guesser)) ShotLimit += LastImpostor.CanGuessTime.GetInt();
        if (pc.Is(CustomRoles.LastImpostor) && (!LastImpostor.AddShotLimit.GetBool() || !pc.Is(CustomRoles.Guesser))) ShotLimit = LastImpostor.CanGuessTime.GetInt();

        //1会議
        var OneMeetingShotLimit = 0;
        if (pc.Is(CustomRoles.Guesser)) OneMeetingShotLimit = Guesser.CanGuessTime.GetInt();
        if (roleaddon && data.GiveGuesser.GetBool()) if (data.GiveGuesser.GetBool()) OneMeetingShotLimit = data.OwnCanGuessTime.GetInt();
        if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.giveguesser) OneMeetingShotLimit = LastImpostor.CanGuessTime.GetInt();

        if (GuesserGuessed[pc.PlayerId] >= ShotLimit)
        {
            Utils.SendMessage(GetString("GuessercountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuessercountErrorT")));
            return true;
        }
        else
        if (OneMeetingGuessed[pc.PlayerId] >= OneMeetingShotLimit)
        {
            Utils.SendMessage(GetString("GuesserMTGcountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuesserMTGcountErrorT")));
            return true;
        }
        else return false;
    }
    public static bool GuessCountNeu(PlayerControl pc)
    {
        CustomRoles? RoleNullable = pc?.GetCustomRole();
        CustomRoles role = RoleNullable.Value;
        var roleaddon = RoleAddAddons.GetRoleAddon(role, out var data, pc, subrole: CustomRoles.Guesser);

        //全体
        var ShotLimit = 0;
        if (pc.Is(CustomRoles.Guesser)) ShotLimit = Guesser.CanGuessTime.GetInt();
        if (roleaddon && data.GiveGuesser.GetBool())
        {
            if (data.AddShotLimit.GetBool()) ShotLimit += data.CanGuessTime.GetInt();
            else ShotLimit = data.CanGuessTime.GetInt();

        }
        if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.AddShotLimit.GetBool() && pc.Is(CustomRoles.Guesser)) ShotLimit += LastNeutral.CanGuessTime.GetInt();
        if (pc.Is(CustomRoles.LastNeutral) && (!LastNeutral.AddShotLimit.GetBool() || !pc.Is(CustomRoles.Guesser))) ShotLimit = LastNeutral.CanGuessTime.GetInt();

        //1会議
        var OneMeetingShotLimit = 0;
        if (pc.Is(CustomRoles.Guesser)) OneMeetingShotLimit = Guesser.CanGuessTime.GetInt();
        if (roleaddon && data.GiveGuesser.GetBool()) OneMeetingShotLimit = data.OwnCanGuessTime.GetInt();
        if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool()) OneMeetingShotLimit = LastNeutral.CanGuessTime.GetInt();

        if (GuesserGuessed[pc.PlayerId] >= ShotLimit)
        {
            Utils.SendMessage(GetString("GuessercountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuessercountErrorT")));
            return true;
        }
        else
        if (OneMeetingGuessed[pc.PlayerId] >= OneMeetingShotLimit)
        {
            Utils.SendMessage(GetString("GuesserMTGcountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuesserMTGcountErrorT")));
            return true;
        }
        else return false;
    }
    public static bool GuessCountCrewandMad(PlayerControl pc)
    {
        CustomRoles? RoleNullable = pc?.GetCustomRole();
        CustomRoles role = RoleNullable.Value;

        var roleaddon = RoleAddAddons.GetRoleAddon(role, out var data, pc, subrole: CustomRoles.Guesser);

        //全体
        var ShotLimit = 0;
        if (pc.Is(CustomRoles.Guesser)) ShotLimit = Guesser.CanGuessTime.GetInt();
        if (roleaddon && data.GiveGuesser.GetBool())
        {
            if (data.AddShotLimit.GetBool()) ShotLimit += data.CanGuessTime.GetInt();
            else ShotLimit = data.CanGuessTime.GetInt();

        }
        //1会議
        var OneMeetingShotLimit = 0;
        if (pc.Is(CustomRoles.Guesser)) OneMeetingShotLimit = Guesser.CanGuessTime.GetInt();
        if (roleaddon && data.GiveGuesser.GetBool()) OneMeetingShotLimit = data.OwnCanGuessTime.GetInt();

        if (GuesserGuessed[pc.PlayerId] >= ShotLimit)
        {
            Utils.SendMessage(GetString("GuessercountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuessercountErrorT")));
            return true;
        }
        else
        if (OneMeetingGuessed[pc.PlayerId] >= OneMeetingShotLimit)
        {
            Utils.SendMessage(GetString("GuesserMTGcountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuesserMTGcountErrorT")));
            return true;
        }
        else return false;
    }

    public static bool GetImpostorGuessResult(PlayerControl pc, PlayerControl target, CustomRoles guessrole)
    {
        var roleaddon = RoleAddAddons.GetRoleAddon(pc.GetCustomRole(), out var data, pc, subrole: CustomRoles.Guesser);

        if (guessrole is CustomRoles.Snitch && target.AllTasksCompleted())
        {
            var CanGuessSnich = Guesser.ICanGuessTaskDoneSnitch.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) CanGuessSnich = data.ICanGuessTaskDoneSnitch.GetBool();
            if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.giveguesser) CanGuessSnich = LastImpostor.ICanGuessTaskDoneSnitch.GetBool();
            if (!CanGuessSnich)
            {
                Utils.SendMessage(string.Format(GetString("GuessSnitch"), GetString("Impostor")), pc.PlayerId, Utils.ColorString(Palette.ImpostorRed, GetString("GuessSnitchTitle")));
                return true;
            }
        }
        //仲間を撃ちぬけるか
        if (guessrole.IsImpostor() && target.Is(CustomRoleTypes.Impostor))
        {
            var Nakama = Guesser.ICanGuessNakama.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) Nakama = data.ICanGuessNakama.GetBool();
            if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.giveguesser) Nakama = LastImpostor.ICanGuessNakama.GetBool();
            if (!Nakama)
            {
                Utils.SendMessage(GetString("GuessTeamMate"), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Impostor), GetString("GuessTeamMateTitle")));
                return true;
            }
        }
        //各白を打ち抜けるか
        if (guessrole.IsWhiteCrew())
        {
            var CanGuessWhiteCrew = Guesser.ICanWhiteCrew.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) CanGuessWhiteCrew = data.ICanWhiteCrew.GetBool();
            if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.giveguesser) CanGuessWhiteCrew = LastImpostor.ICanWhiteCrew.GetBool();
            if (!CanGuessWhiteCrew)
            {
                Utils.SendMessage(string.Format(GetString("GuessWhiteRole"), GetString("Impostor")), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.UltraStar), GetString("GuessWhiteRoleTitle")));
                return true;
            }
        }
        //バニラを撃ちぬけるか
        if (guessrole.IsVanilla())
        {
            var CanGuessVanillaRole = Guesser.ICanGuessVanilla.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) CanGuessVanillaRole = data.ICanGuessVanilla.GetBool();
            if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.giveguesser) CanGuessVanillaRole = LastImpostor.ICanGuessVanilla.GetBool();
            if (!CanGuessVanillaRole)
            {
                Utils.SendMessage(GetString("GuessVanillaRoleTitle"), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(guessrole), GetString("GuessVanillaRole")));
                return true;
            }
        }
        return false;
    }
    public static bool GetNeutralGuessResult(PlayerControl pc, PlayerControl target, CustomRoles guessrole)
    {
        var roleaddon = RoleAddAddons.GetRoleAddon(pc.GetCustomRole(), out var data, pc, subrole: CustomRoles.Guesser);

        //スニッチを撃ちぬけるか
        if (guessrole is CustomRoles.Snitch && target.AllTasksCompleted())
        {
            var CanGuessSnich = Guesser.NCanGuessTaskDoneSnitch.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) CanGuessSnich = data.ICanGuessTaskDoneSnitch.GetBool();
            if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool()) CanGuessSnich = LastNeutral.ICanGuessTaskDoneSnitch.GetBool();
            if (!CanGuessSnich)
            {
                Utils.SendMessage(string.Format(GetString("GuessSnitch"), GetString("Neutral")), pc.PlayerId, Utils.ColorString(Palette.DisabledGrey, GetString("GuessSnitchTitle")));
                return true;
            }
        }
        //各白を打ち抜けるか
        if (guessrole.IsWhiteCrew())
        {
            var CanGuessWhiteCrew = Guesser.NCanWhiteCrew.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) CanGuessWhiteCrew = data.ICanWhiteCrew.GetBool();
            if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool()) CanGuessWhiteCrew = LastNeutral.ICanWhiteCrew.GetBool();
            if (!CanGuessWhiteCrew)
            {
                Utils.SendMessage(string.Format(GetString("GuessWhiteRole"), GetString("Neutral")), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.UltraStar), GetString("GuessWhiteRoleTitle")));
                return true;
            }
        }
        //バニラを撃ちぬけるか
        if (guessrole.IsVanilla())
        {
            var CanGuessVanillaRole = Guesser.NCanGuessVanilla.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) CanGuessVanillaRole = data.ICanGuessVanilla.GetBool();
            if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool()) CanGuessVanillaRole = LastNeutral.ICanGuessVanilla.GetBool();
            if (!CanGuessVanillaRole)
            {
                Utils.SendMessage(GetString("GuessVanillaRoleTitle"), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(guessrole), GetString("GuessVanillaRole")));
                return true;
            }
        }
        return false;
    }
    public static bool GetMadmateGuessResult(PlayerControl pc, PlayerControl target, CustomRoles guessrole)
    {
        var roleaddon = RoleAddAddons.GetRoleAddon(pc.GetCustomRole(), out var data, pc, subrole: CustomRoles.Guesser);

        if (guessrole is CustomRoles.Snitch && target.AllTasksCompleted())
        {
            var CanGuessSnich = Guesser.MCanGuessTaskDoneSnitch.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) CanGuessSnich = data.ICanGuessTaskDoneSnitch.GetBool();
            if (!CanGuessSnich)
            {
                Utils.SendMessage(string.Format(GetString("GuessSnitch"), GetString("Madmate")), pc.PlayerId, Utils.ColorString(ModColors.MadMateOrenge, GetString("GuessSnitchTitle")));
                return true;
            }
        }
        //仲間を撃ちぬけるか
        if (guessrole.IsImpostorTeam())
        {
            var Nakama = Guesser.MCanGuessNakama.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) Nakama = data.ICanGuessNakama.GetBool();
            if (!Nakama)
            {
                Utils.SendMessage(GetString("GuessTeamMate"), pc.PlayerId, Utils.ColorString(ModColors.MadMateOrenge, GetString("GuessTeamMateTitle")));
                return true;
            }
        }
        //各白を打ち抜けるか
        if (guessrole.IsWhiteCrew())
        {
            var CanGuessWhiteCrew = Guesser.MCanWhiteCrew.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) CanGuessWhiteCrew = data.ICanWhiteCrew.GetBool();
            if (!CanGuessWhiteCrew)
            {
                Utils.SendMessage(string.Format(GetString("GuessWhiteRole"), GetString("Madmate")), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.UltraStar), GetString("GuessWhiteRoleTitle")));
                return true;
            }
        }
        //バニラを撃ちぬけるか
        if (guessrole.IsVanilla())
        {
            var CanGuessVanillaRole = Guesser.MCanGuessVanilla.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) CanGuessVanillaRole = data.ICanGuessVanilla.GetBool();
            if (!CanGuessVanillaRole)
            {
                Utils.SendMessage(GetString("GuessVanillaRoleTitle"), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(guessrole), GetString("GuessVanillaRole")));
                return true;
            }
        }
        return false;
    }
    public static bool GetCrewmateGuessResult(PlayerControl pc, PlayerControl target, CustomRoles guessrole)
    {
        var roleaddon = RoleAddAddons.GetRoleAddon(pc.GetCustomRole(), out var data, pc, subrole: CustomRoles.Guesser);

        //仲間を撃ちぬけるか
        if (guessrole.IsCrewmate())
        {
            var Nakama = Guesser.CCanGuessNakama.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) Nakama = data.ICanGuessNakama.GetBool();
            if (!Nakama)
            {
                Utils.SendMessage(GetString("GuessTeamMate"), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Crewmate), GetString("GuessTeamMateTitle")));
                return true;
            }
        }
        //各白を打ち抜けるか
        if (guessrole.IsWhiteCrew())
        {
            var CanGuessWhiteCrew = Guesser.CCanWhiteCrew.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) CanGuessWhiteCrew = data.ICanWhiteCrew.GetBool();
            if (!CanGuessWhiteCrew)
            {
                Utils.SendMessage(string.Format(GetString("GuessWhiteRole"), GetString("Crewmate")), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.UltraStar), GetString("GuessWhiteRoleTitle")));
                return true;
            }
        }
        //バニラを撃ちぬけるか
        if (guessrole.IsVanilla())
        {
            var CanGuessVanillaRole = Guesser.CCanGuessVanilla.GetBool();
            if (roleaddon && data.GiveGuesser.GetBool()) CanGuessVanillaRole = data.ICanGuessVanilla.GetBool();
            if (!CanGuessVanillaRole)
            {
                Utils.SendMessage(GetString("GuessVanillaRoleTitle"), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(guessrole), GetString("GuessVanillaRole")));
                return true;
            }
        }
        return false;
    }
}