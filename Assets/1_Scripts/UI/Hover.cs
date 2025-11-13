using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class HoverTriggerObject
{
    public GameObject triggerObject;
    public Vector2 positionOffset = new Vector2(100, -90);
}

public class Hover : MonoBehaviour
{
    [SerializeField] private GameObject HoverUI;
    [SerializeField] private TextMeshProUGUI hoverText;
    [SerializeField] private List<HoverTriggerObject> hoverTriggerObjects = new List<HoverTriggerObject>();
    
    private Camera mainCamera;
    private bool isHovering = false;
    private RectTransform hoverUIRectTransform;
    private Canvas canvas;
    private Vector2 currentOffset = Vector2.zero;
    
#if UNITY_EDITOR
    void OnValidate()
    {
        // Ensure default position offset is applied in the Editor when items are added
        foreach (var triggerObj in hoverTriggerObjects)
        {
            if (triggerObj != null && triggerObj.positionOffset == Vector2.zero)
            {
                triggerObj.positionOffset = new Vector2(100, -90);
            }
        }
    }
#endif
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Ensure default position offset is applied to any items that don't have it set
        foreach (var triggerObj in hoverTriggerObjects)
        {
            if (triggerObj != null && triggerObj.positionOffset == Vector2.zero)
            {
                triggerObj.positionOffset = new Vector2(100, -90);
            }
        }
        
        // Hide the UI GameObject by default
        if (HoverUI != null)
        {
            HoverUI.SetActive(false);
            hoverUIRectTransform = HoverUI.GetComponent<RectTransform>();
            
            // Find the canvas that contains the HoverUI
            canvas = HoverUI.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
            }
        }
        
        // Get the main camera for raycasting
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindAnyObjectByType<Camera>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        CheckHover();
        
        // Update UI position to follow mouse when hovering
        if (isHovering && hoverUIRectTransform != null)
        {
            UpdateUIPosition();
        }
    }
    
    void CheckHover()
    {
        bool currentlyHovering = false;
        Vector2 offsetToUse = Vector2.zero;
        
        // Check UI elements first
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            
            foreach (RaycastResult result in results)
            {
                foreach (HoverTriggerObject triggerObj in hoverTriggerObjects)
                {
                    if (triggerObj.triggerObject != null && triggerObj.triggerObject == result.gameObject)
                    {
                        currentlyHovering = true;
                        offsetToUse = triggerObj.positionOffset;
                        break;
                    }
                }
                if (currentlyHovering) break;
            }
        }
        
        // Check 3D objects with raycasting
        if (!currentlyHovering && mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                foreach (HoverTriggerObject triggerObj in hoverTriggerObjects)
                {
                    if (triggerObj.triggerObject != null && triggerObj.triggerObject == hit.collider.gameObject)
                    {
                        currentlyHovering = true;
                        offsetToUse = triggerObj.positionOffset;
                        break;
                    }
                }
            }
        }
        
        // Update UI visibility based on hover state
        if (currentlyHovering && !isHovering)
        {
            currentOffset = offsetToUse;
            DisplayUI();
            isHovering = true;
        }
        else if (currentlyHovering && isHovering)
        {
            // Update offset if hovering over a different object
            currentOffset = offsetToUse;
        }
        else if (!currentlyHovering && isHovering)
        {
            HideUI();
            isHovering = false;
        }
    }
    
    void UpdateUIPosition()
    {
        if (hoverUIRectTransform == null || canvas == null) return;
        
        Vector2 mousePosition = Input.mousePosition;
        Vector2 localPoint;
        
        // Convert screen position to local canvas position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );
        
        // Apply position offset for the current hovered object
        localPoint += currentOffset;
        
        // Set the position relative to the canvas
        hoverUIRectTransform.position = canvas.transform.TransformPoint(localPoint);
    }
    
    // Function to display the UI GameObject
    public void DisplayUI()
    {
        if (HoverUI != null)
        {
            HoverUI.SetActive(true);
            UpdateUIPosition();
        }
    }
    
    // Function to hide the UI GameObject
    public void HideUI()
    {
        if (HoverUI != null)
        {
            HoverUI.SetActive(false);
        }
    }
}
