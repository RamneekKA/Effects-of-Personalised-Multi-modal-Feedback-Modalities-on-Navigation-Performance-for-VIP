#if GLEY_PEDESTRIAN_SYSTEM
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;


namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Controls everything.
    /// </summary>
    internal class PedestrianManager : MonoBehaviour
    {
        private Transform[] _activeCameras;
        private Rigidbody[] _pedestrianRigidbody;
        private List<Pedestrian> _pedestrianList;
        private NativeArray<int> _pedestrianListIndex;
        private NativeArray<float3> _activeCameraPositions;
        private NativeArray<float3> _allPositions;
        private NativeArray<float3> _forwardDirections;
        private NativeArray<float3> _targetPosition;
        private NativeArray<bool> _needsWaypoint;
        private NativeArray<bool> _pedestrianReadyToRemove;
        private NativeArray<PedestrianTypes> _pedestrianTypes;
        private NativeArray<float> _turnAngle;
        private WalkJob _walkJob;
        private JobHandle _walkJobHandle;
        private float3 _up;
        private float _distanceToRemove;
        private float _minDistanceToAdd;
        private int _maxNumberOfPedestrians;
        private int _indexToRemove;
        private int _activeSquaresLevel;
        private int _nrOfJobs;
        private int _activeCameraIndex;
        private bool _initialized;

        private static PedestrianManager _instance;
        public static PedestrianManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (MonoBehaviourUtilities.TryGetSceneScript<PedestrianWaypointsData>(out var result))
                    {
                        _instance = result.Value.gameObject.AddComponent<PedestrianManager>();
                    }
                    else
                    {
                        Debug.LogError(result.Error);
                        Debug.LogError(PedestrianSystemErrors.FatalError);
                    }
                }
                return _instance;
            }
        }

        public static bool Exists
        {
            get
            {
                return _instance != null;
            }
        }

        private PedestrianDensityManager _densityManager;
        internal PedestrianDensityManager DensityManager
        {
            get
            {
                if (_densityManager != null)
                {
                    return _densityManager;
                }
                return ReturnError<PedestrianDensityManager>();
            }
        }

        private AllPedestrians _allPedestrians;
        internal AllPedestrians AllPedestrians
        {
            get
            {
                if (_allPedestrians != null)
                {
                    return _allPedestrians;
                }
                return ReturnError<AllPedestrians>();
            }
        }

        private PedestrianWaypointManager _waypointManager;
        internal PedestrianWaypointManager WaypointManager
        {
            get
            {
                if (_waypointManager != null)
                {
                    return _waypointManager;
                }
                return ReturnError<PedestrianWaypointManager>();
            }
        }

        private PedestrianWaypointsDataHandler _pedestrianWaypointsDataHandler;
        internal PedestrianWaypointsDataHandler PedestrianWaypointsDataHandler
        {
            get
            {
                if (_pedestrianWaypointsDataHandler != null)
                {
                    return _pedestrianWaypointsDataHandler;
                }
                return ReturnError<PedestrianWaypointsDataHandler>();
            }
        }

        private PedestrianAI _pedestrianAI;
        internal PedestrianAI PedestrianAI
        {
            get
            {
                if (_pedestrianAI != null)
                {
                    return _pedestrianAI;
                }
                return ReturnError<PedestrianAI>();
            }
        }

        private AllCrossings _allCrossings;
        internal AllCrossings AllCrossings
        {
            get
            {
                if (_allCrossings != null)
                {
                    return _allCrossings;
                }
                return ReturnError<AllCrossings>();
            }
        }

        private PathFindingManager _pathFindingManager;
        internal PathFindingManager PathFindingManager
        {
            get
            {
                if (PedestrianModules.PathFinding)
                {
                    if (_pathFindingManager != null)
                    {
                        return _pathFindingManager;
                    }
                    else
                    {
                        Debug.LogError(PedestrianSystemErrors.NullPathFindingData);
                    }
                }
                else
                {
                    Debug.LogError(PedestrianSystemErrors.NoPathFindingWaypoints);
                }
                return null;
            }
        }

        private GridData _gridData;
        public GridData GridData
        {
            get
            {
                if (_gridData != null)
                {
                    return _gridData;
                }
                return ReturnError<GridData>();
            }
        }

        private PathFindingData _pedestrianPathFindingData;
        public PathFindingData PedestrianPathFindingData
        {
            get
            {
                if (_pedestrianPathFindingData != null)
                {
                    return _pedestrianPathFindingData;
                }
                return ReturnError<PathFindingData>();
            }
        }

        private WaypointActivator _waypointActivator;
        public WaypointActivator WaypointActivator
        {
            get
            {
                if (_waypointActivator != null)
                {
                    return _waypointActivator;
                }
                return ReturnError<WaypointActivator>();
            }
        }

        private PedestrianModules _pedestrianModules;
        internal PedestrianModules PedestrianModules
        {
            get
            {
                if (_pedestrianModules != null)
                {
                    return _pedestrianModules;
                }
                return ReturnError<PedestrianModules>();
            }
        }

        private DebugManager _debugManager;
        internal DebugManager DebugManager
        {
            get
            {
                if (_debugManager != null)
                {
                    return _debugManager;
                }
                return ReturnError<DebugManager>();
            }
        }

        /// <summary>
        /// Initializing the entire plugin.
        /// </summary>
        internal void Initialize(Transform[] activeCameras, int maxNumberOfPedestrians, PedestrianPool pedestrianPool, PedestrianSystemOptions options)
        {
            LayerSetup layerSetup = Resources.Load<LayerSetup>(PedestrianSystemConstants.LayerSetupData);
            if (layerSetup == null)
            {
                Debug.LogError(PedestrianSystemErrors.LayersNotConfigured);
                return;
            }

            if (maxNumberOfPedestrians <= 0)
            {
                Debug.LogError(PedestrianSystemErrors.NoPedestrians);
                return;
            }

            if (MonoBehaviourUtilities.TryGetSceneScript<GridData>(out var resultGridData))
            {
                if (resultGridData.Value.IsValid(out var error))
                {
                    _gridData = resultGridData.Value;
                }
                else
                {
                    Debug.LogError(error);
                    return;
                }
            }
            else
            {
                Debug.LogError(resultGridData.Error);
                return;
            }


            if (MonoBehaviourUtilities.TryGetSceneScript<PedestrianWaypointsData>(out var resultPedestrianWaypoints))
            {
                if (resultPedestrianWaypoints.Value.IsValid(out var error))
                {
                    _pedestrianWaypointsDataHandler = new PedestrianWaypointsDataHandler(resultPedestrianWaypoints.Value);
                }
                else
                {
                    Debug.LogError(error);
                    return;
                }
            }
            else
            {
                Debug.LogError(resultPedestrianWaypoints.Error);
                return;
            }

            if (MonoBehaviourUtilities.TryGetObjectScript<PedestrianModules>(PedestrianSystemConstants.PlayHolder, out var resultPedestrianModules))
            {
                _pedestrianModules = resultPedestrianModules.Value;
            }
            else
            {
                Debug.LogError(resultPedestrianModules.Error);
            }


            if (PedestrianModules.PathFinding)
            {
                if (MonoBehaviourUtilities.TryGetObjectScript<PathFindingData>(PedestrianSystemConstants.PlayHolder, out var resultPedestrianPathFindingWaypoints))
                {
                    if (resultPedestrianPathFindingWaypoints.Value.IsValid(out var error))
                    {
                        _pedestrianPathFindingData =resultPedestrianPathFindingWaypoints.Value;
                    }
                    else
                    {
                        Debug.LogError(error);
                        return;
                    }
                }
                else
                {
                    Debug.LogError(resultPedestrianPathFindingWaypoints.Error);
                    return;
                }

                _pathFindingManager = new PathFindingManager(GridData, PedestrianPathFindingData);
            }

            bool debugDensity = false;

#if UNITY_EDITOR
            PedestrianDebugSettings debugSettings = PedestrianDebugOptions.LoadOrCreateDebugSettings();
            debugDensity = debugSettings.DebugDensity;
#endif

            Events.OnDestinationChanged += DestinationChanged;
            Events.OnPedestrianAdded += PedestrianAdded;

            _allPositions = new NativeArray<float3>(maxNumberOfPedestrians, Allocator.Persistent);
            _forwardDirections = new NativeArray<float3>(maxNumberOfPedestrians, Allocator.Persistent);
            _targetPosition = new NativeArray<float3>(maxNumberOfPedestrians, Allocator.Persistent);
            _turnAngle = new NativeArray<float>(maxNumberOfPedestrians, Allocator.Persistent);
            _needsWaypoint = new NativeArray<bool>(maxNumberOfPedestrians, Allocator.Persistent);
            _pedestrianReadyToRemove = new NativeArray<bool>(maxNumberOfPedestrians, Allocator.Persistent);
            _pedestrianTypes = new NativeArray<PedestrianTypes>(maxNumberOfPedestrians, Allocator.Persistent);
            _pedestrianListIndex = new NativeArray<int>(maxNumberOfPedestrians, Allocator.Persistent);
            _pedestrianRigidbody = new Rigidbody[maxNumberOfPedestrians];

            _up = Vector3.up;
            _activeCameras = activeCameras;
            _maxNumberOfPedestrians = maxNumberOfPedestrians;
            _activeSquaresLevel = options.ActiveSquaresLevel;

            _allPedestrians = new AllPedestrians(transform, pedestrianPool, maxNumberOfPedestrians, layerSetup.BuildingsLayers, layerSetup.ObstaclesLayers, layerSetup.PlayerLayers, options.PedestrianBehaviours);

            _activeCameraPositions = new NativeArray<float3>(activeCameras.Length, Allocator.Persistent);

            for (int i = 0; i < _activeCameraPositions.Length; i++)
            {
                _activeCameraPositions[i] = activeCameras[i].position;
            }

            if (options.DistanceToRemove < 0)
            {
                float cellSize = GridData.GridCellSize;
                options.DistanceToRemove = 2 * cellSize + cellSize / 2;
            }

            if (options.MinDistanceToAdd < 0)
            {
                float cellSize = GridData.GridCellSize;
                options.MinDistanceToAdd = cellSize + cellSize / 2;
            }
            _distanceToRemove = options.DistanceToRemove * options.DistanceToRemove;
            _minDistanceToAdd = options.MinDistanceToAdd;

            var positionValidator = new PedestrianPositionValidator(_activeCameras, layerSetup.PedestrianLayers, layerSetup.PlayerLayers, layerSetup.BuildingsLayers, _minDistanceToAdd, debugDensity);

            _waypointManager = new PedestrianWaypointManager(maxNumberOfPedestrians, GridData, PedestrianWaypointsDataHandler);
            WaypointManager.SetSpawnWaypointSelector(options.SpawnWaypointSelector);

            _waypointActivator = new WaypointActivator(options.DisableWaypointsArea, PedestrianWaypointsDataHandler, GridData);

            _densityManager = new PedestrianDensityManager(AllPedestrians, WaypointManager, PedestrianWaypointsDataHandler, GridData, activeCameras, maxNumberOfPedestrians, activeCameras[0].position, activeCameras[0].forward, _minDistanceToAdd, layerSetup.PedestrianLayers, layerSetup.BuildingsLayers,
                positionValidator, _activeSquaresLevel, options.UseWaypointPriority, options.InitialDensity, debugDensity);
           
            _pedestrianAI = new PedestrianAI(WaypointManager, PedestrianWaypointsDataHandler, AllPedestrians, options.DefaultPathLength);
           
            _allCrossings = new AllCrossings(PedestrianWaypointsDataHandler);

            _pedestrianList = AllPedestrians.GetAllPedestrians();
            for (int i = 0; i < _pedestrianList.Count; i++)
            {
                _pedestrianTypes[i] = _pedestrianList[i].Type;
                _pedestrianRigidbody[i] = _pedestrianList[i].GetRigidBody();
                _pedestrianListIndex[i] = _pedestrianList[i].PedestrianIndex;
            }

            // Set the number of jobs based on processor count.
            if (SystemInfo.processorCount != 0)
            {
                _nrOfJobs = maxNumberOfPedestrians / SystemInfo.processorCount + 1;
            }
            else
            {
                _nrOfJobs = maxNumberOfPedestrians / 4;
            }
#if UNITY_EDITOR
            if (PedestrianModules.PathFinding)
            {
                _debugManager = new DebugManager(debugSettings, AllPedestrians, WaypointManager, PedestrianWaypointsDataHandler, WaypointActivator, AllCrossings, GridData, PedestrianPathFindingData);
            }
            else
            {
                _debugManager = new DebugManager(debugSettings, AllPedestrians, WaypointManager, PedestrianWaypointsDataHandler, WaypointActivator, AllCrossings, GridData, null);
            }
#endif

            _initialized = true;
        }


        private void Update()
        {
            if (!_initialized)
                return;

            PedestrianAI.UpdatePedestrians(_turnAngle);

            //remove vehicles that are too far away and not in view
            _indexToRemove++;
            if (_indexToRemove == _maxNumberOfPedestrians)
            {
                _indexToRemove = 0;
            }

            _activeCameraIndex = UnityEngine.Random.Range(0, _activeCameraPositions.Length);
            if (_pedestrianReadyToRemove[_indexToRemove] == true)
            {
                if (AllPedestrians.IsActive(_indexToRemove))
                {
                    if (AllPedestrians.CanBeRemoved(_pedestrianListIndex[_indexToRemove]) == true)
                    {
                        RemovePedestrian(_indexToRemove);
                        _pedestrianReadyToRemove[_indexToRemove] = false;
                    }
                }
            }
            // Update additional managers.
            for (int i = 0; i < _activeCameras.Length; i++)
            {
                _activeCameraPositions[i] = _activeCameras[i].transform.position;
            }

            DensityManager.UpdatePedestrianDensity(_activeCameras[_activeCameraIndex].position, _activeCameras[_activeCameraIndex].forward, _activeCameraPositions[_activeCameraIndex]);
        }


        private void FixedUpdate()
        {
            if (!_initialized)
                return;

            for (int i = 0; i < _maxNumberOfPedestrians; i++)
            {
                _allPositions[i] = AllPedestrians.GetPosition(i);
                _forwardDirections[i] = AllPedestrians.GetForwardVector(i);
            }

            _walkJob = new WalkJob()
            {
                AllBotsPosition = _allPositions,
                ForwardDirection = _forwardDirections,
                NeedsWaypoint = _needsWaypoint,
                TargetWaypointPosition = _targetPosition,
                CurrentAngle = _turnAngle,
                FixedDeltaTime = Time.fixedDeltaTime,
                ReadyToRemove = _pedestrianReadyToRemove,
                CameraPositions = _activeCameraPositions,
                DistanceToRemove = _distanceToRemove,
                WorldUp = _up
            };

            _walkJobHandle = _walkJob.Schedule(_maxNumberOfPedestrians, _nrOfJobs);
            _walkJobHandle.Complete();
            _needsWaypoint = _walkJob.NeedsWaypoint;
            _pedestrianReadyToRemove = _walkJob.ReadyToRemove;
            _turnAngle = _walkJob.CurrentAngle;

            for (int i = 0; i < _maxNumberOfPedestrians; i++)
            {
                if (_pedestrianRigidbody[i].gameObject.activeSelf == true)
                {
                    if (_needsWaypoint[i] == true)
                    {
                        PedestrianAI.RequestWaypoint(i, false);
                        _needsWaypoint[i] = false;
                    }
                }
            }
        }


        internal bool IsInitialized()
        {
            return _initialized;
        }


        internal void SetActiveSquaresLevel(int activeSquaresLevel)
        {
            if (!_initialized)
                return;

            _activeSquaresLevel = activeSquaresLevel;
            DensityManager.UpdateActiveSquares(activeSquaresLevel);
        }


        internal void UpdateCamera(Transform[] activeCameras)
        {
            if (!_initialized)
                return;

            if (activeCameras.Length != _activeCameraPositions.Length)
            {
                _activeCameraPositions = new NativeArray<float3>(activeCameras.Length, Allocator.Persistent);
            }

            _activeCameras = activeCameras;
            DensityManager.UpdateCameraPositions(activeCameras);
        }


        internal void SetDestination(int pedestrianIndex, Vector3 position)
        {
            if (PathFindingManager == null)
                return;
            var path = PathFindingManager.GetPathToDestination(pedestrianIndex, WaypointManager.GetCurrentWaypointIndexFromPath(pedestrianIndex), position, AllPedestrians.GetPedestrianType(pedestrianIndex));
            WaypointManager.SetPath(pedestrianIndex, path);
        }


        internal void AddPedestrianAtPosition(Vector3 position, PedestrianTypes pedestrianType, UnityAction<Pedestrian, int> completeMethod, Vector3 destination, int pedestrianIndex)
        {
            if (PathFindingManager == null)
                return;
            // If path is required -> compute path.
            List<int> path = null;
            if (destination != default)
            {
                path = PathFindingManager.GetPath(position, destination, pedestrianType);
                if (path == null)
                {
                    Debug.LogWarning($"Path not found from {position} to {destination} for {pedestrianType}. This request will be ignored");
                    return;
                }
            }
            DensityManager.AddPedestrianAtPosition(position, pedestrianType, completeMethod, path, pedestrianIndex);
        }


        private T ReturnError<T>()
        {
            StackTrace stackTrace = new StackTrace();
            string callingMethodName = string.Empty;
            if (stackTrace.FrameCount >= 3)
            {
                StackFrame callingFrame = stackTrace.GetFrame(1);
                callingMethodName = callingFrame.GetMethod().Name;
            }
            Debug.LogError($"Mobile Pedestrian System is not initialized. Call Gley.PedestrianSystem.Initialize() before calling {callingMethodName}");
            return default(T);
        }


        private void PedestrianAdded(int pedestrianIndex, Vector3 position)
        {
            _targetPosition[pedestrianIndex] = position;
            _turnAngle[pedestrianIndex] = 0;
        }


        private void DestinationChanged(int pedestrianIndex, Vector3 newPosition)
        {
            _targetPosition[pedestrianIndex] = newPosition;
        }


        private void RemovePedestrian(int index)
        {
            if (!_initialized)
                return;
            DensityManager.RemovePedestrian(_pedestrianListIndex[index]);
        }


        private void OnDestroy()
        {
            if (!_initialized)
                return;
            _initialized = false;
            Events.OnDestinationChanged -= DestinationChanged;
            Events.OnPedestrianAdded -= PedestrianAdded;

            _activeCameraPositions.Dispose();
            _allPositions.Dispose();
            _forwardDirections.Dispose();
            _needsWaypoint.Dispose();
            _targetPosition.Dispose();
            _pedestrianTypes.Dispose();
            _pedestrianListIndex.Dispose();
            _turnAngle.Dispose();
            _pedestrianReadyToRemove.Dispose();
            DestroyableManager.Instance.DestroyAll();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_initialized)
                return;
            DebugManager.DrawGizmos();
        }
#endif
    }
}
#endif