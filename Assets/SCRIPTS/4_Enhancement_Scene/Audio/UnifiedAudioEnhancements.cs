using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Unified Audio Controller - Single audio system for navigation assistance
/// Score 1-3: Full TTS speech with direction and object type
/// Score 4-6: Spearcons (pre-recorded audio files) for all nearby objects
/// Score 7-10: Spearcons only for objects beyond clarity distance (where vision becomes unclear)
/// UPDATED: Now supports manual override for LLM trials
/// </summary>
public class UnifiedAudioController : MonoBehaviour
{
    [Header("Assessment Integration")]
    [Tooltip("Central vision score from algorithmic assessment (1-10)")]
    [SerializeField] private int centralVisionScore = 5;
    
    [Tooltip("Distance where objects become unclear (from assessment)")]
    [SerializeField] private float objectClarityDistance = 5f;
    
    [Header("Manual Control Override")]
    [Tooltip("Allow manual control to override automatic assessment-based behavior")]
    public bool allowManualOverride = false;
    [SerializeField] private bool manualModeActive = false;
    
    [Header("Audio Mode Configuration")]
    [SerializeField] private AudioMode currentAudioMode = AudioMode.Disabled;
    [SerializeField] private bool systemInitialized = false;
    [SerializeField] private bool audioSystemActive = false;
    
    [Header("TTS Speech Settings (Scores 1-3)")]
    [Tooltip("How often to announce objects via TTS speech")]
    [Range(0.5f, 3f)]
    public float speechAnnouncementInterval = 1f;
    
    [Tooltip("Maximum range for TTS announcements")]
    [Range(5f, 30f)]
    public float speechDetectionRange = 15f;
    
    [Tooltip("Speech rate for TTS (words per minute)")]
    [Range(100, 400)]
    public int speechRate = 300;
    
    [Header("Spearcon Settings (Scores 4-10)")]
    [Tooltip("Pre-recorded audio clips for each object type")]
    public AudioClipMapping[] objectAudioClips;
    
    [Tooltip("How often to play spearcons")]
    [Range(0.5f, 3f)]
    public float spearconAnnouncementInterval = 1.5f;
    
    [Tooltip("Maximum range to detect objects for spearcons")]
    [Range(5f, 50f)]
    public float spearconDetectionRange = 25f;
    
    [Header("Spatial Audio Settings")]
    [Tooltip("Enable spatial audio positioning for spearcons")]
    public bool enableSpatialAudio = true;
    
    [Tooltip("How far left/right to position audio sources")]
    [Range(1f, 10f)]
    public float spatialRange = 5f;
    
    [Tooltip("3D spatial blend (0=2D, 1=full 3D)")]
    [Range(0f, 1f)]
    public float spatialBlend = 1f;
    
    [Header("Volume Control")]
    [Tooltip("Master volume for all audio")]
    [Range(0f, 1f)]
    public float masterVolume = 0.8f;
    
    [Tooltip("Volume rolloff curve based on distance")]
    public AnimationCurve distanceVolumeRolloff = AnimationCurve.EaseInOut(0f, 1f, 25f, 0.2f);
    
    [Tooltip("Minimum volume (prevents objects from becoming silent)")]
    [Range(0f, 0.5f)]
    public float minimumVolume = 0.1f;
    
    [Header("Audio Management")]
    [Tooltip("Maximum number of simultaneous audio announcements")]
    [Range(1, 3)]
    public int maxSimultaneousAudio = 1;
    
    [Tooltip("Minimum time between audio announcements (seconds)")]
    [Range(0.1f, 2f)]
    public float minimumAudioGap = 0.3f;
    
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    
    [Header("Current Status")]
    [SerializeField] private DetectableObject currentClosestObject;
    [SerializeField] private float currentClosestDistance;
    [SerializeField] private string lastAnnouncementText = "";
    [SerializeField] private float lastAnnouncementTime = 0f;
    [SerializeField] private int totalNearbyObjects = 0;
    
    // Internal state
    private Transform playerTransform;
    private Camera playerCamera;
    private Coroutine audioAnnouncementCoroutine;
    
    // TTS Audio Management
    private AudioSource ttsAudioSource;
    private bool isTTSSpeaking = false;
    
    // Spearcon Audio Management
    private Dictionary<string, AudioClip> audioClipDict = new Dictionary<string, AudioClip>();
    private const int AUDIO_SOURCE_POOL_SIZE = 8;
    private List<AudioSource> audioSourcePool = new List<AudioSource>();
    private List<bool> audioSourceInUse = new List<bool>();
    
    public enum AudioMode
    {
        Disabled,
        FullSpeech,        // Scores 1-3
        StandardSpearcons, // Scores 4-6
        LimitedSpearcons   // Scores 7-10
    }
    
    [System.Serializable]
    public class AudioClipMapping
    {
        public string objectType;
        public AudioClip audioClip;
        [Range(0f, 2f)]
        public float volumeMultiplier = 1f;
    }
    
    [System.Serializable]
    private class ObjectDistance
    {
        public DetectableObject detectableObject;
        public float distance;
        public Vector3 direction;
        public AudioClip audioClip;
        public float volumeMultiplier;
    }
    
