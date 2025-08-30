using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.PedestrianSystem
{
    /// <summary>
    /// Avoid static obstacles by adding additional points inside the path.
    /// </summary>
    public class AvoidStaticObstacles : PedestrianBehaviour
    {
        private List<int> _addedWaypoints;
        private Transform _pedestrianTransform;
        private Collider _oldObstacle;


        /// <summary>
        /// Constructor
        /// Listen for events
        /// </summary>
        public AvoidStaticObstacles()
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
            // Cache the pedestrian transform for performance improvements
            _pedestrianTransform = characterAnimator.transform;
        }


        /// <summary>
        /// Executes on update when the behaviour is active
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to.</param>
        /// <param name="moveSpeed">The walking speed.</param>
        internal override void Execute(float turnAngle, float moveSpeed)
        {
            AnimatorMethods.FreeWalk(turnAngle, moveSpeed);
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
            // Run only for static obstacles.
            if (pedestrianIndex == PedestrianIndex && obstacleType == ObstacleTypes.StaticObject)
            {
                if (CanRun() == false)
                {
                    Start();

                    // If there is a new collider, or the previous avoidance points have already been passed. 
                    if (_oldObstacle != collider || !API.GetWaypointList(PedestrianIndex).Any(cond => _addedWaypoints.Contains(cond)))
                    {
                        float sign = Mathf.Sign(Vector3.SignedAngle(_pedestrianTransform.forward, collider.transform.position - _pedestrianTransform.position, Vector3.up));
                        // Add 2 points at one of the edges of the bounding box of the obstacle.
                        AddAvoidanceWaypoints(collider, sign);
                    }
                }
            }
        }


        private void AddAvoidanceWaypoints(Collider collider, float sign)
        {
            // Store current obstacle.
            _oldObstacle = collider;

            Bounds bounds = collider.bounds;

            // Get the corner points of the bounds.
            var corners = new Vector3[4];
            corners[0] = new Vector3(bounds.extents.x + bounds.center.x, _pedestrianTransform.position.y, bounds.extents.z + bounds.center.z); // Top-Front-Right
            corners[1] = new Vector3(bounds.extents.x + bounds.center.x, _pedestrianTransform.position.y, -bounds.extents.z + bounds.center.z); // Top-Front-Left
            corners[2] = new Vector3(-bounds.extents.x + bounds.center.x, _pedestrianTransform.position.y, -bounds.extents.z + bounds.center.z); // Top-Back-Left
            corners[3] = new Vector3(-bounds.extents.x + bounds.center.x, _pedestrianTransform.position.y, bounds.extents.z + bounds.center.z); // Top-Back-Right

            // Get points that are located in the moving direction to avoid sharp turns.
            var sideCorners = GetCornersInMovingDirection(corners, sign, API.GetCurrentWaypointPosition(PedestrianIndex), bounds.center);
            _addedWaypoints = new List<int>();

            for (int i = 0; i < sideCorners.Count; i++)
            {

                //sideCorners[i] += (sideCorners[i] - new Vector3(bounds.center.x, _pedestrianTransform.position.y, bounds.center.z)).normalized * 1f;
                // Add the new waypoints on the pedestrian path.
                _addedWaypoints.Add(API.InsertPositionAsTarget(PedestrianIndex, 0, sideCorners[i]));
            }



            if (_addedWaypoints.Count > 0)
            {
                // Trigger a waypoint request event so pedestrian will walk to the newly created waypoints.
                Events.TriggerDestinationChangedEvent(PedestrianIndex, API.GetCurrentWaypointPosition(PedestrianIndex));
            }
        }


        private List<Vector3> GetCornersInMovingDirection(Vector3[] corners, float sign, Vector3 currentWaypoint, Vector3 center)
        {
            List<Vector3> result = new List<Vector3>();

            for (int i = 0; i < corners.Length; i++)
            {
                if (result.Count < 2)
                {
                    // Translate the corners outwards to allow the pedestrian to fit between corner and obstacle.
                    corners[i] += (corners[i] - new Vector3(center.x, _pedestrianTransform.position.y, center.z)).normalized * 1.2f;

                    var angle = Vector3.SignedAngle(currentWaypoint - _pedestrianTransform.position, corners[i] - _pedestrianTransform.position, Vector3.up);
                    // Discard corners that are behind.
                    if (Mathf.Abs(angle) < 90)
                    {
                        // Get corners in direction.
                        if (Mathf.Sign(angle) != sign)
                        {
                            result.Add(corners[i]);
                        }
                    }
                }
            }

            // Sort them by the distance from the pedestrian.
            result.Sort(new DistanceComparer(_pedestrianTransform.position));

            return result;
        }


        /// <summary>
        /// Comparer class for distance.
        /// </summary>
        private class DistanceComparer : IComparer<Vector3>
        {
            private Vector3 referencePosition;

            public DistanceComparer(Vector3 referencePosition)
            {
                this.referencePosition = referencePosition;
            }

            // Compare method implementation to compare Vector3 points based on their distance from the reference position
            public int Compare(Vector3 a, Vector3 b)
            {
                float distanceA = Vector3.Distance(referencePosition, a);
                float distanceB = Vector3.Distance(referencePosition, b);

                // Compare distances and return the appropriate result for sorting
                if (distanceA < distanceB)
                    return 1;
                else if (distanceA > distanceB)
                    return -1;
                else
                    return 0;
            }
        }


        /// <summary>
        /// Remove events listener.
        /// </summary>
        internal override void OnDestroy()
        {
            Events.OnObjectTriggerStay -= SomethingInTrigger;
            Events.OnObjectTriggerExit -= NothingInTrigger;
        }
    }
}
