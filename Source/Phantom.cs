using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace InnerEigong;

/// <summary>
/// Represents an enemy doppelgänger that attacks alongside the actual enemy.
/// </summary>
internal class Phantom : MonoBehaviour {
    /// <summary>
    /// Cached <see cref="MonsterBase">monster</see> component.
    /// </summary>
    private MonsterBase _monster;
    
    private void Start() {
        AutoAttributeManager.AutoReferenceAllChildren(gameObject);

        TryGetComponent(out _monster);
        _monster.EnterLevelAwake();
        _monster.EnterLevelReset();

        foreach (var rend in GetComponentsInChildren<SpriteRenderer>()) {
            if (rend.name.Contains("Shadow")) continue;
            rend.TryGetCompOrAdd<_2dxFX_ColorRGB>();
            rend.TryGetCompOrAdd<_2dxFX_Negative>();
        }
        
        _monster.Hide();
    }

    /// <summary>
    /// Generate a new <see cref="Guid">GUID</see> to make this phantom unique from its origin enemy.
    /// </summary>
    /// <param name="seed">A unique seed for <see cref="Guid">GUID</see> randomization.</param>
    internal void ScrambleGuid(int seed) {
        var guid = gameObject.GetGuidComponent();
        var guidType = guid.GetType();
        var bytes = new byte[16];
        var random = new System.Random(seed);
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