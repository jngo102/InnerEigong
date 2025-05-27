using System.Linq;
using UnityEngine;

namespace InnerEigong;

/// <summary>
/// Fixes broken behavior when switching to and from a custom runtime animator controller.
/// </summary>
[RequireComponent(typeof(Animator))]
internal class AnimatorFixer : MonoBehaviour {
    private MonsterBase _monster = null!;
    private GameObject _phase2Activator = null!;
    private GameObject[] _activatorChildren = null!;
    private bool[] _previousActiveChildren = null!;

    private void Awake() {
        _monster = GetComponentInParent<MonsterBase>();
        if (_monster) {
            _phase2Activator = _monster.monsterCore.logicRoot.Find("Phase2 Activator").gameObject;
            if (_phase2Activator) {
                _activatorChildren = _phase2Activator.transform.GetComponentsInChildren<FxPlayer>(true).Select(child => child.gameObject).ToArray();
                _previousActiveChildren = _activatorChildren.Select(child => child.gameObject.activeSelf).ToArray();
            }
        }
    }

    private void Update() {
        for (var i = 0; i < _activatorChildren.Length; i++) {
            if (_activatorChildren[i].activeSelf != _previousActiveChildren[i]) {
                _phase2Activator.SetActive(true);
                _previousActiveChildren = _activatorChildren.Select(child => child.activeSelf).ToArray();
                break;
            }
        }
    }
}