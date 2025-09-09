using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Text;
using System;
using System.Linq;

/// <summary>
/// CLEANED VERSION: Updated to work with new NavigationSession structure
/// Analyzes navigation sessions from the new 5-trial pipeline
/// CLEANED: Removed references to old AppliedEnhancements system
/// </summary>
public class GeminiNavigationAnalyzer : MonoBehaviour
{
    [Header("Gemini API Settings")]
    public string geminiApiKey = "AIzaSyDBI39ajifrB_GqCfeWIG1RBt9KEzVfuU4";
    public string geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
    
    [Header("Analysis Settings")]
    public bool autoAnalyzeOnSessionEnd = true;
    public bool includeScreenshots = true;
    public int maxScreenshotsPerAnalysis = 10; // To stay within API limits
    
    [Header("SessionManager Integration")]
    [Tooltip("Use SessionManager to find navigation data")]
    public bool useSessionManager = true;
    
    private string currentSessionPath;
    private NavigationSession currentSession;
    
    void Start()
    {
        if (string.IsNullOrEmpty(geminiApiKey) || geminiApiKey == "YOUR_GEMINI_API_KEY_HERE")
        {
            Debug.LogError("⚠️ Gemini API key not set! Get one from https://makersuite.google.com/app/apikey");
        }
    }
    
    [ContextMenu("Analyze Latest Session")]
    public void AnalyzeLatestSession()
    {
        if (useSessionManager && SessionManager.Instance != null)
        {
            AnalyzeCurrentSessionManagerSession();
        }
        else
        {
            AnalyzeLegacySession();
        }
    }
    
    void AnalyzeCurrentSessionManagerSession()
    {
        UserSession currentUserSession = SessionManager.Instance.GetCurrentSession();
        
        if (currentUserSession == null)
        {
            Debug.LogError("⚠️ No current session found in SessionManager!");
            return;
        }
        
        Debug.Log($"🔍 Looking for navigation data in SessionManager session: {currentUserSession.userID}");
        
        // Look for completed navigation trials
        List<string> completedTrials = currentUserSession.completedTrials;
        
        if (completedTrials.Count == 0)
        {
            Debug.LogError("⚠️ No completed trials found!");
            return;
        }
        
        // Get the most recent completed navigation trial
        string latestTrial = FindLatestNavigationTrial(completedTrials);
        
        if (string.IsNullOrEmpty(latestTrial))
        {
            Debug.LogError("⚠️ No navigation trials found in completed trials!");
            return;
        }
        
        // Get the path for this trial
        string trialDataPath = SessionManager.Instance.GetTrialDataPath(latestTrial);
        AnalyzeSessionFromPath(trialDataPath, latestTrial);
    }
    
    string FindLatestNavigationTrial(List<string> completedTrials)
    {
        // Navigation trials are: baseline, short_llm, short_algorithmic, long_llm, long_algorithmic
        string[] navigationTrials = { "long_llm", "long_algorithmic", "short_llm", "short_algorithmic", "baseline" };
        
        // Return the most recent (highest priority) navigation trial
        foreach (string navTrial in navigationTrials)
        {
            if (completedTrials.Contains(navTrial))
            {
                Debug.Log($"🔍✅ Found latest navigation trial: {navTrial}");
                return navTrial;
            }
        }
        
        return null;
    }
    
    void AnalyzeLegacySession()
    {
        Debug.LogWarning("⚠️ Using legacy session analysis (old folder structure)");
        
        // Check SessionManager's base path first, then fallback to persistent data path
        string baseNavigationPath;
        if (SessionManager.Instance != null)
        {
            baseNavigationPath = Path.Combine(SessionManager.Instance.GetBaseDataPath(), "Users");
        }
        else
        {
            baseNavigationPath = Path.Combine(Application.persistentDataPath, "NavigationData");
            string usersPath = Path.Combine(baseNavigationPath, "Users");
            baseNavigationPath = usersPath;
        }
        
        Debug.Log($"🔍 Looking for session folders in: {baseNavigationPath}");
        
        if (!Directory.Exists(baseNavigationPath))
        {
            Debug.LogError($"⚠️ Users directory not found at: {baseNavigationPath}");
            return;
        }
        
        // Find session folders (User001_YYYYMMDD_HHMMSS pattern)
        string[] sessionFolders = Directory.GetDirectories(baseNavigationPath)
            .Where(dir => Path.GetFileName(dir).StartsWith("User"))
            .OrderByDescending(dir => Directory.GetCreationTime(dir))
            .ToArray();
        
        if (sessionFolders.Length == 0)
        {
            Debug.LogError("⚠️ No user session folders found!");
            return;
        }
        
        string latestSessionFolder = sessionFolders[0];
        Debug.Log($"🔍✅ Found latest session folder: {Path.GetFileName(latestSessionFolder)}");
        
        // Look for navigation trials in this session
        string foundTrialPath = FindNavigationTrialInSession(latestSessionFolder);
        
        if (string.IsNullOrEmpty(foundTrialPath))
        {
            Debug.LogError("⚠️ No navigation data found in any trial folders!");
            return;
        }
        
        AnalyzeSessionFromPath(foundTrialPath, "detected");
    }
    
