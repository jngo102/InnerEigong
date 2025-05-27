using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using HarmonyLib;
using UnityEngine;
using Random = System.Random;

namespace InnerEigong;

/// <summary>
/// Represents an enemy doppelgänger that attacks alongside the actual enemy.
/// </summary>
[RequireComponent(typeof(MonsterBase))]
internal class Phantom : MonoBehaviour {
    /// <summary>
    /// Cached <see cref="MonsterBase">monster</see> component.
    /// </summary>
    private MonsterBase _monster;

    private Animator Anim => _monster.animator;

    private void Start() {
        AutoAttributeManager.AutoReferenceAllChildren(gameObject);

        TryGetComponent(out _monster);
        _monster.EnterLevelAwake();
        _monster.EnterLevelReset();
        
        // Destroy damage receivers so phantom cannot take damage 
        foreach (var decreasePostureReceiver in _monster.damageReceivers) {
            foreach (var effectReceiver in Traverse.Create(decreasePostureReceiver).Field<EffectReceiver[]>("effectReceivers").Value) {
                Destroy(effectReceiver.gameObject);
            }
        }
        // foreach (var monsterPushAway in Anim.GetComponentsInChildren<MonsterPushAway>(true)) {
        //     Destroy(monsterPushAway.gameObject);
        // }

        // Make sprite colors inverted
        var yiGungView = Anim.transform.Find("View/YiGung");
        var body = yiGungView.Find("Body");
        body.gameObject.AddComponent<_2dxFX_Negative>();
        Destroy(body.GetComponent<_2dxFX_ColorRGB>());
        var arm = body.Find("Arm");
        arm.gameObject.AddComponent<_2dxFX_Negative>();
        var swordSprite = yiGungView.Find("Weapon/Sword/Sword Sprite");
        swordSprite.gameObject.AddComponent<_2dxFX_Negative>();

        _monster.Hide();
    }

    /// <summary>
    /// Generate a new <see cref="Guid">GUID</see> to make this phantom unique from its origin enemy.
    /// </summary>
    /// <param name="random">The object to randomize the phantom's <see cref="Guid">GUID</see>.</param>
    internal void ScrambleGuid(Random random) {
        var guid = gameObject.GetGuidComponent();
        var guidType = guid.GetType();
        var bytes = new byte[16];
        random.NextBytes(bytes);
        var newGuid = new Guid(bytes);
        var guidTraverse = Traverse.Create(guidType);
        guidTraverse.Field<Guid>("guid").Value = newGuid;
        guidTraverse.Field<byte[]>("serializedGuid").Value = newGuid.ToByteArray();
        guid.Invoke("CreateGuid", 0);
    }

    /// <summary>
    /// Fade in the <see cref="Phantom">phantom</see> at a monster's position.
    /// </summary>
    /// <param name="refMonster">The original monster's <see cref="MonsterBase">monster</see> component.</param>
    /// <param name="spawnCancelToken">A <see cref="CancellationToken">cancellation token</see> that may stop the spawn task.</param>
    /// <param name="spawnDelaySeconds">The duration before when the clone actually spawns.</param>
    internal async UniTask Spawn(MonsterBase refMonster, CancellationToken spawnCancelToken,
        float spawnDelaySeconds = 0.25f) {
        transform.position = refMonster.transform.position;
        var currentState = refMonster.CurrentState;
        await UniTask.Delay(TimeSpan.FromSeconds(spawnDelaySeconds), cancellationToken: spawnCancelToken);
        _monster.Show();
        _monster.health.SetReceiversActivate(false);
        _monster.health.BecomeInvincible(_monster);
        _monster.ChangeStateIfValid(currentState);
        await FadeIn(spawnCancelToken, spawnDelaySeconds);
        await UniTask.WaitUntil(() => _monster.CurrentState != currentState, cancellationToken: spawnCancelToken);
        await FadeOut(spawnCancelToken, spawnDelaySeconds);
    }

    /// <summary>
    /// Fade in the <see cref="Phantom">phantom</see>.
    /// </summary>
    /// <param name="fadeCancelToken">A <see cref="CancellationToken">cancellation token</see> that may stop this fade in routine.</param>
    /// <param name="fadeTimeSec">The duration in seconds to fade in for.</param>
    private async UniTask FadeIn(CancellationToken fadeCancelToken, float fadeTimeSec = 0.25f) {
        var fadeStartTime = Time.timeSinceLevelLoad;
        float alpha = 0;
        await UniTask.WaitUntil(() => {
            foreach (var fx in GetComponentsInChildren<_2dxFX_ColorRGB>(true)) {
                fx._Alpha = alpha;
            }

            foreach (var fx in GetComponentsInChildren<_2dxFX_Negative>(true)) {
                fx._Alpha = alpha;
            }

            alpha = (Time.timeSinceLevelLoad - fadeStartTime) / fadeTimeSec;
            return alpha >= 1;
        }, cancellationToken: fadeCancelToken);
    }

    /// <summary>
    /// Fade out the <see cref="Phantom">phantom</see>.
    /// </summary>
    /// <param name="fadeCancelToken">A <see cref="CancellationToken">cancellation token</see> that may stop this fade out routine.</param>
    /// <param name="fadeTimeSec">The duration in seconds to fade out for.</param>
    internal async UniTask FadeOut(CancellationToken fadeCancelToken, float fadeTimeSec = 0.25f) {
        float alpha = 1;
        float fadeStartTime = Time.timeSinceLevelLoad;
        await UniTask.WaitUntil(() => {
            foreach (var fx in GetComponentsInChildren<_2dxFX_ColorRGB>(true)) {
                fx._Alpha = alpha;
            }

            foreach (var fx in GetComponentsInChildren<_2dxFX_Negative>(true)) {
                fx._Alpha = alpha;
            }

            alpha = 1 - (Time.timeSinceLevelLoad - fadeStartTime) / fadeTimeSec;
            return alpha <= 0;
        }, cancellationToken: fadeCancelToken);
        _monster.Hide();
    }
}