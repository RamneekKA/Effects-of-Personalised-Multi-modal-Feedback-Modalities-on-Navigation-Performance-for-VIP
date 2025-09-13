using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// Unified Enhancement Controller - Single point of control for visual enhancements
/// UPDATED: Added reliable avoidance distance-based bounding box range calculation
/// Handles both bounding boxes and navigation line enhancements
/// Only applies enhancements to short_algorithmic and long_algorithmic trials
/// </summary>
public class UnifiedEnhancementController : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private RouteGuideSystem routeGuideSystem;
    [SerializeField] private DynamicObjectManager dynamicObjectManager;
    [SerializeField] private UnifiedAudioController unifiedAudioController;
    
    [Header("Enhancement Settings")]
    [Range(0.1f, 3.0f)]
    public float navigationLineWidthRange = 0.45f;
    [Range(0f, 1f)]
    public float navigationLineOpacityRange = 1.0f;
    public Color navigationLineColor = Color.cyan;
    
    [Range(0.02f, 0.3f)]
    public float boundingBoxWidthRange = 0.15f;
    [Range(0f, 1f)]
    public float boundingBoxOpacityRange = 1.0f;
    public Color boundingBoxColor = Color.green;
    
    [Header("Vision Thresholds")]
    [Range(1, 10)]
    public int poorVisionThreshold = 3;
    
    [Header("Debug Information")]
    [SerializeField] private VisualEnhancementSettings currentEnhancementSettings;
    [SerializeField] private bool enhancementsActive = false;
    [SerializeField] private string currentTrialType;
    [SerializeField] private int visionRating = 5;
    [SerializeField] private float reliableAvoidanceDistance = 2.5f; // NEW: Debug display
    [SerializeField] private float calculatedBoundingBoxRange = 25f; // NEW: Debug display
    [SerializeField] private bool loadedFromFile = false;
    
    // Internal state
    private bool systemInitialized = false;
    
    void Start()
    {
        InitializeSystem();
        
        // Wait a frame for all systems to be ready
        StartCoroutine(DelayedInitialization());
    }
    
    void InitializeSystem()
    {
        // Auto-find system references if not assigned
        if (routeGuideSystem == null)
            routeGuideSystem = FindObjectOfType<RouteGuideSystem>();
        
        if (dynamicObjectManager == null)
            dynamicObjectManager = FindObjectOfType<DynamicObjectManager>();
        
        if (unifiedAudioController == null)
            unifiedAudioController = FindObjectOfType<UnifiedAudioController>();
        
        Debug.Log("UnifiedEnhancementController: System references initialized");
    }
    
    IEnumerator DelayedInitialization()
    {
        yield return new WaitForEndOfFrame();
        
        // Determine current trial type
        if (SessionManager.Instance != null)
        {
            currentTrialType = SessionManager.Instance.GetCurrentTrial();
            
            // ONLY apply enhancements for algorithmic trials
            if (IsAlgorithmicTrial(currentTrialType))
            {
                Debug.Log($"UnifiedEnhancementController: Applying algorithmic enhancements for trial: {currentTrialType}");
                yield return new WaitForSeconds(0.5f);
                StartCoroutine(ApplyAlgorithmicEnhancements());
            }
            else
            {
                Debug.Log($"UnifiedEnhancementController: No enhancements applied - trial '{currentTrialType}' is not an algorithmic enhancement trial");
                // Explicitly disable any existing enhancements
                DisableAllEnhancements();
            }
        }
        else
        {
            Debug.LogWarning("UnifiedEnhancementController: SessionManager not found - no enhancements applied");
            DisableAllEnhancements();
        }
        
        systemInitialized = true;
    }
    
    bool IsAlgorithmicTrial(string trialType)
    {
        // ONLY apply algorithmic enhancements to these specific trials
        return trialType == "short_algorithmic" || trialType == "long_algorithmic";
    }
    
    IEnumerator ApplyAlgorithmicEnhancements()
    {
        if (!ValidateSystemReferences())
        {
            Debug.LogError("UnifiedEnhancementController: Required system references missing");
            yield break;
        }
        
        UserSession session = SessionManager.Instance.GetCurrentSession();
        if (session == null)
        {
            Debug.LogError("UnifiedEnhancementController: No session data found");
            yield break;
        }
        
        // Try to load algorithmic assessment results
        AlgorithmicAssessmentResults algorithmicResults = LoadAlgorithmicResults(session);
        if (algorithmicResults == null || !algorithmicResults.completed)
        {
            Debug.LogError("UnifiedEnhancementController: No completed algorithmic assessment found");
            yield break;
        }
        
        visionRating = algorithmicResults.centralVisionRating;
        reliableAvoidanceDistance = algorithmicResults.reliableAvoidanceDistance; // NEW: Store for debugging
        
        Debug.Log($"UnifiedEnhancementController: Processing vision rating {visionRating}/10, avoidance distance {reliableAvoidanceDistance}m");
        
        // Get baseline navigation data for collision analysis
        NavigationSession baselineSession = GetBaselineNavigationSession(session);
        
        // Generate enhancement settings with new avoidance-based range calculation
        currentEnhancementSettings = GenerateEnhancementSettings(visionRating, baselineSession, algorithmicResults);
        
        // Apply the enhancements
        ApplyEnhancements(currentEnhancementSettings);
        
        Debug.Log("UnifiedEnhancementController: Algorithmic enhancements successfully applied");
    }
    
    bool ValidateSystemReferences()
    {
        bool isValid = true;
        
        if (routeGuideSystem == null)
        {
            Debug.LogError("UnifiedEnhancementController: RouteGuideSystem not found");
            isValid = false;
        }
        
        if (dynamicObjectManager == null)
        {
            Debug.LogError("UnifiedEnhancementController: DynamicObjectManager not found");
            isValid = false;
        }
        
        return isValid;
    }
    
    AlgorithmicAssessmentResults LoadAlgorithmicResults(UserSession session)
    {
        // First try session memory
        if (session.algorithmicResults != null && session.algorithmicResults.completed)
        {
            return session.algorithmicResults;
        }
        
        // Fallback: load from file
        try
        {
            string sessionPath = SessionManager.Instance.GetSessionPath();
            string assessmentPath = Path.Combine(sessionPath, "03_AlgorithmicAssessment");
            
            if (!Directory.Exists(assessmentPath))
            {
                Debug.LogError($"UnifiedEnhancementController: Assessment folder not found: {assessmentPath}");
                return null;
            }
            
            string[] files = Directory.GetFiles(assessmentPath, "algorithmic_assessment_*.json");
            if (files.Length == 0)
            {
                Debug.LogError($"UnifiedEnhancementController: No assessment files found");
                return null;
            }
            
            string latestFile = files.OrderByDescending(f => File.GetCreationTime(f)).First();
            string jsonData = File.ReadAllText(latestFile);
            
            AlgorithmicAssessmentResults results = JsonUtility.FromJson<AlgorithmicAssessmentResults>(jsonData);
            
            if (results != null && results.completed)
            {
                loadedFromFile = true;
                Debug.Log($"UnifiedEnhancementController: Loaded assessment from file - vision: {results.centralVisionRating}/10, avoidance: {results.reliableAvoidanceDistance}m");
                
                // Update session with loaded data
                session.algorithmicResults = results;
                SessionManager.Instance.SaveSessionData();
                
                return results;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UnifiedEnhancementController: Error loading assessment: {e.Message}");
        }
        
        return null;
    }
    
    NavigationSession GetBaselineNavigationSession(UserSession session)
    {
        if (session.baselineResults?.shortDistanceSession != null)
            return session.baselineResults.shortDistanceSession;
        
        // Fallback to any available navigation session
        if (session.shortLLMResults?.navigationSession != null)
            return session.shortLLMResults.navigationSession;
        if (session.shortAlgorithmicResults?.navigationSession != null)
            return session.shortAlgorithmicResults.navigationSession;
        
        return null;
    }
    
    /// <summary>
    /// UPDATED: Generate enhancement settings with reliable avoidance distance-based bounding box range
    /// </summary>
    VisualEnhancementSettings GenerateEnhancementSettings(int visionRating, NavigationSession baselineSession, AlgorithmicAssessmentResults assessmentResults)
    {
        VisualEnhancementSettings settings = new VisualEnhancementSettings();
        
        // Calculate bounding box range based on reliable avoidance distance
        float boundingBoxRange = CalculateAvoidanceBasedRange(assessmentResults.reliableAvoidanceDistance);
        calculatedBoundingBoxRange = boundingBoxRange; // Store for debugging
        
        // Determine if maximum enhancement is needed for other visual elements
        bool hasCollisions = baselineSession?.totalCollisions > 0;
        bool useMaximumEnhancement = visionRating <= poorVisionThreshold || hasCollisions;
        
        if (useMaximumEnhancement)
        {
            // Maximum enhancement for lines and opacity, but calculated range for bounding box distance
            settings.enableBoundingBoxes = true;
            settings.boundingBoxLineWidth = boundingBoxWidthRange;
            settings.boundingBoxOpacity = boundingBoxOpacityRange * 255f;
            settings.boundingBoxRange = boundingBoxRange; // NEW: Use calculated range instead of fixed
            settings.enhanceNavigationLine = true;
            settings.navigationLineWidth = navigationLineWidthRange;
            settings.navigationLineOpacity = navigationLineOpacityRange * 255f;
            settings.decisionReason = $"Maximum visual enhancement applied. Vision: {visionRating}/10, Avoidance distance: {assessmentResults.reliableAvoidanceDistance}m → Range: {boundingBoxRange}m" + 
                                    (hasCollisions ? $", Baseline collisions: {baselineSession.totalCollisions}" : "");
        }
        else
        {
            // Scaled enhancement based on vision rating, but always use calculated range for bounding boxes
            float enhancementScale = Mathf.InverseLerp(10f, poorVisionThreshold + 1f, visionRating);
            
            settings.enableBoundingBoxes = true;
            settings.boundingBoxLineWidth = Mathf.Lerp(0.05f, boundingBoxWidthRange, enhancementScale);
            settings.boundingBoxOpacity = Mathf.Lerp(200f, boundingBoxOpacityRange * 255f, enhancementScale);
            settings.boundingBoxRange = boundingBoxRange; // NEW: Use calculated range instead of fixed
            settings.enhanceNavigationLine = true;
            settings.navigationLineWidth = Mathf.Lerp(0.25f, navigationLineWidthRange, enhancementScale);
            settings.navigationLineOpacity = Mathf.Lerp(200f, navigationLineOpacityRange * 255f, enhancementScale);
            settings.decisionReason = $"Scaled visual enhancement. Vision: {visionRating}/10, Avoidance distance: {assessmentResults.reliableAvoidanceDistance}m → Range: {boundingBoxRange}m";
        }
        
        // Set object type enhancements
        settings.enhanceStaticObjects = true;
        settings.enhanceDynamicObjects = true;
        
        // Store metadata including new avoidance distance info
        settings.centralVisionRating = visionRating;
        settings.reliableAvoidanceDistance = assessmentResults.reliableAvoidanceDistance; // NEW
        settings.hadCollisions = hasCollisions;
        settings.totalCollisions = baselineSession?.totalCollisions ?? 0;
        
        return settings;
    }
    
    /// <summary>
    /// NEW: Calculate bounding box range based on reliable avoidance distance
    /// Uses the specified ranges based on user's self-reported avoidance capability
    /// </summary>
    private float CalculateAvoidanceBasedRange(float avoidanceDistance)
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
    
    void ApplyEnhancements(VisualEnhancementSettings settings)
    {
        enhancementsActive = true;
        currentEnhancementSettings = settings;
        
        // Apply navigation line enhancements directly
        if (settings.enhanceNavigationLine && routeGuideSystem != null)
        {
            routeGuideSystem.SetLineWidth(settings.navigationLineWidth);
            
            Color navColor = new Color(navigationLineColor.r, navigationLineColor.g, navigationLineColor.b, 
                                     settings.navigationLineOpacity / 255f);
            routeGuideSystem.SetRouteColor(navColor);
            routeGuideSystem.SetRouteVisibility(true);
            
            Debug.Log($"UnifiedEnhancementController: Navigation line - width: {settings.navigationLineWidth:F2}, opacity: {settings.navigationLineOpacity:F0}");
        }
        
        // Apply bounding box enhancements with calculated range
        if (settings.enableBoundingBoxes && dynamicObjectManager != null)
        {
            dynamicObjectManager.showBoundingBoxes = true;
            dynamicObjectManager.lineWidth = settings.boundingBoxLineWidth;
            
            Color boundingColor = new Color(boundingBoxColor.r, boundingBoxColor.g, boundingBoxColor.b, 
                                          settings.boundingBoxOpacity / 255f);
            dynamicObjectManager.boundingBoxColor = boundingColor;
            dynamicObjectManager.boundingBoxRange = settings.boundingBoxRange; // NEW: Use calculated range
            
            Debug.Log($"UnifiedEnhancementController: Bounding boxes - width: {settings.boundingBoxLineWidth:F3}, opacity: {settings.boundingBoxOpacity:F0}, range: {settings.boundingBoxRange:F1}m");
        }
        
        Debug.Log($"UnifiedEnhancementController: Enhancement decision - {settings.decisionReason}");
        
        // Save complete enhancement settings to JSON
        SaveCompleteEnhancementSettings(settings);
    }
    
    void SaveCompleteEnhancementSettings(VisualEnhancementSettings visualSettings)
    {
        if (SessionManager.Instance == null) return;
        
        var completeSettings = new CompleteEnhancementSettings();
        completeSettings.visualSettings = visualSettings;
        completeSettings.trialType = currentTrialType;
        completeSettings.timestamp = System.DateTime.Now.ToString();
        completeSettings.visionRating = visionRating;
        
        // Get algorithmic assessment results for complete data
        UserSession session = SessionManager.Instance.GetCurrentSession();
        AlgorithmicAssessmentResults assessmentResults = session?.algorithmicResults;
        
        // Collect audio settings with assessment-derived values
        if (unifiedAudioController != null && assessmentResults != null)
        {
            completeSettings.audioSettings = new AudioEnhancementSettings
            {
                // Assessment-derived values
                centralVisionScore = assessmentResults.centralVisionRating,
                objectClarityDistance = assessmentResults.objectClarityDistance,
                reliableAvoidanceDistance = assessmentResults.reliableAvoidanceDistance,
                
                // Logic-derived values
                audioEnabled = unifiedAudioController.IsSystemActive(),
                audioMode = unifiedAudioController.GetCurrentAudioMode().ToString(),
                masterVolume = unifiedAudioController.masterVolume,
                decisionReason = GenerateAudioDecisionReason(assessmentResults.centralVisionRating)
            };
        }
        
        // Collect haptic settings with assessment-derived values
        var hapticController = FindObjectOfType<SpatialHapticController>();
        if (hapticController != null && assessmentResults != null)
        {
            completeSettings.hapticSettings = new HapticEnhancementSettings
            {
                // Assessment-derived values
                centralVisionScore = assessmentResults.centralVisionRating,
                leftPeripheralScore = assessmentResults.leftPeripheralRating,
                rightPeripheralScore = assessmentResults.rightPeripheralRating,
                
                // Logic-derived intensity ranges
                frontIntensityRange = CalculateHapticIntensityRange(assessmentResults.centralVisionRating, "Front"),
                leftIntensityRange = CalculateHapticIntensityRange(assessmentResults.leftPeripheralRating, "Left"),
                rightIntensityRange = CalculateHapticIntensityRange(assessmentResults.rightPeripheralRating, "Right"),
                
                // Applied settings
                hapticEnabled = hapticController.IsHapticEnabled(),
                baseIntensity = hapticController.baseIntensity,
                detectionRange = hapticController.detectionRange,
                maxHapticObjects = hapticController.maxHapticObjects,
                usingCustomSettings = hapticController.IsUsingCustomSettings(),
                decisionReason = GenerateHapticDecisionReason(assessmentResults)
            };
        }
        
        // Save to JSON
        string trialDataPath = SessionManager.Instance.GetTrialDataPath(currentTrialType);
        string jsonPath = Path.Combine(trialDataPath, "complete_enhancements.json");
        
        string jsonData = JsonUtility.ToJson(completeSettings, true);
        File.WriteAllText(jsonPath, jsonData);
        
        Debug.Log($"Complete enhancement settings saved to: {jsonPath}");
        Debug.Log($"Saved bounding box range: {visualSettings.boundingBoxRange:F1}m (calculated from avoidance distance: {assessmentResults?.reliableAvoidanceDistance:F1}m)");
    }
    
    string GenerateAudioDecisionReason(int centralVisionScore)
    {
        if (centralVisionScore >= 1 && centralVisionScore <= 3)
        {
            return $"Full TTS Speech mode: Vision score {centralVisionScore}/10 (range 1-3) = poor vision requires detailed verbal descriptions";
        }
        else if (centralVisionScore >= 4 && centralVisionScore <= 6)
        {
            return $"Standard Spearcons mode: Vision score {centralVisionScore}/10 (range 4-6) = moderate vision uses audio clips for all nearby objects";
        }
        else if (centralVisionScore >= 7 && centralVisionScore <= 10)
        {
            return $"Limited Spearcons mode: Vision score {centralVisionScore}/10 (range 7-10) = good vision only needs audio for distant/unclear objects";
        }
        else
        {
            return $"Audio disabled: Invalid vision score ({centralVisionScore})";
        }
    }
    
    HapticIntensityRange CalculateHapticIntensityRange(int visionScore, string direction)
    {
        HapticIntensityRange range = new HapticIntensityRange();
        
        if (visionScore >= 1 && visionScore <= 3)
        {
            range.minIntensity = 0.7f;
            range.maxIntensity = 1.0f;
            range.reasoning = $"Vision score {visionScore}/10 (range 1-3) = poor vision requires strong haptic feedback (70%-100% intensity)";
        }
        else if (visionScore >= 4 && visionScore <= 6)
        {
            range.minIntensity = 0.4f;
            range.maxIntensity = 0.6f;
            range.reasoning = $"Vision score {visionScore}/10 (range 4-6) = moderate vision uses medium haptic feedback (40%-60% intensity)";
        }
        else if (visionScore >= 7 && visionScore <= 10)
        {
            range.minIntensity = 0.1f;
            range.maxIntensity = 0.3f;
            range.reasoning = $"Vision score {visionScore}/10 (range 7-10) = good vision uses subtle haptic feedback (10%-30% intensity)";
        }
        else
        {
            range.minIntensity = 0.0f;
            range.maxIntensity = 0.0f;
            range.reasoning = $"Invalid vision score ({visionScore}) = no haptic feedback";
        }
        
        return range;
    }
    
    string GenerateHapticDecisionReason(AlgorithmicAssessmentResults assessment)
    {
        return $"Haptic intensity ranges calculated from assessment: Central={assessment.centralVisionRating}/10, " +
               $"Left={assessment.leftPeripheralRating}/10, Right={assessment.rightPeripheralRating}/10. " +
               $"Each direction gets different intensity caps based on corresponding vision score.";
    }
    
    // PUBLIC API METHODS - Runtime control
    
    public void SetNavigationLineWidth(float width)
    {
        if (currentEnhancementSettings != null && routeGuideSystem != null && enhancementsActive)
        {
            currentEnhancementSettings.navigationLineWidth = Mathf.Clamp(width, 0.1f, 3.0f);
            routeGuideSystem.SetLineWidth(currentEnhancementSettings.navigationLineWidth);
            Debug.Log($"UnifiedEnhancementController: Navigation line width updated to {width:F2}");
        }
    }
    
    public void SetNavigationLineOpacity(float opacity01)
    {
        if (currentEnhancementSettings != null && routeGuideSystem != null && enhancementsActive)
        {
            currentEnhancementSettings.navigationLineOpacity = Mathf.Clamp(opacity01 * 255f, 0f, 255f);
            Color newColor = new Color(navigationLineColor.r, navigationLineColor.g, navigationLineColor.b, opacity01);
            routeGuideSystem.SetRouteColor(newColor);
            Debug.Log($"UnifiedEnhancementController: Navigation line opacity updated to {opacity01:F2}");
        }
    }
    
    public void SetBoundingBoxWidth(float width)
    {
        if (currentEnhancementSettings != null && dynamicObjectManager != null && enhancementsActive)
        {
            currentEnhancementSettings.boundingBoxLineWidth = Mathf.Clamp(width, 0.02f, 0.3f);
            dynamicObjectManager.lineWidth = currentEnhancementSettings.boundingBoxLineWidth;
            Debug.Log($"UnifiedEnhancementController: Bounding box width updated to {width:F3}");
        }
    }
    
    public void SetBoundingBoxOpacity(float opacity01)
    {
        if (currentEnhancementSettings != null && dynamicObjectManager != null && enhancementsActive)
        {
            currentEnhancementSettings.boundingBoxOpacity = Mathf.Clamp(opacity01 * 255f, 0f, 255f);
            Color newColor = new Color(boundingBoxColor.r, boundingBoxColor.g, boundingBoxColor.b, opacity01);
            dynamicObjectManager.boundingBoxColor = newColor;
            Debug.Log($"UnifiedEnhancementController: Bounding box opacity updated to {opacity01:F2}");
        }
    }
    
    public void DisableAllEnhancements()
    {
        enhancementsActive = false;
        
        if (dynamicObjectManager != null)
            dynamicObjectManager.showBoundingBoxes = false;
        
        if (routeGuideSystem != null)
            routeGuideSystem.SetRouteVisibility(false);
        
        Debug.Log("UnifiedEnhancementController: All enhancements disabled");
    }
    
    // Public getters
    public VisualEnhancementSettings GetCurrentEnhancements() => currentEnhancementSettings;
    public bool AreEnhancementsActive() => enhancementsActive && systemInitialized;
    public int GetCurrentVisionRating() => visionRating;
    public float GetReliableAvoidanceDistance() => reliableAvoidanceDistance; // NEW
    public float GetCalculatedBoundingBoxRange() => calculatedBoundingBoxRange; // NEW
    public bool WasLoadedFromFile() => loadedFromFile;
    
    // Context menu methods for testing
    [ContextMenu("Test: Apply Maximum Enhancements")]
    public void TestMaximumEnhancements()
    {
        var mockAssessment = new AlgorithmicAssessmentResults
        {
            centralVisionRating = 3,
            reliableAvoidanceDistance = 1.0f, // Should result in 25m range
            completed = true
        };
        
        var testSettings = GenerateEnhancementSettings(3, null, mockAssessment);
        ApplyEnhancements(testSettings);
        Debug.Log("UnifiedEnhancementController: Applied maximum test enhancements with calculated range");
    }
    
    [ContextMenu("Test: Apply Scaled Enhancements")]
    public void TestScaledEnhancements()
    {
        var mockAssessment = new AlgorithmicAssessmentResults
        {
            centralVisionRating = 7,
            reliableAvoidanceDistance = 4.5f, // Should result in 5m range
            completed = true
        };
        
        var testSettings = GenerateEnhancementSettings(7, null, mockAssessment);
        ApplyEnhancements(testSettings);
        Debug.Log("UnifiedEnhancementController: Applied scaled test enhancements with calculated range");
    }
    
    [ContextMenu("Test: Disable Enhancements")]
    public void TestDisableEnhancements()
    {
        DisableAllEnhancements();
    }
    
    [ContextMenu("Debug: Show Current Status")]
    public void DebugShowStatus()
    {
        Debug.Log("=== UNIFIED ENHANCEMENT CONTROLLER STATUS ===");
        Debug.Log($"System Initialized: {systemInitialized}");
        Debug.Log($"Current Trial: {currentTrialType}");
        Debug.Log($"Enhancements Active: {enhancementsActive}");
        Debug.Log($"Vision Rating: {visionRating}/10");
        Debug.Log($"Reliable Avoidance Distance: {reliableAvoidanceDistance}m");
        Debug.Log($"Calculated Bounding Box Range: {calculatedBoundingBoxRange}m");
        Debug.Log($"Loaded From File: {loadedFromFile}");
        Debug.Log($"Route System: {(routeGuideSystem != null ? "FOUND" : "MISSING")}");
        Debug.Log($"Dynamic Object Manager: {(dynamicObjectManager != null ? "FOUND" : "MISSING")}");
        
        if (currentEnhancementSettings != null)
        {
            Debug.Log($"Current Settings: {currentEnhancementSettings.decisionReason}");
            Debug.Log($"Navigation Line: {currentEnhancementSettings.enhanceNavigationLine} (width: {currentEnhancementSettings.navigationLineWidth:F2}, opacity: {currentEnhancementSettings.navigationLineOpacity:F0})");
            Debug.Log($"Bounding Boxes: {currentEnhancementSettings.enableBoundingBoxes} (width: {currentEnhancementSettings.boundingBoxLineWidth:F3}, opacity: {currentEnhancementSettings.boundingBoxOpacity:F0}, range: {currentEnhancementSettings.boundingBoxRange:F1}m)");
        }
    }
    
    [ContextMenu("Test: Range Calculation Examples")]
    public void TestRangeCalculationExamples()
    {
        Debug.Log("=== BOUNDING BOX RANGE CALCULATION TEST ===");
        
        float[] testDistances = { 0.5f, 1.0f, 1.5f, 2.0f, 3.0f, 3.5f, 4.0f, 4.5f, 5.0f };
        
        foreach (float distance in testDistances)
        {
            float calculatedRange = CalculateAvoidanceBasedRange(distance);
            Debug.Log($"Avoidance Distance: {distance}m → Bounding Box Range: {calculatedRange}m");
        }
        
        Debug.Log("=== RANGE CALCULATION BRACKETS ===");
        Debug.Log("0.5m → 35m range");
        Debug.Log("0.6-1.5m → 25m range"); 
        Debug.Log("1.6-3.0m → 15m range");
        Debug.Log("3.1-4.0m → 10m range");
        Debug.Log("4.1-5.0m → 5m range");
    }
    
    [ContextMenu("Debug: Check Current Trial Enhancement Status")]
    public void DebugCheckTrialEnhancementStatus()
    {
        string trial = SessionManager.Instance?.GetCurrentTrial() ?? "None";
        bool shouldEnhance = IsAlgorithmicTrial(trial);
        
        Debug.Log($"=== TRIAL ENHANCEMENT CHECK ===");
        Debug.Log($"Current Trial: {trial}");
        Debug.Log($"Should Apply Enhancements: {shouldEnhance}");
        Debug.Log($"Enhancements Currently Active: {enhancementsActive}");
        Debug.Log($"System Initialized: {systemInitialized}");
        
        if (shouldEnhance && !enhancementsActive && systemInitialized)
        {
            Debug.LogWarning("Enhancements should be active but are not! Check assessment completion.");
        }
        else if (!shouldEnhance && enhancementsActive)
        {
            Debug.LogWarning("Enhancements are active but shouldn't be for this trial type!");
        }
        else
        {
            Debug.Log("Enhancement status matches trial requirements ✓");
        }
    }
    
    [ContextMenu("Debug: Force Load Assessment")]
    public void DebugForceLoadAssessment()
    {
        if (SessionManager.Instance != null)
        {
            var session = SessionManager.Instance.GetCurrentSession();
            var results = LoadAlgorithmicResults(session);
            
            if (results != null)
            {
                Debug.Log($"Loaded assessment: Vision {results.centralVisionRating}/10, Avoidance {results.reliableAvoidanceDistance}m, Completed: {results.completed}");
                float testRange = CalculateAvoidanceBasedRange(results.reliableAvoidanceDistance);
                Debug.Log($"Would calculate bounding box range: {testRange}m");
            }
            else
            {
                Debug.LogError("Failed to load algorithmic assessment");
            }
        }
        else
        {
            Debug.LogError("SessionManager not available");
        }
    }
}