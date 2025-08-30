using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Simplified conversational UI - clean, reliable, and easy to maintain
/// No complex layouts - just simple positioning that works
/// </summary>
public class ConversationalUI : MonoBehaviour
{
    [Header("UI Settings")]
    public int systemMessageFontSize = 22;
    public int userMessageFontSize = 20;
    public int inputTextFontSize = 18;
    public int buttonTextFontSize = 16;
    
    // UI Components
    private Canvas mainCanvas;
    private GameObject mainPanel;
    private ScrollRect messageScrollRect;
    private Transform messageContainer;
    private InputField inputField;
    private Button submitButton;
    private Button skipButton;
    
    // State management
    private bool waitingForInput = false;
    private bool isSubmittingResponse = false; // Prevent double submission
    private List<GameObject> messageObjects = new List<GameObject>();
    
    // Events
    public System.Action<string> OnUserSubmittedResponse;
    
    void Start()
    {
        CreateSimpleUI();
    }
    
    void CreateSimpleUI()
    {
        Debug.Log("🎨 Creating simplified conversational interface...");
        
        CreateEventSystem();
        CreateCanvas();
        CreateMainPanel();
        CreateMessageArea();
        CreateInputArea();
        
        // No test message - let the conversation system handle all messages
        
        Debug.Log("✅ Simple conversational interface created");
    }
    
    // Test methods removed - conversation system handles all messages
    
