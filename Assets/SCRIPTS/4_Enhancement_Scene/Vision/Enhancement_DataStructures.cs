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
    public float boundingBoxRange = 25f; /
    
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
    public float reliableAvoidanceDistance = 0f; 
    public bool hadCollisions = false;
    public int totalCollisions = 0;
}

/// <summary>
/// Complete Enhancement Settings - includes visual, audio, and haptic
/// </summary>
[System.Serializable]
public class CompleteEnhancementSettings
{
    [Header("Visual Enhancements")]
    public VisualEnhancementSettings visualSettings;
    
    [Header("Audio Enhancements")]
    public AudioEnhancementSettings audioSettings;
    
    [Header("Haptic Enhancements")]
    public HapticEnhancementSettings hapticSettings;
    
    [Header("Trial Info")]
    public string trialType;
    public string timestamp;
    public int visionRating;
}

[System.Serializable]
public class AudioEnhancementSettings
{
    [Header("Assessment-Derived Values")]
    public int centralVisionScore;           // 1-10 from algorithmic assessment
    public float objectClarityDistance;      // 1-10m from assessment question
    public float reliableAvoidanceDistance;  // 0.5-5m from assessment question
    
    [Header("Logic-Derived Values")]
    public bool audioEnabled;
    public string audioMode;                 // "FullSpeech", "StandardSpearcons", "LimitedSpearcons", "Disabled"
    public float masterVolume;
    public string decisionReason;
}

[System.Serializable]
public class HapticEnhancementSettings
{
    [Header("Assessment-Derived Values")]
    public int centralVisionScore;           // 1-10 from algorithmic assessment
    public int leftPeripheralScore;          // 1-10 from algorithmic assessment
    public int rightPeripheralScore;         // 1-10 from algorithmic assessment
    
    [Header("Logic-Derived Intensity Ranges")]
    public HapticIntensityRange frontIntensityRange;    // Based on centralVisionScore
    public HapticIntensityRange leftIntensityRange;     // Based on leftPeripheralScore
    public HapticIntensityRange rightIntensityRange;    // Based on rightPeripheralScore
    
    [Header("Applied Settings")]
    public bool hapticEnabled;
    public float baseIntensity;
    public float detectionRange;
    public int maxHapticObjects;
    public bool usingCustomSettings;
    public string decisionReason;
}

[System.Serializable]
public class HapticIntensityRange
{
    public float minIntensity;    // 0.0-1.0
    public float maxIntensity;    // 0.0-1.0
    public string reasoning;      // e.g., "Vision score 3/10 = 70%-100% intensity range"
    
    public HapticIntensityRange()
    {
        minIntensity = 0f;
        maxIntensity = 0f;
        reasoning = "";
    }
    
    public HapticIntensityRange(float min, float max, string reason)
    {
        minIntensity = min;
        maxIntensity = max;
        reasoning = reason;
    }
}

/// <summary>
/// Simplified Visual Enhancement Generator
/// Handles enhancement decision logic without the complexity of the original
/// </summary>
[System.Serializable]
public class SimpleEnhancementGenerator
{

    public static VisualEnhancementSettings GenerateEnhancements(int visionRating, NavigationSession baselineSession, 
        float reliableAvoidanceDistance = 2.5f, int poorVisionThreshold = 3)
    {
        VisualEnhancementSettings settings = new VisualEnhancementSettings();
        
        // Extract key data
        bool hadCollisions = baselineSession?.totalCollisions > 0;
        int totalCollisions = baselineSession?.totalCollisions ?? 0;
        
        // Calculate bounding box range based on reliable avoidance distance
        float boundingBoxRange = CalculateAvoidanceBasedRange(reliableAvoidanceDistance);
        
        // Store decision info including new avoidance distance
        settings.centralVisionRating = visionRating;
        settings.reliableAvoidanceDistance = reliableAvoidanceDistance; // NEW
        settings.hadCollisions = hadCollisions;
        settings.totalCollisions = totalCollisions;
        settings.boundingBoxRange = boundingBoxRange; // NEW: Use calculated range
        
        // Main decision logic
        bool useMaximumEnhancement = visionRating <= poorVisionThreshold || hadCollisions;
        
        if (useMaximumEnhancement)
        {
            // Maximum enhancement (except range which is always calculated from avoidance distance)
            settings.enableBoundingBoxes = true;
            settings.boundingBoxLineWidth = 0.15f;
            settings.boundingBoxOpacity = 255f;
            settings.boundingBoxSpacing = 2.0f;
            // boundingBoxRange already set above
            
            settings.enhanceNavigationLine = true;
            settings.navigationLineWidth = 0.45f;
            settings.navigationLineOpacity = 255f;
            settings.navigationLineSpacing = 2.0f;
            
            settings.decisionReason = $"Maximum enhancement applied. Vision: {visionRating}/10, Avoidance: {reliableAvoidanceDistance}m → Range: {boundingBoxRange}m, Collisions: {totalCollisions}";
        }
        else
        {
            // Scaled enhancement based on vision rating (except range which is always calculated from avoidance distance)
            float visionScale = Mathf.InverseLerp(10f, poorVisionThreshold + 1f, visionRating);
            visionScale = Mathf.Clamp01(visionScale);
            
            settings.enableBoundingBoxes = true;
            settings.boundingBoxLineWidth = Mathf.Lerp(0.05f, 0.15f, visionScale);
            settings.boundingBoxOpacity = Mathf.Lerp(200f, 255f, visionScale);
            settings.boundingBoxSpacing = Mathf.Lerp(1.0f, 2.0f, visionScale);
            // boundingBoxRange already set above
            
            settings.enhanceNavigationLine = true;
            settings.navigationLineWidth = Mathf.Lerp(0.25f, 0.45f, visionScale);
            settings.navigationLineOpacity = Mathf.Lerp(200f, 255f, visionScale);
            settings.navigationLineSpacing = Mathf.Lerp(1.0f, 2.0f, visionScale);
            
            settings.decisionReason = $"Scaled enhancement applied. Vision: {visionRating}/10, Avoidance: {reliableAvoidanceDistance}m → Range: {boundingBoxRange}m";
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
    

    private static float CalculateAvoidanceBasedRange(float avoidanceDistance)
    {
        // Apply the specified ranges based on avoidance distance
        if (avoidanceDistance <= 0.5f)
        {
            return 35f; // Users who can avoid at very close range get longest warning distance
        }
        else if (avoidanceDistance <= 1.5f) // 0.6-1.5
        {
            return 25f;
        }
        else if (avoidanceDistance <= 3.0f) // 1.6-3.0
        {
            return 15f;
        }
        else if (avoidanceDistance <= 4.0f) // 3.1-4.0
        {
            return 10f;
        }
        else // 4.1-5.0
        {
            return 5f; // Users who need lots of space to avoid get shorter warning (less clutter)
        }
    }
}
