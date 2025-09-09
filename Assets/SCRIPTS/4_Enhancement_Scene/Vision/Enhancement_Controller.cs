using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// Unified Enhancement Controller - Single point of control for visual enhancements
/// CLEANED: Removed old AppliedEnhancements system references
/// Handles both bounding boxes and navigation line enhancements
/// Only applies enhancements to short_algorithmic and long_algorithmic trials
/// </summary>
public class UnifiedEnhancementController : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private RouteGuideSystem routeGuideSystem;
    [SerializeField] private DynamicObjectManager dynamicObjectManager;
    
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
        Debug.Log($"UnifiedEnhancementController: Processing vision rating {visionRating}/10");
        
        // Get baseline navigation data for collision analysis
        NavigationSession baselineSession = GetBaselineNavigationSession(session);
        
        // Generate enhancement settings directly (no external generator needed)
        currentEnhancementSettings = GenerateEnhancementSettings(visionRating, baselineSession);
        
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
                Debug.Log($"UnifiedEnhancementController: Loaded assessment from file - vision rating: {results.centralVisionRating}/10");
                
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
    
    VisualEnhancementSettings GenerateEnhancementSettings(int visionRating, NavigationSession baselineSession)
    {
        VisualEnhancementSettings settings = new VisualEnhancementSettings();
        
        // Determine if maximum enhancement is needed
        bool hasCollisions = baselineSession?.totalCollisions > 0;
        bool useMaximumEnhancement = visionRating <= poorVisionThreshold || hasCollisions;
        
        if (useMaximumEnhancement)
        {
            // Maximum enhancement
            settings.enableBoundingBoxes = true;
            settings.boundingBoxLineWidth = boundingBoxWidthRange;
            settings.boundingBoxOpacity = boundingBoxOpacityRange * 255f;
            settings.enhanceNavigationLine = true;
            settings.navigationLineWidth = navigationLineWidthRange;
            settings.navigationLineOpacity = navigationLineOpacityRange * 255f;
            settings.decisionReason = $"Maximum enhancement applied. Vision rating: {visionRating}/10" + 
                                    (hasCollisions ? $", Baseline collisions: {baselineSession.totalCollisions}" : "");
        }
        else
        {
            // Scaled enhancement based on vision rating
            float enhancementScale = Mathf.InverseLerp(10f, poorVisionThreshold + 1f, visionRating);
            
            settings.enableBoundingBoxes = true;
            settings.boundingBoxLineWidth = Mathf.Lerp(0.05f, boundingBoxWidthRange, enhancementScale);
            settings.boundingBoxOpacity = Mathf.Lerp(200f, boundingBoxOpacityRange * 255f, enhancementScale);
            settings.enhanceNavigationLine = true;
            settings.navigationLineWidth = Mathf.Lerp(0.25f, navigationLineWidthRange, enhancementScale);
            settings.navigationLineOpacity = Mathf.Lerp(200f, navigationLineOpacityRange * 255f, enhancementScale);
            settings.decisionReason = $"Scaled enhancement applied based on vision rating: {visionRating}/10";
        }
        
        // Set object type enhancements
        settings.enhanceStaticObjects = true;
        settings.enhanceDynamicObjects = true;
        
        // Store metadata
        settings.centralVisionRating = visionRating;
        settings.hadCollisions = hasCollisions;
        settings.totalCollisions = baselineSession?.totalCollisions ?? 0;
        
        return settings;
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
        
        // Apply bounding box enhancements directly
        if (settings.enableBoundingBoxes && dynamicObjectManager != null)
        {
            dynamicObjectManager.showBoundingBoxes = true;
            dynamicObjectManager.lineWidth = settings.boundingBoxLineWidth;
            
            Color boundingColor = new Color(boundingBoxColor.r, boundingBoxColor.g, boundingBoxColor.b, 
                                          settings.boundingBoxOpacity / 255f);
            dynamicObjectManager.boundingBoxColor = boundingColor;
            dynamicObjectManager.boundingBoxRange = 50f;
            
            Debug.Log($"UnifiedEnhancementController: Bounding boxes - width: {settings.boundingBoxLineWidth:F3}, opacity: {settings.boundingBoxOpacity:F0}");
        }
        
        Debug.Log($"UnifiedEnhancementController: Enhancement decision - {settings.decisionReason}");
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
    public bool WasLoadedFromFile() => loadedFromFile;
    
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
            decisionReason = "Test: Maximum enhancement",
            centralVisionRating = 3
        };
        
        ApplyEnhancements(testSettings);
        Debug.Log("UnifiedEnhancementController: Applied maximum test enhancements");
    }
    
    [ContextMenu("Test: Apply Scaled Enhancements")]
    public void TestScaledEnhancements()
    {
        var testSettings = GenerateEnhancementSettings(7, null); // Good vision, no collisions
        ApplyEnhancements(testSettings);
        Debug.Log("UnifiedEnhancementController: Applied scaled test enhancements");
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
        Debug.Log($"Loaded From File: {loadedFromFile}");
        Debug.Log($"Route System: {(routeGuideSystem != null ? "FOUND" : "MISSING")}");
        Debug.Log($"Dynamic Object Manager: {(dynamicObjectManager != null ? "FOUND" : "MISSING")}");
        
        if (currentEnhancementSettings != null)
        {
            Debug.Log($"Current Settings: {currentEnhancementSettings.decisionReason}");
            Debug.Log($"Navigation Line: {currentEnhancementSettings.enhanceNavigationLine} (width: {currentEnhancementSettings.navigationLineWidth:F2}, opacity: {currentEnhancementSettings.navigationLineOpacity:F0})");
            Debug.Log($"Bounding Boxes: {currentEnhancementSettings.enableBoundingBoxes} (width: {currentEnhancementSettings.boundingBoxLineWidth:F3}, opacity: {currentEnhancementSettings.boundingBoxOpacity:F0})");
        }
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
                Debug.Log($"Loaded assessment: Vision rating {results.centralVisionRating}/10, Completed: {results.completed}");
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
    
    // Debug test scenarios
    [ContextMenu("Debug Scenario 1: CV=3, No Collisions")]
    public void DebugScenario1_CV3_NoCollisions()
    {
        Debug.Log("=== DEBUG SCENARIO 1: CV=3, NO COLLISIONS ===");
        var mockBaseline = CreateMockBaselineSession(0); // No collisions
        var settings = GenerateEnhancementSettings(3, mockBaseline);
        LogEnhancementDetails(settings, "CV=3, No Collisions");
        ApplyEnhancements(settings);
    }
    
    [ContextMenu("Debug Scenario 2: CV=5, No Collisions")]
    public void DebugScenario2_CV5_NoCollisions()
    {
        Debug.Log("=== DEBUG SCENARIO 2: CV=5, NO COLLISIONS ===");
        var mockBaseline = CreateMockBaselineSession(0); // No collisions
        var settings = GenerateEnhancementSettings(5, mockBaseline);
        LogEnhancementDetails(settings, "CV=5, No Collisions");
        ApplyEnhancements(settings);
    }
    
    [ContextMenu("Debug Scenario 3: CV=7, No Collisions")]
    public void DebugScenario3_CV7_NoCollisions()
    {
        Debug.Log("=== DEBUG SCENARIO 3: CV=7, NO COLLISIONS ===");
        var mockBaseline = CreateMockBaselineSession(0); // No collisions
        var settings = GenerateEnhancementSettings(7, mockBaseline);
        LogEnhancementDetails(settings, "CV=7, No Collisions");
        ApplyEnhancements(settings);
    }
    
    [ContextMenu("Debug Scenario 4: CV=10, No Collisions")]
    public void DebugScenario4_CV10_NoCollisions()
    {
        Debug.Log("=== DEBUG SCENARIO 4: CV=10, NO COLLISIONS ===");
        var mockBaseline = CreateMockBaselineSession(0); // No collisions
        var settings = GenerateEnhancementSettings(10, mockBaseline);
        LogEnhancementDetails(settings, "CV=10, No Collisions");
        ApplyEnhancements(settings);
    }
    
    [ContextMenu("Debug Scenario 5: CV=7, Dynamic Object Collisions")]
    public void DebugScenario5_CV7_DynamicCollisions()
    {
        Debug.Log("=== DEBUG SCENARIO 5: CV=7, DYNAMIC OBJECT COLLISIONS ===");
        var mockBaseline = CreateMockBaselineSession(3, true, false); // 3 dynamic collisions
        var settings = GenerateEnhancementSettings(7, mockBaseline);
        LogEnhancementDetails(settings, "CV=7, Dynamic Object Collisions");
        ApplyEnhancements(settings);
    }
    
    [ContextMenu("Debug Scenario 6: CV=7, Static Object Collisions")]
    public void DebugScenario6_CV7_StaticCollisions()
    {
        Debug.Log("=== DEBUG SCENARIO 6: CV=7, STATIC OBJECT COLLISIONS ===");
        var mockBaseline = CreateMockBaselineSession(4, false, true); // 4 static collisions
        var settings = GenerateEnhancementSettings(7, mockBaseline);
        LogEnhancementDetails(settings, "CV=7, Static Object Collisions");
        ApplyEnhancements(settings);
    }
    
    NavigationSession CreateMockBaselineSession(int totalCollisions, bool hasDynamicCollisions = false, bool hasStaticCollisions = false)
    {
        NavigationSession mockSession = new NavigationSession();
        mockSession.totalCollisions = totalCollisions;
        
        if (totalCollisions > 0)
        {
            mockSession.collisionsByObjectType = new System.Collections.Generic.Dictionary<string, int>();
            
            if (hasDynamicCollisions)
            {
                mockSession.collisionsByObjectType["Car"] = totalCollisions / 2;
                mockSession.collisionsByObjectType["Bus"] = (totalCollisions + 1) / 2;
            }
            
            if (hasStaticCollisions)
            {
                mockSession.collisionsByObjectType["Tree"] = totalCollisions / 2;
                mockSession.collisionsByObjectType["Pole"] = (totalCollisions + 1) / 2;
            }
            
            if (!hasDynamicCollisions && !hasStaticCollisions)
            {
                // Mixed collisions
                mockSession.collisionsByObjectType["Car"] = totalCollisions / 3;
                mockSession.collisionsByObjectType["Tree"] = totalCollisions / 3;
                mockSession.collisionsByObjectType["Wall"] = totalCollisions - (2 * (totalCollisions / 3));
            }
        }
        
        return mockSession;
    }
    
    void LogEnhancementDetails(VisualEnhancementSettings settings, string scenario)
    {
        Debug.Log($"SCENARIO: {scenario}");
        Debug.Log($"DECISION: {settings.decisionReason}");
        Debug.Log($"VISION RATING: {settings.centralVisionRating}/10");
        Debug.Log($"HAD COLLISIONS: {settings.hadCollisions} (Total: {settings.totalCollisions})");
        Debug.Log($"");
        
        Debug.Log($"NAVIGATION LINE SETTINGS:");
        Debug.Log($"  - Enabled: {settings.enhanceNavigationLine}");
        Debug.Log($"  - Width: {settings.navigationLineWidth:F3}");
        Debug.Log($"  - Opacity: {settings.navigationLineOpacity:F0} (Alpha: {settings.navigationLineOpacity/255f:F2})");
        Debug.Log($"  - Spacing: {settings.navigationLineSpacing:F1}");
        Debug.Log($"");
        
        Debug.Log($"BOUNDING BOX SETTINGS:");
        Debug.Log($"  - Enabled: {settings.enableBoundingBoxes}");
        Debug.Log($"  - Width: {settings.boundingBoxLineWidth:F3}");
        Debug.Log($"  - Opacity: {settings.boundingBoxOpacity:F0} (Alpha: {settings.boundingBoxOpacity/255f:F2})");
        Debug.Log($"  - Spacing: {settings.boundingBoxSpacing:F1}");
        Debug.Log($"");
        
        Debug.Log($"OBJECT ENHANCEMENT:");
        Debug.Log($"  - Static Objects: {settings.enhanceStaticObjects}");
        Debug.Log($"  - Dynamic Objects: {settings.enhanceDynamicObjects}");
        Debug.Log($"");
        
        // Calculate enhancement percentages for easy comparison
        float navWidthPercent = (settings.navigationLineWidth / navigationLineWidthRange) * 100f;
        float navOpacityPercent = (settings.navigationLineOpacity / 255f) * 100f;
        float boxWidthPercent = (settings.boundingBoxLineWidth / boundingBoxWidthRange) * 100f;
        float boxOpacityPercent = (settings.boundingBoxOpacity / 255f) * 100f;
        
        Debug.Log($"ENHANCEMENT INTENSITY (as percentage of maximum):");
        Debug.Log($"  - Navigation Line Width: {navWidthPercent:F0}%");
        Debug.Log($"  - Navigation Line Opacity: {navOpacityPercent:F0}%");
        Debug.Log($"  - Bounding Box Width: {boxWidthPercent:F0}%");
        Debug.Log($"  - Bounding Box Opacity: {boxOpacityPercent:F0}%");
        Debug.Log($"================================================");
    }
}