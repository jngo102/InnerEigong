using UnityEngine;

namespace InnerEigong;

/// <summary>
/// Overlays a texture over a specific color.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ColorKeyOverlayer : MonoBehaviour {
    private SpriteRenderer _spriteRenderer;
    
    private static readonly int OverlayScaleID = Shader.PropertyToID("_OverlayScale");
    private static readonly int ToleranceID = Shader.PropertyToID("_Tolerance");
    private static readonly int SmoothingID = Shader.PropertyToID("_Smoothing");

    /// <summary>
    /// The amount of tolerance provided to matching the target key color.
    /// </summary>
    internal float Tolerance {
        get => _spriteRenderer.material.GetFloat(ToleranceID);
        set => _spriteRenderer.material.SetFloat(ToleranceID, value);
    }

    /// <summary>
    /// The amount of blending between colors around the keyed color.
    /// </summary>
    internal float Smoothing {
        get => _spriteRenderer.material.GetFloat(SmoothingID);
        set => _spriteRenderer.material.SetFloat(SmoothingID, value);
    }

    /// <summary>
    /// The scale of the overlaid texture.
    /// </summary>
    internal float OverlayScale {
        get => _spriteRenderer.material.GetFloat(OverlayScaleID);
        set => _spriteRenderer.material.SetFloat(OverlayScaleID, value);
    }

    private void Awake() {
        TryGetComponent(out _spriteRenderer);
        if (AssetManager.TryGet<Material>("_2dxFX_ColorKeyOverlay", out var colorKeyOverlayMaterial)) {
            _spriteRenderer.material = colorKeyOverlayMaterial;
        }
    }
}