    string FindNavigationTrialInSession(string sessionFolder)
    {
        // Navigation trial folder mapping
        Dictionary<string, string> trialFolders = new Dictionary<string, string>
        {
            {"baseline", "02_BaselineNavigation"},
            {"short_llm", "05_EnhancedNavigation/short_llm_enhanced"},
            {"short_algorithmic", "05_EnhancedNavigation/short_algorithmic_enhanced"},
            {"long_llm", "05_EnhancedNavigation/long_llm_enhanced"},
            {"long_algorithmic", "05_EnhancedNavigation/long_algorithmic_enhanced"}
        };
        
        // Check each trial type in priority order
        string[] trialPriority = { "long_llm", "long_algorithmic", "short_llm", "short_algorithmic", "baseline" };
        
        foreach (string trialType in trialPriority)
        {
            string trialFolderPath = Path.Combine(sessionFolder, trialFolders[trialType]);
            string navigationDataPath = Path.Combine(trialFolderPath, "navigation_data.json");
            
            Debug.Log($"🔍 Checking for navigation data at: {navigationDataPath}");
            
            if (File.Exists(navigationDataPath))
            {
                Debug.Log($"✅ Found navigation data for trial: {trialType}");
                return trialFolderPath;
            }
        }
        
        Debug.LogWarning("⚠️ No navigation_data.json found in any trial folders");
        
        // List what's actually in the session folder for debugging
        Debug.Log($"🔍✅ Contents of session folder {sessionFolder}:");
        if (Directory.Exists(sessionFolder))
        {
            string[] subFolders = Directory.GetDirectories(sessionFolder);
            foreach (string folder in subFolders)
            {
                string folderName = Path.GetFileName(folder);
                Debug.Log($"  📁 {folderName}");
                
                // Check if it has navigation data
                string navDataPath = Path.Combine(folder, "navigation_data.json");
                if (File.Exists(navDataPath))
                {
                    Debug.Log($"    ✅ Has navigation_data.json");
                    return folder; // Return this folder if it has navigation data
                }
                else
                {
                    Debug.Log($"    ⚠️ No navigation_data.json");
                }
            }
        }
        
        return null;
    }
    
    public void AnalyzeSessionFromPath(string sessionPath, string trialType)
    {
        currentSessionPath = sessionPath;
        
        Debug.Log($"🔍 Analyzing session from: {sessionPath}");
        Debug.Log($"🔍✅ Trial type: {trialType}");
        
        // Load navigation data
        string jsonPath = Path.Combine(sessionPath, "navigation_data.json");
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"⚠️ Navigation data not found at: {jsonPath}");
            
