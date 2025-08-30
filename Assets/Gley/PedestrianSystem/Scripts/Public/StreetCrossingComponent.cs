using Gley.PedestrianSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PedestrianWaypointsDataHandler = Gley.PedestrianSystem.Internal.PedestrianWaypointsDataHandler;
using PedestrianWaypointSettings = Gley.PedestrianSystem.Internal.PedestrianWaypointSettings;


namespace Gley.PedestrianSystem
{
    /// <summary>
    /// Control when pedestrians can cross a road using this component.
    /// </summary>
    public class StreetCrossingComponent : MonoBehaviour, UrbanSystem.Internal.IIntersection
    {
        [SerializeField][HideInInspector] private List<PedestrianWaypointSettings> _stopWaypoints = new List<PedestrianWaypointSettings>();
        [SerializeField][HideInInspector] private List<PedestrianWaypointSettings> _directionWaypoints = new List<PedestrianWaypointSettings>();
        [SerializeField][HideInInspector] private int[] _intersectionWaypoints;

        [SerializeField] private string _crossingName;

        private PedestrianWaypointsDataHandler _pedestrianWaypointsDataHandler;

        public string CrossingName => _crossingName;


        /// <summary>
        /// Initialize crossing.
        /// </summary>
        internal void Initialize(PedestrianWaypointsDataHandler pedestrianWaypointsDataHandler)
        {
            _pedestrianWaypointsDataHandler = pedestrianWaypointsDataHandler;
            _pedestrianWaypointsDataHandler.SetIntersection(_intersectionWaypoints, this);
            SetStopWaypointsState(true);
        }


        /// <summary>
        /// Convert from Editor waypoints to play mode waypoints.
        /// </summary>
        /// <param name="allWaypoints"></param>
        public void ConvertWaypoints(PedestrianWaypointSettings[] allWaypoints)
        {
            _intersectionWaypoints = new int[_stopWaypoints.Count];
            for (int i = 0; i < _stopWaypoints.Count; i++)
            {
                _intersectionWaypoints[i] = _stopWaypoints[i].ToListIndex(allWaypoints);
            }
        }

        public void SetStopWaypoints(List<PedestrianWaypointSettings> stopWaypoints)
        {
            _stopWaypoints = stopWaypoints;
        }

        public void SetDirectionWaypoints(List<PedestrianWaypointSettings> directionWaypoints)
        {
            _directionWaypoints = directionWaypoints;
        }


        /// <summary>
        /// Change the stop state of the crossing using the available API.
        /// </summary>
        /// <param name="stop"></param>
        public void SetStopWaypointsState(bool stop)
        {
            for (int i = 0; i < _intersectionWaypoints.Length; i++)
            {
                Events.TriggerStopStateChangedEvent(_intersectionWaypoints[i], stop);
            }
        }


        /// <summary>
        /// Get the current stop state of the crossing.
        /// </summary>
        /// <returns>True -> pedestrians are not allowed to pass.</returns>
        public bool GetStopState()
        {
            if (_intersectionWaypoints.Length > 0)
            {
                return _pedestrianWaypointsDataHandler.IsStop(_intersectionWaypoints[0]);
            }
            return false;
        }

        internal int[] GetIntersectionWaypoints()
        {
            return _intersectionWaypoints;
        }

        /// <summary>
        /// Check and remove missing waypoints assigned.
        /// </summary>
        /// <returns></returns>
        public bool VerifyAssignments()
        {
            for (int i = _directionWaypoints.Count - 1; i >= 0; i--)
            {
                if (_directionWaypoints[i] == null)
                {
                    _directionWaypoints.RemoveAt(i);
                }
                else
                {
                    if (!_directionWaypoints[i].neighbors.Intersect(_stopWaypoints).Any() && !_directionWaypoints[i].prev.Intersect(_stopWaypoints).Any())
                    {
                        _directionWaypoints.RemoveAt(i);
                    }
                }
            }


            for (int i = _stopWaypoints.Count - 1; i >= 0; i--)
            {
                if (_stopWaypoints[i] == null)
                {
                    _stopWaypoints.RemoveAt(i);
                }
                else
                {
                    if (!_stopWaypoints[i].neighbors.Intersect(_directionWaypoints).Any() && !_stopWaypoints[i].prev.Intersect(_directionWaypoints).Any())
                    {
                        Debug.LogError($"Pedestrian waypoint {_stopWaypoints[i].name} from intersection {name} has no direction assigned", gameObject);
                        return false;
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Return all stop waypoints.
        /// </summary>
        /// <returns></returns>
        public List<PedestrianWaypointSettings> GetPedestrianWaypoints()
        {
            return _stopWaypoints;
        }


        /// <summary>
        /// Return all direction waypoints.
        /// </summary>
        /// <returns></returns>
        public List<PedestrianWaypointSettings> GetDirectionWaypoints()
        {
            return _directionWaypoints;
        }


        // Interface implementation, not required for this type of crossing.
        public bool IsPathFree(int waypointIndex)
        {
            return true;
        }


        public void VehicleLeft(int vehicleIndex)
        {

        }


        public void VehicleEnter(int vehicleIndex)
        {

        }


        public void PedestrianPassed(int agentIndex)
        {

        }
    }
}
