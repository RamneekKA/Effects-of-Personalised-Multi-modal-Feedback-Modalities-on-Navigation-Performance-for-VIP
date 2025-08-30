using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data structures for the enhancement system
/// Contains all the classes needed by UnifiedEnhancementController and SimplifiedVisualEnhancementManager
/// </summary>

/// <summary>
/// Visual Enhancement Settings Data Structure
/// </summary>
[System.Serializable]
public class VisualEnhancementSettings
{
    [Header("Bounding Box Settings")]
    public bool enableBoundingBoxes = false;
    public float boundingBoxLineWidth = 0.05f;
    public float boundingBoxOpacity = 200f; // Alpha value 0-255
    public float boundingBoxSpacing = 1.0f;
    
    [Header("Navigation Line Settings")]
    public bool enhanceNavigationLine = false;
    public float navigationLineWidth = 0.5f;
    public float navigationLineOpacity = 200f; // Alpha value 0-255
    public float navigationLineSpacing = 1.0f;
    
    [Header("Object Filtering")]
    public bool enhanceStaticObjects = false;
    public bool enhanceDynamicObjects = false;
    
    [Header("Decision Info")]
    public string decisionReason = "";
    public int centralVisionRating = 0;
    public bool hadCollisions = false;
    public int totalCollisions = 0;
}

/// <summary>
/// Simplified Visual Enhancement Generator
/// Handles enhancement decision logic without the complexity of the original
/// </summary>
[System.Serializable]
public class SimpleEnhancementGenerator
{
    public static VisualEnhancementSettings GenerateEnhancements(int visionRating, NavigationSession baselineSession, int poorVisionThreshold = 3)
    {
        VisualEnhancementSettings settings = new VisualEnhancementSettings();
        
        // Extract key data
        bool hadCollisions = baselineSession?.totalCollisions > 0;
        int totalCollisions = baselineSession?.totalCollisions ?? 0;
        
        // Store decision info
        settings.centralVisionRating = visionRating;
        settings.hadCollisions = hadCollisions;
        settings.totalCollisions = totalCollisions;
        
        // Main decision logic
        bool useMaximumEnhancement = visionRating <= poorVisionThreshold || hadCollisions;
        
        if (useMaximumEnhancement)
        {
            // Maximum enhancement
            settings.enableBoundingBoxes = true;
            settings.boundingBoxLineWidth = 0.15f;
            settings.boundingBoxOpacity = 255f;
            settings.boundingBoxSpacing = 2.0f;
            
            settings.enhanceNavigationLine = true;
            settings.navigationLineWidth = 0.45f;
            settings.navigationLineOpacity = 255f;
            settings.navigationLineSpacing = 2.0f;
            
            settings.decisionReason = $"Maximum enhancement applied. Vision rating: {visionRating}/10, Collisions: {totalCollisions}";
        }
        else
        {
            // Scaled enhancement based on vision rating
            float visionScale = Mathf.InverseLerp(10f, poorVisionThreshold + 1f, visionRating);
            visionScale = Mathf.Clamp01(visionScale);
            
            settings.enableBoundingBoxes = true;
            settings.boundingBoxLineWidth = Mathf.Lerp(0.05f, 0.15f, visionScale);
            settings.boundingBoxOpacity = Mathf.Lerp(200f, 255f, visionScale);
            settings.boundingBoxSpacing = Mathf.Lerp(1.0f, 2.0f, visionScale);
            
            settings.enhanceNavigationLine = true;
            settings.navigationLineWidth = Mathf.Lerp(0.25f, 0.45f, visionScale);
            settings.navigationLineOpacity = Mathf.Lerp(200f, 255f, visionScale);
            settings.navigationLineSpacing = Mathf.Lerp(1.0f, 2.0f, visionScale);
            
            settings.decisionReason = $"Scaled enhancement applied based on vision rating: {visionRating}/10";
        }
        
        // Determine object types to enhance
        settings.enhanceStaticObjects = true;
        settings.enhanceDynamicObjects = true;
        
        if (baselineSession?.collisionsByObjectType != null && baselineSession.collisionsByObjectType.Count > 0)
        {
            // Could add logic here to only enhance object types that caused collisions
            // For now, enhance all types if any collisions occurred
        }
        
        return settings;
    }
}