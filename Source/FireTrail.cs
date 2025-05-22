using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PoolManager;

namespace InnerEigong;

/// <summary>
/// Leaves a trail of fire at the actor's feet.
/// </summary>
internal class FireTrail : MonoBehaviour {
    internal float flameSpacing = 35;

    private ActorBody _body = null!;
    private Collider2D _collider = null!;
    private ObjectPool _firePool = null!;

    private readonly List<PoolObject> _flames = [];

    private void Awake() {
        _body = GetComponentInChildren<ActorBody>(true);
        _collider = _body.GetComponent<Collider2D>();
    }

    private IEnumerator Start() {
        yield return new WaitUntil(() => {
            _firePool = PoolManager.Instance.allPools.FirstOrDefault(pool => pool._prefab.name == "Fire_FX_damage_Long jiechuan");
            return _firePool != null;
        });
    }

    private void Update() {
        if (!_body.actor.IsOnGround || _firePool == null) return;
        var colCenterX = _collider.bounds.center.x;
        var colBottomY = _collider.bounds.min.y;
        if (_flames.Count > 0) {
            var closestFlame = _flames.Aggregate(_flames[0], (closest, nextFlame) => {
                var nextDist = Mathf.Abs(nextFlame.transform.position.x - transform.position.x);
                var closeDist = Mathf.Abs(closest.transform.position.x - transform.position.x);
                return nextDist < closeDist ? nextFlame : closest;
            });
            var closestOffsetX = transform.position.x - closestFlame.transform.position.x;
            var closestDistance = Mathf.Abs(closestOffsetX);
            if (closestDistance <= flameSpacing) return;
            var spawnDirection = Mathf.Sign(closestOffsetX);
            var spawnX = closestFlame.transform.position.x + flameSpacing * spawnDirection;
            var condition = spawnDirection > 0 ? spawnX <= colCenterX : spawnX >= colCenterX;
            while (condition) {
                var flameSpawnPos = new Vector2(spawnX, colBottomY);
                SpawnFlame(flameSpawnPos);
                spawnX += flameSpacing * spawnDirection;
                condition = spawnDirection > 0 ? spawnX <= colCenterX : spawnX >= colCenterX;
            }
        } else {
            var flameSpawnPos = new Vector2(colCenterX, colBottomY);
            SpawnFlame(flameSpawnPos);
        }
    }

    /// <summary>
    /// Spawn a Jiequan flame.
    /// </summary>
    /// <param name="position">The position to spawn the flame at.</param>
    internal void SpawnFlame(Vector2 position) {
        var flame = _firePool.Borrow(position, Quaternion.identity);
        if (!flame) return;
        flame.OnReturnEvent.AddListener((obj) => {
            if (_flames.Contains(obj)) {
                _flames.Remove(obj);
            }
        });
        _flames.Add(flame);
    }

    /// <summary>
    /// Reset the flames list.
    /// </summary>
    internal void ResetFlames() {
        _flames.Clear();
    }
}