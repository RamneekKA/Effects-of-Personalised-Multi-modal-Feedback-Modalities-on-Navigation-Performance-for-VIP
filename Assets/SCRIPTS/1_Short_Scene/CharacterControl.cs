using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

namespace FCG
{
    /// <summary>
    /// Updated Character Controller with VR Support and Unified Audio System Integration
    /// VR Mode: Head tracking for looking, left joystick for movement, right joystick for camera rotation
    /// Traditional Mode: Mouse look + WASD movement
    /// </summary>
    public class CharacterControl : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float speed = 10.0f;
        public float sensitivity = 100f;

        [Header("VR Settings")]
        [Tooltip("Enable VR mode - disables mouse look, enables VR head tracking + controller input")]
        public bool enableVR = false;
        
        [Tooltip("VR Camera Rig (XR Origin or XR Rig)")]
        public Transform vrCameraRig;
        
        [Tooltip("Main VR Camera (usually child of VR rig)")]
        public Camera vrCamera;

        [Header("Human Body Setup")]
        public GameObject humanBodyPrefab;
        public bool useHumanBodyCollision = true;
        public float cameraHeightOffset = 1.6f;

        [Header("Navigation Tracking")]
        public float dataLogInterval = 0.5f;
        public float screenshotInterval = 3.0f;
        public float nearbyObjectRange = 25f;
        public bool enableTracking = true;
        
        [Header("Route Configuration")]
        public RouteGuideSystem routeGuideSystem;
        
        [Header("Session Integration")]
        public bool useSessionManager = true;
        
        [Header("Pre-Analysis Coordination")]
        [Tooltip("Wait for scene pre-analysis before enabling navigation")]
        public bool waitForPreAnalysis = true;
        private bool preAnalysisCompleted = false;
        private GeminiScenePreAnalyzer geminiPreAnalyzer;
        
        [Header("Unified Audio Integration")]
        [Tooltip("Reference to the unified audio controller")]
        public UnifiedAudioController unifiedAudioController;
        
        [Tooltip("Enable audio enhancements based on assessment")]
        public bool enableAudioEnhancements = true;
        
        [Header("Ambient Audio Settings")]
        [Tooltip("Background audio clip to play during navigation")]
        public AudioClip ambientNavigationClip;
        
        [Tooltip("Volume level for ambient audio (0-1)")]
        [Range(0f, 1f)]
        public float ambientVolume = 0.3f;
        
        [Tooltip("Play ambient audio only during navigation trials")]
        public bool playOnlyDuringNavigation = true;
        
        [Tooltip("Fade in/out duration when starting/stopping audio")]
        public float audioFadeDuration = 2f;
        
        [Header("Collision Detection Settings")]
        public LayerMask obstacleLayerMask = 1 << 8;
        public LayerMask ignoreCollisionLayers = 1 << 0 | (1 << 13);

        // Movement and camera
        private float xRotation = 0f;
        private float yRotation = 0f;
        private Transform cam;
        private CharacterController charController;
        private Vector3 initialPosition = Vector3.zero;
        
        // Body detection
        private GameObject humanBodyInstance;
        private SimpleBodyZoneDetector bodyZoneDetector;

        // Navigation tracking
        private List<NavigationDataPoint> navigationData = new List<NavigationDataPoint>();
        private float lastLogTime = 0f;
        private float lastScreenshotTime = 0f;
        private int screenshotCounter = 0;
        private Vector3 lastFramePosition;
        private string currentTrialType;
        private string trialDataPath;
        
        // Collision state tracking
        private HashSet<string> currentCollisions = new HashSet<string>();
        private Vector3 lastFrameVelocity = Vector3.zero;
        
        // Session management
        private bool navigationEnabled = false;
        private UserSession currentSession;
        
        // Ambient audio management
        private AudioSource ambientAudioSource;
        private bool isAudioPlaying = false;
        private Coroutine audioFadeCoroutine;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            charController = GetComponent<CharacterController>();
            
            // Setup VR or traditional camera
            if (enableVR)
            {
                SetupVR();
            }
            else
            {
                SetupTraditionalCamera();
            }

            bodyZoneDetector = gameObject.AddComponent<SimpleBodyZoneDetector>();
            
            // Initialize rotations
            yRotation = transform.eulerAngles.y;
            xRotation = 0f;
            
            if (cam != null && !enableVR)
                cam.localRotation = Quaternion.identity;
            
            initialPosition = transform.position;
            lastFramePosition = transform.position;

            // Find RouteGuideSystem if not assigned
            if (routeGuideSystem == null)
                routeGuideSystem = FindObjectOfType<RouteGuideSystem>();

            // Find UnifiedAudioController if not assigned
            if (unifiedAudioController == null)
                unifiedAudioController = FindObjectOfType<UnifiedAudioController>();

            // Find the pre-analyzer
            geminiPreAnalyzer = FindObjectOfType<GeminiScenePreAnalyzer>();
            
            // Subscribe to pre-analysis events
            GeminiScenePreAnalyzer.OnPreAnalysisCompleted += OnPreAnalysisCompleted;
            GeminiScenePreAnalyzer.OnPreAnalysisFailed += OnPreAnalysisFailed;

            // Setup ambient audio
            SetupAmbientAudio();

            // Initialize session-based configuration
            if (useSessionManager)
            {
                InitializeWithSessionManager();
            }
            else
            {
                InitializeStandaloneMode();
            }