    void CreateEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
    }
    
    void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("ConversationCanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
    }
    
    void CreateMainPanel()
    {
        mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(mainCanvas.transform, false);
        
        // Dark background
        Image panelImage = mainPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        // Full screen with some margins
        RectTransform panelRect = mainPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.05f);
        panelRect.anchorMax = new Vector2(0.95f, 0.95f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
    }
    
    void CreateMessageArea()
    {
        // Message scroll area - takes up most of the screen
        GameObject scrollArea = new GameObject("MessageScrollArea");
        scrollArea.transform.SetParent(mainPanel.transform, false);
        
        RectTransform scrollRect = scrollArea.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.02f, 0.2f);  // Leave space at bottom for input
        scrollRect.anchorMax = new Vector2(0.98f, 0.98f); // Leave small margin at top
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;
        
        // Add scroll rect component
        messageScrollRect = scrollArea.AddComponent<ScrollRect>();
        messageScrollRect.horizontal = false;
        messageScrollRect.vertical = true;
        messageScrollRect.movementType = ScrollRect.MovementType.Clamped;
        messageScrollRect.scrollSensitivity = 40f;
        
        // Scrollbar
        CreateScrollbar(scrollArea);
        
        // Content container
        GameObject content = new GameObject("MessageContent");
        content.transform.SetParent(scrollArea.transform, false);
        
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        
        // Simple vertical layout with better spacing
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 25f; // Increased spacing between messages
        layout.padding = new RectOffset(30, 30, 30, 30);
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperLeft;
        
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        messageScrollRect.content = contentRect;
        messageContainer = content.transform;
    }
    
    void CreateScrollbar(GameObject parent)
    {
        GameObject scrollbarObj = new GameObject("Scrollbar");
        scrollbarObj.transform.SetParent(parent.transform, false);
        
        RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.offsetMin = new Vector2(-20, 0);
        scrollbarRect.offsetMax = new Vector2(0, 0);
        
        Image scrollbarBg = scrollbarObj.AddComponent<Image>();
        scrollbarBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        
        // Scrollbar handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(scrollbarObj.transform, false);
        
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;
        
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        
        messageScrollRect.verticalScrollbar = scrollbar;
    }
    
    void CreateInputArea()
    {
        // Fixed input area at bottom
        GameObject inputArea = new GameObject("InputArea");
        inputArea.transform.SetParent(mainPanel.transform, false);
        
        RectTransform inputRect = inputArea.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.02f, 0.02f);
        inputRect.anchorMax = new Vector2(0.98f, 0.18f);  // Fixed height at bottom
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;
        
        // Input field background
        Image inputBg = inputArea.AddComponent<Image>();
        inputBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        
        CreateInputField(inputArea);
        CreateButtons(inputArea);
    }
    
    void CreateInputField(GameObject parent)
    {
        GameObject inputObj = new GameObject("InputField");
        inputObj.transform.SetParent(parent.transform, false);
        
        // Position input field - leave space for buttons on the right
        RectTransform inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.02f, 0.1f);
        inputRect.anchorMax = new Vector2(0.75f, 0.9f);  // 75% width, leave 25% for buttons
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;
        
        // White background
        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = Color.white;
        
        // Input field component
        inputField = inputObj.AddComponent<InputField>();
        inputField.lineType = InputField.LineType.MultiLineNewline;
        inputField.characterLimit = 1000;
        inputField.targetGraphic = inputBg;
        
        // Text component
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(15, 10);
        textRect.offsetMax = new Vector2(-15, -10);
        
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = "";
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = inputTextFontSize;
        textComponent.color = Color.black;
        textComponent.alignment = TextAnchor.UpperLeft;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        
        // Placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputObj.transform, false);
        
        RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(15, 10);
        placeholderRect.offsetMax = new Vector2(-15, -10);
        
        Text placeholderText = placeholderObj.AddComponent<Text>();
        placeholderText.text = "Type your response here...";
        placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholderText.fontSize = inputTextFontSize;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholderText.alignment = TextAnchor.UpperLeft;
        placeholderText.fontStyle = FontStyle.Italic;
        
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholderText;
        
        // Submit on Enter (allow Shift+Enter for new lines)
        inputField.onEndEdit.AddListener((string value) => {
            // Don't auto-submit on Enter in input field to prevent double submission
            // Let the Update() method handle Enter key detection
        });
    }
    
    void CreateButtons(GameObject parent)
    {
        // Submit button - now takes more space since no skip button
        submitButton = CreateButton(parent, "Submit", new Vector2(0.77f, 0.25f), new Vector2(0.96f, 0.75f), new Color(0.2f, 0.6f, 0.2f));
        submitButton.onClick.AddListener(SubmitResponse);
        
        // No skip button
    }
    
    Button CreateButton(GameObject parent, string text, Vector2 anchorMin, Vector2 anchorMax, Color buttonColor)
    {
        GameObject buttonObj = new GameObject(text + "Button");
        buttonObj.transform.SetParent(parent.transform, false);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = buttonColor;
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        
        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = buttonTextFontSize;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.fontStyle = FontStyle.Bold;
        
        return button;
    }
    
    // PUBLIC METHODS
    public void ShowSystemMessage(string message)
    {
        StartCoroutine(DisplaySystemMessage(message));
    }
    
    public void ShowUserMessage(string message)
    {
        Debug.Log($"📝 CREATING USER MESSAGE: {message}");
        Debug.Log($"📊 Current message count: {messageObjects.Count}");
        
        CreateUserMessage(message);
        ScrollToBottom();
    }
    
    public void SetWaitingForInput(bool waiting)
    {
        Debug.Log($"🎯 SetWaitingForInput called: {waiting}");
        waitingForInput = waiting;
        
        if (inputField != null)
        {
            inputField.interactable = waiting;
            if (waiting)
            {
                inputField.text = "";
                inputField.ActivateInputField();
                isSubmittingResponse = false; // Reset submission lock when starting to wait
            }
        }
        
        if (submitButton != null)
            submitButton.interactable = waiting;
        // No skip button to manage
    }
    
    public void ShowFinalResults(LLMAssessmentResults results)
    {
        StartCoroutine(DisplayFinalResults(results));
    }
    
    // PRIVATE METHODS
    IEnumerator DisplaySystemMessage(string message)
    {
        yield return new WaitForSeconds(0.5f); // Longer pause before system messages
        CreateSystemMessage(message);
        yield return new WaitForSeconds(0.3f); // Brief pause after creation
        ScrollToBottom();
    }
    
    // Typing effect coroutine
    IEnumerator TypeText(Text textComponent, string fullText)
    {
        textComponent.text = "";
        
        for (int i = 0; i <= fullText.Length; i++)
        {
            textComponent.text = fullText.Substring(0, i);
            
            // Update layout and scroll as text appears
            if (i % 10 == 0) // Every 10 characters, update layout
            {
                Canvas.ForceUpdateCanvases();
                ScrollToBottom();
            }
            
            yield return new WaitForSeconds(0.02f); // Typing speed
        }
        
        // Final layout update
        Canvas.ForceUpdateCanvases();
        ScrollToBottom();
    }
    
    void CreateSystemMessage(string message)
    {
        GameObject messageObj = new GameObject("SystemMessage");
        messageObj.transform.SetParent(messageContainer, false);
        
        // Simple layout element with more spacing
        LayoutElement layout = messageObj.AddComponent<LayoutElement>();
        layout.minHeight = 40;
        layout.flexibleHeight = -1;
        layout.preferredHeight = -1;
        
        // No background - clean text only
        
        // Text component directly on the message object
        Text textComponent = messageObj.AddComponent<Text>();
        textComponent.text = ""; // Start empty for typing effect
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = systemMessageFontSize;
        textComponent.color = new Color(0.9f, 0.9f, 0.9f, 1f); // Light gray text
        textComponent.alignment = TextAnchor.UpperLeft;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        
        // Position text with padding
        RectTransform textRect = textComponent.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 10);
        textRect.offsetMax = new Vector2(-20, -10);
        
        // Auto-size to fit content
        ContentSizeFitter fitter = messageObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        messageObjects.Add(messageObj);
        
        // Start typing effect
        StartCoroutine(TypeText(textComponent, $"SYSTEM: {message}"));
    }
    
    void CreateUserMessage(string message)
    {
        GameObject messageObj = new GameObject("UserMessage");
        messageObj.transform.SetParent(messageContainer, false);
        
        // Simple layout element with more spacing
        LayoutElement layout = messageObj.AddComponent<LayoutElement>();
        layout.minHeight = 40;
        layout.flexibleHeight = -1;
        layout.preferredHeight = -1;
        
        // No background - clean text only
        
        // Text component directly on the message object
        Text textComponent = messageObj.AddComponent<Text>();
        textComponent.text = $"YOU: {message}"; // User messages appear instantly
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = userMessageFontSize;
        textComponent.color = new Color(0.7f, 0.9f, 1f, 1f); // Light blue text
        textComponent.alignment = TextAnchor.UpperLeft;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        
        // Position text with padding
        RectTransform textRect = textComponent.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 10);
        textRect.offsetMax = new Vector2(-20, -10);
        
        // Auto-size to fit content
        ContentSizeFitter fitter = messageObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        messageObjects.Add(messageObj);
    }
    
    void ScrollToBottom()
    {
        if (messageScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            messageScrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    void SubmitResponse()
    {
        if (!waitingForInput || string.IsNullOrEmpty(inputField.text.Trim()) || isSubmittingResponse)
            return;
        
        isSubmittingResponse = true; // Lock to prevent double submission
        
        string userResponse = inputField.text.Trim();
        Debug.Log($"👤 SUBMITTING: {userResponse}");
        
        // Clear input immediately
        inputField.text = "";
        SetWaitingForInput(false);
        
        // Show user message once
        ShowUserMessage(userResponse);
        
        // Notify listeners once
        OnUserSubmittedResponse?.Invoke(userResponse);
        
        Debug.Log($"👤 User submission complete: {userResponse}");
        
        // Reset submission lock after a brief delay
        StartCoroutine(ResetSubmissionLock());
    }
    
    IEnumerator ResetSubmissionLock()
    {
        yield return new WaitForSeconds(0.1f);
        isSubmittingResponse = false;
    }
    
    void SkipQuestion()
    {
        if (!waitingForInput) return;
        
        ShowUserMessage("[Skipped this question]");
        inputField.text = "";
        SetWaitingForInput(false);
        
        OnUserSubmittedResponse?.Invoke("SKIPPED");
        
        Debug.Log("⭐ User skipped question");
    }
    
    IEnumerator DisplayFinalResults(LLMAssessmentResults results)
    {
        yield return new WaitForSeconds(1f);
        
        ShowSystemMessage("Assessment complete! Here are your personalized navigation enhancement recommendations:");
        
        yield return new WaitForSeconds(2f);
        
        string resultsText = FormatResults(results);
        ShowSystemMessage(resultsText);
        
        yield return new WaitForSeconds(1f);
        
        if (!string.IsNullOrEmpty(results.llmReasoning))
        {
            ShowSystemMessage($"Reasoning: {results.llmReasoning}");
        }
    }
    
    string FormatResults(LLMAssessmentResults results)
    {
        if (results.llmDecisions == null) return "No decisions available.";
        
        var decisions = results.llmDecisions;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine("🎯 PERSONALIZED NAVIGATION ENHANCEMENTS");
        sb.AppendLine();
        
        sb.AppendLine("OBJECT PRIORITIES:");
        if (decisions.highPriorityObjects.Count > 0)
            sb.AppendLine($"🔴 High: {string.Join(", ", decisions.highPriorityObjects)}");
        if (decisions.mediumPriorityObjects.Count > 0)
            sb.AppendLine($"🟡 Medium: {string.Join(", ", decisions.mediumPriorityObjects)}");
        if (decisions.lowPriorityObjects.Count > 0)
            sb.AppendLine($"🟢 Low: {string.Join(", ", decisions.lowPriorityObjects)}");
        
        sb.AppendLine();
        sb.AppendLine("ALERT SETTINGS:");
        sb.AppendLine($"🔊 Audio: {(decisions.useAudio ? "ON" : "OFF")}");
        sb.AppendLine($"📳 Haptic: {(decisions.useHaptics ? "ON" : "OFF")}");
        sb.AppendLine($"🎵 Spearcons: {(decisions.useSpearcons ? "ON" : "OFF")}");
        sb.AppendLine($"👀 Visual: {(decisions.useVisualEnhancements ? "ON" : "OFF")}");
        
        sb.AppendLine();
        sb.AppendLine($"📏 Alert Distance: {decisions.alertDistance:F1}m");
        sb.AppendLine($"⚠️ Warning Distance: {decisions.warningDistance:F1}m");
        sb.AppendLine($"🛑 Critical Distance: {decisions.criticalDistance:F1}m");
        
        if (decisions.recommendSlowerSpeed)
        {
            sb.AppendLine($"🌊 Recommended Speed: {(decisions.recommendedSpeedMultiplier * 100):F0}%");
        }
        
        return sb.ToString();
    }
    
    public void ClearMessages()
    {
        foreach (GameObject messageObj in messageObjects)
        {
            if (messageObj != null)
                DestroyImmediate(messageObj);
        }
        messageObjects.Clear();
    }
    
    // Utility methods
    public bool IsWaitingForInput() => waitingForInput;
    public int GetMessageCount() => messageObjects.Count;
    
    // Debug methods
    [ContextMenu("Debug: Test System Message")]
    public void DebugTestSystemMessage()
    {
        ShowSystemMessage("This is a test system message to check the simplified UI formatting and display.");
    }
    
    [ContextMenu("Debug: Test User Message")]
    public void DebugTestUserMessage()
    {
        ShowUserMessage("This is a test user response to see how user messages appear in the simplified interface.");
    }
    
    [ContextMenu("Debug: Clear All Messages")]
    public void DebugClearMessages()
    {
        ClearMessages();
    }
    
    void Update()
    {
        // Enter to submit (no ESC skip functionality)
        if (Input.GetKeyDown(KeyCode.Return) && !Input.GetKey(KeyCode.LeftShift) && waitingForInput && !isSubmittingResponse)
        {
            Debug.Log("🔑 Enter key detected, submitting response");
            SubmitResponse();
        }
    }
    
    void OnDestroy()
    {
        if (submitButton != null)
            submitButton.onClick.RemoveAllListeners();
        if (inputField != null)
            inputField.onEndEdit.RemoveAllListeners();
    }
}