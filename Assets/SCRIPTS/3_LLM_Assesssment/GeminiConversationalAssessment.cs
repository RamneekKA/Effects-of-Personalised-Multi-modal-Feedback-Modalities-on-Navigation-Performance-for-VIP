using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Text;
using System;
using System.Linq;

/// <summary>
/// Real-time LLM Conversational Assessment System
/// Conducts intelligent, adaptive conversation based on navigation data + pre-analysis
/// Each question builds on previous responses for natural dialogue flow
/// UPDATED: Enhanced navigation data loading with fallback search
/// </summary>
public class GeminiConversationalAssessment : MonoBehaviour
{
    [Header("Gemini API Settings")]
    public string geminiApiKey = "AIzaSyDBI39ajifrB_GqCfeWIG1RBt9KEzVfuU4";
    public string geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
    
    [Header("Assessment Limits")]
    public int maxQuestions = 15;
    
    [Header("SessionManager Integration")]
    public bool useSessionManager = true;
    
    [Header("UI Integration")]
    public ConversationalUI conversationUI;
    
    // Assessment State
    private NavigationSession currentSession;
    private SceneAnalysisData sceneAnalysisData;
    private string preAnalysisText = "";
    private List<ChatMessage> conversationHistory = new List<ChatMessage>();
    
    // Conversation Management
    private int questionCount = 0;
    private bool assessmentComplete = false;
    private bool conversationInProgress = false;
    private string fullContextPrompt = ""; // Master context for the entire conversation
    
    // Results
    private LLMAssessmentResults assessmentResults;
    
    // Events
    public static System.Action<LLMAssessmentResults> OnAssessmentCompleted;
    
    void Start()
    {
        if (conversationUI == null)
            conversationUI = FindObjectOfType<ConversationalUI>();
            
        InitializeAssessment();
    }
    
    void InitializeAssessment()
    {
        Debug.Log("ðŸ¤– Initializing Real-time LLM Conversational Assessment...");
        
        // Initialize results structure
        assessmentResults = new LLMAssessmentResults();
        assessmentResults.conversationDateTime = System.DateTime.Now.ToString();
        assessmentResults.conversationLog = new List<ChatMessage>();
        assessmentResults.questionsAsked = new List<string>();
        assessmentResults.extractedResponses = new Dictionary<string, string>();
        
        // Load all navigation and analysis data
        LoadNavigationData();
        LoadPreAnalysisData();
        
        // Create master context prompt that will be used throughout conversation
        CreateMasterContextPrompt();
        
        // Start the real-time conversation
        StartCoroutine(BeginRealTimeConversation());
    }
    
    void LoadNavigationData()
    {
        if (useSessionManager && SessionManager.Instance != null)
        {
            UserSession session = SessionManager.Instance.GetCurrentSession();
            
            if (session == null)
            {
                Debug.LogError("âŒ No current session found in SessionManager!");
                return;
            }
            
            Debug.Log($"ðŸ” Looking for navigation data in session: {session.userID}");
            Debug.Log($"ðŸ“Š Completed trials: [{string.Join(", ", session.completedTrials)}]");
            
            // OPTION 1: Try to find from completed trials first
            string latestNavigationTrial = FindLatestNavigationTrial(session.completedTrials);
            
            // OPTION 2: If no completed trials found, search for actual data files
            if (string.IsNullOrEmpty(latestNavigationTrial))
            {
                Debug.LogWarning("âš ï¸ No completed navigation trials found in session data.");
                Debug.Log("ðŸ” Searching for actual navigation data files...");
                latestNavigationTrial = FindNavigationDataFiles();
            }
            
            if (!string.IsNullOrEmpty(latestNavigationTrial))
            {
                string trialPath = SessionManager.Instance.GetTrialDataPath(latestNavigationTrial);
                LoadNavigationSessionFromPath(trialPath);
                Debug.Log($"ðŸ“Š Loaded navigation data from trial: {latestNavigationTrial}");
            }
            else
            {
                Debug.LogError("âŒ No navigation data found anywhere!");
                Debug.Log("ðŸ’¡ Make sure you have completed at least one navigation trial first.");
                // Continue anyway - we can still do a conversation without navigation data
            }
        }
        else
        {
            Debug.LogWarning("âš ï¸ SessionManager not available - using fallback data loading");
        }
    }
    
