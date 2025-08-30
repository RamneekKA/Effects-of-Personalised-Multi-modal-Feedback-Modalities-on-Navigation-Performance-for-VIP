using Gley.UrbanSystem.Internal;
using UnityEditor;
using UnityEngine;


namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Display informations about the pedestrian system inside the scene.
    /// </summary>
    internal class DebugManager
    {
#if UNITY_EDITOR
        private PedestrianDebugSettings _debugSettings;
        private AllPedestrians _allPedestrians;
        private PedestrianWaypointManager _waypointManager;
        private PathFindingData _pathFindingData;
        private PedestrianWaypointsDataHandler _waypointsDataHandler;
        private WaypointActivator _waypointActivator;
        private AllCrossings _allCrossings;
        private GridData _gridData;


        internal DebugManager(PedestrianDebugSettings debugSettings, AllPedestrians allPedestrians, PedestrianWaypointManager waypointManager, PedestrianWaypointsDataHandler waypointsDataHandler, WaypointActivator waypointActivator, AllCrossings allCrossings, GridData gridData, PathFindingData pathFindingData)
        {
            _debugSettings = debugSettings;
            _allPedestrians = allPedestrians;
            _waypointManager = waypointManager;
            _waypointsDataHandler = waypointsDataHandler;
            _waypointActivator = waypointActivator;
            _allCrossings = allCrossings;
            _gridData = gridData;
            _pathFindingData = pathFindingData;
        }


        /// <summary>
        /// Debug.
        /// </summary>
        internal void DrawGizmos()
        {

            if (_debugSettings.DebugWaypoints)
            {
                DebugWaypoints();
            }

            if (_debugSettings.DebugDisabledWaypoints)
            {
                DebugDisabledWaypoints();
            }

            if (_debugSettings.DebugAI)
            {
                DebugAI();
            }

            if (_debugSettings.DebugStopWaypoints)
            {
                DebugStopWaypoints();
            }

            if (_debugSettings.DebugSpawnWaypoints)
            {
                DebugSpawnWaypoints();
            }

            if (_debugSettings.DebugPlayModeWaypoints)
            {
                DebugPlayModeWaypoints();
            }
        }


        private void DebugPlayModeWaypoints()
        {
            if (Application.isPlaying)
            {
                Vector3 position;
                var allWaypoints = _gridData.GetAllPedestrianPlayModeWaypoints();
                for (int i = 0; i < allWaypoints.Count; i++)
                {
                    Gizmos.color = Color.blue;
                    position = _waypointsDataHandler.GetPosition(allWaypoints[i]);
                    if (_debugSettings.ShowPosition)
                    {
                        Gizmos.DrawSphere(position, 0.4f);
                    }
                    if (_debugSettings.ShowIndex)
                    {
                        Handles.Label(position, allWaypoints[i].ToString());
                    }
                }
            }
        }


        private void DebugSpawnWaypoints()
        {
            if (Application.isPlaying)
            {
                var allWaypoints = _gridData.GetAllPedestrianSpawnWaypoints();
                for (int i = 0; i < allWaypoints.Count; i++)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(_waypointsDataHandler.GetPosition(allWaypoints[i].WaypointIndex), 0.5f);
                }
            }
        }


        private void DebugStopWaypoints()
        {
            if (Application.isPlaying)
            {
                foreach (var crossings in _allCrossings.GetAllCrossings())
                {
                    foreach (var crossing in crossings.Value)
                    {
                        for (int i = 0; i < crossing.GetIntersectionWaypoints().Length; i++)
                        {
                            if (_waypointsDataHandler.IsStop(crossing.GetIntersectionWaypoints()[i]))
                            {
                                Gizmos.color = Color.red;
                                Gizmos.DrawSphere(_waypointsDataHandler.GetPosition(crossing.GetIntersectionWaypoints()[i]), 1);
                                Gizmos.color = Color.green;
                                var neighbors = _waypointsDataHandler.GetNeighbors(crossing.GetIntersectionWaypoints()[i]);
                                for (int j = 0; j < neighbors.Length; j++)
                                {
                                    if (_waypointsDataHandler.IsCrossing(neighbors[j]))
                                    {
                                        Gizmos.DrawSphere(_waypointsDataHandler.GetPosition(neighbors[j]), 1);
                                    }
                                }
                                var prevs = _waypointsDataHandler.GetPrevs(crossing.GetIntersectionWaypoints()[i]);
                                for (int j = 0; j < prevs.Length; j++)
                                {
                                    if (_waypointsDataHandler.IsCrossing(prevs[j]) == true)
                                    {
                                        Gizmos.DrawSphere(_waypointsDataHandler.GetPosition(prevs[j]), 1);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        private void DebugAI()
        {
            var pedestrians = _allPedestrians.GetAllPedestrians();
            for (int i = 0; i < pedestrians.Count; i++)
            {
                if (pedestrians[i].CurrentBehaviour != null)
                {
                    var hasPath = _waypointManager.HasCustomPath(pedestrians[i].PedestrianIndex) ? "Has Path" : "";
                    var text = $"{pedestrians[i].PedestrianIndex}. Action {pedestrians[i].CurrentBehaviour.Name} {hasPath} \n";

                    Handles.Label(pedestrians[i].transform.position + new Vector3(1, 1, 1), text);
                    if (_debugSettings.DebugPathFinding)
                    {
                        if (_waypointManager.HasCustomPath(pedestrians[i].PedestrianIndex))
                        {
                            var path = _waypointManager.GetPathWaypoints(pedestrians[i].PedestrianIndex);
                            foreach (int n in path)
                            {
                                Gizmos.color = Color.red;
                                var position = _pathFindingData.AllPathFindingWaypoints[n].WorldPosition;
                                Gizmos.DrawWireSphere(position, 1);
                                position.y += 1;
                                Handles.Label(position, pedestrians[i].PedestrianIndex.ToString());
                            }
                        }
                    }
                }
            }
        }


        private void DebugDisabledWaypoints()
        {
            for (int i = 0; i < _waypointActivator.GetDisabledWaypoints().Count; i++)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(_waypointsDataHandler.GetPosition(_waypointActivator.GetDisabledWaypoints()[i]), 0.5f);
            }
        }


        private void DebugWaypoints()
        {
            foreach (var pair in _waypointManager.GetMovementParams())
            {
                for (int i = 0; i < pair.Value.GetAllPathPositions().Count; i++)
                {
                    Vector3 position = pair.Value.GetAllPathPositions()[i];
                    float dimension = 0.4f;
                    bool isWaypoint = true;
                    if (_waypointManager.IsJustPosition(pair.Value.GetAllPathWaypoints()[i]))
                    {
                        dimension = 0.2f;
                        isWaypoint = false;
                    }
                    if (i == 0)
                    {
                        Gizmos.color = Color.green;
                        position = _waypointManager.GetCurrentWaypointPositionFromPath(pair.Key);
                        Gizmos.DrawSphere(position, dimension + 0.1f);
                        Handles.Label(position + new Vector3(0, 1.5f, 0), pair.Key.ToString());
                    }
                    else
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawSphere(position, dimension);

                    }
                    if (isWaypoint)
                    {
                        Gizmos.DrawSphere(_waypointsDataHandler.GetPosition(pair.Value.GetAllPathWaypoints()[i]), 0.1f);
                        Gizmos.DrawLine(_waypointsDataHandler.GetPosition(pair.Value.GetAllPathWaypoints()[i]), position);
                    }
                }
            }
        }
#endif
    }
}

