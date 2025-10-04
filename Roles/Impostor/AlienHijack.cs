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

using static TownOfHost.Roles.Impostor.Alien;

namespace TownOfHost.Roles.Impostor;

public sealed class AlienHijack : RoleBase, IMeetingTimeAlterable, IImpostor, INekomata
{
    public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(AlienHijack),
                player => new AlienHijack(player),
                CustomRoles.AlienHijack,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                2300,
                null,
                "HAl",
                OptionSort: (1, 1),
                introSound: () => GetIntroSound(RoleTypes.Shapeshifter)
            );
    public AlienHijack(PlayerControl player)
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
        UetukeNokori = OptUetuketukeTrun.GetInt();
        AbductTimer = 255f;
        stopCount = false;
        Aliens.Add(this);
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
        if (!Player.IsAlive()) return;

        RestartAbduct();

        if (UetukeNokori <= 0)
        {
            byte playerid = Player.PlayerId;
            _ = new LateTask(() =>
            {
                CustomRoleManager.AllActiveRoles[playerid] = MaenoRole;
                UtilsNotifyRoles.NotifyRoles();
            }, 2, "Modosu", true);
            Player.RpcSetCustomRole(MaenoCRole);
        }
        UetukeNokori--;
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
    public override string GetProgressText(bool comms = false, bool GameLog = false) => OptUetuketukeTrun.GetBool() ? $"<color=#ff1919>({UetukeNokori})</color>" : "";
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
    public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
    {
        roleText = Mode() + roleText;
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

        if (modeVampire) return "<color=#ff1919>" + GetString("Vampire") + "</color>";
        if (modeEvilHacker) return "<color=#ff1919>" + GetString("EvilHacker") + "</color>";
        if (modeLimiter) return "<color=#ff1919>" + GetString("Limiter") + "</color>";
        if (modePuppeteer) return "<color=#ff1919>" + GetString("Puppeteer") + "</color>";
        if (modeStealth) return "<color=#ff1919>" + GetString("Stealth") + "</color>";
        if (modeRemotekiller) return "<color=#8f00ce>" + GetString("Remotekiller") + "</color>";
        if (modeNotifier) return "<color=#ff1919>" + GetString("Notifier") + "</color>";
        if (modeTimeThief) return "<color=#ff1919>" + GetString("TimeThief") + "</color>";
        if (modeTairo) return "<color=#ff1919>" + GetString("Tairou") + "</color>";
        if (modeMayor) return "<color=#204d42>" + GetString("Mayor") + "</color>";
        if (modeMole) return "<color=#ff1919>" + GetString("Mole") + "</color>";
        if (modeProgresskiller) return "<color=#ff1919>" + GetString("ProgressKiller") + "</color>";
        if (modeNekokabocha) return "<color=#ff1919>" + GetString("NekoKabocha") + "</color>";
        if (modeinsider) return "<color=#ff1919>" + GetString("Insider") + "</color>";
        if (modepenguin) return "<color=#ff1919>" + GetString("Penguin") + "</color>";
        if (modeComebaker) return "<color=#ff9966>" + GetString("Comebacker") + "</color>";

        return "<color=#ff1919>？</color>";
    }
    void Init()
    {
        modeVampire = false;
        modeEvilHacker = false;
        modeLimiter = false;
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
        Tp = new(999f, 999f);
    }
    public RoleBase MaenoRole;
    public CustomRoles MaenoCRole;
    public static HashSet<AlienHijack> Aliens = new();
    //ヴァンパイア
    Dictionary<byte, float> BittenPlayers = new(14);
    public bool modeVampire;
    //イビルハッカー
    public bool modeEvilHacker;
    static Dictionary<byte, string> NameAddmin = new();
    //リミッター
    public bool modeLimiter;
    //パペッティア
    static Dictionary<byte, float> PuppetCooltime = new(15);
    static Dictionary<byte, AlienHijack> Puppets = new(15);
    public bool modePuppeteer;
    //リモートキラー
    public bool modeRemotekiller;
    byte Remotekillertarget;
    //ステルス
    public bool modeStealth;
    float darkenTimer;
    PlayerControl[] darkenedPlayers;
    SystemTypes? darkenedRoom = null;
    //ノーティファー
    public bool modeNotifier;
    //タイムシーフ
    public bool modeTimeThief;
    int Count;
    public bool RevertOnDie => Alien.TimeThiefReturnStolenTimeUponDeath;
    //大狼
    public bool modeTairo;
    //メイヤー
    public bool modeMayor;
    //モグラ
    public bool modeMole;
    //プログレスキラー
    public bool modeProgresskiller;
    //ネコカボチャ
    public bool modeNekokabocha;
    //インサイダー
    List<byte> InsiderCansee = new();
    public bool modeinsider;
    //ペンギン
    PlayerControl AbductVictim;
    public bool modepenguin;
    float AbductTimer;
    bool stopCount;
    //カムバッカー
    public bool modeComebaker;
    private Vector2 Tp;
    // 戻るなら
    int UetukeNokori;
}
#endregion