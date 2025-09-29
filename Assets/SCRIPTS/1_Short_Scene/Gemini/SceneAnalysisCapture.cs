using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;

/// <summary>
/// Enhanced Scene Analysis Capture with robust duplicate prevention
/// Uses position + name based deduplication to prevent counting objects multiple times
/// </summary>
public class SceneAnalysisCapture : MonoBehaviour
{
    [Header("Scene Analysis Settings")]
    public bool captureOnStart = false;
    public float analysisRadius = 100f;
    public bool includeMovingObjects = true;
    public bool captureTopDownScreenshot = true;
    
    [Header("Duplicate Prevention")]
    [Tooltip("Distance threshold for considering objects as duplicates (in meters)")]
    public float duplicatePositionThreshold = 0.1f;
    
    [Header("Route Analysis")]
    [Tooltip("Focus analysis around the navigation route")]
    public RouteGuideSystem routeGuideSystem;
    public float routeAnalysisWidth = 50f;
    public bool enableRouteBasedFiltering = true;
    
    [Header("Session Integration")]
    [Tooltip("Save to SessionManager folder structure")]
    public bool useSessionManager = true;
    
    [Header("Real-World Filtering")]
    [Tooltip("Only include objects that exist in real-world navigation scenarios")]
    public bool filterToRealWorldObjects = true;
    
    [Header("Object Classification")]
    public string[] buildingParentNames = {"Buildings", "City_Maker", "BD-B", "BD-L", "BD-LB", "BD-LT", "BD-RB"};
    public string[] obstacleParentNames = {"Obstacles", "Objects"};
    public string[] movingObjectTags = {"Car", "Vehicle", "Moving"};
    
    private Transform playerTransform;
    private string sessionID;
    private string sceneAnalysisPath;
    
    // DUPLICATE PREVENTION SYSTEM
    private HashSet<string> processedObjectKeys = new HashSet<string>();
    private List<ObjectSignature> processedSignatures = new List<ObjectSignature>();
    
    [System.Serializable]
    private class ObjectSignature
    {
        public string name;
        public Vector3 position;
        public string className;
        
        public ObjectSignature(string name, Vector3 position, string className)
        {
            this.name = name;
            this.position = position;
            this.className = className;
        }
        
        public bool IsDuplicateOf(ObjectSignature other, float positionThreshold)
        {
            // Same name and class, and very close position
            return this.name == other.name && 
                   this.className == other.className &&
                   Vector3.Distance(this.position, other.position) < positionThreshold;
        }
        
        public override string ToString()
        {
            return $"{className}({name}) at {position}";
        }
    }
    
    // Real-world object filtering
    private HashSet<string> technicalObjectTypes = new HashSet<string>
    {
        "Invisible Collider", "Parent Object", "Decoration", "Debug Object",
        "Scene Manager", "Ray Detector", "Reference Object", "Object Identifier",
        "Material Object", "Collision Object", "Scene Organizer", "Mesh Object",
        "Light Source", "Audio Source", "Particle Effect", "Dynamic Object",
        "Body Part", "Camera", "Unidentified Object"
    };

    private HashSet<string> realWorldObjectTypes = new HashSet<string>
    {
        // VEHICLES
        "Car", "Bus", "Van", "Truck", "Vehicle", "Bicycle",
        // INFRASTRUCTURE  
        "Building", "Road", "Sidewalk", "Wall", "Electrical Box", "Bench",
        // URBAN FURNITURE
        "Tree", "Pole", "Hydrant", "Bench", "Trash Bin", "Table & Chairs",
        "Street Sign", "Street Light", "Traffic Light",
        // SURFACES
        "Ground", "Grass", "Concrete", "Bin", "Barrier",
        // NAVIGATION
        "Navigation Path",
        // GEOMETRIC OBJECTS
        "Cube", "Sphere", "Plane", "Cylinder",
        // CITY ELEMENTS
        "Building Block", "City Element", "Person"
    };
    
    void Start()
    {
        // Get player reference
        FCG.CharacterControl characterControl = FindObjectOfType<FCG.CharacterControl>();
        if (characterControl != null)
            playerTransform = characterControl.transform;
        
        // Get route guide system if not assigned
        if (routeGuideSystem == null)
            routeGuideSystem = FindObjectOfType<RouteGuideSystem>();
        
        if (captureOnStart && playerTransform != null)
        {
            CaptureSceneAnalysis();
        }
    }
    
    #region Duplicate Prevention Methods
    
    void InitializeDuplicateDetection()
    {
        processedObjectKeys.Clear();
        processedSignatures.Clear();
        Debug.Log("ðŸ” Duplicate detection system initialized");
    }
    
    bool IsObjectAlreadyProcessed(GameObject obj, string className)
    {
        // Create signature for this object
        ObjectSignature newSignature = new ObjectSignature(obj.name, obj.transform.position, className);
        
        // Check against all previously processed signatures
        foreach (ObjectSignature existing in processedSignatures)
        {
            if (newSignature.IsDuplicateOf(existing, duplicatePositionThreshold))
            {
                Debug.Log($"ðŸš« DUPLICATE DETECTED: {newSignature} (matches {existing})");
                return true;
            }
        }
        
        // Not a duplicate - add to processed list
        processedSignatures.Add(newSignature);
        return false;
    }
    
