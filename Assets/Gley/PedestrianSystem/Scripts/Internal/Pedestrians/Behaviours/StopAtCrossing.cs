using Gley.UrbanSystem.Internal;
using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Makes a pedestrian stop when a crossing is reached.
    /// </summary>
    public class StopAtCrossing : PedestrianBehaviour
    {
        private Transform _pedestrianTransform;
        private IIntersection _intersection;
        private Vector3 _destinationWaypointPosition;


        /// <summary>
        /// Constructor
        /// Listen for street crossing event.
        /// </summary>
        public StopAtCrossing()
        {
            Events.OnStreetCrossing += PedestrianIsAtCrossing;
        }


        /// <summary>
        /// Internally called by the system to initialize the behaviour.
        /// </summary>
        /// <param name="characterAnimator">The animator from the character.</param>
        /// <param name="pedestrianIndex">The index of the pedestrian associated with the behaviour.</param>
        /// <param name="priority">The priority of the behaviour.</param>
        internal override void Initialize(Animator characterAnimator, int pedestrianIndex, int priority)
        {
            base.Initialize(characterAnimator, pedestrianIndex, priority);

            // Cache the pedestrian transform for performance improvements.
            _pedestrianTransform = characterAnimator.transform;
        }


        /// <summary>
        /// Listen for stop state change when behaviour becomes active.
        /// </summary>
        protected override void OnBecomeActive()
        {
            Events.OnStopStateChanged += StopStateChanged;
            base.OnBecomeActive();
        }


        /// <summary>
        /// Executes on update when the behaviour is active.
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to.</param>
        /// <param name="moveSpeed">The walking speed.</param>
        internal override void Execute(float turnAngle, float walkSpeed)
        {
            // Stop movement and look at the next waypoint.
            AnimatorMethods.LookAtTarget(_pedestrianTransform.forward, _destinationWaypointPosition - _pedestrianTransform.position);
            AnimatorMethods.StopMoving();
        }


        /// <summary>
        /// Remove stop listener when behaviour becomes inactive.
        /// </summary>
        protected override void OnBecameInactive()
        {
            Events.OnStopStateChanged -= StopStateChanged;
            base.OnBecameInactive();
        }


        /// <summary>
        /// Event listener triggered when a pedestrian is at a crossing.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian at the crossing.</param>
        /// <param name="intersection">The crossing interface.</param>
        /// <param name="waypointIndex">The current target waypoint of the pedestrian.</param>
        private void PedestrianIsAtCrossing(int pedestrianIndex, IIntersection intersection, int waypointIndex)
        {
            // If it is not this pedestrian, do nothing.
            if (PedestrianIndex != pedestrianIndex)
            {
                return;
            }
            // Convert index to waypoint.
            var waypoint = API.GetWaypointFromIndex(waypointIndex);
            // Store the intersection locally.
            _intersection = intersection;

            if (waypoint.Stop)
            {
                // If waypoint has it's stop property true -> pedestrian is not allowed to cross, start this behaviour.
                Start();
                // Mark pedestrian as cannot pass.
                API.DoNotChangeWaypoint(pedestrianIndex, true);
                // Store the destination, pedestrian will turn to that point while waiting.
                _destinationWaypointPosition = API.GetNextWaypointPosition(pedestrianIndex);
            }
            else
            {
                // If stop property is false, pedestrian can continue -> stop this behaviour.
                Stop();
                // Start the crossing behaviour.
                API.StartBehavior<Crossing>(PedestrianIndex);
                // Set intersection for the crossing behaviour.
                ((Crossing)API.GetPedestrianBehaviourOfType<Crossing>(PedestrianIndex)).SetIntersection(intersection, waypointIndex);
            }
        }


        /// <summary>
        /// Event listener triggered when a stop state changes.
        /// </summary>
        private void StopStateChanged(int waypointIndex, bool stop)
        {
            // If pedestrian is at this waypoint.
            if (API.IsPedestrianAtThisWaypoint(PedestrianIndex, waypointIndex))
            {
                // Pedestrian is allowed to cross.
                if (stop == false)
                {
                    // Set cannot pass property to false, to allow the pedestrian to move.
                    API.DoNotChangeWaypoint(PedestrianIndex, false);
                    // Stop current behaviour.
                    Stop();
                    // Start crossing behaviour.
                    API.StartBehavior<Crossing>(PedestrianIndex);
                    ((Crossing)API.GetPedestrianBehaviourOfType<Crossing>(PedestrianIndex)).SetIntersection(_intersection, waypointIndex);
                }
            }
        }


        /// <summary>
        /// Remove the events.
        /// </summary>
        internal override void OnDestroy()
        {
            Events.OnStreetCrossing -= PedestrianIsAtCrossing;
            Events.OnStopStateChanged -= StopStateChanged;
        }
    }
}
