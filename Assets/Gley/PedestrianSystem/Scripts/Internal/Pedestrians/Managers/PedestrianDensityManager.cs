using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Controls activation/deactivation of the pedestrians 
    /// </summary>
    internal class PedestrianDensityManager
    {
        private Queue<PedestrianRequest> _requestedPedestrians;
        private AllPedestrians _allPedestrians;
        private PedestrianWaypointManager _waypointManager;
        private GridData _gridData;
        private PedestrianPositionValidator _positionValidator;
        private PedestrianWaypointsDataHandler _pedestrianWaypointsDatahandler;
        private int _maxNumberOfPedestrians;
        private int _currentNrOfPedestrians;
        private int _activeSquaresLevel;
        private bool _useWaypointPriority;
        private bool _debugDensity;


        /// <summary>
        /// Storage class to create a specific instantiation request. 
        /// </summary>
        private class PedestrianRequest
        {
            internal UnityAction<Pedestrian, int> CompleteMethod { get; }
            internal List<int> Path { get; }
            internal Category Category { get; }
            internal Pedestrian Pedestrian { get; set; }
            internal PedestrianTypes Type { get; set; }
            internal int WaypointIndex { get; set; }

            internal PedestrianRequest(int waypoint, PedestrianTypes type, Category category, Pedestrian pedestrian, UnityAction<Pedestrian, int> completeMethod, List<int> path)
            {
                WaypointIndex = waypoint;
                Type = type;
                Category = category;
                Pedestrian = pedestrian;
                CompleteMethod = completeMethod;
                Path = path;
            }


            internal PedestrianRequest()
            {

            }


            internal bool IsValid()
            {
                if (WaypointIndex < 0)
                    return false;
                if (Pedestrian == null)
                    return false;
                if (Pedestrian.gameObject.activeSelf)
                    return false;
                return true;
            }
        }

        /// <summary>
        /// Types of the pedestrians
        /// </summary>
        private enum Category
        {
            Idle,
            Exlcuded,
        }



        internal PedestrianDensityManager(AllPedestrians allPedestrians, PedestrianWaypointManager waypointManager, PedestrianWaypointsDataHandler pedestrianWaypointsDataHandler, GridData gridData, Transform[] activeCameras, int maxNumberOfPedestrians, Vector3 position, Vector3 direction, float minDistanceToAdd, LayerMask trafficLayer, LayerMask buildingsLayers,
            PedestrianPositionValidator positionValidator, int activeSquaresLevel, bool useWaypointPriority, int initialDensity, bool debugDensity)
        {
            _waypointManager = waypointManager;
            _allPedestrians = allPedestrians;
            _maxNumberOfPedestrians = maxNumberOfPedestrians;
            _gridData = gridData;
            _useWaypointPriority = useWaypointPriority;
            _activeSquaresLevel = activeSquaresLevel;
            _positionValidator = positionValidator;
            _debugDensity = debugDensity;
            _requestedPedestrians = new Queue<PedestrianRequest>();
            _pedestrianWaypointsDatahandler = pedestrianWaypointsDataHandler;

            if (initialDensity >= 0)
            {
                SetPedestrianDensity(initialDensity);
            }

            List<CellData> gridCells = new List<CellData>();
            for (int i = 0; i < activeCameras.Length; i++)
            {
                gridCells.Add(_gridData.GetCell(activeCameras[i].position.x, activeCameras[i].position.z));
            }

            LoadInitialPedestrians(gridCells, position, direction);
        }


        /// <summary>
        /// Change the number of active squares.
        /// </summary>
        internal void UpdateActiveSquares(int newLevel)
        {
            _activeSquaresLevel = newLevel;
        }


        /// <summary>
        /// Update camera transform
        /// </summary>
        internal void UpdateCameraPositions(Transform[] activeCameras)
        {
            _positionValidator.UpdateCamera(activeCameras);
        }


        /// <summary>
        /// Adds new vehicles if required.
        /// </summary>
        internal void UpdatePedestrianDensity(Vector3 playerPosition, Vector3 playerDirection, Vector3 activeCameraposition)
        {
            if (_currentNrOfPedestrians < _maxNumberOfPedestrians)
            {
                CellData gridCell = _gridData.GetCell(activeCameraposition);
                StartAddingPedestrian(playerPosition, playerDirection, gridCell, false);
            }
        }


        /// <summary>
        /// Remove a pedestrian.
        /// </summary>
        internal void RemovePedestrian(int pedestrianIndex)
        {
            _currentNrOfPedestrians--;
            _waypointManager.RemovePedestrian(pedestrianIndex);
            Events.TriggerPedestrianRemovedEvent(pedestrianIndex);
            _allPedestrians.DisablePedestrian(pedestrianIndex);
        }

        
        /// <summary>
        /// Add a pedestrian.
        /// </summary>
        internal void AddPedestrianAtPosition(Vector3 position, PedestrianTypes type, UnityAction<Pedestrian, int> completeMethod, List<int> path, int pedestrianIndex)
        {
            // Verify waypoint.
            var waypointIndex = GetClosestSpawnWaypoint(position, type);
            if (waypointIndex == PedestrianSystemConstants.INVALID_WAYPOINT_INDEX)
            {
                Debug.LogWarning("There are no free waypoints in the current cell");
                return;
            }

            PedestrianTypes pedestrianType = type;
            Pedestrian pedestrian = null;
            Category category = Category.Idle;

            // Load pedestrian if required.
            if (pedestrianIndex != PedestrianSystemConstants.INVALID_PEDESTRIAN_INDEX)
            {
                pedestrian = _allPedestrians.GetPedestrianWithValidation(pedestrianIndex);
                if (pedestrian == null)
                {
                    Debug.LogWarning($"No pedestrian found with index {pedestrianIndex}. This request will be ignored");
                    return;
                }
                pedestrianType = pedestrian.Type;
                category = Category.Exlcuded;
            }

            _requestedPedestrians.Enqueue(new PedestrianRequest(waypointIndex, pedestrianType, category, pedestrian, completeMethod, path));
        }


        internal void UpdateOffset(int pedestrianIndex, float offset)
        {
            _allPedestrians.UpdateOffset(pedestrianIndex, offset);
            _waypointManager.UpdateOffset(pedestrianIndex, offset);
        }

        internal void SetPedestrianDensity(int nrOfPedestrians)
        {
            _maxNumberOfPedestrians = nrOfPedestrians;
        }

        private int GetClosestSpawnWaypoint(Vector3 position, PedestrianTypes type)
        {
            List<SpawnWaypoint> possibleWaypoints = _gridData.GetPedestrianSpawnWaypoipointsAroundPosition(position, (int)type);

            if (possibleWaypoints.Count == 0)
                return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;

            float distance = float.MaxValue;
            int waypointIndex = PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
            for (int i = 0; i < possibleWaypoints.Count; i++)
            {
                float newDistance = Vector3.SqrMagnitude(_pedestrianWaypointsDatahandler.GetPosition(possibleWaypoints[i].WaypointIndex) - position);
                if (newDistance < distance)
                {
                    distance = newDistance;
                    waypointIndex = possibleWaypoints[i].WaypointIndex;
                }
            }
            return waypointIndex;
        }


       


        /// <summary>
        /// Load pedestrians around player without LOS restriction.
        /// </summary>
        private void LoadInitialPedestrians(List<CellData> gridCells, Vector3 playerPosition, Vector3 playerDirection)
        {
            for (int i = 0; i < _maxNumberOfPedestrians; i++)
            {
                int cellIndex = Random.Range(0, gridCells.Count);
                StartAddingPedestrian(playerPosition, playerDirection, gridCells[cellIndex], true);
            }
        }


        private void StartAddingPedestrian(Vector3 playerPosition, Vector3 playerDirection, CellData gridCell, bool ignorLOS)
        {
            PedestrianRequest request = null;
            if (_requestedPedestrians.Count != 0)
            {
                // Add specific pedestrian on position.
                request = _requestedPedestrians.Peek();
                switch (request.Category)
                {
                    case Category.Idle:
                        if (request.Pedestrian == null)
                        {
                            var pedestrian = _allPedestrians.GetIdlePedestrianOfType(request.Type);
                            // If an idle pedestrian of the requested type does not exists.
                            if (pedestrian != null)
                            {
                                request.Pedestrian = pedestrian;
                                _allPedestrians.RemoveIdlePedestrian(pedestrian);
                            }
                            else
                            {
                                if (_debugDensity)
                                {
                                    Debug.Log($"Density: No pedestrian of type {request.Type} is idle");
                                }
                            }
                        }
                        break;
                }
            }

            // Verify if stored request is valid.
            if (RequestIsValid(request))
            {
                _requestedPedestrians.Dequeue();
                InstantiatePedestrian(request);
                return;
            }

            // Create new request for any pedestrian.
            request = CreateRequest(playerPosition, playerDirection, gridCell, ignorLOS);

            if (RequestIsValid(request))
            {
                InstantiatePedestrian(request);
                return;
            }
        }


        private PedestrianRequest CreateRequest(Vector3 playerPosition, Vector3 playerDirection, CellData gridCell, bool ignorLOS)
        {
            PedestrianRequest request = new PedestrianRequest();

            // Check for an idle pedestrian.
            int idlePedestrianIndex = _allPedestrians.GetRandomIdlePedestrianIndex();
            if (idlePedestrianIndex == PedestrianSystemConstants.INVALID_PEDESTRIAN_INDEX)
            {
                if (_debugDensity)
                {
                    Debug.Log("Density: No idle pedestrians found");
                }
                return null;
            }
            request.Type = _allPedestrians.GetIdlePedestrianType(idlePedestrianIndex);


            // Check for a free waypoint.
            int freeWaypointIndex = _waypointManager.GetNeighborCellWaypoint(gridCell.CellProperties.Row, gridCell.CellProperties.Column, _activeSquaresLevel, request.Type, playerPosition, playerDirection, _useWaypointPriority);
            if (freeWaypointIndex == PedestrianSystemConstants.INVALID_WAYPOINT_INDEX)
            {
                if (_debugDensity)
                {
                    Debug.Log("Density: No free waypoint found");
                }
                return null;
            }

            // If a valid waypoint was found, check if it was not manually disabled.
            if (_pedestrianWaypointsDatahandler.IsTemporaryDisabled(freeWaypointIndex))
            {
                if (_debugDensity)
                {
                    Debug.Log("Density: waypoint is disabled");
                }
                return null;
            }


            // Check if the pedestrian type can be instantiated on selected waypoint.
            var position = _pedestrianWaypointsDatahandler.GetPosition(freeWaypointIndex) + _pedestrianWaypointsDatahandler.GetLaneWidth(freeWaypointIndex) / 2 * _allPedestrians.GetIdlePedestrianWithVaidation(idlePedestrianIndex).Offset * _pedestrianWaypointsDatahandler.GetLeftDirection(freeWaypointIndex);

            if (!_positionValidator.IsValid(position, ignorLOS))
            {
                return null;
            }

            // Create the request.
            request.WaypointIndex = freeWaypointIndex;
            request.Pedestrian = _allPedestrians.GetIdlePedestrianWithVaidation(idlePedestrianIndex);
            return request;
        }


        private bool RequestIsValid(PedestrianRequest request)
        {
            if (request == null)
                return false;
            return request.IsValid();
        }


        /// <summary>
        /// Instantiate a pedestrian on scene.
        /// </summary>
        private void InstantiatePedestrian(PedestrianRequest request)
        {
            _currentNrOfPedestrians++;
            _allPedestrians.RemoveIdlePedestrian(request.Pedestrian);
            _waypointManager.SetInitialWaypoint(request.Pedestrian.PedestrianIndex, request.WaypointIndex, request.Pedestrian.Type, request.Pedestrian.Offset);
            _allPedestrians.ActivatePedestrian(request.Pedestrian, _waypointManager.GetCurrentWaypointPositionFromPath(request.Pedestrian.PedestrianIndex), _waypointManager.GetCurrentWaypointRotation(request.Pedestrian.PedestrianIndex));
            request.CompleteMethod?.Invoke(request.Pedestrian, request.WaypointIndex);
            if (request.Path != null)
            {
                _waypointManager.SetPath(request.Pedestrian.PedestrianIndex, request.Path);
            }
            Events.TriggerPedestrianAddedEvent(request.Pedestrian.PedestrianIndex, _waypointManager.GetCurrentWaypointPositionFromPath(request.Pedestrian.PedestrianIndex));
        }
    }
}