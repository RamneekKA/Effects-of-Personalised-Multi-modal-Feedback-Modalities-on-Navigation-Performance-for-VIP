using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace Gley.PedestrianSystem.Internal
{
    internal class PedestrianAI
    {
        private Pedestrian[] _activePedestrians;
        private PedestrianWaypointManager _waypointManager;
        private PedestrianWaypointsDataHandler _pedestrianWaypointsDataHandler;
        private AllPedestrians _allPedestrians;
        private int _defaultPathLength;


        /// <summary>
        /// Initializes class members
        /// </summary>
        internal PedestrianAI (PedestrianWaypointManager waypointManager, PedestrianWaypointsDataHandler pedestrianWaypointsDataHandler, AllPedestrians allPedestrians, int defaultPathLength)
        {
            _waypointManager = waypointManager;
            _allPedestrians = allPedestrians;
            _defaultPathLength = defaultPathLength;
            _pedestrianWaypointsDataHandler = pedestrianWaypointsDataHandler;
            _activePedestrians = allPedestrians.GetAllPedestrians().ToArray();
        }


        /// <summary>
        /// Request a new waypoint for a pedestrian.
        /// </summary>
        internal void RequestWaypoint(int pedestrianIndex, bool forceSkip)
        {
            // If pedestrian is marked as cannot pass, do nothing. Used for red light.
            if (_waypointManager.CannotPass(pedestrianIndex))
            {
                return;
            }

            // If has no points in path, do nothing.           
            if (_waypointManager.HasCustomPath(pedestrianIndex) && _waypointManager.GetPathLength(pedestrianIndex) == 0)
            {
                return;
            }

            int currentWaypointIndex = PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
            if (_waypointManager.GetPathLength(pedestrianIndex) > 0)
            {
                currentWaypointIndex = _waypointManager.GetCurrentWaypointIndexFromPath(pedestrianIndex);
            }

            // If it is not just a position waypoint.
            if (!_waypointManager.IsJustPosition(currentWaypointIndex))
            {
                PedestrianWaypoint currentWaypoint = null;
                // Get current waypoint
                if (currentWaypointIndex >= 0)
                {
                    currentWaypoint = _pedestrianWaypointsDataHandler.GetWaypointWithValidation(currentWaypointIndex);
                }
                if (currentWaypoint == null)
                {
                    // If pedestrian has no waypoint -> had a path before.
                    // Add as current waypoint the previous waypoint.
                    _waypointManager.AddWaypointToPath(pedestrianIndex, _waypointManager.GetPreviousWaypointIndexFromPath(pedestrianIndex));
                    currentWaypoint = _pedestrianWaypointsDataHandler.GetWaypointWithValidation(_waypointManager.GetCurrentWaypointIndexFromPath(pedestrianIndex));
                }

                if (!_waypointManager.HasCustomPath(pedestrianIndex))
                {
                    // Add waypoints until path is full.
                    if (_waypointManager.GetPathLength(pedestrianIndex) <= _defaultPathLength)
                    {
                        var waypointIndex = GetNewWaypointIndex(_waypointManager.GetLastWaypointIndexFromPath(pedestrianIndex), pedestrianIndex, _allPedestrians.GetPedestrianType(pedestrianIndex));
                        do
                        {
                            if (waypointIndex != PedestrianSystemConstants.INVALID_WAYPOINT_INDEX)
                            {
                                _waypointManager.AddWaypointToPath(pedestrianIndex, waypointIndex);
                                waypointIndex = GetNewWaypointIndex(_waypointManager.GetLastWaypointIndexFromPath(pedestrianIndex), pedestrianIndex, _allPedestrians.GetPedestrianType(pedestrianIndex));
                            }
                        } while (_waypointManager.GetPathLength(pedestrianIndex) <= _defaultPathLength && waypointIndex != PedestrianSystemConstants.INVALID_WAYPOINT_INDEX);
                    }
                }

                if (_waypointManager.GetPathLength(pedestrianIndex) > 1)
                {
                    if (!_waypointManager.IsJustPosition(_waypointManager.GetNextWaypointIndexFromPath(pedestrianIndex)))
                    {
                        // Get next waypoint.
                        var nextWaypointIndex = _waypointManager.GetNextWaypointIndexFromPath(pedestrianIndex);

                        // If current waypoint is inside an intersection, and next waypoint is on crossing direction -> trigger crossing event.
                        if (currentWaypoint.AssociatedIntersection != null && nextWaypointIndex != PedestrianSystemConstants.INVALID_WAYPOINT_INDEX)
                        {
                            if (_pedestrianWaypointsDataHandler.IsCrossing(nextWaypointIndex))
                            {
                                // Pedestrian wants to cross associated intersection, used to trigger the event only once.
                                if (_waypointManager.IsCrossing(pedestrianIndex) == false)
                                {
                                    Events.TriggerStreetCrossingEvent(pedestrianIndex, currentWaypoint.AssociatedIntersection, _waypointManager.GetCurrentWaypointIndexFromPath(pedestrianIndex));
                                    _waypointManager.SetCrossingProperty(pedestrianIndex, true);
                                    if (!forceSkip)
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            _waypointManager.ChangeWaypoint(pedestrianIndex);

            // Condition to skip waypoint
            if (_waypointManager.GetPathLength(pedestrianIndex) > 1 && _waypointManager.GetCurrentWaypointIndexFromPath(pedestrianIndex) >= 0)
            {
                // If angle between (current-prev) and (next - current) > 90 -> skip waypoint.
                if (Vector3.Angle(_waypointManager.GetCurrentWaypointPositionFromPath(pedestrianIndex) - _waypointManager.GetPreviousWaypointPositionFromPath(pedestrianIndex),
                    _waypointManager.GetNextWaypointPositionFromPath(pedestrianIndex) - _waypointManager.GetCurrentWaypointPositionFromPath(pedestrianIndex)) > 90)
                {
                    // If it is a stop waypoint and it is red, don't skip it.
                    if ((_waypointManager.GetCurrentWaypointStopState(pedestrianIndex) != true ||
                        _waypointManager.GetNextWaypointCrossingState(pedestrianIndex) != true) &&
                        _waypointManager.GetPreviousWaypointIndexFromPath(pedestrianIndex) != _waypointManager.GetNextWaypointIndexFromPath(pedestrianIndex))
                    {
                        RequestWaypoint(pedestrianIndex, true);
                    }
                }
            }
        }


        /// <summary>
        /// Get a waypoint that is adjacent to the current one.
        /// </summary>
        internal int GetNewWaypointIndex(int currentWaypointIndex, int pedestrianIndex, PedestrianTypes pedestrianType)
        {
            List<int> possibleWaypoints = _pedestrianWaypointsDataHandler.GetPossibleWaypoints(currentWaypointIndex, pedestrianType);

            if (possibleWaypoints.Count < 0)
            {
                return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
            }

            if (possibleWaypoints.Count > 1)
            {
                if (_waypointManager.GetPathLength(pedestrianIndex) > 1)
                {
                    // Remove the waypoint from which the pedestrian came from.
                    var oldWaypointIndex = _waypointManager.GetSecondToLastWaypointIndex(pedestrianIndex);
                    if (oldWaypointIndex >= 0 && oldWaypointIndex != PedestrianSystemConstants.INVALID_WAYPOINT_INDEX)
                    {
                        possibleWaypoints.Remove(oldWaypointIndex);
                    }
                }
            }

            return possibleWaypoints[Random.Range(0, possibleWaypoints.Count)];
        }


        /// <summary>
        /// Updates and executes the pedestrian behaviours.
        /// </summary>
        internal void UpdatePedestrians(NativeArray<float> turnAngle)
        {
            for (int i = 0; i < _activePedestrians.Length; i++)
            {
                _activePedestrians[i].ExecuteBehaviour(turnAngle[_activePedestrians[i].PedestrianIndex]);
            }
        }
    }
}
