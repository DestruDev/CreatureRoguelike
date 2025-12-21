using UnityEngine;
using AllIn1SpriteShader;
using UnityEngine.Rendering;

public class HighlightUnit : MonoBehaviour
{
    private Unit unit;
    private GameManager gameManager;
    private SpriteRenderer spriteRenderer;
    private UnityEngine.UI.Image uiImage;
    private Renderer rendererComponent;
    private Material originalMaterial;
    private Material originalSharedMaterial; // Store the shared material asset (for builds)
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

        // Find GameManager (retry in Update if not found, as initialization order may differ in builds)
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("HighlightUnit: GameManager not found in scene! Will retry in Update.");
        }

        // Get the renderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<UnityEngine.UI.Image>();
        rendererComponent = GetComponent<Renderer>();

        // Store the original material (use sharedMaterial for builds compatibility)
        if (spriteRenderer != null)
        {
            originalSharedMaterial = spriteRenderer.sharedMaterial;
            // Get the current material instance (creates one if needed)
            // In builds, this might return null initially, so we'll use sharedMaterial as fallback
            if (spriteRenderer.material != null)
            {
                originalMaterial = spriteRenderer.material;
            }
            else if (originalSharedMaterial != null)
            {
                originalMaterial = originalSharedMaterial;
            }
        }
        else if (uiImage != null)
        {
            originalSharedMaterial = uiImage.material; // UI Image doesn't have sharedMaterial
            originalMaterial = uiImage.material;
        }
        else if (rendererComponent != null)
        {
            originalSharedMaterial = rendererComponent.sharedMaterial;
            if (rendererComponent.material != null)
            {
                originalMaterial = rendererComponent.material;
            }
            else if (originalSharedMaterial != null)
            {
                originalMaterial = originalSharedMaterial;
            }
        }
        else
        {
            Debug.LogWarning("HighlightUnit: No renderer found on " + gameObject.name + ". Need SpriteRenderer, Image, or Renderer component.");
        }
        
        // Ensure we have a valid material reference
        if (originalMaterial == null && originalSharedMaterial != null)
        {
            originalMaterial = originalSharedMaterial;
        }
    }

    void Update()
    {
        // Re-find GameManager if it's null (can happen in builds if initialization order is different)
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
                return;
        }
        
        if (unit == null)
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
        // Ensure we have valid references
        if (gameManager == null || gameManager.highlightMaterial == null)
        {
            return;
        }
        
        // Always create a new material from the highlight material to ensure it uses AllIn1SpriteShader
        // This is critical for builds - the original material might not use the correct shader
        combinedMaterial = new Material(gameManager.highlightMaterial);
        
        // CRITICAL FOR BUILDS: Ensure the shader is correct for the current render pipeline
        // If using URP and the shader doesn't match, find and use the correct variant
        Shader correctShader = GetCorrectShaderForRenderPipeline(gameManager.highlightMaterial.shader);
        if (correctShader != null && combinedMaterial.shader != correctShader)
        {
            combinedMaterial.shader = correctShader;
        }
        
        // If we have an original material, try to preserve its properties (texture, color, etc.)
        Material sourceMaterial = originalMaterial != null ? originalMaterial : originalSharedMaterial;
        
        if (sourceMaterial != null)
        {
            // Preserve the main texture from the original material if it exists
            if (sourceMaterial.mainTexture != null && combinedMaterial.HasProperty("_MainTex"))
            {
                combinedMaterial.SetTexture("_MainTex", sourceMaterial.mainTexture);
            }
            
            // Preserve the main color from the original material if it exists
            if (sourceMaterial.HasProperty("_Color") && combinedMaterial.HasProperty("_Color"))
            {
                combinedMaterial.SetColor("_Color", sourceMaterial.GetColor("_Color"));
            }
            // If original doesn't have _Color but spriteRenderer has a color, use that
            else if (spriteRenderer != null && combinedMaterial.HasProperty("_Color"))
            {
                combinedMaterial.SetColor("_Color", spriteRenderer.color);
            }
        }
        else if (spriteRenderer != null)
        {
            // If no original material, use sprite texture and color directly
            if (spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null && combinedMaterial.HasProperty("_MainTex"))
            {
                combinedMaterial.SetTexture("_MainTex", spriteRenderer.sprite.texture);
            }
            
            if (combinedMaterial.HasProperty("_Color"))
            {
                combinedMaterial.SetColor("_Color", spriteRenderer.color);
            }
        }
        
        // Copy highlight/outline properties from the highlight material (this ensures outline is enabled)
        CopyHighlightProperties(gameManager.highlightMaterial, combinedMaterial);

        // Apply the combined material
        if (spriteRenderer != null)
        {
            // Use material (not sharedMaterial) to ensure we get an instance in builds
            spriteRenderer.material = combinedMaterial;
        }
        else if (uiImage != null)
        {
            uiImage.material = combinedMaterial;
        }
        else if (rendererComponent != null)
        {
            // Use material (not sharedMaterial) to ensure we get an instance in builds
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

        // CRITICAL: Ensure the destination material uses the same shader as the highlight (AllIn1SpriteShader)
        // This is essential for the outline to work in builds
        if (highlightSource.shader != null)
        {
            destination.shader = highlightSource.shader;
        }
        
        // Preserve the original sprite texture and color if they exist
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // Copy main texture from sprite
            if (destination.HasProperty("_MainTex"))
            {
                Texture mainTex = spriteRenderer.sprite.texture;
                if (mainTex != null)
                {
                    destination.SetTexture("_MainTex", mainTex);
                }
            }
            
            // Copy main color
            if (destination.HasProperty("_Color"))
            {
                destination.SetColor("_Color", spriteRenderer.color);
            }
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
        // Clean up the combined material if we created one
        if (combinedMaterial != null && combinedMaterial != gameManager?.highlightMaterial)
        {
            Destroy(combinedMaterial);
            combinedMaterial = null;
        }

        // Use sharedMaterial for builds compatibility, or fall back to originalMaterial
        Material materialToUse = originalSharedMaterial != null ? originalSharedMaterial : originalMaterial;
        
        if (materialToUse == null)
            return;

        if (spriteRenderer != null)
        {
            spriteRenderer.sharedMaterial = materialToUse;
        }
        else if (uiImage != null)
        {
            uiImage.material = materialToUse;
        }
        else if (rendererComponent != null)
        {
            rendererComponent.sharedMaterial = materialToUse;
        }
        isHighlighted = false;
    }

    /// <summary>
    /// Gets the correct shader variant for the current render pipeline
    /// This is critical for builds where shader variants must match the render pipeline
    /// </summary>
    private Shader GetCorrectShaderForRenderPipeline(Shader originalShader)
    {
        if (originalShader == null)
            return null;
        
        // Check if we're using URP
        if (GraphicsSettings.defaultRenderPipeline != null)
        {
            string rpType = GraphicsSettings.defaultRenderPipeline.GetType().Name;
            
            // If using URP, try to use SRPBatch variant if available
            if (rpType.Contains("Universal") || rpType.Contains("URP"))
            {
                // Try to find the SRPBatch variant
                Shader srpBatchShader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShaderSRPBatch");
                if (srpBatchShader != null)
                {
                    return srpBatchShader;
                }
            }
        }
        
        // Fall back to original shader or try to find the standard variant
        Shader standardShader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShader");
        if (standardShader != null)
        {
            return standardShader;
        }
        
        // If all else fails, return the original shader
        return originalShader;
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
