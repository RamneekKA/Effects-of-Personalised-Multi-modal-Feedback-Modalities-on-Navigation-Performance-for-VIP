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
/// </summary>
public class GeminiConversationalAssessment : MonoBehaviour
{
    [Header("Gemini API Settings")]
    public string geminiApiKey = "";
    public string geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
    
    [Header("Assessment Limits")]
    public int maxQuestions = 10;
    public int minQuestions = 4;
    
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
    private string fullContextPrompt = "";
    
    // Results - using new data structure
    private EnhancementAssessmentResults assessmentResults;
    
    // Events
    public static System.Action<EnhancementAssessmentResults> OnAssessmentCompleted;
    
    void Start()
    {
        if (conversationUI == null)
            conversationUI = FindObjectOfType<ConversationalUI>();
            
        InitializeAssessment();
    }
    
    void InitializeAssessment()
    {
        Debug.Log("Initializing Real-time LLM Conversational Assessment...");
        
        // Initialize results structure with new data
        assessmentResults = new EnhancementAssessmentResults();
        assessmentResults.conversationDateTime = System.DateTime.Now.ToString();
        assessmentResults.conversationLog = new List<ChatMessage>();
        assessmentResults.questionsAsked = new List<string>();
        assessmentResults.extractedResponses = new Dictionary<string, string>();
        
        // Load all navigation and analysis data
        LoadNavigationData();
        LoadPreAnalysisData();
        
        // Create master context prompt
        CreateMasterContextPrompt();
        
        // Start the conversation
        StartCoroutine(BeginRealTimeConversation());
    }
    
    void LoadNavigationData()
    {
        if (useSessionManager && SessionManager.Instance != null)
        {
            UserSession session = SessionManager.Instance.GetCurrentSession();
            
            if (session == null)
            {
                Debug.LogError("No current session found in SessionManager!");
                return;
            }
            
            Debug.Log($"Looking for navigation data in session: {session.userID}");
            
            string latestNavigationTrial = FindLatestNavigationTrial(session.completedTrials);
            
            if (string.IsNullOrEmpty(latestNavigationTrial))
            {
                Debug.LogWarning("No completed navigation trials found in session data.");
                latestNavigationTrial = FindNavigationDataFiles();
            }
            
            if (!string.IsNullOrEmpty(latestNavigationTrial))
            {
                string trialPath = SessionManager.Instance.GetTrialDataPath(latestNavigationTrial);
                LoadNavigationSessionFromPath(trialPath);
                Debug.Log($"Loaded navigation data from trial: {latestNavigationTrial}");
            }
            else
            {
                Debug.LogError("No navigation data found anywhere!");
            }
        }
        else
        {
            Debug.LogWarning("SessionManager not available - using fallback data loading");
        }
    }
    
    string FindNavigationDataFiles()
    {
        if (SessionManager.Instance == null) return null;
        
        string sessionPath = SessionManager.Instance.GetSessionPath();
        
        if (string.IsNullOrEmpty(sessionPath) || !Directory.Exists(sessionPath))
        {
            Debug.LogError($"Session path not found: {sessionPath}");
            return null;
        }
        
        string[] navigationTrials = { "baseline", "short_llm", "short_algorithmic", "long_llm", "long_algorithmic" };
        
        foreach (string trial in navigationTrials)
        {
            string trialPath = SessionManager.Instance.GetTrialDataPath(trial);
            string navigationDataPath = Path.Combine(trialPath, "navigation_data.json");
            
            if (File.Exists(navigationDataPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(navigationDataPath);
                    if (!string.IsNullOrEmpty(jsonContent) && jsonContent.Length > 100)
                    {
                        Debug.Log($"Found navigation data for trial: {trial}");
                        return trial;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not read navigation data for {trial}: {e.Message}");
                }
            }
        }
        
        return null;
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
            Debug.Log($"Navigation session loaded: {currentSession.totalCollisions} collisions, {currentSession.duration:F1}s duration");

            ExtractCollisionDataFromNavigationPoints();

        }
        else
        {
            Debug.LogError($"Navigation data not found at: {jsonPath}");
        }
    }
    
