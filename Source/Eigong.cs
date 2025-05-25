using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using States = MonsterBase.States;

namespace InnerEigong;

/// <summary>
/// Modifies the behavior of the Eigong boss.
/// </summary>
[RequireComponent(typeof(StealthGameMonster))]
internal class Eigong : MonoBehaviour {
    private StealthGameMonster _monster = null!;
    private AnimatorOverrideController _originalController = null!;
    private RuntimeAnimatorController _newController = null!;

    private void Awake() {
        TryGetComponent(out _monster);
        _monster.OverrideWanderingIdleTime(0);
#if DEBUG
        _monster.StartingPhaseIndex = 2;
#endif

        _originalController = _monster.animator.runtimeAnimatorController as AnimatorOverrideController;
        AssetManager.TryGet("Inner Eigong Controller", out _newController);

        PreSetupGunAttack();

        ResetMonster();
    }

    private async void Start() {
        await UniTask.WaitUntil(() => _monster.fsm != null);

        PostSetupGunAttack();
    }

    private void CreateTrackingSlashes() {
        AssetManager.Inst<GameObject>("Tracking Slashes", null);
    }

    private OldPivotRotate _armRotate = null!;
    private LaserAttackController _laserAttack = null!;

    private Animator _originalAnimator = null!;

    private void PreSetupGunAttack() {
        _originalAnimator = _monster.animator;
        var body = _originalAnimator.transform.Find("View/YiGung/Body");
        var arm = AssetManager.Inst<GameObject>("Arm", body);
        arm.name = arm.name.Replace("(Clone)", "");
        arm.transform.localPosition = new Vector2(20, -23);
        var armRenderer = arm.GetComponent<SpriteRenderer>();
        armRenderer.sortingLayerName = "Monster";
        // var overlayer = arm.AddComponent<ColorKeyOverlayer>();
        // overlayer.OverlayScale = 100;
        // overlayer.Tolerance = 0.1f;
        // overlayer.Smoothing = 0.05f;
        _armRotate = arm.AddComponent<OldPivotRotate>();
        _armRotate.inverseAngle = true;
        _armRotate.KeepLookAtPlayer = true;
        _armRotate.minRotate = -60;
        _armRotate.maxRotate = 75;
        _armRotate.offset = 0;
        _armRotate.referenceActor = _monster;
        if (PreloadManager.TryGet("Sniper", out var sniperRef)) {
            var bow = sniperRef.transform.Find("Dragon_Sniper/DragonSniper/RotateRArm/RArm/Bow");
            var sniperCoreRef = bow.transform.Find("SniperLaserCore").gameObject;
            var sniperCore = Instantiate(sniperCoreRef, arm.transform);
            sniperCore.name = sniperCore.name.Replace("(Clone)", "");
            sniperCore.transform.localPosition = new Vector2(-38, 5.3f);
            sniperCore.transform.localScale = new Vector3(-1, 1, 1);
            foreach (var renderer in sniperCore.GetComponentsInChildren<Renderer>(true)) {
                renderer.sortingLayerName = "Monster";
                renderer.sortingOrder = armRenderer.sortingOrder - 100;
            }
            var parriableOwner = sniperCore.AddComponent<GeneralParriableOwner>();
            Traverse.Create(parriableOwner).Field<MonsterBase>("bindMonster").Value = _monster;
            var shootPos = sniperCore.GetComponentsInChildren<SpawnAtPoint>(true).FirstOrDefault(child => child.name == "ShootPos");
            if (shootPos) {
                shootPos.transform.localPosition += Vector3.right;
                var preFireCircleRef = bow.Find("BowHead/512 fade inner circle blurred");
                var preFireCircle = Instantiate(preFireCircleRef, shootPos.transform);
                preFireCircle.name = "Anticipation Sphere";
                var fxOffset = Vector3.right * 24;
                preFireCircle.transform.localPosition += fxOffset;
                preFireCircle.gameObject.SetActive(true);
                shootPos.transform.Find("ShootExplosion").localPosition += fxOffset;
            }
            _laserAttack = sniperCore.GetComponentInChildren<LaserAttackController>(true);
            var parriable = _laserAttack.gameObject.AddComponent<ParriableAttackEffect>();
            parriable.param = new ParryParam {
                knockBackType = KnockBackType.Large,
                knockBackValue = 500,
                LiftYForce = 100,
                hurtLiftType = HurtType.HurtLarge
            };
            _laserAttack.gameObject.SetActive(true);
            var laserDamager = _laserAttack.GetComponentInChildren<DamageDealer>(true);
            laserDamager.damageAmount = 100;
            laserDamager.attacker = GetComponentInChildren<Health>();
            laserDamager.bindingParry = parriable;
            parriable.bindDamage = laserDamager;
            var laserDetector = _laserAttack.GetComponentInChildren<TriggerDetector>(true);
            laserDetector.Invoke("Awake", 0);
            sniperCore.SetActive(true);
            var laserEffector = _laserAttack.GetComponentInChildren<EffectDealer>();
            var laserView = sniperCore.GetComponentInChildren<LaserViewController>(true);
            laserView.gameObject.SetActive(true);
            arm.SetActive(false);

            var attackStates = transform.Find("States/Attacks");
            var gunStateObj = new GameObject($"[{(int)Constants.GunMonsterState}] Gun");
            gunStateObj.transform.SetParent(attackStates);
            var gunBossState = gunStateObj.AddComponent<BossGeneralState>();
            gunBossState.BindingAnimation = Constants.GunMonsterAnimation;
            gunBossState.state = Constants.GunMonsterState;
            // gunBossState.AutoFlipAround = true;
            gunBossState.ToCloseTransitionState = States.Attack5;
            AssetManager.TryGet(Constants.GunMonsterAnimation, out gunBossState.clip);
            gunBossState.EnterLevelReset();

            // Create laser sniper audio
            var sniperAudioRef = sniperRef.transform.Find("LogicRoot/Audio/EnemySFX_Sniper_Attack").gameObject;
            var sniperAudio = Instantiate(sniperAudioRef, _monster.monsterCore.logicRoot.Find("Audio"));
            sniperAudio.name = sniperAudio.name.Replace("(Clone)", "");
            var sniperSound = sniperAudio.GetComponent<SoundPlayer>();
            AkBankManager.LoadBank("Dragon_Sniper", false, false, sniperSound);
            sniperSound.EnterLevelReset();
        }
    }

