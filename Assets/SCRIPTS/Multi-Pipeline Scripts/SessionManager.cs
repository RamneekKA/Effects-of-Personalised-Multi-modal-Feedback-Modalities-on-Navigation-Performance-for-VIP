using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// Simplified Session Manager without auto-advance functionality
/// Provides manual control over trial types and session management
/// </summary>
public class SessionManager : MonoBehaviour
{
    [Header("Session Configuration")]
    [Tooltip("Manually assigned User ID (e.g., User001)")]
    public string userID = "User001";
    
    [Tooltip("Current trial being executed - set this manually in each scene")]
    public string currentTrial = "baseline";
    
    [Header("Session Loading")]
    [Tooltip("Try to load existing session instead of creating new one")]
    public bool loadExistingSession = true;
    
    [Header("Pre-Analysis Coordination")]
    [Tooltip("Ensure pre-analysis runs before navigation trials")]
    public bool requirePreAnalysis = true;
    
    [Header("Debug Information")]
    [SerializeField] private UserSession currentSession;
    [SerializeField] private string sessionFolderPath;
    [SerializeField] private bool preAnalysisCompleted = false;

    // Trial definitions (for reference only - no auto-sequencing)
    private readonly string[] AVAILABLE_TRIALS = {
        "baseline",           // 1. Short baseline
        "algorithmic",        // 2. Algorithmic assessment  
        "llm",               // 3. LLM assessment
        "short_algorithmic", // 4. Short with algorithmic enhancements
        "short_llm",         // 5. Short with LLM enhancements
        "long_algorithmic",  // 6. Long with algorithmic enhancements
        "long_llm"          // 7. Long with LLM enhancements
    };
    
    // Singleton pattern
    public static SessionManager Instance { get; private set; }
    
    // Events
    public static System.Action<string> OnTrialChanged;
    public static System.Action<UserSession> OnSessionDataUpdated;
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeOrLoadSession();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeOrLoadSession()
    {
        bool sessionLoaded = false;
        
        if (loadExistingSession)
        {
            sessionLoaded = TryLoadExistingSession();
        }
        
        if (!sessionLoaded)
        {
            CreateNewSession();
        }
        
        // Override with inspector setting if different
        if (!string.IsNullOrEmpty(currentTrial) && currentTrial != currentSession.currentTrial)
        {
            Debug.Log($"Overriding loaded trial '{currentSession.currentTrial}' with inspector setting '{currentTrial}'");
            currentSession.currentTrial = currentTrial;
        }
        
        // Subscribe to pre-analysis events
        GeminiScenePreAnalyzer.OnPreAnalysisCompleted += OnPreAnalysisCompleted;
        GeminiScenePreAnalyzer.OnPreAnalysisFailed += OnPreAnalysisFailed;
        
        // Save session data
        SaveSessionData();
        
        Debug.Log($"Session Manager initialized for {currentSession.userID}");
        Debug.Log($"Session path: {sessionFolderPath}");
        Debug.Log($"Current trial: {currentSession.currentTrial}");
        
        // Notify other systems of current trial
        OnTrialChanged?.Invoke(currentSession.currentTrial);
    }
    
    bool TryLoadExistingSession()
    {
        Debug.Log($"Looking for existing session for user: {userID}");
        
        string baseNavigationPath = Path.Combine(Application.persistentDataPath, "NavigationData", "Users");
        
        if (!Directory.Exists(baseNavigationPath))
        {
            Debug.Log("No Users folder found, will create new session");
            return false;
        }
        
        // Find session folders that start with the userID
        string[] sessionFolders = Directory.GetDirectories(baseNavigationPath)
            .Where(dir => Path.GetFileName(dir).StartsWith(userID + "_"))
            .OrderByDescending(dir => Directory.GetCreationTime(dir)) // Most recent first
            .ToArray();
        
        if (sessionFolders.Length == 0)
        {
            Debug.Log($"No existing session found for {userID}, will create new session");
            return false;
        }
        
        string latestSessionFolder = sessionFolders[0];
        string userProfilePath = Path.Combine(latestSessionFolder, "UserProfile.json");
        
        if (!File.Exists(userProfilePath))
        {
            Debug.LogWarning($"Session folder exists but no UserProfile.json found: {latestSessionFolder}");
            return false;
        }
        
        try
        {
            string jsonData = File.ReadAllText(userProfilePath);
            currentSession = JsonUtility.FromJson<UserSession>(jsonData);
            sessionFolderPath = latestSessionFolder;
            
            Debug.Log($"Loaded existing session: {Path.GetFileName(latestSessionFolder)}");
            Debug.Log($"Completed trials: {string.Join(", ", currentSession.completedTrials)}");
            
            // Check if pre-analysis exists
            CheckExistingPreAnalysis();
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load session data: {e.Message}");
            return false;
        }
    }
    
