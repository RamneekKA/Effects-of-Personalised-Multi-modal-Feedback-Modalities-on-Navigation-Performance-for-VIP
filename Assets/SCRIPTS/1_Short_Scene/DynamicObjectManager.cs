using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enhanced Dynamic Object Manager with close proximity bounding box detection
/// Shows bounding boxes only for objects within 1m and in user's field of view
/// </summary>
public class DynamicObjectManager : MonoBehaviour
{
    [Header("Dynamic Object Control")]
    [Tooltip("Automatically find and manage all moving objects")]
    public bool autoFindDynamicObjects = true;
    
    [Tooltip("Manually assign specific objects to control")]
    public GameObject[] manualDynamicObjects;
    
    [Header("Detection Settings")]
    [Tooltip("Objects with these components will be considered dynamic")]
    public bool managePythonScripts = true;
    public bool manageCarAI = true;
    public bool manageRigidbodies = true;
    public bool manageNavMeshAgents = true;
    
    [Header("Enhanced Bounding Box Settings")]
    [Tooltip("Show bounding boxes around nearby DetectableObjects")]
    public bool showBoundingBoxes = false;
    
    [Tooltip("Maximum distance to show bounding boxes (recommend 1-3 meters for close proximity)")]
    [Range(0.5f, 5f)]
    public float boundingBoxRange = 1.5f;
    
    [Tooltip("Field of view angle for bounding box detection (degrees)")]
    [Range(30f, 120f)]
    public float fieldOfViewAngle = 80f;
    
    [Tooltip("Perform raycast to check if object is actually visible (not behind walls)")]
    public bool useOcclusionChecking = true;
    
    [Tooltip("Color of the bounding boxes")]
    public Color boundingBoxColor = Color.green;
    
    [Tooltip("Width of the bounding box lines")]
    [Range(0.01f, 0.3f)]
    public float lineWidth = 0.02f;
    
    [Tooltip("Show object labels above bounding boxes")]
    public bool showLabels = true;
    
    [Header("Enhanced Visibility Detection")]
    [Tooltip("Check multiple points on object bounds for better visibility detection")]
    public bool useAdvancedVisibilityCheck = true;
    
    [Tooltip("Minimum percentage of object bounds that must be visible")]
    [Range(0.1f, 1f)]
    public float visibilityThreshold = 0.3f;
    
    [Header("Manual Control")]
    [Tooltip("Manually pause/resume dynamic objects (overrides automatic behavior)")]
    public bool manualControl = false;
    
    [Header("Scene Analysis Integration")]
    [Tooltip("Automatically resume objects during scene analysis")]
    public bool allowSceneAnalysisOverride = true;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool showVisibilityDebugRays = false;
    
    // Storage for paused object states
    private List<DynamicObjectState> dynamicObjects = new List<DynamicObjectState>();
    private bool objectsPaused = false;
    
    // Enhanced bounding box visualization
    private Camera detectionCamera;
    private List<DetectableObject> detectedObjects = new List<DetectableObject>();
    private Dictionary<DetectableObject, GameObject> boundingBoxes = new Dictionary<DetectableObject, GameObject>();
    private Material boundingBoxMaterial;
    
    // Update every frame for real-time responsiveness
    
    [System.Serializable]
    private class DynamicObjectState
    {
        public GameObject gameObject;
        public string objectType;
        public bool wasEnabled;
        
        // Rigidbody state
        public Rigidbody rigidbody;
        public bool wasKinematic;
        public Vector3 savedVelocity;
        public Vector3 savedAngularVelocity;
        
        // NavMeshAgent state
        public UnityEngine.AI.NavMeshAgent navMeshAgent;
        public bool agentWasEnabled;
        
        // MonoBehaviour scripts state
        public List<MonoBehaviour> scripts = new List<MonoBehaviour>();
        public List<bool> scriptStates = new List<bool>();
        