    void LoadPreAnalysisData()
    {
        if (SessionManager.Instance != null)
        {
            string sessionPath = SessionManager.Instance.GetSessionPath();
            
            string sceneAnalysisPath = Path.Combine(sessionPath, "01_SceneAnalysis", "scene_analysis.json");
            if (File.Exists(sceneAnalysisPath))
            {
                string sceneJson = File.ReadAllText(sceneAnalysisPath);
                sceneAnalysisData = JsonUtility.FromJson<SceneAnalysisData>(sceneJson);
                Debug.Log($"Scene analysis loaded: {sceneAnalysisData.staticObjects?.Count ?? 0} objects");
            }
            
            string preAnalysisPath = Path.Combine(sessionPath, "01_SceneAnalysis", "gemini_route_pre_analysis.txt");
            if (File.Exists(preAnalysisPath))
            {
                preAnalysisText = File.ReadAllText(preAnalysisPath);
                Debug.Log($"Pre-analysis loaded: {preAnalysisText.Length} characters");
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
            string truncatedPreAnalysis = preAnalysisText.Length > 500 ? 
                preAnalysisText.Substring(0, 500) + "..." : preAnalysisText;
            contextBuilder.AppendLine(truncatedPreAnalysis);
        }
        
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("AVAILABLE ENHANCEMENT MODALITIES & CONFIGURATIONS:");
        contextBuilder.AppendLine("Use this knowledge to inform your questions, but don't mention technical details to the user.");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("1. VISUAL ENHANCEMENTS:");
        contextBuilder.AppendLine("   - Navigation Line: width (0.2-0.6), opacity (0-100%)");
        contextBuilder.AppendLine("     * Thicker lines = more visible but potentially distracting");
        contextBuilder.AppendLine("     * Higher opacity = more prominent, lower = more subtle");
        contextBuilder.AppendLine("   - Bounding Boxes: width (0.02-0.2), opacity (0-100%), range (5-35m)");
        contextBuilder.AppendLine("     * Shows outlines around detected objects");
        contextBuilder.AppendLine("     * Range determines how far away objects start showing boxes");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("2. AUDIO ENHANCEMENTS (choose only ONE type):");
        contextBuilder.AppendLine("   - TTS Speech (Text-to-Speech with Direction): interval (0.15-5s)");
        contextBuilder.AppendLine("     * Speaks both object name AND directional information aloud (e.g., 'left car', 'right tree')");
        contextBuilder.AppendLine("     * Always announces the single closest object to the user");
        contextBuilder.AppendLine("     * Lower intervals = more frequent announcements");
        contextBuilder.AppendLine("   - Spatial Speech (Accelerated Audio with Spatial Positioning): interval (0.5-3s)");
        contextBuilder.AppendLine("     * Plays accelerated speech of object name only (e.g., 'car', 'tree')");
        contextBuilder.AppendLine("     * Uses spatial audio positioning - audio comes FROM the direction of the object");
        contextBuilder.AppendLine("     * If object is on the left, you hear 'car' in your left ear; if on right, you hear it in right ear");
        contextBuilder.AppendLine("     * Always announces the single closest object to the user");
        contextBuilder.AppendLine("   - Spatial Speech with Distance Filtering (Accelerated Audio for Distant Objects): interval (0.5-3s), distance threshold (0-10m)");
        contextBuilder.AppendLine("     * Same as Spatial Speech above - object name with spatial positioning");
        contextBuilder.AppendLine("     * BUT only announces objects that are BEYOND a minimum distance threshold");
        contextBuilder.AppendLine("     * Filters out very close objects, focusing on approaching obstacles you need to plan for");
        contextBuilder.AppendLine("     * Example: With 3m threshold, won't announce objects closer than 3m, only distant approaching ones");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("3. HAPTIC ENHANCEMENTS (vibration vest):");
        contextBuilder.AppendLine("   - Three regions: Central, Left, Right chest areas");
        contextBuilder.AppendLine("   - Each region: min intensity (0-100%), max intensity (0-100%)");
        contextBuilder.AppendLine("     * These intensities are applied between 1.5m-2.5m of the object");
        contextBuilder.AppendLine("     * Closer to object = closer to max intensity, further away = closer to minimum intensity");
        contextBuilder.AppendLine("   - Object count (1, 2, or 3) - how many nearest objects to convey");
        contextBuilder.AppendLine("     * 1 = simple (only closest), 3 = complex (multiple objects)");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("CONVERSATION RULES:");
        contextBuilder.AppendLine("1. You are having a REAL-TIME conversation - each response should flow naturally from what the person just said");
        contextBuilder.AppendLine("2. Ask ONE question at a time based on their navigation patterns AND their previous responses");
        contextBuilder.AppendLine("3. Reference specific observations when relevant (e.g., 'I noticed you collided more with X...')");
        contextBuilder.AppendLine("4. Keep questions conversational and non-technical");
        contextBuilder.AppendLine("5. Probe deeper when responses are unclear or interesting");
        contextBuilder.AppendLine("6. Naturally explore their preferences for different types of assistance (visual, audio, haptic feedback)");
        contextBuilder.AppendLine("7. Ask questions that help you understand their needs so YOU can configure the optimal settings");
        contextBuilder.AppendLine("8. Ask about what they think might be helpful for their navigation challenges");
        contextBuilder.AppendLine("9. Don't use all questions for modality selection - first understand the challenges they faced during navigation");
        contextBuilder.AppendLine($"10. You MUST ask at least {minQuestions} questions before finishing the assessment");
        contextBuilder.AppendLine($"11. MAXIMUM {maxQuestions} questions total");
        contextBuilder.AppendLine("12. Only stop asking questions when you have enough information AND have reached the minimum");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("GOAL: Determine the best navigation assistance modalities for this person based on both their performance AND their preferences.");
        contextBuilder.AppendLine();
        
        fullContextPrompt = contextBuilder.ToString();
        
        Debug.Log($"Created master context prompt: {fullContextPrompt.Length} characters");
    }

    void ExtractCollisionDataFromNavigationPoints()
    {
        if (currentSession == null || currentSession.dataPoints == null) return;
        
        // Extract collision data from individual data points if dictionaries are empty
        if (currentSession.collisionsByObjectType == null || currentSession.collisionsByObjectType.Count == 0)
        {
            Debug.Log("Extracting collision object types from navigation data points...");
            
            currentSession.collisionsByObjectType = new Dictionary<string, int>();
            
            var collisionPoints = currentSession.dataPoints.Where(dp => dp.isCollision && !string.IsNullOrEmpty(dp.collisionObject));
            
            foreach (var collision in collisionPoints)
            {
                string objectType = collision.collisionObject;
                
                if (currentSession.collisionsByObjectType.ContainsKey(objectType))
                {
                    currentSession.collisionsByObjectType[objectType]++;
                }
                else
                {
                    currentSession.collisionsByObjectType[objectType] = 1;
                }
            }
            
            Debug.Log($"Extracted collision data: {currentSession.collisionsByObjectType.Count} object types");
            foreach (var kvp in currentSession.collisionsByObjectType)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value} collisions");
            }
        }
        
