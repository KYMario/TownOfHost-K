using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Hazel;
using AmongUs.GameOptions;

namespace TownOfHost.Roles.Core;

public abstract class RoleBase : IDisposable
{
    public PlayerControl Player { get; private set; }
    /// <summary>
    /// プレイヤーの状態
    /// </summary>
    public readonly PlayerState MyState;
    /// <summary>
    /// プレイヤーのタスク状態
    /// </summary>
    public readonly TaskState MyTaskState;
    /// <summary>
    /// タスクを持っているか。
    /// 初期値はクルー役職のみ持つ
    /// </summary>
    protected Func<HasTask> hasTasks;
    /// <summary>
    /// タスクを持っているか
    /// </summary>
    public HasTask HasTasks => hasTasks.Invoke();
    /// <summary>
    /// タスクが完了しているか
    /// </summary>
    public bool IsTaskFinished => MyTaskState.IsTaskFinished;
    /// <summary>
    /// アビリティボタンで発動する能力を持っているか
    /// </summary>
    public bool HasAbility;
    public RoleBase(
        SimpleRoleInfo roleInfo,
        PlayerControl player,
        Func<HasTask> hasTasks = null,
        bool? hasAbility = null
    )
    {
        Player = player;
        this.hasTasks = hasTasks ?? (roleInfo.CustomRoleType == CustomRoleTypes.Crewmate ? () => HasTask.True : () => HasTask.False);
        HasAbility = hasAbility ?? roleInfo.BaseRoleType.Invoke() is
            RoleTypes.Shapeshifter or
            RoleTypes.Phantom or
            RoleTypes.Engineer or
            RoleTypes.Scientist or
            RoleTypes.Tracker or
            RoleTypes.Detective or
            RoleTypes.GuardianAngel or
            RoleTypes.CrewmateGhost or
            RoleTypes.ImpostorGhost;

        MyState = PlayerState.GetByPlayerId(player.PlayerId);
        MyTaskState = MyState.GetTaskState();

        CustomRoleManager.AllActiveRoles.TryAdd(Player.PlayerId, this);
    }
#pragma warning disable CA1816
    public void Dispose()
    {
        OnDestroy();
        CustomRoleManager.AllActiveRoles.Remove(Player.PlayerId);
        Player = null;
    }
#pragma warning restore CA1816
    public bool Is(PlayerControl player)
    {
        return player.PlayerId == Player.PlayerId;
    }
    /// <summary>
    /// インスタンス作成後すぐに呼ばれる関数
    /// </summary>
    public virtual void Add()
    { }
    /// <summary>
    /// ゲーム開始後にインスタンス作成された時に呼ばれる関数
    /// </summary>
    public virtual void ChengeRoleAdd()
    { }
    /// <summary>
    /// ロールベースが破棄されるときに呼ばれる関数
    /// </summary>
    public virtual void OnDestroy()
    { }
    /// <summary>
    /// RoleBase専用のRPC送信クラス
    /// 自身のPlayerIdを自動的に送信する
    /// </summary>
    protected class RoleRPCSender : IDisposable
    {
        public MessageWriter Writer;
        public RoleRPCSender(RoleBase role)
        {
            Writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.CustomRoleSync, SendOption.None, -1);
            Writer.Write(role.Player.PlayerId);
        }
        public void Dispose()
        {
            if (!PlayerCatch.AnyModClient())
            {
                Writer.Recycle();
                return;
            }
            AmongUsClient.Instance.FinishRpcImmediately(Writer);
        }
    }
    /// <summary>
    /// RPC送信クラスの作成
    /// PlayerIdは自動的に追記されるので意識しなくてもよい。
    /// </summary>
    /// <param name="rpcType">送信するCustomRPC</param>
    /// <returns>送信に使用するRoleRPCSender</returns>
    protected RoleRPCSender CreateSender()
    {
        return new RoleRPCSender(this);
    }
    /// <summary>
    /// RPCを受け取った時に呼ばれる関数
    /// RoleRPCSenderで送信されたPlayerIdは削除されて渡されるため意識しなくてもよい。
    /// </summary>
    /// <param name="reader">届いたRPCの情報</param>
    /// <param name="rpcType">届いたCustomRPC</param>
    public virtual void ReceiveRPC(MessageReader reader)
    { }
    /// <summary>
    /// 能力ボタンを使えるかどうか
    /// </summary>
    /// <returns>trueを返した場合、能力ボタンを使える</returns>
    public virtual bool CanUseAbilityButton() => true;
    /// <summary>
    /// BuildGameOptionsで呼ばれる関数
    /// </summary>
    public virtual void ApplyGameOptions(IGameOptions opt)
    { }

    /// <summary>
    /// ターゲットとしてのCheckMurder処理
    /// キラーより先に判定
    /// キル出来ない状態(無敵など)はinfo.CanKill=falseとしてtrueを返す
    /// キル行為自体をなかったことにする場合はfalseを返す。
    /// [ホストのみ]
    /// </summary>
    /// <param name="info">キル関係者情報</param>
    /// <returns>false:キル行為を起こさせない</returns>
    public virtual bool OnCheckMurderAsTarget(MurderInfo info) => true;

    /// <summary>
    /// ターゲットとしてのMurderPlayer処理
    /// [ホストのみ]
    /// </summary>
    /// <param name="info">キル関係者情報</param>
    public virtual void OnMurderPlayerAsTarget(MurderInfo info)
    { }

    /// <summary>
    /// シェイプシフト時に呼ばれる関数
    /// 自分自身について呼ばれるため本人確認不要
    /// [全クライアント]
    /// </summary>
    /// <param name="target">変身先</param>
    public virtual void OnShapeshift(PlayerControl target)
    { }

    /// <summary>
    /// 自視点のみ変身する
    /// 抜け殻を自視点のみに残すことが可能
    /// </summary>
    public virtual bool CanDesyncShapeshift => false;

    /// <summary>
    /// シェイプシフトされる前に呼ばれる関数
    /// falseを返すとシェイプシフトをなかったことにできる
    /// 自分自身について呼ばれるため本人確認不要
    /// [ホストのみ]
    /// </summary>
    /// <param name="target">変身先</param>
    /// <param name="shouldAnimate">アニメーションを再生するか</param>
    public virtual bool CheckShapeshift(PlayerControl target, ref bool shouldAnimate) => true;

    /// <summary>
    /// 透明化 発動前に呼ばれる関数
    /// falseを返すと透明化をなかったことにできる
    /// 自分自身について呼ばれるため本人確認不要
    /// Host以外も呼ばれるので注意
    /// </summary>
    /// <returns>cancelするならfalse</returns>
    public virtual bool CheckVanish() => true;

    /// <summary>
    /// タスクターンに常時呼ばれる関数
    /// 自分自身について呼ばれるため本人確認不要
    /// Host以外も呼ばれるので注意
    /// playerが自分以外であるときに処理したい場合は同じ引数でstaticとして実装し
    /// CustomRoleManager.OnFixedUpdateOthersに登録する
    /// [全クライアント]
    /// </summary>
    /// <param name="player">対象プレイヤー</param>
    public virtual void OnFixedUpdate(PlayerControl player)
    { }

    /// <summary>
    /// 通報時，会議が呼ばれることが確定してから呼ばれる関数<br/>
    /// 通報に関係ないプレイヤーも呼ばれる
    /// [ホストのみ]
    /// </summary>
    /// <param name="reporter">通報したプレイヤー</param>
    /// <param name="target">通報されたプレイヤー</param>
    public virtual void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    { }
    /// <summary>
    /// ベントボタンがそもそも押せるか
    /// </summary>
    public virtual bool CanClickUseVentButton => true;

    /// <summary>
    /// <para>ベントに入ったときに呼ばれる関数</para>
    /// <para>キャンセル可</para>
    /// </summary>
    /// <param name="physics"></param>
    /// <param name="id"></param>
    /// <returns>falseを返すとベントから追い出され、他人からアニメーションも見られません</returns>
    public virtual bool OnEnterVent(PlayerPhysics physics, int ventId) => true;
    /// <summary>
    /// ベント移動を封じるかの関数。<br/>
    /// OnEnterVentの方が速く呼ばれる。<br/>
    /// 基本的に移動を封じる時のみ使う。
    /// [ホストのみ]
    /// </summary>
    /// <param name="physics"></param>
    /// <param name="Id"></param>
    /// <returns>falseを返すとベント移動が出来ません。</returns>
    public virtual bool CanVentMoving(PlayerPhysics physics, int ventId) => true;
    /// <summary>
    /// ミーティングが始まった時に呼ばれる関数
    /// [全クライアント]
    /// </summary>
    public virtual void OnStartMeeting()
    { }
    /// <summary>
    /// ミーティングが始まった時同数などと一緒に表示されるメッセージ
    /// </summary>
    /// <returns></returns>
    public virtual string MeetingAddMessage() => "";

    /// <summary>
    /// 自分が投票した瞬間，票がカウントされる前に呼ばれる<br/>
    /// falseを返すと投票行動自体をなかったことにし，再度投票できるようになる<br/>
    /// 投票行動自体は取り消さず，票だけカウントさせない場合は<see cref="ModifyVote"/>を使用し，doVoteをfalseにする
    /// [ホストのみ]
    /// </summary>
    /// <param name="votedForId">投票先</param>
    /// <param name="voter">投票した人</param>
    /// <returns>falseを返すと投票自体がなかったことになり，投票者自身以外には投票したことがバレません</returns>
    public virtual bool CheckVoteAsVoter(byte votedForId, PlayerControl voter) => true;

    /// <summary>
    /// 誰かが投票した瞬間に呼ばれ，票を書き換えることができる<br/>
    /// 投票行動自体をなかったことにしたい場合は<see cref="CheckVoteAsVoter"/>を使用する
    /// </summary>
    /// <param name="voterId">投票した人のID</param>
    /// <param name="sourceVotedForId">投票された人のID</param>
    /// <returns>(変更後の投票先(変更しないならnull), 変更後の票数(変更しないならnull), 投票をカウントするか)</returns>
    public virtual (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional) => (null, null, true);

    /// <summary>
    /// 追放後に行われる処理
    /// </summary>
    /// <param name="exiled">追放されるプレイヤー</param>
    /// <param name="DecidedWinner">勝者を確定させるか</param>
    public virtual void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    { }

    /// <summary>
    /// タスクターンが始まる直前に毎回呼ばれる関数
    /// </summary>
    public virtual void AfterMeetingTasks()
    { }
    /// <summary>
    /// タスクターンにスポーンした時に呼ばれる関数
    /// 実行後必ず、SyncSettings()、RpcResetAbilityCooldown()が呼ばれる
    /// </summary>
    /// <param name="initialState">ゲーム最初のスポーンかどうか</param>
    public virtual void OnSpawn(bool initialState = false)
    { }
    /// <summary>
    /// ゲーム開始のイントロ後に呼ばれる関数。
    /// ※アムネシア制御効かないので個別で処理
    /// </summary>
    public virtual void StartGameTasks()
    { }
    /// <summary>
    /// モノクラー等に使う。シェイプ後,イントロ後,タスクターン始めに呼ばれる。
    /// ※アムネシア制御効かないので個別で処理
    /// </summary>
    public virtual void ChangeColor()
    { }

    /// <summary>
    /// タスクが一個完了するごとに呼ばれる関数
    /// [全クライアント]
    /// </summary>
    /// <returns>falseを返すとバニラ処理をキャンセルする</returns>
    public virtual bool OnCompleteTask(uint taskid) => true;

    // == Sabotage関連処理 ==
    /// <summary>
    /// サボタージュを起すことが出来るか判定する。
    /// ドア閉めには関与できない
    /// ※アムネシア制御効かないので個別で処理
    /// </summary>
    /// <param name="systemType">サボタージュの種類</param>
    /// <returns>falseでサボタージュをキャンセル</returns>
    public virtual bool OnInvokeSabotage(SystemTypes systemType) => true;

    /// <summary>
    /// 誰かがサボタージュを発生させたときに呼ばれる
    /// ※アムネシア制御効かないので個別で処理
    /// </summary>
    /// <param name="player">アクションを起こしたプレイヤー</param>
    /// <param name="systemType">サボタージュの種類</param>
    /// <returns>falseでサボタージュのキャンセル</returns>
    public virtual bool OnSabotage(PlayerControl player, SystemTypes systemType) => true;

    /// <summary>
    /// サボタージュ後に行われる処理
    /// </summary>
    public virtual void AfterSabotage(SystemTypes systemType) { }

    // NameSystem
    // 名前は下記の構成で表示される
    // [Role][Progress]
    // [Name][Mark]
    // [Lower][suffix]
    // Progress:タスク進捗/残弾等の状態表示
    // Mark:役職能力によるターゲットマークなど
    // Lower:役職用追加文字情報。Modの場合画面下に表示される。
    // Suffix:ターゲット矢印などの追加情報。

    public virtual bool NotifyRolesCheckOtherName => false;
    /// <summary>
    /// seenによる表示上のRoleNameの書き換え。つまりみてみて～!!ってこと。
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="enabled">RoleNameを表示するかどうか</param>
    /// <param name="roleColor">RoleNameの色</param>
    /// <param name="roleText">RoleNameのテキスト</param>
    public virtual void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    { }
    /// <summary>
    /// seerによる表示上のRoleNameの書き換え。つまり見ちゃうぞ!!ってこと。
    /// </summary>
    /// <param name="seen">見られる側</param>
    /// <param name="enabled">RoleNameを表示するかどうか</param>
    /// <param name="roleColor">RoleNameの色</param>
    /// <param name="roleText">RoleNameのテキスト</param>
    public virtual void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    { }
    /// <summary>
    /// 本来の役職名の書き換え
    /// </summary>
    /// <param name="roleColor">RoleNameの色</param>
    /// <param name="roleText">RoleNameのテキスト</param>
    public virtual void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
    { }
    /// <summary>
    /// seerによるProgressTextの書き換え
    /// </summary>
    /// <param name="seen">見られる側</param>
    /// <param name="enabled">ProgressTextを表示するかどうか</param>
    /// <param name="text">ProgressTextのテキスト</param>
    public virtual void OverrideProgressTextAsSeer(PlayerControl seen, ref bool enabled, ref string text)
    { }
    /// <summary>
    /// seenによるProgressTextの書き換え
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="enabled">ProgressTextを表示するかどうか</param>
    /// <param name="text">ProgressTextのテキスト</param>
    public virtual void OverrideProgressTextAsSeen(PlayerControl seer, ref bool enabled, ref string text)
    { }
    /// <summary>
    /// 役職名の横に出るテキスト
    /// </summary>
    /// <param name="comms">コミュサボ中扱いするかどうか</param>
    public virtual string GetProgressText(bool comms = false, bool GameLog = false) => "";
    /// <summary>
    /// seerが自分であるときのMark
    /// seer,seenともに自分以外であるときに表示したい場合は同じ引数でstaticとして実装し
    /// CustomRoleManager.MarkOthersに登録する
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <returns>構築したMark</returns>
    public virtual string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => "";
    /// <summary>
    /// seerが自分であるときのLowerTex
    /// seer,seenともに自分以外であるときに表示したい場合は同じ引数でstaticとして実装し
    /// CustomRoleManager.LowerOthersに登録する
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <param name="isForHud">ModでHudとして表示する場合</param>
    /// <returns>構築したLowerText</returns>
    public virtual string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false) => "";
    /// <summary>
    /// seer自分であるときのSuffix
    /// seer,seenともに自分以外であるときに表示したい場合は同じ引数でstaticとして実装し
    /// CustomRoleManager.SuffixOthersに登録する
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <returns>構築したMark</returns>
    public virtual string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => "";

    public virtual bool AllEnabledColor => false;
    /// <summary>
    /// アビリティボタンのテキストを変更します
    /// </summary>
    public virtual string GetAbilityButtonText()
    {
        StringNames? str = Player?.Data?.Role?.Role switch
        {
            RoleTypes.Engineer => StringNames.VentAbility,
            RoleTypes.Scientist => StringNames.VitalsAbility,
            RoleTypes.Tracker => StringNames.TrackerAbility,
            RoleTypes.Shapeshifter => StringNames.ShapeshiftAbility,
            RoleTypes.Phantom => StringNames.PhantomAbility,
            RoleTypes.GuardianAngel => StringNames.ProtectAbility,
            RoleTypes.Detective => StringNames.DetectiveAbilityNotes,
            RoleTypes.ImpostorGhost or RoleTypes.CrewmateGhost => StringNames.HauntAbilityName,
            _ => null//アプデ対応用
        };
        return str.HasValue ? Translator.GetString(str.Value) : "Invalid";
    }
    /// <summary>
    /// アビリティボタンの画像を変更します。
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public virtual bool OverrideAbilityButton(out string text)
    {
        text = default;
        return false;
    }
    /// <summary>
    /// 会議をキャンセルするために使う<br/>
    /// <see cref="OnReportDeadBody"/>より先に呼ばれる、キャンセルした場合は呼ばれない<br/>
    /// trueを返すとキャンセルされる
    /// </summary>
    public virtual bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, ref DontReportreson reason) => false;

    /// <summary>
    /// 占い結果で表示される役職を変更することができる<br/>
    /// NotAssignedを返すと変更されない
    /// </summary>
    public virtual CustomRoles TellResults(PlayerControl player) => CustomRoles.NotAssigned;

    /// <summary>
    /// 投票結果を返す<br/>
    /// trueを返すと追放の「ランダム追放」「全員追放」などが実行されない
    /// </summary>
    public virtual bool VotingResults(ref NetworkedPlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile) => false;

    /// <summary>
    /// ベントの出入り、移動で呼び出される
    /// </summary>
    public virtual void OnVentilationSystemUpdate(PlayerControl user, VentilationSystem.Operation Operation, int ventId)
    { }

    /// <summary>
    /// 名前を一時的に変更する時に使う<br/>
    /// NotifyRoles時に呼び出される
    /// </summary>
    /// <param name = "name" > 変更する名前 </param>
    /// <param name = "NoMarker" > マーカーや追加情報を表示しない </param>
    /// <returns>名前を変更するかどうか</returns>
    public virtual bool GetTemporaryName(ref string name, ref bool NoMarker, PlayerControl seer, PlayerControl seen = null) => false;

    /// <summary>
    /// 回線切断者が起こった時に呼ばれる関数
    /// </summary>
    /// <param name="player"></param>
    public virtual void OnLeftPlayer(PlayerControl player) { }

    /// <summary>
    /// 自身がゲッサーされそうになった時に呼ばれる関数
    /// falseを返すと返り討ち。
    /// nullなら流す
    /// </summary>
    /// <param name="killer"></param>
    /// <returns></returns>
    public virtual bool? CheckGuess(PlayerControl killer) => true;

    /// <summary>
    /// Host用。
    /// タスクが出来るか。
    /// falseだとできない。
    /// </summary>
    /// <returns></returns>
    public virtual bool CanTask() => UtilsTask.HasTasks(PlayerControl.LocalPlayer.Data, false);
    /// <summary>
    /// 会議後の置き換え役職の変更。<br/>
    /// 生存中しか適応されない
    /// </summary>
    public virtual RoleTypes? AfterMeetingRole => null;

    /// <summary>
    /// 勝利処理がほぼ終わった後に処理される<br/>
    /// </summary>
    public virtual void CheckWinner()
    { }

    /// <summary>
    /// 自身を別役職だと思い込む。
    /// </summary>
    public virtual CustomRoles Misidentify() => CustomRoles.NotAssigned;
    protected static AudioClip GetIntroSound(RoleTypes roleType) =>
        RoleManager.Instance.AllRoles.ToArray().Where((role) => role.Role == roleType).FirstOrDefault().IntroSound;
    public static AudioClip GetIntrosound(RoleTypes roleType) =>
        RoleManager.Instance.AllRoles.ToArray().Where((role) => role.Role == roleType).FirstOrDefault().IntroSound;
    public static FloatValueRule OptionBaseCoolTime => new(0, 180, 0.5f);

    //一々Translator参照戦でいいから多分楽 
    public static string GetString(StringNames stringName)
            => DestroyableSingleton<TranslationController>.Instance.GetString(stringName, new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppSystem.Object>(0));
    public static string GetString(string str, Dictionary<string, string> replacementDic = null) => Translator.GetString(str);
    protected enum GeneralOption
    {
        Cooldown,
        KillCooldown,
        CanVent,
        ImpostorVision,
        CanUseSabotage,
        CanCreateSideKick,
        Duration,
        cantaskcount,
        MeetingMaxTime,
        PlayShapeAnimate,
        TaskAwakening,
        AwakeningTaskcount,
        AbilityAwakening,
        OptionCount,
        EngineerInVentCooldown,
        TaskTrigger,
        CanUseActiveComms
    }
    public enum DontReportreson
    {
        None,
        wait,
        NonReport,
        Transparent,
        CantUseButton,
        Eat,
        Other,
        Impostor,
    }
}
