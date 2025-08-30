using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene controller for the LLM Conversational Assessment scene
/// Updated for manual trial control (no auto-advance)
/// </summary>
public class LLMAssessmentSceneController : MonoBehaviour
{
    [Header("Scene References")]
    public GeminiConversationalAssessment conversationalAssessment;
    public ConversationalUI conversationUI;
    
    [Header("Scene Settings")]
    public bool autoStartAssessment = true;
    public string returnToSceneName = "MainNavigationScene";
    
    [Header("Next Scene Configuration")]
    [Tooltip("Scene to load after assessment completion")]
    public string nextSceneName = "4_enhanced_navigation_scene";
    
    [Header("Manual Trial Control")]
    [Tooltip("Trial type to set after assessment completion")]
    public string nextTrialType = "short_algorithmic";
    
    [Header("Debug Settings")]
    public bool enableDebugMode = false;
    
    void Start()
    {
        Debug.Log("LLM Assessment Scene starting...");
        
        InitializeScene();
        
        if (autoStartAssessment)
        {
            StartAssessment();
        }
    }
    
    void InitializeScene()
    {
        // Find components if not assigned
        if (conversationalAssessment == null)
        {
            conversationalAssessment = FindObjectOfType<GeminiConversationalAssessment>();
        }
        
        if (conversationUI == null)
        {
            conversationUI = FindObjectOfType<ConversationalUI>();
        }
        
        // Verify SessionManager integration
        if (SessionManager.Instance == null)
        {
            Debug.LogError("SessionManager not found! LLM Assessment requires session data.");
            ShowErrorMessage("Session data not available. Please complete navigation first.");
            return;
        }
        
        // Subscribe to assessment completion
        GeminiConversationalAssessment.OnAssessmentCompleted += OnAssessmentCompleted;
        
        Debug.Log("LLM Assessment Scene initialized successfully");
    }
    
    void StartAssessment()
    {
        if (conversationalAssessment == null)
        {
            Debug.LogError("GeminiConversationalAssessment component not found!");
            ShowErrorMessage("Assessment system not available.");
            return;
        }
        
        Debug.Log("Starting LLM conversational assessment...");
        
        // The GeminiConversationalAssessment will handle the rest automatically
        // through its Start() method
    }
    
    void OnAssessmentCompleted(LLMAssessmentResults results)
    {
        Debug.Log("LLM Assessment completed successfully!");
        Debug.Log($"Results: {results.totalQuestions} questions, {results.conversationDuration:F1}s duration");
        
        // Mark the LLM trial as completed
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.MarkTrialCompleted("llm");
        }
        
