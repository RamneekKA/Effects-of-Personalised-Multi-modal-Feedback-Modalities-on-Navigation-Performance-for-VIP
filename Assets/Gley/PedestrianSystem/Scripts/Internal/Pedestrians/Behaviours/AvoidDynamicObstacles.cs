using System.Collections.Generic;
using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Avoid moving objects.
    /// </summary>
    public class AvoidDynamicObstacles : PedestrianBehaviour
    {
        private Transform _pedestrianTransform;
        private Collider _oldObstacle;
        private int _addedWaypoint;


        /// <summary>
        /// Constructor
        /// Add event listeners
        /// </summary>
        public AvoidDynamicObstacles()
        {
            Events.OnObjectTriggerStay += SomethingInTrigger;
            Events.OnObjectTriggerExit += NothingInTrigger;
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
        /// Executes on update when the behaviour is active.
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to.</param>
        /// <param name="moveSpeed">The walking speed.</param>
        internal override void Execute(float turnAngle, float moveSpeed)
        {
            AnimatorMethods.FreeWalk(turnAngle, moveSpeed);
            // Avoid obstacle.
            SetAvoidingPoint(_oldObstacle);
        }


        /// <summary>
        /// Listener triggered when an object left the trigger.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        private void NothingInTrigger(int pedestrianIndex)
        {
            if (PedestrianIndex == pedestrianIndex)
            {
                Stop();
            }
        }


        /// <summary>
        /// Listener triggered when something is inside the trigger.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian that triggered the event.</param>
        /// <param name="collider">The collider that triggered the event.</param>
        /// <param name="obstacleType">The type of obstacle.</param>

        private void SomethingInTrigger(int pedestrianIndex, Collider collider, ObstacleTypes obstacleType)
        {
            if (pedestrianIndex == PedestrianIndex)
            {
                // Run only for dynamic obstacles.
                if (obstacleType == ObstacleTypes.DynamicObject || obstacleType == ObstacleTypes.Player)
                {
                    if (CanRun() == false)
                    {
                        Start();
                    }
                    // Store the old obstacle.
                    _oldObstacle = collider;
                }
            }
        }


        /// <summary>
        /// Set a point inside path to avoid the obstacle.
        /// </summary>
        private void SetAvoidingPoint(Collider collider)
        {
            // Remove previously added point.
            if (API.GetWaypointList(PedestrianIndex).Contains(_addedWaypoint))
            {
                API.RemoveWaypointFromPath(PedestrianIndex, _addedWaypoint);
            }

            // Get the bounds corners of the obstacle.
            Bounds bounds = collider.bounds;
            var corners = new Vector3[4];
            corners[0] = new Vector3(bounds.extents.x + bounds.center.x, bounds.center.y, bounds.extents.z + bounds.center.z); // Top-Front-Right
            corners[1] = new Vector3(bounds.extents.x + bounds.center.x, bounds.center.y, -bounds.extents.z + bounds.center.z); // Top-Front-Left
            corners[2] = new Vector3(-bounds.extents.x + bounds.center.x, bounds.center.y, -bounds.extents.z + bounds.center.z); // Top-Back-Left
            corners[3] = new Vector3(-bounds.extents.x + bounds.center.x, bounds.center.y, bounds.extents.z + bounds.center.z); // Top-Back-Right
            List<int> availablePoints = new List<int>();
            
            // Set the linecast origin.
            Vector3 position = new Vector3(_pedestrianTransform.position.x, bounds.center.y, _pedestrianTransform.position.z);
            
            for (int i = 0; i < corners.Length; i++)
            {
                // Spread the points outwards to have enough room to pass. 
                corners[i] += (corners[i] - bounds.center).normalized * 1f;
                if (!Physics.Linecast(position, corners[i], 1 << collider.gameObject.layer))
                {
                    // If a direct way to the point does not pass through the obstacle, it is a valid point.
                    availablePoints.Add(i);
                }
            }

            if(availablePoints.Count==0)
            {
                return;
            }    

            // Get the closest point in the moving direction to avoid sharp turns.
            int selectedPoint = 0;
            float minDistance = Mathf.Infinity;
            for (int i = 0; i < availablePoints.Count; i++)
            {
                var newDistance = (position - corners[availablePoints[i]]).sqrMagnitude;
                if (newDistance < minDistance)
                {
                    var angle = Vector3.Angle(_pedestrianTransform.forward, corners[availablePoints[i]] - _pedestrianTransform.position);
                    if (angle < 90)
                    {
                        minDistance = newDistance;
                        selectedPoint = i;
                    }
                }
            }

            // Insert the avoidance point in path and trigger a change event.
            _addedWaypoint = API.InsertPositionAsTarget(PedestrianIndex, 0, new Vector3(corners[availablePoints[selectedPoint]].x, _pedestrianTransform.position.y, corners[availablePoints[selectedPoint]].z));
            Events.TriggerDestinationChangedEvent(PedestrianIndex, API.GetCurrentWaypointPosition(PedestrianIndex));
        }


        /// <summary>
        /// Remove event listeners.
        /// </summary>
        internal override void OnDestroy()
        {
            Events.OnObjectTriggerStay -= SomethingInTrigger;
            Events.OnObjectTriggerExit -= NothingInTrigger;
        }
    }
}