    void CheckExistingPreAnalysis()
    {
        string analysisPath = Path.Combine(sessionFolderPath, "01_SceneAnalysis");
        string successFile = Path.Combine(analysisPath, "gemini_route_pre_analysis.txt");
        
        if (File.Exists(successFile))
        {
            preAnalysisCompleted = true;
            Debug.Log("Found existing pre-analysis - navigation can proceed");
        }
        else
        {
            preAnalysisCompleted = false;
            Debug.Log("No pre-analysis found - may need to run analysis");
        }
    }
    
    void CreateNewSession()
    {
        Debug.Log("Creating new session");
        
        // Create new session
        currentSession = new UserSession
        {
            userID = userID,
            sessionDateTime = DateTime.Now.ToString("yyyyMMdd_HHmmss"),
            currentTrial = currentTrial, // Use inspector value
            totalSessionTime = 0f
        };
        
        // Create session folder structure
        CreateSessionFolders();
        
        preAnalysisCompleted = false;
    }
    
    void CreateSessionFolders()
    {
        string basePath = Path.Combine(Application.persistentDataPath, "NavigationData", "Users");
        sessionFolderPath = Path.Combine(basePath, $"{currentSession.userID}_{currentSession.sessionDateTime}");
        currentSession.sessionPath = sessionFolderPath;
        
        // Create main session folder
        Directory.CreateDirectory(sessionFolderPath);
        
        // Create all subfolders
        string[] subFolders = {
            "01_SceneAnalysis",
            "02_BaselineNavigation/short_distance",
            "02_BaselineNavigation/Screenshots",
            "03_AlgorithmicAssessment", 
            "04_LLMAssessment",
            "05_EnhancedNavigation/short_llm_enhanced",
            "05_EnhancedNavigation/short_algorithmic_enhanced", 
            "05_EnhancedNavigation/long_llm_enhanced",
            "05_EnhancedNavigation/long_algorithmic_enhanced",
            "06_FinalAnalysis"
        };
        
        foreach (string folder in subFolders)
        {
            string fullPath = Path.Combine(sessionFolderPath, folder);
            Directory.CreateDirectory(fullPath);
            
            // Create Screenshots subfolders for enhanced navigation
            if (folder.Contains("enhanced"))
            {
                Directory.CreateDirectory(Path.Combine(fullPath, "Screenshots"));
            }
        }
        
        Debug.Log($"Created session folder structure at: {sessionFolderPath}");
    }
    
    // Manual trial setting method
    public void SetCurrentTrial(string trialType)
    {
        if (System.Array.IndexOf(AVAILABLE_TRIALS, trialType) != -1)
        {
            string previousTrial = currentSession.currentTrial;
            currentSession.currentTrial = trialType;
            currentTrial = trialType; // Update inspector field too
            
            Debug.Log($"Current trial set to: {trialType}");
            SaveSessionData();
            OnTrialChanged?.Invoke(trialType);
        }
        else
        {
            Debug.LogError($"Invalid trial type: {trialType}");
        }
    }
    
