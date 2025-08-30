using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.PedestrianSystem
{
    /// <summary>
    /// Script that goes on every pedestrian from the system.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public class Pedestrian : MonoBehaviour
    {
        private Dictionary<string, PedestrianBehaviour> _possibleBehaviours;
        private Collider[] _allColliders;
        private List<Collider> _obstacles;
        private PedestrianRagDoll _ragDollComponent;
        private LayerMask _buildingLayers;
        private LayerMask _obstacleLayers;
        private LayerMask _playerLayers;
        private float _originalSpeed;

        [SerializeField][HideInInspector] private UrbanSystem.Internal.VisibilityScript _visibilityScript;
        [SerializeField][HideInInspector] private GameObject _ragDollPrefab;
        [SerializeField][HideInInspector] private bool _hasRagdoll;

        [SerializeField] private float _minWalkSpeed = 0.4f;
        [SerializeField] private float _maxWalkSpeed = 0.6f;
        [SerializeField] private PedestrianTypes _type;
#if UNITY_EDITOR
#pragma warning disable CS0414
        [SerializeField] private float _triggerLength = 2;
#pragma warning restore CS0414
#endif

        public PedestrianTypes Type => _type;
        public PedestrianBehaviour CurrentBehaviour { get; private set; }
        public float WalkSpeed { get; private set; }
        public float Offset { get; internal set; }
        public int PedestrianIndex { get; private set; }
        public bool IsActive { get; private set; }
        public bool RagDollIsActive { get; private set; }
        public bool Excluded { get; internal set; }


        /// <summary>
        /// Initializes class members.
        /// </summary>
        public virtual Pedestrian Initialize(LayerMask buildingLayers, LayerMask obstacleLayers, LayerMask playerLayers, int listIndex, bool excluded, List<PedestrianBehaviour> pedestrianBehaviours)
        {
            PedestrianIndex = listIndex;
            name += PedestrianIndex;
            Excluded = excluded;
            _buildingLayers = buildingLayers;
            _obstacleLayers = obstacleLayers;
            _playerLayers = playerLayers;
            _allColliders = GetComponentsInChildren<Collider>();
            if (_hasRagdoll)
            {
                _ragDollPrefab = Instantiate(_ragDollPrefab, transform.parent);
                _ragDollComponent = _ragDollPrefab.AddComponent<PedestrianRagDoll>().Initialize();
                _allColliders = _allColliders.Concat(_ragDollComponent.GetColliders()).ToArray();
            }
            SetPedestrianBehaviours(pedestrianBehaviours);
            return this;
        }


        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!other.isTrigger)
            {
                _obstacles.Add(other);
                bool pedestrianHit = other.gameObject.layer == gameObject.layer;
                if (pedestrianHit)
                {
                    if (other.attachedRigidbody.TryGetComponent<Pedestrian>(out var otherComponent))
                    {
                        //sees a pedestrian
                        Events.TriggerPedestrianTriggerEnterEvent(PedestrianIndex, otherComponent);
                    }
                    else
                    {
                        Debug.LogWarning(gameObject.name + " is on the wrong layer. Only pedestrians should be on this layer", gameObject);
#if UNITY_6000_0_OR_NEWER
                        if (other.attachedRigidbody.linearVelocity.sqrMagnitude > 0)
#else
                        if (other.attachedRigidbody.velocity.sqrMagnitude > 0)
#endif
                        {
                            Events.TriggerObjectTriggerEnterEvent(PedestrianIndex, other, ObstacleTypes.DynamicObject);
                        }
                        else
                        {
                            Events.TriggerObjectTriggerEnterEvent(PedestrianIndex, other, ObstacleTypes.StaticObject);
                        }
                    }
                }
                else
                {
                    //sees an obstacle
                    if (_buildingLayers == (_buildingLayers | (1 << other.gameObject.layer)))
                    {
                        Events.TriggerObjectTriggerEnterEvent(PedestrianIndex, other, ObstacleTypes.StaticObject);
                    }
                    else
                    {
                        if (_obstacleLayers == (_obstacleLayers | (1 << other.gameObject.layer)))
                        {
                            Events.TriggerObjectTriggerEnterEvent(PedestrianIndex, other, ObstacleTypes.DynamicObject);
                        }
                        else
                        {
                            if (_playerLayers == (_playerLayers | (1 << other.gameObject.layer)))
                            {
                                Events.TriggerObjectTriggerEnterEvent(PedestrianIndex, other, ObstacleTypes.Player);
                            }
                        }
                    }
                }
            }
        }


        protected virtual void OnTriggerExit(Collider other)
        {
            if (!other.isTrigger)
            {
                _obstacles.Remove(other);
                bool pedestrianHit = other.gameObject.layer == gameObject.layer;
                if (pedestrianHit)
                {
                    if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<Pedestrian>(out var otherComponent))
                    {
                        //sees a pedestrian
                        Events.TriggerPedestrianTriggerExitEvent(PedestrianIndex, otherComponent);
                    }
                    else
                    {
                        Events.TriggerObjectTriggerExitEvent(PedestrianIndex);
                    }
                }
                else
                {
                    //sees an obstacle
                    if (_buildingLayers == (_buildingLayers | (1 << other.gameObject.layer)) ||
                        _obstacleLayers == (_obstacleLayers | (1 << other.gameObject.layer)) ||
                        _playerLayers == (_playerLayers | (1 << other.gameObject.layer)))
                    {
                        Events.TriggerObjectTriggerExitEvent(PedestrianIndex);
                    }
                }
            }
        }


        protected virtual void OnTriggerStay(Collider other)
        {
            if (!other.isTrigger)
            {
                bool pedestrianHit = other.gameObject.layer == gameObject.layer;
                if (pedestrianHit)
                {
                    Pedestrian otherComponent = other.attachedRigidbody.GetComponent<Pedestrian>();
                    if (otherComponent != null)
                    {
                        //sees a pedestrian
                        Events.TriggerPedestrianTriggerStayEvent(PedestrianIndex, otherComponent, other);
                    }
                    else
                    {
                        Debug.LogWarning(gameObject.name + " is on the wrong layer. Only pedestrians should be on this layer", gameObject);
#if UNITY_6000_0_OR_NEWER
                        if (other.attachedRigidbody.linearVelocity.sqrMagnitude > 0)
#else
                        if (other.attachedRigidbody.velocity.sqrMagnitude > 0)
#endif
                        {
                            Events.TriggerObjectTriggerStayEvent(PedestrianIndex, other, ObstacleTypes.DynamicObject);
                        }
                        else
                        {
                            Events.TriggerObjectTriggerStayEvent(PedestrianIndex, other, ObstacleTypes.StaticObject);
                        }
                    }
                }
                else
                {
                    //sees an obstacle
                    if (_buildingLayers == (_buildingLayers | (1 << other.gameObject.layer)))
                    {
                        Events.TriggerObjectTriggerStayEvent(PedestrianIndex, other, ObstacleTypes.StaticObject);
                    }
                    else
                    {
                        if (_obstacleLayers == (_obstacleLayers | (1 << other.gameObject.layer)))
                        {
#if UNITY_6000_0_OR_NEWER
                            if (other.attachedRigidbody != null && other.attachedRigidbody.linearVelocity.sqrMagnitude > 1)
#else
                            if (other.attachedRigidbody != null && other.attachedRigidbody.velocity.sqrMagnitude > 1)
#endif
                            {
                                Events.TriggerObjectTriggerStayEvent(PedestrianIndex, other, ObstacleTypes.DynamicObject);
                            }
                            else
                            {
                                Events.TriggerObjectTriggerStayEvent(PedestrianIndex, other, ObstacleTypes.StaticObject);
                            }
                        }
                        else
                        {
                            if (_playerLayers == (_playerLayers | (1 << other.gameObject.layer)))
                            {
                                Events.TriggerObjectTriggerStayEvent(PedestrianIndex, other, ObstacleTypes.Player);
                            }
                        }
                    }
                }
            }
        }


        protected virtual void OnCollisionEnter(UnityEngine.Collision collision)
        {
            bool pedestrianHit = collision.collider.gameObject.layer == gameObject.layer;
            if (pedestrianHit)
            {
                Events.TriggerCollisionEnterEvent(PedestrianIndex, collision, ObstacleTypes.Pedestrian);
            }
            else
            {
                if (_buildingLayers == (_buildingLayers | (1 << collision.collider.gameObject.layer)))
                {
                    Events.TriggerCollisionEnterEvent(PedestrianIndex, collision, ObstacleTypes.StaticObject);
                }
                else
                {
                    if (_obstacleLayers == (_obstacleLayers | (1 << collision.collider.gameObject.layer)))
                    {
                        Events.TriggerCollisionEnterEvent(PedestrianIndex, collision, ObstacleTypes.DynamicObject);
                    }
                    else
                    {
                        if (_playerLayers == (_playerLayers | (1 << collision.collider.gameObject.layer)))
                        {
                            Events.TriggerCollisionEnterEvent(PedestrianIndex, collision, ObstacleTypes.Player);
                        }
                    }
                }
            }

        }


        /// <summary>
        /// Deactivates a pedestrian. 
        /// </summary>
        public virtual void DeactivatePedestrian()
        {
            IsActive = false;

            TriggerColliderRemoved();

            RagDollIsActive = false;
            CurrentBehaviour = null;
            foreach (var behaviour in _possibleBehaviours)
            {
                behaviour.Value.ResetBehaviour();
            }
            _obstacles = new List<Collider>();

            if (_hasRagdoll)
            {
                _ragDollPrefab.SetActive(false);
                _ragDollComponent.ResetRagDoll();
            }

            _visibilityScript.Reset();
            gameObject.SetActive(false);
        }





        /// <summary>
        /// Activate a pedestrian.
        /// </summary>
        public virtual void ActivatePedestrian(Vector3 position, Quaternion rotation)
        {
            Offset = Random.Range(-1f, 1f);
            WalkSpeed = _originalSpeed = Random.Range(_minWalkSpeed, _maxWalkSpeed);
            gameObject.transform.SetPositionAndRotation(position + new Vector3(0, 0.1f, 0), rotation);
            SelectBehaviour();
            gameObject.SetActive(true);
            IsActive = true;
        }


        /// <summary>
        /// Instantiate associated ragdoll.
        /// </summary>
        public virtual void InstantiateRagDoll()
        {
            if (_hasRagdoll)
            {
                _ragDollPrefab.transform.position = gameObject.transform.position;
                _ragDollPrefab.transform.rotation = gameObject.transform.rotation;
                _ragDollPrefab.SetActive(true);

                gameObject.SetActive(false);
                RagDollIsActive = true;
                TriggerColliderRemoved();
                Events.TriggerPedestrianRemovedEvent(PedestrianIndex);
            }
            else
            {
                Debug.LogWarning($"RagDoll not found on pedestrian {gameObject.name}", gameObject);
            }
        }


        /// <summary>
        /// Return the ragdoll position in case pedestrian is not available.
        /// </summary>
        /// <returns></returns>
        internal Vector3 GetRagDollPosition()
        {
            return _ragDollComponent.GetPosition();
        }


        /// <summary>
        /// Get the rigidbody component
        /// </summary>
        /// <returns></returns>
        internal Rigidbody GetRigidBody()
        {
            return GetComponent<Rigidbody>();
        }


        /// <summary>
        /// If the pedestrian is not in view, notify that is can be removed.
        /// </summary>
        /// <returns></returns>
        internal bool CanBeRemoved()
        {
            return _visibilityScript.IsNotInView();
        }


        /// <summary>
        /// Reset walk speed for a limited amount of time.
        /// </summary>
        internal void SloWDown(float newSpeed, float timeToSlowDown)
        {
            WalkSpeed = newSpeed;
            Invoke(nameof(ResetWalkSpeed), timeToSlowDown);
        }


        /// <summary>
        /// Reset walk speed to original speed.
        /// </summary>
        internal void ResetWalkSpeed()
        {
            WalkSpeed = _originalSpeed;
        }


        /// <summary>
        /// Trigger OnTriggerExit when a collider was destroyed so it can be removed from obstacle list. 
        /// </summary>
        internal void ColliderRemoved(Collider[] colliders)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (_obstacles.Any(cond => cond == colliders[i]))
                {
                    OnTriggerExit(colliders[i]);
                }
            }
        }


        #region Behaviours
        /// <summary>
        /// Set the complete behaviour list for this pedestrian.
        /// </summary>
        internal void SetPedestrianBehaviours(List<PedestrianBehaviour> pedestrianBehaviours)
        {
            if (pedestrianBehaviours == null || pedestrianBehaviours.Count <= 0)
            {
                Debug.LogError("Behaviours list has no elements");
                return;
            }

            //remove old behaviours
            CurrentBehaviour = null;
            if (_possibleBehaviours != null)
            {
                foreach (var behavoiur in _possibleBehaviours)
                {
                    if (behavoiur.Value != null)
                    {
                        behavoiur.Value.OnDestroy();
                    }
                }
            }

            Animator animator = transform.GetComponent<Animator>();


            _possibleBehaviours = new Dictionary<string, PedestrianBehaviour>();
            for (int i = 0; i < pedestrianBehaviours.Count; i++)
            {
                if (_possibleBehaviours.TryAdd(pedestrianBehaviours[i].Name, pedestrianBehaviours[i]))
                {
                    pedestrianBehaviours[i].Initialize(animator, PedestrianIndex, i);
                }
            }
        }


        /// <summary>
        /// Start a specific behaviour for this pedestrian.
        /// </summary>
        internal void StartBehavior(string behaviourName)
        {
            GetBehaviour(behaviourName)?.Start();
        }


        /// <summary>
        /// Execute current behaviour. This method is called every frame on update.
        /// </summary>
        internal void ExecuteBehaviour(float angle)
        {
            SelectBehaviour();
            CurrentBehaviour?.Execute(angle, WalkSpeed);
        }


        /// <summary>
        /// Stop the specified behaviour.
        /// </summary>
        internal void StopBehaviour(string behaviourName)
        {
            GetBehaviour(behaviourName)?.Stop();
        }


        /// <summary>
        /// Get the script that controls the specified behaviour name. 
        /// </summary>
        internal PedestrianBehaviour GetBehaviour(string behaviourName)
        {

            if (_possibleBehaviours.TryGetValue(behaviourName, out var behaviour))
            {
                return behaviour;
            }

            Debug.LogWarning($"Behaviour {behaviourName} not found on pedestrian index {PedestrianIndex}");
            return null;
        }


        /// <summary>
        /// Select the behaviour with the highest priority to be the active one.
        /// </summary>
        private void SelectBehaviour()
        {
            foreach (var behaviour in _possibleBehaviours)
            {
                if (behaviour.Value.IsRunning() && behaviour.Value.CanRun())
                {
                    // This is the current active behaviour -> do nothing.
                    break;
                }

                // A behavior can run.
                if (behaviour.Value.CanRun())
                {
                    // Deactivate current behavior.
                    if (CurrentBehaviour != null)
                    {
                        CurrentBehaviour.DeactivateBehaviour();
                    }
                    // Set current behaviour as new behaviour.
                    CurrentBehaviour = behaviour.Value;
                    // Activate current behaviour.
                    CurrentBehaviour.ActivateBehaviour();
                    Events.TriggerBehaviourChangedEvent(PedestrianIndex, CurrentBehaviour);
                    break;
                }
            }
        }
        #endregion


        private void TriggerColliderRemoved()
        {
            // Force trigger exit method for the pedestrian colliders
            if (API.IsInitialized())
            {
                API.TriggerColliderRemovedEvent(_allColliders);
            }
#if GLEY_TRAFFIC_SYSTEM
            // Force trigger exit method for the pedestrian colliders in case of Traffic System usage.
            if (TrafficSystem.API.IsInitialized())
            {
                TrafficSystem.API.TriggerColliderRemovedEvent(_allColliders);
            }
#endif
        }


        protected virtual void OnDestroy()
        {
            if (_possibleBehaviours != null)
            {
                foreach (var behaviour in _possibleBehaviours)
                {
                    if (_possibleBehaviours.Values != null)
                    {
                        behaviour.Value.OnDestroy();
                    }
                }
            }
        }
    }
}
