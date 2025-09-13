using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;

/// <summary>
/// Manual Enhancement Controller for LLM Trials
/// Provides inspector-based manual control of all enhancements during short_llm and long_llm trials
/// Now automatically loads and applies LLM assessment decisions for LLM trials
/// All settings are contained in this single GameObject for easy access
/// </summary>
public class ManualEnhancementController : MonoBehaviour
{
    [Header("Trial Detection")]
    [Tooltip("Only applies manual settings during LLM trials (short_llm, long_llm)")]
    [SerializeField] private bool isLLMTrial = false;
    [SerializeField] private string currentTrial = "";
    
    [Header("LLM Integration")]
    [Tooltip("Automatically load LLM assessment decisions for LLM trials")]
    public bool autoLoadLLMDecisions = true;
    [SerializeField] private bool llmDecisionsLoaded = false;
    [SerializeField] private string llmAssessmentSource = "";
    
    [Header("System References")]
    [Tooltip("Route guide system for navigation line control")]
    public RouteGuideSystem routeGuideSystem;
    
    [Tooltip("Dynamic object manager for bounding box control")]
    public DynamicObjectManager dynamicObjectManager;
    
    [Tooltip("Unified audio controller for speech and spearcon control")]
    public UnifiedAudioController unifiedAudioController;
    
    [Tooltip("Spatial haptic controller for haptic feedback control")]
    public SpatialHapticController spatialHapticController;
    
    [Header("=== VISUAL ENHANCEMENT SETTINGS ===")]
    
    [Header("Navigation Line Controls")]
    [Tooltip("Enable manual control of navigation line")]
    public bool enableNavigationLineControl = true;
    
    [Range(0.2f, 0.6f)]
    [Tooltip("Width of the navigation line")]
    public float navigationLineWidth = 0.2f;
    
    [Range(0f, 1f)]
    [Tooltip("Opacity of the navigation line (0=transparent, 1=opaque)")]
    public float navigationLineOpacity = 1.0f;
    
    [Tooltip("Color of the navigation line")]
    public Color navigationLineColor = Color.cyan;
    
    [Header("Bounding Box Controls")]
    [Tooltip("Enable manual control of bounding boxes")]
    public bool enableBoundingBoxControl = true;
    
    [Range(0.02f, 0.20f)]
    [Tooltip("Width of the bounding box lines")]
    public float boundingBoxLineWidth = 0.20f;
    
    [Range(0f, 1f)]
    [Tooltip("Opacity of the bounding boxes (0=transparent, 1=opaque)")]
    public float boundingBoxOpacity = 1.0f;
    
    [Tooltip("Color of the bounding boxes")]
    public Color boundingBoxColor = Color.green;
    
    [Range(5.0f, 35f)]
    [Tooltip("Range to show bounding boxes around objects")]
    public float boundingBoxRange = 15f;
    
    [Header("=== AUDIO ENHANCEMENT SETTINGS ===")]
    
    [Header("Audio Mode Selection")]
    [Tooltip("Choose audio enhancement type for LLM trial")]
    public AudioEnhancementMode audioMode = AudioEnhancementMode.Disabled;
    
    [Header("TTS Speech Settings")]
    [Range(0.5f, 5f)]
    [Tooltip("How often to announce objects via speech")]
    public float speechInterval = 2f;
    
    [Range(5f, 30f)]
    [Tooltip("Detection range for speech announcements")]
    public float speechRange = 15f;
    
    [Range(0f, 1f)]
    [Tooltip("Volume for speech announcements")]
    public float speechVolume = 0.8f;
    
    [Header("Spearcon Settings")]
    [Range(0.5f, 5f)]
    [Tooltip("How often to play spearcon audio clips")]
    public float spearconInterval = 1.5f;
    
    [Range(5f, 50f)]
    [Tooltip("Detection range for spearcon announcements")]
    public float spearconRange = 25f;
    
    [Range(0f, 1f)]
    [Tooltip("Volume for spearcon announcements")]
    public float spearconVolume = 0.8f;
    
    [Range(1f, 20f)]
    [Tooltip("Distance threshold - only announce objects beyond this distance")]
    public float spearconDistanceThreshold = 5f;
    
    [Header("=== HAPTIC ENHANCEMENT SETTINGS ===")]
    
    [Header("Haptic System Control")]
    [Tooltip("Enable haptic feedback during LLM trial")]
    public bool enableHapticFeedback = true;
    