    // Method to mark trial as completed (manual)
    public void MarkTrialCompleted(string trialType = null)
    {
        string trialToMark = trialType ?? currentSession.currentTrial;
        
        if (!currentSession.completedTrials.Contains(trialToMark))
        {
            currentSession.completedTrials.Add(trialToMark);
            Debug.Log($"Marked trial as completed: {trialToMark}");
            Debug.Log($"Total completed trials: {currentSession.completedTrials.Count}");
            
            SaveSessionData();
            OnSessionDataUpdated?.Invoke(currentSession);
        }
    }
    
    public void CompleteSession()
    {
        currentSession.sessionCompleted = true;
        currentSession.totalSessionTime = Time.time;
        
        SaveSessionData();
        
        Debug.Log($"Session completed for {currentSession.userID}!");
        Debug.Log($"Total trials: {currentSession.completedTrials.Count}");
        Debug.Log($"Total time: {currentSession.totalSessionTime:F1} seconds");
    }
    
    public void SaveSessionData()
    {
        try
        {
            string sessionDataPath = Path.Combine(sessionFolderPath, "UserProfile.json");
            string jsonData = JsonUtility.ToJson(currentSession, true);
            File.WriteAllText(sessionDataPath, jsonData);
            
            Debug.Log($"Session data saved to: {sessionDataPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save session data: {e.Message}");
        }
    }
    
    void OnPreAnalysisCompleted()
    {
        preAnalysisCompleted = true;
        Debug.Log("Pre-analysis completed - navigation trials can now proceed");
        
        // Update session data
        if (currentSession.baselineResults == null)
            currentSession.baselineResults = new BaselineResults();
        
        currentSession.baselineResults.geminiAnalysisPath = Path.Combine(sessionFolderPath, "01_SceneAnalysis");
        SaveSessionData();
    }

    void OnPreAnalysisFailed(string errorMessage)
    {
        preAnalysisCompleted = true; // Allow navigation anyway
        Debug.LogWarning($"Pre-analysis failed but navigation will proceed: {errorMessage}");
        
        // Log the failure in session data
        if (currentSession.baselineResults == null)
            currentSession.baselineResults = new BaselineResults();
        
        currentSession.notes += $"Pre-analysis failed: {errorMessage}. ";
        SaveSessionData();
    }
    
    // PRE-ANALYSIS HELPER METHODS
    public bool IsPreAnalysisRequired(string trialType)
    {
        if (!requirePreAnalysis) return false;
        return IsNavigationTrial(trialType);
    }

    public bool IsPreAnalysisCompleted()
    {
        if (!requirePreAnalysis) return true;
        return preAnalysisCompleted;
    }

    public bool CanStartNavigation(string trialType)
    {
        if (!IsNavigationTrial(trialType)) return true;
        if (!requirePreAnalysis) return true;
        return preAnalysisCompleted;
    }
    
    // PUBLIC METHODS FOR OTHER SYSTEMS
    public string GetCurrentTrial()
    {
        return currentSession?.currentTrial ?? "unknown";
    }
    
    public UserSession GetCurrentSession()
    {
        return currentSession;
    }
    
    public string GetSessionPath()
    {
        return sessionFolderPath;
    }
    
    public string GetTrialDataPath(string trialType)
    {
        switch (trialType)
        {
            case "baseline":
                return Path.Combine(sessionFolderPath, "02_BaselineNavigation");
            case "algorithmic":
                return Path.Combine(sessionFolderPath, "03_AlgorithmicAssessment");
            case "llm":
                return Path.Combine(sessionFolderPath, "04_LLMAssessment");
            case "short_algorithmic":
                return Path.Combine(sessionFolderPath, "05_EnhancedNavigation", "short_algorithmic_enhanced");
            case "short_llm":
                return Path.Combine(sessionFolderPath, "05_EnhancedNavigation", "short_llm_enhanced");
            case "long_algorithmic":
                return Path.Combine(sessionFolderPath, "05_EnhancedNavigation", "long_algorithmic_enhanced");
            case "long_llm":
                return Path.Combine(sessionFolderPath, "05_EnhancedNavigation", "long_llm_enhanced");
            default:
                return sessionFolderPath;
        }
    }
    