        // Transform state (for objects that move via transform)
        public Vector3 savedPosition;
        public Quaternion savedRotation;
    }
    
    void Start()
    {
        // Initialize detection camera
        detectionCamera = Camera.main;
        if (detectionCamera == null)
            detectionCamera = FindObjectOfType<Camera>();
        
        // Create bounding box material
        boundingBoxMaterial = CreateBoundingBoxMaterial();
        
        if (autoFindDynamicObjects)
        {
            FindAllDynamicObjects();
        }
        
        // Add manually assigned objects
        foreach (GameObject obj in manualDynamicObjects)
        {
            if (obj != null)
            {
                AddDynamicObject(obj, "Manual");
            }
        }
        
        Debug.Log($"DynamicObjectManager initialized with {dynamicObjects.Count} dynamic objects");
        Debug.Log($"Enhanced bounding boxes: {(showBoundingBoxes ? "ENABLED" : "DISABLED")} (Range: {boundingBoxRange}m, FOV: {fieldOfViewAngle}Ãƒâ€šÃ‚Â°)");
    }
    
    void Update()
    {
        // Update bounding boxes every frame for real-time responsiveness
        if (showBoundingBoxes)
        {
            UpdateSimplifiedBoundingBoxVisualization();
        }
        else
        {
            // Clear bounding boxes if disabled
            ClearBoundingBoxes();
        }
    }
    
    void FindAllDynamicObjects()
    {
        dynamicObjects.Clear();
        
        // Find objects with Rigidbodies
        if (manageRigidbodies)
        {
            Rigidbody[] rigidbodies = FindObjectsOfType<Rigidbody>();
            foreach (Rigidbody rb in rigidbodies)
            {
                // Skip the player character
                FCG.CharacterControl playerController = rb.GetComponent<FCG.CharacterControl>();
                if (playerController != null) continue;
                
                AddDynamicObject(rb.gameObject, "Rigidbody");
            }
        }
        
        // Find objects with NavMeshAgents
        if (manageNavMeshAgents)
        {
            UnityEngine.AI.NavMeshAgent[] agents = FindObjectsOfType<UnityEngine.AI.NavMeshAgent>();
            foreach (UnityEngine.AI.NavMeshAgent agent in agents)
            {
                AddDynamicObject(agent.gameObject, "NavMeshAgent");
            }
        }
        
        // Find objects with common movement scripts
        if (managePythonScripts)
        {
            FindObjectsWithMovementScripts();
        }
        
        // Find Car AI scripts specifically
        if (manageCarAI)
        {
            FindCarAIObjects();
        }
    }
    
    void FindObjectsWithMovementScripts()
    {
        // Look for common Unity movement script patterns
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
        
        foreach (MonoBehaviour script in allScripts)
        {
            string scriptName = script.GetType().Name.ToLower();
            
            // Common movement script patterns
            if (scriptName.Contains("movement") || 
                scriptName.Contains("mover") || 
                scriptName.Contains("drive") || 
                scriptName.Contains("vehicle") || 
                scriptName.Contains("car") || 
                scriptName.Contains("traffic") || 
                scriptName.Contains("patrol") || 
                scriptName.Contains("wander") ||
                scriptName.Contains("follow") ||
                scriptName.Contains("ai"))
            {
                AddDynamicObject(script.gameObject, $"MovementScript ({script.GetType().Name})");
            }
        }
    }
    
