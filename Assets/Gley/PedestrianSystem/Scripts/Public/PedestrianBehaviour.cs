using UnityEngine;

namespace Gley.PedestrianSystem
{
    /// <summary>
    /// Starting class for any custom pedestrian behaviour.
    /// </summary>
    public abstract class PedestrianBehaviour
    { 
        public PedestrianBehaviour()
        {
            Name = GetType().Name;
        }

        /// <summary>
        /// The name of the behaviour
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The priority of the behaviour. Higher is more important
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        /// The index of the pedestrian executing this behaviour
        /// </summary>
        protected int PedestrianIndex { get; private set; }  
        
        /// <summary>
        /// The animator of the pedestrian executing this behaviour
        /// </summary>
        protected Animator CharacterAnimator { get; private set; }

        /// <summary>
        /// An instance of the class that controls the Move Blend Tree
        /// </summary>
        protected AnimatorMethods AnimatorMethods { get; private set; }

        private bool _behaviourCanRun;
        private bool _behaviourIsActive;
        private bool _debugBehaviours = false;

        /// <summary>
        /// Executes on update when the behaviour is active
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to</param>
        /// <param name="moveSpeed">The walking speed</param>
        internal abstract void Execute(float turnAngle, float moveSpeed);

        /// <summary>
        /// Executes when behaviour is destroyed
        /// </summary>
        internal abstract void OnDestroy();


        /// <summary>
        /// Internally called by the system to initialize the behaviour
        /// </summary>
        /// <param name="characterAnimator">The animator from the character</param>
        /// <param name="pedestrianIndex">The index of the pedestrian associated with the behaviour</param>
        /// <param name="priority">The priority of the behaviour</param>
        internal virtual void Initialize(Animator characterAnimator, int pedestrianIndex, int priority)
        {
            CharacterAnimator = characterAnimator;
            PedestrianIndex = pedestrianIndex;
            Priority = priority;
            AnimatorMethods = new AnimatorMethods(CharacterAnimator);
        }


        /// <summary>
        /// Start the behaviour.
        /// The behaviour will be executed only if no behavior with a higher priority is active
        /// </summary>
        public void Start()
        {
            if (_debugBehaviours)
            {
                Debug.Log($"Can run {Name} for {PedestrianIndex}");
            }
            _behaviourCanRun = true;
        }


        /// <summary>
        /// Stop behaviour from executing
        /// </summary>
        public void Stop()
        {
            if (_debugBehaviours)
            {
                Debug.Log($"Can't run {Name} for {PedestrianIndex}");
            }
            _behaviourCanRun = false;
        }


        /// <summary>
        /// Check if current behaviour can run
        /// </summary>
        /// <returns>True if can run</returns>
        internal bool CanRun()
        {
            return _behaviourCanRun;
        }


        /// <summary>
        /// Internally called to activate the behaviour. 
        /// The active behaviour is currently running on a pedestrian
        /// </summary>
        internal void ActivateBehaviour()
        {
            if (_behaviourCanRun)
            {
                _behaviourIsActive = true;
                OnBecomeActive();
            }
        }


        /// <summary>
        /// Check if current behaviour is running on this pedestrian
        /// </summary>
        /// <returns>True if running</returns>
        internal bool IsRunning()
        {
            return _behaviourIsActive;
        }


        /// <summary>
        /// Stop running this behaviour on current pedestrian
        /// </summary>
        internal void DeactivateBehaviour()
        {
            _behaviourIsActive = false;
            OnBecameInactive();
        }


        /// <summary>
        /// Called when a pedestrian is disabled to reset everything to initial state
        /// </summary>
        internal void ResetBehaviour()
        {
            Stop();
            DeactivateBehaviour();
            OnReset();
        }


        /// <summary>
        /// Override it on behaviour for performing reset operations
        /// </summary>
        protected virtual void OnReset()
        {
            if (_debugBehaviours)
            {
                Debug.Log($"{Name} is reset {PedestrianIndex}");
            }
        }


        /// <summary>
        /// Override it on behaviour to perform operations when behaviour becomes active
        /// </summary>
        protected virtual void OnBecomeActive()
        {
            if (_debugBehaviours)
            {
                Debug.Log($"{Name} became active for {PedestrianIndex}");
            }
        }


        /// <summary>
        /// Override it on behaviour to perform operations when behaviour becomes inactive
        /// </summary>
        protected virtual void OnBecameInactive()
        {
            if (_debugBehaviours)
            {
                Debug.Log($"{Name} became inactive for {PedestrianIndex}");
            }
        }
    }
}