    public bool IsNavigationTrial(string trialType)
    {
        return trialType == "baseline" || 
               trialType.Contains("short_") || 
               trialType.Contains("long_");
    }
    
    public bool IsAssessmentTrial(string trialType)
    {
        return trialType == "algorithmic" || trialType == "llm";
    }
    
    public string GetRouteType(string trialType)
    {
        if (trialType == "baseline" || trialType.Contains("short_"))
            return "short";
        else if (trialType.Contains("long_"))
            return "long";
        else
            return "none"; // Assessment trials
    }
    
    // CONTEXT MENU METHODS FOR MANUAL CONTROL
    [ContextMenu("Manual: Mark Current Trial Complete")]
    public void ManualMarkTrialComplete()
    {
        MarkTrialCompleted();
    }
    
    [ContextMenu("Manual: Set Trial to Baseline")]
    public void ManualSetBaseline()
    {
        SetCurrentTrial("baseline");
    }
    
    [ContextMenu("Manual: Set Trial to Short Algorithmic")]
    public void ManualSetShortAlgorithmic()
    {
        SetCurrentTrial("short_algorithmic");
    }
    
    [ContextMenu("Manual: Set Trial to Short LLM")]
    public void ManualSetShortLLM()
    {
        SetCurrentTrial("short_llm");
    }
    
    [ContextMenu("Manual: Set Trial to Long Algorithmic")]
    public void ManualSetLongAlgorithmic()
    {
        SetCurrentTrial("long_algorithmic");
    }
    
    [ContextMenu("Manual: Set Trial to Long LLM")]
    public void ManualSetLongLLM()
    {
        SetCurrentTrial("long_llm");
    }
    
    [ContextMenu("Manual: Complete Session")]
    public void ManualCompleteSession()
    {
        CompleteSession();
    }
    
    [ContextMenu("Manual: Reset Session")]
    public void ManualResetSession()
    {
        currentSession.completedTrials.Clear();
        currentSession.currentTrial = "baseline";
        currentTrial = "baseline";
        currentSession.sessionCompleted = false;
        currentSession.totalSessionTime = 0f;
        preAnalysisCompleted = false;
        
        SaveSessionData();
        
        Debug.Log("Session reset to beginning");
    }
    
    [ContextMenu("Debug: Show Session Info")]
    public void DebugShowSessionInfo()
    {
        if (currentSession == null)
        {
            Debug.Log("No session data available");
            return;
        }
        
        Debug.Log($"USER: {currentSession.userID}");
        Debug.Log($"SESSION: {currentSession.sessionDateTime}");
        Debug.Log($"CURRENT TRIAL: {currentSession.currentTrial}");
        Debug.Log($"COMPLETED: {string.Join(", ", currentSession.completedTrials)}");
        Debug.Log($"PATH: {sessionFolderPath}");
        Debug.Log($"FINISHED: {currentSession.sessionCompleted}");
        Debug.Log($"PRE-ANALYSIS: {preAnalysisCompleted}");
    }
    
    [ContextMenu("Debug: Force Load Existing Session")]
    public void ForceLoadExistingSession()
    {
        if (TryLoadExistingSession())
        {
            Debug.Log("Successfully loaded existing session");
            // Override with inspector setting
            if (!string.IsNullOrEmpty(currentTrial) && currentTrial != currentSession.currentTrial)
            {
                Debug.Log($"Overriding with inspector setting: {currentTrial}");
                currentSession.currentTrial = currentTrial;
                SaveSessionData();
                OnTrialChanged?.Invoke(currentTrial);
            }
        }
        else
        {
            Debug.LogError("Failed to load existing session");
        }
    }
    
    void OnDestroy()
    {
        // Clean up events
        GeminiScenePreAnalyzer.OnPreAnalysisCompleted -= OnPreAnalysisCompleted;
        GeminiScenePreAnalyzer.OnPreAnalysisFailed -= OnPreAnalysisFailed;
    }
}