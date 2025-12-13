using UnityEngine;
using AllIn1SpriteShader;

public class HighlightUnit : MonoBehaviour
{
    private Unit unit;
    private GameManager gameManager;
    private SpriteRenderer spriteRenderer;
    private UnityEngine.UI.Image uiImage;
    private Renderer rendererComponent;
    private Material originalMaterial;
    private Material combinedMaterial; // Temporary material that combines original + highlight
    private bool isHighlighted = false;

    void Start()
    {
        // Get the Unit component on this GameObject
        unit = GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogWarning("HighlightUnit: No Unit component found on " + gameObject.name);
        }

        // Find GameManager
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("HighlightUnit: GameManager not found in scene!");
        }

        // Get the renderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<UnityEngine.UI.Image>();
        rendererComponent = GetComponent<Renderer>();

        // Store the original material
        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }
        else if (uiImage != null)
        {
            originalMaterial = uiImage.material;
        }
        else if (rendererComponent != null)
        {
            originalMaterial = rendererComponent.material;
        }
        else
        {
            Debug.LogWarning("HighlightUnit: No renderer found on " + gameObject.name + ". Need SpriteRenderer, Image, or Renderer component.");
        }
    }

    void Update()
    {
        if (unit == null || gameManager == null)
            return;

        // Check if highlighting is enabled
        if (!gameManager.enableUnitHighlighting)
        {
            // If highlighting is disabled and we're currently highlighted, revert
            if (isHighlighted)
            {
                RevertToOriginalMaterial();
            }
            return;
        }

        if (gameManager.highlightMaterial == null)
            return;

        // Check if this unit is the current unit
        Unit currentUnit = gameManager.GetCurrentUnit();
        bool shouldBeHighlighted = (currentUnit == unit);

        // Apply or remove material based on turn
        if (shouldBeHighlighted && !isHighlighted)
        {
            ApplyHighlightMaterial();
        }
        else if (!shouldBeHighlighted && isHighlighted)
        {
            RevertToOriginalMaterial();
        }
    }

    private void ApplyHighlightMaterial()
    {
        // Apply highlight properties onto the original material
        if (originalMaterial != null && gameManager.highlightMaterial != null)
        {
            // Create a new material instance from the original material to preserve it
            combinedMaterial = new Material(originalMaterial);
            
            // Copy highlight/outline properties from the highlight material onto the original
            CopyHighlightProperties(gameManager.highlightMaterial, combinedMaterial);
        }
        else if (gameManager.highlightMaterial != null)
        {
            // If no original material, just use the highlight material directly
            combinedMaterial = gameManager.highlightMaterial;
        }
        else
        {
            return; // No materials available
        }

        // Apply the combined material
        if (spriteRenderer != null)
        {
            spriteRenderer.material = combinedMaterial;
        }
        else if (uiImage != null)
        {
            uiImage.material = combinedMaterial;
        }
        else if (rendererComponent != null)
        {
            rendererComponent.material = combinedMaterial;
        }
        isHighlighted = true;
    }

    /// <summary>
    /// Copies highlight/outline properties from highlight material to destination material
    /// This adds the outline effect onto the original material while preserving all original properties
    /// </summary>
    private void CopyHighlightProperties(Material highlightSource, Material destination)
    {
        if (highlightSource == null || destination == null) return;

        // Ensure the destination material uses the same shader as the highlight (AllIn1SpriteShader)
        // This is important so it supports outline properties
        if (highlightSource.shader != null)
        {
            destination.shader = highlightSource.shader;
        }

        // Copy outline properties from highlight material
        if (highlightSource.HasProperty("_OutlineColor") && destination.HasProperty("_OutlineColor"))
        {
            destination.SetColor("_OutlineColor", highlightSource.GetColor("_OutlineColor"));
        }

        if (highlightSource.HasProperty("_OutlineAlpha") && destination.HasProperty("_OutlineAlpha"))
        {
            destination.SetFloat("_OutlineAlpha", highlightSource.GetFloat("_OutlineAlpha"));
        }

        if (highlightSource.HasProperty("_OutlineGlow") && destination.HasProperty("_OutlineGlow"))
        {
            destination.SetFloat("_OutlineGlow", highlightSource.GetFloat("_OutlineGlow"));
        }

        if (highlightSource.HasProperty("_OutlineWidth") && destination.HasProperty("_OutlineWidth"))
        {
            destination.SetFloat("_OutlineWidth", highlightSource.GetFloat("_OutlineWidth"));
        }

        if (highlightSource.HasProperty("_OutlinePixelWidth") && destination.HasProperty("_OutlinePixelWidth"))
        {
            destination.SetInt("_OutlinePixelWidth", highlightSource.GetInt("_OutlinePixelWidth"));
        }

        // Copy outline texture properties
        if (highlightSource.HasProperty("_OutlineTex") && destination.HasProperty("_OutlineTex"))
        {
            Texture outlineTex = highlightSource.GetTexture("_OutlineTex");
            if (outlineTex != null)
            {
                destination.SetTexture("_OutlineTex", outlineTex);
            }
        }

        if (highlightSource.HasProperty("_OutlineTexXSpeed") && destination.HasProperty("_OutlineTexXSpeed"))
        {
            destination.SetFloat("_OutlineTexXSpeed", highlightSource.GetFloat("_OutlineTexXSpeed"));
        }

        if (highlightSource.HasProperty("_OutlineTexYSpeed") && destination.HasProperty("_OutlineTexYSpeed"))
        {
            destination.SetFloat("_OutlineTexYSpeed", highlightSource.GetFloat("_OutlineTexYSpeed"));
        }

        // Copy outline distortion properties
        if (highlightSource.HasProperty("_OutlineDistortTex") && destination.HasProperty("_OutlineDistortTex"))
        {
            Texture distortTex = highlightSource.GetTexture("_OutlineDistortTex");
            if (distortTex != null)
            {
                destination.SetTexture("_OutlineDistortTex", distortTex);
            }
        }

        if (highlightSource.HasProperty("_OutlineDistortAmount") && destination.HasProperty("_OutlineDistortAmount"))
        {
            destination.SetFloat("_OutlineDistortAmount", highlightSource.GetFloat("_OutlineDistortAmount"));
        }

        if (highlightSource.HasProperty("_OutlineDistortTexXSpeed") && destination.HasProperty("_OutlineDistortTexXSpeed"))
        {
            destination.SetFloat("_OutlineDistortTexXSpeed", highlightSource.GetFloat("_OutlineDistortTexXSpeed"));
        }

        if (highlightSource.HasProperty("_OutlineDistortTexYSpeed") && destination.HasProperty("_OutlineDistortTexYSpeed"))
        {
            destination.SetFloat("_OutlineDistortTexYSpeed", highlightSource.GetFloat("_OutlineDistortTexYSpeed"));
        }

        // Enable outline shader keywords
        EnableOutlineKeywords(highlightSource, destination);
    }

    /// <summary>
    /// Enables the appropriate outline shader keywords based on the highlight material
    /// </summary>
    private void EnableOutlineKeywords(Material highlightSource, Material destination)
    {
        // Enable base outline keyword
        if (highlightSource.IsKeywordEnabled("OUTBASE_ON"))
        {
            destination.EnableKeyword("OUTBASE_ON");
        }
        else
        {
            destination.DisableKeyword("OUTBASE_ON");
        }

        // Enable only outline keyword
        if (highlightSource.IsKeywordEnabled("ONLYOUTLINE_ON"))
        {
            destination.EnableKeyword("ONLYOUTLINE_ON");
        }
        else
        {
            destination.DisableKeyword("ONLYOUTLINE_ON");
        }

        // Enable outline texture keyword
        if (highlightSource.IsKeywordEnabled("OUTTEX_ON"))
        {
            destination.EnableKeyword("OUTTEX_ON");
        }
        else
        {
            destination.DisableKeyword("OUTTEX_ON");
        }

        // Enable outline distortion keyword
        if (highlightSource.IsKeywordEnabled("OUTDIST_ON"))
        {
            destination.EnableKeyword("OUTDIST_ON");
        }
        else
        {
            destination.DisableKeyword("OUTDIST_ON");
        }
    }

    private void RevertToOriginalMaterial()
    {
        if (originalMaterial == null)
            return;

        // Clean up the combined material if we created one
        if (combinedMaterial != null && combinedMaterial != gameManager.highlightMaterial)
        {
            Destroy(combinedMaterial);
            combinedMaterial = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.material = originalMaterial;
        }
        else if (uiImage != null)
        {
            uiImage.material = originalMaterial;
        }
        else if (rendererComponent != null)
        {
            rendererComponent.material = originalMaterial;
        }
        isHighlighted = false;
    }

    void OnDestroy()
    {
        // Clean up the combined material when the component is destroyed
        if (combinedMaterial != null && combinedMaterial != gameManager?.highlightMaterial)
        {
            Destroy(combinedMaterial);
        }
    }
}
