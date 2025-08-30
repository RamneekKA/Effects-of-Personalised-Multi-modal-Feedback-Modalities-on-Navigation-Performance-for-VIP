using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Pre-recorded Spatial Audio Alert System
/// Uses pre-recorded AudioClips for true spatial audio positioning
/// Replaces the TTS-based SpatialAudioAlertSystem with clip-based approach
/// </summary>
public class PreRecordedSpatialAudioSystem : MonoBehaviour
{
    [Header("Audio Clip Configuration")]
    [Tooltip("Pre-recorded audio clips for each object type")]
    public AudioClipMapping[] objectAudioClips;
    
    [Header("Simple Alert Settings")]
    [Tooltip("How often to check and announce closest object (seconds)")]
    [Range(0.5f, 3f)]
    public float announceInterval = 1.5f;
    
    [Tooltip("Maximum range to detect objects (meters)")]
    [Range(5f, 50f)]
    public float detectionRange = 25f;
    
    [Header("Spatial Audio Settings")]
    [Tooltip("Enable spatial audio positioning")]
    public bool enableSpatialAudio = true;
    
    [Tooltip("Enable continuous audio announcements")]
    public bool audioEnabled = true;
    
    [Header("Volume Control")]
    [Tooltip("Master volume for all announcements")]
    [Range(0f, 1f)]
    public float masterVolume = 0.8f;
    
    [Tooltip("Volume rolloff curve based on distance")]
    public AnimationCurve distanceVolumeRolloff = AnimationCurve.EaseInOut(0f, 1f, 25f, 0.2f);
    
    [Tooltip("Minimum volume (prevents objects from becoming completely silent)")]
    [Range(0f, 0.5f)]
    public float minimumVolume = 0.1f;
    
    [Header("Anti-Overlap Audio Settings")]
    [Tooltip("Maximum number of simultaneous audio announcements")]
    [Range(1, 3)]
    public int maxSimultaneousAudio = 1;
    
    [Tooltip("Minimum time between audio announcements (seconds)")]
    [Range(0.1f, 2f)]
    public float minimumAudioGap = 0.3f;
    
    [Tooltip("Queue announcements instead of dropping them")]
    public bool useAudioQueue = true;
    
    [Tooltip("Maximum queue size")]
    [Range(3, 10)]
    public int maxQueueSize = 5;
    
    [Header("Spatial Configuration")]
    [Tooltip("How far left/right to position audio sources")]
    [Range(1f, 10f)]
    public float spatialRange = 5f;
    
    [Tooltip("3D spatial blend (0=2D, 1=full 3D)")]
    [Range(0f, 1f)]
    public float spatialBlend = 1f;
    
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    public bool showAudioDebugInfo = true;
    
    [Header("Current Status")]
    [SerializeField] private DetectableObject currentClosestObject;
    [SerializeField] private float currentClosestDistance;
    [SerializeField] private int totalNearbyObjects = 0;
    [SerializeField] private bool systemActive = false;
    [SerializeField] private int currentActiveAudioSources = 0;
    [SerializeField] private int currentQueuedAnnouncements = 0;
    
    // Internal state
    private Transform playerTransform;
    private Camera playerCamera;
    private Coroutine announcementCoroutine;
    
    // Audio clip management
    private Dictionary<string, AudioClip> audioClipDict = new Dictionary<string, AudioClip>();
    private Queue<QueuedAnnouncement> audioQueue = new Queue<QueuedAnnouncement>();
    private float lastAnnouncementTime = 0f;
    
    // Audio source pool for spatial positioning
    private const int AUDIO_SOURCE_POOL_SIZE = 8;
    private List<AudioSource> audioSourcePool = new List<AudioSource>();
    private List<bool> audioSourceInUse = new List<bool>();
    
    [System.Serializable]
    public class AudioClipMapping
    {
        public string objectType;
        public AudioClip audioClip;
        [Range(0f, 2f)]
        public float volumeMultiplier = 1f; // Per-object volume adjustment
    }
    
    [System.Serializable]
    private class ObjectDistance
    {
        public DetectableObject detectableObject;
        public float distance;
        public Vector3 direction; // Local direction from player
        public AudioClip audioClip;
        public float volumeMultiplier;
    }
    
    [System.Serializable]
    private class QueuedAnnouncement
    {
        public ObjectDistance objectDistance;
        public float queueTime;
    }
    
