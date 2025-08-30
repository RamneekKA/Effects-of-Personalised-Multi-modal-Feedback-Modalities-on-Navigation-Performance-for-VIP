using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Updated RouteGuideSystem with integrated route configuration management
/// Now handles short vs long distance routes internally based on SessionManager
/// REPLACES: RouteConfigurationManager (removed)
/// </summary>
public class RouteGuideSystem : MonoBehaviour
{
    [Header("Route Definition")]
    [Tooltip("Define waypoints for SHORT distance route (15-25m)")]
    public Transform[] shortRouteWaypoints;
    
    [Tooltip("Define waypoints for LONG distance route (50-75m)")]
    public Transform[] longRouteWaypoints;
    
    [Tooltip("Automatically create route between start and end (simple straight line)")]
    public bool useSimpleRoute = false;
    public Transform startPoint;
    public Transform endPoint;
    
    [Header("SessionManager Integration")]
    [Tooltip("Use SessionManager to determine which route to show")]
    public bool useSessionManager = true;
    
    [Header("Visual Settings")]
    [Tooltip("Width/size of the route markers")]
    public float lineWidth = 0.5f;
    
    [Tooltip("Color of the route")]
    public Color routeColor = Color.cyan;
    
    [Header("Opacity Control")]
    [Tooltip("Opacity of the navigation line (0 = fully transparent, 1 = fully opaque)")]
    [Range(0f, 1f)]
    public float routeOpacity = 1f;
    
    [Tooltip("Height offset above ground for markers")]
    public float lineHeightOffset = 0.2f;
    
    [Tooltip("Spacing between route markers")]
    public float markerSpacing = 1f;
    
    [Header("Marker Appearance")]
    [Tooltip("Use rectangular markers for more path-like appearance")]
    public bool useRectangularMarkers = true;
    public Vector3 rectangularScale = new Vector3(0.4f, 0.15f, 1.2f); // width, height, length
    
    [Tooltip("Enable pulsing effect for enhanced visibility")]
    public bool enablePulsing = false;
    [Range(0.5f, 3f)]
    public float pulseSpeed = 1f;
    
    [Header("Progress Tracking")]
    [Tooltip("Distance to waypoint to consider it 'reached'")]
    public float waypointReachDistance = 3f;
    
    [Tooltip("Player transform to track progress")]
    public Transform playerTransform;
    
    [Header("Audio Feedback")]
    [Tooltip("Play sound when reaching waypoints")]
    public AudioClip waypointReachedSound;
    
    [Tooltip("Play sound when completing route")]
    public AudioClip routeCompletedSound;
    
    [Header("Debug Information")]
    [SerializeField] private string currentRouteType = "none";
    [SerializeField] private Transform[] activeRouteWaypoints;
    [SerializeField] private float currentRouteDistance = 0f;
    
    // Internal components - maintaining compatibility with old system
    private List<Vector3> routePoints = new List<Vector3>();
    private List<GameObject> routeMarkers = new List<GameObject>();
    private List<WaypointData> waypoints = new List<WaypointData>();
    private Material routeMaterial;
    private AudioSource audioSource;
    
    // Progress tracking
    private int currentWaypointIndex = 0;
    private bool routeCompleted = false;
    private float totalRouteDistance = 0f;
    
    // Opacity tracking
    private float lastRouteOpacity = 1f;
    
    // Events
    public System.Action<int> OnWaypointReached;
    public System.Action OnRouteCompleted;
    public static System.Action<string> OnRouteConfigured;
    
    [System.Serializable]
    private class WaypointData
    {
        public Vector3 position;
        public bool reached = false;
        public GameObject markerObject;
        public float distanceFromStart;
        
        public WaypointData(Vector3 pos, float distance)
        {
            position = pos;
            distanceFromStart = distance;
        }
    }
    