    void FindCarAIObjects()
    {
        // Look for objects that are likely vehicles based on their names
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            string objName = obj.name.ToLower();
            
            if ((objName.Contains("car") || 
                 objName.Contains("vehicle") || 
                 objName.Contains("bus") || 
                 objName.Contains("truck") || 
                 objName.Contains("van")) &&
                !objName.Contains("player") &&
                !objName.Contains("character"))
            {
                // Check if it has any scripts that might control movement
                MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
                if (scripts.Length > 0)
                {
                    AddDynamicObject(obj, "Vehicle AI");
                }
            }
        }
    }
    
    void AddDynamicObject(GameObject obj, string objectType)
    {
        // Check if already added
        foreach (DynamicObjectState existing in dynamicObjects)
        {
            if (existing.gameObject == obj)
                return; // Already added
        }
        
        DynamicObjectState state = new DynamicObjectState
        {
            gameObject = obj,
            objectType = objectType,
            wasEnabled = obj.activeInHierarchy
        };
        
        // Store Rigidbody state
        state.rigidbody = obj.GetComponent<Rigidbody>();
        if (state.rigidbody != null)
        {
            state.wasKinematic = state.rigidbody.isKinematic;
            state.savedVelocity = state.rigidbody.linearVelocity;
            state.savedAngularVelocity = state.rigidbody.angularVelocity;
        }
        
        // Store NavMeshAgent state
        state.navMeshAgent = obj.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (state.navMeshAgent != null)
        {
            state.agentWasEnabled = state.navMeshAgent.enabled;
        }
        
        // Store all MonoBehaviour scripts that might control movement
        MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            // Skip certain Unity built-in components
            if (script is Transform || script is Rigidbody || script is Collider ||
                script is Renderer || script is UnityEngine.AI.NavMeshAgent)
                continue;
            
            // Skip the DetectableObject script (it's just for classification)
            if (script is DetectableObject)
                continue;
            
            state.scripts.Add(script);
            state.scriptStates.Add(script.enabled);
        }
        
        // Store transform state
        state.savedPosition = obj.transform.position;
        state.savedRotation = obj.transform.rotation;
        
        dynamicObjects.Add(state);
        
        if (showDebugInfo)
        {
            Debug.Log($"Added dynamic object: {obj.name} ({objectType})");
        }
    }
    
    #region Enhanced Bounding Box Visualization
    
    void UpdateSimplifiedBoundingBoxVisualization()
    {
        if (detectionCamera == null) return;
        
        DetectNearbyObjectsSimple();
        UpdateBoundingBoxes();
    }
    
    void DetectNearbyObjectsSimple()
    {
        detectedObjects.Clear();
        
        // Find all detectable objects within close proximity and viewing angle only
        DetectableObject[] allDetectable = FindObjectsOfType<DetectableObject>();
        
        foreach (DetectableObject obj in allDetectable)
        {
            if (obj == null || obj.gameObject == null) continue;
            
            // Check distance to object bounds (not center) - more accurate for large objects
            float distanceToBounds = GetDistanceToBounds(detectionCamera.transform.position, obj);
            if (distanceToBounds > boundingBoxRange) continue;
            
            // Check if object is within field of view (use center point for FOV calculation)
            float distanceToCenter = Vector3.Distance(detectionCamera.transform.position, obj.transform.position);
            if (!IsObjectInFieldOfView(obj, distanceToCenter)) continue;
            
            // Object passed both distance and FOV checks - add it
            detectedObjects.Add(obj);
            
            if (showDebugInfo && detectedObjects.Count <= 10) // Limit debug spam
            {
                Debug.Log($"Detected object: {obj.className} ({obj.name}) - bounds distance: {distanceToBounds:F2}m, center distance: {distanceToCenter:F2}m");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Total objects detected: {detectedObjects.Count}");
        }
    }
    
    float GetDistanceToBounds(Vector3 point, DetectableObject obj)
    {
        // Get the object's world bounds
        Bounds bounds = obj.worldBounds;
        
        // If bounds are not set or invalid, fall back to renderer bounds
        if (bounds.size == Vector3.zero)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                bounds = renderer.bounds;
            }
            else
            {
                // Final fallback: use collider bounds or distance to transform
                Collider collider = obj.GetComponent<Collider>();
                if (collider != null)
                {
                    bounds = collider.bounds;
                }
                else
                {
                    // No bounds available, fall back to center distance
                    return Vector3.Distance(point, obj.transform.position);
                }
            }
        }
        
        // Calculate closest point on bounds to the given point
        Vector3 closestPoint = bounds.ClosestPoint(point);
        
        // Return distance from point to closest point on bounds
        float distance = Vector3.Distance(point, closestPoint);
        
        // If point is inside bounds, distance will be 0, so return a small value
        if (distance == 0f)
        {
            distance = 0.01f; // Very close but not zero
        }
        
        return distance;
    }
    
    bool IsObjectInFieldOfView(DetectableObject obj, float distance)
    {
        Vector3 cameraPosition = detectionCamera.transform.position;
        Vector3 cameraForward = detectionCamera.transform.forward;
        Vector3 directionToObject = (obj.transform.position - cameraPosition).normalized;
        
        // Calculate angle between camera forward and direction to object
        float angle = Vector3.Angle(cameraForward, directionToObject);
        
        // Check if within field of view angle
        bool inFOV = angle <= fieldOfViewAngle * 0.5f;
        
        if (showVisibilityDebugRays && inFOV)
        {
            Debug.DrawRay(cameraPosition, directionToObject * distance, Color.green, 0.1f);
        }
        else if (showVisibilityDebugRays)
        {
            Debug.DrawRay(cameraPosition, directionToObject * distance, Color.red, 0.1f);
        }
        
        return inFOV;
    }
    
    bool IsObjectOccluded(DetectableObject obj)
    {
        Vector3 cameraPosition = detectionCamera.transform.position;
        Vector3 objectPosition = obj.transform.position;
        Vector3 direction = (objectPosition - cameraPosition).normalized;
        float distance = Vector3.Distance(cameraPosition, objectPosition);
        
        // Perform raycast to check for occlusion
        RaycastHit hit;
        if (Physics.Raycast(cameraPosition, direction, out hit, distance))
        {
            // Check if the raycast hit the target object or something else
            DetectableObject hitDetectable = hit.collider.GetComponent<DetectableObject>();
            if (hitDetectable != null && hitDetectable == obj)
            {
                // Ray hit the target object directly
                return false;
            }
            else
            {
                // Ray hit something else first (object is occluded)
                if (showVisibilityDebugRays)
                {
                    Debug.DrawRay(cameraPosition, direction * hit.distance, Color.yellow, 0.1f);
                }
                return true;
            }
        }
        
        // No obstruction found
        return false;
    }
    
    bool IsObjectSufficientlyVisible(DetectableObject obj)
    {
        // Get object bounds
        Bounds bounds = obj.worldBounds;
        if (bounds.size == Vector3.zero)
        {
            // Fallback to renderer bounds if DetectableObject bounds not set
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
                bounds = renderer.bounds;
            else
                return true; // Can't check visibility, assume visible
        }
        
        // Check visibility of multiple points on the object bounds
        Vector3[] checkPoints = GetBoundsCheckPoints(bounds);
        int visiblePoints = 0;
        
        foreach (Vector3 point in checkPoints)
        {
            Vector3 screenPoint = detectionCamera.WorldToViewportPoint(point);
            
            // Check if point is in camera view and in front of camera
            if (screenPoint.z > 0 && screenPoint.x >= 0 && screenPoint.x <= 1 && 
                screenPoint.y >= 0 && screenPoint.y <= 1)
            {
                visiblePoints++;
            }
        }
        
        float visibilityRatio = (float)visiblePoints / checkPoints.Length;
        return visibilityRatio >= visibilityThreshold;
    }
    
    Vector3[] GetBoundsCheckPoints(Bounds bounds)
    {
        // Check 9 points: 8 corners + center
        Vector3 center = bounds.center;
        Vector3 size = bounds.size * 0.5f;
        
        return new Vector3[]
        {
            center, // Center point
            center + new Vector3(-size.x, -size.y, -size.z), // Bottom-back-left
            center + new Vector3(+size.x, -size.y, -size.z), // Bottom-back-right
            center + new Vector3(-size.x, +size.y, -size.z), // Top-back-left
            center + new Vector3(+size.x, +size.y, -size.z), // Top-back-right
            center + new Vector3(-size.x, -size.y, +size.z), // Bottom-front-left
            center + new Vector3(+size.x, -size.y, +size.z), // Bottom-front-right
            center + new Vector3(-size.x, +size.y, +size.z), // Top-front-left
            center + new Vector3(+size.x, +size.y, +size.z)  // Top-front-right
        };
    }
    
    void UpdateBoundingBoxes()
    {
        // Remove old boxes for objects no longer detected
        List<DetectableObject> toRemove = new List<DetectableObject>();
        foreach (var kvp in boundingBoxes)
        {
            if (!detectedObjects.Contains(kvp.Key) || kvp.Key == null)
            {
                if (kvp.Value != null)
                    DestroyImmediate(kvp.Value);
                toRemove.Add(kvp.Key);
            }
            else
            {
                // Update position of existing boxes
                UpdateBoundingBoxPosition(kvp.Value, kvp.Key);
            }
        }
        
        foreach (var obj in toRemove)
        {
            boundingBoxes.Remove(obj);
        }
        
        // Create new bounding boxes for newly detected objects
        foreach (DetectableObject obj in detectedObjects)
        {
            if (!boundingBoxes.ContainsKey(obj))
            {
                GameObject boundingBox = CreateBoundingBox(obj);
                boundingBoxes[obj] = boundingBox;
            }
        }
    }
    
    void UpdateBoundingBoxPosition(GameObject boundingBox, DetectableObject obj)
    {
        Vector3[] corners = GetBoundingBoxCorners(obj.worldBounds);
        
        // Define the 12 edges of a cube
        int[,] edges = new int[,] {
            {0,1}, {1,2}, {2,3}, {3,0}, // bottom face
            {4,5}, {5,6}, {6,7}, {7,4}, // top face
            {0,4}, {1,5}, {2,6}, {3,7}  // vertical edges
        };
        
        // Update all LineRenderer positions
        for (int i = 0; i < 12; i++)
        {
            Transform edgeTransform = boundingBox.transform.GetChild(i);
            LineRenderer line = edgeTransform.GetComponent<LineRenderer>();
            
            if (line != null)
            {
                line.SetPosition(0, corners[edges[i,0]]);
                line.SetPosition(1, corners[edges[i,1]]);
            }
        }
        
        // Update label position if it exists
        Transform labelTransform = boundingBox.transform.Find("Label");
        if (labelTransform != null)
        {
            Vector3 labelPosition = obj.worldBounds.center + Vector3.up * (obj.worldBounds.size.y * 0.5f + 0.3f);
            labelTransform.position = labelPosition;
            
            // Update label orientation
            if (detectionCamera != null)
            {
                labelTransform.LookAt(detectionCamera.transform);
                labelTransform.Rotate(0, 180, 0);
            }
        }
    }
    
    void ClearBoundingBoxes()
    {
        foreach (var kvp in boundingBoxes)
        {
            if (kvp.Value != null)
                DestroyImmediate(kvp.Value);
        }
        boundingBoxes.Clear();
    }
    
    GameObject CreateBoundingBox(DetectableObject obj)
    {
        GameObject boundingBox = new GameObject($"BoundingBox_{obj.className}");
        boundingBox.transform.SetParent(this.transform);
        
        // Create the wireframe cube using LineRenderer
        Vector3[] corners = GetBoundingBoxCorners(obj.worldBounds);
        
        // Define the 12 edges of a cube
        int[,] edges = new int[,] {
            {0,1}, {1,2}, {2,3}, {3,0}, // bottom face
            {4,5}, {5,6}, {6,7}, {7,4}, // top face
            {0,4}, {1,5}, {2,6}, {3,7}  // vertical edges
        };
        
        for (int i = 0; i < 12; i++)
        {
            GameObject lineObj = new GameObject($"Edge_{i}");
            lineObj.transform.SetParent(boundingBox.transform);
            
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.material = boundingBoxMaterial;
            line.startColor = boundingBoxColor;
            line.endColor = boundingBoxColor;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.positionCount = 2;
            line.useWorldSpace = true;
            
            line.SetPosition(0, corners[edges[i,0]]);
            line.SetPosition(1, corners[edges[i,1]]);
        }
        
        // Add label if enabled
        if (showLabels)
        {
            CreateLabel(boundingBox, obj);
        }
        
        return boundingBox;
    }
    
    Vector3[] GetBoundingBoxCorners(Bounds bounds)
    {
        Vector3[] corners = new Vector3[8];
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;
        
        corners[0] = center + new Vector3(-size.x, -size.y, -size.z) * 0.5f; // bottom-back-left
        corners[1] = center + new Vector3(+size.x, -size.y, -size.z) * 0.5f; // bottom-back-right
        corners[2] = center + new Vector3(+size.x, -size.y, +size.z) * 0.5f; // bottom-front-right
        corners[3] = center + new Vector3(-size.x, -size.y, +size.z) * 0.5f; // bottom-front-left
        corners[4] = center + new Vector3(-size.x, +size.y, -size.z) * 0.5f; // top-back-left
        corners[5] = center + new Vector3(+size.x, +size.y, -size.z) * 0.5f; // top-back-right
        corners[6] = center + new Vector3(+size.x, +size.y, +size.z) * 0.5f; // top-front-right
        corners[7] = center + new Vector3(-size.x, +size.y, +size.z) * 0.5f; // top-front-left
        
        return corners;
    }
    
    void CreateLabel(GameObject parent, DetectableObject obj)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(parent.transform, false);
        
        // Position label just above the top of the bounding box
        Vector3 labelPosition = obj.worldBounds.center + Vector3.up * (obj.worldBounds.size.y * 0.5f + 0.3f);
        labelObj.transform.position = labelPosition;
        
        // Make label face the camera
        if (detectionCamera != null)
        {
            labelObj.transform.LookAt(detectionCamera.transform);
            labelObj.transform.Rotate(0, 180, 0);
        }
        
        // Create text mesh
        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = obj.className;
        textMesh.fontSize = 50;
        textMesh.characterSize = 0.05f;
        textMesh.color = Color.white;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
    }
    
    Material CreateBoundingBoxMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = boundingBoxColor;
        return mat;
    }
    
    #endregion
    
    #region Dynamic Object Control (existing functionality)
    
    public void PauseAllDynamicObjects()
    {
        if (objectsPaused) return;
        
        Debug.Log("PAUSING all dynamic objects...");
        
        foreach (DynamicObjectState state in dynamicObjects)
        {
            if (state.gameObject == null) continue;
            
            // Pause Rigidbody
            if (state.rigidbody != null)
            {
                state.savedVelocity = state.rigidbody.linearVelocity;
                state.savedAngularVelocity = state.rigidbody.angularVelocity;
                state.rigidbody.linearVelocity = Vector3.zero;
                state.rigidbody.angularVelocity = Vector3.zero;
                state.rigidbody.isKinematic = true;
            }
            
            // Pause NavMeshAgent
            if (state.navMeshAgent != null && state.navMeshAgent.enabled)
            {
                state.navMeshAgent.enabled = false;
            }
            
            // Pause movement scripts
            for (int i = 0; i < state.scripts.Count; i++)
            {
                if (state.scripts[i] != null && state.scripts[i].enabled)
                {
                    state.scripts[i].enabled = false;
                }
            }
        }
        
        objectsPaused = true;
        Debug.Log($"Paused {dynamicObjects.Count} dynamic objects");
    }
    
    public void ResumeAllDynamicObjects()
    {
        if (!objectsPaused) return;
        
        Debug.Log("RESUMING all dynamic objects!");
        
        foreach (DynamicObjectState state in dynamicObjects)
        {
            if (state.gameObject == null) continue;
            
            // Resume Rigidbody
            if (state.rigidbody != null)
            {
                state.rigidbody.isKinematic = state.wasKinematic;
            }
            
            // Resume NavMeshAgent
            if (state.navMeshAgent != null && state.agentWasEnabled)
            {
                state.navMeshAgent.enabled = true;
            }
            
            // Resume movement scripts
            for (int i = 0; i < state.scripts.Count; i++)
            {
                if (state.scripts[i] != null && state.scriptStates[i])
                {
                    state.scripts[i].enabled = true;
                }
            }
        }
        
        objectsPaused = false;
        Debug.Log($"Resumed {dynamicObjects.Count} dynamic objects");
    }
    
    #endregion
    
    #region Public API Methods
    
    public bool AreObjectsPaused()
    {
        return objectsPaused;
    }
    
    public int GetDynamicObjectCount()
    {
        return dynamicObjects.Count;
    }
    
    public List<string> GetDynamicObjectNames()
    {
        List<string> names = new List<string>();
        foreach (DynamicObjectState state in dynamicObjects)
        {
            if (state.gameObject != null)
            {
                names.Add($"{state.gameObject.name} ({state.objectType})");
            }
        }
        return names;
    }
    
    public bool AreBoundingBoxesVisible()
    {
        return showBoundingBoxes;
    }
    
    public int GetDetectedObjectCount()
    {
        return detectedObjects.Count;
    }
    
    public List<GameObject> GetAllDynamicObjects()
    {
        List<GameObject> objects = new List<GameObject>();
        foreach (DynamicObjectState state in dynamicObjects)
        {
            if (state.gameObject != null)
            {
                objects.Add(state.gameObject);
            }
        }
        return objects;
    }
    
    public void ForceResumeForSceneAnalysis()
    {
        if (!allowSceneAnalysisOverride) return;
        
        Debug.Log("FORCE RESUMING dynamic objects for scene analysis...");
        
        foreach (DynamicObjectState state in dynamicObjects)
        {
            if (state.gameObject == null) continue;
            
            // Force resume Rigidbody
            if (state.rigidbody != null)
            {
                state.rigidbody.isKinematic = state.wasKinematic;
            }
            
            // Force resume NavMeshAgent
            if (state.navMeshAgent != null && state.agentWasEnabled)
            {
                state.navMeshAgent.enabled = true;
            }
            
            // Force resume movement scripts
            for (int i = 0; i < state.scripts.Count; i++)
            {
                if (state.scripts[i] != null && state.scriptStates[i])
                {
                    state.scripts[i].enabled = true;
                }
            }
        }
        
        Debug.Log($"Force resumed {dynamicObjects.Count} objects for scene analysis");
    }
    
    public void RestorePauseStateAfterSceneAnalysis()
    {
        if (!allowSceneAnalysisOverride) return;
        
        if (objectsPaused)
        {
            Debug.Log("RESTORING pause state after scene analysis...");
            
            // Re-pause all objects
            foreach (DynamicObjectState state in dynamicObjects)
            {
                if (state.gameObject == null) continue;
                
                // Re-pause Rigidbody
                if (state.rigidbody != null)
                {
                    state.rigidbody.linearVelocity = Vector3.zero;
                    state.rigidbody.angularVelocity = Vector3.zero;
                    state.rigidbody.isKinematic = true;
                }
                
                // Re-pause NavMeshAgent
                if (state.navMeshAgent != null)
                {
                    state.navMeshAgent.enabled = false;
                }
                
                // Re-pause movement scripts
                for (int i = 0; i < state.scripts.Count; i++)
                {
                    if (state.scripts[i] != null)
                    {
                        state.scripts[i].enabled = false;
                    }
                }
            }
            
            Debug.Log($"Restored pause state for {dynamicObjects.Count} objects");
        }
    }
    
    #endregion
    
    #region Context Menu Methods
    
    [ContextMenu("Toggle Enhanced Bounding Boxes")]
    public void ToggleBoundingBoxes()
    {
        showBoundingBoxes = !showBoundingBoxes;
        if (!showBoundingBoxes)
        {
            ClearBoundingBoxes();
        }
        Debug.Log($"Enhanced bounding boxes: {(showBoundingBoxes ? "ENABLED" : "DISABLED")}");
    }
    
    [ContextMenu("Test Close Proximity Detection")]
    public void TestCloseProximityDetection()
    {
        if (detectionCamera == null)
        {
            Debug.LogError("No detection camera found!");
            return;
        }
        
        Debug.Log($"Testing close proximity detection (Range: {boundingBoxRange}m, FOV: {fieldOfViewAngle}Ãƒâ€šÃ‚Â°)");
        
        DetectableObject[] allObjects = FindObjectsOfType<DetectableObject>();
        int closeObjects = 0;
        int inViewObjects = 0;
        int visibleObjects = 0;
        
        foreach (DetectableObject obj in allObjects)
        {
            float distance = Vector3.Distance(detectionCamera.transform.position, obj.transform.position);
            
            if (distance <= boundingBoxRange)
            {
                closeObjects++;
                
                if (IsObjectInFieldOfView(obj, distance))
                {
                    inViewObjects++;
                    
                    if (!useOcclusionChecking || !IsObjectOccluded(obj))
                    {
                        if (!useAdvancedVisibilityCheck || IsObjectSufficientlyVisible(obj))
                        {
                            visibleObjects++;
                            Debug.Log($"  VISIBLE: {obj.className} at {distance:F2}m");
                        }
                    }
                }
            }
        }
        
        Debug.Log($"Results: {closeObjects} close, {inViewObjects} in view, {visibleObjects} fully visible");
    }
    
    [ContextMenu("Debug: Show Detection Settings")]
    public void DebugShowDetectionSettings()
    {
        Debug.Log("ENHANCED BOUNDING BOX SETTINGS:");
        Debug.Log($"Range: {boundingBoxRange}m");
        Debug.Log($"Field of View: {fieldOfViewAngle}Ãƒâ€šÃ‚Â°");
        Debug.Log($"Occlusion Checking: {useOcclusionChecking}");
        Debug.Log($"Advanced Visibility: {useAdvancedVisibilityCheck}");
        Debug.Log($"Visibility Threshold: {visibilityThreshold:P0}");
        Debug.Log($"Updates: Every frame");
        Debug.Log($"Currently Detected: {detectedObjects.Count} objects");
    }
    
    [ContextMenu("Manual: Pause All Dynamic Objects")]
    public void ManualPauseAll()
    {
        PauseAllDynamicObjects();
    }
    
    [ContextMenu("Manual: Resume All Dynamic Objects")]
    public void ManualResumeAll()
    {
        ResumeAllDynamicObjects();
    }
    
    #endregion
    
    void OnDrawGizmosSelected()
    {
        if (showBoundingBoxes && detectionCamera != null)
        {
            // Draw detection range sphere
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(detectionCamera.transform.position, boundingBoxRange);
            
            // Draw field of view cone
            Gizmos.color = Color.cyan;
            Vector3 forward = detectionCamera.transform.forward;
            Vector3 left = Quaternion.AngleAxis(-fieldOfViewAngle * 0.5f, detectionCamera.transform.up) * forward;
            Vector3 right = Quaternion.AngleAxis(fieldOfViewAngle * 0.5f, detectionCamera.transform.up) * forward;
            
            Gizmos.DrawRay(detectionCamera.transform.position, left * boundingBoxRange);
            Gizmos.DrawRay(detectionCamera.transform.position, right * boundingBoxRange);
            Gizmos.DrawRay(detectionCamera.transform.position, forward * boundingBoxRange);
        }
    }
}