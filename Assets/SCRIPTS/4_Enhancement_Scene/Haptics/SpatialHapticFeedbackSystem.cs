using UnityEngine;
using Bhaptics.SDK2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Spatial Haptic Feedback System for Navigation
/// Provides directional haptic feedback for up to 2 closest objects from different directions
/// Uses assessment scores to cap intensity ranges
/// UPDATED: Added manual control methods for LLM trials
/// </summary>
public class SpatialHapticController : MonoBehaviour
{
    [Header("Haptic Settings")]
    [Range(0.1f, 2.0f)]
    public float baseIntensity = 1.0f;
    
    [Range(0.5f, 3.0f)]
    public float baseDuration = 1.0f;
    
    [Header("Detection Settings")]
    [Tooltip("How often to check and provide haptic feedback (seconds)")]
    [Range(0.5f, 3f)]
    public float feedbackInterval = 1.5f;
    
    [Tooltip("Maximum range to detect objects (meters)")]
    [Range(5f, 50f)]
    public float detectionRange = 25f;
    
    [Tooltip("Enable haptic feedback")]
    public bool hapticEnabled = true;
    
    [Header("Multi-Object Settings")]
    [Tooltip("Number of objects to provide haptic feedback for")]
    [Range(1, 3)]
    public int maxHapticObjects = 2;
    
    [Tooltip("Minimum time between haptic feedback (seconds)")]
    [Range(0.1f, 2f)]
    public float minimumHapticGap = 0.5f;
    
    [Header("Distance-Based Intensity")]
    [Tooltip("Distance where intensity starts at minimum (meters)")]
    [Range(1f, 5f)]
    public float minIntensityDistance = 2.5f;
    
    [Tooltip("Distance where intensity reaches maximum (meters)")]
    [Range(0.1f, 2f)]
    public float maxIntensityDistance = 1.5f;
    
    [Header("Assessment-Based Intensity Caps")]
    [Tooltip("Vision assessment scores from algorithmic assessment")]
    [SerializeField] private int centralVisionScore = 5;
    [SerializeField] private int leftPeripheralScore = 5;
    [SerializeField] private int rightPeripheralScore = 5;
    [SerializeField] private bool assessmentDataLoaded = false;
    
    [Header("Manual Control Override")]
    [Tooltip("Use custom manual settings instead of assessment-based")]
    [SerializeField] private bool useCustomSettings = false;
    
    [Header("Direction Mapping")]
    [Tooltip("Angle threshold for front detection (degrees from forward)")]
    [Range(15f, 60f)]
    public float frontAngleThreshold = 45f;
    
    [Tooltip("Angle threshold for back detection (degrees from backward)")]
    [Range(15f, 60f)]
    public float backAngleThreshold = 45f;
    
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    public bool showDirectionDebugRays = false;
    
    [Header("Current Status")]
    [SerializeField] private List<DetectableObject> currentHapticObjects = new List<DetectableObject>();
    [SerializeField] private List<float> currentHapticDistances = new List<float>();
    [SerializeField] private List<string> currentDirections = new List<string>();
    [SerializeField] private string lastHapticEvent;
    [SerializeField] private int totalNearbyObjects = 0;
    [SerializeField] private bool systemActive = false;
    
    // Internal state
    private Transform playerTransform;
    private Camera playerCamera;
    private Coroutine feedbackCoroutine;
    private float lastHapticTime = 0f;
    
    // Haptic event mapping
    private Dictionary<DirectionType, string> hapticEventMap;
    
    // Manual control dictionaries
    private Dictionary<string, float> customMinIntensities = new Dictionary<string, float>();
    private Dictionary<string, float> customMaxIntensities = new Dictionary<string, float>();
    private Dictionary<string, float> customFeedbackIntervals = new Dictionary<string, float>();
    
    private enum DirectionType
    {
        Front,
        Left,
        Right,
        LeftBack,
        RightBack,
        CenterLeftBack,
        CenterRightBack
    }
    
    [System.Serializable]
    private class ObjectDistance
    {
        public DetectableObject detectableObject;
        public float distance;
        public Vector3 direction; // Local direction from player
        public DirectionType directionType;
    }
    
    [System.Serializable]
    private class IntensityRange
    {
        public float min;
        public float max;
    }

