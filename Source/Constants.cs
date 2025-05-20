using UnityEngine;
using UnityEngine.SceneManagement;

namespace InnerEigong;

/// <summary>
/// Defines shared constant values across all mod classes.
/// </summary>
internal static class Constants {
    /// <summary>
    /// The name of the <see cref="Scene">scene</see> containing the Eigong boss fight.
    /// </summary>
    internal const string BossSceneName = "A11_S0_Boss_YiGung";
    /// <summary>
    /// The <see cref="GameObject.name">name</see> of the Eigong boss <see cref="GameObject">game object</see>.
    /// </summary>
    internal const string BossName = "Boss_Yi Gung";
    /// <summary>
    /// The gun firing monster state.
    /// </summary>
    internal const MonsterBase.States GunMonsterState = MonsterBase.States.AttackParrying2;
}