    /// <summary>
    /// Search for actual navigation_data.json files in the session folders
    /// </summary>
    string FindNavigationDataFiles()
    {
        if (SessionManager.Instance == null) return null;
        
        string sessionPath = SessionManager.Instance.GetSessionPath();
        
        if (string.IsNullOrEmpty(sessionPath) || !Directory.Exists(sessionPath))
        {
            Debug.LogError($"âŒ Session path not found: {sessionPath}");
            return null;
        }
        
        // List of navigation trials to check (in priority order)
        string[] navigationTrials = { "baseline", "short_llm", "short_algorithmic", "long_llm", "long_algorithmic" };
        
        Debug.Log($"ðŸ” Searching in session path: {sessionPath}");
        
        foreach (string trial in navigationTrials)
        {
            string trialPath = SessionManager.Instance.GetTrialDataPath(trial);
            string navigationDataPath = Path.Combine(trialPath, "navigation_data.json");
            
            Debug.Log($"ðŸ” Checking: {navigationDataPath}");
            
            if (File.Exists(navigationDataPath))
            {
                Debug.Log($"âœ… Found navigation data for trial: {trial}");
                
                // Check if the file has actual content
                try
                {
                    string jsonContent = File.ReadAllText(navigationDataPath);
                    if (!string.IsNullOrEmpty(jsonContent) && jsonContent.Length > 100) // Basic validation
                    {
                        Debug.Log($"âœ… Navigation data file is valid for trial: {trial}");
                        return trial;
                    }
                    else
                    {
                        Debug.LogWarning($"âš ï¸ Navigation data file exists but appears empty: {trial}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"âš ï¸ Could not read navigation data for {trial}: {e.Message}");
                }
            }
            else
            {
                Debug.Log($"âŒ No navigation data found for trial: {trial}");
            }
        }
        
        // If we get here, no navigation data was found
        Debug.LogError("âŒ No navigation_data.json files found in any trial folders!");
        
        // List what's actually in the session folder for debugging
        DebugListSessionContents();
        
        return null;
    }
    
    /// <summary>
    /// Debug helper to show what's actually in the session folder
    /// </summary>
    void DebugListSessionContents()
    {
        if (SessionManager.Instance == null) return;
        
        string sessionPath = SessionManager.Instance.GetSessionPath();
        
        if (!Directory.Exists(sessionPath))
        {
            Debug.LogError($"âŒ Session directory doesn't exist: {sessionPath}");
            return;
        }
        
        Debug.Log($"ðŸ“ SESSION FOLDER CONTENTS: {sessionPath}");
        
        try
        {
            string[] subDirectories = Directory.GetDirectories(sessionPath);
            
            foreach (string dir in subDirectories)
            {
                string dirName = Path.GetFileName(dir);
                Debug.Log($"  ðŸ“ {dirName}/");
                
                // Check if this looks like a trial folder
                if (dirName.Contains("Navigation") || dirName.Contains("Assessment"))
                {
                    // List files in this directory
                    string[] files = Directory.GetFiles(dir);
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        long fileSize = new FileInfo(file).Length;
                        Debug.Log($"    ðŸ“„ {fileName} ({fileSize} bytes)");
                    }
                    
                    // Check subdirectories
                    string[] subDirs = Directory.GetDirectories(dir);
                    foreach (string subDir in subDirs)
                    {
                        string subDirName = Path.GetFileName(subDir);
                        Debug.Log($"    ðŸ“ {subDirName}/");
                        
                        // If it's a specific trial subfolder, check for navigation_data.json
                        string navDataPath = Path.Combine(subDir, "navigation_data.json");
                        if (File.Exists(navDataPath))
                        {
                            long navDataSize = new FileInfo(navDataPath).Length;
                            Debug.Log($"      âœ… navigation_data.json ({navDataSize} bytes)");
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"âŒ Error listing session contents: {e.Message}");
        }
    }
    
    string FindLatestNavigationTrial(List<string> completedTrials)
    {
        string[] navigationTrials = { "long_llm", "long_algorithmic", "short_llm", "short_algorithmic", "baseline" };
        
        foreach (string navTrial in navigationTrials)
        {
            if (completedTrials.Contains(navTrial))
            {
                return navTrial;
            }
        }
        
        return null;
    }
    
    void LoadNavigationSessionFromPath(string trialPath)
    {
        string jsonPath = Path.Combine(trialPath, "navigation_data.json");
        
        if (File.Exists(jsonPath))
        {
            string jsonData = File.ReadAllText(jsonPath);
            currentSession = JsonUtility.FromJson<NavigationSession>(jsonData);
            Debug.Log($"ðŸ“ˆ Navigation session loaded: {currentSession.totalCollisions} collisions, {currentSession.duration:F1}s duration");
        }
        else
        {
            Debug.LogError($"âŒ Navigation data not found at: {jsonPath}");
        }
    }
    
    void LoadPreAnalysisData()
    {
        if (SessionManager.Instance != null)
        {
            string sessionPath = SessionManager.Instance.GetSessionPath();
            
            // Load scene analysis data
            string sceneAnalysisPath = Path.Combine(sessionPath, "01_SceneAnalysis", "scene_analysis.json");
            if (File.Exists(sceneAnalysisPath))
            {
                string sceneJson = File.ReadAllText(sceneAnalysisPath);
                sceneAnalysisData = JsonUtility.FromJson<SceneAnalysisData>(sceneJson);
                Debug.Log($"ðŸ›ï¸ Scene analysis loaded: {sceneAnalysisData.staticObjects?.Count ?? 0} objects");
            }
            
            // Load Gemini pre-analysis text
            string preAnalysisPath = Path.Combine(sessionPath, "01_SceneAnalysis", "gemini_route_pre_analysis.txt");
            if (File.Exists(preAnalysisPath))
            {
                preAnalysisText = File.ReadAllText(preAnalysisPath);
                Debug.Log($"ðŸ§  Pre-analysis loaded: {preAnalysisText.Length} characters");
            }
        }
    }
    
    void CreateMasterContextPrompt()
    {
        StringBuilder contextBuilder = new StringBuilder();
        
        contextBuilder.AppendLine("You are an expert vision assessment interviewer conducting a real-time conversation to understand a person's spatial vision capabilities.");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("CONTEXT - This information stays constant throughout our conversation:");
        contextBuilder.AppendLine();
        
        // Navigation session data
        if (currentSession != null)
        {
            contextBuilder.AppendLine("OBSERVED NAVIGATION BEHAVIOR:");
            float duration = currentSession.endTime - currentSession.startTime;
            contextBuilder.AppendLine($"- Duration: {duration:F1} seconds");
            contextBuilder.AppendLine($"- Total collisions: {currentSession.totalCollisions}");
            contextBuilder.AppendLine($"- Average speed: {currentSession.averageSpeed:F1}m/s");
            contextBuilder.AppendLine($"- Route deviation: {currentSession.averageAbsoluteDeviation:F1}m average");
            
            if (currentSession.collisionsByBodyPart != null && currentSession.collisionsByBodyPart.Count > 0)
            {
                contextBuilder.AppendLine("- Collision patterns by body part:");
                foreach (var kvp in currentSession.collisionsByBodyPart)
                {
                    contextBuilder.AppendLine($"  * {kvp.Key}: {kvp.Value} collisions");
                }
            }
            
            if (currentSession.collisionsByObjectType != null && currentSession.collisionsByObjectType.Count > 0)
            {
                contextBuilder.AppendLine("- Objects that caused collisions:");
                foreach (var kvp in currentSession.collisionsByObjectType)
                {
                    contextBuilder.AppendLine($"  * {kvp.Key}: {kvp.Value} collisions");
                }
            }
        }
        else
        {
            contextBuilder.AppendLine("NAVIGATION DATA: No navigation data available - will conduct general assessment");
        }
        
        contextBuilder.AppendLine();
        
        // Pre-analysis insights
        if (!string.IsNullOrEmpty(preAnalysisText))
        {
            contextBuilder.AppendLine("PRE-NAVIGATION ROUTE ANALYSIS:");
            // Include key insights (truncated for token management)
            string truncatedPreAnalysis = preAnalysisText.Length > 800 ? 
                preAnalysisText.Substring(0, 800) + "..." : preAnalysisText;
            contextBuilder.AppendLine(truncatedPreAnalysis);
        }
        
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("CONVERSATION RULES:");
        contextBuilder.AppendLine("1. You are having a REAL-TIME conversation - each response should flow naturally from what the person just said");
        contextBuilder.AppendLine("2. Ask ONE question at a time based on their navigation patterns AND their previous responses");
        contextBuilder.AppendLine("3. Reference specific observations when relevant (e.g., 'I noticed you collided more with X...')");
        contextBuilder.AppendLine("4. Keep questions conversational and non-technical");
        contextBuilder.AppendLine("5. Probe deeper when responses are unclear or interesting");
        contextBuilder.AppendLine("6. Stop asking questions when you have enough information to make enhancement decisions");
        contextBuilder.AppendLine($"7. MAXIMUM {maxQuestions} questions total");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("GOAL: Determine the best object prioritization and alert modalities for this person's navigation assistance.");
        contextBuilder.AppendLine();
        
        fullContextPrompt = contextBuilder.ToString();
        
        Debug.Log($"ðŸ§  Created master context prompt: {fullContextPrompt.Length} characters");
    }
    
    IEnumerator BeginRealTimeConversation()
    {
        // Show welcome message
        if (conversationUI != null)
        {
            conversationUI.ShowSystemMessage("Welcome to your Enhanced Vision Assessment! I'm going to have a conversation with you to understand your vision in detail.");
            yield return new WaitForSeconds(2f);
            
            conversationUI.ShowSystemMessage("This will help me create a detailed map of your vision capabilities at different angles and distances.");
            yield return new WaitForSeconds(2f);
            
            if (currentSession != null)
            {
                conversationUI.ShowSystemMessage("I've analyzed your navigation patterns and I'd like to ask you some questions based on what I observed.");
            }
            else
            {
                conversationUI.ShowSystemMessage("Since I don't have navigation data, I'll ask you general questions about your vision and spatial awareness.");
            }
            yield return new WaitForSeconds(1f);
            
            conversationUI.ShowSystemMessage("âœ… AI interviewer connected. Beginning personalized assessment...");
            yield return new WaitForSeconds(1f);
        }
        
        conversationInProgress = true;
        
        // Ask the first question
        yield return StartCoroutine(AskNextQuestion());
    }
    
    IEnumerator AskNextQuestion()
    {
        if (questionCount >= maxQuestions)
        {
            Debug.Log("ðŸ“‹ Reached maximum question limit, proceeding to final decisions");
            yield return StartCoroutine(GenerateFinalDecisions());
            yield break;
        }
        
        questionCount++;
        
        Debug.Log($"ðŸ¤” Generating question {questionCount}/{maxQuestions}...");
        
        if (conversationUI != null)
        {
            conversationUI.ShowSystemMessage("Thinking...");
        }
        
        string questionPrompt = CreateRealTimeQuestionPrompt();
        
        yield return StartCoroutine(SendQuestionRequest(questionPrompt));
    }
    
    string CreateRealTimeQuestionPrompt()
    {
        StringBuilder promptBuilder = new StringBuilder();
        
        // Add the master context
        promptBuilder.AppendLine(fullContextPrompt);
        
        // Add conversation history
        if (conversationHistory.Count > 0)
        {
            promptBuilder.AppendLine("CONVERSATION SO FAR:");
            foreach (ChatMessage message in conversationHistory)
            {
                string speaker = message.sender == "assistant" ? "YOU" : "PERSON";
                promptBuilder.AppendLine($"{speaker}: {message.message}");
            }
            promptBuilder.AppendLine();
        }
        
        // Current task
        if (questionCount == 1)
        {
            if (currentSession != null)
            {
                promptBuilder.AppendLine("TASK: Ask your FIRST question. Start with the most important observation from their navigation behavior.");
            }
            else
            {
                promptBuilder.AppendLine("TASK: Ask your FIRST question. Start with a general question about their vision and spatial awareness since no navigation data is available.");
            }
        }
        else
        {
            promptBuilder.AppendLine($"TASK: Ask your NEXT question (#{questionCount}). Build on their previous responses and your observations.");
            promptBuilder.AppendLine("Consider: Do you need clarification? Want to explore something deeper? Have enough info on this topic?");
        }
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("RESPONSE FORMAT:");
        promptBuilder.AppendLine("If you want to ask another question, respond with:");
        promptBuilder.AppendLine("QUESTION: [Your question here]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("If you have enough information to make decisions, respond with:");
        promptBuilder.AppendLine("ENOUGH_INFO: I have sufficient information to make personalized recommendations.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Generate your response now:");
        
        return promptBuilder.ToString();
    }
    
    IEnumerator SendQuestionRequest(string prompt)
    {
        string requestJson = CreateGeminiRequestJson(prompt);
        
        using (UnityWebRequest request = new UnityWebRequest(geminiApiUrl + "?key=" + geminiApiKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                ProcessQuestionResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"âŒ Question request failed: {request.error}");
                HandleQuestionRequestFailure();
            }
        }
    }
    
    void ProcessQuestionResponse(string response)
    {
        try
        {
            GeminiResponse geminiResponse = JsonUtility.FromJson<GeminiResponse>(response);
            
            if (geminiResponse?.candidates?.Length > 0 &&
                geminiResponse.candidates[0].content?.parts?.Length > 0)
            {
                string responseText = geminiResponse.candidates[0].content.parts[0].text.Trim();
                
                Debug.Log($"ðŸ¤– AI Response: {responseText}");
                
                // Check if AI wants to stop asking questions
                if (responseText.ToUpper().Contains("ENOUGH_INFO:"))
                {
                    Debug.Log("ðŸŽ¯ AI has enough information, proceeding to decisions");
                    StartCoroutine(GenerateFinalDecisions());
                    return;
                }
                
                // Extract question from response
                string question = ExtractQuestionFromResponse(responseText);
                
                if (!string.IsNullOrEmpty(question))
                {
                    // Show the question and wait for user response
                    ShowQuestionAndWaitForResponse(question);
                }
                else
                {
                    Debug.LogWarning("âš ï¸ Could not extract question from AI response, using fallback");
                    ShowQuestionAndWaitForResponse("Can you tell me more about any challenges you experienced during navigation?");
                }
            }
            else
            {
                Debug.LogError("âŒ Invalid response format from Gemini");
                HandleQuestionRequestFailure();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"âŒ Error processing question response: {e.Message}");
            HandleQuestionRequestFailure();
        }
    }
    
    string ExtractQuestionFromResponse(string response)
    {
        // Look for "QUESTION:" prefix
        if (response.ToUpper().Contains("QUESTION:"))
        {
            int questionStart = response.ToUpper().IndexOf("QUESTION:") + "QUESTION:".Length;
            string question = response.Substring(questionStart).Trim();
            
            // Clean up any extra formatting
            question = question.Replace("\n", " ").Trim();
            
            return question;
        }
        
        // If no prefix, assume the whole response is the question
        return response.Trim();
    }
    
    void ShowQuestionAndWaitForResponse(string question)
    {
        // Add question to our tracking
        assessmentResults.questionsAsked.Add(question);
        
        // Show question in UI
        if (conversationUI != null)
        {
            conversationUI.ShowSystemMessage(question);
        }
        
        // Add to conversation history
        ChatMessage questionMessage = new ChatMessage
        {
            sender = "assistant",
            message = question,
            timestamp = Time.time,
            messageType = "question"
        };
        conversationHistory.Add(questionMessage);
        assessmentResults.conversationLog.Add(questionMessage);
        
        // Wait for user response
        StartCoroutine(WaitForUserResponse(question));
    }
    
    IEnumerator WaitForUserResponse(string question)
    {
        bool responseReceived = false;
        string userResponse = "";
        
        if (conversationUI != null)
        {
            conversationUI.SetWaitingForInput(true);
            
            // Subscribe to user response
            System.Action<string> responseHandler = (response) =>
            {
                userResponse = response;
                responseReceived = true;
            };
            
            conversationUI.OnUserSubmittedResponse += responseHandler;
            
            // Wait for response
            yield return new WaitUntil(() => responseReceived);
            
            // Unsubscribe
            conversationUI.OnUserSubmittedResponse -= responseHandler;
            conversationUI.SetWaitingForInput(false);
        }
        else
        {
            // Fallback if no UI
            yield return new WaitForSeconds(1f);
            userResponse = "Test response";
            responseReceived = true;
        }
        
        // Handle the response
        HandleUserResponse(question, userResponse);
    }
    
    void HandleUserResponse(string question, string response)
    {
        Debug.Log($"ðŸ‘¤ User responded: {response}");
        
        // Record the response
        ChatMessage responseMessage = new ChatMessage
        {
            sender = "user",
            message = response,
            timestamp = Time.time,
            messageType = "response"
        };
        
        conversationHistory.Add(responseMessage);
        assessmentResults.conversationLog.Add(responseMessage);
        assessmentResults.extractedResponses[question] = response;
        
        // DON'T show user message in UI - the UI already handled this!
        // REMOVED: conversationUI.ShowUserMessage(response);
        
        // Continue conversation or finish
        if (response.ToUpper() == "SKIPPED" || questionCount >= maxQuestions)
        {
            // Either skipped or hit limit
            StartCoroutine(GenerateFinalDecisions());
        }
        else
        {
            // Ask next question
            StartCoroutine(AskNextQuestion());
        }
    }
    
    void HandleQuestionRequestFailure()
    {
        Debug.LogWarning("âš ï¸ Question generation failed, using fallback question");
        
        // Use fallback questions based on question count
        string[] fallbackQuestions = {
            "How confident do you feel when navigating around vehicles or moving objects?",
            "Do you find it easier to detect obstacles on your left side or right side?",
            "At what distance do you typically first notice potential obstacles in your path?",
            "How do you prefer to receive alerts about nearby obstacles?",
            "What types of objects do you find most challenging to navigate around?",
            "Do you feel more comfortable moving quickly or taking your time when navigating?",
            "Have you noticed any patterns in when or where you encounter navigation difficulties?",
            "What would help you feel more confident when walking in unfamiliar environments?"
        };
        
        int fallbackIndex = Mathf.Min(questionCount - 1, fallbackQuestions.Length - 1);
        string fallbackQuestion = fallbackQuestions[fallbackIndex];
        
        ShowQuestionAndWaitForResponse(fallbackQuestion);
    }
    
    IEnumerator GenerateFinalDecisions()
    {
        Debug.Log("ðŸŽ¯ Generating final enhancement decisions based on conversation...");
        
        conversationInProgress = false;
        
        if (conversationUI != null)
        {
            conversationUI.ShowSystemMessage("Thank you for your responses! Analyzing everything to create your personalized navigation enhancements...");
        }
        
        string decisionPrompt = CreateFinalDecisionPrompt();
        
        yield return StartCoroutine(SendFinalDecisionRequest(decisionPrompt));
    }
    
    string CreateFinalDecisionPrompt()
    {
        StringBuilder promptBuilder = new StringBuilder();
        
        promptBuilder.AppendLine("You are making final personalized navigation assistance decisions based on comprehensive data:");
        promptBuilder.AppendLine();
        
        // Include the master context
        promptBuilder.AppendLine(fullContextPrompt);
        
        // Include the full conversation
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("COMPLETE CONVERSATION:");
        foreach (ChatMessage message in conversationHistory)
        {
            string speaker = message.sender == "assistant" ? "INTERVIEWER" : "PERSON";
            promptBuilder.AppendLine($"{speaker}: {message.message}");
        }
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("DECISION TASK:");
        promptBuilder.AppendLine("Based on BOTH observed navigation behavior AND conversation responses, make these decisions:");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("1. OBJECT PRIORITIZATION:");
        promptBuilder.AppendLine("   List 3-5 object types for HIGH priority alerts");
        promptBuilder.AppendLine("   List 3-5 object types for MEDIUM priority alerts");
        promptBuilder.AppendLine("   List remaining types for LOW priority");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("2. MODALITY SELECTION:");
        promptBuilder.AppendLine("   Audio alerts: YES/NO");
        promptBuilder.AppendLine("   Haptic feedback: YES/NO");
        promptBuilder.AppendLine("   Spearcons: YES/NO");
        promptBuilder.AppendLine("   Visual enhancements: YES/NO");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("3. ALERT DISTANCES:");
        promptBuilder.AppendLine("   Alert distance (2-8 meters)");
        promptBuilder.AppendLine("   Warning distance (1-4 meters)");
        promptBuilder.AppendLine("   Critical distance (0.5-2 meters)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("4. SPEED RECOMMENDATIONS:");
        promptBuilder.AppendLine("   Recommend slower speed: YES/NO");
        promptBuilder.AppendLine("   Speed multiplier (0.5-1.0 if slower recommended)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("5. REASONING:");
        promptBuilder.AppendLine("   Provide 2-3 sentence explanation connecting observed behavior to conversation insights");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("RESPONSE FORMAT - Use this exact structure:");
        promptBuilder.AppendLine("HIGH_PRIORITY: Car,Bus,Pole");
        promptBuilder.AppendLine("MEDIUM_PRIORITY: Tree,Bench,Wall");
        promptBuilder.AppendLine("LOW_PRIORITY: Building,Ground");
        promptBuilder.AppendLine("AUDIO: YES");
        promptBuilder.AppendLine("HAPTICS: NO");
        promptBuilder.AppendLine("SPEARCONS: YES");
        promptBuilder.AppendLine("VISUAL: NO");
        promptBuilder.AppendLine("ALERT_DISTANCE: 4.0");
        promptBuilder.AppendLine("WARNING_DISTANCE: 2.0");
        promptBuilder.AppendLine("CRITICAL_DISTANCE: 1.0");
        promptBuilder.AppendLine("SLOWER_SPEED: NO");
        promptBuilder.AppendLine("SPEED_MULTIPLIER: 1.0");
        promptBuilder.AppendLine("REASONING: [Your detailed reasoning here]");
        
        return promptBuilder.ToString();
    }
    
    IEnumerator SendFinalDecisionRequest(string prompt)
    {
        string requestJson = CreateGeminiRequestJson(prompt);
        
        using (UnityWebRequest request = new UnityWebRequest(geminiApiUrl + "?key=" + geminiApiKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                ProcessFinalDecisionResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"âŒ Final decision request failed: {request.error}");
                CreateFallbackDecisions();
                CompleteAssessment();
            }
        }
    }
    
    void ProcessFinalDecisionResponse(string response)
    {
        try
        {
            GeminiResponse geminiResponse = JsonUtility.FromJson<GeminiResponse>(response);
            
            if (geminiResponse?.candidates?.Length > 0 &&
                geminiResponse.candidates[0].content?.parts?.Length > 0)
            {
                string decisionText = geminiResponse.candidates[0].content.parts[0].text;
                ParseFinalDecisions(decisionText);
                
                assessmentResults.llmReasoning = ExtractReasoning(decisionText);
                
                CompleteAssessment();
            }
            else
            {
                Debug.LogWarning("âš ï¸ Invalid decision response, using fallback");
                CreateFallbackDecisions();
                CompleteAssessment();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"âŒ Error parsing final decisions: {e.Message}");
            CreateFallbackDecisions();
            CompleteAssessment();
        }
    }
    
    void ParseFinalDecisions(string decisionText)
    {
        AppliedEnhancements decisions = new AppliedEnhancements();
        decisions.sourceAssessment = "llm";
        
        string[] lines = decisionText.Split('\n');
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("HIGH_PRIORITY:"))
            {
                string objectList = trimmedLine.Substring("HIGH_PRIORITY:".Length).Trim();
                decisions.highPriorityObjects = ParseObjectList(objectList);
            }
            else if (trimmedLine.StartsWith("MEDIUM_PRIORITY:"))
            {
                string objectList = trimmedLine.Substring("MEDIUM_PRIORITY:".Length).Trim();
                decisions.mediumPriorityObjects = ParseObjectList(objectList);
            }
            else if (trimmedLine.StartsWith("LOW_PRIORITY:"))
            {
                string objectList = trimmedLine.Substring("LOW_PRIORITY:".Length).Trim();
                decisions.lowPriorityObjects = ParseObjectList(objectList);
            }
            else if (trimmedLine.StartsWith("AUDIO:"))
            {
                decisions.useAudio = ParseBooleanValue(trimmedLine, "AUDIO:");
            }
            else if (trimmedLine.StartsWith("HAPTICS:"))
            {
                decisions.useHaptics = ParseBooleanValue(trimmedLine, "HAPTICS:");
            }
            else if (trimmedLine.StartsWith("SPEARCONS:"))
            {
                decisions.useSpearcons = ParseBooleanValue(trimmedLine, "SPEARCONS:");
            }
            else if (trimmedLine.StartsWith("VISUAL:"))
            {
                decisions.useVisualEnhancements = ParseBooleanValue(trimmedLine, "VISUAL:");
            }
            else if (trimmedLine.StartsWith("ALERT_DISTANCE:"))
            {
                decisions.alertDistance = ParseFloatValue(trimmedLine, "ALERT_DISTANCE:", 4.0f);
            }
            else if (trimmedLine.StartsWith("WARNING_DISTANCE:"))
            {
                decisions.warningDistance = ParseFloatValue(trimmedLine, "WARNING_DISTANCE:", 2.0f);
            }
            else if (trimmedLine.StartsWith("CRITICAL_DISTANCE:"))
            {
                decisions.criticalDistance = ParseFloatValue(trimmedLine, "CRITICAL_DISTANCE:", 1.0f);
            }
            else if (trimmedLine.StartsWith("SLOWER_SPEED:"))
            {
                decisions.recommendSlowerSpeed = ParseBooleanValue(trimmedLine, "SLOWER_SPEED:");
            }
            else if (trimmedLine.StartsWith("SPEED_MULTIPLIER:"))
            {
                decisions.recommendedSpeedMultiplier = ParseFloatValue(trimmedLine, "SPEED_MULTIPLIER:", 1.0f);
            }
        }
        
        assessmentResults.llmDecisions = decisions;
        
        Debug.Log("âœ… Successfully parsed LLM enhancement decisions from real-time conversation");
        Debug.Log($"ðŸŽ¯ High priority: {string.Join(", ", decisions.highPriorityObjects)}");
        Debug.Log($"ðŸ”Š Audio: {decisions.useAudio}, Haptics: {decisions.useHaptics}");
    }
    
    List<string> ParseObjectList(string objectList)
    {
        return objectList.Split(',')
            .Select(obj => obj.Trim())
            .Where(obj => !string.IsNullOrEmpty(obj))
            .ToList();
    }
    
    bool ParseBooleanValue(string line, string prefix)
    {
        string value = line.Substring(prefix.Length).Trim().ToUpper();
        return value == "YES" || value == "TRUE";
    }
    
    float ParseFloatValue(string line, string prefix, float defaultValue)
    {
        string valueStr = line.Substring(prefix.Length).Trim();
        if (float.TryParse(valueStr, out float result))
        {
            return result;
        }
        return defaultValue;
    }
    
    string ExtractReasoning(string decisionText)
    {
        string[] lines = decisionText.Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().StartsWith("REASONING:"))
            {
                StringBuilder reasoning = new StringBuilder();
                reasoning.AppendLine(lines[i].Substring("REASONING:".Length).Trim());
                
                // Include subsequent lines until we hit another directive or end
                for (int j = i + 1; j < lines.Length; j++)
                {
                    string line = lines[j].Trim();
                    if (string.IsNullOrEmpty(line) || line.Contains(":"))
                        break;
                    reasoning.AppendLine(line);
                }
                
                return reasoning.ToString().Trim();
            }
        }
        
        return "Reasoning based on observed navigation patterns and conversation responses.";
    }
    
    void CreateFallbackDecisions()
    {
        Debug.Log("ðŸ”§ Creating fallback enhancement decisions...");
        
        AppliedEnhancements fallbackDecisions = new AppliedEnhancements();
        fallbackDecisions.sourceAssessment = "llm_fallback";
        
        // Basic fallback decisions based on navigation data if available
        if (currentSession != null && currentSession.collisionsByObjectType != null)
        {
            var topCollisionObjects = currentSession.collisionsByObjectType
                .OrderByDescending(kvp => kvp.Value)
                .Take(3)
                .Select(kvp => kvp.Key)
                .ToList();
            
            fallbackDecisions.highPriorityObjects = topCollisionObjects;
        }
        else
        {
            fallbackDecisions.highPriorityObjects = new List<string> { "Car", "Pole", "Wall" };
        }
        
        fallbackDecisions.mediumPriorityObjects = new List<string> { "Tree", "Bench", "Bus" };
        fallbackDecisions.lowPriorityObjects = new List<string> { "Building", "Ground" };
        
        fallbackDecisions.useAudio = true;
        fallbackDecisions.useHaptics = false;
        fallbackDecisions.useSpearcons = true;
        fallbackDecisions.useVisualEnhancements = false;
        
        fallbackDecisions.alertDistance = 4.0f;
        fallbackDecisions.warningDistance = 2.0f;
        fallbackDecisions.criticalDistance = 1.0f;
        
        fallbackDecisions.recommendSlowerSpeed = false;
        fallbackDecisions.recommendedSpeedMultiplier = 1.0f;
        
        assessmentResults.llmDecisions = fallbackDecisions;
        assessmentResults.llmReasoning = "Fallback decisions applied due to processing error during conversation.";
    }
    
    void CompleteAssessment()
    {
        assessmentResults.completed = true;
        assessmentResults.conversationDuration = Time.time;
        assessmentResults.totalQuestions = questionCount;
        
        SaveAssessmentResults();
        
        // Show final results in UI
        if (conversationUI != null)
        {
            conversationUI.ShowFinalResults(assessmentResults);
        }
        
        // Notify other systems
        OnAssessmentCompleted?.Invoke(assessmentResults);
        
        Debug.Log("ðŸŽ‰ Real-time LLM Conversational Assessment completed!");
        Debug.Log($"ðŸ“Š {assessmentResults.totalQuestions} questions asked over {assessmentResults.conversationDuration:F1} seconds");
        Debug.Log($"ðŸ’¬ Conversation included {conversationHistory.Count} total messages");
    }
    
    void SaveAssessmentResults()
    {
        if (SessionManager.Instance != null)
        {
            string sessionPath = SessionManager.Instance.GetSessionPath();
            string llmAssessmentPath = Path.Combine(sessionPath, "04_LLMAssessment");
            Directory.CreateDirectory(llmAssessmentPath);
            
            string jsonPath = Path.Combine(llmAssessmentPath, $"llm_realtime_assessment_{System.DateTime.Now:yyyyMMdd_HHmmss}.json");
            string jsonData = JsonUtility.ToJson(assessmentResults, true);
            File.WriteAllText(jsonPath, jsonData);
            
            Debug.Log($"ðŸ’¾ Real-time LLM assessment results saved to: {jsonPath}");
            
            // Update SessionManager with results
            UserSession session = SessionManager.Instance.GetCurrentSession();
            session.llmResults = assessmentResults;
            SessionManager.Instance.SaveSessionData();
        }
    }
    
    string CreateGeminiRequestJson(string prompt)
    {
        StringBuilder jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{\"contents\":[{\"parts\":[");
        
        jsonBuilder.Append($"{{\"text\":\"{EscapeJsonString(prompt)}\"}}");
        
        jsonBuilder.Append("]}],\"generationConfig\":{\"maxOutputTokens\":8192,\"temperature\":0.4}}");
        
        return jsonBuilder.ToString();
    }
    
    string EscapeJsonString(string str)
    {
        return str.Replace("\\", "\\\\")
                  .Replace("\"", "\\\"")
                  .Replace("\r", "\\r")
                  .Replace("\n", "\\n")
                  .Replace("\t", "\\t");
    }
    
    // Public methods for external control
    public bool IsAssessmentComplete()
    {
        return assessmentComplete;
    }
    
    public bool IsConversationInProgress()
    {
        return conversationInProgress;
    }
    
    public int GetQuestionCount()
    {
        return questionCount;
    }
    
    public LLMAssessmentResults GetResults()
    {
        return assessmentResults;
    }
    
    [ContextMenu("Debug: Show Conversation State")]
    public void DebugShowConversationState()
    {
        Debug.Log($"ðŸ“‹ CONVERSATION STATE:");
        Debug.Log($"Questions asked: {questionCount}/{maxQuestions}");
        Debug.Log($"Conversation in progress: {conversationInProgress}");
        Debug.Log($"Assessment complete: {assessmentComplete}");
        Debug.Log($"Conversation history: {conversationHistory.Count} messages");
        Debug.Log($"Navigation session loaded: {currentSession != null}");
        Debug.Log($"Scene analysis loaded: {sceneAnalysisData != null}");
        Debug.Log($"Pre-analysis loaded: {!string.IsNullOrEmpty(preAnalysisText)}");
    }
    
    [ContextMenu("Debug: Show Master Context")]
    public void DebugShowMasterContext()
    {
        Debug.Log($"ðŸ§  MASTER CONTEXT ({fullContextPrompt.Length} chars):");
        Debug.Log(fullContextPrompt.Substring(0, Mathf.Min(500, fullContextPrompt.Length)) + "...");
    }
    
    [ContextMenu("Debug: Force Find Navigation Data")]
    public void DebugForceFindNavigationData()
    {
        string foundTrial = FindNavigationDataFiles();
        if (!string.IsNullOrEmpty(foundTrial))
        {
            Debug.Log($"âœ… Found navigation data: {foundTrial}");
        }
        else
        {
            Debug.Log("âŒ No navigation data found");
        }
    }

    // Add this method to your GeminiConversationalAssessment.cs file
// Place it at the bottom of the class, right before the closing brace

[ContextMenu("Debug: Check Navigation Data Paths")]
public void DebugCheckNavigationPaths()
{
    Debug.Log("=== NAVIGATION DATA PATH DEBUG ===");
    
    if (SessionManager.Instance != null)
    {
        string sessionPath = SessionManager.Instance.GetSessionPath();
        Debug.Log($"Current Session Path: {sessionPath}");
        
        // Check each trial folder
        string[] trials = { "baseline", "short_llm", "short_algorithmic", "long_llm", "long_algorithmic" };
        foreach (string trial in trials)
        {
            string trialPath = SessionManager.Instance.GetTrialDataPath(trial);
            string navFile = Path.Combine(trialPath, "navigation_data.json");
            
            Debug.Log($"{trial}: {(File.Exists(navFile) ? "EXISTS" : "MISSING")} at {navFile}");
            
            if (File.Exists(navFile))
            {
                FileInfo info = new FileInfo(navFile);
                Debug.Log($"  Size: {info.Length} bytes, Modified: {info.LastWriteTime}");
            }
        }
        
        // Check what completed trials are recorded
        var session = SessionManager.Instance.GetCurrentSession();
        Debug.Log($"Recorded completed trials: [{string.Join(", ", session.completedTrials)}]");
    }
    else
    {
        Debug.LogError("SessionManager.Instance is null!");
    }
}
}