    void Start()
    {
        InitializeSystem();
        StartCoroutine(DelayedSetupWithAssessment());
    }
    
    void InitializeSystem()
    {
        // Find player components
        FCG.CharacterControl characterControl = FindObjectOfType<FCG.CharacterControl>();
        if (characterControl != null)
        {
            playerTransform = characterControl.transform;
            playerCamera = characterControl.GetComponentInChildren<Camera>();
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerCamera = player.GetComponentInChildren<Camera>();
            }
        }
        
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        if (playerTransform == null)
        {
            Debug.LogError("UnifiedAudioController: Could not find player transform!");
            return;
        }
        
        // Setup audio components
        SetupTTSAudioSource();
        BuildAudioClipDictionary();
        CreateAudioSourcePool();
        
        Debug.Log("UnifiedAudioController: System initialized");
    }
    
    void SetupTTSAudioSource()
    {
        GameObject ttsAudioObj = new GameObject("TTSAudioSource");
        ttsAudioObj.transform.SetParent(transform);
        
        ttsAudioSource = ttsAudioObj.AddComponent<AudioSource>();
        ttsAudioSource.spatialBlend = 0f; // 2D audio for TTS
        ttsAudioSource.volume = masterVolume;
        ttsAudioSource.playOnAwake = false;
        
        Debug.Log("TTS Audio source created");
    }
    
    void BuildAudioClipDictionary()
    {
        audioClipDict.Clear();
        
        foreach (AudioClipMapping mapping in objectAudioClips)
        {
            if (mapping.audioClip != null && !string.IsNullOrEmpty(mapping.objectType))
            {
                audioClipDict[mapping.objectType] = mapping.audioClip;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"Mapped audio clip: {mapping.objectType} -> {mapping.audioClip.name}");
                }
            }
        }
        
        Debug.Log($"Built audio clip dictionary with {audioClipDict.Count} entries");
    }
    
    void CreateAudioSourcePool()
    {
        for (int i = 0; i < AUDIO_SOURCE_POOL_SIZE; i++)
        {
            GameObject audioObj = new GameObject($"SpearconAudioSource_{i}");
            audioObj.transform.SetParent(transform);
            
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.spatialBlend = enableSpatialAudio ? spatialBlend : 0.0f;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.maxDistance = spearconDetectionRange;
            audioSource.dopplerLevel = 0f;
            audioSource.playOnAwake = false;
            audioSource.volume = masterVolume;
            
            if (enableSpatialAudio)
            {
                audioSource.minDistance = 1f;
                audioSource.spread = 0f;
                audioSource.rolloffMode = AudioRolloffMode.Custom;
            }
            
            audioSourcePool.Add(audioSource);
            audioSourceInUse.Add(false);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Created spearcon audio source pool with {AUDIO_SOURCE_POOL_SIZE} sources");
        }
    }
    
    IEnumerator DelayedSetupWithAssessment()
    {
        yield return new WaitForSeconds(1f); // Wait for SessionManager to initialize
        
        // Check if manual mode is active
        if (manualModeActive)
        {
            Debug.Log("UnifiedAudioController: Manual mode active - skipping automatic setup");
            systemInitialized = true;
            yield break;
        }
        
        // Check if current trial should use audio enhancements
        if (!ShouldEnableAudioForCurrentTrial())
        {
            Debug.Log($"UnifiedAudioController: Audio enhancements disabled for trial '{GetCurrentTrial()}'");
            currentAudioMode = AudioMode.Disabled;
            systemInitialized = true;
            yield break;
        }
        
        LoadAssessmentResults();
        ConfigureAudioMode();
        StartAudioSystem();
        
        systemInitialized = true;
    }
    
    bool ShouldEnableAudioForCurrentTrial()
    {
        // Manual mode override
        if (allowManualOverride && manualModeActive)
        {
            return true;
        }
        
        // Original logic for automatic mode
        if (SessionManager.Instance == null) return false;
        
        string currentTrial = SessionManager.Instance.GetCurrentTrial();
        return currentTrial == "short_algorithmic" || currentTrial == "long_algorithmic";
    }
    
    string GetCurrentTrial()
    {
        return SessionManager.Instance?.GetCurrentTrial() ?? "unknown";
    }
    
    void LoadAssessmentResults()
    {
        Debug.Log("=== LOADING ALGORITHMIC ASSESSMENT RESULTS ===");
        
        if (SessionManager.Instance == null)
        {
            Debug.LogWarning("UnifiedAudioController: No SessionManager found - using default values");
            Debug.LogWarning("Will use default scores: Central=5, Clarity Distance=5m");
            return;
        }
        
        UserSession session = SessionManager.Instance.GetCurrentSession();
        if (session?.algorithmicResults != null && session.algorithmicResults.completed)
        {
            // Load the specific scores that determine audio behavior
            centralVisionScore = session.algorithmicResults.centralVisionRating;
            objectClarityDistance = session.algorithmicResults.objectClarityDistance;
            
            Debug.Log($"✅ ASSESSMENT DATA LOADED:");
            Debug.Log($"   Central Vision Rating: {centralVisionScore}/10");
            Debug.Log($"   Object Clarity Distance: {objectClarityDistance}m");
            Debug.Log($"   (Distance where objects become unclear/hard to identify)");
            
            // Validate the scores
            if (centralVisionScore < 1 || centralVisionScore > 10)
            {
                Debug.LogError($"Invalid central vision score: {centralVisionScore}. Using default (5)");
                centralVisionScore = 5;
            }
            
            if (objectClarityDistance < 1f || objectClarityDistance > 10f)
            {
                Debug.LogError($"Invalid clarity distance: {objectClarityDistance}. Using default (5m)");
                objectClarityDistance = 5f;
            }
        }
        else
        {
            Debug.LogWarning("⚠ NO COMPLETED ALGORITHMIC ASSESSMENT FOUND");
            Debug.LogWarning("Using default values - you may need to complete the algorithmic assessment first");
            Debug.LogWarning($"Default scores: Central={centralVisionScore}, Clarity Distance={objectClarityDistance}m");
            
            // Show what assessment data is available
            if (session?.algorithmicResults != null)
            {
                Debug.Log($"Assessment exists but completed={session.algorithmicResults.completed}");
            }
            else
            {
                Debug.Log("No algorithmic results object found in session");
            }
        }
        
        Debug.Log("=== ASSESSMENT LOADING COMPLETE ===");
    }
    
    void ConfigureAudioMode()
    {
        Debug.Log($"=== CONFIGURING AUDIO MODE BASED ON ALGORITHMIC SCORES ===");
        Debug.Log($"Central Vision Score: {centralVisionScore}/10");
        Debug.Log($"Object Clarity Distance: {objectClarityDistance}m");
        
        if (centralVisionScore >= 1 && centralVisionScore <= 3)
        {
            currentAudioMode = AudioMode.FullSpeech;
            Debug.Log($"DECISION: FULL SPEECH (TTS) - Vision score {centralVisionScore} is in range 1-3");
            Debug.Log($"Will announce: direction + object type (e.g., 'ahead car', 'left tree')");
        }
        else if (centralVisionScore >= 4 && centralVisionScore <= 6)
        {
            currentAudioMode = AudioMode.StandardSpearcons;
            Debug.Log($"DECISION: STANDARD SPEARCONS - Vision score {centralVisionScore} is in range 4-6");
            Debug.Log($"Will play: pre-recorded audio clips for nearest objects within {spearconDetectionRange}m");
        }
        else if (centralVisionScore >= 7 && centralVisionScore <= 10)
        {
            currentAudioMode = AudioMode.LimitedSpearcons;
            Debug.Log($"DECISION: LIMITED SPEARCONS - Vision score {centralVisionScore} is in range 7-10");
            Debug.Log($"Will play: pre-recorded audio clips ONLY for objects beyond {objectClarityDistance}m (where vision becomes unclear)");
        }
        else
        {
            currentAudioMode = AudioMode.Disabled;
            Debug.LogWarning($"DECISION: DISABLED - Invalid vision score ({centralVisionScore}) - disabling audio");
        }
        
        Debug.Log($"=== AUDIO MODE CONFIGURED: {currentAudioMode} ===");
    }
    
    void StartAudioSystem()
    {
        // Stop any existing audio first
        StopAllAudio();
        
        Debug.Log($"=== STARTING AUDIO SYSTEM ===");
        Debug.Log($"Mode Decision: {currentAudioMode}");
        
        switch (currentAudioMode)
        {
            case AudioMode.FullSpeech:
                Debug.Log("STARTING: TTS Speech System");
                Debug.Log($"- Will use built-in text-to-speech");
                Debug.Log($"- Will announce: '[direction] [object]' (e.g., 'ahead car')");
                Debug.Log($"- Detection range: {speechDetectionRange}m");
                Debug.Log($"- Announcement interval: {speechAnnouncementInterval}s");
                StartSpeechAnnouncements();
                break;
                
            case AudioMode.StandardSpearcons:
                Debug.Log("STARTING: Standard Spearcons System");
                Debug.Log($"- Will use pre-recorded audio clips");
                Debug.Log($"- Will announce nearest object within {spearconDetectionRange}m");
                Debug.Log($"- No distance filtering applied");
                Debug.Log($"- Announcement interval: {spearconAnnouncementInterval}s");
                StartSpearconAnnouncements(false); // No distance filtering
                break;
                
            case AudioMode.LimitedSpearcons:
                Debug.Log("STARTING: Limited Spearcons System");
                Debug.Log($"- Will use pre-recorded audio clips");
                Debug.Log($"- Will announce objects ONLY beyond {objectClarityDistance}m");
                Debug.Log($"- Reason: Objects closer than {objectClarityDistance}m should be clear with vision score {centralVisionScore}");
                Debug.Log($"- Detection range: {spearconDetectionRange}m");
                Debug.Log($"- Announcement interval: {spearconAnnouncementInterval}s");
                StartSpearconAnnouncements(true); // With distance filtering
                break;
                
            case AudioMode.Disabled:
            default:
                Debug.Log("AUDIO SYSTEM: DISABLED");
                break;
        }
        
        audioSystemActive = (currentAudioMode != AudioMode.Disabled);
        Debug.Log($"Audio System Active: {audioSystemActive}");
        Debug.Log($"=== AUDIO SYSTEM STARTUP COMPLETE ===");
    }
    
    // NEW MANUAL CONTROL METHODS
    
    /// <summary>
    /// Enable manual control mode - bypasses assessment checks and trial restrictions
    /// </summary>
    public void EnableManualMode()
    {
        allowManualOverride = true;
        manualModeActive = true;
        Debug.Log("UnifiedAudioController: Manual mode enabled - assessment checks bypassed");
    }
    
    /// <summary>
    /// Disable manual control mode - restores automatic behavior
    /// </summary>
    public void DisableManualMode()
    {
        allowManualOverride = false;
        manualModeActive = false;
        StopAllAudio();
        Debug.Log("UnifiedAudioController: Manual mode disabled - restored automatic behavior");
    }
    
    /// <summary>
    /// Force a specific audio mode (for manual control)
    /// </summary>
    public void ForceAudioMode(AudioMode mode)
    {
        if (!allowManualOverride)
        {
            Debug.LogWarning("UnifiedAudioController: Cannot force audio mode - manual override not enabled");
            return;
        }
        
        currentAudioMode = mode;
        audioSystemActive = (mode != AudioMode.Disabled);
        
        // Start the appropriate audio system
        StopAllAudio();
        
        switch (mode)
        {
            case AudioMode.FullSpeech:
                Debug.Log("UnifiedAudioController: Forced to TTS Speech mode");
                StartSpeechAnnouncements();
                break;
                
            case AudioMode.StandardSpearcons:
                Debug.Log("UnifiedAudioController: Forced to Standard Spearcons mode");
                StartSpearconAnnouncements(false);
                break;
                
            case AudioMode.LimitedSpearcons:
                Debug.Log("UnifiedAudioController: Forced to Limited Spearcons mode");
                StartSpearconAnnouncements(true);
                break;
                
            case AudioMode.Disabled:
            default:
                Debug.Log("UnifiedAudioController: Forced to Disabled mode");
                audioSystemActive = false;
                break;
        }
    }
    
    /// <summary>
    /// Set object clarity distance for manual control
    /// </summary>
    public void SetObjectClarityDistance(float distance)
    {
        objectClarityDistance = Mathf.Clamp(distance, 1f, 50f);
        Debug.Log($"UnifiedAudioController: Object clarity distance set to {objectClarityDistance}m");
    }
    
    #region Full Speech Mode (Scores 1-3)
    
    void StartSpeechAnnouncements()
    {
        if (audioAnnouncementCoroutine != null)
            StopCoroutine(audioAnnouncementCoroutine);
        
        audioAnnouncementCoroutine = StartCoroutine(SpeechAnnouncementLoop());
        Debug.Log("Started TTS speech announcements");
    }
    
    IEnumerator SpeechAnnouncementLoop()
    {
        while (audioSystemActive && currentAudioMode == AudioMode.FullSpeech)
        {
            if (playerTransform != null && !isTTSSpeaking)
            {
                FindAndAnnounceSpeech();
            }
            
            yield return new WaitForSeconds(speechAnnouncementInterval);
        }
    }
    
    void FindAndAnnounceSpeech()
    {
        DetectableObject[] allObjects = FindObjectsOfType<DetectableObject>();
        List<ObjectDistance> nearbyObjects = new List<ObjectDistance>();
        
        foreach (DetectableObject obj in allObjects)
        {
            if (obj == null) continue;
            
            float distance = GetDistanceToObjectEdge(playerTransform.position, obj);
            
            if (distance <= speechDetectionRange)
            {
                nearbyObjects.Add(new ObjectDistance
                {
                    detectableObject = obj,
                    distance = distance,
                    direction = GetDirectionToObject(obj.transform.position)
                });
            }
        }
        
        totalNearbyObjects = nearbyObjects.Count;
        
        if (nearbyObjects.Count == 0)
        {
            currentClosestObject = null;
            currentClosestDistance = 0f;
            return;
        }
        
        // Sort by distance and announce closest
        nearbyObjects.Sort((a, b) => a.distance.CompareTo(b.distance));
        ObjectDistance closest = nearbyObjects[0];
        
        currentClosestObject = closest.detectableObject;
        currentClosestDistance = closest.distance;
        
        string speechText = CreateSpeechMessage(closest);
        StartCoroutine(SpeakWithTTS(speechText));
    }
    
    string CreateSpeechMessage(ObjectDistance objectDistance)
    {
        string message = "";
        
        // Add direction
        string direction = GetDirectionName(objectDistance.direction);
        if (!string.IsNullOrEmpty(direction))
        {
            message = direction + " ";
        }
        
        // Add object type
        message += objectDistance.detectableObject.className.ToLower();
        
        return message;
    }
    
    IEnumerator SpeakWithTTS(string text)
    {
        if (string.IsNullOrEmpty(text) || isTTSSpeaking)
            yield break;
        
        isTTSSpeaking = true;
        lastAnnouncementText = text;
        lastAnnouncementTime = Time.time;
        
        Debug.Log($"TTS Speech: \"{text}\"");
        
        // Use platform-specific TTS
        yield return StartCoroutine(PlatformSpecificTTS(text));
        
        isTTSSpeaking = false;
    }
    
    IEnumerator PlatformSpecificTTS(string text)
    {
        float estimatedDuration = EstimateSpeechDuration(text);
        
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        try
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.FileName = "powershell";
            startInfo.Arguments = $"-Command \"Add-Type -AssemblyName System.Speech; $synth = New-Object System.Speech.Synthesis.SpeechSynthesizer; $synth.Rate = {GetWindowsSpeechRate()}; $synth.Speak('{text}')\"";
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            
            System.Diagnostics.Process.Start(startInfo);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TTS Error: {e.Message}");
        }
        
        yield return new WaitForSeconds(estimatedDuration);
        
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        try
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.FileName = "say";
            startInfo.Arguments = $"-r {GetMacSpeechRate()} \"{text}\"";
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = true;
            
            System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo);
            
            float elapsedTime = 0f;
            while (elapsedTime < estimatedDuration && !process.HasExited)
            {
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TTS Error: {e.Message}");
            yield return new WaitForSeconds(estimatedDuration);
        }
        