    void Start()
    {
        if (playerTransform == null)
        {
            FCG.CharacterControl characterControl = FindObjectOfType<FCG.CharacterControl>();
            if (characterControl != null)
                playerTransform = characterControl.transform;
        }
        
        audioSource = gameObject.AddComponent<AudioSource>();
        
        // Initialize opacity tracking
        lastRouteOpacity = routeOpacity;
        
        // Configure route based on SessionManager
        if (useSessionManager && SessionManager.Instance != null)
        {
            string currentTrial = SessionManager.Instance.GetCurrentTrial();
            ConfigureRouteForTrial(currentTrial);
            
            // Subscribe to trial changes
            SessionManager.OnTrialChanged += OnTrialChanged;
        }
        else
        {
            // Fallback to short route or simple route
            if (shortRouteWaypoints != null && shortRouteWaypoints.Length > 1)
            {
                ConfigureRoute("short");
            }
            else
            {
                ConfigureRoute("simple");
            }
        }
        
        SetupRoute();
        CreateRouteVisualization();
        
        Debug.Log($"Route Guide System initialized: {currentRouteType} route with {waypoints.Count} waypoints");
    }
    
    void OnTrialChanged(string newTrial)
    {
        Debug.Log($"Trial changed to: {newTrial} - reconfiguring route");
        ConfigureRouteForTrial(newTrial);
        RefreshRouteVisualization();
    }
    
