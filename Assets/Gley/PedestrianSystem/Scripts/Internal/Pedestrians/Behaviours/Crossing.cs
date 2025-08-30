using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Active when a pedestrian is crossing a road.
    /// </summary>
    public class Crossing : PedestrianBehaviour
    {
        private List<int> _waypointList;
        private IIntersection _intersection;
        private int _currentWaypoint;
        private bool _run;


        /// <summary>
        /// Add event listeners every time this behaviour becomes active.
        /// </summary>
        protected override void OnBecomeActive()
        {
            Events.OnStopStateChanged += StopStateChanged;
            Events.OnDestinationChanged += WaypointChanged;
            base.OnBecomeActive();
        }


        /// <summary>
        /// Executes on update when the behaviour is active.
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to.</param>
        /// <param name="moveSpeed">The walking speed.</param>
        internal override void Execute(float turnAngle, float moveSpeed)
        {
            if (!_run)
            {
                AnimatorMethods.FreeWalk(turnAngle, moveSpeed);
            }
            else
            {
                //if it is running move faster
                AnimatorMethods.FreeWalk(turnAngle, moveSpeed + 0.3f);
            }
        }


        /// <summary>
        /// Reset values when behaviour becomes inactive.
        /// </summary>
        protected override void OnBecameInactive()
        {
            Events.OnStopStateChanged -= StopStateChanged;
            Events.OnDestinationChanged -= WaypointChanged;
            _run = false;
            _waypointList = null;
            base.OnBecameInactive();
        }


        /// <summary>
        /// Set the current intersection.
        /// </summary>
        /// <param name="crossingIntersection">The interface representing the intersection where the pedestrian is now.</param>
        /// <param name="waypointIndex">The waypoint index where the pedestrian is now.</param>
        internal void SetIntersection(IIntersection crossingIntersection, int waypointIndex)
        {
            _intersection = crossingIntersection;
            _currentWaypoint = waypointIndex;
        }


        /// <summary>
        /// Listener triggered every time a pedestrian changes waypoint.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian that changes the waypoint.</param>
        /// <param name="newPosition">The target position.</param>
        private void WaypointChanged(int pedestrianIndex, Vector3 newPosition)
        {
            // If it is this pedestrian.
            if (PedestrianIndex == pedestrianIndex)
            {
                // Get all target waypoints for the current pedestrian.
                _waypointList = API.GetWaypointList(PedestrianIndex);
                if (!_run)
                {
                    // Loop through all waypoints, and if one of the is true, make it run.
                    foreach (var waypointIndex in _waypointList)
                    {
                        if (waypointIndex >= 0)
                        {
                            var waypoint = API.GetWaypointFromIndex(waypointIndex);
                            if (waypoint != null)
                            {
                                if (waypoint.Stop == true)
                                {
                                    _run = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                // If previous waypoint is in the current intersection ->
                // current pedestrian has left the intersection -> stop this behaviour.
                if (API.GetPreviousWaypointIndex(PedestrianIndex) >= 0 &&
                    API.GetPreviousWaypointIndex(PedestrianIndex) != _currentWaypoint
                    && API.GetWaypointFromIndex(API.GetPreviousWaypointIndex(PedestrianIndex)).AssociatedIntersection == _intersection)
                {
                    Stop();
                }
            }
        }

      
        /// <summary>
        /// Listener triggered when a waypoint changes its stop state.
        /// </summary>
        /// <param name="waypointIndex">The index of the waypoint that changed the state.</param>
        /// <param name="stop">The state of the stop parameter.</param>
        private void StopStateChanged(int waypointIndex, bool stop)
        {
            // Loop through all waypoints, and if one of the is true, make it run if it is not running already.
            if (stop && !_run)
            {
                if (_waypointList != null)
                {
                    if (_waypointList.Contains(waypointIndex))
                    {
                        if (API.GetWaypointFromIndex(waypointIndex).Stop)
                        {
                            _run = true;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Executes when behaviour is destroyed.
        /// </summary>
        internal override void OnDestroy()
        {

        }
    }
}