    private static States[] _gunPrevStates = [
        States.Attack1,
        States.Attack5,
        States.Attack7,
        States.Attack10,
        States.Attack13,
        States.Attack14,
        States.Attack16,
        States.Attack18
    ];

    private void PostSetupGunAttack() {
        var gunBossState = _monster.FindState(Constants.GunMonsterState);
        var gunStateParent = gunBossState.transform;
        var linkNextMoveWeightObj = new GameObject("weight");
        linkNextMoveWeightObj.transform.SetParent(gunStateParent);
        var linkNextMoveWeightComp = linkNextMoveWeightObj.AddComponent<LinkNextMoveStateWeight>();
#if DEBUG
        var gunWeight = 100;
#else
        var gunWeight = 0.5f;
#endif
        var gunStateWeight = new AttackWeight {
            state = gunBossState,
            weight = gunWeight
        };
        var engagingState = _monster.GetComponentInChildren<StealthEngaging>(true);
        var engagingStateWeight = new AttackWeight {
            state = engagingState,
            weight = 1
        };
        linkNextMoveWeightComp.stateWeightList = [engagingStateWeight];

        _monster.animator.gameObject.AddComponent<GunAnimatorFixer>();

        foreach (var state in _gunPrevStates) {
            var bossState = _monster.FindState(state);
            foreach (var linkNextMoveWeight in bossState.GetComponentsInChildren<LinkNextMoveStateWeight>(true)) {
                linkNextMoveWeight.stateWeightList.Add(gunStateWeight);
            }
        }

        _monster.postureSystem.OnPostureEmpty.AddListener(HandleDeath);
    }

    private void StopArmFollow() {
        _armRotate.KeepLookAtPlayer = false;
    }

    private void RestartArmFollow() {
        _armRotate.KeepLookAtPlayer = true;
    }

    private CancellationTokenSource? _fireLaserCancelTokenSrc;

    /// <summary>
    /// Fire the laser.
    /// </summary>
    internal async UniTask FireLaser() {
        var head = _originalAnimator.transform.Find("View/YiGung/Head");
        head.Find("Hair").localPosition = Vector3.zero;
        head.gameObject.SetActive(true);
        _fireLaserCancelTokenSrc?.Cancel();
        _fireLaserCancelTokenSrc = new CancellationTokenSource();
        var fireLaserCancelToken = _fireLaserCancelTokenSrc.Token;
        _originalAnimator.runtimeAnimatorController = _newController;
        _monster.FacePlayer();
        RestartArmFollow();
        await UniTask.Delay(TimeSpan.FromSeconds(1.5f), cancellationToken: fireLaserCancelToken);
        // Laser 1
        StopArmFollow();
        _monster.FacePlayer();
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: fireLaserCancelToken);
        RestartArmFollow();
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f + 1 / 3f), cancellationToken: fireLaserCancelToken);
        // Laser 2
        StopArmFollow();
        _monster.FacePlayer();
        await UniTask.Delay(TimeSpan.FromSeconds(1 / 3f), cancellationToken: fireLaserCancelToken);
        // Laser 3
        _monster.FacePlayer();
        await UniTask.Delay(TimeSpan.FromSeconds(5 / 6f), cancellationToken: fireLaserCancelToken);
        ResetAnimator();
        _monster.ChangeStateIfValid(States.Engaging);
    }

    internal async void HandleDeath() {
        ResetAnimator();
        // Log.Debug("Phase: "  + _monster.PhaseIndex);
        // if (_monster.PhaseIndex >= 2) {
        //     await UniTask.Delay(TimeSpan.FromSeconds(0.25f));
        //     Log.Debug("TO DEAD");
        //     // _monster.GetType().GetMethod("ForceDie", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(_monster, []);
        //     _monster.ChangeStateIfValid(States.Dead);
        // }
    }

    /// <summary>
    /// Reset the modified <see cref="Animator">animator</see> to its original.
    /// </summary>
    private void ResetAnimator() {
        _fireLaserCancelTokenSrc?.Cancel();
        _fireLaserCancelTokenSrc = null;
        // _gunAnimator.StopPlayback();
        // _gunAnimator.enabled = false;
        // _originalAnimator.enabled = true;
        _originalAnimator.runtimeAnimatorController = _originalController;
        _armRotate.gameObject.SetActive(false);
    }

    /// <summary>
    /// Re-initialize the <see cref="MonsterBase">monster</see> component.
    /// </summary>
    private void ResetMonster() {
        AutoAttributeManager.AutoReferenceAllChildren(gameObject);

        Traverse.Create(_monster).Field("_inited").SetValue(false);
        _monster.Invoke("CheckInit", 0);
        _monster.EnterLevelReset();
#if DEBUG
        var statTraverse = Traverse.Create(_monster.monsterStat);
        statTraverse.Field<float>("BaseHealthValue").Value = 500;
#endif
    }
}