    [Range(5f, 50f)]
    [Tooltip("Detection range for haptic feedback")]
    public float hapticDetectionRange = 25f;
    
    [Range(1, 3)]
    [Tooltip("Maximum number of objects to provide haptic feedback for simultaneously")]
    public int maxHapticObjects = 2;
    
    [Header("Grouped Haptic Event Configurations")]
    [Tooltip("Configure settings for central direction events (centre, centre_leftback, centre_rightback)")]
    public HapticGroupConfig centralGroup = new HapticGroupConfig 
    { 
        groupName = "Central",
        minIntensity = 0.3f, 
        maxIntensity = 1.0f, 
        feedbackInterval = 1.5f 
    };
    
    [Tooltip("Configure settings for left direction events (left, left_back)")]
    public HapticGroupConfig leftGroup = new HapticGroupConfig 
    { 
        groupName = "Left",
        minIntensity = 0.3f, 
        maxIntensity = 1.0f, 
        feedbackInterval = 1.5f 
    };
    
    [Tooltip("Configure settings for right direction events (right, right_back)")]
    public HapticGroupConfig rightGroup = new HapticGroupConfig 
    { 
        groupName = "Right",
        minIntensity = 0.3f, 
        maxIntensity = 1.0f, 
        feedbackInterval = 1.5f 
    };
    
    [Header("=== CURRENT STATUS ===")]
    [SerializeField] private bool manualControlActive = false;
    [SerializeField] private bool settingsApplied = false;
    
    // Audio mode enum for LLM trials
    public enum AudioEnhancementMode
    {
        Disabled,
        TTSSpeech,
        StandardSpearcons,
        DistanceFilteredSpearcons
    }
    
    [System.Serializable]
    public class HapticGroupConfig
    {
        public string groupName;
        [Range(0f, 1f)] public float minIntensity = 0.1f;
        [Range(0f, 1f)] public float maxIntensity = 1.0f;
        [Range(0.5f, 5f)] public float feedbackInterval = 1.5f;
        [Range(1f, 50f)] public float detectionRange = 25f;
    }
    
    void Start()
    {
        // Auto-find system references if not assigned
        FindSystemReferences();
        
        // Check if this is an LLM trial
        CheckTrialType();
        
        if (isLLMTrial)
        {
            Debug.Log($"ManualEnhancementController: LLM trial detected ({currentTrial})");
            
            if (autoLoadLLMDecisions)
            {
                Debug.Log("Attempting to load LLM assessment decisions...");
                StartCoroutine(LoadLLMDecisionsWithDelay());
            }
            else
            {
                Debug.Log("Auto-loading disabled - applying manual settings");
                StartCoroutine(ApplyManualSettingsWithDelay());
            }
        }
        else
        {
            Debug.Log($"ManualEnhancementController: Not an LLM trial ({currentTrial}) - manual control disabled");
            manualControlActive = false;
        }
    }
    
    void FindSystemReferences()
    {
        if (routeGuideSystem == null)
            routeGuideSystem = FindObjectOfType<RouteGuideSystem>();
        
        if (dynamicObjectManager == null)
            dynamicObjectManager = FindObjectOfType<DynamicObjectManager>();
        
        if (unifiedAudioController == null)
            unifiedAudioController = FindObjectOfType<UnifiedAudioController>();
        
        if (spatialHapticController == null)
            spatialHapticController = FindObjectOfType<SpatialHapticController>();
        
        Debug.Log("ManualEnhancementController: System references found");
    }
    
    void CheckTrialType()
    {
        if (SessionManager.Instance != null)
        {
            currentTrial = SessionManager.Instance.GetCurrentTrial();
            isLLMTrial = currentTrial == "short_llm" || currentTrial == "long_llm";
            
            // Subscribe to trial changes
            SessionManager.OnTrialChanged += OnTrialChanged;
        }
        else
        {
            Debug.LogWarning("ManualEnhancementController: No SessionManager found");
            currentTrial = "unknown";
            isLLMTrial = false;
        }
    }
    
    void OnTrialChanged(string newTrial)
    {
        currentTrial = newTrial;
        bool wasLLMTrial = isLLMTrial;
        isLLMTrial = newTrial == "short_llm" || newTrial == "long_llm";
        
        if (isLLMTrial && !wasLLMTrial)
        {
            Debug.Log($"ManualEnhancementController: Switched to LLM trial ({newTrial}) - enabling manual control");
            
            if (autoLoadLLMDecisions)
            {
                StartCoroutine(LoadLLMDecisionsWithDelay());
            }
            else
            {
                StartCoroutine(ApplyManualSettingsWithDelay());
            }
        }
        else if (!isLLMTrial && wasLLMTrial)
        {
            Debug.Log($"ManualEnhancementController: Switched away from LLM trial ({newTrial}) - disabling manual control");
            DisableManualControl();
        }
    }
    
