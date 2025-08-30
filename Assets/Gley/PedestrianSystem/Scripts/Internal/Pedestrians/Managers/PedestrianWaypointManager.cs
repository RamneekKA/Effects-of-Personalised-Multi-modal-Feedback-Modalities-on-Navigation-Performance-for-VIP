using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Controls the pedestrian destination.
    /// </summary>
    internal class PedestrianWaypointManager : IDestroyable
    {
        private readonly Dictionary<int, MovementParams> _movementParams;
        private readonly GridData _gridData;
        private readonly PedestrianWaypointsDataHandler _pedestrianWaypointsDataHendler;

        private PedestrianSpawnWaypointSelector _spawnWaypointSelector;

        /// <summary>
        /// Special type of waypoint that was not drawn in editor.
        /// Associates and index with a position.
        /// </summary>
        internal class PositionWaypoint
        {
            internal int WaypointIndex;
            internal Vector3 WaypointPosition;

            internal PositionWaypoint(int waypointIndex, Vector3 waypointPosition)
            {
                WaypointIndex = waypointIndex;
                WaypointPosition = waypointPosition;
            }
        }

        /// <summary>
        /// Movement details for each pedestrian.
        /// </summary>
        internal class MovementParams
        {
            private List<PositionWaypoint> _targetWaypoints = new List<PositionWaypoint>();
            private int _negativePositionIndex;

            internal Vector3 OldPosition { get; private set; }
            internal int OldWaypointIndex { get; private set; }
            internal bool HasPath { get; private set; }
            internal float Offset { get; set; }
            internal bool IsCrossing { get; set; }
            internal bool CannotPass { get; set; }

            internal int PathLength
            {
                get
                {
                    return _targetWaypoints.Count;
                }
            }


            internal int GetWaypointIndex(int pathPosition)
            {
                if (IsPathPositionValid(pathPosition))
                {
                    return _targetWaypoints[pathPosition].WaypointIndex;
                }
                return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
            }


            internal Vector3 GetWaypointPosition(int pathPosition)
            {
                if (IsPathPositionValid(pathPosition))
                {
                    return _targetWaypoints[pathPosition].WaypointPosition;
                }
                return PedestrianSystemConstants.INVALID_WAYPOINT_POSITION;
            }


            internal void AddWaypointAsTarget(int waypointIndex, Vector3 waypointPosition)
            {
                if (waypointIndex < 0)
                {
                    Debug.LogError($"Cannot add {waypointIndex} as target");
                    return;
                }
                _targetWaypoints.Add(new PositionWaypoint(waypointIndex, waypointPosition));
            }


            internal void RemoveWaypointFromTarget(int waypointIndex)
            {
                _targetWaypoints.Remove(_targetWaypoints.FirstOrDefault(cond => cond.WaypointIndex == waypointIndex));
            }


            // Returns the waypoint index which is negative if just a position is added as a target.
            internal int InsertPositionAsTarget(int positionToInsert, Vector3 targetPosition)
            {
                if (positionToInsert < 0 || positionToInsert >= _targetWaypoints.Count)
                {
                    Debug.LogError($"Cannot add target position {targetPosition} on list position {positionToInsert}");
                    return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
                }
                _negativePositionIndex--;
                if (_negativePositionIndex == int.MinValue)
                {
                    _negativePositionIndex = -1;
                }
                _targetWaypoints.Insert(positionToInsert, new PositionWaypoint(_negativePositionIndex, targetPosition));
                return _negativePositionIndex;
            }


            internal void SetPath(List<PositionWaypoint> path)
            {
                _targetWaypoints = path;
                HasPath = true;
            }


            internal void TargetPassed()
            {
                if (_targetWaypoints.Count <= 0)
                {
                    return;
                }
                //is just a position in the list
                if (_targetWaypoints[0].WaypointIndex >= 0)
                {
                    OldWaypointIndex = _targetWaypoints[0].WaypointIndex;
                }
                OldPosition = _targetWaypoints[0].WaypointPosition;
                _targetWaypoints.RemoveAt(0);
                if (CannotPass != false)
                {
                    CannotPass = false;
                }
                if (IsCrossing != false)
                {
                    IsCrossing = false;
                }
            }


            internal void ClearTarget()
            {
                _targetWaypoints = new List<PositionWaypoint>();
                CannotPass = false;
                OldWaypointIndex = PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
                OldPosition = default;
            }


            internal void ClearPath()
            {
                if (_targetWaypoints.Count > 0)
                {
                    _targetWaypoints = new List<PositionWaypoint> { _targetWaypoints[0] };
                }
                CannotPass = false;
                HasPath = false;
            }


            internal List<int> GetAllPathWaypoints()
            {
                return _targetWaypoints.Select(cond => cond.WaypointIndex).ToList();
            }


            internal List<Vector3> GetAllPathPositions()
            {
                return _targetWaypoints.Select(cond => cond.WaypointPosition).ToList();
            }


            internal List<PositionWaypoint> GetPath()
            {
                return _targetWaypoints;
            }


            internal void SetTargetWaypoints(List<PositionWaypoint> path)
            {
                _targetWaypoints = path;
            }


            private bool IsPathPositionValid(int pathPosition)
            {
                if (pathPosition < 0)
                {
                    Debug.LogError($"Path position {pathPosition} should be >= 0");
                    return false;
                }
                if (pathPosition >= _targetWaypoints.Count)
                {
                    Debug.LogError($"Path position {pathPosition} should be < {_targetWaypoints.Count}");
                    return false;
                }
                return true;
            }
        }


        internal PedestrianWaypointManager(int maxNumberOfPedestrians, GridData gridData, PedestrianWaypointsDataHandler pedestrianWaypointsDataHandler)
        {
            _gridData = gridData;
            _pedestrianWaypointsDataHendler = pedestrianWaypointsDataHandler;
            _movementParams = new Dictionary<int, MovementParams>();

            for (int i = 0; i < maxNumberOfPedestrians; i++)
            {
                _movementParams.Add(i, new MovementParams());
            }
            Assign();
            Events.OnStopStateChanged += StopStateChanged;
        }

        public void Assign()
        {
            DestroyableManager.Instance.Register(this);
        }


        #region PathWaypoints
        internal void SetPath(int pedestrianIndex, List<int> pathWaypoints)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                List<PositionWaypoint> path = new List<PositionWaypoint>();
                foreach (var waypoint in pathWaypoints)
                {
                    path.Add(new PositionWaypoint(waypoint, GetWaypointPositionWithOffset(pedestrianIndex, waypoint)));
                }
                _movementParams[pedestrianIndex].SetPath(path);
                Events.TriggerDestinationChangedEvent(pedestrianIndex, GetCurrentWaypointPositionFromPath(pedestrianIndex));
            }
        }


        internal Dictionary<int, MovementParams> GetMovementParams()
        {
            return _movementParams; 
        }


        internal void RemovePath(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                _movementParams[pedestrianIndex].ClearPath();
            }
        }


        internal List<int> GetPathWaypoints(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].GetAllPathWaypoints();
            }
            return null;
        }


        internal int GetPathLength(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].PathLength;
            }
            return 0;
        }


        internal bool HasCustomPath(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].HasPath;
            }
            return false;
        }


        internal int InsertPositionAsTarget(int pedestrianIndex, int positionInCurrentPath, Vector3 position)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].InsertPositionAsTarget(positionInCurrentPath, position);
            }
            return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
        }


        internal void AddWaypointToPath(int pedestrianIndex, int waypointIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                _movementParams[pedestrianIndex].AddWaypointAsTarget(waypointIndex, GetWaypointPositionWithOffset(pedestrianIndex, waypointIndex));
            }
        }


        internal void RemoveWaypointFromPath(int pedestrianIndex, int waypointIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                _movementParams[pedestrianIndex].RemoveWaypointFromTarget(waypointIndex);
            }
        }


        /// <summary>
        /// Check if the current waypoint is a real waypoint or just a 3D position added at runtime.
        /// </summary>
        internal bool IsJustPosition(int waypointIndex)
        {
            if (waypointIndex < 0 && waypointIndex != PedestrianSystemConstants.INVALID_WAYPOINT_INDEX)
            {
                return true;
            }
            return false;
        }


        internal int GetPreviousWaypointIndexFromPath(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].OldWaypointIndex;
            }
            return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
        }


        internal int GetCurrentWaypointIndexFromPath(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].GetWaypointIndex(0);
            }
            return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
        }


        internal int GetNextWaypointIndexFromPath(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].GetWaypointIndex(1);
            }
            return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
        }


        internal int GetSecondToLastWaypointIndex(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].GetWaypointIndex(_movementParams[pedestrianIndex].PathLength - 2);
            }
            return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
        }


        internal int GetLastWaypointIndexFromPath(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].GetWaypointIndex(_movementParams[pedestrianIndex].PathLength - 1);
            }
            return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
        }


        internal Vector3 GetPreviousWaypointPositionFromPath(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].OldPosition;
            }
            return PedestrianSystemConstants.INVALID_WAYPOINT_POSITION;
        }


        internal Vector3 GetCurrentWaypointPositionFromPath(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                if (IsCurrentWaypointAStopWaypoint(pedestrianIndex))
                {
                    return _pedestrianWaypointsDataHendler.GetPosition(GetCurrentWaypointIndexFromPath(pedestrianIndex)) + _pedestrianWaypointsDataHendler.GetLaneWidth(GetNextWaypointIndexFromPath(pedestrianIndex)) / 2 * _movementParams[pedestrianIndex].Offset * _pedestrianWaypointsDataHendler.GetLeftDirection(GetNextWaypointIndexFromPath(pedestrianIndex));
                }
                return _movementParams[pedestrianIndex].GetWaypointPosition(0);
            }
            return PedestrianSystemConstants.INVALID_WAYPOINT_POSITION;
        }


        /// <summary>
        /// Check if the current waypoint is in intersection and the next waypoint is in direction of the crossing.
        /// </summary>
        internal bool IsCurrentWaypointAStopWaypoint(int pedestrianIndex)
        {
            if (_movementParams[pedestrianIndex].PathLength > 1)
            {
                if (!IsJustPosition(GetCurrentWaypointIndexFromPath(pedestrianIndex)))
                {
                    var waypointIndex = GetCurrentWaypointIndexFromPath(pedestrianIndex);

                    if (_pedestrianWaypointsDataHendler.IsStop(waypointIndex) && _pedestrianWaypointsDataHendler.IsCrossing(GetNextWaypointIndexFromPath(pedestrianIndex)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        internal Vector3 GetNextWaypointPositionFromPath(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].GetWaypointPosition(1);
            }
            return PedestrianSystemConstants.INVALID_WAYPOINT_POSITION;
        }


        internal bool GetCurrentWaypointStopState(int pedestrianIndex)
        {
            if (!IsJustPosition(GetCurrentWaypointIndexFromPath(pedestrianIndex)))
            {
                return _pedestrianWaypointsDataHendler.IsStop(GetCurrentWaypointIndexFromPath(pedestrianIndex));
            }
            return false;
        }


        internal bool GetNextWaypointCrossingState(int pedestrianIndex)
        {
            if (!IsJustPosition(GetNextWaypointIndexFromPath(pedestrianIndex)))
            {
                return _pedestrianWaypointsDataHendler.IsCrossing(GetNextWaypointIndexFromPath(pedestrianIndex));
            }
            return false;
        }


        /// <summary>
        /// Check if pedestrian is allowed to change waypoint.
        /// </summary>
        internal bool CannotPass(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].CannotPass;
            }
            return false;
        }


        /// <summary>
        /// Check if pedestrian starts to cross.
        /// Used to trigger the crossing event only once.
        /// </summary>
        internal bool IsCrossing(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].IsCrossing;
            }
            return false;
        }


        internal void SetCrossingProperty(int pedestrianIndex, bool wantsToCross)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                _movementParams[pedestrianIndex].IsCrossing = wantsToCross;
            }
        }


        internal void SetCannotPassProperty(int pedestrianIndex, bool cannotPass)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                _movementParams[pedestrianIndex].CannotPass = cannotPass;
            }
        }


        /// <summary>
        /// Remove target waypoint for the agent at index
        /// </summary>
        /// <param name="pedestrianIndex"></param>
        internal void RemovePedestrian(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                _movementParams[pedestrianIndex].ClearTarget();
            }
        }
        #endregion


        #region Spawn Waypoints
        /// <summary>
        /// Set the default waypoint generating method
        /// </summary>
        internal void SetSpawnWaypointSelector(PedestrianSpawnWaypointSelector spawnWaypointSelector)
        {
            _spawnWaypointSelector = spawnWaypointSelector;
        }


        internal int GetNeighborCellWaypoint(int row, int column, int depth, PedestrianTypes pedestrianType, Vector3 playerPosition, Vector3 playerDirection, bool useWaypointPriority)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            // Get all cell neighbors for the specified depth.
            List<Vector2Int> neighbors = _gridData.GetCellNeighbors(row, column, depth, false);



            for (int i = neighbors.Count - 1; i >= 0; i--)
            {
                if (!_gridData.HasPedestrianSpawnWaypoints(neighbors[i]))
                {
                    neighbors.RemoveAt(i);
                }
            }
            // If neighbors exists.
            if (neighbors.Count > 0)
            {
                return ApplyNeighborSelectorMethod(neighbors, playerPosition, playerDirection, pedestrianType, useWaypointPriority);
            }