    void Start()
    {
        InitializeSystem();
        BuildAudioClipDictionary();
        CreateAudioSourcePool();
        StartContinuousAnnouncements();
    }
    
    void InitializeSystem()
    {
        // Find player transform and camera
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
        {
            playerCamera = Camera.main;
        }
        
        if (playerTransform == null)
        {
            Debug.LogError("PreRecordedSpatialAudioSystem: Could not find player transform!");
            return;
        }
        
        systemActive = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Pre-recorded Spatial Audio System initialized");
            Debug.Log($"Player: {playerTransform.name}");
            Debug.Log($"Camera: {(playerCamera != null ? playerCamera.name : "None")}");
            Debug.Log($"Detection Range: {detectionRange}m");
            Debug.Log($"Audio Clips Configured: {objectAudioClips.Length}");
        }
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
        
        // Warn about missing clips
        if (audioClipDict.Count == 0)
        {
            Debug.LogWarning("No audio clips configured! Please assign AudioClip mappings in the inspector.");
        }
    }
    
    void CreateAudioSourcePool()
    {
        for (int i = 0; i < AUDIO_SOURCE_POOL_SIZE; i++)
        {
            GameObject audioObj = new GameObject($"SpatialAudioSource_{i}");
            audioObj.transform.SetParent(transform);
            
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.spatialBlend = enableSpatialAudio ? spatialBlend : 0.0f;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.maxDistance = detectionRange;
            audioSource.dopplerLevel = 0f; // Disable doppler for alerts
            audioSource.playOnAwake = false;
            audioSource.volume = masterVolume;
            
            // Set up 3D audio settings for better spatial perception
            if (enableSpatialAudio)
            {
                audioSource.minDistance = 1f;
                audioSource.spread = 0f; // Directional audio
                audioSource.rolloffMode = AudioRolloffMode.Custom;
            }
            
            audioSourcePool.Add(audioSource);
            audioSourceInUse.Add(false);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Created audio source pool with {AUDIO_SOURCE_POOL_SIZE} sources");
        }
    }
    
    void StartContinuousAnnouncements()
    {
        if (announcementCoroutine != null)
        {
            StopCoroutine(announcementCoroutine);
        }
        
        announcementCoroutine = StartCoroutine(ContinuousAnnouncementLoop());
    }
    
    IEnumerator ContinuousAnnouncementLoop()
    {
        while (systemActive && audioEnabled)
        {
            if (playerTransform != null)
            {
                FindAndAnnounceClosestObject();
                ProcessAudioQueue();
            }
            
            yield return new WaitForSeconds(announceInterval);
        }
    }
    
    void FindAndAnnounceClosestObject()
    {
        DetectableObject[] allObjects = FindObjectsOfType<DetectableObject>();
        List<ObjectDistance> nearbyObjects = new List<ObjectDistance>();
        
        foreach (DetectableObject obj in allObjects)
        {
            if (obj == null) continue;
            
            // Calculate distance to object edge
            float edgeDistance = GetDistanceToObjectEdge(playerTransform.position, obj);
            
            if (edgeDistance <= detectionRange)
            {
                // Check if we have an audio clip for this object type
                AudioClip clip = GetAudioClipForObject(obj.className);
                float volumeMultiplier = GetVolumeMultiplierForObject(obj.className);
                
                if (clip != null)
                {
                    nearbyObjects.Add(new ObjectDistance
                    {
                        detectableObject = obj,
                        distance = edgeDistance,
                        direction = GetDirectionToObject(obj.transform.position),
                        audioClip = clip,
                        volumeMultiplier = volumeMultiplier
                    });
                }
            }
        }
        
        totalNearbyObjects = nearbyObjects.Count;
        
        if (nearbyObjects.Count == 0)
        {
            currentClosestObject = null;
            currentClosestDistance = 0f;
            return;
        }
        
        // Sort by distance and get the closest with audio clip
        nearbyObjects.Sort((a, b) => a.distance.CompareTo(b.distance));
        ObjectDistance closest = nearbyObjects[0];
        
        currentClosestObject = closest.detectableObject;
        currentClosestDistance = closest.distance;
        
        // Queue or announce the closest object
        QueueAnnouncement(closest);
        
        if (enableDebugLogs)
        {
            string directionText = GetDirectionName(closest.direction);
            Debug.Log($"Closest: {closest.detectableObject.className} at {closest.distance:F1}m {directionText} ({totalNearbyObjects} total)");
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
        return 1f; // Default volume
    }
    
    Vector3 GetDirectionToObject(Vector3 objectPosition)
    {
        if (playerTransform == null) return Vector3.zero;
        
        Vector3 direction = (objectPosition - playerTransform.position).normalized;
        
        if (playerCamera != null)
        {
            Vector3 localDirection = playerCamera.transform.InverseTransformDirection(direction);
            return localDirection;
        }
        else
        {
            Vector3 localDirection = playerTransform.InverseTransformDirection(direction);
            return localDirection;
        }
    }
    
    string GetDirectionName(Vector3 localDirection)
    {
        if (localDirection == Vector3.zero) return "";
        
        float absX = Mathf.Abs(localDirection.x);
        float absZ = Mathf.Abs(localDirection.z);
        
        if (absX > absZ)
        {
            return localDirection.x > 0 ? "right" : "left";
        }
        else
        {
            return localDirection.z > 0 ? "ahead" : "behind";
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
    
    void QueueAnnouncement(ObjectDistance objectDistance)
    {
        if (CanAnnounceNow())
        {
            StartCoroutine(AnnounceObjectSpatially(objectDistance));
        }
        else if (useAudioQueue)
        {
            QueuedAnnouncement queuedAnnouncement = new QueuedAnnouncement
            {
                objectDistance = objectDistance,
                queueTime = Time.time
            };
            
            // Remove oldest if queue is full
            while (audioQueue.Count >= maxQueueSize)
            {
                audioQueue.Dequeue();
            }
            
            audioQueue.Enqueue(queuedAnnouncement);
            currentQueuedAnnouncements = audioQueue.Count;
            
            if (showAudioDebugInfo)
            {
                Debug.Log($"Queued: {objectDistance.detectableObject.className} (Queue: {audioQueue.Count})");
            }
        }
    }
    
    bool CanAnnounceNow()
    {
        int activeCount = GetActiveAudioSourceCount();
        currentActiveAudioSources = activeCount;
        
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
    
    void ProcessAudioQueue()
    {
        currentQueuedAnnouncements = audioQueue.Count;
        
        while (audioQueue.Count > 0 && CanAnnounceNow())
        {
            QueuedAnnouncement queuedAnnouncement = audioQueue.Dequeue();
            
            // Check if announcement is still relevant (not too old)
            if (Time.time - queuedAnnouncement.queueTime < 5f)
            {
                StartCoroutine(AnnounceObjectSpatially(queuedAnnouncement.objectDistance));
            }
            else if (showAudioDebugInfo)
            {
                Debug.Log($"Expired: {queuedAnnouncement.objectDistance.detectableObject.className}");
            }
        }
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
            if (showAudioDebugInfo)
            {
                Debug.LogWarning("No available audio sources");
            }
            yield break;
        }
        
        DetectableObject obj = objectDistance.detectableObject;
        float distance = objectDistance.distance;
        Vector3 direction = objectDistance.direction;
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
        
        // Calculate final volume
        float distanceVolume = distanceVolumeRolloff.Evaluate(distance);
        float finalVolume = masterVolume * objectDistance.volumeMultiplier * distanceVolume;
        finalVolume = Mathf.Max(finalVolume, minimumVolume); // Apply minimum volume
        
        audioSource.volume = finalVolume;
        audioSource.clip = clip;
        
        // Update timing and play
        lastAnnouncementTime = Time.time;
        audioSource.Play();
        
        // Wait for clip to finish
        yield return new WaitForSeconds(clip.length);
        
        // Release the audio source
        ReleaseAudioSource(audioSource);
        
        if (enableDebugLogs)
        {
            string positionInfo = enableSpatialAudio ? $" at {audioSource.transform.position}" : "";
            Debug.Log($"Played: {obj.className} (vol: {finalVolume:F2}){positionInfo}");
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
        {
            distance = 0.1f;
        }
        
        return distance;
    }
    
    // PUBLIC CONTROL METHODS
    
    public void EnableAudio()
    {
        audioEnabled = true;
        StartContinuousAnnouncements();
        
        if (enableDebugLogs)
        {
            Debug.Log("Pre-recorded spatial audio enabled");
        }
    }
    
    public void DisableAudio()
    {
        audioEnabled = false;
        
        // Stop all playing audio
        foreach (AudioSource source in audioSourcePool)
        {
            if (source.isPlaying)
            {
                source.Stop();
            }
        }
        
        // Clear queue
        audioQueue.Clear();
        currentQueuedAnnouncements = 0;
        
        // Reset usage
        for (int i = 0; i < audioSourceInUse.Count; i++)
        {
            audioSourceInUse[i] = false;
        }
        
        if (announcementCoroutine != null)
        {
            StopCoroutine(announcementCoroutine);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log("Pre-recorded spatial audio disabled");
        }
    }
    
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        // Update all audio sources
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
    
    public void SetSpatialAudioEnabled(bool enabled)
    {
        enableSpatialAudio = enabled;
        
        foreach (AudioSource source in audioSourcePool)
        {
            source.spatialBlend = enabled ? spatialBlend : 0.0f;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Spatial audio {(enabled ? "enabled" : "disabled")}");
        }
    }
    
    public void SetDetectionRange(float range)
    {
        detectionRange = Mathf.Clamp(range, 5f, 100f);
        
        foreach (AudioSource source in audioSourcePool)
        {
            source.maxDistance = detectionRange;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Detection range set to: {detectionRange}m");
        }
    }
    
    // STATUS METHODS
    
    public bool IsAudioEnabled() => audioEnabled;
    public DetectableObject GetCurrentClosestObject() => currentClosestObject;
    public float GetCurrentClosestDistance() => currentClosestDistance;
    public int GetTotalNearbyObjects() => totalNearbyObjects;
    public int GetActiveAudioCount() => GetActiveAudioSourceCount();
    public int GetQueuedAnnouncementCount() => audioQueue.Count;
    public int GetConfiguredClipCount() => audioClipDict.Count;
    
    // CONTEXT MENU TESTING METHODS
    
    [ContextMenu("Test: Play Test Announcement")]
    public void TestPlayAnnouncement()
    {
        if (audioClipDict.Count > 0)
        {
            var firstClip = audioClipDict.First();
            Debug.Log($"Testing audio clip: {firstClip.Key}");
            
            // Create a simple test without using AnnounceObjectSpatially
            StartCoroutine(PlayTestAudioDirect(firstClip.Value, firstClip.Key));
        }
        else
        {
            Debug.LogWarning("No audio clips configured for testing");
        }
    }
    
    IEnumerator PlayTestAudioDirect(AudioClip clip, string objectType)
    {
        AudioSource audioSource = GetAvailableAudioSource();
        if (audioSource == null)
        {
            Debug.LogWarning("No available audio sources for test");
            yield break;
        }
        
        // Position audio source in front of player for test
        if (playerTransform != null)
        {
            audioSource.transform.position = playerTransform.position + playerTransform.forward * 3f;
        }
        
        audioSource.volume = masterVolume;
        audioSource.clip = clip;
        audioSource.Play();
        
        Debug.Log($"Playing test audio: {objectType} ({clip.length:F1}s duration)");
        
        // Wait for clip to finish
        yield return new WaitForSeconds(clip.length);
        
        ReleaseAudioSource(audioSource);
    }
    
    [ContextMenu("Debug: Show Audio Status")]
    public void DebugShowAudioStatus()
    {
        Debug.Log("=== PRE-RECORDED SPATIAL AUDIO STATUS ===");
        Debug.Log($"System Active: {systemActive}");
        Debug.Log($"Audio Enabled: {audioEnabled}");
        Debug.Log($"Spatial Audio: {enableSpatialAudio}");
        Debug.Log($"Master Volume: {masterVolume:F2}");
        Debug.Log($"Detection Range: {detectionRange}m");
        Debug.Log($"Configured Clips: {audioClipDict.Count}");
        Debug.Log($"Active Audio Sources: {GetActiveAudioSourceCount()}");
        Debug.Log($"Queued Announcements: {audioQueue.Count}");
        Debug.Log($"Player Found: {playerTransform != null}");
        Debug.Log($"Camera Found: {playerCamera != null}");
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
        systemActive = false;
        
        if (announcementCoroutine != null)
        {
            StopCoroutine(announcementCoroutine);
        }
        
        foreach (AudioSource source in audioSourcePool)
        {
            if (source != null && source.isPlaying)
            {
                source.Stop();
            }
        }
    }
}