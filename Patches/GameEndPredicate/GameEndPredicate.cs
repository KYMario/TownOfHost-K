using System.Linq;
using HarmonyLib;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;

namespace TownOfHost;

public abstract class GameEndPredicate
{
    /// <summary>ゲームの終了条件をチェックし、CustomWinnerHolderに値を格納します。</summary>
    /// <params name="reason">バニラのゲーム終了処理に使用するGameOverReason</params>
    /// <returns>ゲーム終了の条件を満たしているかどうか</returns>
    public abstract bool CheckForEndGame(out GameOverReason reason);

    /// <summary>GameData.TotalTasksとCompletedTasksをもとにタスク勝利が可能かを判定します。</summary>
    public virtual bool CheckGameEndByTask(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorsByKill;
        if (Options.DisableTaskWin.GetBool() || TaskState.InitialTotalTasks == 0) return false;

        if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
        {
            reason = GameOverReason.CrewmatesByTask;
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Crewmate, byte.MaxValue);
            return true;
        }
        return false;
    }
    /// <summary>ShipStatus.Systems内の要素をもとにサボタージュ勝利が可能かを判定します。</summary>
    public virtual bool CheckGameEndBySabotage(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorsByKill;
        if (ShipStatus.Instance.Systems == null) return false;
        if (GameStates.IsMeeting) return false;

        // TryGetValueは使用不可
        var systems = ShipStatus.Instance.Systems;
        LifeSuppSystemType LifeSupp;
        if (systems.ContainsKey(SystemTypes.LifeSupp) && // サボタージュ存在確認
            (LifeSupp = systems[SystemTypes.LifeSupp].TryCast<LifeSuppSystemType>()) != null && // キャスト可能確認
            LifeSupp.Countdown < 0f) // タイムアップ確認
        {
            // 酸素サボタージュ
            if (Options.ChangeSabotageWinRole.GetBool())
            {
                var pc = PlayerCatch.GetPlayerById(Main.LastSab);
                var role = pc.GetCustomRole();

                switch (role)
                {
                    case CustomRoles.Jackal:
                    case CustomRoles.JackalMafia:
                    case CustomRoles.JackalAlien:
                        CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Jackal, byte.MaxValue);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalMafia);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalAlien);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackaldoll);
                        break;
                    case CustomRoles.GrimReaper:
                        CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.GrimReaper, byte.MaxValue);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.GrimReaper);
                        CustomWinnerHolder.NeutralWinnerIds.Add(PlayerCatch.AllPlayerControls
                            .Where(pc => pc.GetCustomRole() is CustomRoles.GrimReaper).FirstOrDefault()?.PlayerId ?? byte.MaxValue);
                        break;
                    case CustomRoles.Egoist:
                        CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Egoist, byte.MaxValue);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Egoist);
                        CustomWinnerHolder.NeutralWinnerIds.Add(PlayerCatch.AllPlayerControls
                            .Where(pc => pc.GetCustomRole() is CustomRoles.Egoist).FirstOrDefault()?.PlayerId ?? byte.MaxValue);
                        break;
                    case CustomRoles.MadBetrayer:
                        CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.MadBetrayer, byte.MaxValue);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.MadBetrayer);
                        CustomWinnerHolder.NeutralWinnerIds.Add(PlayerCatch.AllPlayerControls
                            .Where(pc => pc.GetCustomRole() is CustomRoles.MadBetrayer).FirstOrDefault()?.PlayerId ?? byte.MaxValue);
                        break;
                    default:
                        CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, byte.MaxValue);
                        break;
                }
                reason = GameOverReason.ImpostorsBySabotage;
                Main.IsActiveSabotage = false;
                LifeSupp.Countdown = 10000f;
                return true;
            }
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, byte.MaxValue);
            Main.IsActiveSabotage = false;
            reason = GameOverReason.ImpostorsBySabotage;
            LifeSupp.Countdown = 10000f;
            return true;
        }

        ISystemType sys = null;
        if (systems.ContainsKey(SystemTypes.Reactor)) sys = systems[SystemTypes.Reactor];
        else if (systems.ContainsKey(SystemTypes.Laboratory)) sys = systems[SystemTypes.Laboratory];
        else if (systems.ContainsKey(SystemTypes.HeliSabotage)) sys = systems[SystemTypes.HeliSabotage];
        ICriticalSabotage critical;
        if (sys != null && // サボタージュ存在確認
            (critical = sys.TryCast<ICriticalSabotage>()) != null && // キャスト可能確認
            critical.Countdown < 0f) // タイムアップ確認
        {
            if (SuddenDeathMode.NowSuddenDeathMode)
            {
                PlayerCatch.AllAlivePlayerControls.Do(p => p.RpcMurderPlayerV2(p));
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                Main.IsActiveSabotage = false;
                reason = GameOverReason.ImpostorsBySabotage;
                critical.ClearSabotage();
                return true;
            }
            // リアクターサボタージュ
            if (Options.ChangeSabotageWinRole.GetBool())
            {
                var pc = PlayerCatch.GetPlayerById(Main.LastSab);
                var role = pc.GetCustomRole();

                switch (role)
                {
                    case CustomRoles.Jackal:
                    case CustomRoles.JackalMafia:
                    case CustomRoles.JackalAlien:
                        CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Jackal, byte.MaxValue);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalMafia);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalAlien);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackaldoll);
                        break;
                    case CustomRoles.GrimReaper:
                        CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.GrimReaper, byte.MaxValue);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.GrimReaper);
                        CustomWinnerHolder.NeutralWinnerIds.Add(PlayerCatch.AllPlayerControls
                            .Where(pc => pc.GetCustomRole() is CustomRoles.GrimReaper).FirstOrDefault()?.PlayerId ?? byte.MaxValue);
                        break;
                    case CustomRoles.Egoist:
                        CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Egoist, byte.MaxValue);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Egoist);
                        CustomWinnerHolder.NeutralWinnerIds.Add(PlayerCatch.AllPlayerControls
                            .Where(pc => pc.GetCustomRole() is CustomRoles.Egoist).FirstOrDefault()?.PlayerId ?? byte.MaxValue);
                        break;
                    case CustomRoles.MadBetrayer:
                        CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.MadBetrayer, byte.MaxValue);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.MadBetrayer);
                        CustomWinnerHolder.NeutralWinnerIds.Add(PlayerCatch.AllPlayerControls
                            .Where(pc => pc.GetCustomRole() is CustomRoles.MadBetrayer).FirstOrDefault()?.PlayerId ?? byte.MaxValue);
                        break;
                    default:
                        CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, byte.MaxValue);
                        break;
                }
                Main.IsActiveSabotage = false;
                reason = GameOverReason.ImpostorsBySabotage;
                critical.ClearSabotage();
                return true;
            }
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, byte.MaxValue);
            Main.IsActiveSabotage = false;
            reason = GameOverReason.ImpostorsBySabotage;
            critical.ClearSabotage();
            return true;
        }

        return false;
    }
}