#endif
            return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
        }
        #endregion


        internal bool IsPedestrianAtThisWaypoint(int pedestrianIndex, int waypointIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _movementParams[pedestrianIndex].GetWaypointIndex(0) == waypointIndex;
            }
            return false;
        }


        internal void ChangeWaypoint(int pedestrianIndex)
        {
            if (!IsPedestrianIndexValid(pedestrianIndex))
                return;

            // Check if it is waypoint.
            if (!IsJustPosition(GetCurrentWaypointIndexFromPath(pedestrianIndex)))
            {
                int waypointIndex = GetCurrentWaypointIndexFromPath(pedestrianIndex);
                if (_pedestrianWaypointsDataHendler.HasIntersection(waypointIndex))
                {
                    _pedestrianWaypointsDataHendler.GetAssociatedIntersection(waypointIndex).PedestrianPassed(pedestrianIndex);
                }

                // Trigger custom event if exists.
                if (_pedestrianWaypointsDataHendler.IsTriggerEvent(waypointIndex))
                {
                    Events.TriggerWaypointReachedEvent(pedestrianIndex, waypointIndex, _pedestrianWaypointsDataHendler.GetEventData(waypointIndex));
                }
            }



            // Remove first waypoint from queue.
            _movementParams[pedestrianIndex].TargetPassed();

            if (_movementParams[pedestrianIndex].PathLength != 0)
            {
                Events.TriggerDestinationChangedEvent(pedestrianIndex, GetCurrentWaypointPositionFromPath(pedestrianIndex));
            }
            else
            {
                Events.TriggerDestinationReachedEvent(pedestrianIndex);
            }
        }


        internal void SetInitialWaypoint(int pedestrianIndex, int waypointIndex, PedestrianTypes type, float offset)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                _movementParams[pedestrianIndex].Offset = offset;
                AddWaypointToPath(pedestrianIndex, waypointIndex);
                var possibleWaypoints = _pedestrianWaypointsDataHendler.GetPossibleWaypoints(waypointIndex, type);
                if (possibleWaypoints.Count > 0)
                {
                    AddWaypointToPath(pedestrianIndex, possibleWaypoints[Random.Range(0, possibleWaypoints.Count)]);
                }
            }
        }


        internal void UpdateOffset(int pedestrianIndex, float offset)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                if (offset >= -1 && offset <= 1)
                {
                    _movementParams[pedestrianIndex].Offset = offset;
                    var path = _movementParams[pedestrianIndex].GetPath();
                    for (int i = 0; i < path.Count; i++)
                    {
                        if (path[i].WaypointIndex >= 0)
                        {
                            path[i].WaypointPosition = GetWaypointPositionWithOffset(pedestrianIndex, path[i].WaypointIndex);
                        }
                    }

                    _movementParams[pedestrianIndex].SetTargetWaypoints(path);

                }
                else
                {
                    Debug.LogWarning($"Offset {offset} is not valid. Should be between [-1,1]");
                }
            }
        }


        /// <summary>
        /// Get rotation of the target waypoint
        /// </summary>
        /// <param name="pedestrianIndex"></param>
        /// <returns></returns>
        internal Quaternion GetCurrentWaypointRotation(int pedestrianIndex)
        {
            return Quaternion.LookRotation(_pedestrianWaypointsDataHendler.GetPosition(GetNextWaypointIndexFromPath(pedestrianIndex)) - _pedestrianWaypointsDataHendler.GetPosition(GetCurrentWaypointIndexFromPath(pedestrianIndex)));
        }


        private int ApplyNeighborSelectorMethod(List<Vector2Int> neighbors, Vector3 playerPosition, Vector3 playerDirection, PedestrianTypes pedestrianType, bool useWaypointPriority)
        {
            try
            {
                return _spawnWaypointSelector(neighbors, playerPosition, playerDirection, pedestrianType, useWaypointPriority);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Your neighbor selector method has the following error: " + e.Message);
                return DefaultPedestrianDelegates.GetRandomSpawnWaypoint(neighbors, playerPosition, playerDirection, pedestrianType, useWaypointPriority);
            }
        }


        private bool IsPedestrianIndexValid(int pedestrianIndex)
        {
            if (pedestrianIndex < 0)
            {
                Debug.LogError($"Pedestrian index {pedestrianIndex} should be >= 0");
                return false;
            }
            if (_movementParams.Count <= pedestrianIndex)
            {
                Debug.LogError($"Pedestrian index {pedestrianIndex} should be < {_movementParams.Count}");
                return false;
            }
            if (_movementParams[pedestrianIndex] == null)
            {
                Debug.LogError($"No movement parameters found for {pedestrianIndex}");
                return false;
            }

            return true;
        }


        private Vector3 GetWaypointPositionWithOffset(int pedestrianIndex, int waypointIndex)
        {
            if (!IsPedestrianIndexValid(pedestrianIndex))
            {
                Debug.LogError($" Waypoint index {waypointIndex} is not valid for pedestrian index {pedestrianIndex}");
                return PedestrianSystemConstants.INVALID_WAYPOINT_POSITION;
            }


            return _pedestrianWaypointsDataHendler.GetPosition(waypointIndex) + _pedestrianWaypointsDataHendler.GetLaneWidth(waypointIndex) / 2 * _movementParams[pedestrianIndex].Offset * _pedestrianWaypointsDataHendler.GetLeftDirection(waypointIndex);
        }


        /// <summary>
        /// Event listener for traffic lights.
        /// </summary>
        private void StopStateChanged(int waypointIndex, bool newValue)
        {
            _pedestrianWaypointsDataHendler.SetStopValue(waypointIndex, newValue);
        }


        public void OnDestroy()
        {
            Events.OnStopStateChanged -= StopStateChanged;
        }
    }
}