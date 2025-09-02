using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.IO;

public class AlgorithmicAssessmentUI : MonoBehaviour
{
    [Header("User Session Settings")]
    [Tooltip("Enter the User ID (e.g., User001) to save to correct session folder")]
    public string userID = "User001";
    
    [Header("UI Components - Will be created at runtime")]
    private Canvas assessmentCanvas;
    private GameObject questionPanel;
    private Text questionText;
    private InputField numberInput;
    private Button yesButton, noButton;
    private List<Button> colorButtons = new List<Button>();
    private List<Button> modalityButtons = new List<Button>();
    private Button nextButton;
    private Text errorText; // Error message display
    
    [Header("Assessment Data")]
    public AlgorithmicAssessmentResults assessmentResults;
    
    // Question management
    private int currentQuestionIndex = 0;
    private List<AssessmentQuestion> questions;
    private bool isShowingColorSelection = false;
    private bool isShowingModalitySelection = false;
    private string selectedColorType = "";
    private string selectedModalityType = "";
    
    // Input validation
    private bool hasValidInput = false;
    private bool selectedYesNoAnswer = false;
    private bool yesNoAnswerValue = false;
    
    void Start()
    {
        SetupQuestions();
        CreateEventSystem();
        CreateUI();
        ShowCurrentQuestion();
    }
    