    void Start()
    {
        InitializeSystem();
        SetupHapticEventMapping();
        LoadAssessmentScores();
        
        // Only start haptics for algorithmic trials
        if (ShouldEnableHapticsForCurrentTrial())
        {
            StartContinuousFeedback();
            Debug.Log("Haptic feedback enabled for algorithmic trial");
        }
        else
        {
            hapticEnabled = false;
            Debug.Log($"Haptic feedback disabled - current trial '{GetCurrentTrial()}' does not use haptics");
        }
    }

    bool ShouldEnableHapticsForCurrentTrial()
    {
        if (SessionManager.Instance == null) return false;
        
        string currentTrial = SessionManager.Instance.GetCurrentTrial();
        return currentTrial == "short_algorithmic" || currentTrial == "long_algorithmic";
    }

    string GetCurrentTrial()
    {
        return SessionManager.Instance?.GetCurrentTrial() ?? "unknown";
    }

    void InitializeSystem()
    {
        // Find player transform and camera
        FCG.CharacterControl characterControl = FindObjectOfType<FCG.CharacterControl>();
        if (characterControl != null)
        {
            playerTransform = characterControl.transform;
            playerCamera = characterControl.GetComponentInChildren<Camera>();
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerCamera = player.GetComponentInChildren<Camera>();
            }
        }
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        if (playerTransform == null)
        {
            Debug.LogError("SpatialHapticController: Could not find player transform!");
            return;
        }
        
