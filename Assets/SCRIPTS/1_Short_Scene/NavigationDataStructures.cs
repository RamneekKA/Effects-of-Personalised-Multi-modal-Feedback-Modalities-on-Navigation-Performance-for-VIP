using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Updated data structures for the full 5-trial pipeline
/// CLEANED: Removed old AppliedEnhancements system
/// Now uses specialized controllers for enhancements
/// </summary>

[System.Serializable]
public class UserSession
{
    [Header("Session Information")]
    public string userID;
    public string sessionDateTime;
    public string sessionPath; // Full path to user's session folder
    
    [Header("Trial Progress")]
    public List<string> completedTrials = new List<string>();
    public string currentTrial = "baseline";
    public float totalSessionTime = 0f;
    
    [Header("Trial Results")]
    public BaselineResults baselineResults;
    public AlgorithmicAssessmentResults algorithmicResults;
    public LLMAssessmentResults llmResults;
    public EnhancedNavigationResults shortLLMResults;
    public EnhancedNavigationResults shortAlgorithmicResults;
    public EnhancedNavigationResults longLLMResults;
    public EnhancedNavigationResults longAlgorithmicResults;
    
    [Header("Metadata")]
    public string notes = "";
    public bool sessionCompleted = false;
}

[System.Serializable]
public class NavigationDataPoint
{
    public float timestamp;
    public Vector3 position;
    public Vector3 rotation;
    public float currentSpeed;
    public List<NearbyObject> nearbyObjects = new List<NearbyObject>();
    public string screenshotPath = "";
    public bool isCollision = false;
    public string collisionObject = "";
    public Vector3 collisionPoint;
    
    // Enhanced collision analysis
    public string bodyPartInvolved = "";
    public float collisionVelocity = 0f;
    public Vector3 approachDirection = Vector3.zero;
    public float timeNearObjectBeforeCollision = 0f;
    
    // Route deviation tracking
    public float signedDeviationFromRoute = 0f;
}

[System.Serializable]
public class NearbyObject
{
    public string className;
    public string objectName;
    public float distance;
    public float angle;
    public Vector3 worldPosition;
    public bool wasDetectedPreviousFrame = false;
    public float timeFirstDetected = 0f;
}

[System.Serializable]
public class NavigationSession
{
    [Header("Session Identity")]
    public string sessionID;
    public string trialType; // "baseline", "short_llm", "short_algorithmic", "long_llm", "long_algorithmic"
    public string routeType; // "short", "long"
    
    [Header("Timing")]
    public float startTime;
    public float endTime;
    public float duration;
    
    [Header("Navigation Data")]
    public int totalDataPoints;
    public List<NavigationDataPoint> dataPoints = new List<NavigationDataPoint>();
    
    [Header("Performance Metrics")]
    public int totalCollisions = 0;
    public Dictionary<string, int> collisionsByBodyPart = new Dictionary<string, int>();
    public Dictionary<string, int> collisionsByObjectType = new Dictionary<string, int>();
    
    [Header("Route Performance")]
    public float averageAbsoluteDeviation = 0f;
    public float averageSignedDeviation = 0f;
    public float maximumDeviation = 0f;
    public float minimumDeviation = 0f;
    public float timeSpentOffRoute = 0f;
    public float routeCompletionPercentage = 0f;
    public float averageSpeed = 0f;
}

[System.Serializable]
public class BaselineResults
{
    public NavigationSession shortDistanceSession;
    public string geminiAnalysisPath;
    public float completionTime;
    public bool completed = false;
}

[System.Serializable]
public class AlgorithmicAssessmentResults
{
    [Header("Vision Ratings (1-10 scale)")]
    public int centralVisionRating;
    public int leftPeripheralRating;
    public int rightPeripheralRating;
    
    [Header("Distance Questions")]
    public float objectClarityDistance; // Distance where objects become unclear (1-10m)
    public float reliableAvoidanceDistance; // Distance needed for reliable avoidance (0.5-5m)
    
    [Header("Color & Light")]
    public bool isColorBlind;
    public List<string> colorBlindnessTypes = new List<string>();
    public int lightSensitivityRating; // 1-10 scale
    public int lowLightDifficultyRating; // 1-10 scale
    
    [Header("Output Modality Preference")]
    public string preferredModalityType; // "Audio", "Haptics", or "Visual"
    
    [Header("Assessment Metadata")]
    public float assessmentDuration;
    public string assessmentDateTime;
    public bool completed = false;
}

[System.Serializable]
public class LLMAssessmentResults
{
    [Header("Conversation Data")]
    public List<ChatMessage> conversationLog = new List<ChatMessage>();
    public List<string> questionsAsked = new List<string>();
    public Dictionary<string, string> extractedResponses = new Dictionary<string, string>();
    
    [Header("Assessment Metadata")]
    public float conversationDuration;
    public string conversationDateTime;
    public int totalQuestions;
    public bool completed = false;
    
    [Header("Enhancement Notes")]
    public string enhancementNotes; // Notes about what enhancements were configured manually
    public string llmReasoning; // LLM's reasoning for enhancement decisions
}

[System.Serializable]
public class ChatMessage
{
    public string sender; // "user" or "assistant"
    public string message;
    public float timestamp;
    public string messageType; // "question", "response", "clarification"
}

[System.Serializable]
public class EnhancedNavigationResults
{
    public NavigationSession navigationSession;
    public PerformanceComparison comparisonToBaseline;
    public bool completed = false;
    
    [Header("Enhancement Information")]
    public string enhancementType; // "algorithmic" or "llm"
    public string enhancementSummary; // Brief description of what enhancements were applied
}

[System.Serializable]
public class PerformanceComparison
{
    [Header("Collision Comparison")]
    public int baselineCollisions;
    public int enhancedCollisions;
    public float collisionReduction; // Percentage
    
    [Header("Navigation Comparison")]
    public float baselineDeviation;
    public float enhancedDeviation;
    public float deviationImprovement; // Percentage
    
    [Header("Speed Comparison")]
    public float baselineSpeed;
    public float enhancedSpeed;
    public float speedChange; // Percentage
    
    [Header("Overall Assessment")]
    public float overallImprovement; // -1 to 1 scale
    public string improvementSummary;
}