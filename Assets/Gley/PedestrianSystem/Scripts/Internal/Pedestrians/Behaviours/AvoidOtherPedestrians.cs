using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Avoid collisions with other pedestrians.
    /// </summary>
    public class AvoidOtherPedestrians : PedestrianBehaviour
    {
        private Transform _pedestrianTransform;
        private int _addedWaypoint;
        private int _oldIndex;


        /// <summary>
        /// Constructor
        /// Add event listeners
        /// </summary>
        public AvoidOtherPedestrians()
        {
            Events.OnPedestrianTriggerEnter += SomethingInTrigger;
            Events.OnPedestrianTriggerExit += NothingInTrigger;
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
            _pedestrianTransform = characterAnimator.transform;
            _oldIndex = -1;
        }


        /// <summary>
        /// Reset the old index every time this behaviour becomes inactive.
        /// </summary>
        protected override void OnBecameInactive()
        {
            base.OnBecameInactive();
            _oldIndex = -1;
        }


        /// <summary>
        /// Executes on update when the behaviour is active.
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to.</param>
        /// <param name="moveSpeed">The walking speed.</param>
        internal override void Execute(float turnAngle, float moveSpeed)
        {
            // Increase the angle to force avoidance.
            AnimatorMethods.FreeWalk(turnAngle * 3, moveSpeed);
        }


        /// <summary>
        /// Listener triggered when a pedestrian left the trigger.
        /// </summary>
        private void NothingInTrigger(int pedestrianIndex, Pedestrian other)
        {
            if (PedestrianIndex == pedestrianIndex)
            {
                Stop();
            }
        }


        /// <summary>
        ///  Listener triggered when another pedestrian is inside the trigger.
        /// </summary>
        private void SomethingInTrigger(int pedestrianIndex, Pedestrian other)
        {
            // Trigger is for the current pedestrian.
            if (pedestrianIndex == PedestrianIndex)
            {
                // If pedestrian is currently not in a more important behaviour.
                if (API.GetCurrentBehaviour(PedestrianIndex).Priority > Priority)
                {
                    // If other pedestrian is not moving in the same direction.
                    if (Vector3.Angle(_pedestrianTransform.forward, other.transform.forward) > 60)
                    {
                        // If current waypoint is a stop waypoint do not avoid the pedestrian sitting there.
                        if (!API.IsCurrentWaypointAStopWaypoint(PedestrianIndex))
                        {
                            // If the pedestrian did not reached the previous assigned waypoint.
                            if (!API.GetWaypointList(PedestrianIndex).Contains(_addedWaypoint))
                            {
                                // If it is not already running.
                                if (CanRun() == false)
                                {
                                    Start();

                                    int randomSign = Random.Range(0, 2) == 0 ? -1 : 1;

                                    // Get a random perpendicular direction on the current moving direction.
                                    Vector3 randomPerpendicularDirection = Quaternion.Euler(0, randomSign * 90, 0) * (API.GetCurrentWaypointPosition(PedestrianIndex) - _pedestrianTransform.position).normalized;

                                    // Add a point located at 1 m to the left or to the right of the other pedestrian.
                                    Vector3 avoidancePoint = other.transform.position + randomPerpendicularDirection;

                                    // Check if the trajectory to the newly added point is not intersecting the other pedestrian trajectory.
                                    if (LineSegmentsIntersect(_pedestrianTransform.transform.position, avoidancePoint, other.transform.position, API.GetCurrentWaypointPosition(other.PedestrianIndex)))
                                    {
                                        // Move the point on the other side.
                                        avoidancePoint = other.transform.position - randomPerpendicularDirection;
                                    }

                                    // Assign the new point as a destination for current pedestrian.
                                    _addedWaypoint = API.InsertPositionAsTarget(PedestrianIndex, 0, avoidancePoint);
                                    Events.TriggerDestinationChangedEvent(PedestrianIndex, API.GetCurrentWaypointPosition(PedestrianIndex));
                                }
                            }
                        }
                    }
                    else
                    {
                        // If they move in the same direction.
                        if (API.GetCurrentBehaviour(PedestrianIndex).Priority > Priority)
                        {
                            // Is not the same pedestrian.
                            if (_oldIndex != other.PedestrianIndex)
                            {
                                // if the front pedestrian speed is smaller than the current pedestrian speed.
                                if (other.WalkSpeed < API.GetPedestrian(PedestrianIndex).WalkSpeed)
                                {
                                    // Change the offset to not collide with the front pedestrian.
                                    if (other.Offset < 0)
                                    {
                                        API.SetPedestrianOffset(PedestrianIndex, other.Offset + 0.5f);
                                    }
                                    else
                                    {
                                        API.SetPedestrianOffset(PedestrianIndex, other.Offset - 0.5f);
                                    }
                                    // Trigger a destination change event to update the offset for the current pedestrian.
                                    Events.TriggerDestinationChangedEvent(PedestrianIndex, API.GetCurrentWaypointPosition(PedestrianIndex));
                                    _oldIndex = other.PedestrianIndex;
                                }
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Detects if 2 segments intersect each other.
        /// </summary>
        /// <returns>true if the 2 segments intersect</returns>
        private bool LineSegmentsIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            float denominator = (p4.z - p3.z) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.z - p1.z);
            float numerator1 = (p4.x - p3.x) * (p1.z - p3.z) - (p4.z - p3.z) * (p1.x - p3.x);
            float numerator2 = (p2.x - p1.x) * (p1.z - p3.z) - (p2.z - p1.z) * (p1.x - p3.x);

            // Detect coincident lines (parallel and overlapping)
            if (Mathf.Approximately(numerator1, 0) && Mathf.Approximately(numerator2, 0) && Mathf.Approximately(denominator, 0))
            {
                float dotProduct = Vector3.Dot((p2 - p1).normalized, (p3 - p1).normalized);
                if (dotProduct < 0)
                    return true;

                dotProduct = Vector3.Dot((p1 - p2).normalized, (p4 - p2).normalized);
                if (dotProduct < 0)
                    return true;
            }

            // Check if the line segments intersect.
            if (!Mathf.Approximately(denominator, 0) && (numerator1 / denominator >= 0 && numerator1 / denominator <= 1) && (numerator2 / denominator >= 0 && numerator2 / denominator <= 1))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Remove event listeners.
        /// </summary>
        internal override void OnDestroy()
        {
            Events.OnPedestrianTriggerEnter -= SomethingInTrigger;
            Events.OnPedestrianTriggerExit -= NothingInTrigger;
        }
    }
}