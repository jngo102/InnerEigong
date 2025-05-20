using UnityEngine;
using UnityEngine.Events;

namespace InnerEigong;

/// <summary>
/// Sets up the tracking slashes attack.
/// </summary>
internal class TrackingSlashes : MonoBehaviour {
    private void Awake() {
        var follower = gameObject.TryGetCompOrAdd<PlayerPosFollower>();
        follower.followMode = PlayerPosFollower.FollowMode.Instant;
        follower.IsForceFollowPlayer = true;
        for (var i = 1; i <= 5; i++) {
            var slash = transform.Find($"Slash {i}");
            var slashObj = slash.gameObject;
            var damageScalar = slashObj.AddComponent<DamageScalarSource>();
            var parriableOwner = slashObj.AddComponent<GeneralParriableOwner>();
            var slashPoolObjComp = slashObj.AddComponent<PoolObject>();

            var animator = GetComponent<Animator>();
            var clip = animator.runtimeAnimatorController.animationClips[0];
            var animEvent = clip.events[0];
            animEvent.functionName = nameof(DestroySelf);
            clip.events = [animEvent];

            var parriableObj = slash.Find("Parriable").gameObject;
            var parriableEffect = parriableObj.AddComponent<ParriableAttackEffect>();
            parriableEffect.param = new ParryParam {
                knockBackType = KnockBackType.None,
                knockBackValue = 0,
                hurtLiftType = HurtType.HurtHitToGround,
            };
            var effectReceiver = parriableObj.AddComponent<EffectReceiver>();
            effectReceiver.effectType = EffectType.ParryEffect;
            effectReceiver.facingRule = FacingDetectionRule.IgnoreFacing;
            effectReceiver.OnEmitEffectEvent = new PosEvent();
            effectReceiver.OnHitEnterEvent = new EffectHitEvent();
            effectReceiver.OnHitExitEvent = new EffectHitEvent();
            effectReceiver.OnHitRaw = new UnityEvent();
            effectReceiver.OnHitStayEvent = new EffectHitEvent();

            var dmgArea = slash.Find("DamageArea");
            var dmgAreaObj = dmgArea.gameObject;
            var effectDealer = dmgAreaObj.AddComponent<EffectDealer>();
            effectDealer.OnEffectEnter = new EffectHitEvent();
            effectDealer.OnEffectStay = new EffectHitEvent();
            effectDealer.OnEmitEffectEvent = new EffectHitEvent();
            effectDealer.type = EffectType.EnemyAttack | EffectType.BreakableBreaker;
            var damageDealer = dmgAreaObj.AddComponent<DamageDealer>();
            damageDealer.bindingParry = parriableEffect;
            damageDealer.type = DamageType.MonsterAttack;
            parriableEffect.bindDamage = damageDealer;

            var triggerDetector = dmgAreaObj.AddComponent<TriggerDetector>();
            triggerDetector.Invoke("Awake", 0);

            var hitResultPlayer = dmgArea.Find("hit Result Player");
            hitResultPlayer.gameObject.AddComponent<EffectDealerHitResultPlayer>();
            var fxPlayer = hitResultPlayer.Find("fxPlayer");
            fxPlayer.gameObject.AddComponent<FxPlayer>();
        }

        AutoAttributeManager.AutoReferenceAllChildren(gameObject);
        foreach (var levelAwake in GetComponentsInChildren<ILevelAwake>(true)) {
            levelAwake.EnterLevelAwake();
        }
        foreach (var resetter in GetComponentsInChildren<IResetter>(true)) {
            resetter.EnterLevelReset();
        }

        var parentPoolObjComp = gameObject.AddComponent<PoolObject>();
        foreach (var poolObj in GetComponentsInChildren<IPoolObject>(true)) {
            poolObj.PoolOnPrepared(parentPoolObjComp);
        }
    }

    private void DestroySelf() {
        Destroy(gameObject);
    }
}