using UnityEngine;

/// <summary>
/// Simplified Visual Enhancement Manager - Compatibility layer only
/// CLEANED: Removed old AppliedEnhancements system references
/// The actual enhancement application is now handled by UnifiedEnhancementController
/// This script maintains compatibility with existing systems that expect VisualEnhancementManager
/// </summary>
public class SimplifiedVisualEnhancementManager : MonoBehaviour
{
    [Header("Compatibility Layer")]
    [Tooltip("Reference to the unified controller that actually handles enhancements")]
    public UnifiedEnhancementController unifiedController;
    
    [Header("Current Enhancement Settings")]
    [SerializeField] private VisualEnhancementSettings currentSettings;
    [SerializeField] private bool enhancementsApplied = false;
    
    void Start()
    {
        // Find the unified controller if not assigned
        if (unifiedController == null)
        {
            unifiedController = FindObjectOfType<UnifiedEnhancementController>();
        }
        
        if (unifiedController == null)
        {
            Debug.LogWarning("SimplifiedVisualEnhancementManager: No UnifiedEnhancementController found. This is a compatibility-only layer.");
        }
        
        Debug.Log("SimplifiedVisualEnhancementManager: Compatibility layer initialized");
    }
    
    /// <summary>
    /// Apply visual enhancements - delegates to UnifiedEnhancementController
    /// Maintains compatibility with existing code that calls this method
    /// </summary>
    public void ApplyEnhancements(VisualEnhancementSettings settings)
    {
        if (settings == null)
        {
            Debug.LogWarning("SimplifiedVisualEnhancementManager: Cannot apply null enhancement settings");
            return;
        }
        
        currentSettings = settings;
        enhancementsApplied = true;
        
        // Log the compatibility call
        Debug.Log($"SimplifiedVisualEnhancementManager: Compatibility call - ApplyEnhancements({settings.decisionReason})");
        
        // The actual enhancement application is handled by UnifiedEnhancementController
        // This is just for compatibility with systems that expect this method to exist
        
        if (unifiedController != null && !unifiedController.AreEnhancementsActive())
        {
            Debug.LogWarning("SimplifiedVisualEnhancementManager: UnifiedEnhancementController is not active. Enhancements may not be applied properly.");
        }
    }
    
    /// <summary>
    /// Disable all visual enhancements - delegates to UnifiedEnhancementController
    /// </summary>
    public void DisableAllEnhancements()
    {
        if (unifiedController != null)
        {
            unifiedController.DisableAllEnhancements();
        }
        
        enhancementsApplied = false;
        currentSettings = new VisualEnhancementSettings();
        
        Debug.Log("SimplifiedVisualEnhancementManager: Disabled all enhancements via unified controller");
    }
    
    /// <summary>
    /// Get the current enhancement settings
    /// </summary>
    public VisualEnhancementSettings GetCurrentSettings()
    {
        if (unifiedController != null)
        {
            // Get the actual current settings from the unified controller
            var unifiedSettings = unifiedController.GetCurrentEnhancements();
            if (unifiedSettings != null)
            {
                currentSettings = unifiedSettings;
            }
        }
        
        return currentSettings;
    }
    
    /// <summary>
    /// Check if enhancements are currently applied
    /// </summary>
    public bool AreEnhancementsApplied()
    {
        if (unifiedController != null)
        {
            return unifiedController.AreEnhancementsActive();
        }
        
        return enhancementsApplied;
    }
    
    /// <summary>
    /// Runtime control methods - delegate to unified controller
    /// </summary>
    public void SetNavigationLineWidth(float width)
    {
        if (unifiedController != null)
        {
            unifiedController.SetNavigationLineWidth(width);
        }
    }
    
    public void SetNavigationLineOpacity(float opacity01)
    {
        if (unifiedController != null)
        {
            unifiedController.SetNavigationLineOpacity(opacity01);
        }
    }
    
    public void SetBoundingBoxWidth(float width)
    {
        if (unifiedController != null)
        {
            unifiedController.SetBoundingBoxWidth(width);
        }
    }
    
    public void SetBoundingBoxOpacity(float opacity01)
    {
        if (unifiedController != null)
        {
            unifiedController.SetBoundingBoxOpacity(opacity01);
        }
    }
    
    // Context menu methods for testing
    [ContextMenu("Test: Apply Maximum Enhancements")]
    public void TestMaximumEnhancements()
    {
        var testSettings = new VisualEnhancementSettings
        {
            enableBoundingBoxes = true,
            boundingBoxLineWidth = 0.15f,
            boundingBoxOpacity = 255f,
            enhanceNavigationLine = true,
            navigationLineWidth = 0.45f,
            navigationLineOpacity = 255f,
            enhanceStaticObjects = true,
            enhanceDynamicObjects = true,
            decisionReason = "Test: Maximum enhancement via compatibility layer"
        };
        
        ApplyEnhancements(testSettings);
        
        if (unifiedController != null)
        {
            unifiedController.TestMaximumEnhancements();
        }
    }
    
    [ContextMenu("Debug: Show Compatibility Status")]
    public void DebugShowCompatibilityStatus()
    {
        Debug.Log("=== SIMPLIFIED VISUAL ENHANCEMENT MANAGER STATUS ===");
        Debug.Log($"Unified Controller Found: {unifiedController != null}");
        Debug.Log($"Enhancements Applied (Local): {enhancementsApplied}");
        
        if (unifiedController != null)
        {
            Debug.Log($"Unified Controller Active: {unifiedController.AreEnhancementsActive()}");
            Debug.Log($"Current Vision Rating: {unifiedController.GetCurrentVisionRating()}/10");
            
            var unifiedSettings = unifiedController.GetCurrentEnhancements();
            if (unifiedSettings != null)
            {
                Debug.Log($"Unified Settings: {unifiedSettings.decisionReason}");
            }
        }
        
        if (currentSettings != null)
        {
            Debug.Log($"Local Settings: {currentSettings.decisionReason}");
            Debug.Log($"Navigation Line: {currentSettings.enhanceNavigationLine}");
            Debug.Log($"Bounding Boxes: {currentSettings.enableBoundingBoxes}");
        }
    }
}