    IEnumerator LoadLLMDecisionsWithDelay()
    {
        // Wait for other systems to initialize
        yield return new WaitForSeconds(1f);
        
        if (TryLoadLLMAssessmentDecisions())
        {
            Debug.Log("ManualEnhancementController: Successfully loaded and applied LLM decisions");
            llmDecisionsLoaded = true;
        }
        else
        {
            Debug.LogWarning("ManualEnhancementController: Failed to load LLM decisions - falling back to manual settings");
            llmDecisionsLoaded = false;
            ApplyAllManualSettings();
        }
        
        manualControlActive = true;
        settingsApplied = true;
    }
    
    bool TryLoadLLMAssessmentDecisions()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogError("SessionManager not available for LLM decision loading");
            return false;
        }
        
        // Get the LLM assessment folder path
        string sessionPath = SessionManager.Instance.GetSessionPath();
        string llmAssessmentPath = Path.Combine(sessionPath, "04_LLMAssessment");
        
        if (!Directory.Exists(llmAssessmentPath))
        {
            Debug.LogError($"LLM assessment folder not found: {llmAssessmentPath}");
            return false;
        }
        
        // Find the most recent LLM assessment JSON file
        string[] jsonFiles = Directory.GetFiles(llmAssessmentPath, "llm_realtime_assessment_*.json");
        
        if (jsonFiles.Length == 0)
        {
            Debug.LogError("No LLM assessment JSON files found");
            return false;
        }
        
        // Get the most recent file (by creation time)
        string mostRecentFile = jsonFiles
            .OrderByDescending(f => File.GetCreationTime(f))
            .First();
        
        Debug.Log($"Loading LLM decisions from: {Path.GetFileName(mostRecentFile)}");
        llmAssessmentSource = Path.GetFileName(mostRecentFile);
        
        try
        {
            string jsonContent = File.ReadAllText(mostRecentFile);
            EnhancementAssessmentResults results = JsonUtility.FromJson<EnhancementAssessmentResults>(jsonContent);
            
            if (results.enhancementConfiguration == null)
            {
                Debug.LogError("No enhancement configuration found in LLM assessment results");
                return false;
            }
            
            // Apply the LLM decisions to manual controller settings
            ApplyLLMEnhancementConfiguration(results.enhancementConfiguration);
            
            // Now apply all settings (which will use the LLM-loaded values)
            ApplyAllManualSettings();
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load LLM assessment decisions: {e.Message}");
            return false;
        }
    }
    
    void ApplyLLMEnhancementConfiguration(EnhancementConfiguration config)
    {
        Debug.Log("=== APPLYING LLM ENHANCEMENT DECISIONS ===");
        Debug.Log($"Source Assessment: {config.sourceAssessment}");
        
        // VISUAL ENHANCEMENTS
        Debug.Log($"Visual Enabled: {config.visualEnabled}");
        
        if (config.visualEnabled)
        {
            // Navigation Line
            enableNavigationLineControl = true;
            navigationLineWidth = Mathf.Clamp(config.navLineWidth, 0.2f, 0.6f);
            navigationLineOpacity = Mathf.Clamp(config.navLineOpacity / 100f, 0f, 1f); // Convert 0-100% to 0-1
            
            // Bounding Box
            enableBoundingBoxControl = true;
            boundingBoxLineWidth = Mathf.Clamp(config.bboxWidth, 0.02f, 0.2f);
            boundingBoxOpacity = Mathf.Clamp(config.bboxOpacity / 100f, 0f, 1f); // Convert 0-100% to 0-1
            boundingBoxRange = Mathf.Clamp(config.bboxRange, 5f, 50f);
            
            Debug.Log($"  Nav Line: width={navigationLineWidth:F2}, opacity={navigationLineOpacity:F2}");
            Debug.Log($"  Bbox: width={boundingBoxLineWidth:F3}, opacity={boundingBoxOpacity:F2}, range={boundingBoxRange:F1}m");
        }
        else
        {
            enableNavigationLineControl = false;
            enableBoundingBoxControl = false;
            Debug.Log("  Visual enhancements disabled");
        }
        
        // AUDIO ENHANCEMENTS
        Debug.Log($"Audio Enabled: {config.audioEnabled}");
        
        if (config.audioEnabled)
        {
            // Map LLM audio types to manual controller modes
            switch (config.audioType)
            {
                case "TTS":
                case "TTS_SPEECH":
                    audioMode = AudioEnhancementMode.TTSSpeech;
                    speechInterval = Mathf.Clamp(config.audioInterval, 0.15f, 5f);
                    Debug.Log($"  Audio Mode: TTS Speech, interval={speechInterval:F2}s");
                    break;
                    
                case "SPEARCON":
                case "SPATIAL_SPEECH":
                    audioMode = AudioEnhancementMode.StandardSpearcons;
                    spearconInterval = Mathf.Clamp(config.audioInterval, 0.5f, 3f);
                    Debug.Log($"  Audio Mode: Standard Spearcons, interval={spearconInterval:F2}s");
                    break;
                    
                case "SPEARCON_DISTANCE":
                case "SPATIAL_SPEECH_DISTANCE":
                    audioMode = AudioEnhancementMode.DistanceFilteredSpearcons;
                    spearconInterval = Mathf.Clamp(config.audioInterval, 0.5f, 3f);
                    spearconDistanceThreshold = Mathf.Clamp(config.audioDistance, 0f, 10f);
                    Debug.Log($"  Audio Mode: Distance Filtered Spearcons, interval={spearconInterval:F2}s, threshold={spearconDistanceThreshold:F1}m");
                    break;
                    
                default:
                    audioMode = AudioEnhancementMode.Disabled;
                    Debug.LogWarning($"  Unknown audio type '{config.audioType}', disabling audio");
                    break;
            }
        }
        else
        {
            audioMode = AudioEnhancementMode.Disabled;
            Debug.Log("  Audio enhancements disabled");
        }
        
        // HAPTIC ENHANCEMENTS
        Debug.Log($"Haptic Enabled: {config.hapticEnabled}");
        
        enableHapticFeedback = config.hapticEnabled;
        
        if (config.hapticEnabled)
        {
            // Map object count (clamp to valid range for manual controller)
            maxHapticObjects = Mathf.Clamp(config.hapticObjectCount, 1, 3);
            
            // Map intensities (convert 0-100% to 0-1 and ensure min <= max)
            float centralMin = Mathf.Clamp(config.hapticCentralMin / 100f, 0f, 1f);
            float centralMax = Mathf.Clamp(config.hapticCentralMax / 100f, 0f, 1f);
            centralGroup.minIntensity = Mathf.Min(centralMin, centralMax);
            centralGroup.maxIntensity = Mathf.Max(centralMin, centralMax);
            
            float leftMin = Mathf.Clamp(config.hapticLeftMin / 100f, 0f, 1f);
            float leftMax = Mathf.Clamp(config.hapticLeftMax / 100f, 0f, 1f);
            leftGroup.minIntensity = Mathf.Min(leftMin, leftMax);
            leftGroup.maxIntensity = Mathf.Max(leftMin, leftMax);
            
            float rightMin = Mathf.Clamp(config.hapticRightMin / 100f, 0f, 1f);
            float rightMax = Mathf.Clamp(config.hapticRightMax / 100f, 0f, 1f);
            rightGroup.minIntensity = Mathf.Min(rightMin, rightMax);
            rightGroup.maxIntensity = Mathf.Max(rightMin, rightMax);
            
            Debug.Log($"  Max Objects: {maxHapticObjects}");
            Debug.Log($"  Central: {centralGroup.minIntensity:F2}-{centralGroup.maxIntensity:F2}");
            Debug.Log($"  Left: {leftGroup.minIntensity:F2}-{leftGroup.maxIntensity:F2}");
            Debug.Log($"  Right: {rightGroup.minIntensity:F2}-{rightGroup.maxIntensity:F2}");
        }
        else
        {
            Debug.Log("  Haptic enhancements disabled");
        }
        
        Debug.Log("=== LLM DECISIONS MAPPED TO MANUAL CONTROLLER ===");
    }
    
    IEnumerator ApplyManualSettingsWithDelay()
    {
        // Wait for other systems to initialize
        yield return new WaitForSeconds(1f);
        
        ApplyAllManualSettings();
        manualControlActive = true;
        settingsApplied = true;
        
        Debug.Log("ManualEnhancementController: All manual settings applied for LLM trial");
    }
    
    void ApplyAllManualSettings()
    {
        // Disable automatic enhancement systems first
        DisableAutomaticSystems();
        
        // Apply visual enhancements
        ApplyVisualSettings();
        
        // Apply audio enhancements
        ApplyAudioSettings();
        
        // Apply haptic enhancements
        ApplyHapticSettings();
        
        Debug.Log("ManualEnhancementController: Applied all manual enhancement settings");
    }
    
    void DisableAutomaticSystems()
    {
        // Disable UnifiedEnhancementController if it exists
        UnifiedEnhancementController autoController = FindObjectOfType<UnifiedEnhancementController>();
        if (autoController != null)
        {
            autoController.DisableAllEnhancements();
            autoController.enabled = false;
            Debug.Log("ManualEnhancementController: Disabled automatic UnifiedEnhancementController");
        }
        
        // Disable automatic audio and haptic systems
        if (unifiedAudioController != null)
        {
            unifiedAudioController.DisableAudioSystem();
        }
        
        if (spatialHapticController != null)
        {
            spatialHapticController.DisableHaptic();
        }
    }
    
    void ApplyVisualSettings()
    {
        Debug.Log("=== APPLYING VISUAL SETTINGS ===");
        
        // Navigation Line Settings
        if (enableNavigationLineControl && routeGuideSystem != null)
        {
            routeGuideSystem.SetLineWidth(navigationLineWidth);
            routeGuideSystem.SetRouteOpacity(navigationLineOpacity);
            routeGuideSystem.SetRouteColor(navigationLineColor);
            routeGuideSystem.SetRouteVisibility(true);
            
            Debug.Log($"Navigation Line: width={navigationLineWidth:F2}, opacity={navigationLineOpacity:F2}, color={navigationLineColor}");
        }
        
        // Bounding Box Settings
        if (enableBoundingBoxControl && dynamicObjectManager != null)
        {
            dynamicObjectManager.showBoundingBoxes = true;
            dynamicObjectManager.lineWidth = boundingBoxLineWidth;
            dynamicObjectManager.boundingBoxColor = new Color(boundingBoxColor.r, boundingBoxColor.g, boundingBoxColor.b, boundingBoxOpacity);
            dynamicObjectManager.boundingBoxRange = boundingBoxRange;
            
            Debug.Log($"Bounding Boxes: width={boundingBoxLineWidth:F3}, opacity={boundingBoxOpacity:F2}, range={boundingBoxRange:F1}m");
        }
    }
    
    void ApplyAudioSettings()
    {
        Debug.Log("=== APPLYING AUDIO SETTINGS ===");
        
        if (unifiedAudioController == null)
        {
            Debug.LogWarning("ManualEnhancementController: No UnifiedAudioController found - audio settings skipped");
            return;
        }
        
        switch (audioMode)
        {
            case AudioEnhancementMode.TTSSpeech:
                Debug.Log("Audio Mode: TTS Speech");
                SetupManualTTSMode();
                break;
                
            case AudioEnhancementMode.StandardSpearcons:
                Debug.Log("Audio Mode: Standard Spearcons (all objects)");
                SetupManualSpearconMode(false);
                break;
                
            case AudioEnhancementMode.DistanceFilteredSpearcons:
                Debug.Log($"Audio Mode: Distance Filtered Spearcons (beyond {spearconDistanceThreshold}m)");
                SetupManualSpearconMode(true);
                break;
                
            case AudioEnhancementMode.Disabled:
            default:
                Debug.Log("Audio Mode: Disabled");
                unifiedAudioController.DisableAudioSystem();
                break;
        }
    }
    
    void SetupManualTTSMode()
    {
        // Enable manual mode and override settings
        unifiedAudioController.EnableManualMode();
        unifiedAudioController.speechAnnouncementInterval = speechInterval;
        unifiedAudioController.speechDetectionRange = speechRange;
        unifiedAudioController.SetMasterVolume(speechVolume);
        
        // Force TTS mode
        unifiedAudioController.ForceAudioMode(UnifiedAudioController.AudioMode.FullSpeech);
        
        Debug.Log($"TTS Settings: interval={speechInterval}s, range={speechRange}m, volume={speechVolume:F2}");
    }
    
    void SetupManualSpearconMode(bool useDistanceFiltering)
    {
        // Enable manual mode and override settings
        unifiedAudioController.EnableManualMode();
        unifiedAudioController.spearconAnnouncementInterval = spearconInterval;
        unifiedAudioController.spearconDetectionRange = spearconRange;
        unifiedAudioController.SetMasterVolume(spearconVolume);
        
        // Set distance threshold if using filtering
        if (useDistanceFiltering)
        {
            unifiedAudioController.SetObjectClarityDistance(spearconDistanceThreshold);
            unifiedAudioController.ForceAudioMode(UnifiedAudioController.AudioMode.LimitedSpearcons);
        }
        else
        {
            unifiedAudioController.ForceAudioMode(UnifiedAudioController.AudioMode.StandardSpearcons);
        }
        
        string filterInfo = useDistanceFiltering ? $", threshold={spearconDistanceThreshold}m" : "";
        Debug.Log($"Spearcon Settings: interval={spearconInterval}s, range={spearconRange}m, volume={spearconVolume:F2}{filterInfo}");
    }
    
    void ApplyHapticSettings()
    {
        Debug.Log("=== APPLYING HAPTIC SETTINGS ===");
        
        if (spatialHapticController == null)
        {
            Debug.LogWarning("ManualEnhancementController: No SpatialHapticController found - haptic settings skipped");
            return;
        }
        
        if (enableHapticFeedback)
        {
            // Enable custom settings mode
            spatialHapticController.EnableCustomSettings();
            
            // Apply global haptic settings
            spatialHapticController.SetDetectionRange(hapticDetectionRange);
            spatialHapticController.SetMaxHapticObjects(maxHapticObjects);
            
            // Apply individual haptic event settings
            ApplyHapticEventSettings();
            
            // Enable the haptic system
            spatialHapticController.EnableHaptic();
            
            Debug.Log($"Haptic System: enabled with custom settings, range={hapticDetectionRange}m, max objects={maxHapticObjects}");
        }
        else
        {
            spatialHapticController.DisableHaptic();
            Debug.Log("Haptic System: disabled");
        }
    }
    
    void ApplyHapticEventSettings()
    {
        Debug.Log("Haptic Event Settings:");
        
        // Central group: centre_100, centre_leftback_100, centre_rightback_100
        string[] centralEvents = { "centre_100", "centre_leftback_100", "centre_rightback_100" };
        ApplyGroupSettings(centralEvents, centralGroup);
        
        // Left group: left_100, left_back_100
        string[] leftEvents = { "left_100", "left_back_100" };
        ApplyGroupSettings(leftEvents, leftGroup);
        
        // Right group: right_100, right_back_100
        string[] rightEvents = { "right_100", "right_back_100" };
        ApplyGroupSettings(rightEvents, rightGroup);
    }
    
    void ApplyGroupSettings(string[] eventNames, HapticGroupConfig groupConfig)
    {
        Debug.Log($"  {groupConfig.groupName} Group: min={groupConfig.minIntensity:F2}, max={groupConfig.maxIntensity:F2}, interval={groupConfig.feedbackInterval:F1}s");
        
        foreach (string eventName in eventNames)
        {
            Debug.Log($"    -> {eventName}");
            
            // Now these methods exist and will work:
            spatialHapticController.SetEventIntensityRange(eventName, groupConfig.minIntensity, groupConfig.maxIntensity);
            spatialHapticController.SetEventFeedbackInterval(eventName, groupConfig.feedbackInterval);
        }
    }
    
    void DisableManualControl()
    {
        manualControlActive = false;
        settingsApplied = false;
        llmDecisionsLoaded = false;
        llmAssessmentSource = "";
        
        // Disable all manual enhancements
        if (routeGuideSystem != null)
        {
            routeGuideSystem.SetRouteVisibility(false);
        }
        
        if (dynamicObjectManager != null)
        {
            dynamicObjectManager.showBoundingBoxes = false;
        }
        
        if (unifiedAudioController != null)
        {
            unifiedAudioController.DisableAudioSystem();
        }
        
        if (spatialHapticController != null)
        {
            spatialHapticController.DisableHaptic();
        }
        
        // Re-enable automatic systems
        UnifiedEnhancementController autoController = FindObjectOfType<UnifiedEnhancementController>();
        if (autoController != null)
        {
            autoController.enabled = true;
        }
        
        Debug.Log("ManualEnhancementController: Manual control disabled - restored automatic systems");
    }
    
    // Runtime update methods for inspector changes
    void Update()
    {
        // Apply settings in real-time when changed in inspector during LLM trials
        if (manualControlActive && Application.isPlaying)
        {
            UpdateVisualSettings();
            UpdateHapticSettings();
        }
    }
    
    void UpdateVisualSettings()
    {
        // Update navigation line if settings changed
        if (enableNavigationLineControl && routeGuideSystem != null)
        {
            routeGuideSystem.SetLineWidth(navigationLineWidth);
            routeGuideSystem.SetRouteOpacity(navigationLineOpacity);
            
            // Update color if it has an alpha component
            Color colorWithOpacity = new Color(navigationLineColor.r, navigationLineColor.g, navigationLineColor.b, navigationLineOpacity);
            routeGuideSystem.SetRouteColor(colorWithOpacity);
        }
        
        // Update bounding boxes if settings changed
        if (enableBoundingBoxControl && dynamicObjectManager != null)
        {
            dynamicObjectManager.lineWidth = boundingBoxLineWidth;
            dynamicObjectManager.boundingBoxRange = boundingBoxRange;
            
            Color colorWithOpacity = new Color(boundingBoxColor.r, boundingBoxColor.g, boundingBoxColor.b, boundingBoxOpacity);
            dynamicObjectManager.boundingBoxColor = colorWithOpacity;
        }
    }
    
    void UpdateHapticSettings()
    {
        // Update haptic settings in real-time if manual control is active
        if (enableHapticFeedback && spatialHapticController != null && spatialHapticController.IsUsingCustomSettings())
        {
            spatialHapticController.SetDetectionRange(hapticDetectionRange);
            spatialHapticController.SetMaxHapticObjects(maxHapticObjects);
        }
    }
    
    // Context menu methods for testing
    [ContextMenu("Manual: Apply All Settings Now")]
    public void ManualApplyAllSettings()
    {
        if (isLLMTrial)
        {
            ApplyAllManualSettings();
            Debug.Log("ManualEnhancementController: Manually applied all settings");
        }
        else
        {
            Debug.LogWarning("ManualEnhancementController: Cannot apply settings - not in an LLM trial");
        }
    }
    
    [ContextMenu("LLM: Force Load Assessment Decisions")]
    public void ForceLoadLLMDecisions()
    {
        if (TryLoadLLMAssessmentDecisions())
        {
            Debug.Log("ManualEnhancementController: Force loaded LLM decisions successfully");
        }
        else
        {
            Debug.LogError("ManualEnhancementController: Failed to force load LLM decisions");
        }
    }
    
    [ContextMenu("Test: Force Enable Manual Control")]
    public void TestForceEnableManualControl()
    {
        isLLMTrial = true;
        currentTrial = "short_llm";
        ApplyAllManualSettings();
        manualControlActive = true;
        Debug.Log("ManualEnhancementController: Force enabled manual control for testing");
    }
    
    [ContextMenu("Test: Apply Visual Settings Only")]
    public void TestApplyVisualOnly()
    {
        ApplyVisualSettings();
        Debug.Log("ManualEnhancementController: Applied visual settings only");
    }
    
    [ContextMenu("Test: Apply Audio Settings Only")]
    public void TestApplyAudioOnly()
    {
        ApplyAudioSettings();
        Debug.Log("ManualEnhancementController: Applied audio settings only");
    }
    
    [ContextMenu("Test: Apply Haptic Settings Only")]
    public void TestApplyHapticOnly()
    {
        ApplyHapticSettings();
        Debug.Log("ManualEnhancementController: Applied haptic settings only");
    }
    
    [ContextMenu("Debug: Show Current Settings")]
    public void DebugShowCurrentSettings()
    {
        Debug.Log("=== MANUAL ENHANCEMENT CONTROLLER STATUS ===");
        Debug.Log($"Current Trial: {currentTrial}");
        Debug.Log($"Is LLM Trial: {isLLMTrial}");
        Debug.Log($"Manual Control Active: {manualControlActive}");
        Debug.Log($"Settings Applied: {settingsApplied}");
        Debug.Log($"LLM Decisions Loaded: {llmDecisionsLoaded}");
        Debug.Log($"LLM Assessment Source: {llmAssessmentSource}");
        Debug.Log("");
        
        Debug.Log("VISUAL SETTINGS:");
        Debug.Log($"  Navigation Line: {(enableNavigationLineControl ? "ENABLED" : "DISABLED")}");
        if (enableNavigationLineControl)
        {
            Debug.Log($"    Width: {navigationLineWidth:F2}, Opacity: {navigationLineOpacity:F2}");
        }
        Debug.Log($"  Bounding Boxes: {(enableBoundingBoxControl ? "ENABLED" : "DISABLED")}");
        if (enableBoundingBoxControl)
        {
            Debug.Log($"    Width: {boundingBoxLineWidth:F3}, Opacity: {boundingBoxOpacity:F2}, Range: {boundingBoxRange:F1}m");
        }
        Debug.Log("");
        
        Debug.Log($"AUDIO SETTINGS:");
        Debug.Log($"  Mode: {audioMode}");
        if (audioMode == AudioEnhancementMode.TTSSpeech)
        {
            Debug.Log($"    Interval: {speechInterval}s, Range: {speechRange}m, Volume: {speechVolume:F2}");
        }
        else if (audioMode != AudioEnhancementMode.Disabled)
        {
            Debug.Log($"    Interval: {spearconInterval}s, Range: {spearconRange}m, Volume: {spearconVolume:F2}");
            if (audioMode == AudioEnhancementMode.DistanceFilteredSpearcons)
            {
                Debug.Log($"    Distance Threshold: {spearconDistanceThreshold}m");
            }
        }
        Debug.Log("");
        
        Debug.Log($"HAPTIC SETTINGS:");
        Debug.Log($"  Enabled: {enableHapticFeedback}");
        if (enableHapticFeedback)
        {
            Debug.Log($"  Detection Range: {hapticDetectionRange}m");
            Debug.Log($"  Max Haptic Objects: {maxHapticObjects}");
            Debug.Log($"  Central Group: min={centralGroup.minIntensity:F2}, max={centralGroup.maxIntensity:F2}, interval={centralGroup.feedbackInterval:F1}s");
            Debug.Log($"  Left Group: min={leftGroup.minIntensity:F2}, max={leftGroup.maxIntensity:F2}, interval={leftGroup.feedbackInterval:F1}s");
            Debug.Log($"  Right Group: min={rightGroup.minIntensity:F2}, max={rightGroup.maxIntensity:F2}, interval={rightGroup.feedbackInterval:F1}s");
        }
    }
    
    [ContextMenu("Debug: Check System References")]
    public void DebugCheckSystemReferences()
    {
        Debug.Log("=== SYSTEM REFERENCES STATUS ===");
        Debug.Log($"RouteGuideSystem: {(routeGuideSystem != null ? "FOUND" : "MISSING")}");
        Debug.Log($"DynamicObjectManager: {(dynamicObjectManager != null ? "FOUND" : "MISSING")}");
        Debug.Log($"UnifiedAudioController: {(unifiedAudioController != null ? "FOUND" : "MISSING")}");
        Debug.Log($"SpatialHapticController: {(spatialHapticController != null ? "FOUND" : "MISSING")}");
        Debug.Log($"SessionManager: {(SessionManager.Instance != null ? "FOUND" : "MISSING")}");
        
        if (SessionManager.Instance != null)
        {
            Debug.Log($"Current Trial from SessionManager: {SessionManager.Instance.GetCurrentTrial()}");
        }
    }
    
    [ContextMenu("Debug: Show LLM Assessment Path")]
    public void DebugShowLLMAssessmentPath()
    {
        if (SessionManager.Instance != null)
        {
            string sessionPath = SessionManager.Instance.GetSessionPath();
            string llmAssessmentPath = Path.Combine(sessionPath, "04_LLMAssessment");
            
            Debug.Log($"Session Path: {sessionPath}");
            Debug.Log($"LLM Assessment Path: {llmAssessmentPath}");
            Debug.Log($"Path Exists: {Directory.Exists(llmAssessmentPath)}");
            
            if (Directory.Exists(llmAssessmentPath))
            {
                string[] jsonFiles = Directory.GetFiles(llmAssessmentPath, "llm_realtime_assessment_*.json");
                Debug.Log($"JSON Files Found: {jsonFiles.Length}");
                
                foreach (string file in jsonFiles)
                {
                    Debug.Log($"  - {Path.GetFileName(file)} (Created: {File.GetCreationTime(file)})");
                }
            }
        }
        else
        {
            Debug.LogError("SessionManager not available");
        }
    }
    
    void OnDestroy()
    {
        // Clean up event subscriptions
        if (SessionManager.Instance != null)
        {
            SessionManager.OnTrialChanged -= OnTrialChanged;
        }
    }
}