    void ConfigureRouteForTrial(string trialType)
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogWarning("SessionManager not available, using default route");
            ConfigureRoute("short");
            return;
        }
        
        string routeType = SessionManager.Instance.GetRouteType(trialType);
        ConfigureRoute(routeType);
    }
    
    void ConfigureRoute(string routeType)
    {
        currentRouteType = routeType;
        
        switch (routeType)
        {
            case "short":
                if (shortRouteWaypoints != null && shortRouteWaypoints.Length > 1)
                {
                    activeRouteWaypoints = shortRouteWaypoints;
                    Debug.Log($"Configured SHORT route: {shortRouteWaypoints.Length} waypoints");
                }
                else
                {
                    Debug.LogError("Short route waypoints not configured!");
                    useSimpleRoute = true;
                }
                break;
                
            case "long":
                if (longRouteWaypoints != null && longRouteWaypoints.Length > 1)
                {
                    activeRouteWaypoints = longRouteWaypoints;
                    Debug.Log($"Configured LONG route: {longRouteWaypoints.Length} waypoints");
                }
                else
                {
                    Debug.LogError("Long route waypoints not configured!");
                    useSimpleRoute = true;
                }
                break;
                
            case "none":
                // Assessment trial - disable route visualization
                activeRouteWaypoints = new Transform[0];
                SetRouteVisibility(false);
                Debug.Log("Route disabled for assessment trial");
                return;
                
            case "simple":
            default:
                useSimpleRoute = true;
                Debug.Log("Using simple route fallback");
                break;
        }
        
        OnRouteConfigured?.Invoke(routeType);
    }
    
    void RefreshRouteVisualization()
    {
        // Clear existing route
        ClearRouteVisualization();
        
        // Reset progress tracking
        currentWaypointIndex = 0;
        routeCompleted = false;
        
        // Rebuild route with new configuration
        SetupRoute();
        CreateRouteVisualization();
        
        Debug.Log($"Route refreshed: {currentRouteType} - {waypoints.Count} waypoints");
    }
    
    void Update()
    {
        if (playerTransform != null && !routeCompleted && activeRouteWaypoints != null && activeRouteWaypoints.Length > 0)
        {
            UpdateProgress();
        }
        
        // Check for opacity changes in inspector
        CheckOpacityChange();
    }
    
    /// <summary>
    /// Check if opacity has changed in the inspector and update accordingly
    /// </summary>
    void CheckOpacityChange()
    {
        if (Mathf.Abs(routeOpacity - lastRouteOpacity) > 0.01f)
        {
            UpdateRouteOpacity();
            lastRouteOpacity = routeOpacity;
        }
    }
    
    /// <summary>
    /// Update the opacity of all route markers
    /// </summary>
    void UpdateRouteOpacity()
    {
        Color newColor = new Color(routeColor.r, routeColor.g, routeColor.b, routeOpacity);
        SetRouteColor(newColor);
        
        Debug.Log($"Route opacity updated to: {routeOpacity:F2} (Alpha: {newColor.a:F2})");
    }
    
    void SetupRoute()
    {
        routePoints.Clear();
        waypoints.Clear();
        
        if (useSimpleRoute && startPoint != null && endPoint != null)
        {
            routePoints.Add(startPoint.position);
            routePoints.Add(endPoint.position);
        }
        else if (activeRouteWaypoints != null && activeRouteWaypoints.Length > 0)
        {
            foreach (Transform waypoint in activeRouteWaypoints)
            {
                if (waypoint != null)
                {
                    routePoints.Add(waypoint.position);
                }
            }
        }
        else
        {
            Debug.LogError("No route configured! Set waypoints or use simple route with start/end points.");
            return;
        }
        
        // Calculate total route distance and create waypoint data
        totalRouteDistance = 0f;
        float cumulativeDistance = 0f;
        
        waypoints.Add(new WaypointData(routePoints[0], 0f));
        
        for (int i = 1; i < routePoints.Count; i++)
        {
            float segmentDistance = Vector3.Distance(routePoints[i - 1], routePoints[i]);
            cumulativeDistance += segmentDistance;
            totalRouteDistance += segmentDistance;
            
            waypoints.Add(new WaypointData(routePoints[i], cumulativeDistance));
        }
        
        currentRouteDistance = totalRouteDistance;
        
        Debug.Log($"Route setup complete: {currentRouteType} route - {totalRouteDistance:F1}m with {waypoints.Count} waypoints");
        
        if (currentRouteType == "short" && (totalRouteDistance < 10f || totalRouteDistance > 30f))
        {
            Debug.LogWarning($"Short route distance ({totalRouteDistance:F1}m) outside recommended range (10-30m)");
        }
        else if (currentRouteType == "long" && (totalRouteDistance < 40f || totalRouteDistance > 100f))
        {
            Debug.LogWarning($"Long route distance ({totalRouteDistance:F1}m) outside recommended range (40-100m)");
        }
    }
    
    void CreateRouteVisualization()
    {
        if (routePoints.Count == 0 || currentRouteType == "none")
        {
            return;
        }
        
        ClearRouteVisualization();
        CreateRouteMaterial();
        
        // Generate path markers between waypoints
        List<Vector3> pathMarkers = GeneratePathMarkers();
        
        foreach (Vector3 markerPosition in pathMarkers)
        {
            CreateRouteMarker(markerPosition);
        }
        
        // Create special waypoint markers
        CreateWaypointMarkers();
        
        Debug.Log($"Created route visualization with {routeMarkers.Count} markers");
    }
    
    List<Vector3> GeneratePathMarkers()
    {
        List<Vector3> markers = new List<Vector3>();
        
        if (routePoints.Count < 2) return markers;
        
        for (int i = 0; i < routePoints.Count - 1; i++)
        {
            Vector3 start = routePoints[i];
            Vector3 end = routePoints[i + 1];
            float segmentLength = Vector3.Distance(start, end);
            
            int numMarkers = Mathf.Max(1, Mathf.RoundToInt(segmentLength / markerSpacing));
            
            for (int j = 0; j < numMarkers; j++)
            {
                float t = (float)j / numMarkers;
                Vector3 markerPos = Vector3.Lerp(start, end, t);
                markerPos.y += lineHeightOffset;
                markers.Add(markerPos);
            }
        }
        
        return markers;
    }
    
    void CreateRouteMarker(Vector3 position)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.transform.position = position;
        marker.transform.SetParent(transform);
        
        // Set scale based on lineWidth
        if (useRectangularMarkers)
        {
            marker.transform.localScale = rectangularScale * lineWidth;
        }
        else
        {
            marker.transform.localScale = Vector3.one * lineWidth;
        }
        
        // Orient along path direction if rectangular
        if (useRectangularMarkers)
        {
            Vector3 direction = GetDirectionAtPosition(position);
            if (direction != Vector3.zero)
            {
                marker.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }
        
        // Setup material with transparency support
        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (routeMaterial == null)
            {
                CreateRouteMaterial();
            }
            
            renderer.material = routeMaterial;
            
            // Apply current opacity to the color
            Color colorWithOpacity = new Color(routeColor.r, routeColor.g, routeColor.b, routeOpacity);
            renderer.material.color = colorWithOpacity;
        }
        
        // Remove collider
        Destroy(marker.GetComponent<Collider>());
        
        // Add pulsing effect if enabled
        if (enablePulsing)
        {
            PulsingMarker pulser = marker.AddComponent<PulsingMarker>();
            pulser.pulseSpeed = pulseSpeed;
            pulser.originalScale = marker.transform.localScale;
        }
        
        routeMarkers.Add(marker);
    }
    
    void CreateWaypointMarkers()
    {
        // Skip creating visual waypoint markers - only track waypoint data internally
        for (int i = 0; i < waypoints.Count; i++)
        {
            waypoints[i].markerObject = null; // No visual marker
        }
    }
    
    Vector3 GetDirectionAtPosition(Vector3 position)
    {
        if (routePoints.Count < 2) return Vector3.forward;
        
        float closestDistance = float.MaxValue;
        Vector3 pathDirection = Vector3.forward;
        
        // Check each route segment to find which one this position belongs to
        for (int i = 0; i < routePoints.Count - 1; i++)
        {
            Vector3 segmentStart = routePoints[i];
            Vector3 segmentEnd = routePoints[i + 1];
            
            // Find closest point on this line segment
            Vector3 segmentVector = segmentEnd - segmentStart;
            Vector3 positionVector = position - segmentStart;
            
            float t = Vector3.Dot(positionVector, segmentVector) / segmentVector.sqrMagnitude;
            t = Mathf.Clamp01(t); // Clamp to segment bounds
            
            Vector3 closestPointOnSegment = segmentStart + t * segmentVector;
            float distanceToSegment = Vector3.Distance(position, closestPointOnSegment);
            
            if (distanceToSegment < closestDistance)
            {
                closestDistance = distanceToSegment;
                pathDirection = segmentVector.normalized; // Use the actual segment direction
            }
        }
        
        return pathDirection;
    }
    
    /// <summary>
    /// Creates a material that supports transparency using the most compatible shader
    /// FIXED: Prevents pink materials by using reliable fallback shaders
    /// </summary>
    Material CreateSafeTransparentMaterial(Color initialColor)
    {
        Material material = null;
        
        // Method 1: Try Sprites/Default (most reliable for transparency)
        try
        {
            Shader spritesShader = Shader.Find("Sprites/Default");
            if (spritesShader != null)
            {
                material = new Material(spritesShader);
                material.color = initialColor;
                Debug.Log("Created material with Sprites/Default shader");
                return material;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to create Sprites/Default material: {e.Message}");
        }
        
        // Method 2: Try UI/Default (also supports transparency)
        try
        {
            Shader uiShader = Shader.Find("UI/Default");
            if (uiShader != null)
            {
                material = new Material(uiShader);
                material.color = initialColor;
                Debug.Log("Created material with UI/Default shader");
                return material;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to create UI/Default material: {e.Message}");
        }
        
        // Method 3: Create Standard material and manually set it to transparent
        try
        {
            material = new Material(Shader.Find("Standard"));
            
            // Force Standard shader into transparent mode
            material.SetFloat("_Mode", 3f); // Transparent
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000; // Transparent render queue
            
            material.color = initialColor;
            
            Debug.Log("Created transparent Standard material");
            return material;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to create Standard transparent material: {e.Message}");
        }
        
        // Method 4: Last resort - use default material but warn about no transparency
        try
        {
            material = new Material(Shader.Find("Standard"));
            material.color = new Color(initialColor.r, initialColor.g, initialColor.b, 1f); // Force full alpha
            Debug.LogWarning("Using opaque Standard material - transparency will not work");
            return material;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create any material: {e.Message}");
        }
        
        return null;
    }
    
    void CreateRouteMaterial()
    {
        Color initialColor = new Color(routeColor.r, routeColor.g, routeColor.b, routeOpacity);
        routeMaterial = CreateSafeTransparentMaterial(initialColor);
        
        if (routeMaterial == null)
        {
            Debug.LogError("Failed to create route material!");
            // Create a basic fallback
            routeMaterial = new Material(Shader.Find("Standard"));
            routeMaterial.color = initialColor;
        }
        
        Debug.Log($"Route material created. Color: {routeMaterial.color} (Alpha: {routeMaterial.color.a})");
    }
    
    void ClearRouteVisualization()
    {
        foreach (GameObject marker in routeMarkers)
        {
            if (marker != null)
            {
                DestroyImmediate(marker);
            }
        }
        routeMarkers.Clear();
        
        // Clear waypoint marker references
        foreach (WaypointData waypoint in waypoints)
        {
            waypoint.markerObject = null;
        }
    }
    
    void UpdateProgress()
    {
        if (currentWaypointIndex >= waypoints.Count) return;
        
        WaypointData currentWaypoint = waypoints[currentWaypointIndex];
        float distanceToWaypoint = Vector3.Distance(playerTransform.position, currentWaypoint.position);
        
        // Check if waypoint reached
        if (distanceToWaypoint <= waypointReachDistance)
        {
            currentWaypoint.reached = true;
            
            OnWaypointReached?.Invoke(currentWaypointIndex);
            
            if (waypointReachedSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(waypointReachedSound);
            }
            
            Debug.Log($"Waypoint {currentWaypointIndex} reached!");
            
            currentWaypointIndex++;
            
            if (currentWaypointIndex >= waypoints.Count)
            {
                CompleteRoute();
            }
        }
    }
    
    void CompleteRoute()
    {
        routeCompleted = true;
        OnRouteCompleted?.Invoke();
        
        if (routeCompletedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(routeCompletedSound);
        }
        
        Debug.Log("Route completed successfully!");
    }
    
    // PUBLIC API METHODS - Full compatibility with existing VisualEnhancementManager
    
    public void SetRouteColor(Color newColor)
    {
        routeColor = new Color(newColor.r, newColor.g, newColor.b, 1f); // Store base color without alpha
        
        // Apply current opacity to the new color
        Color colorWithOpacity = new Color(newColor.r, newColor.g, newColor.b, newColor.a);
        
        if (routeMaterial != null)
        {
            routeMaterial.color = colorWithOpacity;
        }
        
        // Update all existing markers to use the new color
        foreach (GameObject marker in routeMarkers)
        {
            if (marker != null)
            {
                Renderer renderer = marker.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = colorWithOpacity;
                }
            }
        }
        
        Debug.Log($"Route color updated to: {colorWithOpacity} (Alpha: {colorWithOpacity.a})");
    }
    
    /// <summary>
    /// Set the route opacity directly (0-1 range)
    /// </summary>
    public void SetRouteOpacity(float opacity)
    {
        routeOpacity = Mathf.Clamp01(opacity);
        UpdateRouteOpacity();
    }
    
    /// <summary>
    /// Set the route opacity using 0-255 range (for compatibility with VisualEnhancementManager)
    /// </summary>
    public void SetRouteOpacity255(float opacity255)
    {
        routeOpacity = Mathf.Clamp01(opacity255 / 255f);
        UpdateRouteOpacity();
    }
    
    public void SetLineWidth(float width)
    {
        SetRouteWidth(width);
    }
    
    public void SetRouteWidth(float width)
    {
        lineWidth = width;
        
        // Update existing route markers
        foreach (GameObject marker in routeMarkers)
        {
            if (marker != null)
            {
                if (useRectangularMarkers)
                {
                    marker.transform.localScale = rectangularScale * width;
                }
                else
                {
                    marker.transform.localScale = Vector3.one * width;
                }
            }
        }
        
        Debug.Log($"Route width updated to: {width:F2}");
    }
    
    public void SetRouteVisibility(bool visible)
    {
        foreach (GameObject marker in routeMarkers)
        {
            if (marker != null)
            {
                marker.SetActive(visible);
            }
        }
        
        Debug.Log($"Route visibility set to: {visible}");
    }
    
    // Material access for VisualEnhancementManager
    public Material GetRouteMaterial()
    {
        return routeMaterial;
    }
    
    // ROUTE ANALYSIS METHODS - Full compatibility with existing systems
    
    public List<Vector3> GetRoutePoints()
    {
        return new List<Vector3>(routePoints);
    }
    
    public float GetSignedDeviationFromRoute(Vector3 playerPosition)
    {
        if (routePoints.Count < 2)
            return 0f;
        
        // Find closest point on the route and the route direction at that point
        Vector3 closestPoint;
        Vector3 routeDirection;
        GetClosestPointOnRoute(playerPosition, out closestPoint, out routeDirection);
        
        // Calculate player's offset from the route
        Vector3 playerOffset = playerPosition - closestPoint;
        
        // Use cross product to determine left/right (ignoring Y component for 2D analysis)
        Vector3 routeDirectionFlat = new Vector3(routeDirection.x, 0, routeDirection.z).normalized;
        Vector3 playerOffsetFlat = new Vector3(playerOffset.x, 0, playerOffset.z);
        
        // Cross product in 2D: positive = right, negative = left
        float crossProduct = routeDirectionFlat.x * playerOffsetFlat.z - routeDirectionFlat.z * playerOffsetFlat.x;
        
        // The magnitude is the distance, the sign indicates left/right
        float distance = playerOffsetFlat.magnitude;
        
        return crossProduct >= 0 ? -distance : distance;
    }
    
    void GetClosestPointOnRoute(Vector3 playerPosition, out Vector3 closestPoint, out Vector3 routeDirection)
    {
        closestPoint = Vector3.zero;
        routeDirection = Vector3.forward;
        
        float closestDistance = float.MaxValue;
        int closestSegmentIndex = 0;
        
        // Check each segment of the route
        for (int i = 0; i < routePoints.Count - 1; i++)
        {
            Vector3 segmentStart = routePoints[i];
            Vector3 segmentEnd = routePoints[i + 1];
            
            // Find closest point on this segment
            Vector3 segmentClosestPoint;
            float t;
            GetClosestPointOnSegment(playerPosition, segmentStart, segmentEnd, out segmentClosestPoint, out t);
            
            float distance = Vector3.Distance(playerPosition, segmentClosestPoint);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = segmentClosestPoint;
                closestSegmentIndex = i;
            }
        }
        
        // Calculate route direction at the closest point
        Vector3 segmentDirection = (routePoints[closestSegmentIndex + 1] - routePoints[closestSegmentIndex]).normalized;
        routeDirection = segmentDirection;
    }
    
    void GetClosestPointOnSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd, out Vector3 closestPoint, out float t)
    {
        Vector3 segmentVector = segmentEnd - segmentStart;
        Vector3 pointVector = point - segmentStart;
        
        float segmentLength = segmentVector.magnitude;
        if (segmentLength == 0f)
        {
            closestPoint = segmentStart;
            t = 0f;
            return;
        }
        
        // Project point onto segment
        t = Vector3.Dot(pointVector, segmentVector) / (segmentLength * segmentLength);
        t = Mathf.Clamp01(t); // Clamp to segment bounds
        
        closestPoint = segmentStart + t * segmentVector;
    }
    
    public float GetRouteDistance()
    {
        return totalRouteDistance;
    }
    
    public Vector3 GetRouteStartPoint()
    {
        return routePoints.Count > 0 ? routePoints[0] : Vector3.zero;
    }
    
    public Vector3 GetRouteEndPoint()
    {
        return routePoints.Count > 0 ? routePoints[routePoints.Count - 1] : Vector3.zero;
    }
    
    // ROUTE CONFIGURATION METHODS
    public string GetCurrentRouteType()
    {
        return currentRouteType;
    }
    
    public float GetCurrentRouteDistance()
    {
        return currentRouteDistance;
    }
    
    public Transform[] GetActiveRouteWaypoints()
    {
        return activeRouteWaypoints;
    }
    
    // WAYPOINT ACCESS METHODS
    
    public bool IsRouteCompleted() => routeCompleted;
    public int GetCurrentWaypointIndex() => currentWaypointIndex;
    public int GetTotalWaypoints() => waypoints.Count;
    public Vector3 GetNextWaypoint() => currentWaypointIndex < waypoints.Count ? waypoints[currentWaypointIndex].position : Vector3.zero;
    public float GetProgressPercentage() => waypoints.Count > 0 ? (float)currentWaypointIndex / waypoints.Count : 0f;
    
    public void ResetRoute()
    {
        currentWaypointIndex = 0;
        routeCompleted = false;
        
        // Reset waypoint markers
        foreach (WaypointData waypoint in waypoints)
        {
            waypoint.reached = false;
            if (waypoint.markerObject != null)
            {
                Renderer renderer = waypoint.markerObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color resetColor = new Color(routeColor.r, routeColor.g, routeColor.b, routeOpacity);
                    renderer.material.color = Color.Lerp(resetColor, Color.white, 0.4f);
                }
            }
        }
        
        Debug.Log("Route reset");
    }
    
    // COMPATIBILITY METHODS - for systems that expect these
    public bool IsDashedLineEnabled() => false; // Cube system doesn't use dashed lines
    
    public void ToggleDashedLine()
    {
        // Not applicable for cube system, but maintained for compatibility
        Debug.Log("Dashed line toggle not applicable for cube-based route system");
    }
    
    // CONTEXT MENU METHODS FOR TESTING
    
    [ContextMenu("Manual: Configure Short Route")]
    public void ManualConfigureShortRoute()
    {
        ConfigureRoute("short");
        RefreshRouteVisualization();
    }
    
    [ContextMenu("Manual: Configure Long Route")]
    public void ManualConfigureLongRoute()
    {
        ConfigureRoute("long");
        RefreshRouteVisualization();
    }
    
    [ContextMenu("Manual: Disable Route")]
    public void ManualDisableRoute()
    {
        ConfigureRoute("none");
    }
    
    [ContextMenu("Debug: Show Route Configuration")]
    public void DebugShowRouteConfiguration()
    {
        Debug.Log($"=== ROUTE CONFIGURATION DEBUG ===");
        Debug.Log($"Current Route Type: {currentRouteType}");
        Debug.Log($"Active Waypoints: {(activeRouteWaypoints != null ? activeRouteWaypoints.Length : 0)}");
        Debug.Log($"Route Distance: {currentRouteDistance:F1}m");
        Debug.Log($"Route Points: {routePoints.Count}");
        Debug.Log($"Route Markers: {routeMarkers.Count}");
        
        Debug.Log($"Short Route Available: {(shortRouteWaypoints != null ? shortRouteWaypoints.Length : 0)} waypoints");
        if (shortRouteWaypoints != null && shortRouteWaypoints.Length > 1)
        {
            float shortDistance = CalculateRouteDistance(shortRouteWaypoints);
            Debug.Log($"  Short Route Distance: {shortDistance:F1}m");
        }
        
        Debug.Log($"Long Route Available: {(longRouteWaypoints != null ? longRouteWaypoints.Length : 0)} waypoints");
        if (longRouteWaypoints != null && longRouteWaypoints.Length > 1)
        {
            float longDistance = CalculateRouteDistance(longRouteWaypoints);
            Debug.Log($"  Long Route Distance: {longDistance:F1}m");
        }
        
        if (useSessionManager)
        {
            if (SessionManager.Instance != null)
            {
                string currentTrial = SessionManager.Instance.GetCurrentTrial();
                string expectedRouteType = SessionManager.Instance.GetRouteType(currentTrial);
                Debug.Log($"SessionManager Current Trial: {currentTrial}");
                Debug.Log($"Expected Route Type: {expectedRouteType}");
            }
            else
            {
                Debug.LogWarning("SessionManager integration enabled but no SessionManager found!");
            }
        }
    }
    
    [ContextMenu("Validate Route Setup")]
    public void ValidateRouteSetup()
    {
        bool isValid = true;
        
        Debug.Log("=== VALIDATING ROUTE SETUP ===");
        
        // Check short route
        if (shortRouteWaypoints == null || shortRouteWaypoints.Length < 2)
        {
            Debug.LogError("Short route needs at least 2 waypoints!");
            isValid = false;
        }
        else
        {
            Debug.Log($"✅ Short route: {shortRouteWaypoints.Length} waypoints");
            
            // Calculate short route distance
            float shortDistance = CalculateRouteDistance(shortRouteWaypoints);
            Debug.Log($"📏 Short route distance: {shortDistance:F1}m");
            
            if (shortDistance < 10f || shortDistance > 30f)
            {
                Debug.LogWarning($"⚠️ Short route distance ({shortDistance:F1}m) outside recommended range (10-30m)");
            }
        }
        
        // Check long route
        if (longRouteWaypoints == null || longRouteWaypoints.Length < 2)
        {
            Debug.LogError("Long route needs at least 2 waypoints!");
            isValid = false;
        }
        else
        {
            Debug.Log($"✅ Long route: {longRouteWaypoints.Length} waypoints");
            
            // Calculate long route distance
            float longDistance = CalculateRouteDistance(longRouteWaypoints);
            Debug.Log($"📏 Long route distance: {longDistance:F1}m");
            
            if (longDistance < 40f || longDistance > 100f)
            {
                Debug.LogWarning($"⚠️ Long route distance ({longDistance:F1}m) outside recommended range (40-100m)");
            }
        }
        
        // Check SessionManager integration
        if (useSessionManager)
        {
            if (SessionManager.Instance != null)
            {
                Debug.Log("✅ SessionManager integration active");
            }
            else
            {
                Debug.LogError("⚠️ SessionManager integration enabled but no SessionManager found!");
                isValid = false;
            }
        }
        
        if (isValid)
        {
            Debug.Log("✅ Route setup validation PASSED");
        }
        else
        {
            Debug.LogError("❌ Route setup validation FAILED");
        }
    }
    
    float CalculateRouteDistance(Transform[] waypoints)
    {
        if (waypoints == null || waypoints.Length < 2) return 0f;
        
        float totalDistance = 0f;
        for (int i = 1; i < waypoints.Length; i++)
        {
            if (waypoints[i-1] != null && waypoints[i] != null)
            {
                totalDistance += Vector3.Distance(waypoints[i-1].position, waypoints[i].position);
            }
        }
        return totalDistance;
    }
    
    [ContextMenu("Test: Force SessionManager Trial Change")]
    public void TestForceTrialChange()
    {
        if (SessionManager.Instance != null)
        {
            // Cycle between trial types for testing
            string[] testTrials = { "short_algorithmic", "long_algorithmic", "short_llm", "long_llm", "baseline" };
            string currentTrial = SessionManager.Instance.GetCurrentTrial();
            
            int currentIndex = System.Array.IndexOf(testTrials, currentTrial);
            int nextIndex = (currentIndex + 1) % testTrials.Length;
            
            SessionManager.Instance.SetCurrentTrial(testTrials[nextIndex]);
            Debug.Log($"Force changed trial to: {testTrials[nextIndex]}");
        }
        else
        {
            Debug.LogError("No SessionManager found for testing!");
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
    
    // GIZMOS FOR VISUALIZATION
    void OnDrawGizmosSelected()
    {
        // Draw short route in green
        if (shortRouteWaypoints != null && shortRouteWaypoints.Length > 1)
        {
            Gizmos.color = Color.green;
            DrawRouteGizmos(shortRouteWaypoints, "SHORT");
        }
        
        // Draw long route in blue
        if (longRouteWaypoints != null && longRouteWaypoints.Length > 1)
        {
            Gizmos.color = Color.blue;
            DrawRouteGizmos(longRouteWaypoints, "LONG");
        }
        
        // Highlight active route in yellow
        if (activeRouteWaypoints != null && activeRouteWaypoints.Length > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < activeRouteWaypoints.Length - 1; i++)
            {
                if (activeRouteWaypoints[i] != null && activeRouteWaypoints[i + 1] != null)
                {
                    Vector3 start = activeRouteWaypoints[i].position + Vector3.up * 0.5f;
                    Vector3 end = activeRouteWaypoints[i + 1].position + Vector3.up * 0.5f;
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
    
    void DrawRouteGizmos(Transform[] waypoints, string routeName)
    {
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            
            Vector3 pos = waypoints[i].position;
            
            // Draw waypoint sphere
            Gizmos.DrawWireSphere(pos, 1f);
            
            // Draw line to next waypoint
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(pos, waypoints[i + 1].position);
                
                // Draw direction arrow
                Vector3 direction = (waypoints[i + 1].position - pos).normalized;
                Vector3 arrowPos = Vector3.Lerp(pos, waypoints[i + 1].position, 0.7f);
                
                Vector3 arrowLeft = arrowPos + Quaternion.Euler(0, -30, 0) * (-direction) * 2f;
                Vector3 arrowRight = arrowPos + Quaternion.Euler(0, 30, 0) * (-direction) * 2f;
                
                Gizmos.DrawLine(arrowPos, arrowLeft);
                Gizmos.DrawLine(arrowPos, arrowRight);
            }
        }
        
        // Draw total distance info
        float distance = CalculateRouteDistance(waypoints);
        if (waypoints.Length > 1)
        {
            Vector3 midPoint = Vector3.Lerp(waypoints[0].position, waypoints[waypoints.Length - 1].position, 0.5f);
            
#if UNITY_EDITOR
            UnityEditor.Handles.Label(midPoint + Vector3.up * 5f, $"{routeName}: {distance:F1}m", 
                UnityEditor.EditorStyles.whiteLargeLabel);
#endif
        }
    }
}

// Helper component for pulsing effect (unchanged)
public class PulsingMarker : MonoBehaviour
{
    public float pulseSpeed = 1f;
    public Vector3 originalScale = Vector3.one;
    
    private float pulseTime = 0f;
    
    void Update()
    {
        pulseTime += Time.deltaTime * pulseSpeed;
        float pulseScale = 1f + 0.15f * Mathf.Sin(pulseTime);
        transform.localScale = originalScale * pulseScale;
    }
}