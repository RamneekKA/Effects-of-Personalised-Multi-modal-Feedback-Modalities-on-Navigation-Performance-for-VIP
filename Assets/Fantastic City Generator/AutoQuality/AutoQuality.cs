
using UnityEngine;
using UnityEngine.UI;

namespace FCG
{
    public class AutoQuality : MonoBehaviour
    {
        [Header("Display Options")]
        public bool ShowQualityLevel = true;
        public bool ShowFps = true;
        public bool UpdateColor = true;
        float UpdateFrequency = 0.5f;

        [Header("Optional")]
        public GameObject posProcess;

        private float accum = 0f;
        private int frames = 0;
        private float fps;
        private string sFPS;
        private int currentQualityLevel = -1;
        private int numberOfQualityLevels = 5;

        private Text displayText;
        private float timeLeftToUpdateText;

        void Start()
        {
            numberOfQualityLevels = QualitySettings.names.Length;
            CreateUI();
            InvokeRepeating(nameof(UpdateAutoQuality), 0.5f, 2f);
            timeLeftToUpdateText = UpdateFrequency;
        }

        void Update()
        {
            accum += Time.timeScale / Time.deltaTime;
            frames++;

            timeLeftToUpdateText -= Time.deltaTime;
            if (timeLeftToUpdateText <= 0f)
            {
                UpdateDisplay();
                accum = 0f;
                frames = 0;
                timeLeftToUpdateText = UpdateFrequency;
            }
        }

        void UpdateAutoQuality()
        {

            if (currentQualityLevel == -1)
                currentQualityLevel = QualitySettings.GetQualityLevel();

            if (fps < 28 && currentQualityLevel > 0)
            {
                currentQualityLevel--;
                QualitySettings.SetQualityLevel(currentQualityLevel, false);
            }
            else if (fps > 50 && currentQualityLevel < numberOfQualityLevels - 1)
            {
                currentQualityLevel++;
                QualitySettings.SetQualityLevel(currentQualityLevel, false);
            }

            if (posProcess != null)
                posProcess.SetActive(currentQualityLevel >= numberOfQualityLevels - 1);

        }

        void UpdateDisplay()
        {
            if (frames == 0) return;

            fps = accum / frames;
            sFPS = Mathf.RoundToInt(fps).ToString();

            if (displayText == null) return;

            string text = "";
            if (ShowFps)
                text += sFPS + " FPS";

            if (ShowQualityLevel)
            {
                if (text != "")
                    text += " | ";
                text += "Quality: " + currentQualityLevel;
            }

            displayText.text = text;

            if (UpdateColor)
            {
                if (fps >= 30)
                    displayText.color = Color.green;
                else if (fps > 10)
                    displayText.color = Color.yellow;
                else
                    displayText.color = Color.red;
            }
            else
            {
                displayText.color = Color.white;
            }
        }

        void CreateUI()
        {
            if (GetComponentInChildren<Canvas>() != null)
                return;

            GameObject canvasGO = new GameObject("AutoQualityCanvas");
            canvasGO.transform.SetParent(this.transform);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            GameObject textGO = new GameObject("AutoQualityText");
            textGO.transform.SetParent(canvasGO.transform);
            displayText = textGO.AddComponent<Text>();


            // Set font based on Unity version
#if UNITY_2022_1_OR_NEWER
            displayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            displayText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif

            displayText.fontSize = 26;
            displayText.alignment = TextAnchor.UpperCenter;
            displayText.color = Color.white;

            RectTransform rectTransform = displayText.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0, -40);
            rectTransform.sizeDelta = new Vector2(600, 100);
        }
    }
}