#else
        // Fallback for other platforms
        Debug.Log($"TTS not supported on this platform: \"{text}\" (estimated {estimatedDuration:F1}s)");
        yield return new WaitForSeconds(estimatedDuration);
#endif
    }
    
    int GetWindowsSpeechRate()
    {
        // Windows TTS rate: -10 to +10 (where 0 is normal)
        float normalizedRate = (speechRate - 200f) / 100f;
        return Mathf.RoundToInt(Mathf.Clamp(normalizedRate, -10f, 10f));
    }
    
    int GetMacSpeechRate()
    {
        // Mac 'say' command rate: words per minute
        return Mathf.Clamp(speechRate, 100, 300);
    }
    
    float EstimateSpeechDuration(string text)
    {
        // Rough estimation: ~150 words per minute
        float wordsPerSecond = 150f / 60f;
        float charactersPerWord = 5f;
        float estimatedWords = text.Length / charactersPerWord;
        return Mathf.Max(0.8f, estimatedWords / wordsPerSecond + 0.3f);
    }
    
    #endregion
    
    #region Spearcon Mode (Scores 4-10)
    
    void StartSpearconAnnouncements(bool useClarityFiltering)
    {
        if (audioAnnouncementCoroutine != null)
            StopCoroutine(audioAnnouncementCoroutine);
        
        audioAnnouncementCoroutine = StartCoroutine(SpearconAnnouncementLoop(useClarityFiltering));
        
        string filterInfo = useClarityFiltering ? $" (beyond {objectClarityDistance}m)" : "";
        Debug.Log($"Started spearcon announcements{filterInfo}");
    }
    
    IEnumerator SpearconAnnouncementLoop(bool useClarityFiltering)
    {
        while (audioSystemActive && (currentAudioMode == AudioMode.StandardSpearcons || currentAudioMode == AudioMode.LimitedSpearcons))
        {
            if (playerTransform != null)
            {
                FindAndAnnounceSpearcon(useClarityFiltering);
            }
            
            yield return new WaitForSeconds(spearconAnnouncementInterval);
        }
    }
    
    void FindAndAnnounceSpearcon(bool useClarityFiltering)
    {
        DetectableObject[] allObjects = FindObjectsOfType<DetectableObject>();
        List<ObjectDistance> validObjects = new List<ObjectDistance>();
        int totalObjectsInRange = 0;
        int objectsFilteredOut = 0;
        
        Debug.Log($"=== SPEARCON OBJECT DETECTION ===");
        Debug.Log($"Mode: {(useClarityFiltering ? "LIMITED" : "STANDARD")} Spearcons");
        if (useClarityFiltering)
        {
            Debug.Log($"Filter: Only announce objects BEYOND {objectClarityDistance}m (clarity threshold)");
        }
        else
        {
            Debug.Log($"Filter: Announce ALL objects within {spearconDetectionRange}m");
        }
        
        foreach (DetectableObject obj in allObjects)
        {
            if (obj == null) continue;
            
            float distance = GetDistanceToObjectEdge(playerTransform.position, obj);
            
            // Check basic range first
            if (distance <= spearconDetectionRange)
            {
                totalObjectsInRange++;
                
                // Apply clarity distance filtering for limited mode
                bool shouldAnnounce = true;
                string filterReason = "";
                
                if (useClarityFiltering)
                {
                    // Only announce objects BEYOND clarity distance
                    shouldAnnounce = distance >= objectClarityDistance;
                    filterReason = shouldAnnounce ? 
                        $"beyond {objectClarityDistance}m threshold" : 
                        $"within {objectClarityDistance}m (too close - should be clear with vision)";
                    
                    if (!shouldAnnounce)
                        objectsFilteredOut++;
                }
                else
                {
                    filterReason = "within detection range";
                }
                
                if (enableDebugLogs && totalObjectsInRange <= 5) // Limit spam
                {
                    Debug.Log($"  {obj.className} at {distance:F1}m: {(shouldAnnounce ? "ANNOUNCE" : "SKIP")} - {filterReason}");
                }
                
                if (shouldAnnounce)
                {
                    // Check if we have an audio clip for this object type
                    AudioClip clip = GetAudioClipForObject(obj.className);
                    float volumeMultiplier = GetVolumeMultiplierForObject(obj.className);
                    
                    if (clip != null)
                    {
                        validObjects.Add(new ObjectDistance
                        {
                            detectableObject = obj,
                            distance = distance,
                            direction = GetDirectionToObject(obj.transform.position),
                            audioClip = clip,
                            volumeMultiplier = volumeMultiplier
                        });
                    }
                    else if (enableDebugLogs)
                    {
                        Debug.LogWarning($"  No audio clip configured for: {obj.className}");
                    }
                }
            }
        }
        
        totalNearbyObjects = validObjects.Count;
        
        Debug.Log($"DETECTION SUMMARY:");
        Debug.Log($"  Objects in range ({spearconDetectionRange}m): {totalObjectsInRange}");
        if (useClarityFiltering)
        {
            Debug.Log($"  Objects filtered out (too close): {objectsFilteredOut}");
        }
        Debug.Log($"  Objects with audio clips: {validObjects.Count}");
        
        if (validObjects.Count == 0)
        {
            currentClosestObject = null;
            currentClosestDistance = 0f;
            Debug.Log("  Result: No objects to announce");
            return;
        }
        
        // Sort by distance and announce closest
        validObjects.Sort((a, b) => a.distance.CompareTo(b.distance));
        ObjectDistance closest = validObjects[0];
        
        currentClosestObject = closest.detectableObject;
        currentClosestDistance = closest.distance;
        
        // Announce if timing allows
        if (CanAnnounceNow())
        {
            Debug.Log($"  Result: Announcing {closest.detectableObject.className} at {closest.distance:F1}m");
            StartCoroutine(AnnounceObjectSpatially(closest));
        }
        else
        {
            Debug.Log($"  Result: Timing blocked - last announcement {Time.time - lastAnnouncementTime:F1}s ago");
        }
    }
    
    AudioClip GetAudioClipForObject(string objectType)
    {
        if (audioClipDict.ContainsKey(objectType))
        {
            return audioClipDict[objectType];
        }
        
        // Try common fallbacks
        string[] fallbacks = { "Object", "Unknown", "Obstacle" };
        foreach (string fallback in fallbacks)
        {
            if (audioClipDict.ContainsKey(fallback))
            {
                return audioClipDict[fallback];
            }
        }
        
        return null;
    }
    
    float GetVolumeMultiplierForObject(string objectType)
    {
        foreach (AudioClipMapping mapping in objectAudioClips)
        {
            if (mapping.objectType == objectType)
            {
                return mapping.volumeMultiplier;
            }
        }
        return 1f;
    }
    
    bool CanAnnounceNow()
    {
        int activeCount = GetActiveAudioSourceCount();
        bool timeGapMet = Time.time - lastAnnouncementTime >= minimumAudioGap;
        
        return activeCount < maxSimultaneousAudio && timeGapMet;
    }
    
    int GetActiveAudioSourceCount()
    {
        int count = 0;
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            if (audioSourceInUse[i] && audioSourcePool[i].isPlaying)
            {
                count++;
            }
        }
        return count;
    }
    
    AudioSource GetAvailableAudioSource()
    {
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            if (!audioSourceInUse[i] && !audioSourcePool[i].isPlaying)
            {
                audioSourceInUse[i] = true;
                return audioSourcePool[i];
            }
        }
        return null;
    }
    
    void ReleaseAudioSource(AudioSource source)
    {
        int index = audioSourcePool.IndexOf(source);
        if (index >= 0)
        {
            audioSourceInUse[index] = false;
        }
    }
    
    IEnumerator AnnounceObjectSpatially(ObjectDistance objectDistance)
    {
        AudioSource audioSource = GetAvailableAudioSource();
        if (audioSource == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("No available audio sources");
            }
            yield break;
        }
        
        DetectableObject obj = objectDistance.detectableObject;
        float distance = objectDistance.distance;
        AudioClip clip = objectDistance.audioClip;
        
        // Position the audio source spatially
        if (enableSpatialAudio)
        {
            Vector3 spatialPosition = CalculateSpatialPosition(obj.transform.position);
            audioSource.transform.position = spatialPosition;
        }
        else
        {
            audioSource.transform.position = playerTransform.position;
        }
        
        // Calculate volume based on distance
        float distanceVolume = distanceVolumeRolloff.Evaluate(distance);
        float finalVolume = masterVolume * objectDistance.volumeMultiplier * distanceVolume;
        
        // Apply spatial volume mapping for limited spearcons
        if (currentAudioMode == AudioMode.LimitedSpearcons)
        {
            float maxDistance = spearconDetectionRange;
            float spatialVolumeMultiplier = Mathf.Lerp(1.0f, 0.3f, (distance - objectClarityDistance) / (maxDistance - objectClarityDistance));
            finalVolume *= spatialVolumeMultiplier;
        }
        
        finalVolume = Mathf.Max(finalVolume, minimumVolume);
        
        audioSource.volume = finalVolume;
        audioSource.clip = clip;
        lastAnnouncementTime = Time.time;
        audioSource.Play();
        
        // Wait for clip to finish
        yield return new WaitForSeconds(clip.length);
        
        // Release the audio source
        ReleaseAudioSource(audioSource);
        
        if (enableDebugLogs)
        {
            Debug.Log($"Played spearcon: {obj.className} (vol: {finalVolume:F2})");
        }
    }
    
    Vector3 CalculateSpatialPosition(Vector3 objectWorldPosition)
    {
        if (playerTransform == null) return Vector3.zero;
        
        Vector3 direction = GetDirectionToObject(objectWorldPosition);
        float distance = Vector3.Distance(playerTransform.position, objectWorldPosition);
        
        // Clamp distance for audio positioning
        float spatialDistance = Mathf.Min(distance, spatialRange);
        
        Vector3 worldDirection;
        if (playerCamera != null)
        {
            worldDirection = playerCamera.transform.TransformDirection(direction);
        }
        else
        {
            worldDirection = playerTransform.TransformDirection(direction);
        }
        
        return playerTransform.position + worldDirection * spatialDistance;
    }
    
    #endregion
    
    #region Utility Methods
    
    Vector3 GetDirectionToObject(Vector3 objectPosition)
    {
        if (playerTransform == null) return Vector3.zero;
        
        Vector3 direction = (objectPosition - playerTransform.position).normalized;
        
        if (playerCamera != null)
        {
            return playerCamera.transform.InverseTransformDirection(direction);
        }
        else
        {
            return playerTransform.InverseTransformDirection(direction);
        }
    }
    
    string GetDirectionName(Vector3 localDirection)
    {
        if (localDirection == Vector3.zero) return "";
        
        float absX = Mathf.Abs(localDirection.x);
        float absZ = Mathf.Abs(localDirection.z);
        
        if (absX > absZ)
        {
            // Primarily left/right
            if (localDirection.x > 0)
            {
                return absZ > 0.3f ? (localDirection.z > 0 ? "ahead right" : "behind right") : "right";
            }
            else
            {
                return absZ > 0.3f ? (localDirection.z > 0 ? "ahead left" : "behind left") : "left";
            }
        }
        else
        {
            // Primarily ahead/behind
            if (localDirection.z > 0)
            {
                return absX > 0.3f ? (localDirection.x > 0 ? "ahead right" : "ahead left") : "ahead";
            }
            else
            {
                return absX > 0.3f ? (localDirection.x > 0 ? "behind right" : "behind left") : "behind";
            }
        }
    }
    
    float GetDistanceToObjectEdge(Vector3 playerPosition, DetectableObject obj)
    {
        Bounds bounds = obj.worldBounds;
        
        if (bounds.size == Vector3.zero)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                bounds = renderer.bounds;
            }
            else
            {
                Collider collider = obj.GetComponent<Collider>();
                if (collider != null)
                {
                    bounds = collider.bounds;
                }
                else
                {
                    return Vector3.Distance(playerPosition, obj.transform.position);
                }
            }
        }
        
        Vector3 closestPoint = bounds.ClosestPoint(playerPosition);
        float distance = Vector3.Distance(playerPosition, closestPoint);
        
        if (distance < 0.1f)
            distance = 0.1f;
        
        return distance;
    }
    
    void StopAllAudio()
    {
        // Stop TTS
        if (ttsAudioSource != null && ttsAudioSource.isPlaying)
        {
            ttsAudioSource.Stop();
        }
        isTTSSpeaking = false;
        
        // Stop all spearcon audio sources
        foreach (AudioSource source in audioSourcePool)
        {
            if (source.isPlaying)
            {
                source.Stop();
            }
        }
        
        // Reset usage tracking
        for (int i = 0; i < audioSourceInUse.Count; i++)
        {
            audioSourceInUse[i] = false;
        }
        
        // Stop coroutine
        if (audioAnnouncementCoroutine != null)
        {
            StopCoroutine(audioAnnouncementCoroutine);
        }
    }
    
    #endregion
    
    #region Public API
    
    public AudioMode GetCurrentAudioMode() => currentAudioMode;
    public int GetCentralVisionScore() => centralVisionScore;
    public float GetObjectClarityDistance() => objectClarityDistance;
    public bool IsSystemActive() => audioSystemActive && systemInitialized;
    public DetectableObject GetCurrentClosestObject() => currentClosestObject;
    public float GetCurrentClosestDistance() => currentClosestDistance;
    public string GetLastAnnouncement() => lastAnnouncementText;
    public int GetTotalNearbyObjects() => totalNearbyObjects;
    public bool IsManualModeActive() => manualModeActive;
    
    public void EnableAudioSystem()
    {
        if ((ShouldEnableAudioForCurrentTrial() || manualModeActive) && currentAudioMode != AudioMode.Disabled)
        {
            StartAudioSystem();
            Debug.Log($"UnifiedAudioController: Enabled {currentAudioMode} mode");
        }
    }
    
    public void DisableAudioSystem()
    {
        StopAllAudio();
        audioSystemActive = false;
        Debug.Log("UnifiedAudioController: Audio system disabled");
    }
    
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        // Update TTS volume
        if (ttsAudioSource != null)
            ttsAudioSource.volume = masterVolume;
        
        // Update spearcon volumes
        foreach (AudioSource source in audioSourcePool)
        {
            if (!source.isPlaying) // Don't interrupt playing audio
            {
                source.volume = masterVolume;
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Master volume set to: {masterVolume:F2}");
        }
    }
    
    #endregion
    
    #region Testing and Debug Methods
    
    [ContextMenu("Test: Simulate Score 1-3 (Full Speech)")]
    public void TestFullSpeechMode()
    {
        centralVisionScore = 2;
        ConfigureAudioMode();
        StartAudioSystem();
        Debug.Log("Testing full speech mode (score 1-3)");
    }
    
    [ContextMenu("Test: Simulate Score 4-6 (Standard Spearcons)")]
    public void TestStandardSpearconMode()
    {
        centralVisionScore = 5;
        ConfigureAudioMode();
        StartAudioSystem();
        Debug.Log("Testing standard spearcon mode (score 4-6)");
    }
    
    [ContextMenu("Test: Simulate Score 7-10 (Limited Spearcons)")]
    public void TestLimitedSpearconMode()
    {
        centralVisionScore = 8;
        objectClarityDistance = 4f;
        ConfigureAudioMode();
        StartAudioSystem();
        Debug.Log("Testing limited spearcon mode (score 7-10)");
    }
    
    [ContextMenu("Test: Enable Manual Mode")]
    public void TestEnableManualMode()
    {
        EnableManualMode();
    }
    
    [ContextMenu("Test: Force TTS Speech")]
    public void TestForceTTSSpeech()
    {
        EnableManualMode();
        ForceAudioMode(AudioMode.FullSpeech);
    }
    
    [ContextMenu("Test: Force Standard Spearcons")]
    public void TestForceStandardSpearcons()
    {
        EnableManualMode();
        ForceAudioMode(AudioMode.StandardSpearcons);
    }
    
    [ContextMenu("Test: Force Limited Spearcons")]
    public void TestForceLimitedSpearcons()
    {
        EnableManualMode();
        SetObjectClarityDistance(5f);
        ForceAudioMode(AudioMode.LimitedSpearcons);
    }
    
    [ContextMenu("Test: Announce Closest Object Now")]
    public void TestAnnounceClosest()
    {
        if (currentAudioMode == AudioMode.FullSpeech)
        {
            FindAndAnnounceSpeech();
        }
        else if (currentAudioMode == AudioMode.StandardSpearcons)
        {
            FindAndAnnounceSpearcon(false);
        }
        else if (currentAudioMode == AudioMode.LimitedSpearcons)
        {
            FindAndAnnounceSpearcon(true);
        }
        else
        {
            Debug.Log("No audio mode active for manual testing");
        }
    }
    
    [ContextMenu("Debug: Show Audio Status")]
    public void DebugShowAudioStatus()
    {
        Debug.Log("=== UNIFIED AUDIO CONTROLLER STATUS ===");
        Debug.Log($"System Initialized: {systemInitialized}");
        Debug.Log($"Audio System Active: {audioSystemActive}");
        Debug.Log($"Manual Mode Active: {manualModeActive}");
        Debug.Log($"Allow Manual Override: {allowManualOverride}");
        Debug.Log($"Current Mode: {currentAudioMode}");
        Debug.Log($"Current Trial: {GetCurrentTrial()}");
        Debug.Log($"Should Enable Audio: {ShouldEnableAudioForCurrentTrial()}");
        Debug.Log($"Central Vision Score: {centralVisionScore}/10");
        Debug.Log($"Object Clarity Distance: {objectClarityDistance}m");
        Debug.Log($"Master Volume: {masterVolume:F2}");
        Debug.Log($"Configured Audio Clips: {audioClipDict.Count}");
        Debug.Log($"Active Audio Sources: {GetActiveAudioSourceCount()}");
        Debug.Log($"Player Found: {playerTransform != null}");
        Debug.Log($"Camera Found: {playerCamera != null}");
        Debug.Log($"TTS Speaking: {isTTSSpeaking}");
        Debug.Log($"Total Nearby Objects: {totalNearbyObjects}");
        
        if (currentClosestObject != null)
        {
            Debug.Log($"Current Closest: {currentClosestObject.className} at {currentClosestDistance:F1}m");
        }
        
        Debug.Log($"Last Announcement: \"{lastAnnouncementText}\" at {lastAnnouncementTime:F1}s");
    }
    
    [ContextMenu("Debug: List Audio Clips")]
    public void DebugListAudioClips()
    {
        Debug.Log($"CONFIGURED AUDIO CLIPS ({audioClipDict.Count} total):");
        foreach (var kvp in audioClipDict)
        {
            float duration = kvp.Value != null ? kvp.Value.length : 0f;
            Debug.Log($"  {kvp.Key} -> {(kvp.Value != null ? kvp.Value.name : "NULL")} ({duration:F1}s)");
        }
        
        if (audioClipDict.Count == 0)
        {
            Debug.LogWarning("No audio clips configured! Assign clips in the inspector.");
        }
    }
    
    [ContextMenu("Test: Stop All Audio")]
    public void TestStopAllAudio()
    {
        StopAllAudio();
        Debug.Log("Stopped all audio");
    }
    
    #endregion
    
    void Update()
    {
        // Clean up finished audio sources
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            if (audioSourceInUse[i] && !audioSourcePool[i].isPlaying)
            {
                audioSourceInUse[i] = false;
            }
        }
    }
    
    void OnDestroy()
    {
        audioSystemActive = false;
        StopAllAudio();
    }
}