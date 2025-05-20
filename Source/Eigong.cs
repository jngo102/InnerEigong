using System;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using MonsterLove.StateMachine;
using UnityEngine;
using States = MonsterBase.States;

namespace InnerEigong;

/// <summary>
/// Modifies the behavior of the Eigong boss.
/// </summary>
[RequireComponent(typeof(StealthGameMonster))]
internal class Eigong : MonoBehaviour {
    [Auto(false)]
    private StealthGameMonster _monster = null!;
    private RuntimeAnimatorController _newController = null!;

    private void Awake() {
        _monster.OverrideWanderingIdleTime(0);
        _monster.StartingPhaseIndex = 1;

        var monsterCore = _monster.monsterCore;

        AssetManager.TryGet("Inner Eigong Controller", out _newController);

        SetupGunAttack();

        ResetMonster();
    }

    private void CreateTrackingSlashes() {
        AssetManager.Inst<GameObject>("Tracking Slashes", null);
    }

    private const string GunStateAnimation = "Gun Prepare";

    private OldPivotRotate _armRotate = null!;
    private LaserAttackController _laserAttack = null!;

    private void SetupGunAttack() {
        var body = _monster.monsterCore.transform.Find("Animator(Proxy)/Animator/View/YiGung/Body");
        var arm = AssetManager.Inst<GameObject>("Arm", body);
        arm.name = arm.name.Replace("(Clone)", "");
        arm.transform.localPosition = new Vector2(20, -20);
        var armRenderer = arm.GetComponent<SpriteRenderer>();
        armRenderer.sortingLayerName = "Monster";
        var overlayer = arm.AddComponent<ColorKeyOverlayer>();
        overlayer.OverlayScale = 100;
        overlayer.Tolerance = 0.1f;
        overlayer.Smoothing = 0.05f;
        _armRotate = arm.AddComponent<OldPivotRotate>();
        _armRotate.inverseAngle = true;
        _armRotate.KeepLookAtPlayer = true;
        _armRotate.minRotate = -360;
        _armRotate.maxRotate = 360;
        _armRotate.offset = 0;
        _armRotate.referenceActor = _monster;
        if (PreloadManager.TryGet("Sniper", out var sniperRef)) {
            var sniperCoreRef = sniperRef.transform.Find("Dragon_Sniper/DragonSniper/RotateRArm/RArm/Bow/SniperLaserCore").gameObject;
            var sniperCore = Instantiate(sniperCoreRef, arm.transform);
            sniperCore.name = sniperCore.name.Replace("(Clone)", "");
            sniperCore.transform.localPosition = new Vector2(-38, 5.3f);
            sniperCore.transform.localScale = new Vector3(-1, 1, 1);
            foreach (var renderer in sniperCore.GetComponentsInChildren<Renderer>(true)) {
                renderer.sortingLayerName = "Monster";
                renderer.sortingOrder = armRenderer.sortingOrder - 100;
            }
            var parriableOwner = sniperCore.AddComponent<GeneralParriableOwner>();
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
            gunBossState.BindingAnimation = GunStateAnimation;
            gunBossState.state = Constants.GunMonsterState;
            AssetManager.TryGet(GunStateAnimation, out gunBossState.clip);

            var gunStateParent = gunStateObj.transform;
            var linkNextMoveWeightObj = new GameObject("weight");
            linkNextMoveWeightObj.transform.SetParent(gunStateParent);
            var linkNextMoveWeightComp = linkNextMoveWeightObj.AddComponent<LinkNextMoveStateWeight>();
            var gunStateWeight = new AttackWeight {
                state = gunBossState,
                weight = 1
            };
            var engagingState = _monster.GetComponentInChildren<StealthEngaging>();
            var engagingStateWeight = new AttackWeight {
                state = engagingState,
                weight = 1
            };
            linkNextMoveWeightComp.stateWeightList = [engagingStateWeight];

            foreach (var monsterState in attackStates.GetComponentsInChildren<BossGeneralState>(true)) {
                if (!monsterState.state.ToString().Contains("Attack")) continue;
                if (monsterState.state is States.Attack3 or States.Attack10 or States.Attack15 or States.Attack16) continue;
                foreach (var linkNextMoveWeight in monsterState.GetComponentsInChildren<LinkNextMoveStateWeight>(true)) {
                    linkNextMoveWeight.stateWeightList.Add(gunStateWeight);
                }
            }

            var sniperAudioRef = sniperRef.transform.Find("LogicRoot/Audio/EnemySFX_Sniper_Attack").gameObject;
            var sniperAudio = Instantiate(sniperAudioRef, _monster.monsterCore.logicRoot.Find("Audio"));
            sniperAudio.name = sniperAudio.name.Replace("(Clone)", "");
            var sniperSound = sniperAudio.GetComponent<SoundPlayer>();
            AkBankManager.LoadBank("Dragon_Sniper", false, false, sniperSound);
            sniperSound.EnterLevelReset();

            foreach (var linkNextMoveWeight in engagingState.GetComponentsInChildren<LinkNextMoveStateWeight>(true)) {
                linkNextMoveWeight.stateWeightList.Add(gunStateWeight);
            }
        }
    }

    private void StopArmFollow() {
        _armRotate.KeepLookAtPlayer = false;
    }

    private void RestartArmFollow() {
        _armRotate.KeepLookAtPlayer = true;
    }

    /// <summary>
    /// Fire the laser.
    /// </summary>
    internal async UniTask FireLaser() {
        var animator = _monster.animator;
        var oldController = animator.runtimeAnimatorController;
        animator.runtimeAnimatorController = _newController;
        animator.Play(0);
        RestartArmFollow();
        await UniTask.Delay(TimeSpan.FromSeconds(1.75f));
        // Laser 1
        StopArmFollow();
        _monster.FacePlayer();
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        RestartArmFollow();
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        // Laser 2
        StopArmFollow();
        _monster.FacePlayer();
        await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
        RestartArmFollow();
        await UniTask.Delay(TimeSpan.FromSeconds(0.15f));
        // Laser 3
        StopArmFollow();
        _monster.FacePlayer();
        await UniTask.Delay(TimeSpan.FromSeconds(1));
        animator.runtimeAnimatorController = oldController;
        _monster.ChangeStateIfValid(States.Engaging);
        RestartArmFollow();
    }

    private void ResetMonster() {
        AutoAttributeManager.AutoReferenceAllChildren(gameObject);

        Traverse.Create(_monster).Field("_inited").SetValue(false);
        _monster.Invoke("CheckInit", 0);
        _monster.EnterLevelReset();
    }
}