        // Show completion options
        ShowCompletionOptions();
    }
    
    void ShowCompletionOptions()
    {
        if (conversationUI != null)
        {
            // Wait a moment, then show completion message
            StartCoroutine(ShowCompletionMessageDelayed());
        }
    }
    
    System.Collections.IEnumerator ShowCompletionMessageDelayed()
    {
        yield return new WaitForSeconds(3f);
        
        conversationUI.ShowSystemMessage("Assessment complete! Your personalized navigation enhancements have been saved.");
        
        yield return new WaitForSeconds(2f);
        
        conversationUI.ShowSystemMessage("Press [Space] to continue to enhanced navigation, or [Esc] to return to main menu.");
        
        // Enable keyboard shortcuts for navigation
        StartCoroutine(WaitForCompletionInput());
    }
    
    System.Collections.IEnumerator WaitForCompletionInput()
    {
        bool inputReceived = false;
        
        while (!inputReceived)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Continue to enhanced navigation
                ContinueToEnhancedNavigation();
                inputReceived = true;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Return to main scene
                ReturnToMainScene();
                inputReceived = true;
            }
            
            yield return null;
        }
    }
    
    void ContinueToEnhancedNavigation()
    {
        Debug.Log("Continuing to enhanced navigation...");
        
        // Manually set the next trial type in SessionManager
        if (SessionManager.Instance != null && !string.IsNullOrEmpty(nextTrialType))
        {
            SessionManager.Instance.SetCurrentTrial(nextTrialType);
            Debug.Log($"Set next trial to: {nextTrialType}");
        }
        
        // Load the enhanced navigation scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(returnToSceneName);
        }
    }
    
    void ReturnToMainScene()
    {
        Debug.Log("Returning to main scene...");
        
        // Just return to main scene without setting any trial
        SceneManager.LoadScene(returnToSceneName);
    }
    
    void ShowErrorMessage(string message)
    {
        if (conversationUI != null)
        {
            conversationUI.ShowSystemMessage($"Error: {message}");
            conversationUI.ShowSystemMessage("Press [Esc] to return to the main scene.");
            
            StartCoroutine(WaitForErrorReturn());
        }
        else
        {
            Debug.LogError($"{message}");
        }
    }
    
    System.Collections.IEnumerator WaitForErrorReturn()
    {
        while (!Input.GetKeyDown(KeyCode.Escape))
        {
            yield return null;
        }
        
        ReturnToMainScene();
    }
    
    // Context menu methods for testing
    [ContextMenu("Debug: Start Assessment Manually")]
    public void DebugStartAssessment()
    {
        StartAssessment();
    }
    
    [ContextMenu("Debug: Show Scene Status")]
    public void DebugShowSceneStatus()
    {
        Debug.Log("LLM ASSESSMENT SCENE STATUS:");
        Debug.Log($"ConversationalAssessment: {(conversationalAssessment != null ? "Found" : "Missing")}");
        Debug.Log($"ConversationUI: {(conversationUI != null ? "Found" : "Missing")}");
        Debug.Log($"SessionManager: {(SessionManager.Instance != null ? "Found" : "Missing")}");
        
        if (SessionManager.Instance != null)
        {
            string currentTrial = SessionManager.Instance.GetCurrentTrial();
            var completedTrials = SessionManager.Instance.GetCurrentSession().completedTrials;
            
            Debug.Log($"Current Trial: {currentTrial}");
            Debug.Log($"Completed Trials: {string.Join(", ", completedTrials)}");
        }
        
        if (conversationalAssessment != null)
        {
            Debug.Log($"Assessment Complete: {conversationalAssessment.IsAssessmentComplete()}");
        }
    }
    
    [ContextMenu("Debug: Test Error Message")]
    public void DebugTestErrorMessage()
    {
        ShowErrorMessage("This is a test error message to verify error handling UI.");
    }
    
    [ContextMenu("Debug: Test Completion Flow")]
    public void DebugTestCompletionFlow()
    {
        // Create mock results and trigger completion
        LLMAssessmentResults mockResults = new LLMAssessmentResults();
        mockResults.totalQuestions = 8;
        mockResults.conversationDuration = 120f;
        mockResults.completed = true;
        
        OnAssessmentCompleted(mockResults);
    }
    
    [ContextMenu("Manual: Continue to Enhanced Navigation")]
    public void ManualContinueToEnhancedNavigation()
    {
        ContinueToEnhancedNavigation();
    }
    
    void OnDestroy()
    {
        // Clean up event subscriptions
        GeminiConversationalAssessment.OnAssessmentCompleted -= OnAssessmentCompleted;
    }
    
    void Update()
    {
        // Debug mode shortcuts
        if (enableDebugMode)
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                DebugShowSceneStatus();
            }
            
            if (Input.GetKeyDown(KeyCode.F2))
            {
                DebugStartAssessment();
            }
            
            if (Input.GetKeyDown(KeyCode.F3) && conversationUI != null)
            {
                conversationUI.DebugTestSystemMessage();
            }
            
            if (Input.GetKeyDown(KeyCode.F4))
            {
                ManualContinueToEnhancedNavigation();
            }
        }
    }
}