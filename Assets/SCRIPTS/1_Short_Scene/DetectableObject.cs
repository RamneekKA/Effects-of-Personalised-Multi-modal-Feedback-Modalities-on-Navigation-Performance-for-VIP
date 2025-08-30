using UnityEngine;

[System.Serializable]
public class DetectableObject : MonoBehaviour
{
    public string className = "Car";
    
    [Header("Bounding Box Settings")]
    public bool useManualBounds = false;
    public Vector3 manualBoundsSize = new Vector3(2f, 3f, 2f); // Width, Height, Depth
    public Vector3 manualBoundsOffset = Vector3.zero; // Offset from object center
    
    [Header("Automatic Bounds Adjustments")]
    [Tooltip("Vertical offset to apply to automatic bounds (useful for objects that get cut off at bottom)")]
    public float verticalOffset = 0f;
    [Tooltip("Additional padding to add to automatic bounds (X=width, Y=height, Z=depth)")]
    public Vector3 boundsExpansion = Vector3.zero;
    
    [HideInInspector]
    public Bounds worldBounds;
    
    void Start()
    {
        // Calculate world bounds including all child renderers
        CalculateWorldBounds();
    }
    
    void CalculateWorldBounds()
    {
        if (useManualBounds)
        {
            // Use manually specified bounds
            Vector3 center = transform.position + manualBoundsOffset;
            worldBounds = new Bounds(center, manualBoundsSize);
        }
        else
        {
            // Auto-calculate from renderers with adjustments
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            
            worldBounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                worldBounds.Encapsulate(renderer.bounds);
            }
            
            // Apply vertical offset to the center
            if (verticalOffset != 0f)
            {
                Vector3 newCenter = worldBounds.center;
                newCenter.y += verticalOffset;
                worldBounds.center = newCenter;
            }
            
            // Apply bounds expansion
            if (boundsExpansion != Vector3.zero)
            {
                Vector3 newSize = worldBounds.size + boundsExpansion;
                worldBounds.size = newSize;
            }
        }
    }
    
    void Update()
    {
        // Recalculate bounds if object moves
        if (transform.hasChanged)
        {
            CalculateWorldBounds();
            transform.hasChanged = false;
        }
    }
    
    // Helper method to quickly set up common object types
    [ContextMenu("Setup as Tree")]
    void SetupAsTree()
    {
        className = "Tree";
        useManualBounds = false; // Use automatic bounds with adjustments
        verticalOffset = 0.5f; // Move bounding box up to include trunk base
        boundsExpansion = new Vector3(0.5f, 1f, 0.5f); // Add some padding
        CalculateWorldBounds();
    }
    
    [ContextMenu("Setup as Car")]
    void SetupAsCar()
    {
        className = "Car";
        useManualBounds = false; // Cars usually have good auto-bounds
        verticalOffset = 0f;
        boundsExpansion = Vector3.zero;
        CalculateWorldBounds();
    }
    
    [ContextMenu("Setup as Bus Stop")]
    void SetupAsBusStop()
    {
        className = "Bus Stop";
        useManualBounds = true;
        manualBoundsSize = new Vector3(4f, 3f, 2f);
        manualBoundsOffset = new Vector3(0f, 1.5f, 0f);
        verticalOffset = 0f; // Not used when manual bounds are enabled
        boundsExpansion = Vector3.zero; // Not used when manual bounds are enabled
        CalculateWorldBounds();
    }
    
    [ContextMenu("Reset Bounds Adjustments")]
    void ResetBoundsAdjustments()
    {
        verticalOffset = 0f;
        boundsExpansion = Vector3.zero;
        CalculateWorldBounds();
    }
}