    bool TryAddUniqueObject(StaticObjectData objectData, List<StaticObjectData> targetList, string source)
    {
        // Create signature for comparison
        ObjectSignature signature = new ObjectSignature(objectData.objectName, objectData.position, objectData.className);
        
        // Check for duplicates in the target list
        foreach (StaticObjectData existing in targetList)
        {
            ObjectSignature existingSignature = new ObjectSignature(existing.objectName, existing.position, existing.className);
            if (signature.IsDuplicateOf(existingSignature, duplicatePositionThreshold))
            {
                Debug.Log($"ðŸš« DUPLICATE BLOCKED: {signature} from {source} (already in list)");
                return false;
            }
        }
        
        // Check against global processed signatures
        foreach (ObjectSignature existing in processedSignatures)
        {
            if (signature.IsDuplicateOf(existing, duplicatePositionThreshold))
            {
                Debug.Log($"ðŸš« DUPLICATE BLOCKED: {signature} from {source} (globally processed)");
                return false;
            }
        }
        
        // Not a duplicate - add it
        targetList.Add(objectData);
        processedSignatures.Add(signature);
        Debug.Log($"âœ… ADDED UNIQUE: {signature} from {source}");
        return true;
    }
    
    #endregion
    
    bool IsRealWorldObject(string className)
    {
        if (!filterToRealWorldObjects)
            return true;
            
        if (technicalObjectTypes.Contains(className))
            return false;
            
        if (realWorldObjectTypes.Contains(className))
            return true;
            
        return false;
    }
    
    [ContextMenu("Capture Scene Analysis")]
    public void CaptureSceneAnalysis()
    {
        Debug.Log("ðŸ” Starting route-focused scene analysis with duplicate prevention...");
        
        // Initialize duplicate detection
        InitializeDuplicateDetection();
        
        // Initialize session paths
        InitializeSessionPaths();
        
        // Capture all scene data
        SceneAnalysisData sceneData = new SceneAnalysisData();
        
        // 1. Basic player reference position
        CaptureBasicPlayerData(sceneData);
        
        // 2. Route information
        CaptureRouteData(sceneData);
        
        // 3. Route-focused object analysis
        if (enableRouteBasedFiltering && routeGuideSystem != null)
        {
            CaptureRouteBasedAnalysisWithDuplicateCheck(sceneData);
        }
        else
        {
            Debug.LogWarning("âš ï¸ No route system found - falling back to full scene analysis");
            CaptureStaticEnvironmentWithDuplicateCheck(sceneData);
        }
        
        // 4. Dynamic/moving objects
        if (includeMovingObjects)
            CaptureDynamicObjects(sceneData);
        
        // 5. Spatial relationships
        CaptureSpatialRelationships(sceneData);
        
        // 6. Scene screenshots
        CaptureSceneScreenshots();
        
        // Save all data
        SaveSceneAnalysis(sceneData);
        
        // Report duplicate detection results
        ReportDuplicateDetectionResults();
        
        Debug.Log($"âœ… Route-focused scene analysis complete. Data saved to: {sceneAnalysisPath}");
    }
    
    void CaptureRouteBasedAnalysisWithDuplicateCheck(SceneAnalysisData sceneData)
    {
        List<Vector3> routePoints = routeGuideSystem.GetRoutePoints();
        
        if (routePoints.Count < 2)
        {
            Debug.LogWarning("Route has less than 2 points, using normal analysis");
            CaptureStaticEnvironmentWithDuplicateCheck(sceneData);
            return;
        }
        
        List<StaticObjectData> routeRelevantObjects = new List<StaticObjectData>();
        int totalFoundBeforeFilter = 0;
        int duplicatesBlocked = 0;
        
        Debug.Log("ðŸ” Method 1: Analyzing DetectableObjects near route...");
        
        // Method 1: DetectableObjects near route
        DetectableObject[] detectableObjects = FindObjectsOfType<DetectableObject>();
        foreach (DetectableObject obj in detectableObjects)
        {
            float distanceToRoute = CalculateDistanceToRoute(obj.transform.position, routePoints);
            
            if (distanceToRoute <= routeAnalysisWidth)
            {
                totalFoundBeforeFilter++;
                
                if (IsRealWorldObject(obj.className))
                {
                    StaticObjectData objectData = CreateStaticObjectData(obj.gameObject, obj.className, routePoints);
                    
                    if (TryAddUniqueObject(objectData, routeRelevantObjects, "DetectableObjects"))
                    {
                        // Successfully added
                    }
                    else
                    {
                        duplicatesBlocked++;
                    }
                }
            }
        }
        
        Debug.Log("ðŸ” Method 2: Analyzing Building hierarchy near route...");
        
        // Method 2: Building hierarchy near route
        foreach (string parentName in buildingParentNames)
        {
            GameObject parent = GameObject.Find(parentName);
            if (parent != null)
            {
                AnalyzeBuildingHierarchyNearRouteWithDuplicateCheck(parent.transform, routeRelevantObjects, routePoints, ref totalFoundBeforeFilter, ref duplicatesBlocked);
            }
        }
        
        Debug.Log("ðŸ” Method 3: Analyzing Obstacle hierarchy near route...");
        
        // Method 3: Obstacle hierarchy near route
        foreach (string parentName in obstacleParentNames)
        {
            GameObject parent = GameObject.Find(parentName);
            if (parent != null)
            {
                AnalyzeObstacleHierarchyNearRouteWithDuplicateCheck(parent.transform, routeRelevantObjects, routePoints, ref totalFoundBeforeFilter, ref duplicatesBlocked);
            }
        }
        
        routeRelevantObjects = routeRelevantObjects.OrderBy(obj => obj.distanceFromSpawn).ToList();
        sceneData.staticObjects = routeRelevantObjects;
        
        int filteredOut = totalFoundBeforeFilter - routeRelevantObjects.Count - duplicatesBlocked;
        
        Debug.Log($"ðŸ›£ï¸ Route-focused analysis complete:");
        Debug.Log($"   ðŸ“Š {routeRelevantObjects.Count} unique objects within {routeAnalysisWidth}m of route");
        Debug.Log($"   ðŸš« {duplicatesBlocked} duplicates blocked");
        Debug.Log($"   ðŸ”§ {filteredOut} non-real-world objects filtered");
    }
    