    void CreateEventSystem()
    {
        // Check if EventSystem already exists
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("EventSystem created");
        }
        else
        {
            Debug.Log("EventSystem already exists");
        }
    }
    
    void SetupQuestions()
    {
        questions = new List<AssessmentQuestion>
        {
            new AssessmentQuestion("On a scale of 1-10, how would you rate your central vision? (1 = very low, 10 = very high)", QuestionType.NumberInput),
            new AssessmentQuestion("On a scale of 1-10, how would you rate your left peripheral vision? (1 = very low, 10 = very high)", QuestionType.NumberInput),
            new AssessmentQuestion("On a scale of 1-10, how would you rate your right peripheral vision? (1 = very low, 10 = very high)", QuestionType.NumberInput),
            new AssessmentQuestion("At what distance do objects start to become unclear or hard to identify? (1-10 metres)", QuestionType.NumberInput), // NEW QUESTION 1
            new AssessmentQuestion("How close do objects need to be before you can reliably avoid them? (0.5-5 metres)", QuestionType.NumberInput), // NEW QUESTION 2
            new AssessmentQuestion("What type of colour blindness do you have?", QuestionType.ColorBlind),
            new AssessmentQuestion("On a scale of 1-10, how sensitive are you to bright lights? (1 = not sensitive at all, 10 = extremely sensitive)", QuestionType.NumberInput),
            new AssessmentQuestion("On a scale of 1-10, how much do you struggle to see in low-light conditions? (1 = no struggle, 10 = extreme struggle)", QuestionType.NumberInput),
            new AssessmentQuestion("What is your preference in terms of output modality?", QuestionType.ModalityPreference)
        };
        
        // Initialize results
        assessmentResults = new AlgorithmicAssessmentResults();
        assessmentResults.assessmentDateTime = System.DateTime.Now.ToString();
    }
    
    void CreateUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("AssessmentCanvas");
        assessmentCanvas = canvasObj.AddComponent<Canvas>();
        assessmentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        assessmentCanvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create main question panel
        CreateQuestionPanel();
        CreateQuestionComponents();
    }
    
    void CreateQuestionPanel()
    {
        questionPanel = new GameObject("QuestionPanel");
        questionPanel.transform.SetParent(assessmentCanvas.transform, false);
        
        Image panelImage = questionPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        RectTransform panelRect = questionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.2f);
        panelRect.anchorMax = new Vector2(0.8f, 0.8f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
    }
    
    void CreateQuestionComponents()
    {
        // Question Text
        GameObject questionTextObj = new GameObject("QuestionText");
        questionTextObj.transform.SetParent(questionPanel.transform, false);
        
        questionText = questionTextObj.AddComponent<Text>();
        questionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        questionText.fontSize = 28; // Slightly smaller to accommodate longer text
        questionText.color = Color.white;
        questionText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform questionTextRect = questionTextObj.GetComponent<RectTransform>();
        questionTextRect.anchorMin = new Vector2(0.1f, 0.7f);
        questionTextRect.anchorMax = new Vector2(0.9f, 0.9f);
        questionTextRect.offsetMin = Vector2.zero;
        questionTextRect.offsetMax = Vector2.zero;
        
        // Create all input components (hide initially)
        CreateNumberInput();
        CreateYesNoButtons();
        CreateColorButtons();
        CreateModalityButtons();
        CreateErrorText();
        CreateNextButton();
    }
    
    void CreateNumberInput()
    {
        GameObject inputObj = new GameObject("NumberInput");
        inputObj.transform.SetParent(questionPanel.transform, false);
        
        // Background image for input field
        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = new Color(1f, 1f, 1f, 1f); // White background
        inputBg.type = Image.Type.Sliced;
        
        numberInput = inputObj.AddComponent<InputField>();
        numberInput.contentType = InputField.ContentType.DecimalNumber; // Allow decimal for distance questions
        numberInput.characterLimit = 4;
        
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.4f, 0.45f);
        inputRect.anchorMax = new Vector2(0.6f, 0.55f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;
        
        // Create text component for input field display
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform, false);
        
        Text inputText = textObj.AddComponent<Text>();
        inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputText.fontSize = 40;
        inputText.color = Color.black; // Black text on white background
        inputText.alignment = TextAnchor.MiddleCenter;
        inputText.supportRichText = false;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 2);
        textRect.offsetMax = new Vector2(-10, -2);
        
        // Create placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputObj.transform, false);
        
        Text placeholderText = placeholderObj.AddComponent<Text>();
        placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholderText.fontSize = 20;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray placeholder
        placeholderText.alignment = TextAnchor.MiddleCenter;
        placeholderText.text = "Enter value";
        
        RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 2);
        placeholderRect.offsetMax = new Vector2(-10, -2);
        
        // Assign components to input field
        numberInput.targetGraphic = inputBg;
        numberInput.textComponent = inputText;
        numberInput.placeholder = placeholderText;
        
        // Add input validation with real-time checking
        numberInput.onValueChanged.AddListener(ValidateCurrentInput);
        numberInput.onEndEdit.AddListener(ValidateNumberInput);
        
        inputObj.SetActive(false);
    }
    
    void ValidateCurrentInput(string input)
    {
        ValidateInput(input, false); // Real-time validation without auto-correction
    }
    
    void ValidateNumberInput(string input)
    {
        ValidateInput(input, true); // Final validation with auto-correction if needed
    }
    
    void ValidateInput(string input, bool allowAutoCorrect)
    {
        ClearErrorMessage();
        hasValidInput = false;
        
        if (string.IsNullOrEmpty(input))
        {
            // Only show error if user has previously typed something (not on initial load)
            if (allowAutoCorrect) // This indicates the user finished editing (onEndEdit)
            {
                ShowErrorMessage("Please enter a value");
            }
            UpdateNextButtonState();
            return;
        }
        
        if (float.TryParse(input, out float value))
        {
            // Different validation based on current question
            if (currentQuestionIndex == 3) // Object clarity distance question (1-10m)
            {
                if (value < 1 || value > 10)
                {
                    ShowErrorMessage("Please enter a distance between 1 and 10 metres");
                    if (allowAutoCorrect)
                    {
                        numberInput.text = Mathf.Clamp(value, 1, 10).ToString("F1");
                        hasValidInput = true;
                        ClearErrorMessage();
                    }
                }
                else
                {
                    hasValidInput = true;
                }
            }
            else if (currentQuestionIndex == 4) // Avoidance distance question (0.5-5m)
            {
                if (value < 0.5f || value > 5)
                {
                    ShowErrorMessage("Please enter a distance between 0.5 and 5 metres");
                    if (allowAutoCorrect)
                    {
                        numberInput.text = Mathf.Clamp(value, 0.5f, 5f).ToString("F1");
                        hasValidInput = true;
                        ClearErrorMessage();
                    }
                }
                else
                {
                    hasValidInput = true;
                }
            }
            else // Scale questions (1-10)
            {
                int intValue = Mathf.RoundToInt(value);
                if (intValue < 1 || intValue > 10)
                {
                    ShowErrorMessage("Please enter a number between 1 and 10");
                    if (allowAutoCorrect)
                    {
                        numberInput.text = Mathf.Clamp(intValue, 1, 10).ToString();
                        hasValidInput = true;
                        ClearErrorMessage();
                    }
                }
                else
                {
                    hasValidInput = true;
                    if (allowAutoCorrect && value != intValue)
                    {
                        numberInput.text = intValue.ToString(); // Round to integer for scale questions
                    }
                }
            }
        }
        else
        {
            ShowErrorMessage("Please enter a valid number");
        }
        
        UpdateNextButtonState();
    }
    
    void CreateYesNoButtons()
    {
        // Yes Button
        yesButton = CreateButton("YesButton", "YES", new Vector2(0.2f, 0.4f), new Vector2(0.45f, 0.52f), Color.blue);
        yesButton.onClick.AddListener(() => {
            Debug.Log("YES button clicked!");
            SelectYesNoAnswer(true);
        });
        
        // No Button  
        noButton = CreateButton("NoButton", "NO", new Vector2(0.55f, 0.4f), new Vector2(0.8f, 0.52f), Color.blue);
        noButton.onClick.AddListener(() => {
            Debug.Log("NO button clicked!");
            SelectYesNoAnswer(false);
        });

        // Make Yes/No button text bigger
        yesButton.GetComponentInChildren<Text>().fontSize = 28;
        noButton.GetComponentInChildren<Text>().fontSize = 28;
        
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
    }
    
    void CreateColorButtons()
    {
        colorButtons.Clear();
        
        string[] colorOptions = { "None", "Red", "Green", "Blue", "Red-Green", "Complete" };
        Color[] buttonColors = { 
            Color.blue, 
            Color.blue, 
            Color.blue, 
            Color.blue, 
            Color.blue, 
            Color.blue 
        };
        
        // Arrange buttons in 2 rows of 3
        for (int i = 0; i < colorOptions.Length; i++)
        {
            // Calculate position (2 rows, 3 columns)
            int row = i / 3;
            int col = i % 3;
            
            float buttonWidth = 0.25f;
            float buttonHeight = 0.12f;
            float startX = 0.1f;
            float startY = 0.55f;
            float spacingX = 0.27f;
            float spacingY = 0.15f;
            
            Vector2 anchorMin = new Vector2(startX + col * spacingX, startY - row * spacingY);
            Vector2 anchorMax = new Vector2(startX + col * spacingX + buttonWidth, startY - row * spacingY + buttonHeight);
            
            string colorType = colorOptions[i];
            Button colorButton = CreateButton($"Color_{colorType}", colorType, anchorMin, anchorMax, buttonColors[i]);
            
            // Make text color contrasting and bigger
            Text buttonText = colorButton.GetComponentInChildren<Text>();
            buttonText.fontSize = 24;
            buttonText.color = Color.white;
            
            colorButton.onClick.AddListener(() => {
                Debug.Log($"Color button clicked: {colorType}");
                SelectColorType(colorType);
            });
            
            colorButtons.Add(colorButton);
            colorButton.gameObject.SetActive(false);
        }
    }
    
    void CreateModalityButtons()
    {
        modalityButtons.Clear();
        
        string[] modalityOptions = { "Audio", "Haptics", "Visual" };
        
        // Arrange buttons horizontally
        for (int i = 0; i < modalityOptions.Length; i++)
        {
            float buttonWidth = 0.25f;
            float buttonHeight = 0.12f;
            float startX = 0.125f;
            float startY = 0.45f;
            float spacingX = 0.275f;
            
            Vector2 anchorMin = new Vector2(startX + i * spacingX, startY);
            Vector2 anchorMax = new Vector2(startX + i * spacingX + buttonWidth, startY + buttonHeight);
            
            string modalityType = modalityOptions[i];
            Button modalityButton = CreateButton($"Modality_{modalityType}", modalityType, anchorMin, anchorMax, Color.blue);
            
            // Make text color contrasting and bigger
            Text buttonText = modalityButton.GetComponentInChildren<Text>();
            buttonText.fontSize = 24;
            buttonText.color = Color.white;
            
            modalityButton.onClick.AddListener(() => {
                Debug.Log($"Modality button clicked: {modalityType}");
                SelectModalityType(modalityType);
            });
            
            modalityButtons.Add(modalityButton);
            modalityButton.gameObject.SetActive(false);
        }
    }
    
    void CreateErrorText()
    {
        GameObject errorTextObj = new GameObject("ErrorText");
        errorTextObj.transform.SetParent(questionPanel.transform, false);
        
        errorText = errorTextObj.AddComponent<Text>();
        errorText.text = "";
        errorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        errorText.fontSize = 20;
        errorText.color = Color.red;
        errorText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform errorTextRect = errorTextObj.GetComponent<RectTransform>();
        errorTextRect.anchorMin = new Vector2(0.1f, 0.28f);
        errorTextRect.anchorMax = new Vector2(0.9f, 0.35f);
        errorTextRect.offsetMin = Vector2.zero;
        errorTextRect.offsetMax = Vector2.zero;
        
        errorTextObj.SetActive(false);
    }
    
    void ShowErrorMessage(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
    }
    
    void ClearErrorMessage()
    {
        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }
    }
    
    void UpdateNextButtonState()
    {
        bool canProceed = false;
        
        // Check based on current question type
        AssessmentQuestion currentQuestion = questions[currentQuestionIndex];
        
        switch (currentQuestion.type)
        {
            case QuestionType.NumberInput:
                canProceed = hasValidInput;
                break;
            case QuestionType.YesNo:
                canProceed = selectedYesNoAnswer;
                break;
            case QuestionType.ColorBlind:
                canProceed = !string.IsNullOrEmpty(selectedColorType);
                break;
            case QuestionType.ModalityPreference:
                canProceed = !string.IsNullOrEmpty(selectedModalityType);
                break;
        }
        
        // Update button appearance and interactability
        if (nextButton != null)
        {
            nextButton.interactable = canProceed;
            Image buttonImage = nextButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                // Grey out button when disabled, normal color when enabled
                buttonImage.color = canProceed ? new Color(0.2f, 0.6f, 0.8f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
            }
            
            // Only show the button if we have any kind of selection/input
            bool shouldShowButton = canProceed || 
                                   (currentQuestion.type == QuestionType.NumberInput && !string.IsNullOrEmpty(numberInput.text)) ||
                                   selectedYesNoAnswer ||
                                   !string.IsNullOrEmpty(selectedColorType) ||
                                   !string.IsNullOrEmpty(selectedModalityType);
            
            nextButton.gameObject.SetActive(shouldShowButton);
        }
    }

    Button CreateButton(string name, string text, Vector2 anchorMin, Vector2 anchorMax, Color buttonColor)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(questionPanel.transform, false);
        
        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = buttonColor;
        
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        
        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 18;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        return button;
    }
    
    void CreateNextButton()
    {
        nextButton = CreateButton("NextButton", "NEXT", new Vector2(0.35f, 0.15f), new Vector2(0.65f, 0.27f), new Color(0.2f, 0.6f, 0.8f, 1f));
        nextButton.onClick.AddListener(() => {
            Debug.Log("NEXT button clicked!");
            NextQuestion();
        });
        
        // Make Next button text bigger
        nextButton.GetComponentInChildren<Text>().fontSize = 28;
        
        nextButton.gameObject.SetActive(false);
    }
    
    void SelectColorType(string colorType)
    {
        selectedColorType = colorType;
        Debug.Log($"Selected color type: {colorType}");
        
        // Highlight selected button
        foreach (Button colorButton in colorButtons)
        {
            Image buttonImage = colorButton.GetComponent<Image>();
            if (colorButton.name.Contains(colorType))
            {
                // Add a border or brightness to show selection
                buttonImage.color = Color.Lerp(buttonImage.color, Color.white, 0.3f);
            }
            else
            {
                // Reset other buttons to normal color
                buttonImage.color = Color.blue;
            }
        }
        
        // Update validation state
        ClearErrorMessage();
        UpdateNextButtonState();
    }
    
    void SelectModalityType(string modalityType)
    {
        selectedModalityType = modalityType;
        Debug.Log($"Selected modality type: {modalityType}");
        
        // Highlight selected button
        foreach (Button modalityButton in modalityButtons)
        {
            Image buttonImage = modalityButton.GetComponent<Image>();
            if (modalityButton.name.Contains(modalityType))
            {
                // Add a border or brightness to show selection
                buttonImage.color = Color.Lerp(buttonImage.color, Color.white, 0.3f);
            }
            else
            {
                // Reset other buttons to normal color
                buttonImage.color = Color.blue;
            }
        }
        
        // Update validation state
        ClearErrorMessage();
        UpdateNextButtonState();
    }
    
    void ShowCurrentQuestion()
    {
        if (currentQuestionIndex >= questions.Count)
        {
            CompleteAssessment();
            return;
        }
        
        // Hide all input components and reset validation state
        HideAllInputs();
        
        AssessmentQuestion currentQuestion = questions[currentQuestionIndex];
        questionText.text = $"Question {currentQuestionIndex + 1}/{questions.Count}\n\n{currentQuestion.questionText}";
        
        // Show appropriate input for this question type
        switch (currentQuestion.type)
        {
            case QuestionType.NumberInput:
                numberInput.gameObject.SetActive(true);
                numberInput.text = ""; // Clear previous input
                
                // Update placeholder based on question
                Text placeholder = numberInput.placeholder as Text;
                if (currentQuestionIndex == 3) // Object clarity distance
                {
                    placeholder.text = "1-10 metres";
                }
                else if (currentQuestionIndex == 4) // Avoidance distance
                {
                    placeholder.text = "0.5-5 metres";
                }
                else // Scale questions (0, 1, 2, 6, 7)
                {
                    placeholder.text = "Enter 1-10";
                }
                
                // Initial validation state check
                UpdateNextButtonState();
                
                // Focus the input field after a frame
                StartCoroutine(FocusInputField());
                break;
                
            case QuestionType.YesNo:
                yesButton.gameObject.SetActive(true);
                noButton.gameObject.SetActive(true);
                UpdateNextButtonState();
                break;
                
            case QuestionType.ColorBlind:
                ShowColorSelection();
                break;
                
            case QuestionType.ModalityPreference:
                ShowModalitySelection();
                break;
        }
        
        Debug.Log($"Showing question {currentQuestionIndex + 1}: {currentQuestion.questionText}");
    }
    
    System.Collections.IEnumerator FocusInputField()
    {
        yield return null; // Wait one frame
        numberInput.ActivateInputField();
        numberInput.Select();
    }
    
    void HideAllInputs()
    {
        numberInput.gameObject.SetActive(false);
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        
        // Hide all color buttons
        foreach (Button colorButton in colorButtons)
        {
            colorButton.gameObject.SetActive(false);
        }
        
        // Hide all modality buttons
        foreach (Button modalityButton in modalityButtons)
        {
            modalityButton.gameObject.SetActive(false);
        }
        
        nextButton.gameObject.SetActive(false);
        
        // Reset all validation states
        hasValidInput = false;
        selectedYesNoAnswer = false;
        yesNoAnswerValue = false;
        isShowingColorSelection = false;
        isShowingModalitySelection = false;
        selectedColorType = "";
        selectedModalityType = "";
        
        // Clear any error messages
        ClearErrorMessage();
        
        // Reset button colors
        if (yesButton != null)
            yesButton.GetComponent<Image>().color = Color.blue;
        if (noButton != null)
            noButton.GetComponent<Image>().color = Color.blue;
        
        // Reset color button colors
        foreach (Button colorButton in colorButtons)
        {
            colorButton.GetComponent<Image>().color = Color.blue;
        }
        
        // Reset modality button colors
        foreach (Button modalityButton in modalityButtons)
        {
            modalityButton.GetComponent<Image>().color = Color.blue;
        }
    }
    
    void SelectYesNoAnswer(bool answer)
    {
        selectedYesNoAnswer = true;
        yesNoAnswerValue = answer;
        
        Debug.Log($"Selected Y/N answer: {answer}");
        
        // Highlight selected button and dim the other
        if (answer)
        {
            // YES selected
            yesButton.GetComponent<Image>().color = Color.Lerp(Color.blue, Color.white, 0.3f);
            noButton.GetComponent<Image>().color = Color.blue;
        }
        else
        {
            // NO selected
            noButton.GetComponent<Image>().color = Color.Lerp(Color.blue, Color.white, 0.3f);
            yesButton.GetComponent<Image>().color = Color.blue;
        }
        
        // Update validation state
        ClearErrorMessage();
        UpdateNextButtonState();
    }
    
    void ShowColorSelection()
    {
        questionText.text = $"Question {currentQuestionIndex + 1}/{questions.Count}\n\n{questions[currentQuestionIndex].questionText}";
        HideAllInputs();
        
        // Show all color buttons
        foreach (Button colorButton in colorButtons)
        {
            colorButton.gameObject.SetActive(true);
        }
        
        isShowingColorSelection = true;
        UpdateNextButtonState(); // Initially disabled until selection is made
    }
    
    void ShowModalitySelection()
    {
        questionText.text = $"Question {currentQuestionIndex + 1}/{questions.Count}\n\n{questions[currentQuestionIndex].questionText}";
        HideAllInputs();
        
        // Show all modality buttons
        foreach (Button modalityButton in modalityButtons)
        {
            modalityButton.gameObject.SetActive(true);
        }
        
        isShowingModalitySelection = true;
        UpdateNextButtonState(); // Initially disabled until selection is made
    }
    
    void NextQuestion()
    {
        // Check if we can proceed (validation should prevent this, but double-check)
        if (!CanProceedToNextQuestion())
        {
            Debug.LogWarning("Attempted to proceed without valid input!");
            return;
        }
        
        // Record answers based on question type
        RecordCurrentAnswer();
        
        currentQuestionIndex++;
        ShowCurrentQuestion();
    }
    
    bool CanProceedToNextQuestion()
    {
        AssessmentQuestion currentQuestion = questions[currentQuestionIndex];
        
        switch (currentQuestion.type)
        {
            case QuestionType.NumberInput:
                return hasValidInput;
            case QuestionType.YesNo:
                return selectedYesNoAnswer;
            case QuestionType.ColorBlind:
                return !string.IsNullOrEmpty(selectedColorType);
            case QuestionType.ModalityPreference:
                return !string.IsNullOrEmpty(selectedModalityType);
            default:
                return false;
        }
    }
    
    void RecordCurrentAnswer()
    {
        // Record number input answers
        if (numberInput.gameObject.activeInHierarchy)
        {
            string inputValue = numberInput.text;
            float numberValue = 5f; // Default value
            
            if (float.TryParse(inputValue, out float parsedValue))
            {
                if (currentQuestionIndex == 3) // Object clarity distance (1-10m)
                {
                    numberValue = Mathf.Clamp(parsedValue, 1, 10);
                    Debug.Log($"Recording object clarity distance: {numberValue} metres");
                    assessmentResults.objectClarityDistance = numberValue;
                }
                else if (currentQuestionIndex == 4) // Avoidance distance (0.5-5m)
                {
                    numberValue = Mathf.Clamp(parsedValue, 0.5f, 5f);
                    Debug.Log($"Recording avoidance distance: {numberValue} metres");
                    assessmentResults.reliableAvoidanceDistance = numberValue;
                }
                else // Scale questions - convert to int
                {
                    int intValue = Mathf.RoundToInt(Mathf.Clamp(parsedValue, 1, 10));
                    Debug.Log($"Recording scale answer: {intValue} for question {currentQuestionIndex}");
                    
                    switch (currentQuestionIndex)
                    {
                        case 0: assessmentResults.centralVisionRating = intValue; break;
                        case 1: assessmentResults.leftPeripheralRating = intValue; break;
                        case 2: assessmentResults.rightPeripheralRating = intValue; break;
                        case 6: assessmentResults.lightSensitivityRating = intValue; break; // Color question is at index 5 now
                        case 7: assessmentResults.lowLightDifficultyRating = intValue; break; // Light sensitivity at index 6
                    }
                }
            }
        }
        
        // Record Yes/No answers (if any remain)
        if (selectedYesNoAnswer)
        {
            // Handle any remaining Y/N questions if needed
        }
        
        // Record color selection (now at index 5)
        if (isShowingColorSelection && !string.IsNullOrEmpty(selectedColorType))
        {
            assessmentResults.colorBlindnessTypes.Clear();
            assessmentResults.colorBlindnessTypes.Add(selectedColorType);
            
            // Set isColorBlind based on selection
            assessmentResults.isColorBlind = selectedColorType != "None";
            
            Debug.Log($"Recording color blindness: {selectedColorType}");
        }
        
        // Record modality preference (now at index 8)
        if (isShowingModalitySelection && !string.IsNullOrEmpty(selectedModalityType))
        {
            assessmentResults.preferredModalityType = selectedModalityType;
            Debug.Log($"Recording modality preference: {selectedModalityType}");
        }
    }
    
    void CompleteAssessment()
    {
        assessmentResults.completed = true;
        assessmentResults.assessmentDuration = Time.time;
        
        SaveAssessmentData();
        
        questionText.text = "Assessment Complete!\n\nThank you for your responses.";
        HideAllInputs();
        
        Debug.Log("Assessment completed");
        Debug.Log($"Results: Central={assessmentResults.centralVisionRating}, Left={assessmentResults.leftPeripheralRating}, Right={assessmentResults.rightPeripheralRating}");
        Debug.Log($"Object clarity distance: {assessmentResults.objectClarityDistance}m");
        Debug.Log($"Avoidance distance: {assessmentResults.reliableAvoidanceDistance}m");
        Debug.Log($"Color blind: {assessmentResults.isColorBlind}, Light sensitivity: {assessmentResults.lightSensitivityRating}, Low-light difficulty: {assessmentResults.lowLightDifficultyRating}");
        Debug.Log($"Modality preference: {assessmentResults.preferredModalityType}");
        if (assessmentResults.colorBlindnessTypes.Count > 0)
        {
            Debug.Log($"Color blindness type: {assessmentResults.colorBlindnessTypes[0]}");
        }
    }
    
    void SaveAssessmentData()
    {
        // Use SessionManager's cross-platform path system
        string userSessionPath = "";
        
        if (SessionManager.Instance != null)
        {
            // Primary method: Use SessionManager's path system
            userSessionPath = SessionManager.Instance.GetSessionPath();
            
            if (!string.IsNullOrEmpty(userSessionPath) && Directory.Exists(userSessionPath))
            {
                SaveToSessionManagerPath(userSessionPath);
                return;
            }
        }
        
        // Fallback method: Find user session folder using cross-platform base path
        userSessionPath = FindUserSessionFolderCrossPlatform();
        
        if (!string.IsNullOrEmpty(userSessionPath))
        {
            SaveToSessionManagerPath(userSessionPath);
        }
        else
        {
            Debug.LogError($"Could not find or create session folder for user: {userID}");
            Debug.LogError("Make sure SessionManager is initialized or user has completed navigation first");
        }
    }
    
    void SaveToSessionManagerPath(string userSessionPath)
    {
        // Create the AlgorithmicAssessment subfolder within the user's session
        string assessmentPath = Path.Combine(userSessionPath, "03_AlgorithmicAssessment");
        Directory.CreateDirectory(assessmentPath);
        
        // Save as JSON
        string jsonPath = Path.Combine(assessmentPath, $"algorithmic_assessment_{System.DateTime.Now:yyyyMMdd_HHmmss}.json");
        string jsonData = JsonUtility.ToJson(assessmentResults, true);
        File.WriteAllText(jsonPath, jsonData);
        
        Debug.Log($"Assessment data saved to: {jsonPath}");
        Debug.Log($"Full path: {Path.GetFullPath(jsonPath)}");
    }
    
    string FindUserSessionFolderCrossPlatform()
    {
        // Get base path from SessionManager or use cross-platform detection
        string baseNavigationPath;
        
        if (SessionManager.Instance != null)
        {
            baseNavigationPath = Path.Combine(SessionManager.Instance.GetBaseDataPath(), "Users");
        }
        else
        {
            // Manual cross-platform path detection if SessionManager unavailable
            string documentsPath = GetCrossPlatformDocumentsPath();
            baseNavigationPath = Path.Combine(documentsPath, "VisionAssessmentData", "Users");
        }
        
        if (!Directory.Exists(baseNavigationPath))
        {
            Debug.LogError($"Base navigation data folder not found: {baseNavigationPath}");
            return null;
        }
        
        // Find session folders that start with the userID
        string[] sessionFolders = Directory.GetDirectories(baseNavigationPath);
        
        foreach (string sessionFolder in sessionFolders)
        {
            string folderName = Path.GetFileName(sessionFolder);
            
            // Check if folder name starts with userID (e.g., "User001_20250817_124047")
            if (folderName.StartsWith(userID + "_"))
            {
                Debug.Log($"Found user session folder: {folderName}");
                return sessionFolder;
            }
        }
        
        // If not found, list available folders for debugging
        Debug.LogWarning($"No session folder found for userID: {userID}");
        Debug.Log($"Available session folders in: {baseNavigationPath}");
        foreach (string sessionFolder in sessionFolders)
        {
            string folderName = Path.GetFileName(sessionFolder);
            Debug.Log($"  - {folderName}");
        }
        
        return null;
    }
    
    /// <summary>
    /// Cross-platform Documents folder detection (fallback if SessionManager unavailable)
    /// </summary>
    string GetCrossPlatformDocumentsPath()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
                return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                
            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.LinuxEditor:
                string linuxDocs = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                if (string.IsNullOrEmpty(linuxDocs))
                {
                    string home = System.Environment.GetEnvironmentVariable("HOME");
                    return Path.Combine(home, "Documents");
                }
                return linuxDocs;
                
            default:
                return Application.persistentDataPath;
        }
    }
    
    // Add keyboard support for Enter key
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (nextButton.gameObject.activeInHierarchy && nextButton.interactable)
            {
                Debug.Log("Enter key pressed - advancing question");
                NextQuestion();
            }
        }
    }
    
    // Add context menu for testing and debugging
    [ContextMenu("Debug: Show User Session Path")]
    void DebugShowUserSessionPath()
    {
        string userSessionPath = FindUserSessionFolderCrossPlatform();
        if (!string.IsNullOrEmpty(userSessionPath))
        {
            Debug.Log($"User {userID} session path: {userSessionPath}");
            Debug.Log($"Full path: {Path.GetFullPath(userSessionPath)}");
            
            // Show what's in the session folder
            string[] subFolders = Directory.GetDirectories(userSessionPath);
            Debug.Log($"Subfolders in session:");
            foreach (string folder in subFolders)
            {
                Debug.Log($"  - {Path.GetFileName(folder)}");
            }
        }
        else
        {
            Debug.LogError($"Could not find session for user: {userID}");
        }
    }
    
    [ContextMenu("Debug: List All User Sessions")]
    void DebugListAllUserSessions()
    {
        string baseNavigationPath;
        
        if (SessionManager.Instance != null)
        {
            baseNavigationPath = Path.Combine(SessionManager.Instance.GetBaseDataPath(), "Users");
        }
        else
        {
            string documentsPath = GetCrossPlatformDocumentsPath();
            baseNavigationPath = Path.Combine(documentsPath, "VisionAssessmentData", "Users");
        }
        
        Debug.Log($"Looking for sessions in: {baseNavigationPath}");
        Debug.Log($"Full path: {Path.GetFullPath(baseNavigationPath)}");
        
        if (Directory.Exists(baseNavigationPath))
        {
            string[] sessionFolders = Directory.GetDirectories(baseNavigationPath);
            Debug.Log($"All user session folders ({sessionFolders.Length} total):");
            foreach (string sessionFolder in sessionFolders)
            {
                string folderName = Path.GetFileName(sessionFolder);
                System.DateTime createTime = Directory.GetCreationTime(sessionFolder);
                Debug.Log($"  - {folderName} (Created: {createTime:yyyy-MM-dd HH:mm:ss})");
            }
        }
        else
        {
            Debug.LogError($"Base navigation path not found: {baseNavigationPath}");
        }
    }
    
    [ContextMenu("Debug: Test Cross-Platform Save")]
    void DebugTestCrossPlatformSave()
    {
        Debug.Log("Testing cross-platform save functionality...");
        
        // Create test assessment results
        AlgorithmicAssessmentResults testResults = new AlgorithmicAssessmentResults();
        testResults.centralVisionRating = 7;
        testResults.leftPeripheralRating = 6;
        testResults.rightPeripheralRating = 8;
        testResults.completed = true;
        testResults.assessmentDateTime = System.DateTime.Now.ToString();
        
        // Temporarily set test results
        AlgorithmicAssessmentResults originalResults = assessmentResults;
        assessmentResults = testResults;
        
        // Try to save
        SaveAssessmentData();
        
        // Restore original results
        assessmentResults = originalResults;
        
        Debug.Log("Cross-platform save test completed");
    }
}

[System.Serializable]
public class AssessmentQuestion
{
    public string questionText;
    public QuestionType type;
    
    public AssessmentQuestion(string text, QuestionType questionType)
    {
        questionText = text;
        type = questionType;
    }
}

public enum QuestionType
{
    NumberInput,
    YesNo,
    ColorBlind,
    ModalityPreference
}