            // List available files for debugging
            if (Directory.Exists(sessionPath))
            {
                string[] files = Directory.GetFiles(sessionPath);
                Debug.Log($"🔍✅ Available files in {sessionPath}:");
                foreach (string file in files)
                {
                    Debug.Log($"  📄 {Path.GetFileName(file)}");
                }
                
                // Also check subfolders
                string[] subFolders = Directory.GetDirectories(sessionPath);
                foreach (string folder in subFolders)
                {
                    string folderName = Path.GetFileName(folder);
                    Debug.Log($"  📁 {folderName}/");
                    
                    // Check if subfolder has navigation data
                    string subNavPath = Path.Combine(folder, "navigation_data.json");
                    if (File.Exists(subNavPath))
                    {
                        Debug.Log($"    ✅ Found navigation_data.json in subfolder!");
                        // Recursively analyze the subfolder instead
                        AnalyzeSessionFromPath(folder, trialType + "_subfolder");
                        return;
                    }
                }
            }
            else
            {
                Debug.LogError($"⚠️ Directory does not exist: {sessionPath}");
            }
            return;
        }
        
        string jsonData = File.ReadAllText(jsonPath);
        currentSession = JsonUtility.FromJson<NavigationSession>(jsonData);
        
        Debug.Log($"🔍✅ Starting analysis of session: {currentSession.sessionID}");
        Debug.Log($"🔍📊 Session data: {currentSession.totalDataPoints} data points over {(currentSession.endTime - currentSession.startTime):F1} seconds");
        Debug.Log($"🔍💥 Total collisions: {currentSession.totalCollisions}");
        
        StartCoroutine(PerformGeminiAnalysis());
    }
    
    IEnumerator PerformGeminiAnalysis()
    {
        // Generate analysis summary
        string behaviorSummary = GenerateBehaviorSummary();
        
        // Get key screenshots
        List<string> keyScreenshots = SelectKeyScreenshots();
        
        // Create Gemini request
        string prompt = CreateAnalysisPrompt(behaviorSummary);
        
        yield return StartCoroutine(SendToGemini(prompt, keyScreenshots));
    }
    
    string GenerateBehaviorSummary()
    {
        if (currentSession == null || currentSession.dataPoints.Count == 0)
            return "No session data available.";
        
        StringBuilder summary = new StringBuilder();
        
        // Session overview
        float duration = currentSession.endTime - currentSession.startTime;
        summary.AppendLine($"NAVIGATION SESSION ANALYSIS");
        summary.AppendLine($"Session ID: {currentSession.sessionID}");
        summary.AppendLine($"Trial Type: {currentSession.trialType ?? "Unknown"}");
        summary.AppendLine($"Route Type: {currentSession.routeType ?? "Unknown"}");
        summary.AppendLine($"Duration: {duration:F1} seconds");
        summary.AppendLine($"Data points: {currentSession.totalDataPoints}");
        summary.AppendLine();
        
        // Enhanced session metrics (from new data structure)
        if (currentSession.averageSpeed > 0)
        {
            summary.AppendLine($"ENHANCED METRICS:");
            summary.AppendLine($"Average speed: {currentSession.averageSpeed:F1}m/s");
            summary.AppendLine($"Route deviation (avg): {currentSession.averageAbsoluteDeviation:F1}m");
            summary.AppendLine($"Route deviation (max): {currentSession.maximumDeviation:F1}m");
            summary.AppendLine($"Time off route: {currentSession.timeSpentOffRoute:F1}s");
            summary.AppendLine($"Route completion: {currentSession.routeCompletionPercentage:F1}%");
            summary.AppendLine();
        }
        
        // Collision analysis
        summary.AppendLine($"COLLISION ANALYSIS:");
        summary.AppendLine($"Total collisions: {currentSession.totalCollisions}");
        
        if (currentSession.collisionsByBodyPart != null && currentSession.collisionsByBodyPart.Count > 0)
        {
            summary.AppendLine($"By body part:");
            foreach (var kvp in currentSession.collisionsByBodyPart)
            {
                summary.AppendLine($"  - {kvp.Key}: {kvp.Value} collisions");
            }
        }
        
        if (currentSession.collisionsByObjectType != null && currentSession.collisionsByObjectType.Count > 0)
        {
            summary.AppendLine($"By object type:");
            foreach (var kvp in currentSession.collisionsByObjectType)
            {
                summary.AppendLine($"  - {kvp.Key}: {kvp.Value} collisions");
            }
        }
        summary.AppendLine();
        
        // Enhancement information (simplified for new system)
        string enhancementInfo = GetEnhancementInfo();
        if (!string.IsNullOrEmpty(enhancementInfo))
        {
            summary.AppendLine($"APPLIED ENHANCEMENTS:");
            summary.AppendLine(enhancementInfo);
            summary.AppendLine();
        }
        
        // Detailed movement analysis
        if (currentSession.dataPoints != null && currentSession.dataPoints.Count > 0)
        {
            float totalDistance = 0f;
            float maxSpeed = 0f;
            int pauseCount = 0;
            List<string> problemAreas = new List<string>();
            
            for (int i = 1; i < currentSession.dataPoints.Count; i++)
            {
                NavigationDataPoint current = currentSession.dataPoints[i];
                NavigationDataPoint previous = currentSession.dataPoints[i - 1];
                
                // Calculate distance traveled
                float segmentDistance = Vector3.Distance(current.position, previous.position);
                totalDistance += segmentDistance;
                
                // Track max speed
                if (current.currentSpeed > maxSpeed)
                    maxSpeed = current.currentSpeed;
                
                // Detect pauses (very low speed for extended time)
                if (current.currentSpeed < 0.1f)
                    pauseCount++;
                
                // Enhanced collision analysis
                if (current.isCollision)
                {
                    string problemDesc = $"Collision with {current.collisionObject}";
                    if (!string.IsNullOrEmpty(current.bodyPartInvolved))
                    {
                        problemDesc += $" ({current.bodyPartInvolved})";
                    }
                    problemDesc += $" at {current.position} (t={current.timestamp:F1}s)";
                    problemAreas.Add(problemDesc);
                }
                
                // Detect hesitation near objects
                foreach (NearbyObject obj in current.nearbyObjects)
                {
                    if (obj.distance < 2f && current.currentSpeed < 0.5f)
                    {
                        problemAreas.Add($"Hesitation near {obj.className} at {obj.distance:F1}m, {obj.angle:F0}° (t={current.timestamp:F1}s)");
                    }
                }
            }
            
            float avgSpeed = totalDistance / duration;
            
            summary.AppendLine($"DETAILED MOVEMENT METRICS:");
            summary.AppendLine($"Total distance: {totalDistance:F1}m");
            summary.AppendLine($"Average speed: {avgSpeed:F1}m/s");
            summary.AppendLine($"Maximum speed: {maxSpeed:F1}m/s");
            summary.AppendLine($"Pause periods: {pauseCount} data points");
            summary.AppendLine();
            
            if (problemAreas.Count > 0)
            {
                summary.AppendLine($"POTENTIAL PROBLEM AREAS:");
                foreach (string problem in problemAreas.Take(10)) // Limit to 10 for brevity
                {
                    summary.AppendLine($"- {problem}");
                }
                if (problemAreas.Count > 10)
                {
                    summary.AppendLine($"... and {problemAreas.Count - 10} more issues");
                }
                summary.AppendLine();
            }
            
            // Spatial analysis
            summary.AppendLine($"SPATIAL INTERACTION PATTERNS:");
            Dictionary<string, List<float>> objectDistances = new Dictionary<string, List<float>>();
            
            foreach (NavigationDataPoint point in currentSession.dataPoints)
            {
                foreach (NearbyObject obj in point.nearbyObjects)
                {
                    if (!objectDistances.ContainsKey(obj.className))
                        objectDistances[obj.className] = new List<float>();
                    
                    objectDistances[obj.className].Add(obj.distance);
                }
            }
            
            foreach (var kvp in objectDistances.Take(10)) // Top 10 most encountered objects
            {
                if (kvp.Value.Count > 0)
                {
                    float minDistance = Mathf.Min(kvp.Value.ToArray());
                    float avgDistance = 0f;
                    foreach (float dist in kvp.Value) avgDistance += dist;
                    avgDistance /= kvp.Value.Count;
                    
                    summary.AppendLine($"- {kvp.Key}: closest={minDistance:F1}m, avg={avgDistance:F1}m, encounters={kvp.Value.Count}");
                }
            }
        }
        
        return summary.ToString();
    }
    
    /// <summary>
    /// Get enhancement information from the current trial type and SessionManager
    /// </summary>
    string GetEnhancementInfo()
    {
        if (SessionManager.Instance == null) return "";
        
        string trialType = currentSession.trialType;
        if (string.IsNullOrEmpty(trialType)) return "";
        
        StringBuilder enhancementInfo = new StringBuilder();
        
        // Determine what enhancements should be active based on trial type
        switch (trialType)
        {
            case "baseline":
                enhancementInfo.AppendLine("Trial Type: Baseline (no enhancements)");
                enhancementInfo.AppendLine("- Basic navigation line with default settings");
                break;
                
            case "short_algorithmic":
            case "long_algorithmic":
                enhancementInfo.AppendLine("Trial Type: Algorithmic Enhancements");
                enhancementInfo.AppendLine("- Visual enhancements based on assessment scores");
                enhancementInfo.AppendLine("- Audio feedback based on vision rating");
                enhancementInfo.AppendLine("- Haptic feedback with assessment-based intensity");
                
                // Try to get specific assessment data
                UserSession session = SessionManager.Instance.GetCurrentSession();
                if (session?.algorithmicResults != null && session.algorithmicResults.completed)
                {
                    var results = session.algorithmicResults;
                    enhancementInfo.AppendLine($"- Central vision rating: {results.centralVisionRating}/10");
                    enhancementInfo.AppendLine($"- Object clarity distance: {results.objectClarityDistance}m");
                    enhancementInfo.AppendLine($"- Preferred modality: {results.preferredModalityType}");
                }
                break;
                
            case "short_llm":
            case "long_llm":
                enhancementInfo.AppendLine("Trial Type: LLM-Based Enhancements");
                enhancementInfo.AppendLine("- Manual enhancements configured based on LLM assessment");
                enhancementInfo.AppendLine("- Custom audio, visual, and haptic settings");
                
                // Try to get LLM reasoning
                UserSession llmSession = SessionManager.Instance.GetCurrentSession();
                if (llmSession?.llmResults != null && llmSession.llmResults.completed)
                {
                    if (!string.IsNullOrEmpty(llmSession.llmResults.llmReasoning))
                    {
                        enhancementInfo.AppendLine($"- LLM Reasoning: {llmSession.llmResults.llmReasoning}");
                    }
                    if (!string.IsNullOrEmpty(llmSession.llmResults.enhancementNotes))
                    {
                        enhancementInfo.AppendLine($"- Enhancement Notes: {llmSession.llmResults.enhancementNotes}");
                    }
                }
                break;
        }
        
        return enhancementInfo.ToString();
    }
    
    List<string> SelectKeyScreenshots()
    {
        List<string> keyScreenshots = new List<string>();
        string screenshotPath = Path.Combine(currentSessionPath, "Screenshots");
        
        if (!Directory.Exists(screenshotPath))
        {
            Debug.LogWarning($"⚠️ No Screenshots folder found at: {screenshotPath}");
            return keyScreenshots;
        }
        
        // Get all screenshots
        string[] allScreenshots = Directory.GetFiles(screenshotPath, "*.png");
        
        Debug.Log($"🔍📸 Found {allScreenshots.Length} screenshots in {screenshotPath}");
        
        // Priority: collision screenshots first
        List<string> collisionScreenshots = new List<string>();
        List<string> regularScreenshots = new List<string>();
        
        foreach (string screenshot in allScreenshots)
        {
            string filename = Path.GetFileName(screenshot);
            if (filename.StartsWith("collision_"))
                collisionScreenshots.Add(screenshot);
            else
                regularScreenshots.Add(screenshot);
        }
        
        // Add collision screenshots (high priority)
        keyScreenshots.AddRange(collisionScreenshots);
        
        // Add regular screenshots, evenly spaced
        int remainingSlots = maxScreenshotsPerAnalysis - keyScreenshots.Count;
        if (remainingSlots > 0 && regularScreenshots.Count > 0)
        {
            int interval = Mathf.Max(1, regularScreenshots.Count / remainingSlots);
            for (int i = 0; i < regularScreenshots.Count && keyScreenshots.Count < maxScreenshotsPerAnalysis; i += interval)
            {
                keyScreenshots.Add(regularScreenshots[i]);
            }
        }
        
        Debug.Log($"🔍📷 Selected {keyScreenshots.Count} key screenshots for analysis ({collisionScreenshots.Count} collision, {keyScreenshots.Count - collisionScreenshots.Count} regular)");
        return keyScreenshots;
    }
    
    string CreateAnalysisPrompt(string behaviorSummary)
    {
        return $@"You are a spatial vision assessment specialist analyzing a navigation session from a first-person perspective. 

NAVIGATION DATA SUMMARY:
{behaviorSummary}

VISUAL EVIDENCE:
I'm providing screenshots from key moments during this navigation session.

ANALYSIS TASK:
Based on the movement data and visual evidence, analyze this person's spatial vision capabilities and identify patterns that suggest specific visual impairments or challenges.

Focus on:

1. SPATIAL AWARENESS PATTERNS:
   - Where did hesitations occur? (What objects, what angles, what distances?)
   - Do collisions show directional patterns? (left side, right side, central?)
   - Are there consistent avoidance distances for different object types?
   - How does route deviation pattern suggest spatial awareness issues?

2. NAVIGATION STRATEGIES:
   - How does the person approach obstacles?
   - Do they show preference for certain spatial zones?
   - What does their movement speed tell us about confidence?
   - How effective were any applied enhancements?

3. VISUAL CAPABILITY INFERENCES:
   - Based on behavior patterns, what can you infer about:
     * Central vision clarity
     * Peripheral vision (left vs right)
     * Distance vision capabilities
     * Depth perception

4. ENHANCEMENT EFFECTIVENESS:
   - If enhancements were applied, how did they affect navigation?
   - Which types of alerts/feedback seem most beneficial?
   - What adjustments would improve performance?

5. ASSESSMENT QUESTION RECOMMENDATIONS:
   - What specific questions should be asked based on observed patterns?
   - Which spatial zones need more detailed assessment?
   - What scenarios should be explored in follow-up conversation?

Provide your analysis in a structured format with specific evidence from the data and actionable recommendations for personalized assessment questions.";
    }
    
    IEnumerator SendToGemini(string prompt, List<string> screenshotPaths)
    {
        if (string.IsNullOrEmpty(geminiApiKey) || geminiApiKey == "YOUR_GEMINI_API_KEY_HERE")
        {
            Debug.LogError("⚠️ Gemini API key not configured");
            yield break;
        }
        
        // Create Gemini request JSON with proper image encoding
        string requestJson = CreateGeminiRequestJson(prompt, screenshotPaths);
        
        Debug.Log($"🔍🔄 Sending analysis request to Gemini...");
        Debug.Log($"🔍✅ Request size: {requestJson.Length} characters");
        Debug.Log($"🔍📸 Including {screenshotPaths.Count} screenshots");
        
        using (UnityWebRequest request = new UnityWebRequest(geminiApiUrl + "?key=" + geminiApiKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                ProcessGeminiResponse(response);
            }
            else
            {
                Debug.LogError($"⚠️ Gemini API request failed: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
        }
    }
    
    string CreateGeminiRequestJson(string prompt, List<string> screenshotPaths)
    {
        StringBuilder jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{\"contents\":[{\"parts\":[");
        
        // Add text prompt
        jsonBuilder.Append($"{{\"text\":\"{EscapeJsonString(prompt)}\"}}");
        
        // Add images with proper base64 encoding
        foreach (string imagePath in screenshotPaths)
        {
            if (File.Exists(imagePath))
            {
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(imagePath);
                    string base64Image = Convert.ToBase64String(imageBytes);
                    
                    jsonBuilder.Append($",{{\"inline_data\":{{\"mime_type\":\"image/png\",\"data\":\"{base64Image}\"}}}}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"⚠️ Could not encode image {imagePath}: {e.Message}");
                }
            }
        }
        
        jsonBuilder.Append("]}],\"generationConfig\":{\"maxOutputTokens\":16384,\"temperature\":0.4}}");
        
        return jsonBuilder.ToString();
    }
    
    void ProcessGeminiResponse(string response)
    {
        Debug.Log($"✅ Received Gemini analysis response");
        
        try
        {
            // Parse the JSON response to extract the actual text content
            GeminiResponse geminiResponse = JsonUtility.FromJson<GeminiResponse>(response);
            
            if (geminiResponse != null && geminiResponse.candidates != null && geminiResponse.candidates.Length > 0)
            {
                string analysisText = "";
                var firstCandidate = geminiResponse.candidates[0];
                
                if (firstCandidate.content != null && firstCandidate.content.parts != null && firstCandidate.content.parts.Length > 0)
                {
                    analysisText = firstCandidate.content.parts[0].text;
                }
                
                // Save the analysis
                string analysisPath = Path.Combine(currentSessionPath, "gemini_analysis.txt");
                
                // Create enhanced analysis file
                StringBuilder enhancedAnalysis = new StringBuilder();
                enhancedAnalysis.AppendLine("GEMINI NAVIGATION ANALYSIS");
                enhancedAnalysis.AppendLine($"Generated: {System.DateTime.Now}");
                enhancedAnalysis.AppendLine($"Session: {currentSession?.sessionID ?? "Unknown"}");
                enhancedAnalysis.AppendLine($"Trial Type: {currentSession?.trialType ?? "Unknown"}");
                enhancedAnalysis.AppendLine("=" + new string('=', 50));
                enhancedAnalysis.AppendLine();
                enhancedAnalysis.AppendLine(analysisText);
                
                File.WriteAllText(analysisPath, enhancedAnalysis.ToString());
                
                Debug.Log($"🔍📄 Analysis saved to: {analysisPath}");
                Debug.Log($"🔍✅ Analysis preview: {analysisText.Substring(0, Mathf.Min(200, analysisText.Length))}...");
                
                // TODO: Parse the analysis and feed back into VisualAssessmentChat
                // You could trigger assessment questions based on the analysis results
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"⚠️ Error parsing Gemini response: {e.Message}");
            
            // Fallback: save raw response
            string rawPath = Path.Combine(currentSessionPath, "gemini_raw_response.json");
            File.WriteAllText(rawPath, response);
            Debug.Log($"🔍💾 Raw response saved to: {rawPath}");
        }
    }
    
    string EscapeJsonString(string str)
    {
        return str.Replace("\\", "\\\\")
                  .Replace("\"", "\\\"")
                  .Replace("\r", "\\r")
                  .Replace("\n", "\\n")
                  .Replace("\t", "\\t");
    }
    
    [ContextMenu("Test Analysis Without API")]
    public void TestAnalysisLocal()
    {
        Debug.Log("🔍🧪 Testing local navigation analysis (no API call)...");
        
        if (useSessionManager && SessionManager.Instance != null)
        {
            AnalyzeCurrentSessionManagerSession();
        }
        else
        {
            AnalyzeLegacySession();
        }
    }
    
    [ContextMenu("Debug: Show Available Sessions")]
    public void DebugShowAvailableSessions()
    {
        Debug.Log("🔍 AVAILABLE SESSIONS DEBUG:");
        
        if (useSessionManager && SessionManager.Instance != null)
        {
            UserSession session = SessionManager.Instance.GetCurrentSession();
            Debug.Log($"🔍✅ Current session: {session.userID}_{session.sessionDateTime}");
            Debug.Log($"🔍🔄 Current trial: {session.currentTrial}");
            Debug.Log($"✅ Completed trials: {string.Join(", ", session.completedTrials)}");
            
            foreach (string trial in session.completedTrials)
            {
                if (SessionManager.Instance.IsNavigationTrial(trial))
                {
                    string trialPath = SessionManager.Instance.GetTrialDataPath(trial);
                    string jsonPath = Path.Combine(trialPath, "navigation_data.json");
                    bool exists = File.Exists(jsonPath);
                    Debug.Log($"  🔍 {trial}: {(exists ? "✅ HAS DATA" : "⚠️ NO DATA")} at {trialPath}");
                }
            }
        }
        else
        {
            Debug.Log("🔍🔧 Using legacy session detection");
            
            string baseNavigationPath;
            if (SessionManager.Instance != null)
            {
                baseNavigationPath = Path.Combine(SessionManager.Instance.GetBaseDataPath(), "Users");
            }
            else
            {
                baseNavigationPath = Path.Combine(Application.persistentDataPath, "NavigationData");
                baseNavigationPath = Path.Combine(baseNavigationPath, "Users");
            }
            
            Debug.Log($"🔍 Base path: {baseNavigationPath}");
            
            if (Directory.Exists(baseNavigationPath))
            {
                string[] folders = Directory.GetDirectories(baseNavigationPath);
                Debug.Log($"🔍📁 Found {folders.Length} session folders:");
                foreach (string folder in folders)
                {
                    string folderName = Path.GetFileName(folder);
                    Debug.Log($"  📁 {folderName}");
                    
                    // Check each trial folder for navigation data
                    string foundTrial = FindNavigationTrialInSession(folder);
                    if (!string.IsNullOrEmpty(foundTrial))
                    {
                        Debug.Log($"    ✅ Has navigation data in: {Path.GetFileName(foundTrial)}");
                    }
                    else
                    {
                        Debug.Log($"    ⚠️ No navigation data found");
                    }
                }
            }
            else
            {
                Debug.LogError($"⚠️ Users directory not found at: {baseNavigationPath}");
            }
        }
    }
}

// Data structures for parsing Gemini API responses
[System.Serializable]
public class GeminiResponse
{
    public GeminiCandidate[] candidates;
}

[System.Serializable]
public class GeminiCandidate
{
    public GeminiContent content;
    public string finishReason;
}

[System.Serializable]
public class GeminiContent
{
    public GeminiPart[] parts;
}

[System.Serializable]
public class GeminiPart
{
    public string text;
}