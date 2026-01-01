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
using static TownOfHost.Roles.Impostor.Alien;

namespace TownOfHost.Roles.Neutral;

public sealed class JackalAlien : RoleBase, IMeetingTimeAlterable, ILNKiller, ISchrodingerCatOwner, INekomata, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(JackalAlien),
                player => new JackalAlien(player),
                CustomRoles.JackalAlien,
                () => OptionCanMakeSidekick.GetBool() ? RoleTypes.Phantom : RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                13000,
                SetupOptionItem,
                "JA",
                "#00b4eb",
                (1, 1),
                true,
                countType: CountTypes.Jackal,
                assignInfo: new RoleAssignInfo(CustomRoles.JackalAlien, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                },
                introSound: () => GetIntroSound(RoleTypes.Phantom),
                Desc: () =>
                {
                    return GetString("JackalAlienInfoLong") + (OptionCanMakeSidekick.GetBool() ? string.Format(GetString("JackalDescSidekick"), !OptionImpostorCanSidekick.GetBool() ? GetString("JackalDescImpostorSideKick") : "") : "");
                }
            );
    public JackalAlien(PlayerControl player)
: base(
    RoleInfo,
    player,
    () => HasTask.False
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
        AbductTimer = 255f;
        stopCount = false;
        CanSideKick = OptionCanMakeSidekick.GetBool();
        Init();
        PuppetCooltime.Clear();
        BittenPlayers.Clear();
        tmpSpeed = Main.NormalOptions.PlayerSpeedMod;
        Count = 0;
        Remotekillertarget = 111;
        InsiderCansee.Clear();
        NameAddmin.Clear();
        Aliens.Add(this);
        IsDead = false;
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
        Penguin,
        SideKick,
        SyncAlienMode
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

        if (mode == AlienMode.EvilHacker)
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
                if (entry.TotalPlayers <= 0) continue;
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
    public override CustomRoles TellResults(PlayerControl player) => mode == AlienMode.Tairo ? CustomRoles.Crewmate : CustomRoles.JackalAlien;
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);

        if (Options.firstturnmeeting && Options.FirstTurnMeetingCantability.GetBool() && MeetingStates.FirstMeeting) return (votedForId, numVotes, doVote);
        if (voterId == Player.PlayerId && mode == AlienMode.Mayor)
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
    public bool DoRevenge(CustomDeathReason deathReason) => mode == AlienMode.NekoKabocha && revengeOnExile && deathReason == CustomDeathReason.Vote;
    public override void AfterMeetingTasks()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (!Player.IsAlive()) return;

        RestartAbduct();

        if (!AmongUsClient.Instance.AmHost) return;

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
        if (mode == AlienMode.Limiter)//爆弾最優先
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
                if (CustomRoleManager.OnCheckMurder(Player, bomtarget, bomtarget, bomtarget, true, true, 2, CustomDeathReason.Bombed))
                {
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
        if (mode is AlienMode.RemoteKiller or AlienMode.Vampire)
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
            if (mode == AlienMode.RemoteKiller)
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
        if (mode == AlienMode.Puppeteer)
        {
            Puppets[target.PlayerId] = this;
            PuppetCooltime[target.PlayerId] = 0;
            SendRPC(target.PlayerId, 1);
            killer.SetKillCooldown();
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: killer);
            info.DoKill = false;
            return;
        }
        if (mode == AlienMode.Stealth)
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
        if (mode == AlienMode.Notifier)
        {
            if (!info.IsSuicide)
                if (IRandom.Instance.Next(1, 101) <= NotifierCance)
                    Utils.AllPlayerKillFlash();
            return;
        }
        if (mode == AlienMode.TimeThief)//タイムシーフはタイムシーフモード中じゃないと会議時間を減らさない。
        {
            if (!info.IsSuicide && info.CanKill && info.DoKill)
                Count++;//キルが成功したらカウントを1増やす。
            return;
        }
        if (mode == AlienMode.Insider)
        {
            if (!info.IsSuicide && info.CanKill && info.DoKill)
                InsiderCansee.Add(info.AttemptTarget.PlayerId);
            return;
        }
        if (mode == AlienMode.Penguin)//拉致中処理は上でしてる。
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
        if (mode == AlienMode.NekoKabocha)
        {
            // 普通のキルじゃない．もしくはキルを行わない時はreturn
            if (GameStates.IsMeeting || info.IsAccident || info.IsSuicide || !info.CanKill || !info.DoKill || IsDead) return;
            // 殺してきた人を殺し返す
            if (!GameStates.CalledMeeting && MyState.DeathReason is CustomDeathReason.Revenge) return;
            Logger.Info("ネコカボチャの仕返し", "JJAlien");
            var killer = info.AttemptKiller;
            if (!IsCandidate(killer))
            {
                Logger.Info("キラーは仕返し対象ではないので仕返しされません", "JAlien");
                return;
            }
            IsDead = true;
            CustomRoleManager.OnCheckMurder(Player, killer, Player, killer, true, false, deathReason: CustomDeathReason.Revenge);
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
        if (target == Player) return mode == AlienMode.Tairo;
        return false;
    }
    void KillBitten(PlayerControl target, bool isButton = false)
    {
        if (target == null) return;
        var vampire = Player;
        if (target.IsAlive())
        {
            if (CustomRoleManager.OnCheckMurder(vampire, target, target, target, true, Killpower: 1, deathReason: CustomDeathReason.Bite))
            {
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
        if (mode == AlienMode.Vampire)
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
        if (mode == AlienMode.Stealth)
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
        if (mode != AlienMode.Puppeteer) return;

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
                    if (pc.PlayerId != puppet.PlayerId && !pc.Is(CountTypes.Jackal))
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
        if (mode == AlienMode.Puppeteer)
        {
            if (!(Puppets.ContainsValue(this) && Puppets.ContainsKey(seen.PlayerId))) return "";
            return Utils.ColorString(RoleInfo.RoleColor, "◆");
        }
        if (mode == AlienMode.Stealth)
        {
            if (seer != Player || seen != Player || !darkenedRoom.HasValue) return base.GetSuffix(seer, seen);
            return string.Format(GetString("StealthDarkened"), DestroyableSingleton<TranslationController>.Instance.GetString(darkenedRoom.Value));
        }
        if (mode == AlienMode.ProgressKiller)
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
            if (al.mode == AlienMode.ProgressKiller && al.Player == seen)
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
        addon = false;

        if ((seen.Is(CustomRoles.Jackal) || seen.Is(CustomRoles.JackalMafia) || seen.Is(CustomRoles.JackalAlien)) && OptionJJackalMafiaCanAlsoBeExposedToJackal.GetBool())
            enabled = true;

        if (InsiderCansee.Count == 0) return;
        if (InsiderCansee.Contains(seen.PlayerId))
            enabled = true;
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        addon = false;
        if ((seen.Is(CustomRoles.Jackal) || seen.Is(CustomRoles.JackalMafia) || seen.Is(CustomRoles.JackalAlien)) && OptionJackalCanAlsoBeExposedToJMafia.GetBool())
            enabled = true;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !Player.IsAlive() || JackalDoll.GetSideKickCount() <= JackalDoll.NowSideKickCount || !CanSideKick) return "";

        if (isForHud) return GetString("PhantomButtonSideKick");
        return $"<size=50%>{GetString("PhantomButtonSideKick")}</size>";
    }
    #endregion
    #region Vent
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (Tp != new Vector2(999f, 999f) && mode == AlienMode.Comebacker)
        {
            var tp = Tp;
            _ = new LateTask(() =>
            {
                Player.RpcSnapToForced(tp + new Vector2(0f, 0.1f));
            }, 1f, "TP");
        }
        ShipStatus.Instance.AllVents.DoIf(vent => vent.Id == ventId, vent => Tp = (Vector2)vent.transform.position);

        if (mode == AlienMode.RemoteKiller)
        {
            var user = physics.myPlayer;
            if (Remotekillertarget is not 111 && Player.PlayerId == user.PlayerId)
            {
                var target = PlayerCatch.GetPlayerById(Remotekillertarget);
                if (!target.IsAlive()) return true;
                if (OptionKillAnimation.GetBool())
                {
                    _ = new LateTask(() =>
                    {
                        target.SetRealKiller(user);
                        user.RpcMurderPlayer(target, true);
                    }, 1.2f);
                }
                else
                {
                    target.SetRealKiller(user);
                    target.RpcMurderPlayer(target, true);
                }

                RPC.PlaySoundRPC(user.PlayerId, Sounds.KillSound);
                RPC.PlaySoundRPC(user.PlayerId, Sounds.TaskComplete);
                Logger.Info($"Remotekillerのターゲット{target.name}のキルに成功", "Remotekiller.kill");
                Remotekillertarget = byte.MaxValue;
                return !OptionKillAnimation.GetBool();
            }
        }
        if (mode == AlienMode.Mole)//モグラ
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
        if (mode == AlienMode.Mole) return false;
        if (mode == AlienMode.Comebacker) return false;

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
    private void SendSideKickState()
    {
        using var sender = CreateSender();

        sender.Writer.Write((byte)RPC_type.SideKick);
        sender.Writer.Write(CanSideKick);
    }
    private void RpcSyncAlienMode()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();

        sender.Writer.Write((byte)RPC_type.SyncAlienMode);
        sender.Writer.Write((int)mode);
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
            case RPC_type.SideKick:
                CanSideKick = reader.ReadBoolean();
                break;
            case RPC_type.SyncAlienMode:
                mode = (AlienMode)reader.ReadInt32();
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
    #region Jackal

    public ISchrodingerCatOwner.TeamType SchrodingerCatChangeTo => ISchrodingerCatOwner.TeamType.Jackal;
    public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();
    public bool CanUseSabotageButton() => OptionCanUseSabotage.GetBool();
    public override bool OnInvokeSabotage(SystemTypes systemType) => OptionCanUseSabotage.GetBool();
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(OptionHasImpostorVision.GetBool());
        AURoleOptions.PhantomCooldown = JackalDoll.GetSideKickCount() <= JackalDoll.NowSideKickCount ? 200f : OptionCooldown.GetFloat();
    }
    public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
    public bool UseOneclickButton => CanSideKick;
    public override bool CanUseAbilityButton() => CanSideKick;
    bool IUsePhantomButton.IsPhantomRole => JackalDoll.GetSideKickCount() > JackalDoll.NowSideKickCount;

    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = true;
        if (!CanSideKick) return;

        if (JackalDoll.GetSideKickCount() <= JackalDoll.NowSideKickCount)
        {
            CanSideKick = false;
            SendSideKickState();
            return;
        }
        var target = Player.GetKillTarget(true);

        if (target == null)
        {
            ResetCooldown = false;
            return;
        }
        var targetrole = target.GetCustomRole();
        if (target == null || (targetrole is CustomRoles.King or CustomRoles.Jackal or CustomRoles.JackalAlien or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.Merlin) || ((targetrole.IsImpostor() || targetrole is CustomRoles.Egoist) && !OptionImpostorCanSidekick.GetBool()))
        {
            ResetCooldown = false;
            return;
        }
        if (SuddenDeathMode.NowSuddenDeathTemeMode)
        {
            target.SideKickChangeTeam(Player);
        }
        CanSideKick = false;
        SendSideKickState();
        Player.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(Player);
        target.RpcProtectedMurderPlayer(target);
        UtilsGameLog.AddGameLog($"SideKick", string.Format(GetString("log.Sidekick"), UtilsName.GetPlayerColor(target, true) + $"({UtilsRoleText.GetTrueRoleName(target.PlayerId)})", UtilsName.GetPlayerColor(Player, true)));
        target.RpcSetCustomRole(CustomRoles.Jackaldoll);
        JackalDoll.Sidekick(target, Player);
        if (!Utils.RoleSendList.Contains(target.PlayerId)) Utils.RoleSendList.Add(target.PlayerId);
        UtilsOption.MarkEveryoneDirtySettings();
        UtilsGameLog.LastLogRole[target.PlayerId] += "<b>⇒" + Utils.ColorString(UtilsRoleText.GetRoleColor(target.GetCustomRole()), GetString($"{target.GetCustomRole()}")) + "</b>";
    }

    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        if (killer.Is(CountTypes.Jackal) && !OptionJJackalCanKillMafia.GetBool())
        {
            info.DoKill = false;
            killer.SetKillCooldown();
            return false;
        }
        return true;
    }
    public override string GetAbilityButtonText() => GetString("Sidekick");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "SideKick";
        return true;
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
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
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

        if (mode == AlienMode.None) return size + "<color=#ff1919>mode:None</color></size>";
        if (mode == AlienMode.Vampire) return size + "<color=#ff1919>mode:" + GetString("Vampire") + "</color></size>";
        if (mode == AlienMode.EvilHacker) return size + "<color=#ff1919>mode:" + GetString("EvilHacker") + "</color></size>";
        if (mode == AlienMode.Limiter) return size + "<color=#ff1919>mode:" + GetString("Limiter") + "</color></size>";
        if (mode == AlienMode.Puppeteer) return size + "<color=#ff1919>mode:" + GetString("Puppeteer") + "</color></size>";
        if (mode == AlienMode.Stealth) return size + "<color=#ff1919>mode:" + GetString("Stealth") + "</color></size>";
        if (mode == AlienMode.RemoteKiller) return size + "<color=#8f00ce>mode:" + GetString("Remotekiller") + "</color></size>";
        if (mode == AlienMode.Notifier) return size + "<color=#ff1919>mode:" + GetString("Notifier") + "</color></size>";
        if (mode == AlienMode.TimeThief) return size + "<color=#ff1919>mode:" + GetString("TimeThief") + "</color></size>";
        if (mode == AlienMode.Tairo) return size + "<color=#ff1919>mode:" + GetString("Tairou") + "</color></size>";
        if (mode == AlienMode.Mayor) return size + "<color=#204d42>mode:" + GetString("Mayor") + "</color></size>";
        if (mode == AlienMode.Mole) return size + "<color=#ff1919>mode:" + GetString("Mole") + "</color></size>";
        if (mode == AlienMode.ProgressKiller) return size + "<color=#ff1919>mode:" + GetString("ProgressKiller") + "</color></size>";
        if (mode == AlienMode.NekoKabocha) return size + "<color=#ff1919>mode:" + GetString("NekoKabocha") + "</color></size>";
        if (mode == AlienMode.Insider) return size + "<color=#ff1919>mode:" + GetString("Insider") + "</color></size>";
        if (mode == AlienMode.Penguin) return size + "<color=#ff1919>mode:" + GetString("Penguin") + "</color></size>";
        if (mode == AlienMode.Comebacker) return size + "<color=#ff9966>mode:" + GetString("Comebacker") + "</color></size>";
        if (mode == AlienMode.Normal) return size + "<color=#ff1919>mode:Normal</color></size>";

        return size + "<color=#ff1919>mode:？</color></size>";
    }
    void ChengeMode(int chance)
    {
        if (chance <= RateVampire)
        {
            mode = AlienMode.Vampire;
            Logger.Info("Alienはヴァンパイアになりました。", "JAlien");
        }
        else if (chance <= RateVampire + RateEvilHacker)
        {
            Logger.Info("Alienはイビルハッカーになりました。", "JAlien");
            mode = AlienMode.EvilHacker;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter)
        {
            Logger.Info("Alienはリミッターになりました。", "JAlien");
            mode = AlienMode.Limiter;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer)
        {
            Logger.Info("Alienはパペッティアになりました。", "JAlien");
            mode = AlienMode.Puppeteer;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth)
        {
            Logger.Info("Alienはステルスになりました。", "JAlien");
            mode = AlienMode.Stealth;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller)
        {
            Logger.Info("Alienはリモートキラーになりました。", "JAlien");
            mode = AlienMode.RemoteKiller;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier)
        {
            Logger.Info("Alienはノーティファーになりました。", "JAlien");
            mode = AlienMode.Notifier;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief)
        {
            Logger.Info("Alienはタイムシーフになりました。", "JAlien");
            mode = AlienMode.TimeThief;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo)
        {
            Logger.Info("Alienは大狼になりました。", "JAlien");
            mode = AlienMode.Tairo;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor)
        {
            Logger.Info("Alienはメイヤーになりました。", "JAlien");
            mode = AlienMode.Mayor;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole)
        {
            Logger.Info("Alienはモグラになりました。", "JAlien");
            mode = AlienMode.Mole;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole + RateProgresskiller)
        {
            Logger.Info("Alienはプログレスキラーになりました。", "JAlien");
            mode = AlienMode.ProgressKiller;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole + RateProgresskiller + RateNekokabocha)
        {
            Logger.Info("Alienはネコカボチャになりました。", "JAlien");
            mode = AlienMode.NekoKabocha;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole + RateProgresskiller + RateNekokabocha + RateInsider)
        {
            Logger.Info("Alienはインサイダーになりました。", "JAlien");
            mode = AlienMode.Insider;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole + RateProgresskiller + RateNekokabocha + RateInsider
        + RatePenguin)
        {
            Logger.Info("Alienはペングインになりました", "JAlien");
            mode = AlienMode.Penguin;
        }
        else if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole + RateProgresskiller + RateNekokabocha + RateInsider
        + RatePenguin + RateComebaker)
        {
            Logger.Info("Alienはカムバッカーになりました。", "JAlien");
            mode = AlienMode.Comebacker;
        }
        else//どれにもあてはまらないならとりあえずノーマル
        {
            Logger.Info("ｴｰﾘｱﾝﾜﾀｼｴｰﾘｱﾝ", "JAlien");
            mode = AlienMode.Normal;
        }
        RpcSyncAlienMode();
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

        mode = AlienMode.None;
    }
    #region  Options
    public static HashSet<JackalAlien> Aliens = new();
    //ヴァンパイア
    static OptionItem OptionModeVampire;
    static OptionItem OptionVampireKillDelay;
    static OptionItem SpeedDown;
    static OptionItem SpeedDownCount;
    Dictionary<byte, float> BittenPlayers = new(14);
    static float Spped;
    static float tmpSpeed;
    static float VampireKillDelay;
    static int RateVampire;
    //イビルハッカー
    static OptionItem OptionModeEvilHacker;
    static int RateEvilHacker;
    static Dictionary<byte, string> NameAddmin = new();
    //リミッター
    static OptionItem OptionModeLimiter;
    static OptionItem Optionblastrange;
    static int RateLimiter;
    static float Limiterblastrange;
    //ノーマル
    static OptionItem OptionModeNomal;
    static int RateNomal;
    //パペッティア
    static OptionItem PuppetCool;
    static Dictionary<byte, float> PuppetCooltime = new(15);
    static Dictionary<byte, JackalAlien> Puppets = new(15);
    static OptionItem OptionModePuppeteer;
    static int RatePuppeteer;
    //リモートキラー
    static OptionItem OptionModeRemotekiller;
    public static OptionItem OptionKillAnimation;
    static int RateRemotekiller;
    byte Remotekillertarget;
    //ステルス
    static OptionItem OptionModeStealth;
    static int RateStealth;
    static OptionItem OptionStealthDarkenDuration;
    float StealthDarkenDuration;
    float darkenTimer;
    PlayerControl[] darkenedPlayers;
    SystemTypes? darkenedRoom = null;
    //ノーティファー
    static OptionItem OptionModeNotifier;
    static OptionItem OptionNotifierProbability;
    static int RateNotifier;
    static int NotifierCance;
    //タイムシーフ
    static OptionItem OptionModeTimeThief;
    static OptionItem OptionTimeThiefDecreaseMeetingTime;
    static int RateTimeThief;
    static int TimeThiefDecreaseMeetingTime;
    static OptionItem OptionTimeThiefReturnStolenTimeUponDeath;
    static bool TimeThiefReturnStolenTimeUponDeath;
    public bool RevertOnDie => TimeThiefReturnStolenTimeUponDeath;
    static int Count;
    //大狼
    static OptionItem OptionModeTairo;
    static OptionItem OptionTairoDeathReason;
    static int RateTairo;
    public static bool TairoDeathReason;
    //メイヤー
    static OptionItem OptionModeMayor;
    static OptionItem OptionAdditionalVote;
    static int RateMayor;
    static int AdditionalVote;
    //モグラ
    static OptionItem OptionModeMole;
    static int RateMole;
    //プログレスキラー
    static OptionItem OptionModeProgresskiller;
    static OptionItem OptionProgressKillerMadseen;
    static OptionItem OptionProgressWorkhorseseen;
    static int RateProgresskiller;
    static bool ProgressKillerMadseen;
    public static bool ProgressWorkhorseseen;
    //ネコカボチャ
    static OptionItem OptionModeNekokabocha;
    static BooleanOptionItem optionImpostorsGetRevenged;
    static BooleanOptionItem optionMadmatesGetRevenged;
    static BooleanOptionItem optionNeutralsGetRevenged;
    static BooleanOptionItem optionRevengeOnExile;
    static int RateNekokabocha;
    static bool impostorsGetRevenged;
    static bool madmatesGetRevenged;
    static bool NeutralsGetRevenged;
    static bool revengeOnExile;
    bool IsDead;
    //インサイダー
    static OptionItem OptionModeInsider;
    List<byte> InsiderCansee = new();
    static int RateInsider;
    //ペンギン
    static OptionItem OptionModePenguin;
    static OptionItem OptionAbductTimerLimit;
    static OptionItem OptionMeetingKill;
    PlayerControl AbductVictim;
    static int RatePenguin;
    float AbductTimer;
    float AbductTimerLimit;
    bool stopCount;
    bool MeetingKill;

    //カムバッカー
    static OptionItem OptionModeComebaker;
    static int RateComebaker;
    private Vector2 Tp;

    //秘匿設定
    static OptionItem FirstAbility;
    static OptionItem OptionAlienHideAbility;
    static bool AlienHideAbility;
    /* JackalOption*/
    public static OptionItem OptionKillCooldown;
    private static OptionItem OptionCooldown;
    public static OptionItem OptionCanUseSabotage;
    private static OptionItem OptionJackalCanAlsoBeExposedToJMafia;
    private static OptionItem OptionJJackalMafiaCanAlsoBeExposedToJackal;
    private static OptionItem OptionJJackalCanKillMafia;
    static OptionItem OptionImpostorCanSidekick;
    public static OptionItem OptionSidekickCanSeeOldImpostorTeammates;
    public static OptionItem OptionImpostorCanSeeNameColor;
    public static OptionItem OptionCanMakeSidekick;
    public static OptionItem OptionSidekickPromotion;
    static OptionItem OptionHasImpostorVision;
    bool CanSideKick;
    enum OptionName
    {
        AlienHideAbility, AlienFirstAbility,
        AlienCVampire, VampireKillDelay, VampireSpeedDownCount, VampireSpeedDown,
        AlienCEvilHacker,
        AlienCLimiter, blastrange,
        AlienCPuppeteer, PuppeteerPuppetCool,
        AlienCRemoteKiller, KillAnimation,
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
        AlienCComebacker
    }
    static void SetupOptionItem()//NowMax : 52
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 5, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 7, GeneralOption.CanUseSabotage, false, false);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 53, GeneralOption.ImpostorVision, true, false);
        OptionJJackalCanKillMafia = BooleanOptionItem.Create(RoleInfo, 9, JackalMafia.JackalOption.JackalCanKillMafia, false, false);
        OptionJJackalMafiaCanAlsoBeExposedToJackal = BooleanOptionItem.Create(RoleInfo, 10, JackalMafia.JackalOption.JackalMafiaCanAlsoBeExposedToJackal, false, false);
        OptionJackalCanAlsoBeExposedToJMafia = BooleanOptionItem.Create(RoleInfo, 11, JackalMafia.JackalOption.JackalCanAlsoBeExposedToJMafia, true, false);
        OptionCanMakeSidekick = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanCreateSideKick, true, false);
        OptionImpostorCanSidekick = BooleanOptionItem.Create(RoleInfo, 13, JackalMafia.JackalOption.JackalImpostorCanSidekick, false, false, OptionCanMakeSidekick);
        OptionSidekickCanSeeOldImpostorTeammates = BooleanOptionItem.Create(RoleInfo, 14, JackalMafia.JackalOption.JackalbeforeImpCanSeeImp, false, false, OptionImpostorCanSidekick);
        OptionImpostorCanSeeNameColor = BooleanOptionItem.Create(RoleInfo, 15, JackalMafia.JackalOption.Jackaldollimpgaimpnimieru, false, false, OptionImpostorCanSidekick);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 16, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 30f, false, OptionCanMakeSidekick)
        .SetValueFormat(OptionFormat.Seconds);
        OptionSidekickPromotion = BooleanOptionItem.Create(RoleInfo, 17, JackalMafia.JackalOption.JackalSidekickPromotion, false, false, OptionCanMakeSidekick);
        FirstAbility = BooleanOptionItem.Create(RoleInfo, 4, OptionName.AlienFirstAbility, false, false);
        OptionAlienHideAbility = BooleanOptionItem.Create(RoleInfo, 18, OptionName.AlienHideAbility, false, false);
        OptionModeVampire = FloatOptionItem.Create(RoleInfo, 19, OptionName.AlienCVampire, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        SpeedDown = BooleanOptionItem.Create(RoleInfo, 20, OptionName.VampireSpeedDown, true, false, OptionModeVampire);
        SpeedDownCount = FloatOptionItem.Create(RoleInfo, 21, OptionName.VampireSpeedDownCount, new(0f, 1000f, 1f), 10f, false, SpeedDown).SetValueFormat(OptionFormat.Seconds);
        OptionVampireKillDelay = FloatOptionItem.Create(RoleInfo, 22, OptionName.VampireKillDelay, new(0, 100, 0.2f), 10, false, OptionModeVampire).SetValueFormat(OptionFormat.Seconds);
        OptionModeEvilHacker = FloatOptionItem.Create(RoleInfo, 23, OptionName.AlienCEvilHacker, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionModeLimiter = FloatOptionItem.Create(RoleInfo, 24, OptionName.AlienCLimiter, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        Optionblastrange = FloatOptionItem.Create(RoleInfo, 25, OptionName.blastrange, new(0.5f, 20f, 0.5f), 5f, false, OptionModeLimiter);
        OptionModePuppeteer = FloatOptionItem.Create(RoleInfo, 26, OptionName.AlienCPuppeteer, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        PuppetCool = FloatOptionItem.Create(RoleInfo, 27, OptionName.PuppeteerPuppetCool, new(0, 100, 0.5f), 5f, false, OptionModePuppeteer).SetValueFormat(OptionFormat.Seconds);
        OptionModeStealth = FloatOptionItem.Create(RoleInfo, 28, OptionName.AlienCStealth, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionStealthDarkenDuration = FloatOptionItem.Create(RoleInfo, 29, OptionName.StealthDarkenDuration, new(0.5f, 5f, 0.5f), 1f, false, OptionModeStealth).SetValueFormat(OptionFormat.Seconds);
        OptionModeRemotekiller = FloatOptionItem.Create(RoleInfo, 30, OptionName.AlienCRemoteKiller, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionKillAnimation = BooleanOptionItem.Create(RoleInfo, 55, OptionName.KillAnimation, false, false, OptionModeRemotekiller);
        OptionModeNotifier = FloatOptionItem.Create(RoleInfo, 31, OptionName.AlienCNotifier, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionNotifierProbability = FloatOptionItem.Create(RoleInfo, 32, OptionName.NotifierProbability, new(0, 100, 5), 50, false, OptionModeNotifier).SetValueFormat(OptionFormat.Percent);
        OptionModeTimeThief = FloatOptionItem.Create(RoleInfo, 33, OptionName.AlienCTimeThief, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionTimeThiefDecreaseMeetingTime = FloatOptionItem.Create(RoleInfo, 34, OptionName.TimeThiefDecreaseMeetingTime, new(0, 100, 5), 50, false, OptionModeTimeThief).SetValueFormat(OptionFormat.Seconds);
        OptionTimeThiefReturnStolenTimeUponDeath = BooleanOptionItem.Create(RoleInfo, 35, OptionName.TimeThiefReturnStolenTimeUponDeath, false, false, OptionModeTimeThief);
        OptionModeTairo = FloatOptionItem.Create(RoleInfo, 36, OptionName.AlienCTairo, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionTairoDeathReason = BooleanOptionItem.Create(RoleInfo, 37, OptionName.TairoDeathReason, false, false, OptionModeTairo);
        OptionModeMayor = FloatOptionItem.Create(RoleInfo, 38, OptionName.AlienCMayor, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionAdditionalVote = IntegerOptionItem.Create(RoleInfo, 39, OptionName.MayorAdditionalVote, new(1, 99, 1), 1, false, OptionModeMayor).SetValueFormat(OptionFormat.Votes);
        OptionModeMole = FloatOptionItem.Create(RoleInfo, 40, OptionName.AlienCMole, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionModeProgresskiller = FloatOptionItem.Create(RoleInfo, 41, OptionName.AlienCProgressKiller, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionProgressKillerMadseen = BooleanOptionItem.Create(RoleInfo, 42, OptionName.ProgressKillerMadseen, false, false, OptionModeProgresskiller);
        OptionProgressWorkhorseseen = BooleanOptionItem.Create(RoleInfo, 43, OptionName.ProgressWorkhorseseen, false, false, OptionModeProgresskiller);
        OptionModeNekokabocha = FloatOptionItem.Create(RoleInfo, 44, OptionName.AlienCNekokabocha, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        optionImpostorsGetRevenged = BooleanOptionItem.Create(RoleInfo, 45, OptionName.NekoKabochaImpostorsGetRevenged, false, false, OptionModeNekokabocha);
        optionMadmatesGetRevenged = BooleanOptionItem.Create(RoleInfo, 46, OptionName.NekoKabochaMadmatesGetRevenged, false, false, OptionModeNekokabocha);
        optionNeutralsGetRevenged = BooleanOptionItem.Create(RoleInfo, 47, OptionName.NekoKabochaNeutralsGetRevenged, false, false, OptionModeNekokabocha);
        optionRevengeOnExile = BooleanOptionItem.Create(RoleInfo, 48, OptionName.NekoKabochaRevengeOnExile, false, false, OptionModeNekokabocha);
        OptionModeInsider = FloatOptionItem.Create(RoleInfo, 49, OptionName.AlienCInsider, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionModePenguin = FloatOptionItem.Create(RoleInfo, 50, OptionName.AlienCPenguin, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionAbductTimerLimit = FloatOptionItem.Create(RoleInfo, 51, OptionName.PenguinAbductTimerLimit, new(5f, 100f, 1f), 10f, false, OptionModePenguin).SetValueFormat(OptionFormat.Seconds);
        OptionMeetingKill = BooleanOptionItem.Create(RoleInfo, 52, OptionName.PenguinMeetingKill, false, false, OptionModePenguin);
        OptionModeComebaker = FloatOptionItem.Create(RoleInfo, 54, OptionName.AlienCComebacker, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionModeNomal = FloatOptionItem.Create(RoleInfo, 8, OptionName.AlienCNomal, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        RoleAddAddons.Create(RoleInfo, 100, NeutralKiller: true);
    }
    #endregion
    #endregion

    #region モード
    public AlienMode mode = AlienMode.None;

    #endregion
}