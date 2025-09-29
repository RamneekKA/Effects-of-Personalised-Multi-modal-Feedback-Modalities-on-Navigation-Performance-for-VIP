using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System;
using System.Linq;

/// <summary>
/// Updated Gemini Scene Pre-Analyzer with Complete Navigation Coordination
/// Properly manages dynamic objects and coordinates with CharacterControl
/// </summary>
public class GeminiScenePreAnalyzer : MonoBehaviour
{
    [Header("Integration Settings")]
    public SceneAnalysisCapture sceneCapture;
    
    [Header("Gemini Settings")]
    public string geminiApiKey = "";
    public string geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
    
    [Header("Analysis Control")]
    public bool sendSceneAnalysisOnStart = true;
    public bool includeSceneScreenshot = true;
    
    [Header("Navigation Coordination")]
    [Tooltip("Prevent navigation tracking until Gemini analysis is complete")]
    public bool blockNavigationUntilAnalysisComplete = true;
    
    [Header("Startup Behavior")]
    [Tooltip("Automatically start analysis when scene loads")]
    public bool autoStartOnSceneLoad = true;
    
    [Header("Dynamic Object Management")]
    [Tooltip("Ensure dynamic objects are active during analysis")]
    public bool ensureDynamicObjectsActive = true;
    
    [Header("UI Feedback")]
    [Tooltip("Show analysis progress to user")]
    public bool showUIFeedback = true;
    public Canvas uiCanvas;
    
    [Header("Token Management")]
    public bool useCompactPrompt = true;
    public int maxObjectsInPrompt = 20;
    
    // ANALYSIS STATE TRACKING
    private bool analysisInProgress = false;
    private bool analysisCompleted = false;
    private bool systemReady = false;
    private SceneAnalysisData currentSceneData;
    private string sceneAnalysisResponse = "";
    private string sceneAnalysisPath = "";
    
    // UI COMPONENTS
    private GameObject analysisOverlay;
    private Text statusText;
    private Text detailText;
    private Image progressBar;
    private float progressValue = 0f;
    
    // DYNAMIC OBJECT MANAGEMENT
    private DynamicObjectManager dynamicObjectManager;
    private bool dynamicObjectsWerePaused = false;
    
    // EVENT SYSTEM for coordination with CharacterControl
    public static System.Action OnPreAnalysisStarted;
    public static System.Action OnPreAnalysisCompleted;
    public static System.Action<string> OnPreAnalysisFailed;
    
    void Start()
    {
        if (sceneCapture == null)
            sceneCapture = FindObjectOfType<SceneAnalysisCapture>();
        
        // Find DynamicObjectManager
        dynamicObjectManager = FindObjectOfType<DynamicObjectManager>();
        
        // Setup UI
        if (showUIFeedback)
        {
            SetupAnalysisUI();
        }
        
        // Wait a frame to ensure all systems are initialized
        StartCoroutine(InitializeAfterFrame());
    }
    
    IEnumerator InitializeAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        
        systemReady = true;
        
