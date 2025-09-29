using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data structures for comprehensive scene analysis before user navigation
/// </summary>

[System.Serializable]
public class SceneAnalysisData
{
    [Header("Player Reference Information")]
    public Vector3 playerSpawnPosition;
    public Vector3 playerSpawnRotation;
    
    [Header("Route Information")]
    public Vector3 routeStartPoint;
    public Vector3 routeEndPoint;
    public float routeTotalDistance;
    public List<Vector3> routeWaypoints = new List<Vector3>();
    
    [Header("Environment Objects")]
    public List<StaticObjectData> staticObjects = new List<StaticObjectData>();
    public List<DynamicObjectData> dynamicObjects = new List<DynamicObjectData>();
    
    [Header("Spatial Analysis")]
    public List<SpatialCluster> spatialClusters = new List<SpatialCluster>();
    public List<NavigationCorridor> navigationCorridors = new List<NavigationCorridor>();
    public SceneStatistics statistics = new SceneStatistics();
    
    [Header("Metadata")]
    public string sceneAnalysisID;
    public float timestamp;
    public float analysisRadius;
}

[System.Serializable]
public class StaticObjectData
{
    public string objectName;
    public string className;
    public string parentCategory = ""; // e.g., "Buildings", "Obstacles"
    
    [Header("Spatial Information")]
    public Vector3 position;
    public Vector3 rotation;
    public Bounds bounds;
    
    [Header("Relative to Route Start")]
    public float distanceFromSpawn; // Distance from route start point
    public float angleFromSpawn; // -180 to +180 degrees from route start direction
    
    [Header("Route Relationship")]
    public float distanceToRoute = 0f;           // Distance from object to nearest route point
    public int routeSegmentIndex = -1;           // Which route segment this object is closest to
    
    [Header("Additional Properties")]
    public bool hasCollider = false;
    public bool isOnObstacleLayer = false;
    public float estimatedNavigationImpact = 0f; // 0-1 scale of how much it affects navigation
}

[System.Serializable]
public class DynamicObjectData
{
    public string objectName;
    public string className;
    
    [Header("Movement Information")]
    public Vector3 initialPosition;
    public Vector3 initialRotation;
    public Vector3 currentVelocity;
    public float mass;
    public bool isMoving;
    public string movementPattern; // "Static", "Moving", "Fast Moving", "Circular", etc.
    
    [Header("Predictive Data")]
    public Vector3 predictedPath; // Where it's likely to be in 5 seconds
    public float collisionRisk = 0f; // 0-1 probability of player collision
    public List<Vector3> waypoints = new List<Vector3>(); // If following a path
}

[System.Serializable]
public class SpatialCluster
{
    public Vector3 centerPoint;
    public int objectCount;
    public string dominantObjectType;
    public List<StaticObjectData> objects = new List<StaticObjectData>();
    
    [Header("Cluster Properties")]
    public float density; // Objects per square meter
    public float averageObjectSize;
    public bool isNavigationBarrier = false;
    public List<string> accessibleDirections = new List<string>(); // "North", "South", etc.
}

[System.Serializable]
public class NavigationCorridor
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    public float width;
    public Vector3 direction;
    
    [Header("Corridor Properties")]
    public bool isClear = true;
    public List<StaticObjectData> borderingObjects = new List<StaticObjectData>();
    public float difficultyScore = 0f; // 0-1, higher = more difficult to navigate
    public string corridorType = "Open"; // "Open", "Narrow", "Cluttered", "Blocked"
}

[System.Serializable]
public class SceneStatistics
{
    [Header("Object Counts")]
    public int totalStaticObjects = 0;
    public int totalDynamicObjects = 0;
    public Dictionary<string, int> objectTypeDistribution = new Dictionary<string, int>();
    
    [Header("Spatial Metrics")]
    public float sceneComplexity = 0f; // 0-1 complexity score
    public float openSpacePercentage = 0f; // Percentage of navigable space
    public float obstaclesDensity = 0f; // Objects per square meter
    
    [Header("Navigation Challenges")]
    public List<string> identifiedChallenges = new List<string>();
    public float estimatedDifficultyLevel = 0f; // 0-1, overall scene difficulty
    public List<Vector3> potentialCollisionZones = new List<Vector3>();
}
