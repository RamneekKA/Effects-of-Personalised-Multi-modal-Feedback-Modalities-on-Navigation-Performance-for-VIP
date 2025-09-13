using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace FCG
{
    /// <summary>
    /// Updated Character Controller with VR Input Support
    /// VR controllers replace mouse/WASD - head tracking is passive viewing only
    /// Left stick: Movement (WASD equivalent)
    /// Right stick: Body turning (Mouse X equivalent) 
    /// Head tracking: Looking around only (does NOT move the scene)
    /// </summary>
    public class CharacterControl : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float speed = 10.0f;
        public float sensitivity = 100f;

        [Header("VR Settings")]
        [Tooltip("Separate XR Origin object (NOT a child of this player)")]
        public Transform xrOrigin;
        [Tooltip("Turning speed with right joystick (degrees per second)")]
        public float turnSpeed = 45f;
        [Tooltip("Dead zone for joystick input")]
        [Range(0.01f, 0.5f)]
        public float joystickDeadZone = 0.1f;

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

        // Movement and camera - VR MODIFIED
        private float yRotation = 0f; // Only controlled by joystick now, not head
        private Transform cam; // Original camera - will be DISABLED for VR
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
            // VR: Don't lock cursor - not needed in VR
            Cursor.lockState = CursorLockMode.None;

            charController = GetComponent<CharacterController>();
            
            // VR Setup: Find XR Origin and disable original camera
            SetupVRSystem();
            
            // Setup human body if enabled
            if (useHumanBodyCollision && humanBodyPrefab != null)
            {
                SetupHumanBodySimple();
            }
            // NOTE: No else clause for camera setup - VR camera is handled by XR Origin

            bodyZoneDetector = gameObject.AddComponent<SimpleBodyZoneDetector>();
            
            // Initialize player body rotation (NOT camera rotation in VR)
            yRotation = transform.eulerAngles.y;
            
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

            Debug.Log("Character Controller with VR input system initialized");
        }

        void SetupVRSystem()
        {
            // Find XR Origin if not assigned
            if (xrOrigin == null)
            {
                GameObject xrOriginGO = GameObject.Find("XR Origin (VR)");
                if (xrOriginGO != null)
                {
                    xrOrigin = xrOriginGO.transform;
                    Debug.Log("Found XR Origin automatically");
                }
                else
                {
                    Debug.LogError("XR Origin (VR) not found! Please assign it manually.");
                }
            }

            // Position XR Origin at player position initially
            if (xrOrigin != null)
            {
                xrOrigin.position = transform.position;
                xrOrigin.rotation = transform.rotation;
                Debug.Log("XR Origin synchronized with player position");
            }

            // Find and DISABLE the original camera to prevent conflicts
            cam = transform.Find("Camera");
            if (cam == null)
            {
                // Look for camera in children
                Camera[] childCameras = GetComponentsInChildren<Camera>();
                if (childCameras.Length > 0)
                {
                    cam = childCameras[0].transform;
                }
            }

            // Disable original camera - XR Origin handles the camera now
            if (cam != null)
            {
                Camera originalCamera = cam.GetComponent<Camera>();
                if (originalCamera != null)
                {
                    originalCamera.enabled = false;
                    Debug.Log("Original camera disabled - VR camera will handle display");
                }
            }

            Debug.Log("VR System setup complete");
        }

        void Update()
        {
            // Only allow movement and tracking if pre-analysis is done and navigation is enabled
            if (navigationEnabled && (preAnalysisCompleted || !waitForPreAnalysis || !SessionManager.Instance.IsNavigationTrial(currentTrialType)))
            {
                // VR Input - replaces mouse/WASD
                HandleVRTurning();    // Right stick = Mouse X (body turning)
                HandleVRMovement();   // Left stick = WASD (movement)

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
                    // Also reset XR Origin
                    if (xrOrigin != null)
                    {
                        xrOrigin.position = initialPosition;
                    }
                }
            }
        }

        /// <summary>
        /// Handle VR controller turning (replaces Mouse X input)
        /// Right stick horizontal axis turns the player body
        /// </summary>
        void HandleVRTurning()
        {
            Vector2 rightStick = Vector2.zero;
            InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            
            if (rightDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightStick))
            {
                // Only use horizontal axis for turning, ignore vertical
                float turnInput = rightStick.x;
                
                if (Mathf.Abs(turnInput) > joystickDeadZone)
                {
                    // Apply sensitivity and turning speed
                    float turnAmount = turnInput * turnSpeed * Time.deltaTime;
                    yRotation += turnAmount;
                    
                    // Rotate the player body
                    transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
                    
                    // Keep XR Origin synchronized with player body rotation
                    if (xrOrigin != null)
                    {
                        xrOrigin.rotation = transform.rotation;
                    }
                }
            }
        }

        /// <summary>
        /// Handle VR controller movement (replaces WASD input)
        /// Left stick controls forward/back/strafe movement
        /// </summary>
        void HandleVRMovement()
        {
            Vector2 leftStick = Vector2.zero;
            InputDevice leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            
            if (leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftStick))
            {
                // Apply dead zone
                if (leftStick.magnitude > joystickDeadZone)
                {
                    // Use PLAYER transform for movement direction (not head/camera)
                    // This ensures head movement doesn't affect movement direction
                    Vector3 moveDirection = (transform.right * leftStick.x) + (transform.forward * leftStick.y);
                    moveDirection = Vector3.ClampMagnitude(moveDirection, 1.0f);

                    // Check for sprint with left trigger (replaces Left Shift)
                    bool sprintPressed = false;
                    leftDevice.TryGetFeatureValue(CommonUsages.triggerButton, out sprintPressed);
                    
                    float finalSpeed = sprintPressed ? speed * 1.2f : speed;
                    
                    // Move the player with CharacterController
                    charController.SimpleMove(moveDirection * finalSpeed);
                    
                    // Keep XR Origin position synchronized
                    if (xrOrigin != null)
                    {
                        xrOrigin.position = transform.position;
                    }
                }
            }
        }

        // REST OF THE SCRIPT UNCHANGED FROM ORIGINAL
        // All the session management, audio, collision detection etc. remains the same

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
            
            Debug.Log("Running in standalone mode with VR input system");
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
                
                Debug.Log($"VR Navigation enabled for trial: {currentTrialType}");
                Debug.Log("Use left stick to move, right stick to turn, left trigger to sprint!");
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
            Debug.Log("Scene analysis in progress - VR navigation will begin when complete");
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
            
            Debug.Log($"VR Navigation tracking initialized for {currentTrialType}");
            Debug.Log($"Data path: {trialDataPath}");
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
            
            // VR: Screenshots will be from the VR camera automatically
            screenshotCounter++;
            string filename = $"vr_screenshot_{screenshotCounter:D4}_{Time.time:F1}s.png";
            string fullPath = Path.Combine(trialDataPath, "Screenshots", filename);

            ScreenCapture.CaptureScreenshot(fullPath);
            
            if (navigationData.Count > 0)
            {
                navigationData[navigationData.Count - 1].screenshotPath = filename;
            }

            Debug.Log($"VR Screenshot saved: {filename}");
        }

        // Enhanced collision detection (unchanged)
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
            
            Debug.Log($"VR COLLISION: {bodyPart} hit {objectType} ({objectName}) at {collisionSpeed:F2}m/s");

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
            string filename = $"vr_collision_{screenshotCounter:D4}_{Time.time:F1}s.png";
            string fullPath = Path.Combine(trialDataPath, "Screenshots", filename);
            
            ScreenCapture.CaptureScreenshot(fullPath);
            
            if (navigationData.Count > 0)
            {
                navigationData[navigationData.Count - 1].screenshotPath = filename;
            }
            
            Debug.Log($"VR Collision screenshot saved: {filename}");
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
            
            Debug.Log($"VR Navigation trial '{currentTrialType}' completed");
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
                sessionID = $"VR_{currentTrialType}_{System.DateTime.Now:yyyyMMdd_HHmmss}",
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

            Debug.Log($"VR Navigation data saved: {navigationData.Count} data points to {jsonPath}");
            
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

        // Ambient Audio Methods (unchanged)
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

        // Helper methods (unchanged)
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

        // Camera and movement methods - VR VERSIONS ONLY
        void SetupHumanBodySimple()
        {
            humanBodyInstance = Instantiate(humanBodyPrefab, transform);
            humanBodyInstance.name = "HumanBody";
            humanBodyInstance.transform.localPosition = new Vector3(0, -1.0f, 0);
            humanBodyInstance.transform.localRotation = Quaternion.identity;
            
            // NO camera setup - XR Origin handles the camera
            Debug.Log("Human body visual setup complete (VR mode - no camera attachment)");
        }

        // Context menu methods for testing
        [ContextMenu("Test: VR Controller Input")]
        public void TestVRInput()
        {
            Debug.Log("=== VR CONTROLLER INPUT TEST ===");
            
            // Test left controller (movement)
            InputDevice leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            Vector2 leftStick;
            bool leftTrigger;
            bool leftValid = leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftStick);
            leftDevice.TryGetFeatureValue(CommonUsages.triggerButton, out leftTrigger);
            
            Debug.Log($"Left Controller: Valid={leftValid}, Stick={leftStick}, Trigger={leftTrigger}");
            
            // Test right controller (turning)
            InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            Vector2 rightStick;
            bool rightValid = rightDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightStick);
            
            Debug.Log($"Right Controller: Valid={rightValid}, Stick={rightStick}");
            
            // Test XR Origin sync
            if (xrOrigin != null)
            {
                Debug.Log($"XR Origin Position: {xrOrigin.position}");
                Debug.Log($"Player Position: {transform.position}");
                Debug.Log($"Position Synced: {Vector3.Distance(xrOrigin.position, transform.position) < 0.1f}");
            }
            else
            {
                Debug.LogError("XR Origin not assigned!");
            }
        }

        [ContextMenu("Manual: Complete Current VR Navigation Trial")]
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

        [ContextMenu("Debug: VR System Status")]
        public void DebugVRSystemStatus()
        {
            Debug.Log("VR SYSTEM STATUS:");
            Debug.Log($"XR Origin Assigned: {(xrOrigin != null ? "YES" : "NO")}");
            if (xrOrigin != null)
            {
                Debug.Log($"XR Origin Position: {xrOrigin.position}");
                Debug.Log($"XR Origin Rotation: {xrOrigin.rotation.eulerAngles}");
            }
            
            Debug.Log($"Player Position: {transform.position}");
            Debug.Log($"Player Rotation: {transform.rotation.eulerAngles}");
            Debug.Log($"Original Camera: {(cam != null ? "FOUND" : "MISSING")}");
            
            if (cam != null)
            {
                Camera originalCam = cam.GetComponent<Camera>();
                Debug.Log($"Original Camera Enabled: {(originalCam != null ? originalCam.enabled.ToString() : "NO CAMERA COMPONENT")}");
            }
            
            Debug.Log($"Navigation Enabled: {navigationEnabled}");
            Debug.Log($"Current Trial: {currentTrialType}");
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
    }
}