using BepInEx.Configuration;

namespace InnerEigong;

/// <summary>
/// Manages configuration of the mod.
/// </summary>
internal static class ConfigManager {
    private static readonly string BossModifierSectionName = "Boss.Modifiers";

    private static ConfigEntry<int>? _numberOfPhantoms;

    /// <summary>
    /// The number of Eigong phantoms to possibly spawn for certain attacks.
    /// </summary>
    internal static int NumberOfPhantoms => _numberOfPhantoms.Value;

    private static ConfigEntry<float>? _phantomSpawnChance;

    /// <summary>
    /// The chance that a phantom may spawn for any of a certain number of attacks. Ranges from 0 (never spawn) to 1 (always spawn).
    /// </summary>
    internal static float PhantomSpawnChance => _phantomSpawnChance.Value;

#if DEBUG
    private static ConfigEntry<int>? _startingPhase;

    /// <summary>
    /// The phase to start the boss fight at, ranging from 1 to 3.
    /// </summary>
    internal static int StartingPhase => _startingPhase.Value;

    private static ConfigEntry<float>? _bossHealth;
    /// <summary>
    /// The amount of health to start each boss phase at.
    /// </summary>
    internal static float BossHealth => _bossHealth.Value;
    
    private static ConfigEntry<bool>? _invincible;
    /// <summary>
    /// Whether to not take any hits during the boss fight.
    /// </summary>
    internal static bool Invincible => _invincible.Value;
#endif

    /// <summary>
    /// Initialize the configuration manager.
    /// </summary>
    /// <param name="config">The reference <see cref="ConfigFile">configuration file</see>.</param>
    internal static void Initialize(ConfigFile config) {
        _numberOfPhantoms = config.Bind(
            BossModifierSectionName,
            "Number of Phantoms",
            1,
            new ConfigDescription(
                "The number of Eigong phantoms to possibly spawn for certain attacks. Capped at 4 phantoms.",
                new AcceptableValueRange<int>(0, 4)
            )
        );

        _phantomSpawnChance = config.Bind(
            BossModifierSectionName,
            "Phantom Spawn Chance",
            0.5f,
            new ConfigDescription(
                "The chance that a phantom may spawn for any of a certain number of attacks. Ranges from 0 (never spawn) to 1 (always spawn).",
                new AcceptableValueRange<float>(0, 1)
            )
        );

#if DEBUG
        _startingPhase = config.Bind(
            BossModifierSectionName,
            "Starting Phase",
            1,
            new ConfigDescription(
                "The phase to start the boss fight at, ranging from 1 to 3.",
                new AcceptableValueRange<int>(1, 3)
            )
        );
        
        _bossHealth = config.Bind(
            BossModifierSectionName,
            "Boss Health",
            200f,
            new ConfigDescription(
                "The amount of health to start each boss phase at."
            )
        );
        
        _invincible = config.Bind(
            BossModifierSectionName,
            "Invincible",
            true,
            new ConfigDescription(
                "Whether to not take any hits during the boss fight."
            )
        );
#endif
    }
}