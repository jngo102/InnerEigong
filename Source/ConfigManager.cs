using System.Collections.Generic;
using BepInEx.Configuration;

namespace InnerEigong;

/// <summary>
/// Manages configuration of the mod.
/// </summary>
internal static class ConfigManager {
    private static readonly string BossModifierSectionName = "Boss.Modifiers"; 
    
    private static ConfigEntry<int>? _numberOfPhantomsBackingfield;

    /// <summary>
    /// The number of Eigong phantoms to possibly spawn for certain attacks.
    /// </summary>
    internal static int NumberOfPhantoms => _numberOfPhantomsBackingfield.Value;

    private static ConfigEntry<float>? _phantomSpawnChanceBackingField;

    /// <summary>
    /// The chance that a phantom may spawn for any of a certain number of attacks. Ranges from 0 (never spawn) to 1 (always spawn).
    /// </summary>
    internal static float PhantomSpawnChance => _phantomSpawnChanceBackingField.Value;

    /// <summary>
    /// Initialize the configuration manager.
    /// </summary>
    /// <param name="config">The reference <see cref="ConfigFile">configuration file</see>.</param>
    internal static void Initialize(ConfigFile config) {
        _numberOfPhantomsBackingfield = config.Bind(
            BossModifierSectionName,
            "Number of Phantoms",
            1,
            new ConfigDescription(
                "The number of Eigong phantoms to possibly spawn for certain attacks. Capped at 4 phantoms.",
                new AcceptableValueRange<int>(0, 4)
            )
        );

        _phantomSpawnChanceBackingField = config.Bind(
            BossModifierSectionName,
            "Phantom Spawn Chance",
            0.5f,
            new ConfigDescription(
                "The chance that a phantom may spawn for any of a certain number of attacks. Ranges from 0 (never spawn) to 1 (always spawn).",
                new AcceptableValueRange<float>(0, 1)
            )
        );
    }
}