    StaticObjectData CreateStaticObjectData(GameObject obj, string className, List<Vector3> routePoints)
    {
        StaticObjectData objectData = new StaticObjectData
        {
            objectName = obj.name,
            className = className,
            position = obj.transform.position,
            rotation = obj.transform.rotation.eulerAngles,
            distanceFromSpawn = Vector3.Distance(routePoints[0], obj.transform.position),
            angleFromSpawn = CalculateAngleFromRouteStart(obj.transform.position, routePoints[0], routePoints[1]),
            distanceToRoute = CalculateDistanceToRoute(obj.transform.position, routePoints),
            routeSegmentIndex = GetNearestRouteSegment(obj.transform.position, routePoints),
            isOnObstacleLayer = (obj.layer == LayerMask.NameToLayer("Obstacles")),
            hasCollider = obj.GetComponent<Collider>() != null
        };
        
        // Calculate bounds
        DetectableObject detectableComponent = obj.GetComponent<DetectableObject>();
        if (detectableComponent != null)
        {
            objectData.bounds = detectableComponent.worldBounds;
        }
        else
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
                objectData.bounds = renderer.bounds;
        }
        
        return objectData;
    }
    
    void AnalyzeBuildingHierarchyNearRouteWithDuplicateCheck(Transform parent, List<StaticObjectData> staticObjects, List<Vector3> routePoints, ref int totalCount, ref int duplicatesBlocked)
    {
        // Recursively analyze building hierarchy with duplicate checking
        foreach (Transform child in parent)
        {
            float distanceToRoute = CalculateDistanceToRoute(child.position, routePoints);
            
            if (distanceToRoute <= routeAnalysisWidth)
            {
                totalCount++;
                
                // Try to get DetectableObject, otherwise classify by name
                DetectableObject detectableComponent = child.GetComponent<DetectableObject>();
                string className = detectableComponent?.className ?? "Building";
                
                if (IsRealWorldObject(className))
                {
                    StaticObjectData objectData = CreateStaticObjectData(child.gameObject, className, routePoints);
                    objectData.parentCategory = parent.name;
                    
                    if (!TryAddUniqueObject(objectData, staticObjects, $"Building-{parent.name}"))
                    {
                        duplicatesBlocked++;
                    }
                }
            }
            
            // Recurse into children
            if (child.childCount > 0)
            {
                AnalyzeBuildingHierarchyNearRouteWithDuplicateCheck(child, staticObjects, routePoints, ref totalCount, ref duplicatesBlocked);
            }
        }
    }
    
    void AnalyzeObstacleHierarchyNearRouteWithDuplicateCheck(Transform parent, List<StaticObjectData> staticObjects, List<Vector3> routePoints, ref int totalCount, ref int duplicatesBlocked)
    {
        // Similar to building hierarchy but for obstacles
        foreach (Transform child in parent)
        {
            float distanceToRoute = CalculateDistanceToRoute(child.position, routePoints);
            
            if (distanceToRoute <= routeAnalysisWidth)
            {
                totalCount++;
                
                DetectableObject detectableComponent = child.GetComponent<DetectableObject>();
                string className = detectableComponent?.className ?? ClassifyObjectByName(child.name);
                
                if (IsRealWorldObject(className))
                {
                    StaticObjectData objectData = CreateStaticObjectData(child.gameObject, className, routePoints);
                    objectData.parentCategory = parent.name;
                    
                    if (!TryAddUniqueObject(objectData, staticObjects, $"Obstacle-{parent.name}"))
                    {
                        duplicatesBlocked++;
                    }
                }
            }
            
            // Recurse into children
            if (child.childCount > 0)
            {
                AnalyzeObstacleHierarchyNearRouteWithDuplicateCheck(child, staticObjects, routePoints, ref totalCount, ref duplicatesBlocked);
            }
        }
    }
    
    void CaptureStaticEnvironmentWithDuplicateCheck(SceneAnalysisData sceneData)
    {
        // Fallback method for when route analysis isn't available
        List<StaticObjectData> staticObjects = new List<StaticObjectData>();
        int duplicatesBlocked = 0;
        
        DetectableObject[] detectableObjects = FindObjectsOfType<DetectableObject>();
        foreach (DetectableObject obj in detectableObjects)
        {
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(obj.transform.position, playerTransform.position);
                if (distance <= analysisRadius)
                {
                    if (IsRealWorldObject(obj.className))
                    {
                        StaticObjectData objectData = new StaticObjectData
                        {
                            objectName = obj.gameObject.name,
                            className = obj.className,
                            position = obj.transform.position,
                            rotation = obj.transform.rotation.eulerAngles,
                            bounds = obj.worldBounds,
                            distanceFromSpawn = distance,
                            hasCollider = obj.GetComponent<Collider>() != null
                        };
                        
                        if (!TryAddUniqueObject(objectData, staticObjects, "StaticEnvironment"))
                        {
                            duplicatesBlocked++;
                        }
                    }
                }
            }
        }
        
        sceneData.staticObjects = staticObjects;
        Debug.Log($"ðŸ“Š Static environment captured: {staticObjects.Count} unique objects within {analysisRadius}m");
        Debug.Log($"ðŸš« Blocked {duplicatesBlocked} duplicates");
    }
    
    void ReportDuplicateDetectionResults()
    {
        Debug.Log($"ðŸ” DUPLICATE DETECTION SUMMARY:");
        Debug.Log($"   ðŸ“Š Total unique signatures processed: {processedSignatures.Count}");
        Debug.Log($"   ðŸŽ¯ Position threshold: {duplicatePositionThreshold}m");
        
        // Group by class for detailed breakdown
        var classCounts = processedSignatures.GroupBy(s => s.className).OrderByDescending(g => g.Count());
        Debug.Log($"   ðŸ“‹ Object breakdown:");
        foreach (var group in classCounts)
        {
            Debug.Log($"      â€¢ {group.Key}: {group.Count()} objects");
        }
    }
    
    // Helper methods for route calculations
    float CalculateDistanceToRoute(Vector3 objectPosition, List<Vector3> routePoints)
    {
        if (routePoints.Count < 2) return float.MaxValue;
        
        float minDistance = float.MaxValue;
        
        // Check distance to each route segment
        for (int i = 0; i < routePoints.Count - 1; i++)
        {
            Vector3 segmentStart = routePoints[i];
            Vector3 segmentEnd = routePoints[i + 1];
            
            // Calculate closest point on segment
            Vector3 segmentDirection = (segmentEnd - segmentStart).normalized;
            Vector3 toObject = objectPosition - segmentStart;
            
            float projectionLength = Vector3.Dot(toObject, segmentDirection);
            projectionLength = Mathf.Clamp(projectionLength, 0f, Vector3.Distance(segmentStart, segmentEnd));
            
            Vector3 closestPoint = segmentStart + segmentDirection * projectionLength;
            float distance = Vector3.Distance(objectPosition, closestPoint);
            
            if (distance < minDistance)
                minDistance = distance;
        }
        
        return minDistance;
    }
    
    float CalculateAngleFromRouteStart(Vector3 objectPosition, Vector3 routeStart, Vector3 routeDirection)
    {
        Vector3 directionToObject = (objectPosition - routeStart).normalized;
        Vector3 routeForward = (routeDirection - routeStart).normalized;
        
        return Vector3.SignedAngle(routeForward, directionToObject, Vector3.up);
    }
    
    int GetNearestRouteSegment(Vector3 objectPosition, List<Vector3> routePoints)
    {
        float minDistance = float.MaxValue;
        int nearestSegment = 0;
        
        for (int i = 0; i < routePoints.Count - 1; i++)
        {
            Vector3 segmentCenter = Vector3.Lerp(routePoints[i], routePoints[i + 1], 0.5f);
            float distance = Vector3.Distance(objectPosition, segmentCenter);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestSegment = i;
            }
        }
        
        return nearestSegment;
    }
    
    string ClassifyObjectByName(string objectName)
    {
        string name = objectName.ToLower();
        
        if (name.Contains("car") || name.Contains("vehicle"))
            return "Car";
        else if (name.Contains("bus"))
            return "Bus";
        else if (name.Contains("bicycle") || name.Contains("bike"))
            return "Bicycle";
        else if (name.Contains("tree"))
            return "Tree";
        else if (name.Contains("building"))
            return "Building";
        else if (name.Contains("wall"))
            return "Wall";
        else if (name.Contains("pole"))
            return "Pole";
        else
            return CleanObjectName(objectName);
    }
    
    string CleanObjectName(string objectName)
    {
        string cleaned = objectName.Replace("(Clone)", "").Replace("_", " ").Trim();
        
        if (!string.IsNullOrEmpty(cleaned))
        {
            string[] words = cleaned.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            cleaned = string.Join(" ", words);
        }
        
        return cleaned;
    }
    
    void InitializeSessionPaths()
    {
        if (useSessionManager && SessionManager.Instance != null)
        {
            // Use SessionManager folder structure
            string sessionPath = SessionManager.Instance.GetSessionPath();
            sceneAnalysisPath = Path.Combine(sessionPath, "01_SceneAnalysis");
            sessionID = SessionManager.Instance.GetCurrentSession().userID + "_SceneAnalysis";
            
            Debug.Log($"ðŸ“ Using SessionManager path: {sceneAnalysisPath}");
        }
        else
        {
            // Fallback to old structure
            sessionID = "SceneAnalysis_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            sceneAnalysisPath = Path.Combine(Application.persistentDataPath, "SceneAnalysis");
            Directory.CreateDirectory(sceneAnalysisPath);
            
            Debug.Log($"ðŸ“ Using standalone path: {sceneAnalysisPath}");
        }
        
        // Ensure directory exists
        Directory.CreateDirectory(sceneAnalysisPath);
    }
    
    void CaptureBasicPlayerData(SceneAnalysisData sceneData)
    {
        if (playerTransform == null) return;
        
        sceneData.playerSpawnPosition = playerTransform.position;
        sceneData.playerSpawnRotation = playerTransform.rotation.eulerAngles;
        
        Debug.Log($"ðŸ‘¤ Player reference position: {sceneData.playerSpawnPosition}");
    }
    
    void CaptureRouteData(SceneAnalysisData sceneData)
    {
        if (routeGuideSystem == null) return;
        
        List<Vector3> routePoints = routeGuideSystem.GetRoutePoints();
        
        if (routePoints.Count >= 2)
        {
            sceneData.routeStartPoint = routePoints[0];
            sceneData.routeEndPoint = routePoints[routePoints.Count - 1];
            sceneData.routeTotalDistance = routeGuideSystem.GetRouteDistance();
            sceneData.routeWaypoints = routePoints;
            
            Debug.Log($"ðŸ›£ï¸ Route data captured: {sceneData.routeTotalDistance:F1}m with {routePoints.Count} waypoints");
        }
        else
        {
            Debug.LogWarning("âš ï¸ No valid route found for analysis");
        }
    }
    
    void CaptureDynamicObjects(SceneAnalysisData sceneData)
    {
        sceneData.dynamicObjects = new List<DynamicObjectData>();
        
        Debug.Log("ðŸš— Starting dynamic object capture...");
        
        // Get reference to dynamic object manager
        DynamicObjectManager dynamicManager = FindObjectOfType<DynamicObjectManager>();
        List<GameObject> dynamicObjects = new List<GameObject>();
        
        if (dynamicManager != null)
        {
            // Use DynamicObjectManager's tracked objects
            dynamicObjects = dynamicManager.GetAllDynamicObjects();
            Debug.Log($"ðŸ“Š Found {dynamicObjects.Count} dynamic objects via DynamicObjectManager");
        }
        else
        {
            Debug.LogWarning("âš ï¸ No DynamicObjectManager found - falling back to manual detection");
            // Fallback: manual detection
            dynamicObjects = FindDynamicObjectsManually();
        }
        
        // Process each dynamic object
        List<Vector3> routePoints = routeGuideSystem?.GetRoutePoints() ?? new List<Vector3>();
        
        foreach (GameObject obj in dynamicObjects)
        {
            if (obj == null) continue;
            
            // Skip player object
            if (obj.GetComponent<FCG.CharacterControl>() != null) continue;
            
            // Check if object is near route (if route filtering is enabled)
            bool isNearRoute = true;
            if (enableRouteBasedFiltering && routePoints.Count >= 2)
            {
                float distanceToRoute = CalculateDistanceToRoute(obj.transform.position, routePoints);
                isNearRoute = distanceToRoute <= routeAnalysisWidth;
            }
            
            if (isNearRoute)
            {
                DynamicObjectData dynamicData = AnalyzeDynamicObject(obj);
                if (dynamicData != null && IsRealWorldObject(dynamicData.className))
                {
                    sceneData.dynamicObjects.Add(dynamicData);
                }
            }
        }
        
        Debug.Log($"ðŸš— Dynamic objects captured: {sceneData.dynamicObjects.Count}");
        
        // Log breakdown by type
        var typeGroups = sceneData.dynamicObjects.GroupBy(obj => obj.className);
        foreach (var group in typeGroups)
        {
            Debug.Log($"  â€¢ {group.Key}: {group.Count()} objects");
        }
    }
    
    List<GameObject> FindDynamicObjectsManually()
    {
        List<GameObject> dynamicObjects = new List<GameObject>();
        
        // Method 1: Find objects with Rigidbodies
        Rigidbody[] rigidbodies = FindObjectsOfType<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            if (!dynamicObjects.Contains(rb.gameObject))
                dynamicObjects.Add(rb.gameObject);
        }
        
        // Method 2: Find objects with NavMeshAgent
        UnityEngine.AI.NavMeshAgent[] agents = FindObjectsOfType<UnityEngine.AI.NavMeshAgent>();
        foreach (UnityEngine.AI.NavMeshAgent agent in agents)
        {
            if (!dynamicObjects.Contains(agent.gameObject))
                dynamicObjects.Add(agent.gameObject);
        }
        
        // Method 3: Find objects by tags
        foreach (string tag in movingObjectTags)
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in taggedObjects)
            {
                if (!dynamicObjects.Contains(obj))
                    dynamicObjects.Add(obj);
            }
        }
        
        Debug.Log($"ðŸ” Manual detection found {dynamicObjects.Count} potential dynamic objects");
        return dynamicObjects;
    }
    
    DynamicObjectData AnalyzeDynamicObject(GameObject obj)
    {
        // Get DetectableObject component for classification
        DetectableObject detectableComponent = obj.GetComponent<DetectableObject>();
        string className = detectableComponent?.className ?? ClassifyObjectByName(obj.name);
        
        // Analyze movement components
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        UnityEngine.AI.NavMeshAgent agent = obj.GetComponent<UnityEngine.AI.NavMeshAgent>();
        
        // Determine movement pattern
        string movementPattern = "Static";
        Vector3 currentVelocity = Vector3.zero;
        float mass = 0f;
        bool isMoving = false;
        
        if (rb != null)
        {
            currentVelocity = rb.linearVelocity;
            mass = rb.mass;
            
            if (currentVelocity.magnitude > 0.1f)
            {
                isMoving = true;
                if (currentVelocity.magnitude > 5f)
                    movementPattern = "Fast Moving";
                else
                    movementPattern = "Moving";
            }
        }
        
        if (agent != null && agent.enabled)
        {
            currentVelocity = agent.velocity;
            isMoving = currentVelocity.magnitude > 0.1f;
            
            if (isMoving)
            {
                movementPattern = agent.hasPath ? "Following Path" : "Wandering";
            }
        }
        
        // Check for custom movement scripts
        MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            string scriptName = script.GetType().Name.ToLower();
            if (scriptName.Contains("move") || scriptName.Contains("patrol") || 
                scriptName.Contains("drive") || scriptName.Contains("ai"))
            {
                movementPattern = "Scripted Movement";
                isMoving = true;
                break;
            }
        }
        
        DynamicObjectData dynamicData = new DynamicObjectData
        {
            objectName = obj.name,
            className = className,
            initialPosition = obj.transform.position,
            initialRotation = obj.transform.rotation.eulerAngles,
            currentVelocity = currentVelocity,
            mass = mass,
            isMoving = isMoving,
            movementPattern = movementPattern
        };
        
        // Predict future position (simple linear prediction)
        if (isMoving && currentVelocity.magnitude > 0.1f)
        {
            dynamicData.predictedPath = obj.transform.position + currentVelocity * 5f; // 5 seconds ahead
        }
        else
        {
            dynamicData.predictedPath = obj.transform.position;
        }
        
        // Calculate collision risk (simplified)
        if (playerTransform != null && isMoving)
        {
            float distanceToPlayer = Vector3.Distance(obj.transform.position, playerTransform.position);
            float speed = currentVelocity.magnitude;
            
            if (distanceToPlayer < 20f && speed > 1f)
            {
                dynamicData.collisionRisk = Mathf.Clamp01((20f - distanceToPlayer) / 20f * (speed / 10f));
            }
        }
        
        return dynamicData;
    }
    
    void CaptureSpatialRelationships(SceneAnalysisData sceneData)
    {
        // Initialize spatial analysis
        sceneData.spatialClusters = new List<SpatialCluster>();
        sceneData.navigationCorridors = new List<NavigationCorridor>();
        sceneData.statistics = new SceneStatistics();
        
        if (sceneData.staticObjects == null || sceneData.staticObjects.Count == 0) return;
        
        // Group objects by type for clustering analysis
        var objectsByType = sceneData.staticObjects.GroupBy(obj => obj.className);
        
        foreach (var typeGroup in objectsByType)
        {
            if (typeGroup.Count() >= 2) // Need at least 2 objects to form a cluster
            {
                // Find clusters of same-type objects
                List<StaticObjectData> objects = typeGroup.ToList();
                List<SpatialCluster> typeClusters = FindClusters(objects, 10f); // 10m clustering distance
                
                foreach (var cluster in typeClusters)
                {
                    cluster.dominantObjectType = typeGroup.Key;
                    sceneData.spatialClusters.Add(cluster);
                }
            }
        }
        
        // Update statistics
        sceneData.statistics.totalStaticObjects = sceneData.staticObjects.Count;
        sceneData.statistics.totalDynamicObjects = sceneData.dynamicObjects?.Count ?? 0;
        
        // Object type distribution
        sceneData.statistics.objectTypeDistribution = new Dictionary<string, int>();
        foreach (var group in objectsByType)
        {
            sceneData.statistics.objectTypeDistribution[group.Key] = group.Count();
        }
        
        Debug.Log($"ðŸ”— Spatial relationships captured: {sceneData.spatialClusters.Count} clusters identified");
    }
    
    List<SpatialCluster> FindClusters(List<StaticObjectData> objects, float clusterDistance)
    {
        List<SpatialCluster> clusters = new List<SpatialCluster>();
        List<bool> processed = new List<bool>(new bool[objects.Count]);
        
        for (int i = 0; i < objects.Count; i++)
        {
            if (processed[i]) continue;
            
            SpatialCluster cluster = new SpatialCluster();
            cluster.objects = new List<StaticObjectData>();
            cluster.objects.Add(objects[i]);
            processed[i] = true;
            
            // Find nearby objects of the same type
            for (int j = i + 1; j < objects.Count; j++)
            {
                if (processed[j]) continue;
                
                float distance = Vector3.Distance(objects[i].position, objects[j].position);
                if (distance <= clusterDistance)
                {
                    cluster.objects.Add(objects[j]);
                    processed[j] = true;
                }
            }
            
            // Calculate cluster properties
            if (cluster.objects.Count >= 2)
            {
                Vector3 centerSum = Vector3.zero;
                foreach (var obj in cluster.objects)
                {
                    centerSum += obj.position;
                }
                cluster.centerPoint = centerSum / cluster.objects.Count;
                cluster.objectCount = cluster.objects.Count;
                
                clusters.Add(cluster);
            }
        }
        
        return clusters;
    }
    
    void CaptureSceneScreenshots()
    {
        if (!captureTopDownScreenshot) return;
        
        GameObject tempCameraObj = new GameObject("TempAnalysisCamera");
        Camera tempCamera = tempCameraObj.AddComponent<Camera>();
        
        Vector3 sceneCenter = CalculateSceneCenter();
        tempCamera.transform.position = sceneCenter + Vector3.up * 50f;
        tempCamera.transform.LookAt(sceneCenter);
        tempCamera.orthographic = true;
        tempCamera.orthographicSize = analysisRadius * 0.7f;
        
        RenderTexture renderTexture = new RenderTexture(1024, 1024, 24);
        tempCamera.targetTexture = renderTexture;
        tempCamera.Render();
        
        RenderTexture.active = renderTexture;
        Texture2D screenshot = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
        screenshot.Apply();
        RenderTexture.active = null;
        
        byte[] bytes = screenshot.EncodeToPNG();
        string screenshotPath = Path.Combine(sceneAnalysisPath, "scene_overview.png");
        File.WriteAllBytes(screenshotPath, bytes);
        
        DestroyImmediate(tempCameraObj);
        DestroyImmediate(screenshot);
        renderTexture.Release();
        
        Debug.Log($"ðŸ“¸ Scene overview screenshot saved");
    }
    
    Vector3 CalculateSceneCenter()
    {
        if (routeGuideSystem != null)
        {
            List<Vector3> routePoints = routeGuideSystem.GetRoutePoints();
            if (routePoints.Count >= 2)
            {
                return Vector3.Lerp(routePoints[0], routePoints[routePoints.Count - 1], 0.5f);
            }
        }
        
        return playerTransform?.position ?? Vector3.zero;
    }
    
    void SaveSceneAnalysis(SceneAnalysisData sceneData)
    {
        // Update metadata
        sceneData.sceneAnalysisID = sessionID;
        sceneData.timestamp = Time.time;
        sceneData.analysisRadius = routeAnalysisWidth;
        
        // Save as JSON
        string jsonPath = Path.Combine(sceneAnalysisPath, "scene_analysis.json");
        string jsonData = JsonUtility.ToJson(sceneData, true);
        File.WriteAllText(jsonPath, jsonData);
        
        // Create human-readable summary
        string summaryPath = Path.Combine(sceneAnalysisPath, "scene_summary.txt");
        string summary = GenerateHumanReadableSummary(sceneData);
        File.WriteAllText(summaryPath, summary);
        
        Debug.Log($"ðŸ’¾ Scene analysis saved: {sceneData.staticObjects?.Count ?? 0} static objects, {sceneData.dynamicObjects?.Count ?? 0} dynamic objects");
        
        // Update session data if using SessionManager
        if (useSessionManager && SessionManager.Instance != null)
        {
            UpdateSessionWithSceneData(sceneData);
        }
    }
    
    void UpdateSessionWithSceneData(SceneAnalysisData sceneData)
    {
        UserSession session = SessionManager.Instance.GetCurrentSession();
        
        // You could add scene analysis summary to session data here
        // For now, just log that it's complete
        Debug.Log($"ðŸ“‹ Scene analysis data linked to session: {session.userID}");
        
        SessionManager.Instance.SaveSessionData();
    }
    
    string GenerateHumanReadableSummary(SceneAnalysisData sceneData)
    {
        StringBuilder summary = new StringBuilder();
        summary.AppendLine("ROUTE-FOCUSED SCENE ANALYSIS SUMMARY");
        summary.AppendLine($"Generated: {System.DateTime.Now}");
        summary.AppendLine($"Session ID: {sessionID}");
        summary.AppendLine($"Analysis Type: ROUTE-BASED");
        summary.AppendLine($"Real-World Filtering: {(filterToRealWorldObjects ? "ENABLED" : "DISABLED")}");
        summary.AppendLine($"Route Analysis Width: {routeAnalysisWidth}m");
        summary.AppendLine($"Duplicate Prevention: ENABLED (threshold: {duplicatePositionThreshold}m)");
        summary.AppendLine();
        
        // Route information
        if (sceneData.routeWaypoints != null && sceneData.routeWaypoints.Count > 0)
        {
            summary.AppendLine($"NAVIGATION ROUTE:");
            summary.AppendLine($"Start: {sceneData.routeStartPoint}");
            summary.AppendLine($"End: {sceneData.routeEndPoint}");
            summary.AppendLine($"Distance: {sceneData.routeTotalDistance:F1}m");
            summary.AppendLine($"Waypoints: {sceneData.routeWaypoints.Count}");
            summary.AppendLine();
        }
        
        summary.AppendLine($"PLAYER REFERENCE:");
        summary.AppendLine($"Position: {sceneData.playerSpawnPosition}");
        summary.AppendLine($"Rotation: {sceneData.playerSpawnRotation}");
        
        // Add nearby real-world objects info
        if (sceneData.staticObjects != null)
        {
            var closeObjects = sceneData.staticObjects.Where(obj => obj.distanceFromSpawn < 15f).ToList();
            if (closeObjects.Count > 0)
            {
                summary.AppendLine($"Nearby real-world objects: {string.Join(", ", closeObjects.Select(obj => $"{obj.className} at {obj.distanceFromSpawn:F1}m"))}");
            }
        }
        summary.AppendLine();
        
        if (sceneData.staticObjects != null)
        {
            summary.AppendLine($"REAL-WORLD STATIC OBJECTS: {sceneData.staticObjects.Count} total");
            var grouped = sceneData.staticObjects.GroupBy(obj => obj.className);
            foreach (var group in grouped.OrderByDescending(g => g.Count()))
            {
                summary.AppendLine($"- {group.Key}: {group.Count()} objects");
            }
            
            // Add route proximity analysis
            var closeToRoute = sceneData.staticObjects.Where(obj => obj.distanceToRoute < 3f).Count();
            var farFromRoute = sceneData.staticObjects.Where(obj => obj.distanceToRoute > 5f).Count();
            summary.AppendLine($"Close to route (<3m): {closeToRoute} objects");
            summary.AppendLine($"Far from route (>5m): {farFromRoute} objects");
            summary.AppendLine();
        }
        
        if (sceneData.dynamicObjects != null && sceneData.dynamicObjects.Count > 0)
        {
            summary.AppendLine($"REAL-WORLD DYNAMIC OBJECTS: {sceneData.dynamicObjects.Count} total");
            foreach (DynamicObjectData obj in sceneData.dynamicObjects)
            {
                summary.AppendLine($"- {obj.className} ({obj.objectName}): {obj.movementPattern}");
            }
            summary.AppendLine();
        }
        
        if (sceneData.spatialClusters != null && sceneData.spatialClusters.Count > 0)
        {
            summary.AppendLine($"SPATIAL CLUSTERS: {sceneData.spatialClusters.Count} identified");
            foreach (SpatialCluster cluster in sceneData.spatialClusters)
            {
                summary.AppendLine($"- {cluster.dominantObjectType} cluster: {cluster.objectCount} objects at {cluster.centerPoint}");
            }
            summary.AppendLine();
        }
        
        return summary.ToString();
    }
    
    #region Context Menu Debug Methods
    
    [ContextMenu("Debug: Count Objects by Type")]
    public void DebugCountObjectsByType()
    {
        Debug.Log("ðŸ” OBJECT COUNTING DEBUG:");
        
        // Count DetectableObjects
        DetectableObject[] allDetectable = FindObjectsOfType<DetectableObject>();
        var detectableByClass = allDetectable.GroupBy(obj => obj.className);
        
        Debug.Log($"ðŸ“Š DetectableObject components found: {allDetectable.Length}");
        foreach (var group in detectableByClass.OrderByDescending(g => g.Count()))
        {
            Debug.Log($"   â€¢ {group.Key}: {group.Count()} objects");
            
            if (group.Key == "Bicycle")
            {
                Debug.Log($"     ðŸ“ Bicycle positions:");
                foreach (var bike in group)
                {
                    Debug.Log($"        - {bike.name} at {bike.transform.position} (Parent: {bike.transform.parent?.name ?? "None"})");
                }
            }
        }
        
        // Count GameObjects by name pattern
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        var bicycleObjects = allObjects.Where(obj => obj.name.ToLower().Contains("bicycle")).ToList();
        Debug.Log($"ðŸš² GameObjects with 'bicycle' in name: {bicycleObjects.Count}");
        foreach (var bike in bicycleObjects)
        {
            Debug.Log($"   - {bike.name} at {bike.transform.position}");
        }
    }
    
    [ContextMenu("Debug: Test Duplicate Detection")]
    public void DebugTestDuplicateDetection()
    {
        Debug.Log("ðŸ§ª Testing duplicate detection logic...");
        
        InitializeDuplicateDetection();
        
        // Find all bicycles for testing
        DetectableObject[] allDetectable = FindObjectsOfType<DetectableObject>();
        var bicycles = allDetectable.Where(obj => obj.className == "Bicycle").ToList();
        
        Debug.Log($"ðŸš² Found {bicycles.Count} bicycle DetectableObjects");
        
        List<StaticObjectData> testList = new List<StaticObjectData>();
        
        foreach (var bike in bicycles)
        {
            StaticObjectData objectData = new StaticObjectData
            {
                objectName = bike.name,
                className = bike.className,
                position = bike.transform.position
            };
            
            bool added = TryAddUniqueObject(objectData, testList, "Test");
            Debug.Log($"   ðŸš² {bike.name} at {bike.transform.position}: {(added ? "ADDED" : "BLOCKED as duplicate")}");
        }
        
        Debug.Log($"ðŸŽ¯ Final unique bicycle count: {testList.Count}");
    }
    
    [ContextMenu("Debug: Set Duplicate Threshold")]
    public void DebugSetDuplicateThreshold()
    {
        duplicatePositionThreshold = 0.5f; // Increase for testing
        Debug.Log($"ðŸŽ¯ Duplicate position threshold set to: {duplicatePositionThreshold}m");
    }
    
    [ContextMenu("Debug: Test Session Integration")]
    public void DebugTestSessionIntegration()
    {
        if (SessionManager.Instance != null)
        {
            string sessionPath = SessionManager.Instance.GetSessionPath();
            string trialPath = SessionManager.Instance.GetTrialDataPath("baseline");
            
            Debug.Log($"âœ… SessionManager found");
            Debug.Log($"ðŸ“ Session path: {sessionPath}");
            Debug.Log($"ðŸ“ Trial path: {trialPath}");
            Debug.Log($"ðŸŽ¯ Current trial: {SessionManager.Instance.GetCurrentTrial()}");
        }
        else
        {
            Debug.LogError("âŒ SessionManager not found!");
        }
    }
    
    [ContextMenu("Debug: Test Dynamic Object Detection")]
    public void DebugTestDynamicObjectDetection()
    {
        Debug.Log("ðŸ” Testing dynamic object detection...");
        
        DynamicObjectManager dynamicManager = FindObjectOfType<DynamicObjectManager>();
        if (dynamicManager != null)
        {
            List<GameObject> managedObjects = dynamicManager.GetAllDynamicObjects();
            Debug.Log($"ðŸ“Š DynamicObjectManager tracking: {managedObjects.Count} objects");
            
            foreach (GameObject obj in managedObjects.Take(10)) // Show first 10
            {
                string className = obj.GetComponent<DetectableObject>()?.className ?? ClassifyObjectByName(obj.name);
                Debug.Log($"  â€¢ {obj.name}: {className}");
            }
        }
        else
        {
            Debug.LogWarning("âš ï¸ No DynamicObjectManager found");
        }
        
        // Test manual detection
        List<GameObject> manualObjects = FindDynamicObjectsManually();
        Debug.Log($"ðŸ”§ Manual detection found: {manualObjects.Count} objects");
    }
    
    #endregion
}