        // Check if should auto-start
        if (autoStartOnSceneLoad && sendSceneAnalysisOnStart)
        {
            // Wait for SessionManager if needed
            if (SessionManager.Instance == null)
            {
                yield return new WaitUntil(() => SessionManager.Instance != null);
                yield return new WaitForSeconds(0.5f); // Give it a moment to fully set up
            }
            
            // Only start if this is a navigation trial that needs pre-analysis
            if (ShouldRunPreAnalysis())
            {
                yield return new WaitForSeconds(1f); // Give other systems time to initialize
                StartCoroutine(InitializeSceneAnalysis());
            }
            else
            {
                // Mark as completed so navigation can start
                analysisCompleted = true;
                OnPreAnalysisCompleted?.Invoke();
            }
        }
    }
    
    bool ShouldRunPreAnalysis()
    {
        if (SessionManager.Instance == null) 
        {
            return true; // Default to running it in standalone mode
        }
        
        string currentTrial = SessionManager.Instance.GetCurrentTrial();
        
        // Only run pre-analysis for navigation trials
        if (!SessionManager.Instance.IsNavigationTrial(currentTrial))
        {
            return false;
        }
        
        // Check if pre-analysis was already completed for this session
        string sessionPath = SessionManager.Instance.GetSessionPath();
        string analysisPath = Path.Combine(sessionPath, "01_SceneAnalysis", "gemini_route_pre_analysis.txt");
        
        if (File.Exists(analysisPath))
        {
            LoadExistingPreAnalysis(analysisPath);
            return false;
        }
        
        return true;
    }
    
    void LoadExistingPreAnalysis(string analysisPath)
    {
        try
        {
            sceneAnalysisResponse = File.ReadAllText(analysisPath);
            analysisCompleted = true;
        }
        catch (Exception e)
        {
            analysisCompleted = false;
        }
    }
    
    void SetupAnalysisUI()
    {
        // Find or create canvas
        if (uiCanvas == null)
        {
            uiCanvas = FindObjectOfType<Canvas>();
            if (uiCanvas == null)
            {
                GameObject canvasObj = new GameObject("AnalysisCanvas");
                uiCanvas = canvasObj.AddComponent<Canvas>();
                uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                uiCanvas.sortingOrder = 1000; // High priority
                
                // Add CanvasScaler for responsive UI
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }
        
        // Create overlay panel
        analysisOverlay = new GameObject("AnalysisOverlay");
        analysisOverlay.transform.SetParent(uiCanvas.transform, false);
        
        // Background panel
        Image background = analysisOverlay.AddComponent<Image>();
        background.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black
        
        RectTransform bgRect = analysisOverlay.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // Main content panel
        GameObject contentPanel = new GameObject("ContentPanel");
        contentPanel.transform.SetParent(analysisOverlay.transform, false);
        
        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(800, 400);
        
        // Background for content panel
        Image contentBg = contentPanel.AddComponent<Image>();
        contentBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(contentPanel.transform, false);
        
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 0.9f);
        titleRect.offsetMin = new Vector2(20, 0);
        titleRect.offsetMax = new Vector2(-20, 0);
        
        // Status text
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(contentPanel.transform, false);
        
        statusText = statusObj.AddComponent<Text>();
        statusText.text = "Initializing route analysis...";
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 24;
        statusText.color = new Color(0.8f, 0.8f, 1f);
        statusText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.5f);
        statusRect.anchorMax = new Vector2(1, 0.7f);
        statusRect.offsetMin = new Vector2(20, 0);
        statusRect.offsetMax = new Vector2(-20, 0);
        
        // Detail text
        GameObject detailObj = new GameObject("DetailText");
        detailObj.transform.SetParent(contentPanel.transform, false);
        
        detailText = detailObj.AddComponent<Text>();
        detailText.text = "Analyzing navigation route and corridor objects...";
        detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        detailText.fontSize = 18;
        detailText.color = new Color(0.7f, 0.7f, 0.7f);
        detailText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform detailRect = detailObj.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0, 0.3f);
        detailRect.anchorMax = new Vector2(1, 0.5f);
        detailRect.offsetMin = new Vector2(20, 0);
        detailRect.offsetMax = new Vector2(-20, 0);
        
        // Progress bar background
        GameObject progressBgObj = new GameObject("ProgressBackground");
        progressBgObj.transform.SetParent(contentPanel.transform, false);
        
        Image progressBgImage = progressBgObj.AddComponent<Image>();
        progressBgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        
        RectTransform progressBgRect = progressBgObj.GetComponent<RectTransform>();
        progressBgRect.anchorMin = new Vector2(0.1f, 0.15f);
        progressBgRect.anchorMax = new Vector2(0.9f, 0.25f);
        progressBgRect.offsetMin = Vector2.zero;
        progressBgRect.offsetMax = Vector2.zero;
        
        // Progress bar fill
        GameObject progressObj = new GameObject("ProgressBar");
        progressObj.transform.SetParent(progressBgObj.transform, false);
        
        progressBar = progressObj.AddComponent<Image>();
        progressBar.color = new Color(0.2f, 0.8f, 0.3f, 1f);
        progressBar.type = Image.Type.Filled;
        progressBar.fillMethod = Image.FillMethod.Horizontal;
        
        RectTransform progressRect = progressObj.GetComponent<RectTransform>();
        progressRect.anchorMin = Vector2.zero;
        progressRect.anchorMax = Vector2.one;
        progressRect.offsetMin = Vector2.zero;
        progressRect.offsetMax = Vector2.zero;
        
        // Start hidden
        analysisOverlay.SetActive(false);
        
    }
    
    void UpdateUI(string status, string detail, float progress)
    {
        if (!showUIFeedback || statusText == null) return;
        
        statusText.text = status;
        detailText.text = detail;
        progressValue = Mathf.Clamp01(progress);
        
        if (progressBar != null)
        {
            progressBar.fillAmount = progressValue;
        }
    }
    
    void ShowAnalysisUI()
    {
        if (showUIFeedback && analysisOverlay != null)
        {
            analysisOverlay.SetActive(true);
        }
    }
    
    void HideAnalysisUI()
    {
        if (showUIFeedback && analysisOverlay != null)
        {
            analysisOverlay.SetActive(false);
        }
    }
    
    IEnumerator InitializeSceneAnalysis()
    {
        
        // SHOW UI AND BLOCK NAVIGATION
        ShowAnalysisUI();
        
        analysisInProgress = true;
        analysisCompleted = false;
        OnPreAnalysisStarted?.Invoke();
        
        yield return new WaitForEndOfFrame();
        
        // Ensure dynamic objects are active for analysis
        if (ensureDynamicObjectsActive)
        {
            EnsureDynamicObjectsActive();
            yield return new WaitForSeconds(1f); // Give dynamic objects time to activate
        }
        
        // Initialize scene analysis path
        InitializeSceneAnalysisPath();
        
        // Step 1: Capture route-focused data (with dynamic objects active)        
        if (sceneCapture != null)
        {
            sceneCapture.CaptureSceneAnalysis();
            yield return new WaitForSeconds(3f); // Wait longer for dynamic objects to be captured
            LoadLatestSceneAnalysis();
        }
        else
        {
            yield return StartCoroutine(HandleAnalysisFailure("SceneAnalysisCapture not found"));
            yield break;
        }
        
        // Step 2: Send to Gemini for route-focused pre-analysis
        
        if (currentSceneData != null)
        {
            yield return StartCoroutine(SendSceneToGemini());
        }
        else
        {
            yield return StartCoroutine(HandleAnalysisFailure("No scene data captured"));
            yield break;
        }
        
        // Step 3: Complete and enable navigation
        if (analysisCompleted)
        {
            yield return new WaitForSeconds(2f); // Show completion message
            
            OnPreAnalysisCompleted?.Invoke();
        }
        else
        {
            yield return StartCoroutine(HandleAnalysisFailure("Analysis incomplete"));
        }
        
        analysisInProgress = false;
        HideAnalysisUI();
        
        // Restore dynamic objects state if needed
        if (ensureDynamicObjectsActive)
        {
            RestoreDynamicObjectsState();
        }
    }
    
    IEnumerator HandleAnalysisFailure(string reason)
    {
        yield return new WaitForSeconds(2f);
        
        
        // Still mark as completed so navigation can proceed
        analysisCompleted = true;
        OnPreAnalysisFailed?.Invoke(reason);
    }
    
    void EnsureDynamicObjectsActive()
    {
        
        if (dynamicObjectManager != null)
        {
            // Check if objects are currently paused
            if (dynamicObjectManager.AreObjectsPaused())
            {
                dynamicObjectManager.ResumeAllDynamicObjects();
                dynamicObjectsWerePaused = true;
            }
            else
            {
                dynamicObjectsWerePaused = false;
            }
            
        }
        else
        {
        }
    }
    
    void RestoreDynamicObjectsState()
    {
        if (dynamicObjectManager != null && dynamicObjectsWerePaused)
        {
            dynamicObjectManager.PauseAllDynamicObjects();
        }
    }
    
    void InitializeSceneAnalysisPath()
    {
        // Set the scene analysis path based on SessionManager
        if (SessionManager.Instance != null)
        {
            string sessionPath = SessionManager.Instance.GetSessionPath();
            sceneAnalysisPath = Path.Combine(sessionPath, "01_SceneAnalysis");
        }
        else
        {
            // Fallback to default path
            sceneAnalysisPath = Path.Combine(Application.persistentDataPath, "SceneAnalysis");
        }
        
        // Ensure directory exists
        Directory.CreateDirectory(sceneAnalysisPath);
    }
    
    void LoadLatestSceneAnalysis()
    {
        
        if (!Directory.Exists(sceneAnalysisPath))
        {
            return;
        }
        
        string jsonPath = Path.Combine(sceneAnalysisPath, "scene_analysis.json");
        
        if (!File.Exists(jsonPath))
        {
            return;
        }
        
        try
        {
            string jsonData = File.ReadAllText(jsonPath);
            currentSceneData = JsonUtility.FromJson<SceneAnalysisData>(jsonData);
            if (currentSceneData.routeWaypoints != null)
            {
            }
        }
        catch (Exception e)
        {
        }
    }
    
    IEnumerator SendSceneToGemini()
    {
        if (string.IsNullOrEmpty(geminiApiKey) || geminiApiKey == "YOUR_GEMINI_API_KEY_HERE")
        {
            yield return StartCoroutine(HandleAnalysisFailure("API key not configured"));
            yield break;
        }
        
        
        string prompt = CreateRouteFocusedPrompt();
        
        string screenshotPath = "";
        if (includeSceneScreenshot)
        {
            screenshotPath = FindSceneScreenshot();
        }
        
        yield return StartCoroutine(SendSceneAnalysisRequest(prompt, screenshotPath));
    }
    
    string CreateRouteFocusedPrompt()
    {
        if (currentSceneData == null) return "No scene data available.";
        
        StringBuilder prompt = new StringBuilder();
        
        prompt.AppendLine("You are analyzing a REAL-WORLD navigation route for spatial vision assessment.");
        prompt.AppendLine();
        prompt.AppendLine("NAVIGATION ROUTE:");
        prompt.AppendLine($"Start Point: {currentSceneData.routeStartPoint}");
        prompt.AppendLine($"End Point: {currentSceneData.routeEndPoint}");
        prompt.AppendLine($"Total Route Distance: {currentSceneData.routeTotalDistance:F1}m");
        prompt.AppendLine($"Number of Waypoints: {currentSceneData.routeWaypoints?.Count ?? 0}");
        prompt.AppendLine();
        
        if (currentSceneData.staticObjects != null && currentSceneData.staticObjects.Count > 0)
        {
            // Group objects by route segment for better spatial understanding
            var objectsBySegment = currentSceneData.staticObjects
                .Where(obj => obj.routeSegmentIndex >= 0)
                .GroupBy(obj => obj.routeSegmentIndex)
                .OrderBy(g => g.Key);
            
            prompt.AppendLine($"ROUTE ENVIRONMENT ANALYSIS ({currentSceneData.staticObjects.Count} objects within navigation corridor):");
            prompt.AppendLine();
            
            foreach (var segment in objectsBySegment)
            {
                if (segment.Key < currentSceneData.routeWaypoints.Count - 1)
                {
                    Vector3 segmentStart = currentSceneData.routeWaypoints[segment.Key];
                    Vector3 segmentEnd = currentSceneData.routeWaypoints[segment.Key + 1];
                    float segmentDistance = Vector3.Distance(segmentStart, segmentEnd);
                    
                    prompt.AppendLine($"ROUTE SEGMENT {segment.Key + 1}:");
                    prompt.AppendLine($"  From: {segmentStart} to {segmentEnd} ({segmentDistance:F1}m)");
                    
                    var segmentObjects = segment.OrderBy(obj => obj.distanceToRoute).Take(5); // Top 5 closest to route
                    
                    foreach (var obj in segmentObjects)
                    
                    prompt.AppendLine();
                }
            }
            
            // Overall object summary
            var objectTypeCounts = currentSceneData.staticObjects
                .GroupBy(obj => obj.className)
                .OrderByDescending(g => g.Count())
                .Take(10); // Top 10 most common object types
            
            prompt.AppendLine("OBJECT TYPE SUMMARY ALONG ROUTE:");
            foreach (var group in objectTypeCounts)
            {
                var closestDistance = group.Min(obj => obj.distanceToRoute);
                var avgDistance = group.Average(obj => obj.distanceToRoute);
            }
            prompt.AppendLine();
        }
        
        if (currentSceneData.dynamicObjects != null && currentSceneData.dynamicObjects.Count > 0)
        {
            prompt.AppendLine($"MOVING OBJECTS NEAR ROUTE: {currentSceneData.dynamicObjects.Count}");
            foreach (var obj in currentSceneData.dynamicObjects.Take(5)) // Top 5 dynamic objects
      
            prompt.AppendLine();
        }
        
        prompt.AppendLine("ROUTE-SPECIFIC ASSESSMENT ANALYSIS:");
        prompt.AppendLine("Analyze this navigation route for spatial vision assessment purposes:");
        prompt.AppendLine();
        prompt.AppendLine("1. ROUTE COMPLEXITY ASSESSMENT (1-5 scale):");
        prompt.AppendLine("   - Rate the navigation difficulty from start to end point");
        prompt.AppendLine("   - Identify the most challenging segments");
        prompt.AppendLine();
        prompt.AppendLine("2. KEY OBSTACLE ZONES:");
        prompt.AppendLine("   - Which objects pose the greatest navigation risks along this specific route?");
        prompt.AppendLine("   - At what points along the route do obstacles cluster?");
        prompt.AppendLine();
        prompt.AppendLine("3. SPATIAL VISION TESTING OPPORTUNITIES:");
        prompt.AppendLine("   - Which parts of this route will best reveal spatial visual capabilities?");
        prompt.AppendLine("   - What specific vision challenges (peripheral, depth, central) will this route test?");
        prompt.AppendLine();
        prompt.AppendLine("4. NAVIGATION STRATEGY PREDICTIONS:");
        prompt.AppendLine("   - Based on object positions along the route, predict common navigation challenges");
        prompt.AppendLine("   - What approach strategies might different vision profiles use?");
        prompt.AppendLine();
        prompt.AppendLine("Focus on how a person will navigate from the start point to the end point. Keep response under 1000 words.");
        
        return prompt.ToString();
    }
    
    string FindSceneScreenshot()
    {
        string screenshotPath = Path.Combine(sceneAnalysisPath, "scene_overview.png");
        
        if (File.Exists(screenshotPath))
        {
            return screenshotPath;
        }
        
        return "";
    }
    
    IEnumerator SendSceneAnalysisRequest(string prompt, string screenshotPath)
    {
        string requestJson = CreateSceneAnalysisRequestJson(prompt, screenshotPath);
        
        
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
                ProcessSceneAnalysisResponse(response);
            }
            else
            {
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                SaveScenePreAnalysis($"Request failed: {request.error}\nResponse: {request.downloadHandler.text}", true);
                analysisCompleted = true; // Allow navigation anyway
            }
        }
    }
    
    string CreateSceneAnalysisRequestJson(string prompt, string screenshotPath)
    {
        StringBuilder jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{\"contents\":[{\"parts\":[");
        
        jsonBuilder.Append($"{{\"text\":\"{EscapeJsonString(prompt)}\"}}");
        
        if (!string.IsNullOrEmpty(screenshotPath) && File.Exists(screenshotPath))
        {
            try
            {
                byte[] imageBytes = File.ReadAllBytes(screenshotPath);
                string base64Image = Convert.ToBase64String(imageBytes);
                jsonBuilder.Append($",{{\"inline_data\":{{\"mime_type\":\"image/png\",\"data\":\"{base64Image}\"}}}}");
            }
            
        }
        
        jsonBuilder.Append("]}],\"generationConfig\":{\"maxOutputTokens\":16384,\"temperature\":0.3}}");
        return jsonBuilder.ToString();
    }
    
    void ProcessSceneAnalysisResponse(string response)
    {
        
        try
        {
            GeminiResponse geminiResponse = JsonUtility.FromJson<GeminiResponse>(response);
            
            if (geminiResponse?.candidates?.Length > 0)
            {
                var candidate = geminiResponse.candidates[0];
                
                if (candidate.content?.parts?.Length > 0 && !string.IsNullOrEmpty(candidate.content.parts[0].text))
                {
                    sceneAnalysisResponse = candidate.content.parts[0].text;
                    analysisCompleted = true;
                    
                    SaveScenePreAnalysis(sceneAnalysisResponse);
                    
                }
                else if (!string.IsNullOrEmpty(candidate.finishReason))
                {
                    string errorMessage = $"Gemini response incomplete. Finish reason: {candidate.finishReason}";
                    
                    if (candidate.finishReason == "MAX_TOKENS")
                    {
                        errorMessage += "\n\nReduce 'Max Objects In Prompt' setting.";
                    }
                    
                    SaveScenePreAnalysis(errorMessage + "\n\nRaw response:\n" + response, true);
                    analysisCompleted = true;
                }
                else
                {
                    SaveScenePreAnalysis("Unexpected response format:\n" + response, true);
                    analysisCompleted = true;
                }
            }
            else
            {
                SaveScenePreAnalysis("No candidates in response:\n" + response, true);
                analysisCompleted = true;
            }
        }
        catch (Exception e)
        {
            SaveScenePreAnalysis($"Parse error: {e.Message}\n\nRaw response:\n{response}", true);
            analysisCompleted = true;
        }
    }
    
    void SaveScenePreAnalysis(string analysisText, bool isError = false)
    {
        // Ensure directory exists
        Directory.CreateDirectory(sceneAnalysisPath);
        
        string filename = isError ? "gemini_route_pre_analysis_error.txt" : "gemini_route_pre_analysis.txt";
        string filePath = Path.Combine(sceneAnalysisPath, filename);
        
        try
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine($"Gemini Route Pre-Analysis");
            output.AppendLine($"Generated: {System.DateTime.Now}");
            output.AppendLine($"Status: {(isError ? "ERROR" : "SUCCESS")}");
            output.AppendLine($"Analysis Type: ROUTE-FOCUSED ONLY");
            output.AppendLine($"Use Compact Prompt: {useCompactPrompt}");
            output.AppendLine($"Max Objects: {maxObjectsInPrompt}");
            if (currentSceneData != null && currentSceneData.routeWaypoints != null)
            {
                output.AppendLine($"Route Distance: {currentSceneData.routeTotalDistance:F1}m");
                output.AppendLine($"Route Waypoints: {currentSceneData.routeWaypoints.Count}");
                output.AppendLine($"Objects Near Route: {currentSceneData.staticObjects?.Count ?? 0}");
                output.AppendLine($"Dynamic Objects: {currentSceneData.dynamicObjects?.Count ?? 0}");
            }
            output.AppendLine("=" + new string('=', 50));
            output.AppendLine();
            output.AppendLine(analysisText);
            
            File.WriteAllText(filePath, output.ToString());
        }
        catch (Exception e)

    }
    
    string EscapeJsonString(string str)
    {
        return str.Replace("\\", "\\\\")
                  .Replace("\"", "\\\"")
                  .Replace("\r", "\\r")
                  .Replace("\n", "\\n")
                  .Replace("\t", "\\t");
    }
    
    // PUBLIC METHODS FOR COORDINATION
    public bool IsAnalysisInProgress()
    {
        return analysisInProgress;
    }
    
    public bool IsAnalysisCompleted() 
    {
        return analysisCompleted;
    }
    
    public bool ShouldBlockNavigation()
    {
        return blockNavigationUntilAnalysisComplete && analysisInProgress;
    }
    
    public bool IsSystemReady()
    {
        return systemReady;
    }
    
    public bool IsSceneAnalysisComplete()
    {
        return !string.IsNullOrEmpty(sceneAnalysisResponse);
    }
    
    public string GetScenePreAnalysis()
    {
        return sceneAnalysisResponse;
    }
    
    public SceneAnalysisData GetSceneData()
    {
        return currentSceneData;
    }
    
    // MANUAL TRIGGER METHODS
    [ContextMenu("Manual Route Pre-Analysis")]
    public void ManualScenePreAnalysis()
    {
        
        StartCoroutine(InitializeSceneAnalysis());
    }
    
    [ContextMenu("Force Complete Pre-Analysis")]
    public void ForceCompletePreAnalysis()
    {
        if (analysisInProgress)
        {
            StopAllCoroutines();
            analysisInProgress = false;
        }
        
        analysisCompleted = true;
        HideAnalysisUI();
        OnPreAnalysisCompleted?.Invoke();
    }
    
    [ContextMenu("Test Route Prompt")]
    public void TestRoutePrompt()
    {
        if (currentSceneData == null)
        {
            LoadLatestSceneAnalysis();
        }
        
        if (currentSceneData != null)
        {
            string prompt = CreateRouteFocusedPrompt();
        }
    }
    
}
