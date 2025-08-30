using System.Collections.Generic;
using UnityEngine;

namespace Gley.PedestrianSystem
{
    public class DefaultPedestrianDelegates
    {
        /// <summary>
        /// The default behavior, a random square is chosen from the available ones. 
        /// </summary>
        /// <returns>A free waypoint index.</returns>
        public static int GetRandomSpawnWaypoint(List<Vector2Int> neighbors, Vector3 position, Vector3 direction, PedestrianTypes vehicleType, bool useWaypointPriority)
        {
            Vector2Int selectedNeighbor = neighbors[Random.Range(0, neighbors.Count)];

            // Get a random waypoint that supports the current pedestrian.
            return GetPossibleWaypoint(selectedNeighbor, vehicleType, useWaypointPriority);

        }


        /// <summary>
        /// The square in front of the player is chosen
        /// </summary>
        /// <returns>A free waypoint index.</returns>
        public static int GetForwardSpawnWaypoint(List<Vector2Int> neighbors, Vector3 position, Vector3 direction, PedestrianTypes vehicleType, bool useWaypointPriority)
        {
            Vector2Int selectedNeighbor = Vector2Int.zero;
            float angle = 180;
            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector3 cellDirection = API.GetGridDataHandler().GetCellPosition(neighbors[i]) - position;
                float newAngle = Vector3.Angle(cellDirection, direction);
                if (newAngle < angle)
                {
                    selectedNeighbor = neighbors[i];
                    angle = newAngle;
                }
            }

            // Get a random waypoint that supports the current pedestrian.
            return GetPossibleWaypoint(selectedNeighbor, vehicleType, useWaypointPriority);
        }


        private static int GetPossibleWaypoint(Vector2Int selectedNeighbor, PedestrianTypes vehicleType, bool usePriority)
        {
            List<UrbanSystem.Internal.SpawnWaypoint> possibleWaypoints = API.GetGridDataHandler().GetPedestrianSpawnWaypointsForCell(selectedNeighbor, (int)vehicleType);
            if (possibleWaypoints.Count > 0)
            {
                if (usePriority)
                {
                    int totalPriority = 0;
                    foreach (UrbanSystem.Internal.SpawnWaypoint waypoint in possibleWaypoints)
                    {
                        totalPriority += waypoint.Priority;
                    }
                   
                    int randomPriority = Random.Range(1, totalPriority);
                    totalPriority = 0;
                    for (int i = 0; i < possibleWaypoints.Count; i++)
                    {
                        totalPriority += possibleWaypoints[i].Priority;
                        if (totalPriority >= randomPriority)
                        {
                            return possibleWaypoints[i].WaypointIndex;
                        }
                    }
                }
                else
                {
                    return possibleWaypoints[Random.Range(0, possibleWaypoints.Count)].WaypointIndex;
                }
            }
            return Internal.PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
        }
    }
}