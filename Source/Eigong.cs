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
internal class Eigong : MonoBehaviour {
    private StealthGameMonster _monster = null!;
    private RuntimeAnimatorController _newController = null!;

    private void Awake() {
        TryGetComponent(out _monster);
        _monster.OverrideWanderingIdleTime(0);
        _monster.StartingPhaseIndex = 1;

        var monsterCore = _monster.monsterCore;

        AssetManager.TryGet("Inner Eigong Controller", out _newController);

        SetupGunAttack();

        ResetMonster();

        Log.Debug("DONE AWAKE");
    }

    private StateMachine<States> _stateMachine = null!;

    private async void Start() {
        await UniTask.WaitUntil(() => _monster.fsm != null);

        _stateMachine = _monster.fsm;
        var runner = _stateMachine.runner;

        var slowStartFullCombo = _monster.GetState(States.Attack1);
        var teleportToBigWhiteFlash = _monster.GetState(States.Attack2);
        var issenSlash = _monster.GetState(States.Attack3);
        var uppercutToWhiteSlash = _monster.GetState(States.Attack4);
        var teleportBack = _monster.GetState(States.Attack5);
        var slowStartTrailingCombo = _monster.GetState(States.Attack6);
        var teleportToSlowStartTrailingCombo = _monster.GetState(States.Attack7);
        var sheathToRedWhiteWhite = _monster.GetState(States.Attack8);
        var guardToSlowStartOrTriplePoke = _monster.GetState(States.Attack9);
        var faceAndChargeTalisman = _monster.GetState(States.Attack10);
        var redWhiteWhite = _monster.GetState(States.Attack11);
        var uppercutToFirePillar = _monster.GetState(States.Attack12);
        var triplePoke = _monster.GetState(States.Attack13);
        var chargeBigBall = _monster.GetState(States.Attack14);
        var chargeToTurnTalisman = _monster.GetState(States.Attack15);
        var faceTalisman = _monster.GetState(States.Attack16);
        var overheadToIssenSlashOrTalisman = _monster.GetState(States.Attack17);
        var farTeleportToChargeTalisman = _monster.GetState(States.Attack18);
        var teleportToBigRedCut = _monster.GetState(States.Attack19);
        var bigWhiteFlash = _monster.GetState(States.Attack20);

        // foreach (var groupSequence in GetComponentsInChildren<MonsterStateGroupSequence>(true)) {
        //     var attackSequences = groupSequence.AttackSequence;
        //     foreach (var attackSequence in attackSequences) {
        //         attackSequence.setting.queue = [slowStartFullCombo, issenSlash, redWhiteWhite];   
        //     }
        //     groupSequence.AttackSequence = attackSequences;
        // }
        //
        // var attackSequencer = _monster.monsterCore.attackSequenceMoodule;
        // var phaseSequencesField = attackSequencer.GetType()
        //     .GetField("SequenceForDifferentPhase", BindingFlags.Instance | BindingFlags.NonPublic);
        // if (phaseSequencesField != null) {
        //     var phaseSequences = (MonsterStateSequenceWeight[])phaseSequencesField.GetValue(attackSequencer);
        //     foreach (var phaseSequence in phaseSequences) {
        //         foreach (var groupSequence in phaseSequence.setting.queue) {
        //             var attackSequences = groupSequence.AttackSequence;
        //             foreach (var attackSequence in attackSequences) {
        //                 var setting = attackSequence.setting;
        //                 setting.queue = [slowStartFullCombo, issenSlash, redWhiteWhite];
        //                 attackSequence.setting = setting;
        //             }
        //
        //             groupSequence.AttackSequence = attackSequences;
        //         }
        //     }    
        //     phaseSequencesField.SetValue(attackSequencer, phaseSequences);
        // }
    }

    private void CreateTrackingSlashes() {
        AssetManager.Inst<GameObject>("Tracking Slashes", null);
    }

    private const string GunStateAnimation = "Gun Prepare";
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

    private OldPivotRotate _armRotate = null!;

    private void StopArmFollow() {
        _armRotate.KeepLookAtPlayer = false;
    }

    private void RestartArmFollow() {
        _armRotate.KeepLookAtPlayer = true;
    }

    internal async UniTask FireGun() {
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