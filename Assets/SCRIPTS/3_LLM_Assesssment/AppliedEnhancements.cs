using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// New enhancement data structure to avoid conflicts with existing AppliedEnhancements
/// Create this as a separate file: Assets/SCRIPTS/3_LLM_Assesssment/NewEnhancementData.cs
/// </summary>
[System.Serializable]
public class EnhancementConfiguration
{
    public string sourceAssessment = "llm"; // "llm", "algorithmic", "llm_fallback"
    
    // Visual enhancements
    public bool visualEnabled = false;
    public float navLineWidth = 0.4f;        // 0.2-0.6
    public float navLineOpacity = 80f;       // 0-100%
    public float bboxWidth = 0.1f;           // 0.02-0.2
    public float bboxOpacity = 60f;          // 0-100%
    public float bboxRange = 25f;            // 5-50m
    
    // Audio enhancements (only one can be selected)
    public bool audioEnabled = false;
    public string audioType = "TTS";         // "TTS", "SPEARCON", "SPEARCON_DISTANCE"
    public float audioInterval = 1.0f;       // TTS: 0.15-5s, SPEARCON: 0.5-3s
    public float audioDistance = 5.0f;       // For SPEARCON_DISTANCE: 0-10m
    
    // Haptic enhancements (all settings required if enabled)
    public bool hapticEnabled = false;
    public float hapticCentralMin = 30f;     // 0-100%
    public float hapticCentralMax = 80f;     // 0-100%
    public float hapticLeftMin = 30f;        // 0-100%
    public float hapticLeftMax = 80f;        // 0-100%
    public float hapticRightMin = 30f;       // 0-100%
    public float hapticRightMax = 80f;       // 0-100%
    public int hapticObjectCount = 2;        // 1, 2, or 3
}