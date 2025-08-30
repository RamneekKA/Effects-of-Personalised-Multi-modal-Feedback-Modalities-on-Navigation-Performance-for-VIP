using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Performs path finding operations.
    /// </summary>
    internal class PathFindingManager
    {
        private readonly GridData _gridData;
        private readonly PathFindingData _pedestrianPathFindingData;
        private readonly AStar _aStar;


        internal PathFindingManager(GridData gridData, PathFindingData pedestrianPathFindingData)
        {
            _gridData = gridData;
            _pedestrianPathFindingData = pedestrianPathFindingData;
            _aStar = new AStar();
        }


        /// <summary>
        /// Set a path from the current position of the pedestrian to a destination.
        /// </summary>
        internal List<int> GetPathToDestination(int pedestrianIndex, int currentWaypointIndex, Vector3 position, PedestrianTypes pedestrianType)
        {
            if (currentWaypointIndex < 0)
            {
                Debug.LogWarning($"Cannot find route to destination. Pedestrian at index {pedestrianIndex} is disabled or has an invalid target waypoint");
                return null;
            }

            int closestWaypointIndex = GetClosestPathFindingWaypoint(position, (int)pedestrianType);
            if (closestWaypointIndex < 0)
            {
                Debug.LogWarning("No waypoint found closer to destination");
                return null;
            }

            List<int> path = _aStar.FindPath(currentWaypointIndex, closestWaypointIndex, (int)pedestrianType, _pedestrianPathFindingData.AllPathFindingWaypoints);

            if (path != null)
            {
                return path;
            }

            Debug.LogWarning($"No path found for pedestrian {pedestrianIndex} to {position}");
            return null;
        }


        /// <summary>
        /// Get a path between startPosition and endPosition for a pedestrian type.
        /// </summary>
        internal List<int> GetPath(Vector3 startPosition, Vector3 endPosition, PedestrianTypes pedestrianType)
        {
            var startIndex = GetClosestPathFindingWaypoint(startPosition, (int)pedestrianType);
            if (startIndex == PedestrianSystemConstants.INVALID_WAYPOINT_INDEX)
            {
                Debug.LogWarning($"No pedestrian waypoint found close to {startPosition}");
                return null;
            }

            var endIndex = GetClosestPathFindingWaypoint(endPosition, (int)pedestrianType);
            if (endIndex == PedestrianSystemConstants.INVALID_WAYPOINT_INDEX)
            {
                Debug.LogWarning($"No pedestrian waypoint found closed to {endPosition}");
                return null;
            }

            // Stored for debug purpose.
            var path = _aStar.FindPath(startIndex, endIndex, (int)pedestrianType, _pedestrianPathFindingData.AllPathFindingWaypoints);

            if (path == null)
            {
                Debug.LogWarning($"No path found from {startPosition} to {endPosition}");
            }
            return path;
        }


        /// <summary>
        /// Get the closest waypoint from a given position.
        /// </summary>
        private int GetClosestPathFindingWaypoint(Vector3 position, int type)
        {
            List<int> possibleWaypoints = _gridData.GetPedestrianWaypointsAroundPosition(position);

            if (possibleWaypoints.Count == 0)
            {
                return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
            }

            float distance = float.MaxValue;
            int waypointIndex = PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
            foreach (int waypoint in possibleWaypoints)
            {
                if (_pedestrianPathFindingData.GetAllowedAgents(waypoint).Contains(type))
                {
                    float newDistance = Vector3.SqrMagnitude(_pedestrianPathFindingData.AllPathFindingWaypoints[waypoint].WorldPosition - position);
                    if (newDistance < distance)
                    {
                        distance = newDistance;
                        waypointIndex = waypoint;
                    }
                }
            }
            return waypointIndex;
        }
    }
}
