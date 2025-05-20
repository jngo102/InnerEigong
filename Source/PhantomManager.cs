using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NineSolsAPI.Utils;
using UnityEngine;
using UObj = UnityEngine.Object;

namespace InnerEigong;

/// <summary>
/// Manages the <see cref="Phantom">phantom</see> clone of a <see cref="MonsterBase">monster</see>.
/// </summary>
internal static class PhantomManager {
    private static List<Phantom> _phantoms;

    /// <summary>
    /// Initialize the <see cref="PhantomManager">phantom manager</see>.
    /// </summary>
    /// <param name="refObj">A reference <see cref="GameObject">game object</see> that the <see cref="Phantom">phantom</see> will be a clone of and that it will spawn to.</param>
    /// <param name="numPhantoms">The number of <see cref="Phantom">phantoms</see> to spawn.</param>
    internal static void Initialize(GameObject refObj, int numPhantoms = 1) {
        _phantoms = new List<Phantom>(numPhantoms);
        for (var i = 0; i < numPhantoms; i++) {
            var phantomObj = ObjectUtils.InstantiateInit(refObj);
            phantomObj.SetActive(false);
            phantomObj.transform.position = refObj.transform.position;
            var phantom = phantomObj.AddComponent<Phantom>();
            phantom.ScrambleGuid(i);
            phantomObj.SetActive(true);
            _phantoms.Add(phantom);
        }
    }

    private static List<CancellationTokenSource> _spawnCancelSrcs = [];

    /// <summary>
    /// Spawn <see cref="Phantom">phantoms</see> at the reference <see cref="MonsterBase">monster</see>.
    /// </summary>
    /// <param name="refMonster">The <see cref="MonsterBase">monster</see> to spawn the <see cref="Phantom">phantom</see> at.</param>
    /// <param name="spawnLengthSec">The duration in seconds that the <see cref="Phantom">phantoms</see> will spawn for.</param>
    internal static async UniTask SpawnPhantoms(MonsterBase refMonster, float spawnLengthSec = 0.25f) {
        var spawnInterval = spawnLengthSec / _phantoms.Count;
        _spawnCancelSrcs ??= new List<CancellationTokenSource>(_phantoms.Count);
        
        List<UniTask> spawnTasks = new(_phantoms.Count);
        for (var i = 0; i < _phantoms.Count; i++) {
            var phantom = _phantoms[i];
            CancellationTokenSource spawnCancelSrc;
            if (_spawnCancelSrcs.Count > i) {
                spawnCancelSrc = _spawnCancelSrcs[i];
                if (spawnCancelSrc != null) {
                    spawnCancelSrc.Cancel();
                    _spawnCancelSrcs.Remove(spawnCancelSrc);
                }   
            }
            spawnCancelSrc = new CancellationTokenSource();
            _spawnCancelSrcs.Insert(i, spawnCancelSrc);
            try {
                spawnTasks.Add(phantom.Spawn(refMonster, spawnCancelSrc.Token, spawnInterval));
            } catch (OperationCanceledException) {
                spawnCancelSrc.Cancel();
                spawnTasks.RemoveAt(i);
                spawnTasks.Add(phantom.FadeOut(spawnCancelSrc.Token, spawnInterval));
            }
            await UniTask.Delay(TimeSpan.FromSeconds(spawnInterval), cancellationToken: spawnCancelSrc.Token);
        }

        
        await UniTask.WhenAll(spawnTasks);
    }
}