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
        if (spriteRenderer != null)
        {
            spriteRenderer.material = gameManager.highlightMaterial;
        }
        else if (uiImage != null)
        {
            uiImage.material = gameManager.highlightMaterial;
        }
        else if (rendererComponent != null)
        {
            rendererComponent.material = gameManager.highlightMaterial;
        }
        isHighlighted = true;
    }

    private void RevertToOriginalMaterial()
    {
        if (originalMaterial == null)
            return;

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
}
