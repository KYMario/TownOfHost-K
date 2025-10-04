using System.Collections.Generic;
using System.Linq;
using System.Text;

using Hazel;
using UnityEngine;
using HarmonyLib;
using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Impostor;

/// コードがクッソ長い!!スパゲッティかよ!!まぁ処理が複雑な役職だからね。仕方ない。
//
// メモ
// 追加したいなぁって思ってるの
// マジシャン(キルボタンぜんぶ吹っ飛ばす...流石に強い気がする)
// ウィッチ(キルで呪い付与...弱いかなぁ...)
// エイリアンジャッカルって需要ありそうじゃね?(???)シェイプも丁度開いてるし！

public sealed class Alien : RoleBase, IMeetingTimeAlterable, IImpostor, INekomata
{
    public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Alien),
                player => new Alien(player),
                CustomRoles.Alien,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                2000,
                SetupOptionItem,
                "Al",
                OptionSort: (1, 0),
                introSound: () => GetIntroSound(RoleTypes.Shapeshifter)
            );
    public Alien(PlayerControl player)
: base(
    RoleInfo,
    player
    )
    {
        #region Init
        Init();
        PuppetCooltime.Clear();
        BittenPlayers.Clear();
        tmpSpeed = Main.NormalOptions.PlayerSpeedMod;
        Count = 0;
        Remotekillertarget = 111;

        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        InsiderCansee.Clear();
        NameAddmin.Clear();
    }
    public override void Add()
    {
        Uetukecount = 0;
        AddSelfVotes(Player);
        AbductTimer = 255f;
        stopCount = false;
        Aliens.Add(this);
        if (FirstAbility.GetBool()) AfterMeetingTasks();
    }
    public override void OnDestroy()
    {
        PuppetCooltime.Clear();
        Aliens.Clear();
        Puppets.Clear();
        AbductVictim = null;
    }
    enum RPC_type
    {
        SyncPuppet,
        StealthDarken,
        Penguin
    }
    #endregion

    #region Meeting
    public override void OnReportDeadBody(PlayerControl repo, NetworkedPlayerInfo __)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        Remotekillertarget = 111;
        NameAddmin.Clear();
        Puppets.Clear();
        PuppetCooltime.Clear();
        SendRPC(byte.MaxValue, 0);
        foreach (var targetId in BittenPlayers.Keys)
        {
            var target = PlayerCatch.GetPlayerById(targetId);
            KillBitten(target, true);
        }
        BittenPlayers.Clear();
        stopCount = true;
        // 時間切れ状態で会議を迎えたらはしご中でも構わずキルする
        if (AbductVictim != null && AbductTimer <= 0f)
            Player.RpcMurderPlayer(AbductVictim);
        if (MeetingKill)
        {
            if (AbductVictim != null)
            {
                Player.RpcMurderPlayer(AbductVictim);
                RemoveVictim();
            }
        }
        if (!Player.IsAlive()) return;

        if (modeEvilHacker)
        {
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
                if (entry.NumImpostors > 0)
                {
                    builder.Append(EvilHacker.ImpostorMark);
                }
                // 部屋名と合計プレイヤー数を表記
                builder.Append(DestroyableSingleton<TranslationController>.Instance.GetString(entry.Room));
                builder.Append(": ");
                builder.Append(entry.TotalPlayers);
                // 死体があったら死体の数を書く
                if (entry.NumDeadBodies > 0)
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
        }
    }
    public override void OnStartMeeting()
    {
        if (AmongUsClient.Instance.AmHost)
            ResetDarkenState();
    }
    public override CustomRoles TellResults(PlayerControl player) => modeTairo ? CustomRoles.Crewmate : CustomRoles.Alien;
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);

        if (Options.firstturnmeeting && Options.FirstTurnMeetingCantability.GetBool() && MeetingStates.FirstMeeting) return (votedForId, numVotes, doVote);
        if (voterId == Player.PlayerId && modeMayor)
        {
            numVotes = AdditionalVote + 1;
        }
        return (votedForId, numVotes, doVote);
    }
    public int CalculateMeetingTimeDelta()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return 0;
        var sec = -(TimeThiefDecreaseMeetingTime * Count);
        return sec;
    }
    public bool DoRevenge(CustomDeathReason deathReason) => modeNekokabocha && revengeOnExile && deathReason == CustomDeathReason.Vote;
    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (!Player.IsAlive()) return;

        RestartAbduct();
        UetukeUsed = !OptUetuke.GetBool();

        modeNone = false;
        modeVampire = false;
        modeEvilHacker = false;
        modeLimiter = false;
        modeNomal = false;
        modePuppeteer = false;
        modeStealth = false;
        modeRemotekiller = false;
        modeTimeThief = false;
        modeNotifier = false;
        modeTairo = false;
        modeMayor = false;
        modeMole = false;
        modeProgresskiller = false;
        modeNekokabocha = false;
        modeinsider = false;
        modepenguin = false;
        modeComebaker = false;

        int Count = RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer
                    + RateStealth + RateRemotekiller + RateNotifier + RateTimeThief
                    + RateTairo + RateMayor + RateMole + RateProgresskiller + RateNekokabocha + RateInsider
                    + RatePenguin + RateComebaker + RateNomal;
        int chance = IRandom.Instance.Next(1, Count);
        //ランダム
        ChengeMode(chance);
    }
    #endregion
    #region Kill
    public bool IsKiller => AbductVictim == null;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (modeLimiter)//爆弾最優先
        {
            var Targets = new List<PlayerControl>(PlayerCatch.AllAlivePlayerControls);
            info.DoKill = false;
            foreach (var bomtarget in Targets)
            {
                var distance = Vector3.Distance(Player.transform.position, bomtarget.transform.position);
                if (distance > Limiterblastrange) continue;
                if (bomtarget.PlayerId == Player.PlayerId)
                {
                    PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Bombed;
                    Player.RpcMurderPlayer(Player);
                    continue;
                }
                if (CustomRoleManager.OnCheckMurder(Player, bomtarget, bomtarget, bomtarget, true, true, 2))
                {
                    PlayerState.GetByPlayerId(bomtarget.PlayerId).DeathReason = CustomDeathReason.Bombed;
                    bomtarget.SetRealKiller(bomtarget);
                }
            }
            return;
        }
        if (AbductVictim != null)//会議明けでのペングインを考えてこっち優先。
        {
            if (target != AbductVictim)
            {
                //拉致中は拉致相手しか切れない
                Player.RpcMurderPlayer(AbductVictim);
                Player.ResetKillCooldown();
                info.DoKill = false;
            }
            RemoveVictim();
            return;
        }
        if (modeRemotekiller || modeVampire)
        {
            if (!info.CanKill) return;
            if (target.Is(CustomRoles.Bait)) return;
            if (target.Is(CustomRoles.InSender)) return;
            if (info.IsFakeSuicide) return;
            if (info.CheckHasGuard())
            {
                info.IsGuard = true;
                return;
            }
            info.DoKill = false;
            if (modeRemotekiller)
            {
                Remotekillertarget = target.PlayerId;
                killer.SetKillCooldown(target: target);
                return;
            }
            else
            {
                if (!BittenPlayers.ContainsKey(target.PlayerId))
                {
                    killer.SetKillCooldown();
                    BittenPlayers.Add(target.PlayerId, 0f);
                }
                return;
            }
        }
        if (modePuppeteer)
        {
            Puppets[target.PlayerId] = this;
            PuppetCooltime[target.PlayerId] = 0;
            SendRPC(target.PlayerId, 1);
            killer.SetKillCooldown();
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: killer);
            info.DoKill = false;
            return;
        }
        if (modeStealth)
        {
            if (!info.CanKill || !info.DoKill || info.IsSuicide || info.IsAccident || info.IsFakeSuicide) return;
            IEnumerable<PlayerControl> playersToDarken = null;
            {
                var room = info.AttemptKiller.GetPlainShipRoom();
                if (room != null)
                {
                    var roomArea = room.roomArea;
                    var roomName = room.RoomId;
                    RpcDarken(roomName);
                    playersToDarken = PlayerCatch.AllAlivePlayerControls.Where(player => player != Player && player.Collider.IsTouching(roomArea));
                }
            }
            if (playersToDarken == null)
            {
                Logger.Info("部屋の当たり判定を取得できないため暗転を行いません", "Alien.S");
                return;
            }
            playersToDarken = playersToDarken.Where(player => !player.Is(CustomRoleTypes.Impostor));
            {
                darkenedPlayers = playersToDarken.ToArray();
                foreach (var player in playersToDarken)
                {
                    PlayerState.GetByPlayerId(player.PlayerId).IsBlackOut = true;
                    player.MarkDirtySettings();
                }
            }
            return;
        }
        if (modeNotifier)
        {
            if (!info.IsSuicide)
                if (IRandom.Instance.Next(1, 101) <= NotifierCance)
                    Utils.AllPlayerKillFlash();
            return;
        }
        if (modeTimeThief)//タイムシーフはタイムシーフモード中じゃないと会議時間を減らさない。
        {
            if (!info.IsSuicide && info.CanKill && info.DoKill)
                Count++;//キルが成功したらカウントを1増やす。
            return;
        }
        if (modeinsider)
        {
            if (!info.IsSuicide && info.CanKill && info.DoKill)
                InsiderCansee.Add(info.AttemptTarget.PlayerId);
            return;
        }
        if (modepenguin)//拉致中処理は上でしてる。
        {
            info.DoKill = false;
            PlayerState.GetByPlayerId(target.PlayerId).CanUseMovingPlatform = MyState.CanUseMovingPlatform = false;
            CheckMurderPatch.TimeSinceLastKill[killer.PlayerId] = 0f;
            AbductVictim = target;
            AbductTimer = AbductTimerLimit;
            Player.SyncSettings();
            Player.RpcResetAbilityCooldown();
            using var sender = CreateSender();

            sender.Writer.Write(AbductVictim?.PlayerId ?? 255);
            return;
        }
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        if (modeNekokabocha)
        {
            // 普通のキルじゃない．もしくはキルを行わない時はreturn
            if (GameStates.IsMeeting || info.IsAccident || info.IsSuicide || !info.CanKill || !info.DoKill) return;
            // 殺してきた人を殺し返す
            if (!GameStates.CalledMeeting && MyState.DeathReason is CustomDeathReason.Revenge) return;
            Logger.Info("ネコカボチャの仕返し", "Alien");
            var killer = info.AttemptKiller;
            if (!IsCandidate(killer))
            {
                Logger.Info("キラーは仕返し対象ではないので仕返しされません", "Alien");
                return;
            }
            if (CustomRoleManager.OnCheckMurder(Player, killer, Player, killer, true, false))
            {
                PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Revenge;
            }
        }
    }
    public bool IsCandidate(PlayerControl player)
    {
        return player.GetCustomRole().GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => impostorsGetRevenged,
            CustomRoleTypes.Madmate => madmatesGetRevenged,
            CustomRoleTypes.Neutral => NeutralsGetRevenged,
            _ => true,
        };
    }
    public bool CheckSheriffKill(PlayerControl target)
    {
        if (target == Player) return modeTairo;
        return false;
    }
    void KillBitten(PlayerControl target, bool isButton = false)
    {
        if (target == null) return;
        var vampire = Player;
        if (target.IsAlive())
        {
            if (CustomRoleManager.OnCheckMurder(vampire, target, target, target, true, Killpower: 1))
            {
                PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Bite;
                target.SetRealKiller(vampire);
                Logger.Info($"Alienに噛まれている{target.name}を自爆させました。", "Alien.Va");
                if (!isButton && vampire.IsAlive())
                    RPC.PlaySoundRPC(vampire.PlayerId, Sounds.KillSound);
            }
            else Logger.Info($"Alienに噛まれた{target.name}にキルが通りませんでした。", "Alien.Va");
        }
        else Logger.Info($"Alienに噛まれている{target.name}はすでに死んでいました。", "Alien.Va");
    }
    #endregion
    #region FixUpdata
    static int state = 0;
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (modeVampire)
        {
            if (!GameStates.IsInTask) return;

            foreach (var (targetId, timer) in BittenPlayers.ToArray())
            {
                if (timer >= VampireKillDelay)
                {
                    var target = PlayerCatch.GetPlayerById(targetId);
                    KillBitten(target);
                    BittenPlayers.Remove(targetId);
                }
                else
                {
                    BittenPlayers[targetId] += Time.fixedDeltaTime;

                    if (SpeedDown.GetBool() && timer >= Spped)
                    {
                        var target = PlayerCatch.GetPlayerById(targetId);
                        if (target.IsAlive())
                        {
                            var x = VampireKillDelay - Spped;
                            float Swariai = (VampireKillDelay - Spped - (timer - Spped)) / x;
                            float Sp = tmpSpeed * Swariai;

                            if (VampireKillDelay - timer <= 0.5f) Sp = Main.MinSpeed;//これは残り0,5sになったら静止させてｳｸﾞｯ...ｺｺﾏﾃﾞｶｯ...ってするやつ。

                            if (Sp >= Main.MinSpeed && Sp < tmpSpeed)
                            {
                                Main.AllPlayerSpeed[target.PlayerId] = Sp;
                                target.MarkDirtySettings();
                            }
                        }
                    }
                }
            }
            return;
        }
        if (modeStealth)
        {
            if (darkenedPlayers != null)
            {
                darkenTimer -= Time.fixedDeltaTime;
                if (darkenTimer <= 0) ResetDarkenState();
            }
            return;
        }
        //会議でキルを通さなかった時があるため..
        //if (modepenguin)
        {
            if (!GameStates.IsInTask) return;
            if (!stopCount)
                AbductTimer -= Time.fixedDeltaTime;

            if (AbductVictim != null)
            {
                if (!Player.IsAlive() || !AbductVictim.IsAlive())
                {
                    RemoveVictim();
                    return;
                }
                if (AbductTimer <= 0f && !Player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                {
                    // 先にIsDeadをtrueにする(はしごチェイス封じ)
                    AbductVictim.Data.IsDead = true;
                    GameData.Instance.DirtyAllData();
                    // ペンギン自身がはしご上にいる場合，はしごを降りてからキルする
                    if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                    {
                        var abductVictim = AbductVictim;
                        _ = new LateTask(() =>
                        {
                            var sId = abductVictim.NetTransform.lastSequenceId + 5;
                            abductVictim.NetTransform.SnapTo(Player.transform.position, (ushort)sId);
                            Player.MurderPlayer(abductVictim);

                            var sender = CustomRpcSender.Create("PenguinMurder");
                            {
                                sender.AutoStartRpc(abductVictim.NetTransform.NetId, (byte)RpcCalls.SnapTo);
                                {
                                    NetHelpers.WriteVector2(Player.transform.position, sender.stream);
                                    sender.Write(abductVictim.NetTransform.lastSequenceId);
                                }
                                sender.EndRpc();
                                sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.MurderPlayer);
                                {
                                    sender.WriteNetObject(abductVictim);
                                    sender.Write((int)ExtendedPlayerControl.SucceededFlags);
                                }
                                sender.EndRpc();
                            }
                            sender.SendMessage();
                        }, 0.3f, "PenguinMurder");
                        RemoveVictim();
                    }
                }
                // はしごの上にいるプレイヤーにはSnapToRPCが効かずホストだけ挙動が変わるため，一律でテレポートを行わない
                else if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                {
                    int div = 3;
                    state++;
                    if (state % div == 0)
                    {
                        var position = Player.transform.position;
                        if (Player.PlayerId != 0)
                        {
                            AbductVictim.RpcSnapToForced(position, SendOption.None);
                        }
                        else
                        {
                            _ = new LateTask(() =>
                            {
                                if (AbductVictim != null)
                                {
                                    AbductVictim.RpcSnapToForced(position, SendOption.None);
                                }
                            }
                            , 0.25f, "", true);
                        }
                    }
                }
            }
            else if (AbductTimer <= 100f)
            {
                AbductTimer = 255f;
                Player.RpcResetAbilityCooldown();
            }
        }
    }
    public void OnFixedUpdateOthers(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!modePuppeteer) return;

        if (Puppets.TryGetValue(player.PlayerId, out var puppeteer))
        {
            var puppet = player;

            if (PuppetCooltime.TryGetValue(puppet.PlayerId, out float pu))
            {
                PuppetCooltime[puppet.PlayerId] += Time.fixedDeltaTime;
            }
            else PuppetCooltime.Add(puppet.PlayerId, 0);

            if (pu < PuppetCool.GetFloat()) return;
            if (!puppet.IsAlive())
            {
                Puppets.Remove(puppet.PlayerId);
                SendRPC(puppet.PlayerId, 2);
            }
            else
            {
                var puppetPos = puppet.transform.position;//puppetの位置
                Dictionary<PlayerControl, float> targetDistance = new();
                foreach (var pc in PlayerCatch.AllAlivePlayerControls.ToArray())
                {
                    if (pc.PlayerId != puppet.PlayerId && !pc.Is(CountTypes.Impostor))
                    {
                        var dis = Vector2.Distance(puppetPos, pc.transform.position);
                        targetDistance.Add(pc, dis);
                    }
                }
                if (targetDistance.Keys.Count <= 0) return;

                var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                var target = min.Key;
                var KillRange = NormalGameOptionsV10.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
                if (min.Value <= KillRange && puppet.CanMove && target.CanMove)
                {
                    PuppetCooltime.Remove(puppet.PlayerId);
                    if (CustomRoleManager.OnCheckMurder(Player, target, puppet, target, true, false, 1))
                    {
                        RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                    }
                    UtilsOption.MarkEveryoneDirtySettings();
                    Puppets.Remove(puppet.PlayerId);
                    SendRPC(puppet.PlayerId, 2);
                }
            }
        }
    }
    #endregion
    #region Name
    public override string GetProgressText(bool comms = false, bool gamelog = false)
    {
        if (!Player.IsAlive() && !gamelog) return "";
        if (AlienHideAbility || GameStates.CalledMeeting || gamelog) return Mode(gamelog);

        return "";
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        seen ??= seer;
        if (modePuppeteer)
        {
            if (!(Puppets.ContainsValue(this) && Puppets.ContainsKey(seen.PlayerId))) return "";
            return Utils.ColorString(RoleInfo.RoleColor, "◆");
        }
        if (modeStealth)
        {
            if (seer != Player || seen != Player || !darkenedRoom.HasValue) return base.GetSuffix(seer, seen);
            return string.Format(GetString("StealthDarkened"), DestroyableSingleton<TranslationController>.Instance.GetString(darkenedRoom.Value));
        }
        if (modeProgresskiller)
        {
            if (ProgressKillerMadseen && seen.Is(CustomRoleTypes.Madmate) && seer.Is(CustomRoles.Alien) && seer != seen)
                if (seen.GetPlayerTaskState().IsTaskFinished) return Utils.ColorString(RoleInfo.RoleColor, "☆");
            return "";
        }
        return "";
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        foreach (var al in Aliens)
        {
            if (al.modeProgresskiller && al.Player == seen)
            {
                if (seer.Is(CustomRoles.Alien) && !seen.Is(CustomRoleTypes.Madmate) && seer != seen)
                {
                    if (seen.GetPlayerTaskState().IsTaskFinished)
                        return Utils.ColorString(RoleInfo.RoleColor, "〇");
                }
                return "";
            }
            if (al.Player != seer && seen == al.Player && !seer.IsAlive() && !AlienHideAbility && !GameStates.CalledMeeting && !MeetingStates.FirstMeeting)
            {
                return $"<size=50%>{al.Mode()}</size>";
            }
        }
        return "";
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        var text = "";
        if (isForMeeting)
        {
            if (!NameAddmin.TryGetValue(seen.PlayerId, out var Admin)) return "";
            text = "<color=#8cffff><size=1.5>" + Admin + "</color></size>";
        }
        if (seer != Player || seen != Player)
        {
            return text += base.GetSuffix(seer, seen, isForMeeting);
        }
        return text;
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        seen ??= Player;
        if (InsiderCansee.Count == 0) return;
        if (InsiderCansee.Contains(seen.PlayerId))
            enabled = true;
    }
    #endregion    
    #region Vent
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (Tp != new Vector2(999f, 999f) && modeComebaker)
        {
            var tp = Tp;
            _ = new LateTask(() =>
            {
                Player.RpcSnapToForced(tp + new Vector2(0f, 0.1f));
            }, 1f, "TP");
        }
        ShipStatus.Instance.AllVents.DoIf(vent => vent.Id == ventId, vent => Tp = (Vector2)vent.transform.position);

        if (modeRemotekiller)
        {
            var user = physics.myPlayer;
            if (Remotekillertarget != 111 && Player.PlayerId == user.PlayerId)
            {
                var target = PlayerCatch.GetPlayerById(Remotekillertarget);
                if (!target.IsAlive()) return true;
                _ = new LateTask(() =>
                {
                    target.SetRealKiller(user);
                    user.RpcMurderPlayer(target, true);
                }, 1f);
                RPC.PlaySoundRPC(user.PlayerId, Sounds.KillSound);
                Remotekillertarget = 111;
                return false;
            }
        }
        if (modeMole)//モグラ
        {
            _ = new LateTask(() =>
            {
                int chance = IRandom.Instance.Next(0, ShipStatus.Instance.AllVents.Count);
                Player.RpcSnapToForced((Vector2)ShipStatus.Instance.AllVents[chance].transform.position + new Vector2(0f, 0.1f));
            }, 0.7f, "TP");
        }
        return true;
    }
    public override bool CanVentMoving(PlayerPhysics physics, int ventId)
    {
        if (modeMole) return false;
        if (modeComebaker) return false;

        return true;
    }
    #endregion
    #region RPC
    private void SendRPC(byte targetId, byte typeId)
    {
        using var sender = CreateSender();

        sender.Writer.Write((byte)RPC_type.SyncPuppet);
        sender.Writer.Write(typeId);
        sender.Writer.Write(targetId);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        switch ((RPC_type)reader.ReadByte())
        {
            case RPC_type.StealthDarken:
                var roomId = reader.ReadByte();
                darkenedRoom = roomId == byte.MaxValue ? null : (SystemTypes)roomId;
                break;

            case RPC_type.SyncPuppet:
                var typeId = reader.ReadByte();
                var targetId = reader.ReadByte();
                switch (typeId)
                {
                    case 0: //Dictionaryのクリア
                        Puppets.Clear();
                        PuppetCooltime.Clear();
                        break;
                    case 1: //Dictionaryに追加
                        Puppets[targetId] = this;
                        PuppetCooltime[targetId] = 0;
                        break;
                    case 2: //DictionaryのKey削除
                        Puppets.Remove(targetId);
                        PuppetCooltime.Remove(targetId);
                        break;
                }
                break;
            case RPC_type.Penguin:
                var victim = reader.ReadByte();
                if (victim == 255)
                {
                    AbductVictim = null;
                    AbductTimer = 255f;
                }
                else
                {
                    AbductVictim = PlayerCatch.GetPlayerById(victim);
                    AbductTimer = AbductTimerLimit;
                }
                break;
        }
    }
    void RpcDarken(SystemTypes? roomType)
    {
        Logger.Info($"暗転させている部屋を{roomType?.ToString() ?? "null"}に設定", "Alien.S");
        darkenedRoom = roomType;
        using var sender = CreateSender();
        sender.Writer.Write((byte)RPC_type.StealthDarken);
        sender.Writer.Write((byte?)roomType ?? byte.MaxValue);
    }
    #endregion

    #region 植え付け

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (OptUetukeCount.GetInt() > Uetukecount && Is(voter) && !UetukeUsed && !modeNone && !modeNomal)
        {
            var target = PlayerCatch.GetPlayerById(votedForId);
            {
                if (CheckSelfVoteMode(Player, votedForId, out var status))
                {
                    if (status is VoteStatus.Self)
                        Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.AlienUetuke"), GetString("Vote.AlienUetuke")) + GetString("VoteSkillMode"), Player.PlayerId);
                    if (status is VoteStatus.Skip)
                        Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                    if (status is VoteStatus.Vote)
                        Uetuke(votedForId);
                    SetMode(Player, status is VoteStatus.Self);
                    return false;
                }
            }
        }
        return true;
    }
    void Uetuke(byte toid)
    {
        var pc = PlayerCatch.GetPlayerById(toid);
        if (pc.GetCustomRole().IsImpostor() && !pc.Is(CustomRoles.AlienHijack))
        {
            var Troleclass = pc.GetRoleClass();
            var role = pc.GetCustomRole();

            Uetukecount++;
            UetukeUsed = true;
            pc.RpcSetCustomRole(CustomRoles.AlienHijack, true, null);

            _ = new LateTask(() =>
            {
                if (pc.GetRoleClass() is AlienHijack alienHijack)
                {
                    alienHijack.modeVampire = modeVampire;
                    alienHijack.modeEvilHacker = modeEvilHacker;
                    alienHijack.modeLimiter = modeLimiter;
                    alienHijack.modePuppeteer = modePuppeteer;
                    alienHijack.modeStealth = modeStealth;
                    alienHijack.modeRemotekiller = modeRemotekiller;
                    alienHijack.modeNotifier = modeNotifier;
                    alienHijack.modeTimeThief = modeTimeThief;
                    alienHijack.modeTairo = modeTairo;
                    alienHijack.modeMayor = modeMayor;
                    alienHijack.modeProgresskiller = modeProgresskiller;
                    alienHijack.modeMole = modeMole;
                    alienHijack.modeNekokabocha = modeNekokabocha;
                    alienHijack.modeinsider = modeinsider;
                    alienHijack.modepenguin = modepenguin;
                    alienHijack.modeComebaker = modeComebaker;
                    alienHijack.MaenoRole = Troleclass;
                    alienHijack.MaenoCRole = role;
                }
            }, 4, "AlienUetule", true);
        }
    }

    #endregion
    #region Other
    public override bool NotifyRolesCheckOtherName => true;
    void ResetDarkenState()
    {
        if (darkenedPlayers != null)
        {
            foreach (var player in darkenedPlayers)
            {
                PlayerState.GetByPlayerId(player.PlayerId).IsBlackOut = false;
                player.MarkDirtySettings();
            }
            darkenedPlayers = null;
        }
        darkenTimer = StealthDarkenDuration;
        RpcDarken(null);
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }
    void RemoveVictim()
    {
        if (AbductVictim != null)
        {
            PlayerState.GetByPlayerId(AbductVictim.PlayerId).CanUseMovingPlatform = true;
            AbductVictim = null;
        }
        MyState.CanUseMovingPlatform = true;
        AbductTimer = 255f;
        Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
        using var sender = CreateSender();

        sender.Writer.Write(AbductVictim?.PlayerId ?? 255);
    }
    public void RestartAbduct()
    {
        if (AbductVictim != null)
        {
            stopCount = false;
            state = 0;
        }
    }
    public string Mode(bool gamelog = false)
    {
        if (!Player.IsAlive()) return "";
        var size = gamelog ? "<size=30%>" : "<size=75%>";

        if (modeNone) return size + "<color=#ff1919>mode:None</color></size>";
        if (modeVampire) return size + "<color=#ff1919>mode:" + GetString("Vampire") + "</color></size>";
        if (modeEvilHacker) return size + "<color=#ff1919>mode:" + GetString("EvilHacker") + "</color></size>";
        if (modeLimiter) return size + "<color=#ff1919>mode:" + GetString("Limiter") + "</color></size>";
        if (modePuppeteer) return size + "<color=#ff1919>mode:" + GetString("Puppeteer") + "</color></size>";
        if (modeStealth) return size + "<color=#ff1919>mode:" + GetString("Stealth") + "</color></size>";
        if (modeRemotekiller) return size + "<color=#8f00ce>mode:" + GetString("Remotekiller") + "</color></size>";
        if (modeNotifier) return size + "<color=#ff1919>mode:" + GetString("Notifier") + "</color></size>";
        if (modeTimeThief) return size + "<color=#ff1919>mode:" + GetString("TimeThief") + "</color></size>";
        if (modeTairo) return size + "<color=#ff1919>mode:" + GetString("Tairou") + "</color></size>";
        if (modeMayor) return size + "<color=#204d42>mode:" + GetString("Mayor") + "</color></size>";
        if (modeMole) return size + "<color=#ff1919>mode:" + GetString("Mole") + "</color></size>";
        if (modeProgresskiller) return size + "<color=#ff1919>mode:" + GetString("ProgressKiller") + "</color></size>";
        if (modeNekokabocha) return size + "<color=#ff1919>mode:" + GetString("NekoKabocha") + "</color></size>";
        if (modeinsider) return size + "<color=#ff1919>mode:" + GetString("Insider") + "</color></size>";
        if (modepenguin) return size + "<color=#ff1919>mode:" + GetString("Penguin") + "</color></size>";
        if (modeComebaker) return size + "<color=#ff9966>mode:" + GetString("Comebacker") + "</color></size>";
        if (modeNomal) return size + "<color=#ff1919>mode:Normal</color></size>";

        return size + "<color=#ff1919>mode:？</color></size>";
    }
    void ChengeMode(int chance)
    {
        if (chance <= RateVampire)
        {
            modeVampire = true;
            Logger.Info("Alienはヴァンパイアになりました。", "Alien");
        }
        else if (chance <= RateVampire + RateEvilHacker)
        {
            Logger.Info("Alienはイビルハッカーになりました。", "Alien");
            modeEvilHacker = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter)
        {
            Logger.Info("Alienはリミッターになりました。", "Alien");
            modeLimiter = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer)
        {
            Logger.Info("Alienはパペッティアになりました。", "Alien");
            modePuppeteer = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth)
        {
            Logger.Info("Alienはステルスになりました。", "Alien");
            modeStealth = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller)
        {
            Logger.Info("Alienはリモートキラーになりました。", "Alien");
            modeRemotekiller = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier)
        {
            Logger.Info("Alienはノーティファーになりました。", "Alien");
            modeNotifier = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief)
        {
            Logger.Info("Alienはタイムシーフになりました。", "Alien");
            modeTimeThief = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo)
        {
            Logger.Info("Alienは大狼になりました。", "Alien");
            modeTairo = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor)
        {
            Logger.Info("Alienはメイヤーになりました。", "Alien");
            modeMayor = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole)
        {
            Logger.Info("Alienはモグラになりました。", "Alien");
            modeMole = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole + RateProgresskiller)
        {
            Logger.Info("Alienはプログレスキラーになりました。", "Alien");
            modeProgresskiller = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole + RateProgresskiller + RateNekokabocha)
        {
            Logger.Info("Alienはネコカボチャになりました。", "Alien");
            modeNekokabocha = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole + RateProgresskiller + RateNekokabocha + RateInsider)
        {
            Logger.Info("Alienはインサイダーになりました。", "Alien");
            modeinsider = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole + RateProgresskiller + RateNekokabocha + RateInsider
        + RatePenguin)
        {
            Logger.Info("Alienはペングインになりました", "Alien");
            modepenguin = true;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole + RateProgresskiller + RateNekokabocha + RateInsider
        + RatePenguin + RateComebaker)
        {
            Logger.Info("Alienはカムバッカーになりました。", "Alien");
            modeComebaker = true;
        }
        else//どれにもあてはまらないならとりあえずノーマル
        {
            Logger.Info("ｴｰﾘｱﾝﾜﾀｼｴｰﾘｱﾝ", "Alien");
            modeNomal = true;
        }
    }
    void Init()
    {
        RateVampire = OptionModeVampire.GetInt();
        RateEvilHacker = OptionModeEvilHacker.GetInt();
        RateLimiter = OptionModeLimiter.GetInt();
        RateNomal = OptionModeNomal.GetInt();
        RatePuppeteer = OptionModePuppeteer.GetInt();
        RateStealth = OptionModeStealth.GetInt();
        RateRemotekiller = OptionModeRemotekiller.GetInt();
        RateNotifier = OptionModeNotifier.GetInt();
        RateTimeThief = OptionModeTimeThief.GetInt();
        RateTairo = OptionModeTairo.GetInt();
        RateMayor = OptionModeMayor.GetInt();
        RateProgresskiller = OptionModeProgresskiller.GetInt();
        RateMole = OptionModeMole.GetInt();
        RateNekokabocha = OptionModeNekokabocha.GetInt();
        RateInsider = OptionModeInsider.GetInt();
        RatePenguin = OptionModePenguin.GetInt();
        RateComebaker = OptionModeComebaker.GetInt();
        UetukeUsed = !OptUetuke.GetBool();

        TimeThiefDecreaseMeetingTime = OptionTimeThiefDecreaseMeetingTime.GetInt();
        NotifierCance = OptionNotifierProbability.GetInt();
        VampireKillDelay = OptionVampireKillDelay.GetFloat();
        AlienHideAbility = OptionAlienHideAbility.GetBool();
        Limiterblastrange = Optionblastrange.GetFloat();
        TimeThiefReturnStolenTimeUponDeath = OptionTimeThiefReturnStolenTimeUponDeath.GetBool();
        StealthDarkenDuration = OptionStealthDarkenDuration.GetInt();
        TairoDeathReason = OptionTairoDeathReason.GetBool();
        AdditionalVote = OptionAdditionalVote.GetInt();
        ProgressKillerMadseen = OptionProgressKillerMadseen.GetBool();
        ProgressWorkhorseseen = OptionProgressWorkhorseseen.GetBool();
        impostorsGetRevenged = optionImpostorsGetRevenged.GetBool();
        madmatesGetRevenged = optionMadmatesGetRevenged.GetBool();
        NeutralsGetRevenged = optionNeutralsGetRevenged.GetBool();
        revengeOnExile = optionRevengeOnExile.GetBool();
        Spped = SpeedDownCount.GetFloat();
        AbductTimerLimit = OptionAbductTimerLimit.GetFloat();
        MeetingKill = OptionMeetingKill.GetBool();
        Tp = new(999f, 999f);

        modeNone = true;
        modeVampire = false;
        modeEvilHacker = false;
        modeLimiter = false;
        modeNomal = false;
        modePuppeteer = false;
        modeStealth = false;
        modeRemotekiller = false;
        modeNotifier = false;
        modeTimeThief = false;
        modeTairo = false;
        modeMayor = false;
        modeProgresskiller = false;
        modeMole = false;
        modeNekokabocha = false;
        modeinsider = false;
        modepenguin = false;
        modeComebaker = false;
    }
    #region  Options
    public static HashSet<Alien> Aliens = new();
    //ヴァンパイア
    static OptionItem OptionModeVampire;
    static OptionItem OptionVampireKillDelay;
    public static OptionItem SpeedDown;
    static OptionItem SpeedDownCount;
    Dictionary<byte, float> BittenPlayers = new(14);
    public static float Spped;
    public static float tmpSpeed;
    public static float VampireKillDelay;
    public static int RateVampire;
    bool modeVampire;
    //イビルハッカー
    static OptionItem OptionModeEvilHacker;
    static int RateEvilHacker;
    bool modeEvilHacker;
    static Dictionary<byte, string> NameAddmin = new();
    //リミッター
    static OptionItem OptionModeLimiter;
    static OptionItem Optionblastrange;
    static int RateLimiter;
    public static float Limiterblastrange;
    bool modeLimiter;
    //ノーマル
    static OptionItem OptionModeNomal;
    static int RateNomal;
    bool modeNomal;
    //パペッティア
    public static OptionItem PuppetCool;
    static Dictionary<byte, float> PuppetCooltime = new(15);
    static Dictionary<byte, Alien> Puppets = new(15);
    static OptionItem OptionModePuppeteer;
    static int RatePuppeteer;
    bool modePuppeteer;
    //リモートキラー
    static OptionItem OptionModeRemotekiller;
    static int RateRemotekiller;
    bool modeRemotekiller;
    byte Remotekillertarget;
    //ステルス
    static OptionItem OptionModeStealth;
    static int RateStealth;
    bool modeStealth;
    static OptionItem OptionStealthDarkenDuration;
    public static float StealthDarkenDuration;
    public float darkenTimer;
    PlayerControl[] darkenedPlayers;
    SystemTypes? darkenedRoom = null;
    //ノーティファー
    static OptionItem OptionModeNotifier;
    static OptionItem OptionNotifierProbability;
    static int RateNotifier;
    bool modeNotifier;
    public static int NotifierCance;
    //タイムシーフ
    static OptionItem OptionModeTimeThief;
    static OptionItem OptionTimeThiefDecreaseMeetingTime;
    static int RateTimeThief;
    bool modeTimeThief;
    public static int TimeThiefDecreaseMeetingTime;
    static OptionItem OptionTimeThiefReturnStolenTimeUponDeath;
    public static bool TimeThiefReturnStolenTimeUponDeath;
    public bool RevertOnDie => TimeThiefReturnStolenTimeUponDeath;
    static int Count;
    //大狼
    static OptionItem OptionModeTairo;
    static OptionItem OptionTairoDeathReason;
    static int RateTairo;
    public bool modeTairo;
    public static bool TairoDeathReason;
    //メイヤー
    static OptionItem OptionModeMayor;
    static OptionItem OptionAdditionalVote;
    static int RateMayor;
    bool modeMayor;
    public static int AdditionalVote;
    //モグラ
    static OptionItem OptionModeMole;
    static int RateMole;
    bool modeMole;
    //プログレスキラー
    static OptionItem OptionModeProgresskiller;
    static OptionItem OptionProgressKillerMadseen;
    static OptionItem OptionProgressWorkhorseseen;
    static int RateProgresskiller;
    public bool modeProgresskiller;
    public static bool ProgressKillerMadseen;
    public static bool ProgressWorkhorseseen;
    //ネコカボチャ
    static OptionItem OptionModeNekokabocha;
    static BooleanOptionItem optionImpostorsGetRevenged;
    static BooleanOptionItem optionMadmatesGetRevenged;
    static BooleanOptionItem optionNeutralsGetRevenged;
    static BooleanOptionItem optionRevengeOnExile;
    static int RateNekokabocha;
    bool modeNekokabocha;
    public static bool impostorsGetRevenged;
    public static bool madmatesGetRevenged;
    public static bool NeutralsGetRevenged;
    public static bool revengeOnExile;
    //インサイダー
    static OptionItem OptionModeInsider;
    List<byte> InsiderCansee = new();
    static int RateInsider;
    bool modeinsider;
    //ペンギン
    static OptionItem OptionModePenguin;
    public static OptionItem OptionAbductTimerLimit;
    public static OptionItem OptionMeetingKill;
    PlayerControl AbductVictim;
    static int RatePenguin;
    bool modepenguin;
    public float AbductTimer;
    public static float AbductTimerLimit;
    public bool stopCount;
    public static bool MeetingKill;

    //カムバッカー
    static OptionItem OptionModeComebaker;
    static int RateComebaker;
    bool modeComebaker;
    private Vector2 Tp;

    //秘匿設定
    static OptionItem FirstAbility;
    static OptionItem OptionAlienHideAbility;
    static bool AlienHideAbility;
    bool modeNone;
    //植え付け
    static OptionItem OptUetuke;
    static OptionItem OptUetukeCount;
    public static OptionItem OptUetuketukeTrun;
    int Uetukecount;
    bool UetukeUsed;
    enum OptionName
    {
        AlienHideAbility, AlienFirstAbility,
        AlienCVampire, VampireKillDelay, VampireSpeedDownCount, VampireSpeedDown,
        AlienCEvilHacker,
        AlienCLimiter, blastrange,
        AlienCPuppeteer, PuppeteerPuppetCool,
        AlienCRemoteKiller,
        AlienCStealth, StealthDarkenDuration,
        AlienCNomal,
        AlienCNotifier, NotifierProbability,
        AlienCTimeThief, TimeThiefDecreaseMeetingTime, TimeThiefReturnStolenTimeUponDeath,
        AlienCTairo, TairoDeathReason,
        AlienCMayor, MayorAdditionalVote,
        AlienCMole,
        AlienCProgressKiller, ProgressKillerMadseen, ProgressWorkhorseseen,
        AlienCNekokabocha, NekoKabochaImpostorsGetRevenged, NekoKabochaMadmatesGetRevenged, NekoKabochaNeutralsGetRevenged, NekoKabochaRevengeOnExile,
        AlienCInsider,
        AlienCPenguin, PenguinAbductTimerLimit, PenguinMeetingKill,
        AlienCComebacker,
        AlienUetuke, AlienUetukeCount, AlienUetukeTrun
    }
    static void SetupOptionItem()//NowMax : 42
    {
        FirstAbility = BooleanOptionItem.Create(RoleInfo, 7, OptionName.AlienFirstAbility, false, false);
        OptionAlienHideAbility = BooleanOptionItem.Create(RoleInfo, 9, OptionName.AlienHideAbility, false, false);
        OptionModeVampire = FloatOptionItem.Create(RoleInfo, 10, OptionName.AlienCVampire, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        SpeedDown = BooleanOptionItem.Create(RoleInfo, 40, OptionName.VampireSpeedDown, true, false, OptionModeVampire);
        SpeedDownCount = FloatOptionItem.Create(RoleInfo, 41, OptionName.VampireSpeedDownCount, new(0f, 1000f, 1f), 10f, false, SpeedDown).SetValueFormat(OptionFormat.Seconds);
        OptionVampireKillDelay = FloatOptionItem.Create(RoleInfo, 11, OptionName.VampireKillDelay, new(0, 100, 0.2f), 10, false, OptionModeVampire).SetValueFormat(OptionFormat.Seconds);
        OptionModeEvilHacker = FloatOptionItem.Create(RoleInfo, 12, OptionName.AlienCEvilHacker, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionModeLimiter = FloatOptionItem.Create(RoleInfo, 13, OptionName.AlienCLimiter, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        Optionblastrange = FloatOptionItem.Create(RoleInfo, 14, OptionName.blastrange, new(0.5f, 20f, 0.5f), 5f, false, OptionModeLimiter);
        OptionModePuppeteer = FloatOptionItem.Create(RoleInfo, 15, OptionName.AlienCPuppeteer, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        PuppetCool = FloatOptionItem.Create(RoleInfo, 42, OptionName.PuppeteerPuppetCool, new(0, 100, 0.5f), 5f, false, OptionModePuppeteer).SetValueFormat(OptionFormat.Seconds);
        OptionModeStealth = FloatOptionItem.Create(RoleInfo, 18, OptionName.AlienCStealth, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionStealthDarkenDuration = FloatOptionItem.Create(RoleInfo, 19, OptionName.StealthDarkenDuration, new(0.5f, 5f, 0.5f), 1f, false, OptionModeStealth).SetValueFormat(OptionFormat.Seconds);
        OptionModeRemotekiller = FloatOptionItem.Create(RoleInfo, 20, OptionName.AlienCRemoteKiller, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionModeNotifier = FloatOptionItem.Create(RoleInfo, 21, OptionName.AlienCNotifier, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionNotifierProbability = FloatOptionItem.Create(RoleInfo, 22, OptionName.NotifierProbability, new(0, 100, 5), 50, false, OptionModeNotifier).SetValueFormat(OptionFormat.Percent);
        OptionModeTimeThief = FloatOptionItem.Create(RoleInfo, 23, OptionName.AlienCTimeThief, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionTimeThiefDecreaseMeetingTime = FloatOptionItem.Create(RoleInfo, 24, OptionName.TimeThiefDecreaseMeetingTime, new(0, 100, 5), 50, false, OptionModeTimeThief).SetValueFormat(OptionFormat.Seconds);
        OptionTimeThiefReturnStolenTimeUponDeath = BooleanOptionItem.Create(RoleInfo, 25, OptionName.TimeThiefReturnStolenTimeUponDeath, false, false, OptionModeTimeThief);
        OptionModeTairo = FloatOptionItem.Create(RoleInfo, 26, OptionName.AlienCTairo, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionTairoDeathReason = BooleanOptionItem.Create(RoleInfo, 27, OptionName.TairoDeathReason, false, false, OptionModeTairo);
        OptionModeMayor = FloatOptionItem.Create(RoleInfo, 28, OptionName.AlienCMayor, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionAdditionalVote = IntegerOptionItem.Create(RoleInfo, 29, OptionName.MayorAdditionalVote, new(1, 99, 1), 1, false, OptionModeMayor).SetValueFormat(OptionFormat.Votes);
        OptionModeMole = FloatOptionItem.Create(RoleInfo, 30, OptionName.AlienCMole, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionModeProgresskiller = FloatOptionItem.Create(RoleInfo, 31, OptionName.AlienCProgressKiller, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionProgressKillerMadseen = BooleanOptionItem.Create(RoleInfo, 32, OptionName.ProgressKillerMadseen, false, false, OptionModeProgresskiller);
        OptionProgressWorkhorseseen = BooleanOptionItem.Create(RoleInfo, 33, OptionName.ProgressWorkhorseseen, false, false, OptionModeProgresskiller);
        OptionModeNekokabocha = FloatOptionItem.Create(RoleInfo, 34, OptionName.AlienCNekokabocha, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        optionImpostorsGetRevenged = BooleanOptionItem.Create(RoleInfo, 35, OptionName.NekoKabochaImpostorsGetRevenged, false, false, OptionModeNekokabocha);
        optionMadmatesGetRevenged = BooleanOptionItem.Create(RoleInfo, 36, OptionName.NekoKabochaMadmatesGetRevenged, false, false, OptionModeNekokabocha);
        optionNeutralsGetRevenged = BooleanOptionItem.Create(RoleInfo, 37, OptionName.NekoKabochaNeutralsGetRevenged, false, false, OptionModeNekokabocha);
        optionRevengeOnExile = BooleanOptionItem.Create(RoleInfo, 38, OptionName.NekoKabochaRevengeOnExile, false, false, OptionModeNekokabocha);
        OptionModeInsider = FloatOptionItem.Create(RoleInfo, 39, OptionName.AlienCInsider, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionModePenguin = FloatOptionItem.Create(RoleInfo, 43, OptionName.AlienCPenguin, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionAbductTimerLimit = FloatOptionItem.Create(RoleInfo, 44, OptionName.PenguinAbductTimerLimit, new(5f, 100f, 1f), 10f, false, OptionModePenguin).SetValueFormat(OptionFormat.Seconds);
        OptionMeetingKill = BooleanOptionItem.Create(RoleInfo, 45, OptionName.PenguinMeetingKill, false, false, OptionModePenguin);
        OptionModeComebaker = FloatOptionItem.Create(RoleInfo, 49, OptionName.AlienCComebacker, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionModeNomal = FloatOptionItem.Create(RoleInfo, 8, OptionName.AlienCNomal, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptUetuke = BooleanOptionItem.Create(RoleInfo, 46, OptionName.AlienUetuke, false, false);
        OptUetukeCount = IntegerOptionItem.Create(RoleInfo, 47, OptionName.AlienUetukeCount, new(1, 20, 1), 1, false, OptUetuke);
        OptUetuketukeTrun = IntegerOptionItem.Create(RoleInfo, 48, OptionName.AlienUetukeTrun, new(1, 20, 1), 2, false, OptUetuke);
    }
    #endregion
    #endregion
}