            Debug.Log($"Character Controller initialized in {(enableVR ? "VR" : "Traditional")} mode");
        }

        void SetupVR()
        {
            Debug.Log("Setting up VR mode");
            
            // Find VR components if not assigned
            if (vrCameraRig == null)
            {
                // Try to find XR Origin by name (works without package dependency)
                GameObject xrOrigin = GameObject.Find("XR Origin (VR)") ?? 
                                     GameObject.Find("XR Origin") ?? 
                                     GameObject.Find("XR Rig") ??
                                     GameObject.Find("VR Camera Rig");
                
                if (xrOrigin != null)
                {
                    vrCameraRig = xrOrigin.transform;
                    Debug.Log($"Found VR rig: {xrOrigin.name}");
                }
                else
                {
                    Debug.LogError("No VR rig found! Create an XR Origin in your scene first.");
                }
            }
            
            if (vrCamera == null && vrCameraRig != null)
            {
                vrCamera = vrCameraRig.GetComponentInChildren<Camera>();
            }
            
            if (vrCameraRig != null)
            {
                // Position VR rig at character position
                vrCameraRig.position = transform.position;
                
                // Make VR rig follow character (this is key - head won't move scene!)
                vrCameraRig.SetParent(transform);
                vrCameraRig.localPosition = new Vector3(0, cameraHeightOffset, 0);
                vrCameraRig.localRotation = Quaternion.identity;
                
                // Update camera reference for other systems
                cam = vrCamera?.transform;
                
                // Ensure VR camera is main camera
                if (vrCamera != null)
                {
                    vrCamera.tag = "MainCamera";
                    
                    // Disable any existing main cameras
                    Camera[] allCameras = FindObjectsOfType<Camera>();
                    foreach (Camera cam in allCameras)
                    {
                        if (cam != vrCamera && cam.tag == "MainCamera")
                        {
                            cam.gameObject.SetActive(false);
                            Debug.Log($"Disabled old camera: {cam.name}");
                        }
                    }
                }
                
                Debug.Log("VR setup complete - head tracking for looking, left stick for movement, right stick for camera rotation");
                Debug.Log($"VR Rig parented to character - head movement will NOT move the scene");
            }
            else
            {
                Debug.LogError("VR Camera Rig not found! Make sure XR Origin is in the scene.");
                Debug.LogError("Falling back to traditional camera setup...");
                enableVR = false;
                SetupTraditionalCamera();
            }
        }

        void SetupTraditionalCamera()
        {
            // Setup human body if enabled
            if (useHumanBodyCollision && humanBodyPrefab != null)
            {
                SetupHumanBodySimple();
            }
            else
            {
                cam = transform.Find("Camera");
                if (cam == null)
                    cam = Camera.main?.transform;
            }
        }

        void SetupAmbientAudio()
        {
            if (ambientNavigationClip == null)
            {
                Debug.LogWarning("No ambient navigation clip assigned - audio will not play");
                return;
            }

            // Create a dedicated AudioSource for ambient audio
            GameObject ambientAudioObj = new GameObject("AmbientNavigationAudio");
            ambientAudioObj.transform.SetParent(transform);
            
            ambientAudioSource = ambientAudioObj.AddComponent<AudioSource>();
            ambientAudioSource.clip = ambientNavigationClip;
            ambientAudioSource.loop = true;
            ambientAudioSource.volume = 0f; // Start at 0, will fade in when needed
            ambientAudioSource.spatialBlend = 0f; // 2D audio (non-positional)
            ambientAudioSource.playOnAwake = false;
            ambientAudioSource.priority = 128;
            
            Debug.Log($"Ambient audio setup complete with clip: {ambientNavigationClip.name}");
        }

        void InitializeWithSessionManager()
        {
            if (SessionManager.Instance == null)
            {
                Debug.LogWarning("No SessionManager found! Using standalone mode.");
                InitializeStandaloneMode();
                return;
            }

            currentSession = SessionManager.Instance.GetCurrentSession();
            currentTrialType = SessionManager.Instance.GetCurrentTrial();
            
            Debug.Log($"Configuring for trial: {currentTrialType}");
            
            // Configure based on trial type
            ConfigureForTrial(currentTrialType);
            
            // Subscribe to session events
            SessionManager.OnTrialChanged += OnTrialChanged;
            
            // Check if pre-analysis is required for this trial
            if (ShouldWaitForPreAnalysis())
            {
                Debug.Log("Waiting for scene pre-analysis to complete...");
                ShowPreAnalysisWaitingMessage();
            }
            else
            {
                Debug.Log("No pre-analysis required, enabling navigation immediately");
                EnableNavigationForTrial();
            }
        }

        void InitializeStandaloneMode()
        {
            navigationEnabled = true;
            currentTrialType = "standalone";
            
            if (enableTracking)
            {
                InitializeTracking();
            }
            
            // Start ambient audio in standalone mode if not restricted to navigation
            if (!playOnlyDuringNavigation)
            {
                StartAmbientAudio();
            }
            
            // Enable audio enhancements in standalone mode
            if (enableAudioEnhancements && unifiedAudioController != null)
            {
                unifiedAudioController.EnableAudioSystem();
            }
            
            Debug.Log("Running in standalone mode with unified audio system");
        }

        bool ShouldWaitForPreAnalysis()
        {
            if (!waitForPreAnalysis) return false;
            
            // Only wait for pre-analysis on navigation trials
            if (!SessionManager.Instance.IsNavigationTrial(currentTrialType)) return false;
            
            // Check if pre-analysis is already completed
            if (geminiPreAnalyzer != null && geminiPreAnalyzer.IsAnalysisCompleted())
            {
                Debug.Log("Pre-analysis already completed");
                preAnalysisCompleted = true;
                return false;
            }
            
            return true;
        }

        void OnPreAnalysisCompleted()
        {
            Debug.Log("Pre-analysis completed! Enabling navigation...");
            preAnalysisCompleted = true;
            EnableNavigationForTrial();
        }

        void OnPreAnalysisFailed(string errorMessage)
        {
            Debug.LogWarning($"Pre-analysis failed: {errorMessage}");
            Debug.LogWarning("Proceeding with navigation anyway...");
            preAnalysisCompleted = true;
            EnableNavigationForTrial();
        }

        void EnableNavigationForTrial()
        {
            if (SessionManager.Instance.IsNavigationTrial(currentTrialType))
            {
                navigationEnabled = true;
                if (enableTracking)
                {
                    InitializeTracking();
                }
                
                // Start ambient audio for navigation trials
                if (playOnlyDuringNavigation)
                {
                    StartAmbientAudio();
                }
                
                // Initialize unified audio system for navigation trials
                if (enableAudioEnhancements)
                {
                    InitializeUnifiedAudio();
                }
                
                Debug.Log($"Navigation enabled for trial: {currentTrialType}");
                Debug.Log("You can now navigate the route!");
            }
            else
            {
                navigationEnabled = false;
                
                // Stop ambient audio for non-navigation trials
                if (playOnlyDuringNavigation && isAudioPlaying)
                {
                    StopAmbientAudio();
                }
                
                // Disable unified audio for non-navigation trials
                if (unifiedAudioController != null)
                {
                    unifiedAudioController.DisableAudioSystem();
                }
                
                Debug.Log($"Navigation disabled for assessment trial: {currentTrialType}");
            }
        }

        void InitializeUnifiedAudio()
        {
            if (unifiedAudioController == null)
            {
                Debug.LogWarning("CharacterControl: No UnifiedAudioController found - audio enhancements disabled");
                return;
            }
            
            // Check if this is an enhanced trial that should use audio
            bool shouldUseAudio = IsAudioEnhancedTrial(currentTrialType);
            
            if (shouldUseAudio)
            {
                // Enable the unified audio system
                unifiedAudioController.EnableAudioSystem();
                Debug.Log($"CharacterControl: Enabled unified audio enhancements for trial: {currentTrialType}");
                
                // Log the audio mode that was configured
                var audioMode = unifiedAudioController.GetCurrentAudioMode();
                var visionScore = unifiedAudioController.GetCentralVisionScore();
                Debug.Log($"Audio Enhancement Mode: {audioMode} (Vision Score: {visionScore}/10)");
                
                if (audioMode == UnifiedAudioController.AudioMode.LimitedSpearcons)
                {
                    var clarityDistance = unifiedAudioController.GetObjectClarityDistance();
                    Debug.Log($"Limited spearcons: announce objects beyond {clarityDistance}m");
                }
            }
            else
            {
                Debug.Log($"CharacterControl: No audio enhancements for trial: {currentTrialType}");
            }
        }
        
        bool IsAudioEnhancedTrial(string trialType)
        {
            // Audio enhancements should be used for algorithmic trials only
            return trialType == "short_algorithmic" || trialType == "long_algorithmic";
        }

        void ShowPreAnalysisWaitingMessage()
        {
            Debug.Log("Scene analysis in progress - navigation will begin when complete");
        }

        void ConfigureForTrial(string trialType)
        {
            currentTrialType = trialType;
            trialDataPath = SessionManager.Instance.GetTrialDataPath(trialType);
            
            // Configure route based on trial
            ConfigureRoute(trialType);

            // Configure default navigation line settings for baseline trial
            if (trialType == "baseline" && routeGuideSystem != null)
            {
                routeGuideSystem.SetLineWidth(0.2f);
                routeGuideSystem.SetRouteOpacity(0.4f);
                Debug.Log("Baseline trial: Applied default navigation line settings (width: 0.2, opacity: 0.4)");
            }
            
            Debug.Log($"Trial configuration complete for: {trialType}");
        }

        void ConfigureRoute(string trialType)
        {
            if (routeGuideSystem == null) return;
            
            string routeType = SessionManager.Instance.GetRouteType(trialType);
            
            switch (routeType)
            {
                case "short":
                    Debug.Log("Configured for SHORT distance route (15-25m)");
                    break;
                case "long":
                    Debug.Log("Configured for LONG distance route (50-75m)");
                    break;
                case "none":
                    Debug.Log("No route configuration needed for assessment trial");
                    break;
            }
        }

        void OnTrialChanged(string newTrial)
        {
            Debug.Log($"Trial changed to: {newTrial}");
            ConfigureForTrial(newTrial);
            
            // Reconfigure unified audio for new trial
            if (enableAudioEnhancements && navigationEnabled)
            {
                InitializeUnifiedAudio();
            }
        }

        void InitializeTracking()
        {
            // Only initialize if pre-analysis is complete or not required
            if (!preAnalysisCompleted && waitForPreAnalysis && SessionManager.Instance.IsNavigationTrial(currentTrialType))
            {
                Debug.LogWarning("Attempted to initialize tracking before pre-analysis complete");
                return;
            }
            
            string sessionID = $"{currentTrialType}_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            
            // Create screenshots folder in trial directory
            string screenshotsPath = Path.Combine(trialDataPath, "Screenshots");
            Directory.CreateDirectory(screenshotsPath);
            
            Debug.Log($"Navigation tracking initialized for {currentTrialType}");
            Debug.Log($"Data path: {trialDataPath}");
        }

        void Update()
        {
            // Only allow movement and tracking if pre-analysis is done and navigation is enabled
            if (navigationEnabled && (preAnalysisCompleted || !waitForPreAnalysis || !SessionManager.Instance.IsNavigationTrial(currentTrialType)))
            {
                CameraMovement();
                MoveCharacter();

                // Track velocity for collision analysis
                Vector3 currentVelocity = (transform.position - lastFramePosition) / Time.deltaTime;
                lastFrameVelocity = currentVelocity;

                // Navigation tracking
                if (enableTracking)
                {
                    UpdateNavigationTracking();
                }

                // Reset position if player falls
                if (transform.position.y < -10)
                {
                    transform.position = initialPosition;
                }
            }
        }

        void UpdateNavigationTracking()
        {
            float currentTime = Time.time;

            if (currentTime - lastLogTime >= dataLogInterval)
            {
                LogNavigationData();
                lastLogTime = currentTime;
            }

            if (currentTime - lastScreenshotTime >= screenshotInterval)
            {
                CaptureScreenshot();
                lastScreenshotTime = currentTime;
            }
        }

        void LogNavigationData()
        {
            float currentSpeed = Vector3.Distance(transform.position, lastFramePosition) / dataLogInterval;
            List<NearbyObject> nearbyObjects = GetNearbyObjects();

            NavigationDataPoint dataPoint = new NavigationDataPoint
            {
                timestamp = Time.time,
                position = transform.position,
                rotation = transform.rotation.eulerAngles,
                currentSpeed = currentSpeed,
                nearbyObjects = nearbyObjects,
                screenshotPath = "",
                signedDeviationFromRoute = routeGuideSystem != null ? 
                    routeGuideSystem.GetSignedDeviationFromRoute(transform.position) : 0f
            };

            navigationData.Add(dataPoint);
            lastFramePosition = transform.position;
        }

        List<NearbyObject> GetNearbyObjects()
        {
            List<NearbyObject> nearbyObjects = new List<NearbyObject>();
            DetectableObject[] allDetectable = FindObjectsOfType<DetectableObject>();

            foreach (DetectableObject obj in allDetectable)
            {
                if (humanBodyInstance != null && obj.transform.IsChildOf(humanBodyInstance.transform))
                    continue;

                float distance = Vector3.Distance(transform.position, obj.transform.position);
                
                if (distance <= nearbyObjectRange)
                {
                    Vector3 directionToObject = (obj.transform.position - transform.position).normalized;
                    float angle = Vector3.SignedAngle(transform.forward, directionToObject, Vector3.up);

                    NearbyObject nearbyObj = new NearbyObject
                    {
                        className = obj.className,
                        distance = distance,
                        angle = angle,
                        worldPosition = obj.transform.position,
                        objectName = obj.gameObject.name
                    };

                    nearbyObjects.Add(nearbyObj);
                }
            }

            return nearbyObjects;
        }

        void CaptureScreenshot()
        {
            // Only capture if pre-analysis is complete or not required
            if (!preAnalysisCompleted && waitForPreAnalysis && SessionManager.Instance.IsNavigationTrial(currentTrialType))
            {
                return;
            }
            
            if (cam == null) return;

            screenshotCounter++;
            string filename = $"screenshot_{screenshotCounter:D4}_{Time.time:F1}s.png";
            string fullPath = Path.Combine(trialDataPath, "Screenshots", filename);

            ScreenCapture.CaptureScreenshot(fullPath);
            
            if (navigationData.Count > 0)
            {
                navigationData[navigationData.Count - 1].screenshotPath = filename;
            }

            Debug.Log($"Screenshot saved: {filename}");
        }

        // Enhanced collision detection
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!enableTracking || !navigationEnabled) return;
            
            int hitLayer = hit.collider.gameObject.layer;
            
            if ((ignoreCollisionLayers.value & (1 << hitLayer)) != 0)
                return;
            
            if ((obstacleLayerMask.value & (1 << hitLayer)) == 0)
                return;
            
            string collisionID = "Collision_" + hit.collider.gameObject.name;
            if (currentCollisions.Contains(collisionID))
                return;
                
            currentCollisions.Add(collisionID);
            StartCoroutine(RemoveCollisionAfterDelay(collisionID, 1.0f));

            // Enhanced object info
            DetectableObject detectableObj = hit.gameObject.GetComponent<DetectableObject>();
            string objectType;
            string objectDisplayName;
            
            if (detectableObj != null && !string.IsNullOrEmpty(detectableObj.className))
            {
                objectType = detectableObj.className;
                objectDisplayName = hit.collider.gameObject.name;
            }
            else
            {
                objectDisplayName = hit.collider.gameObject.name;
                objectType = GetObjectTypeFromNameAndLayer(hit.collider.gameObject);
                
                if (objectType == "Unknown Object")
                {
                    objectType = CleanObjectName(objectDisplayName);
                }
            }

            // Determine body part
            string bodyPart = "Unknown";
            if (bodyZoneDetector != null)
            {
                bodyPart = bodyZoneDetector.DetermineBodyPartFromCollisionAdvanced(hit.point, hit.normal);
            }
            
            LogBodyPartCollision(bodyPart, objectType, objectDisplayName, hit.point);
        }

        IEnumerator RemoveCollisionAfterDelay(string collisionID, float delay)
        {
            yield return new WaitForSeconds(delay);
            currentCollisions.Remove(collisionID);
        }

        void LogBodyPartCollision(string bodyPart, string objectType, string objectName, Vector3 collisionPoint)
        {
            float collisionSpeed = lastFrameVelocity.magnitude;
            
            Debug.Log($"COLLISION: {bodyPart} hit {objectType} ({objectName}) at {collisionSpeed:F2}m/s");

            NavigationDataPoint collisionData = new NavigationDataPoint
            {
                timestamp = Time.time,
                position = transform.position,
                rotation = transform.rotation.eulerAngles,
                currentSpeed = 0f,
                nearbyObjects = GetNearbyObjects(),
                screenshotPath = "",
                isCollision = true,
                collisionObject = objectType,
                collisionPoint = collisionPoint,
                bodyPartInvolved = bodyPart,
                collisionVelocity = collisionSpeed,
                approachDirection = transform.forward,
                timeNearObjectBeforeCollision = 0f,
                signedDeviationFromRoute = routeGuideSystem != null ? 
                    routeGuideSystem.GetSignedDeviationFromRoute(transform.position) : 0f
            };

            navigationData.Add(collisionData);
            CaptureCollisionScreenshot();
        }

        void CaptureCollisionScreenshot()
        {
            screenshotCounter++;
            string filename = $"collision_{screenshotCounter:D4}_{Time.time:F1}s.png";
            string fullPath = Path.Combine(trialDataPath, "Screenshots", filename);
            
            ScreenCapture.CaptureScreenshot(fullPath);
            
            if (navigationData.Count > 0)
            {
                navigationData[navigationData.Count - 1].screenshotPath = filename;
            }
            
            Debug.Log($"Collision screenshot saved: {filename}");
        }

        public void CompleteNavigationTrial()
        {
            if (!SessionManager.Instance.IsNavigationTrial(currentTrialType))
            {
                Debug.LogWarning("Cannot complete navigation trial - current trial is not a navigation trial");
                return;
            }

            enableTracking = false;
            
            // Stop ambient audio when navigation trial completes
            if (isAudioPlaying)
            {
                StopAmbientAudio();
            }
            
            // Stop unified audio when trial completes
            if (unifiedAudioController != null)
            {
                unifiedAudioController.DisableAudioSystem();
            }
            
            SaveNavigationData();
            
            Debug.Log($"Navigation trial '{currentTrialType}' completed");
        }

        void SaveNavigationData()
        {
            if (!enableTracking || navigationData.Count == 0) return;

            string jsonPath = Path.Combine(trialDataPath, "navigation_data.json");
            
            // Calculate session statistics
            int totalCollisions = navigationData.Where(dp => dp.isCollision).Count();
            var collisionsByBodyPart = navigationData
                .Where(dp => dp.isCollision && !string.IsNullOrEmpty(dp.bodyPartInvolved))
                .GroupBy(dp => dp.bodyPartInvolved)
                .ToDictionary(g => g.Key, g => g.Count());
            var collisionsByObjectType = navigationData
                .Where(dp => dp.isCollision)
                .GroupBy(dp => dp.collisionObject)
                .ToDictionary(g => g.Key, g => g.Count());
            
            // Calculate deviation statistics
            var deviationValues = navigationData.Select(dp => dp.signedDeviationFromRoute).ToList();
            float averageAbsoluteDeviation = deviationValues.Select(Mathf.Abs).Average();
            float averageSignedDeviation = deviationValues.Average();
            float maximumDeviation = deviationValues.Select(Mathf.Abs).Max();
            float minimumDeviation = deviationValues.Select(Mathf.Abs).Min();
            
            float timeOffRoute = navigationData.Count(dp => Mathf.Abs(dp.signedDeviationFromRoute) > 2f) * dataLogInterval;
            
            // Calculate timing
            float startTime = navigationData.Count > 0 ? navigationData[0].timestamp : 0f;
            float endTime = navigationData.Count > 0 ? navigationData[navigationData.Count - 1].timestamp : 0f;
            float duration = endTime - startTime;
            
            // Calculate average speed
            float totalDistance = 0f;
            for (int i = 1; i < navigationData.Count; i++)
            {
                totalDistance += Vector3.Distance(navigationData[i].position, navigationData[i-1].position);
            }
            float averageSpeed = duration > 0 ? totalDistance / duration : 0f;

            NavigationSession session = new NavigationSession
            {
                sessionID = $"{currentTrialType}_{System.DateTime.Now:yyyyMMdd_HHmmss}",
                trialType = currentTrialType,
                routeType = SessionManager.Instance.GetRouteType(currentTrialType),
                startTime = startTime,
                endTime = endTime,
                duration = duration,
                totalDataPoints = navigationData.Count,
                dataPoints = navigationData,
                totalCollisions = totalCollisions,
                collisionsByBodyPart = collisionsByBodyPart,
                collisionsByObjectType = collisionsByObjectType,
                averageAbsoluteDeviation = averageAbsoluteDeviation,
                averageSignedDeviation = averageSignedDeviation,
                maximumDeviation = maximumDeviation,
                minimumDeviation = minimumDeviation,
                timeSpentOffRoute = timeOffRoute,
                routeCompletionPercentage = 100f,
                averageSpeed = averageSpeed
            };

            string json = JsonUtility.ToJson(session, true);
            File.WriteAllText(jsonPath, json);

            Debug.Log($"Navigation data saved: {navigationData.Count} data points to {jsonPath}");
            
            // Update session results
            UpdateSessionResults(session);
        }

        void UpdateSessionResults(NavigationSession session)
        {
            if (SessionManager.Instance == null) return;

            UserSession userSession = SessionManager.Instance.GetCurrentSession();
            
            // Store results based on trial type
            switch (currentTrialType)
            {
                case "baseline":
                    if (userSession.baselineResults == null)
                        userSession.baselineResults = new BaselineResults();
                    userSession.baselineResults.shortDistanceSession = session;
                    userSession.baselineResults.completionTime = session.duration;
                    userSession.baselineResults.completed = true;
                    break;
                    
                case "short_llm":
                    if (userSession.shortLLMResults == null)
                        userSession.shortLLMResults = new EnhancedNavigationResults();
                    userSession.shortLLMResults.navigationSession = session;
                    userSession.shortLLMResults.completed = true;
                    break;
                    
                case "short_algorithmic":
                    if (userSession.shortAlgorithmicResults == null)
                        userSession.shortAlgorithmicResults = new EnhancedNavigationResults();
                    userSession.shortAlgorithmicResults.navigationSession = session;
                    userSession.shortAlgorithmicResults.completed = true;
                    break;
                    
                case "long_llm":
                    if (userSession.longLLMResults == null)
                        userSession.longLLMResults = new EnhancedNavigationResults();
                    userSession.longLLMResults.navigationSession = session;
                    userSession.longLLMResults.completed = true;
                    break;
                    
                case "long_algorithmic":
                    if (userSession.longAlgorithmicResults == null)
                        userSession.longAlgorithmicResults = new EnhancedNavigationResults();
                    userSession.longAlgorithmicResults.navigationSession = session;
                    userSession.longAlgorithmicResults.completed = true;
                    break;
            }

            SessionManager.Instance.SaveSessionData();
        }

        // Ambient Audio Methods
        public void StartAmbientAudio()
        {
            if (ambientAudioSource == null || ambientNavigationClip == null)
            {
                Debug.LogWarning("Cannot start ambient audio - audio source or clip missing");
                return;
            }

            if (isAudioPlaying)
            {
                Debug.Log("Ambient audio already playing");
                return;
            }

            Debug.Log("Starting ambient navigation audio");
            
            // Stop any existing fade coroutine
            if (audioFadeCoroutine != null)
            {
                StopCoroutine(audioFadeCoroutine);
            }

            // Start playing and fade in
            ambientAudioSource.volume = 0f;
            ambientAudioSource.Play();
            isAudioPlaying = true;
            
            audioFadeCoroutine = StartCoroutine(FadeAudioVolume(0f, ambientVolume, audioFadeDuration));
        }

        public void StopAmbientAudio()
        {
            if (ambientAudioSource == null || !isAudioPlaying)
            {
                return;
            }

            Debug.Log("Stopping ambient navigation audio");
            
            // Stop any existing fade coroutine
            if (audioFadeCoroutine != null)
            {
                StopCoroutine(audioFadeCoroutine);
            }

            // Fade out then stop
            audioFadeCoroutine = StartCoroutine(FadeAudioVolumeAndStop(ambientAudioSource.volume, 0f, audioFadeDuration));
        }

        IEnumerator FadeAudioVolume(float startVolume, float targetVolume, float duration)
        {
            float currentTime = 0f;
            
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                float normalizedTime = currentTime / duration;
                
                ambientAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, normalizedTime);
                
                yield return null;
            }
            
            ambientAudioSource.volume = targetVolume;
            audioFadeCoroutine = null;
        }

        IEnumerator FadeAudioVolumeAndStop(float startVolume, float targetVolume, float duration)
        {
            float currentTime = 0f;
            
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                float normalizedTime = currentTime / duration;
                
                ambientAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, normalizedTime);
                
                yield return null;
            }
            
            ambientAudioSource.volume = targetVolume;
            ambientAudioSource.Stop();
            isAudioPlaying = false;
            audioFadeCoroutine = null;
        }

        // Helper methods
        string GetObjectTypeFromNameAndLayer(GameObject obj)
        {
            string classification = GetObjectTypeFromName(obj.name);
            
            if (classification == "Unknown Object")
            {
                string layerName = LayerMask.LayerToName(obj.layer);
                if (layerName == "Obstacles")
                {
                    return ClassifyObstacleByName(obj.name);
                }
            }
            
            return classification;
        }

        string ClassifyObstacleByName(string objectName)
        {
            string name = objectName.ToLower();
            name = name.Replace("(clone)", "").Replace("_prefab", "").Replace("_instance", "").Trim();
            
            if (name.Contains("car") || name.Contains("auto") || name.Contains("vehicle"))
                return "Car";
            if (name.Contains("tree") || name.Contains("plant"))
                return "Tree";
            if (name.Contains("pole") || name.Contains("post"))
                return "Pole";
            if (name.Contains("bench") || name.Contains("seat"))
                return "Bench";
            if (name.Contains("wall") || name.Contains("barrier"))
                return "Wall";
            
            return CleanObjectName(objectName);
        }

        string GetObjectTypeFromName(string objectName)
        {
            string name = objectName.ToLower();
            
            if (name.Contains("car") || name.Contains("vehicle"))
                return "Vehicle";
            else if (name.Contains("tree"))
                return "Tree";
            else if (name.Contains("building") || name.Contains("house"))
                return "Building";
            else if (name.Contains("wall") || name.Contains("barrier"))
                return "Wall";
            else if (name.Contains("pole") || name.Contains("post"))
                return "Pole";
            
            return "Unknown Object";
        }

        string CleanObjectName(string objectName)
        {
            string cleaned = objectName.Replace("(Clone)", "").Replace("_", " ").Trim();
            
            if (!string.IsNullOrEmpty(cleaned))
            {
                string[] words = cleaned.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].Length > 0)
                    {
                        words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                    }
                }
                cleaned = string.Join(" ", words);
            }
            
            return cleaned;
        }

        // Camera and movement methods
        void CameraMovement()
        {
            if (enableVR)
            {
                // Try direct XR device input for right controller (camera rotation)
                var rightDevices = new List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, rightDevices);
                
                float controllerX = 0f, controllerY = 0f;
                bool vrInputDetected = false;
                
                if (rightDevices.Count > 0)
                {
                    Vector2 primary2D;
                    if (rightDevices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out primary2D))
                    {
                        controllerX = primary2D.x;
                        controllerY = primary2D.y;
                        vrInputDetected = true;
                        
                        // Debug output to verify input
                        if (Mathf.Abs(controllerX) > 0.1f || Mathf.Abs(controllerY) > 0.1f)
                        {
                            Debug.Log($"VR Right Controller Camera: X={controllerX:F2}, Y={controllerY:F2}");
                        }
                    }
                }
                
                // Apply controller input like mouse input
                yRotation += controllerX * sensitivity * Time.deltaTime;
                xRotation -= controllerY * sensitivity * Time.deltaTime;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                // Apply rotations to the character controller (this moves the scene, not just the camera)
                transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
                
                // Apply vertical rotation to camera if available
                if (cam != null)
                    cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }
            else
            {
                // Traditional mouse look
#if ENABLE_LEGACY_INPUT_MANAGER
                float mouseX = Input.GetAxisRaw("Mouse X") * sensitivity * Time.deltaTime;
                float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivity * Time.deltaTime;

                yRotation += mouseX;
                xRotation -= mouseY;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                if (cam != null)
                    cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
#endif
            }
        }

        void MoveCharacter()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            float moveX, moveZ;
            
            if (enableVR)
            {
                // Use LEFT controller for movement, RIGHT for camera rotation
                moveX = GetSafeAxisInput("XRI_Left_Primary2DAxis_Horizontal");
                moveZ = GetSafeAxisInput("XRI_Left_Primary2DAxis_Vertical");
                
                // Fallback to keyboard if no VR input
                if (Mathf.Abs(moveX) < 0.1f && Mathf.Abs(moveZ) < 0.1f)
                {
                    moveX = Input.GetAxis("Horizontal");
                    moveZ = Input.GetAxis("Vertical");
                }
            }
            else
            {
                // Traditional keyboard input
                moveX = Input.GetAxis("Horizontal");
                moveZ = Input.GetAxis("Vertical");
            }

            Vector3 moveDirection = (transform.right * moveX) + (transform.forward * moveZ);
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1.0f);

            float finalSpeed = speed;
            
            // VR or keyboard speed boost
            if (enableVR)
            {
                // Check for VR speed button or fallback to keyboard
                if (GetSafeButtonInput("XRI_Left_GripButton") || Input.GetKey(KeyCode.LeftShift))
                {
                    finalSpeed *= 1.2f;
                }
            }
            else if (Input.GetKey(KeyCode.LeftShift))
            {
                finalSpeed *= 1.2f;
            }
            
            charController.SimpleMove(moveDirection * finalSpeed);