        systemActive = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Spatial Haptic Controller initialized");
            Debug.Log($"Player: {playerTransform.name}");
            Debug.Log($"Camera: {(playerCamera != null ? playerCamera.name : "None")}");
            Debug.Log($"Detection Range: {detectionRange}m");
            Debug.Log($"Feedback Interval: {feedbackInterval}s");
        }
    }
    
    void SetupHapticEventMapping()
    {
        hapticEventMap = new Dictionary<DirectionType, string>
        {
            { DirectionType.Front, "centre_100" },
            { DirectionType.Left, "left_100" },
            { DirectionType.Right, "right_100" },
            { DirectionType.LeftBack, "left_back_100" },
            { DirectionType.RightBack, "right_back_100" },
            { DirectionType.CenterLeftBack, "centre_leftback_100" },
            { DirectionType.CenterRightBack, "centre_rightback_100" }
        };
        
        if (enableDebugLogs)
        {
            Debug.Log("Haptic event mapping initialized:");
            foreach (var mapping in hapticEventMap)
            {
                Debug.Log($"  {mapping.Key} -> {mapping.Value}");
            }
        }
    }
    
    void LoadAssessmentScores()
    {
        // Try to load assessment scores from SessionManager
        if (SessionManager.Instance != null)
        {
            UserSession session = SessionManager.Instance.GetCurrentSession();
            if (session?.algorithmicResults != null && session.algorithmicResults.completed)
            {
                centralVisionScore = session.algorithmicResults.centralVisionRating;
                leftPeripheralScore = session.algorithmicResults.leftPeripheralRating;
                rightPeripheralScore = session.algorithmicResults.rightPeripheralRating;
                assessmentDataLoaded = true;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"Loaded vision scores - Central: {centralVisionScore}, Left: {leftPeripheralScore}, Right: {rightPeripheralScore}");
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("No algorithmic assessment data found - using default scores (5)");
                }
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("SessionManager not found - using default scores (5)");
            }
        }
    }
    
    IntensityRange GetIntensityRangeForDirection(DirectionType direction)
    {
        // If using custom settings, check for event-specific overrides
        if (useCustomSettings)
        {
            string eventName = hapticEventMap.ContainsKey(direction) ? hapticEventMap[direction] : "";
            
            if (!string.IsNullOrEmpty(eventName) && 
                customMinIntensities.ContainsKey(eventName) && 
                customMaxIntensities.ContainsKey(eventName))
            {
                return new IntensityRange 
                { 
                    min = customMinIntensities[eventName], 
                    max = customMaxIntensities[eventName] 
                };
            }
        }
        
        // Fall back to original assessment-based logic
        int visionScore = 5; // Default
        
        // Select appropriate vision score based on direction
        switch (direction)
        {
            case DirectionType.Front:
            case DirectionType.CenterLeftBack:
            case DirectionType.CenterRightBack:
                visionScore = centralVisionScore;
                break;
                
            case DirectionType.Left:
            case DirectionType.LeftBack:
                visionScore = leftPeripheralScore;
                break;
                
            case DirectionType.Right:
            case DirectionType.RightBack:
                visionScore = rightPeripheralScore;
                break;
        }
        
        // Return intensity range based on score
        if (visionScore >= 1 && visionScore <= 3)
        {
            return new IntensityRange { min = 0.7f, max = 1.0f }; // 70%-100%
        }
        else if (visionScore >= 4 && visionScore <= 6)
        {
            return new IntensityRange { min = 0.4f, max = 0.6f }; // 40%-60%
        }
        else // score 7-10
        {
            return new IntensityRange { min = 0.1f, max = 0.3f }; // 10%-30%
        }
    }
    
    void StartContinuousFeedback()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }
        
        feedbackCoroutine = StartCoroutine(ContinuousFeedbackLoop());
    }
    
    IEnumerator ContinuousFeedbackLoop()
    {
        while (systemActive && hapticEnabled)
        {
            if (playerTransform != null)
            {
                FindAndProvideHapticFeedback();
            }
            
            yield return new WaitForSeconds(feedbackInterval);
        }
    }
    
    void FindAndProvideHapticFeedback()
    {
        DetectableObject[] allObjects = FindObjectsOfType<DetectableObject>();
        List<ObjectDistance> nearbyObjects = new List<ObjectDistance>();
        
        foreach (DetectableObject obj in allObjects)
        {
            if (obj == null) continue;
            
            // Calculate distance to object edge
            float edgeDistance = GetDistanceToObjectEdge(playerTransform.position, obj);
            
            if (edgeDistance <= detectionRange)
            {
                Vector3 direction = GetDirectionToObject(obj.transform.position);
                DirectionType directionType = DetermineDirectionType(direction);
                
                nearbyObjects.Add(new ObjectDistance
                {
                    detectableObject = obj,
                    distance = edgeDistance,
                    direction = direction,
                    directionType = directionType
                });
            }
        }
        
        totalNearbyObjects = nearbyObjects.Count;
        
        if (nearbyObjects.Count == 0)
        {
            ClearCurrentHapticObjects();
            return;
        }
        
        // Sort by distance and select up to maxHapticObjects from different directions
        nearbyObjects.Sort((a, b) => a.distance.CompareTo(b.distance));
        List<ObjectDistance> selectedObjects = SelectObjectsFromDifferentDirections(nearbyObjects);
        
        // Update current tracking
        UpdateCurrentHapticObjects(selectedObjects);
        
        // Provide haptic feedback for selected objects
        if (CanProvideHapticNow())
        {
            foreach (ObjectDistance obj in selectedObjects)
            {
                ProvideHapticFeedback(obj);
            }
        }
        
        if (enableDebugLogs && selectedObjects.Count > 0)
        {
            string objectInfo = string.Join(", ", selectedObjects.Select(o => 
                $"{o.detectableObject.className} {o.distance:F1}m {o.directionType}"));
            Debug.Log($"Haptic objects: {objectInfo} ({totalNearbyObjects} total nearby)");
        }
    }
    
    List<ObjectDistance> SelectObjectsFromDifferentDirections(List<ObjectDistance> nearbyObjects)
    {
        List<ObjectDistance> selectedObjects = new List<ObjectDistance>();
        HashSet<DirectionType> usedDirections = new HashSet<DirectionType>();
        
        foreach (ObjectDistance obj in nearbyObjects)
        {
            // Skip if we already have an object from this direction
            if (usedDirections.Contains(obj.directionType))
                continue;
            
            selectedObjects.Add(obj);
            usedDirections.Add(obj.directionType);
            
            // Stop when we have enough objects
            if (selectedObjects.Count >= maxHapticObjects)
                break;
        }
        
        return selectedObjects;
    }
    
    void UpdateCurrentHapticObjects(List<ObjectDistance> selectedObjects)
    {
        currentHapticObjects.Clear();
        currentHapticDistances.Clear();
        currentDirections.Clear();
        
        foreach (ObjectDistance obj in selectedObjects)
        {
            currentHapticObjects.Add(obj.detectableObject);
            currentHapticDistances.Add(obj.distance);
            currentDirections.Add(obj.directionType.ToString());
        }
    }
    
    void ClearCurrentHapticObjects()
    {
        currentHapticObjects.Clear();
        currentHapticDistances.Clear();
        currentDirections.Clear();
    }
    
    Vector3 GetDirectionToObject(Vector3 objectPosition)
    {
        if (playerTransform == null) return Vector3.zero;
        
        Vector3 direction = (objectPosition - playerTransform.position).normalized;
        
        // Convert to local space relative to player's facing direction
        Vector3 localDirection;
        if (playerCamera != null)
        {
            localDirection = playerCamera.transform.InverseTransformDirection(direction);
        }
        else
        {
            localDirection = playerTransform.InverseTransformDirection(direction);
        }
        
        return localDirection;
    }
    
    DirectionType DetermineDirectionType(Vector3 localDirection)
    {
        // Convert to angles for easier classification
        float angle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        
        // Normalize angle to 0-360 range
        if (angle < 0) angle += 360f;
        
        // Classify based on angle ranges
        if ((angle >= 315f && angle <= 360f) || (angle >= 0f && angle <= frontAngleThreshold))
        {
            return DirectionType.Front;
        }
        else if (angle > frontAngleThreshold && angle <= 135f)
        {
            return DirectionType.Right;
        }
        else if (angle > 135f && angle <= 225f)
        {
            float backCenterAngle = 180f;
            float angleFromBack = Mathf.Abs(angle - backCenterAngle);
            
            if (angleFromBack <= backAngleThreshold * 0.5f)
            {
                if (angle < 180f)
                {
                    return DirectionType.CenterLeftBack;
                }
                else
                {
                    return DirectionType.CenterRightBack;
                }
            }
            else if (angle < 180f)
            {
                return DirectionType.LeftBack;
            }
            else
            {
                return DirectionType.RightBack;
            }
        }
        else
        {
            return DirectionType.Left;
        }
    }
    
    bool CanProvideHapticNow()
    {
        float intervalToUse = minimumHapticGap;
        
        // If using custom settings, we could check for the most recent event's custom interval
        // For now, we'll use the global minimumHapticGap
        
        return Time.time - lastHapticTime >= intervalToUse;
    }
    
    void ProvideHapticFeedback(ObjectDistance objectDistance)
    {
        DirectionType direction = objectDistance.directionType;
        float distance = objectDistance.distance;
        
        // Get haptic event name
        if (!hapticEventMap.ContainsKey(direction))
        {
            Debug.LogWarning($"No haptic event mapped for direction: {direction}");
            return;
        }
        
        string eventName = hapticEventMap[direction];
        
        // Get intensity range based on vision score for this direction or custom settings
        IntensityRange intensityRange = GetIntensityRangeForDirection(direction);
        
        // Calculate intensity based on distance within the capped range
        float intensityValue;
        if (distance <= maxIntensityDistance)
        {
            intensityValue = intensityRange.max;
        }
        else if (distance >= minIntensityDistance)
        {
            intensityValue = intensityRange.min;
        }
        else
        {
            float t = (distance - maxIntensityDistance) / (minIntensityDistance - maxIntensityDistance);
            intensityValue = Mathf.Lerp(intensityRange.max, intensityRange.min, t);
        }
        
        // Apply base intensity multiplier
        float finalIntensity = baseIntensity * intensityValue;
        
        // Play haptic event
        int requestId = BhapticsLibrary.Play(
            eventName,
            0,              // No delay
            finalIntensity, // Assessment and distance-adjusted intensity
            baseDuration,   // Standard duration
            0f,             // No rotation needed
            0f              // No vertical offset
        );
        
        lastHapticTime = Time.time;
        lastHapticEvent = eventName;
        
        if (enableDebugLogs)
        {
            if (requestId == -1)
            {
                Debug.LogWarning($"Failed to play haptic event: {eventName}");
            }
            else
            {
                string settingType = useCustomSettings ? "Custom" : "Assessment";
                Debug.Log($"Haptic: {objectDistance.detectableObject.className} -> {eventName} " +
                         $"({settingType}: {intensityRange.min:F1}-{intensityRange.max:F1}, " +
                         $"final intensity: {finalIntensity:F2}, distance: {distance:F1}m)");
            }
        }
        
        // Show debug ray for direction
        if (showDirectionDebugRays && playerTransform != null)
        {
            Vector3 worldDirection = playerTransform.TransformDirection(objectDistance.direction);
            Debug.DrawRay(playerTransform.position, worldDirection * distance, GetDebugColorForDirection(direction), 1f);
        }
    }
    
    int GetVisionScoreForDirection(DirectionType direction)
    {
        switch (direction)
        {
            case DirectionType.Front:
            case DirectionType.CenterLeftBack:
            case DirectionType.CenterRightBack:
                return centralVisionScore;
                
            case DirectionType.Left:
            case DirectionType.LeftBack:
                return leftPeripheralScore;
                
            case DirectionType.Right:
            case DirectionType.RightBack:
                return rightPeripheralScore;
                
            default:
                return 5; // Default fallback
        }
    }
    
    Color GetDebugColorForDirection(DirectionType direction)
    {
        switch (direction)
        {
            case DirectionType.Front: return Color.green;
            case DirectionType.Left: return Color.blue;
            case DirectionType.Right: return Color.red;
            case DirectionType.LeftBack: return Color.cyan;
            case DirectionType.RightBack: return Color.magenta;
            case DirectionType.CenterLeftBack: return Color.yellow;
            case DirectionType.CenterRightBack: return Color.white;
            default: return Color.gray;
        }
    }
    
    float GetDistanceToObjectEdge(Vector3 playerPosition, DetectableObject obj)
    {
        Bounds bounds = obj.worldBounds;
        
        if (bounds.size == Vector3.zero)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                bounds = renderer.bounds;
            }
            else
            {
                Collider collider = obj.GetComponent<Collider>();
                if (collider != null)
                {
                    bounds = collider.bounds;
                }
                else
                {
                    return Vector3.Distance(playerPosition, obj.transform.position);
                }
            }
        }
        
        Vector3 closestPoint = bounds.ClosestPoint(playerPosition);
        float distance = Vector3.Distance(playerPosition, closestPoint);
        
        if (distance < 0.1f)
        {
            distance = 0.1f;
        }
        
        return distance;
    }
    
    // MANUAL CONTROL METHODS
    
    /// <summary>
    /// Set custom intensity range for a specific haptic event
    /// </summary>
    public void SetEventIntensityRange(string eventName, float minIntensity, float maxIntensity)
    {
        customMinIntensities[eventName] = Mathf.Clamp01(minIntensity);
        customMaxIntensities[eventName] = Mathf.Clamp01(maxIntensity);
        useCustomSettings = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Set custom intensity for {eventName}: {minIntensity:F2} - {maxIntensity:F2}");
        }
    }
    
    /// <summary>
    /// Set custom feedback interval for a specific haptic event
    /// </summary>
    public void SetEventFeedbackInterval(string eventName, float interval)
    {
        customFeedbackIntervals[eventName] = Mathf.Clamp(interval, 0.5f, 5f);
        useCustomSettings = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Set custom interval for {eventName}: {interval:F1}s");
        }
    }
    
    /// <summary>
    /// Enable custom manual settings mode
    /// </summary>
    public void EnableCustomSettings()
    {
        useCustomSettings = true;
        Debug.Log("SpatialHapticController: Custom settings enabled");
    }
    
    /// <summary>
    /// Disable custom settings and revert to assessment-based
    /// </summary>
    public void DisableCustomSettings()
    {
        useCustomSettings = false;
        customMinIntensities.Clear();
        customMaxIntensities.Clear();
        customFeedbackIntervals.Clear();
        Debug.Log("SpatialHapticController: Custom settings disabled - reverted to assessment-based");
    }
    
    /// <summary>
    /// Clear all custom settings for a fresh start
    /// </summary>
    public void ClearCustomSettings()
    {
        customMinIntensities.Clear();
        customMaxIntensities.Clear();
        customFeedbackIntervals.Clear();
        Debug.Log("SpatialHapticController: All custom settings cleared");
    }
    
    // PUBLIC CONTROL METHODS
    
    public void EnableHaptic()
    {
        hapticEnabled = true;
        StartContinuousFeedback();
        
        if (enableDebugLogs)
        {
            Debug.Log("Spatial haptic feedback enabled");
        }
    }
        
    public void DisableHaptic()
    {
        hapticEnabled = false;
        
        // Stop any playing haptics
        BhapticsLibrary.StopAll();
        
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log("Spatial haptic feedback disabled");
        }
    }
    
    public void SetBaseIntensity(float intensity)
    {
        baseIntensity = Mathf.Clamp(intensity, 0.1f, 2.0f);
        
        if (enableDebugLogs)
        {
            Debug.Log($"Base intensity set to: {baseIntensity:F2}");
        }
    }
    
    public void SetDetectionRange(float range)
    {
        detectionRange = Mathf.Clamp(range, 5f, 100f);
        
        if (enableDebugLogs)
        {
            Debug.Log($"Detection range set to: {detectionRange}m");
        }
    }
    
    /// <summary>
    /// Set the maximum number of haptic objects to provide feedback for
    /// </summary>
    public void SetMaxHapticObjects(int maxObjects)
    {
        maxHapticObjects = Mathf.Clamp(maxObjects, 1, 3);
        
        if (enableDebugLogs)
        {
            Debug.Log($"Max haptic objects set to: {maxHapticObjects}");
        }
    }
    
    public void SetFeedbackInterval(float interval)
    {
        feedbackInterval = Mathf.Clamp(interval, 0.5f, 5f);
        
        if (enableDebugLogs)
        {
            Debug.Log($"Feedback interval set to: {feedbackInterval}s");
        }
        
        // Restart feedback loop with new interval
        if (hapticEnabled)
        {
            StartContinuousFeedback();
        }
    }
    
    // STATUS METHODS
    
    public bool IsHapticEnabled() => hapticEnabled;
    public bool IsUsingCustomSettings() => useCustomSettings;
    public List<DetectableObject> GetCurrentHapticObjects() => new List<DetectableObject>(currentHapticObjects);
    public List<float> GetCurrentHapticDistances() => new List<float>(currentHapticDistances);
    public List<string> GetCurrentDirections() => new List<string>(currentDirections);
    public int GetTotalNearbyObjects() => totalNearbyObjects;
    public string GetLastHapticEvent() => lastHapticEvent;
    
    // CONTEXT MENU TESTING METHODS
    
    [ContextMenu("Test: Play Front Haptic")]
    public void TestPlayFrontHaptic()
    {
        TestPlayDirectionalHaptic(DirectionType.Front);
    }
    
    [ContextMenu("Test: Play Left Haptic")]
    public void TestPlayLeftHaptic()
    {
        TestPlayDirectionalHaptic(DirectionType.Left);
    }
    
    [ContextMenu("Test: Play Right Haptic")]
    public void TestPlayRightHaptic()
    {
        TestPlayDirectionalHaptic(DirectionType.Right);
    }
    
    [ContextMenu("Test: Play All Directions")]
    public void TestPlayAllDirections()
    {
        StartCoroutine(TestAllDirectionsSequence());
    }
    
    void TestPlayDirectionalHaptic(DirectionType direction)
    {
        if (hapticEventMap.ContainsKey(direction))
        {
            string eventName = hapticEventMap[direction];
            
            int requestId = BhapticsLibrary.Play(
                eventName,
                0,
                baseIntensity,
                baseDuration,
                0f,
                0f
            );
            
            lastHapticEvent = eventName;
            
            Debug.Log($"Test haptic: {direction} -> {eventName} (intensity: {baseIntensity:F2})");
            
            if (requestId == -1)
            {
                Debug.LogWarning($"Failed to play test haptic: {eventName}");
            }
        }
        else
        {
            Debug.LogWarning($"No haptic event mapped for direction: {direction}");
        }
    }
    
    IEnumerator TestAllDirectionsSequence()
    {
        Debug.Log("Testing all haptic directions in sequence...");
        
        DirectionType[] directions = {
            DirectionType.Front,
            DirectionType.Right,
            DirectionType.RightBack,
            DirectionType.CenterRightBack,
            DirectionType.CenterLeftBack,
            DirectionType.LeftBack,
            DirectionType.Left
        };
        
        foreach (DirectionType direction in directions)
        {
            TestPlayDirectionalHaptic(direction);
            yield return new WaitForSeconds(1.5f);
        }
        
        Debug.Log("All direction test sequence completed");
    }
    
    [ContextMenu("Debug: Show Haptic Status")]
    public void DebugShowHapticStatus()
    {
        Debug.Log("=== SPATIAL HAPTIC CONTROLLER STATUS ===");
        Debug.Log($"System Active: {systemActive}");
        Debug.Log($"Haptic Enabled: {hapticEnabled}");
        Debug.Log($"Using Custom Settings: {useCustomSettings}");
        Debug.Log($"Current Trial: {GetCurrentTrial()}");
        Debug.Log($"Should Enable for Trial: {ShouldEnableHapticsForCurrentTrial()}");
        Debug.Log($"Base Intensity: {baseIntensity:F2}");
        Debug.Log($"Detection Range: {detectionRange}m");
        Debug.Log($"Assessment Data Loaded: {assessmentDataLoaded}");
        Debug.Log($"Vision Scores - Central: {centralVisionScore}, Left: {leftPeripheralScore}, Right: {rightPeripheralScore}");
        Debug.Log($"Player Found: {playerTransform != null}");
        Debug.Log($"Camera Found: {playerCamera != null}");
        Debug.Log($"Last Haptic Event: {lastHapticEvent}");
        Debug.Log($"Current Haptic Objects: {currentHapticObjects.Count}");
        
        for (int i = 0; i < currentHapticObjects.Count; i++)
        {
            Debug.Log($"  Object {i + 1}: {currentHapticObjects[i].className} at {currentHapticDistances[i]:F1}m ({currentDirections[i]})");
        }
        
        Debug.Log($"Total Nearby Objects: {totalNearbyObjects}");
        
        if (useCustomSettings)
        {
            Debug.Log($"Custom Intensity Settings: {customMinIntensities.Count}");
            foreach (var kvp in customMinIntensities)
            {
                if (customMaxIntensities.ContainsKey(kvp.Key))
                {
                    Debug.Log($"  {kvp.Key}: {kvp.Value:F2} - {customMaxIntensities[kvp.Key]:F2}");
                }
            }
        }
    }
    
    [ContextMenu("Test: Stop All Haptics")]
    public void TestStopAllHaptics()
    {
        BhapticsLibrary.StopAll();
        Debug.Log("Stopped all haptic feedback");
    }
    
    [ContextMenu("Test: Enable Custom Settings")]
    public void TestEnableCustomSettings()
    {
        EnableCustomSettings();
        
        // Set some test custom values
        SetEventIntensityRange("centre_100", 0.5f, 0.8f);
        SetEventIntensityRange("left_100", 0.3f, 0.6f);
        SetEventIntensityRange("right_100", 0.3f, 0.6f);
        
        Debug.Log("Test: Enabled custom settings with sample values");
    }
    
    [ContextMenu("Test: Disable Custom Settings")]
    public void TestDisableCustomSettings()
    {
        DisableCustomSettings();
        Debug.Log("Test: Disabled custom settings - reverted to assessment-based");
    }
    
    void Update()
    {
        // Quick controls for testing
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (hapticEnabled)
                DisableHaptic();
            else
                EnableHaptic();
        }
        
        // Manual test trigger
        if (Input.GetKeyDown(KeyCode.T))
        {
            FindAndProvideHapticFeedback();
        }
    }
    
    void OnDestroy()
    {
        systemActive = false;
        
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }
        
        // Stop any playing haptics
        BhapticsLibrary.StopAll();
    }
    
    void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, detectionRange);
            
            // Current haptic objects
            if (currentHapticObjects.Count > 0)
            {
                for (int i = 0; i < currentHapticObjects.Count; i++)
                {
                    if (currentHapticObjects[i] != null)
                    {
                        Gizmos.color = i == 0 ? Color.white : Color.yellow;
                        Gizmos.DrawLine(playerTransform.position, currentHapticObjects[i].transform.position);
                        Gizmos.DrawWireSphere(currentHapticObjects[i].transform.position, 1f);
                    }
                }
            }
        }
    }
}