        // Do the same for body parts if needed
        if (currentSession.collisionsByBodyPart == null || currentSession.collisionsByBodyPart.Count == 0)
        {
            Debug.Log("Extracting collision body parts from navigation data points...");
            
            currentSession.collisionsByBodyPart = new Dictionary<string, int>();
            
            var collisionPoints = currentSession.dataPoints.Where(dp => dp.isCollision && !string.IsNullOrEmpty(dp.bodyPartInvolved));
            
            foreach (var collision in collisionPoints)
            {
                string bodyPart = collision.bodyPartInvolved;
                
                if (currentSession.collisionsByBodyPart.ContainsKey(bodyPart))
                {
                    currentSession.collisionsByBodyPart[bodyPart]++;
                }
                else
                {
                    currentSession.collisionsByBodyPart[bodyPart] = 1;
                }
            }
            
            Debug.Log($"Extracted body part data: {currentSession.collisionsByBodyPart.Count} body parts");
            foreach (var kvp in currentSession.collisionsByBodyPart)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value} collisions");
            }
        }
    }
    
    IEnumerator BeginRealTimeConversation()
    {
        if (conversationUI != null)
        {
            conversationUI.ShowSystemMessage("Welcome to your Enhanced Vision Assessment! I'm going to have a conversation with you to understand your vision in detail.");
            yield return new WaitForSeconds(2f);
            
            conversationUI.ShowSystemMessage("This will help me create personalized navigation enhancements for you.");
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
            
            conversationUI.ShowSystemMessage("AI interviewer connected. Beginning personalized assessment...");
            yield return new WaitForSeconds(1f);
        }
        
        conversationInProgress = true;
        
        yield return StartCoroutine(AskNextQuestion());
    }
    
    IEnumerator AskNextQuestion()
    {
        if (questionCount >= maxQuestions)
        {
            Debug.Log("Reached maximum question limit, proceeding to final decisions");
            yield return StartCoroutine(GenerateFinalDecisions());
            yield break;
        }
        
        questionCount++;
        
        Debug.Log($"Generating question {questionCount}/{maxQuestions} (min: {minQuestions})...");
        
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
        
        promptBuilder.AppendLine(fullContextPrompt);
        
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
            
            // ADD MINIMUM LOGIC HERE:
            if (questionCount < minQuestions)
            {
                promptBuilder.AppendLine($"IMPORTANT: You must ask at least {minQuestions} questions total. You are currently on question {questionCount}, so you CANNOT finish yet.");
                promptBuilder.AppendLine("Continue exploring their vision capabilities and navigation challenges.");
            }
            else
            {
                promptBuilder.AppendLine($"You have asked {questionCount} questions (minimum {minQuestions} reached). You can now finish if you have enough information, or continue asking up to {maxQuestions} total.");
                promptBuilder.AppendLine("Consider: Do you need clarification? Want to explore something deeper? Have enough info on this topic?");
            }
        }
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("RESPONSE FORMAT:");
        promptBuilder.AppendLine("If you want to ask another question, respond with:");
        promptBuilder.AppendLine("QUESTION: [Your question here]");
        promptBuilder.AppendLine();
        
        if (questionCount >= minQuestions)
        {
            promptBuilder.AppendLine("If you have enough information to make decisions, respond with:");
            promptBuilder.AppendLine("ENOUGH_INFO: I have sufficient information to make personalized recommendations.");
        }
        else
        {
            promptBuilder.AppendLine($"NOTE: You must ask at least {minQuestions} questions before you can finish the assessment.");
        }
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Generate your response now:");
        
        return promptBuilder.ToString();
    }
    
    IEnumerator SendQuestionRequest(string prompt)
    {
        int maxRetries = 3;
        float retryDelay = 2f;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
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
                    yield break; // Success, exit retry loop
                }
                else if (request.responseCode == 503 && attempt < maxRetries)
                {
                    Debug.LogWarning($"Question request 503 Service Unavailable - Retry {attempt}/{maxRetries} in {retryDelay}s");
                    yield return new WaitForSeconds(retryDelay);
                    retryDelay *= 2; // Exponential backoff
                }
                else
                {
                    Debug.LogError($"Question request failed: {request.error} (Response Code: {request.responseCode})");
                    HandleQuestionRequestFailure();
                    yield break;
                }
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
                
                Debug.Log($"AI Response: {responseText}");
                
                if (responseText.ToUpper().Contains("ENOUGH_INFO:"))
                {
                    if (questionCount >= minQuestions)
                    {
                        Debug.Log($"AI has enough information after {questionCount} questions (min: {minQuestions}), proceeding to decisions");
                        StartCoroutine(GenerateFinalDecisions());
                        return;
                    }
                    else
                    {
                        Debug.Log($"AI tried to finish early ({questionCount}/{minQuestions} questions) - forcing another question");
                        // Force another question by treating this as a regular question response
                        string forceQuestion = $"Please ask one more question to better understand their navigation challenges. You've only asked {questionCount} out of the minimum {minQuestions} questions required.";
                        ShowQuestionAndWaitForResponse(forceQuestion);
                        return;
                    }
                }
                
                string question = ExtractQuestionFromResponse(responseText);
                
                if (!string.IsNullOrEmpty(question))
                {
                    ShowQuestionAndWaitForResponse(question);
                }
                else
                {
                    Debug.LogWarning("Could not extract question from AI response, using fallback");
                    ShowQuestionAndWaitForResponse("Can you tell me more about any challenges you experienced during navigation?");
                }
            }
            else
            {
                Debug.LogError("Invalid response format from Gemini");
                HandleQuestionRequestFailure();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing question response: {e.Message}");
            HandleQuestionRequestFailure();
        }
    }
    
    string ExtractQuestionFromResponse(string response)
    {
        if (response.ToUpper().Contains("QUESTION:"))
        {
            int questionStart = response.ToUpper().IndexOf("QUESTION:") + "QUESTION:".Length;
            string question = response.Substring(questionStart).Trim();
            question = question.Replace("\n", " ").Trim();
            return question;
        }
        
        return response.Trim();
    }
    
    void ShowQuestionAndWaitForResponse(string question)
    {
        assessmentResults.questionsAsked.Add(question);
        
        if (conversationUI != null)
        {
            conversationUI.ShowSystemMessage(question);
        }
        
        ChatMessage questionMessage = new ChatMessage
        {
            sender = "assistant",
            message = question,
            timestamp = Time.time,
            messageType = "question"
        };
        conversationHistory.Add(questionMessage);
        assessmentResults.conversationLog.Add(questionMessage);
        
        StartCoroutine(WaitForUserResponse(question));
    }
    
    IEnumerator WaitForUserResponse(string question)
    {
        bool responseReceived = false;
        string userResponse = "";
        
        if (conversationUI != null)
        {
            conversationUI.SetWaitingForInput(true);
            
            System.Action<string> responseHandler = (response) =>
            {
                userResponse = response;
                responseReceived = true;
            };
            
            conversationUI.OnUserSubmittedResponse += responseHandler;
            
            yield return new WaitUntil(() => responseReceived);
            
            conversationUI.OnUserSubmittedResponse -= responseHandler;
            conversationUI.SetWaitingForInput(false);
        }
        else
        {
            yield return new WaitForSeconds(1f);
            userResponse = "Test response";
            responseReceived = true;
        }
        
        HandleUserResponse(question, userResponse);
    }
    
    void HandleUserResponse(string question, string response)
    {
        Debug.Log($"User responded: {response}");
        
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
        
        if (response.ToUpper() == "SKIPPED" || questionCount >= maxQuestions)
        {
            StartCoroutine(GenerateFinalDecisions());
        }
        else
        {
            StartCoroutine(AskNextQuestion());
        }
    }
    
    void HandleQuestionRequestFailure()
    {
        Debug.LogWarning("Question generation failed, using fallback question");
        
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
        Debug.Log("Generating final enhancement decisions based on conversation...");
        
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
        
        promptBuilder.AppendLine(fullContextPrompt);
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("COMPLETE CONVERSATION:");
        foreach (ChatMessage message in conversationHistory)
        {
            string speaker = message.sender == "assistant" ? "INTERVIEWER" : "PERSON";
            promptBuilder.AppendLine($"{speaker}: {message.message}");
        }
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("DECISION TASK:");
        promptBuilder.AppendLine("Based on BOTH observed navigation behavior AND conversation responses, choose enhancement modalities to help this user navigate:");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("AVAILABLE MODALITIES (choose any combination, but must apply at least one enhancement):");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("1. VISUAL ENHANCEMENTS:");
        promptBuilder.AppendLine("   - Navigation Line:");
        promptBuilder.AppendLine("     * Line width (0.2-0.6): Higher values = thicker, more visible line");
        promptBuilder.AppendLine("     * Opacity (0-100%): Higher values = more solid/opaque, lower = more transparent");
        promptBuilder.AppendLine("   - Bounding Box (shows rectangular outlines around detected objects):");
        promptBuilder.AppendLine("     * Line width (0.02-0.2): Higher values = thicker box outlines, more prominent");
        promptBuilder.AppendLine("     * Opacity (0-100%): Higher values = more solid boxes, lower = more subtle");
        promptBuilder.AppendLine("     * Range (5-35m): Distance at which boxes appear - higher = boxes shown from farther away");
        promptBuilder.AppendLine("   - Can be disabled if visual information would be overwhelming or unhelpful");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("2. AUDIO ENHANCEMENTS (select only ONE audio option):");
        promptBuilder.AppendLine("   - TTS Speech (Text-to-Speech with Direction): Speaks both object name AND directional information aloud (e.g., 'left car', 'right tree')");
        promptBuilder.AppendLine("     * Interval (0.15-5s): How often announcements occur - lower = more frequent updates");
        promptBuilder.AppendLine("     * Always announces the single closest object to the user");
        promptBuilder.AppendLine("   - Spatial Speech (Accelerated Audio with Spatial Positioning): Plays accelerated speech of object name only (e.g., 'car', 'tree')");
        promptBuilder.AppendLine("     * Interval (0.5-3s): How often sound cues play - lower = more frequent audio");
        promptBuilder.AppendLine("     * Uses spatial audio positioning - audio comes FROM the direction of the object");
        promptBuilder.AppendLine("     * If object is on the left, you hear 'car' in your left ear; if on right, you hear it in right ear");
        promptBuilder.AppendLine("     * Always announces the single closest object to the user");
        promptBuilder.AppendLine("   - Spatial Speech with Distance Filtering (Accelerated Audio for Distant Objects): Same as Spatial Speech but only for distant objects");
        promptBuilder.AppendLine("     * Interval (0.5-3s): How often sound cues play - lower = more frequent audio");
        promptBuilder.AppendLine("     * Distance (0-10m): Only objects beyond this distance trigger audio - higher = fewer close objects announced");
        promptBuilder.AppendLine("     * Filters out very close objects, focusing on approaching obstacles you need to plan for");
        promptBuilder.AppendLine("   - Can be disabled if audio would be distracting or user prefers silence");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("3. HAPTIC ENHANCEMENTS (uses haptic vest with vibration feedback):");
        promptBuilder.AppendLine("   If haptics chosen, ALL settings must be configured (each region can have different intensities):");
        promptBuilder.AppendLine("   - Central region (middle of chest): Vibrates for objects directly ahead");
        promptBuilder.AppendLine("     * Min intensity (0-100%): Baseline vibration strength");
        promptBuilder.AppendLine("     * Max intensity (0-100%): Maximum vibration strength");
        promptBuilder.AppendLine("   - Left region (left side of chest): Vibrates for objects to the left");
        promptBuilder.AppendLine("     * Min intensity (0-100%): Baseline vibration strength");
        promptBuilder.AppendLine("     * Max intensity (0-100%): Maximum vibration strength");
        promptBuilder.AppendLine("   - Right region (right side of chest): Vibrates for objects to the right");
        promptBuilder.AppendLine("     * Min intensity (0-100%): Baseline vibration strength");
        promptBuilder.AppendLine("     * Max intensity (0-100%): Maximum vibration strength");
        promptBuilder.AppendLine("   - Intensity is distributed between 1.5m to 2.5m between the object and user");
        promptBuilder.AppendLine("   - Max intensity at 1.5m, min intensity at 2.5m");
        promptBuilder.AppendLine("   - Number of nearest objects to convey: 1 or 2");
        promptBuilder.AppendLine("     * 1 = only closest object causes vibration (simple)");
        promptBuilder.AppendLine("     * 2 = two closest objects can vibrate simultaneously (more information)");
        promptBuilder.AppendLine("   - Can be disabled if physical vibrations would be uncomfortable or distracting");
        promptBuilder.AppendLine();
        
        // Add the anti-copying instructions (Option 3)
        promptBuilder.AppendLine("CRITICAL INSTRUCTIONS:");
        promptBuilder.AppendLine("Do NOT copy any placeholder or example values. You must analyze the conversation and navigation data to determine appropriate settings for THIS specific person. Using generic or placeholder values will result in poor assistance for the user.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Think through each parameter:");
        promptBuilder.AppendLine("1. What did their navigation behavior show?");
        promptBuilder.AppendLine("2. What did they say in the conversation?");
        promptBuilder.AppendLine("3. What would help THEIR specific challenges?");
        promptBuilder.AppendLine("4. Choose values that match THEIR needs, not generic defaults.");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("RESPONSE FORMAT - Use this exact structure with YOUR calculated values:");
        promptBuilder.AppendLine("VISUAL_ENABLED: [YES or NO based on your analysis]");
        promptBuilder.AppendLine("NAV_LINE_WIDTH: [your chosen value between 0.2-0.6]");
        promptBuilder.AppendLine("NAV_LINE_OPACITY: [your chosen percentage 0-100]");
        promptBuilder.AppendLine("BBOX_WIDTH: [your chosen value between 0.02-0.2]");
        promptBuilder.AppendLine("BBOX_OPACITY: [your chosen percentage 0-100]");
        promptBuilder.AppendLine("BBOX_RANGE: [your chosen distance 5-35]");
        promptBuilder.AppendLine("AUDIO_ENABLED: [YES or NO based on your analysis]");
        promptBuilder.AppendLine("AUDIO_TYPE: [TTS, SPATIAL_SPEECH, or SPATIAL_SPEECH_DISTANCE]");
        promptBuilder.AppendLine("AUDIO_INTERVAL: [your chosen value within valid range]");
        promptBuilder.AppendLine("AUDIO_DISTANCE: [your chosen value 0-10, only if using SPATIAL_SPEECH_DISTANCE]");
        promptBuilder.AppendLine("HAPTIC_ENABLED: [YES or NO based on your analysis]");
        promptBuilder.AppendLine("HAPTIC_CENTRAL_MIN: [your chosen percentage 0-100]");
        promptBuilder.AppendLine("HAPTIC_CENTRAL_MAX: [your chosen percentage 0-100]");
        promptBuilder.AppendLine("HAPTIC_LEFT_MIN: [your chosen percentage 0-100]");
        promptBuilder.AppendLine("HAPTIC_LEFT_MAX: [your chosen percentage 0-100]");
        promptBuilder.AppendLine("HAPTIC_RIGHT_MIN: [your chosen percentage 0-100]");
        promptBuilder.AppendLine("HAPTIC_RIGHT_MAX: [your chosen percentage 0-100]");
        promptBuilder.AppendLine("HAPTIC_OBJECT_COUNT: [1 or 2]");
        promptBuilder.AppendLine("REASONING: [Write a comprehensive analysis of at least 300-500 words covering:");
        promptBuilder.AppendLine("1. NAVIGATION ANALYSIS: What specific patterns did you observe in their navigation behavior? Which collision types, body parts, speeds, and deviations informed your decisions?");
        promptBuilder.AppendLine("2. CONVERSATION INSIGHTS: What key preferences, challenges, and feedback did the user express during our conversation?");
        promptBuilder.AppendLine("3. VISUAL DECISIONS: Explain in detail why you chose each visual parameter (line width, opacity, bounding box settings). What user factors influenced these specific values?");
        promptBuilder.AppendLine("4. AUDIO DECISIONS: Explain your audio choice (enabled/disabled, type selection, interval timing). How did user responses guide this decision?");
        promptBuilder.AppendLine("5. HAPTIC DECISIONS: Detail your haptic configuration reasoning (enabled/disabled, intensity levels for each region, object count). What user needs drove these choices?");
        promptBuilder.AppendLine("6. ALTERNATIVE CONSIDERATIONS: What other configurations did you consider but reject? Why were those alternatives less suitable for this specific user?");
        promptBuilder.AppendLine("7. PERSONALIZATION SUMMARY: How does this configuration specifically address this user's unique navigation challenges and preferences?]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("IMPORTANT: Replace ALL bracketed placeholders with actual values based on your analysis. Do not leave any brackets or placeholder text in your response.");
        
        return promptBuilder.ToString();
    }
        
    IEnumerator SendFinalDecisionRequest(string prompt)
    {
        int maxRetries = 3;
        float retryDelay = 2f;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
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
                    yield break; // Success, exit retry loop
                }
                else if (request.responseCode == 503 && attempt < maxRetries)
                {
                    Debug.LogWarning($"Final decision 503 Service Unavailable - Retry {attempt}/{maxRetries} in {retryDelay}s");
                    yield return new WaitForSeconds(retryDelay);
                    retryDelay *= 2; // Exponential backoff
                }
                else
                {
                    Debug.LogError($"Final decision request failed: {request.error} (Response Code: {request.responseCode})");
                    CreateFallbackDecisions();
                    CompleteAssessment();
                    yield break;
                }
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
                Debug.LogWarning("Invalid decision response, using fallback");
                CreateFallbackDecisions();
                CompleteAssessment();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing final decisions: {e.Message}");
            CreateFallbackDecisions();
            CompleteAssessment();
        }
    }
    
    void ParseFinalDecisions(string decisionText)
    {
        EnhancementConfiguration decisions = new EnhancementConfiguration();
        decisions.sourceAssessment = "llm";
        
        string[] lines = decisionText.Split('\n');
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("VISUAL_ENABLED:"))
                decisions.visualEnabled = ParseBooleanValue(trimmedLine, "VISUAL_ENABLED:");
            else if (trimmedLine.StartsWith("NAV_LINE_WIDTH:"))
                decisions.navLineWidth = ParseFloatValue(trimmedLine, "NAV_LINE_WIDTH:", 0.4f);
            else if (trimmedLine.StartsWith("NAV_LINE_OPACITY:"))
                decisions.navLineOpacity = ParseFloatValue(trimmedLine, "NAV_LINE_OPACITY:", 80f);
            else if (trimmedLine.StartsWith("BBOX_WIDTH:"))
                decisions.bboxWidth = ParseFloatValue(trimmedLine, "BBOX_WIDTH:", 0.1f);
            else if (trimmedLine.StartsWith("BBOX_OPACITY:"))
                decisions.bboxOpacity = ParseFloatValue(trimmedLine, "BBOX_OPACITY:", 60f);
            else if (trimmedLine.StartsWith("BBOX_RANGE:"))
                decisions.bboxRange = ParseFloatValue(trimmedLine, "BBOX_RANGE:", 25f);
            else if (trimmedLine.StartsWith("AUDIO_ENABLED:"))
                decisions.audioEnabled = ParseBooleanValue(trimmedLine, "AUDIO_ENABLED:");
            else if (trimmedLine.StartsWith("AUDIO_TYPE:"))
                decisions.audioType = ParseStringValue(trimmedLine, "AUDIO_TYPE:", "TTS");
            else if (trimmedLine.StartsWith("AUDIO_INTERVAL:"))
                decisions.audioInterval = ParseFloatValue(trimmedLine, "AUDIO_INTERVAL:", 1.0f);
            else if (trimmedLine.StartsWith("AUDIO_DISTANCE:"))
                decisions.audioDistance = ParseFloatValue(trimmedLine, "AUDIO_DISTANCE:", 5.0f);
            else if (trimmedLine.StartsWith("HAPTIC_ENABLED:"))
                decisions.hapticEnabled = ParseBooleanValue(trimmedLine, "HAPTIC_ENABLED:");
            else if (trimmedLine.StartsWith("HAPTIC_CENTRAL_MIN:"))
                decisions.hapticCentralMin = ParseFloatValue(trimmedLine, "HAPTIC_CENTRAL_MIN:", 30f);
            else if (trimmedLine.StartsWith("HAPTIC_CENTRAL_MAX:"))
                decisions.hapticCentralMax = ParseFloatValue(trimmedLine, "HAPTIC_CENTRAL_MAX:", 80f);
            else if (trimmedLine.StartsWith("HAPTIC_LEFT_MIN:"))
                decisions.hapticLeftMin = ParseFloatValue(trimmedLine, "HAPTIC_LEFT_MIN:", 30f);
            else if (trimmedLine.StartsWith("HAPTIC_LEFT_MAX:"))
                decisions.hapticLeftMax = ParseFloatValue(trimmedLine, "HAPTIC_LEFT_MAX:", 80f);
            else if (trimmedLine.StartsWith("HAPTIC_RIGHT_MIN:"))
                decisions.hapticRightMin = ParseFloatValue(trimmedLine, "HAPTIC_RIGHT_MIN:", 30f);
            else if (trimmedLine.StartsWith("HAPTIC_RIGHT_MAX:"))
                decisions.hapticRightMax = ParseFloatValue(trimmedLine, "HAPTIC_RIGHT_MAX:", 80f);
            else if (trimmedLine.StartsWith("HAPTIC_OBJECT_COUNT:"))
                decisions.hapticObjectCount = (int)ParseFloatValue(trimmedLine, "HAPTIC_OBJECT_COUNT:", 2f);
        }
        
        assessmentResults.enhancementConfiguration = decisions;
        
        Debug.Log("Successfully parsed LLM enhancement decisions from real-time conversation");
        Debug.Log($"Visual: {decisions.visualEnabled}, Audio: {decisions.audioEnabled}, Haptic: {decisions.hapticEnabled}");
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
    
    string ParseStringValue(string line, string prefix, string defaultValue)
    {
        string value = line.Substring(prefix.Length).Trim();
        return string.IsNullOrEmpty(value) ? defaultValue : value;
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
                
                for (int j = i + 1; j < lines.Length; j++)
                {
                    string line = lines[j].Trim();
                    
                    // Stop if we hit another main parameter (ones that are actual config parameters)
                    if (line.StartsWith("VISUAL_ENABLED:") || 
                        line.StartsWith("NAV_LINE_") || 
                        line.StartsWith("BBOX_") || 
                        line.StartsWith("AUDIO_ENABLED:") || 
                        line.StartsWith("AUDIO_TYPE:") || 
                        line.StartsWith("AUDIO_INTERVAL:") || 
                        line.StartsWith("AUDIO_DISTANCE:") || 
                        line.StartsWith("HAPTIC_"))
                    {
                        break;
                    }
                    
                    // Include all reasoning content (numbered sections, explanations, etc.)
                    reasoning.AppendLine(line);
                }
                
                return reasoning.ToString().Trim();
            }
        }
        
        return "Reasoning based on observed navigation patterns and conversation responses.";
    }
    
    void CreateFallbackDecisions()
    {
        Debug.Log("Creating fallback enhancement decisions...");
        
        EnhancementConfiguration fallbackDecisions = new EnhancementConfiguration();
        fallbackDecisions.sourceAssessment = "llm_fallback";
        
        // Conservative fallback settings
        fallbackDecisions.visualEnabled = true;
        fallbackDecisions.navLineWidth = 0.4f;
        fallbackDecisions.navLineOpacity = 80f;
        fallbackDecisions.bboxWidth = 0.1f;
        fallbackDecisions.bboxOpacity = 60f;
        fallbackDecisions.bboxRange = 25f;
        
        fallbackDecisions.audioEnabled = true;
        fallbackDecisions.audioType = "TTS";
        fallbackDecisions.audioInterval = 2.0f;
        fallbackDecisions.audioDistance = 5.0f;
        
        fallbackDecisions.hapticEnabled = false;
        
        assessmentResults.enhancementConfiguration = fallbackDecisions;
        assessmentResults.llmReasoning = "Fallback decisions applied due to processing error during conversation.";
    }
    
    void CompleteAssessment()
    {
        assessmentResults.completed = true;
        assessmentResults.conversationDuration = Time.time;
        assessmentResults.totalQuestions = questionCount;
        
        SaveAssessmentResults();
        
        if (conversationUI != null)
        {
            conversationUI.ShowFinalResults(assessmentResults);
        }
        
        OnAssessmentCompleted?.Invoke(assessmentResults);
        
        Debug.Log("Real-time LLM Conversational Assessment completed!");
        Debug.Log($"{assessmentResults.totalQuestions} questions asked over {assessmentResults.conversationDuration:F1} seconds");
        Debug.Log($"Conversation included {conversationHistory.Count} total messages");
    }
    
    void SaveAssessmentResults()
    {
        if (SessionManager.Instance != null)
        {
            string sessionPath = SessionManager.Instance.GetSessionPath();
            string llmAssessmentPath = Path.Combine(sessionPath, "04_LLMAssessment");
            Directory.CreateDirectory(llmAssessmentPath);
            
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string jsonPath = Path.Combine(llmAssessmentPath, $"llm_realtime_assessment_{timestamp}.json");
            string reasoningPath = Path.Combine(llmAssessmentPath, $"llm_reasoning_{timestamp}.txt");
            
            // Save JSON without reasoning
            string tempReasoning = assessmentResults.llmReasoning;
            assessmentResults.llmReasoning = "[See separate reasoning file]";
            string jsonData = JsonUtility.ToJson(assessmentResults, true);
            assessmentResults.llmReasoning = tempReasoning; // Restore original
            
            File.WriteAllText(jsonPath, jsonData);
            
            // Save reasoning as separate readable text file
            if (!string.IsNullOrEmpty(tempReasoning))
            {
                File.WriteAllText(reasoningPath, tempReasoning);
            }
            
            Debug.Log($"Assessment results saved to: {jsonPath}");
            Debug.Log($"Reasoning saved to: {reasoningPath}");
            
            UserSession session = SessionManager.Instance.GetCurrentSession();
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
    
    public EnhancementAssessmentResults GetResults()
    {
        return assessmentResults;
    }
}


[System.Serializable]
public class EnhancementAssessmentResults
{
    public bool completed = false;
    public string conversationDateTime;
    public float conversationDuration;
    public int totalQuestions;
    
    public List<ChatMessage> conversationLog = new List<ChatMessage>();
    public List<string> questionsAsked = new List<string>();
    public Dictionary<string, string> extractedResponses = new Dictionary<string, string>();
    
    public EnhancementConfiguration enhancementConfiguration;
    public string llmReasoning;
}