#endif
        }

        // Safe input methods that won't throw errors if axes don't exist
        float GetSafeAxisInput(string axisName)
        {
            try
            {
                return Input.GetAxis(axisName);
            }
            catch (System.ArgumentException)
            {
                // Axis doesn't exist, return 0
                return 0f;
            }
        }

        bool GetSafeButtonInput(string buttonName)
        {
            try
            {
                return Input.GetButton(buttonName);
            }
            catch (System.ArgumentException)
            {
                // Button doesn't exist, return false
                return false;
            }
        }

        void SetupHumanBodySimple()
        {
            humanBodyInstance = Instantiate(humanBodyPrefab, transform);
            humanBodyInstance.name = "HumanBody";
            humanBodyInstance.transform.localPosition = new Vector3(0, -1.0f, 0);
            humanBodyInstance.transform.localRotation = Quaternion.identity;
            SetupCameraOnHead();
            Debug.Log("Human body visual setup complete");
        }

        void SetupCameraOnHead()
        {
            Transform headTransform = FindHeadBone(humanBodyInstance.transform);
            
            if (headTransform != null)
            {
                GameObject cameraObj = new GameObject("Camera");
                cameraObj.transform.SetParent(headTransform);
                cameraObj.transform.localPosition = Vector3.zero;
                cameraObj.transform.localRotation = Quaternion.identity;
                
                Camera cameraComponent = cameraObj.AddComponent<Camera>();
                cameraComponent.tag = "MainCamera";
                
                cam = cameraObj.transform;
                Debug.Log($"Camera attached to head bone: {headTransform.name}");
            }
            else
            {
                GameObject cameraObj = new GameObject("Camera");
                cameraObj.transform.SetParent(humanBodyInstance.transform);
                cameraObj.transform.localPosition = new Vector3(0, cameraHeightOffset, 0);
                cameraObj.transform.localRotation = Quaternion.identity;
                
                Camera cameraComponent = cameraObj.AddComponent<Camera>();
                cameraComponent.tag = "MainCamera";
                
                cam = cameraObj.transform;
                Debug.Log($"Camera positioned at estimated head height: {cameraHeightOffset}m");
            }
        }

        Transform FindHeadBone(Transform parent)
        {
            string[] headBoneNames = { "Head", "head", "bip_Head", "mixamorig:Head", "Bip01 Head", "Armature_Head" };
            
            foreach (string boneName in headBoneNames)
            {
                Transform found = FindChildRecursive(parent, boneName);
                if (found != null)
                    return found;
            }
            
            return null;
        }

        Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;
                
            foreach (Transform child in parent)
            {
                Transform found = FindChildRecursive(child, name);
                if (found != null)
                    return found;
            }
            
            return null;
        }

        void OnDestroy()
        {
            // Stop audio when object is destroyed
            if (isAudioPlaying && ambientAudioSource != null)
            {
                ambientAudioSource.Stop();
            }
            
            // Stop unified audio system
            if (unifiedAudioController != null)
            {
                unifiedAudioController.DisableAudioSystem();
            }

            // Unsubscribe from events
            GeminiScenePreAnalyzer.OnPreAnalysisCompleted -= OnPreAnalysisCompleted;
            GeminiScenePreAnalyzer.OnPreAnalysisFailed -= OnPreAnalysisFailed;
            
            if (SessionManager.Instance != null)
            {
                SessionManager.OnTrialChanged -= OnTrialChanged;
            }
            
            if (enableTracking && navigationData.Count > 0)
            {
                SaveNavigationData();
            }
        }

        // Context menu methods for testing
        [ContextMenu("Complete Current Navigation Trial")]
        public void ManualCompleteNavigationTrial()
        {
            CompleteNavigationTrial();
        }

        [ContextMenu("Test: Start Ambient Audio")]
        public void TestStartAmbientAudio()
        {
            StartAmbientAudio();
        }

        [ContextMenu("Test: Stop Ambient Audio")]
        public void TestStopAmbientAudio()
        {
            StopAmbientAudio();
        }
        
        [ContextMenu("Test: Initialize Unified Audio")]
        public void TestInitializeUnifiedAudio()
        {
            InitializeUnifiedAudio();
        }

        [ContextMenu("Debug: Unified Audio Status")]
        public void DebugUnifiedAudioStatus()
        {
            Debug.Log("UNIFIED AUDIO STATUS:");
            Debug.Log($"Unified Audio Controller: {(unifiedAudioController != null ? "FOUND" : "MISSING")}");
            Debug.Log($"Audio Enhancements Enabled: {enableAudioEnhancements}");
            Debug.Log($"Navigation Enabled: {navigationEnabled}");
            Debug.Log($"Current Trial: {currentTrialType}");
            Debug.Log($"Is Audio Enhanced Trial: {IsAudioEnhancedTrial(currentTrialType)}");
            
            if (unifiedAudioController != null)
            {
                Debug.Log($"Audio System Active: {unifiedAudioController.IsSystemActive()}");
                Debug.Log($"Audio Mode: {unifiedAudioController.GetCurrentAudioMode()}");
                Debug.Log($"Vision Score: {unifiedAudioController.GetCentralVisionScore()}/10");
                Debug.Log($"Clarity Distance: {unifiedAudioController.GetObjectClarityDistance()}m");
            }
        }

        [ContextMenu("Debug: VR Status")]
        public void DebugVRStatus()
        {
            Debug.Log("VR SETUP STATUS:");
            Debug.Log($"VR Enabled: {enableVR}");
            Debug.Log($"VR Camera Rig: {(vrCameraRig != null ? vrCameraRig.name : "MISSING")}");
            Debug.Log($"VR Camera: {(vrCamera != null ? vrCamera.name : "MISSING")}");
            Debug.Log($"Current Camera Reference: {(cam != null ? cam.name : "MISSING")}");
            
            if (enableVR && vrCameraRig != null)
            {
                Debug.Log($"VR Rig Position: {vrCameraRig.position}");
                Debug.Log($"VR Rig Local Position: {vrCameraRig.localPosition}");
                Debug.Log($"VR Rig Parent: {(vrCameraRig.parent != null ? vrCameraRig.parent.name : "None")}");
                Debug.Log("HEAD MOVEMENT WILL NOT MOVE SCENE - Rig is parented to character");
            }
        }

        [ContextMenu("Test: Toggle VR Mode")]
        public void TestToggleVRMode()
        {
            enableVR = !enableVR;
            Debug.Log($"VR Mode toggled to: {enableVR}");
            Debug.Log("Note: You may need to restart the scene for full VR setup");
        }

        [ContextMenu("Debug: Audio Status")]
        public void DebugAudioStatus()
        {
            Debug.Log("AMBIENT AUDIO STATUS:");
            Debug.Log($"Audio Source: {(ambientAudioSource != null ? "FOUND" : "MISSING")}");
            Debug.Log($"Audio Clip: {(ambientNavigationClip != null ? ambientNavigationClip.name : "MISSING")}");
            Debug.Log($"Is Playing: {isAudioPlaying}");
            Debug.Log($"Current Volume: {(ambientAudioSource != null ? ambientAudioSource.volume : 0f)}");
            Debug.Log($"Target Volume: {ambientVolume}");
            Debug.Log($"Navigation Enabled: {navigationEnabled}");
            Debug.Log($"Current Trial: {currentTrialType}");
        }

        [ContextMenu("Debug: Show Current Trial Info")]
        public void DebugShowTrialInfo()
        {
            Debug.Log($"Current Trial: {currentTrialType}");
            Debug.Log($"Data Path: {trialDataPath}");
            Debug.Log($"Navigation Enabled: {navigationEnabled}");
            Debug.Log($"Tracking Enabled: {enableTracking}");
            Debug.Log($"Pre-Analysis Complete: {preAnalysisCompleted}");
            Debug.Log($"Data Points: {navigationData.Count}");
        }

        [ContextMenu("Debug: Pre-Analysis Status")]
        public void DebugPreAnalysisStatus()
        {
            Debug.Log($"PRE-ANALYSIS STATUS:");
            Debug.Log($"Wait for pre-analysis: {waitForPreAnalysis}");
            Debug.Log($"Pre-analysis completed: {preAnalysisCompleted}");
            Debug.Log($"Gemini analyzer found: {geminiPreAnalyzer != null}");
            if (geminiPreAnalyzer != null)
            {
                Debug.Log($"Analysis in progress: {geminiPreAnalyzer.IsAnalysisInProgress()}");
                Debug.Log($"Analysis completed: {geminiPreAnalyzer.IsAnalysisCompleted()}");
            }
            Debug.Log($"Should wait: {ShouldWaitForPreAnalysis()}");
        }
    }
}