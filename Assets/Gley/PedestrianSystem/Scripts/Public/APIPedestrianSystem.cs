using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PedestrianSystemConstants = Gley.PedestrianSystem.Internal.PedestrianSystemConstants;
#if GLEY_PEDESTRIAN_SYSTEM
using PedestrianManager = Gley.PedestrianSystem.Internal.PedestrianManager;
#endif
namespace Gley.PedestrianSystem
{
    public static class API
    {
        #region PedestrianManager
        /// <summary>
        /// Initialize the Pedestrian System.
        /// </summary>
        /// <param name="activeCamera">Camera that follows the player or the player itself.</param>
        /// <param name="maxNumberOfPedestrians">Maximum number of pedestrians active at the same time.</param>
        /// <param name="pedestrianPool">Available pedestrians asset.</param>
        public static void Initialize(Transform activeCamera, int maxNumberOfPedestrians, PedestrianPool pedestrianPool)
        {
            Initialize(activeCamera, maxNumberOfPedestrians, pedestrianPool, new PedestrianSystemOptions());
        }


        /// <summary>
        /// Initialize the Pedestrian System.
        /// </summary>
        /// <param name="activeCamera">Camera that follows the player or the player itself.</param>
        /// <param name="maxNumberOfPedestrians">Maximum number of pedestrians active at the same time.</param>
        /// <param name="pedestrianPool">Available pedestrians asset.</param>
        /// <param name="options">An object used to store the initialization parameters.</param>
        public static void Initialize(Transform activeCamera, int maxNumberOfPedestrians, PedestrianPool pedestrianPool, PedestrianSystemOptions options)
        {
            Initialize(new Transform[] { activeCamera }, maxNumberOfPedestrians, pedestrianPool, options);
        }


        /// <summary>
        /// Initialize the Pedestrian System.
        /// </summary>
        /// <param name="activeCameras">Camera that follows the player or the player itself.</param>
        /// <param name="maxNumberOfPedestrians">Maximum number of pedestrians active at the same time.</param>
        /// <param name="pedestrianPool">Available pedestrians asset.</param>
        /// <param name="options">An object used to store the initialization parameters.</param>
        public static void Initialize(Transform[] activeCameras, int maxNumberOfPedestrians, PedestrianPool pedestrianPool, PedestrianSystemOptions options)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.Initialize(activeCameras, maxNumberOfPedestrians, pedestrianPool, options);
#endif
        }


        /// <summary>
        /// Check if the Pedestrian System is initialized.
        /// </summary>
        /// <returns>true if initialized</returns>
        public static bool IsInitialized()
        {
#if GLEY_PEDESTRIAN_SYSTEM
            if (PedestrianManager.Exists)
            {
                return PedestrianManager.Instance.IsInitialized();
            }
#endif
            return false;
        }


        /// <summary>
        /// Update the active camera that is used to remove pedestrians when they are not in view.
        /// </summary>
        /// <param name="activeCamera">Represents the camera or the player prefab.</param>
        public static void SetCamera(Transform activeCamera)
        {
            SetCameras(new Transform[] { activeCamera });
        }


        /// <summary>
        /// Update active cameras that is used to remove pedestrians when they are not in view.
        /// This is used in multiplayer/split screen setups.
        /// </summary>
        /// <param name="activeCameras">represents the cameras or the players from your game</param>
        public static void SetCameras(Transform[] activeCameras)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.UpdateCamera(activeCameras);
#endif
        }
        #endregion


        #region PathFinding
        /// <summary>
        /// Calculates a path from the current position of the pedestrian to a specified destination.
        /// The destination will be the closest waypoint to the destination point.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="position">The destination position.</param>
        public static void SetDestination(int pedestrianIndex, Vector3 position)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.SetDestination(pedestrianIndex, position);

#endif
        }


        /// <summary>
        /// Assigns a predefined path to a pedestrian.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="path">A list of path waypoint indexes.</param>
        public static void SetPath(int pedestrianIndex, List<int> path)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.WaypointManager?.SetPath(pedestrianIndex, path);
#endif
        }


        /// <summary>
        /// Returns a waypoint path between a start position and an end position for a specific pedestrian type.
        /// </summary>
        /// <param name="startPosition">A Vector3 for the initial position.</param>
        /// <param name="endPosition">A Vector3 for the final position.</param>
        /// <param name="pedestrianType">The pedestrian type for which this path is intended.</param>
        /// <returns>The waypoint indexes of the path between startPosition and endPosition.</returns>
        public static List<int> GetPath(Vector3 startPosition, Vector3 endPosition, PedestrianTypes pedestrianType)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            return PedestrianManager.Instance.PathFindingManager?.GetPath(startPosition, endPosition, pedestrianType);
#else
            return null;
#endif
        }
        #endregion


        #region WaypointManager
        /// <summary>
        /// Remove a predefined path for a pedestrian.
        /// </summary>
        /// <param name="pedestrianIndex"></param>
        public static void RemovePath(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.WaypointManager?.RemovePath(pedestrianIndex);
#endif
        }


        /// <summary>
        /// Returns the PedestrianWaypoint object for a given waypoint index.
        /// </summary>
        /// <param name="waypointIndex">The index of the waypoint.</param>
        /// <returns>The PedestrianWaypoint object at the index position inside the waypoint list.</returns>
        public static Internal.PedestrianWaypoint GetWaypointFromIndex(int waypointIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            return PedestrianManager.Instance.PedestrianWaypointsDataHandler?.GetWaypointWithValidation(waypointIndex);
#else
            return null;
#endif
        }


        /// <summary>
        /// Forbids a pedestrian to receive another waypoint. Useful for traffic lights.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="active">If true -> pedestrian will not receive another waypoint.</param>
        public static void DoNotChangeWaypoint(int pedestrianIndex, bool active)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.WaypointManager?.SetCannotPassProperty(pedestrianIndex, active);
#endif
        }


        /// <summary>
        /// Returns the position of the next waypoint of the pedestrian.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <returns>The position of the waypoint.</returns>
        public static Vector3 GetNextWaypointPosition(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            if (PedestrianManager.Instance.WaypointManager != null)
            {
                return PedestrianManager.Instance.WaypointManager.GetNextWaypointPositionFromPath(pedestrianIndex);
            }
#endif
            return default;
        }


        /// <summary>
        /// Get the waypoint indexes that are included in the pedestrian path.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <returns>A list of waypoint indexes.</returns>
        public static List<int> GetWaypointList(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            if (PedestrianManager.Instance.WaypointManager != null)
            {
                return PedestrianManager.Instance.WaypointManager.GetPathWaypoints(pedestrianIndex);
            }
#endif
            return default;
        }


        /// <summary>
        /// Get the number of waypoints included in the pedestrian path.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <returns>The number of waypoints from the path.</returns>
        public static int GetPathLength(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            if (PedestrianManager.Instance.WaypointManager != null)
            {
                return PedestrianManager.Instance.WaypointManager.GetPathLength(pedestrianIndex);
            }
#endif
            return 0;
        }


        /// <summary>
        /// Returns the index of the waypoint that the pedestrian just passed.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <returns>The index of the previous waypoint.</returns>
        public static int GetPreviousWaypointIndex(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            if (PedestrianManager.Instance.WaypointManager != null)
            {
                return PedestrianManager.Instance.WaypointManager.GetPreviousWaypointIndexFromPath(pedestrianIndex);
            }
#endif
            return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
        }


        /// <summary>
        /// Gets the index of the target waypoint of the pedestrian.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <returns>The index of the current waypoint.</returns>
        public static int GetCurrentWaypointIndex(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            if (PedestrianManager.Instance.WaypointManager != null)
            {
                return PedestrianManager.Instance.WaypointManager.GetCurrentWaypointIndexFromPath(pedestrianIndex);
            }
#endif
            return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
        }


        /// <summary>
        /// Verifies if a pedestrian has as a target a specific waypoint.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="waypointIndex">The index of the waypoint.</param>
        /// <returns>true - if the current target waypoint is equal to the waypoint index.</returns>
        public static bool IsPedestrianAtThisWaypoint(int pedestrianIndex, int waypointIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            if (PedestrianManager.Instance.WaypointManager != null)
            {
                return PedestrianManager.Instance.WaypointManager.IsPedestrianAtThisWaypoint(pedestrianIndex, waypointIndex);
            }
#endif
            return false;

        }


        /// <summary>
        /// Returns the position of the current waypoint of the pedestrian.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <returns>The position of the waypoint.</returns>
        public static Vector3 GetCurrentWaypointPosition(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            if (PedestrianManager.Instance.WaypointManager != null)
            {
                return PedestrianManager.Instance.WaypointManager.GetCurrentWaypointPositionFromPath(pedestrianIndex);
            }
#endif
            return default;
        }


        /// <summary>
        /// Insert a Vector3 position that has no waypoint associated as a target for a pedestrian.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="currentPathPosition">The path index to insert the new position.</param>
        /// <param name="positionToInsert">The position in global coordinates to insert.</param>
        /// <returns>A negative number representing the "waypointIndex" of the added position.</returns>
        public static int InsertPositionAsTarget(int pedestrianIndex, int currentPathPosition, Vector3 positionToInsert)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            if (PedestrianManager.Instance.WaypointManager != null)
            {
                return PedestrianManager.Instance.WaypointManager.InsertPositionAsTarget(pedestrianIndex, currentPathPosition, positionToInsert);
            }
#endif
            return default;
        }


        /// <summary>
        /// Removes a waypoint index from a pedestrian path.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="waypointIndex">The waypoint index to remove.</param>
        public static void RemoveWaypointFromPath(int pedestrianIndex, int waypointIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.WaypointManager?.RemoveWaypointFromPath(pedestrianIndex, waypointIndex);
#endif
        }
        #endregion


        #region Crossings
        /// <summary>
        /// Set the state of a Street Crossing Component.
        /// </summary>
        /// <param name="crossingName">The name of the crossing.</param>
        /// <param name="stop">The state of the component. True -> pedestrians will stop and wait.</param>
        public static void SetCrossingState(string crossingName, bool stop)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.AllCrossings?.SetStopWaypointState(crossingName, stop);
#endif
        }


        /// <summary>
        /// Get the current state of a Street Crossing Component.
        /// </summary>
        /// <param name="crossingName">The name of the crossing.</param>
        /// <returns>True -> pedestrians will stop and wait.</returns>
        public static bool GetCrossingState(string crossingName)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            return PedestrianManager.Instance.AllCrossings.GetstopWaypointState(crossingName);
#else
            return false;
#endif
        }
        #endregion


        #region DensityManager
        /// <summary>
        /// Adds a pedestrian to the nearest waypoint from the provided position.
        /// </summary>
        /// <param name="position">The position where to add a new pedestrian.</param>
        /// <param name="type">The type of the pedestrian to add.</param>
        /// <param name="completeMethod">Callback triggered after instantiation. It returns the PedestrianComponent and the waypoint index where the pedestrian was instantiated.</param>
        public static void AddPedestrian(Vector3 position, PedestrianTypes type, UnityAction<Pedestrian, int> completeMethod)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.DensityManager?.AddPedestrianAtPosition(position, type, completeMethod, default, PedestrianSystemConstants.INVALID_PEDESTRIAN_INDEX);
#endif
        }


        /// <summary>
        /// Adds the pedestrian with the specified index to the nearest waypoint from the provided position. 
        /// </summary>
        /// <param name="position">The position where to add a new pedestrian.</param>
        /// <param name="pedestrianIndex">The index of the pedestrian to be added.</param>
        /// <param name="completeMethod">Callback triggered after instantiation. It returns the PedestrianComponent and the waypoint index where the pedestrian was instantiated.</param>
        public static void AddSpecificPedestrian(Vector3 position, int pedestrianIndex, UnityAction<Pedestrian, int> completeMethod)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.DensityManager?.AddPedestrianAtPosition(position, default, completeMethod, default, pedestrianIndex);
#endif
        }


        /// <summary>
        /// Adds a pedestrian to the nearest waypoint from the provided position and automatically sets a destination for it.
        /// </summary>
        /// <param name="position">The position where to add a new pedestrian.</param>
        /// <param name="destinationPosition">The destination point.</param>
        /// <param name="type">The type of the pedestrian to add.</param>
        /// <param name="completeMethod">Callback triggered after instantiation. It returns the PedestrianComponent and the waypoint index where the pedestrian was instantiated.</param>
        public static void AddPedestrianWithDestination(Vector3 position, Vector3 destinationPosition, PedestrianTypes type, UnityAction<Pedestrian, int> completeMethod)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.AddPedestrianAtPosition(position, type, completeMethod, destinationPosition, PedestrianSystemConstants.INVALID_PEDESTRIAN_INDEX);
#endif
        }


        /// <summary>
        /// Adds the pedestrian with the specified index to the nearest waypoint from the provided position and automatically sets a destination for it. 
        /// </summary>
        /// <param name="position">The position where to add a new pedestrian.</param>
        /// <param name="destinationPosition">The destination point.</param>
        /// <param name="pedestrianIndex">The index of the pedestrian to be added.</param>
        /// <param name="completeMethod">Callback triggered after instantiation. It returns the PedestrianComponent and the waypoint index where the pedestrian was instantiated.</param>
        public static void AddSpecificPedestrianWithDestination(Vector3 position, Vector3 destinationPosition, int pedestrianIndex, UnityAction<Pedestrian, int> completeMethod)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.AddPedestrianAtPosition(position, default, completeMethod, destinationPosition, pedestrianIndex);
#endif
        }


        /// <summary>
        /// Set the deviation between the waypoint that the pedestrian is using. 
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="offset">[-1,1] -1 left side of the sidewalk, 1- right side of the sidewalk</param>
        public static void SetPedestrianOffset(int pedestrianIndex, float offset)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.DensityManager?.UpdateOffset(pedestrianIndex, offset);
#endif
        }


        /// <summary>
        /// Removes a specific pedestrian from the scene.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        public static void RemovePedestrian(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.DensityManager?.RemovePedestrian(pedestrianIndex);
#endif
        }


        /// <summary>
        /// Modify max number of active pedestrians.
        /// </summary>
        /// <param name="nrOfPedestrians">The new number of active pedestrians.</param>
        public static void SetDensity(int nrOfPedestrians)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.DensityManager.SetPedestrianDensity(nrOfPedestrians);
#endif
        }
        #endregion


        #region PedestrianAI
        /// <summary>
        /// Force a waypoint request. This will make the pedestrian skip the current target.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        public static void RequestNewWaypoint(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.PedestrianAI?.RequestWaypoint(pedestrianIndex, false);
#endif
        }
        #endregion


        #region AllPedestrians
        /// <summary>
        /// Adds a previously excluded pedestrian back to the system.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        public static void AddExcludedPedestrianToSystem(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.AllPedestrians?.AddExcludedPedestrianToSystem(pedestrianIndex);
#endif
        }


        /// <summary>
        /// An excluded pedestrian will never be instantiated by the system. Can be instantiated using API.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        public static void ExcludePedestrianFromSystem(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.AllPedestrians?.ExcludePedestrianFromSystem(pedestrianIndex);
#endif
        }


        /// <summary>
        /// Returns a list of all excluded pedestrians.
        /// </summary>
        /// <returns>A list of Pedestrian components associated with excluded pedestrians.</returns>
        public static List<Pedestrian> GetExcludedPedestrianList()
        {
#if GLEY_PEDESTRIAN_SYSTEM
            return PedestrianManager.Instance.AllPedestrians?.GetExcludedPedestrianList();
#else
            return default;
#endif
        }


        /// <summary>
        /// Change the complete behaviour list for a pedestrian.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="pedestrianBehaviours">An ordered list of the new behaviours.</param>
        public static void SetPedestrianBehaviours(int pedestrianIndex, List<PedestrianBehaviour> pedestrianBehaviours)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.AllPedestrians?.SetPedestrianBehaviours(pedestrianIndex, pedestrianBehaviours);
#endif
        }


        /// <summary>
        /// Change the complete behaviour list for all pedestrians.
        /// </summary>
        /// <param name="pedestrianBehaviours">An object the implements IBehaviourList.</param>
        public static void SetAllPedestriansBehaviours(IBehaviourList pedestrianBehaviours)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.AllPedestrians?.SetAllPedestriansBehaviours(pedestrianBehaviours);
#endif
        }


        /// <summary>
        /// Start a specific behaviour.
        /// </summary>
        /// <typeparam name="T">The behaviour to start. An implementation of PedestrianBehaviour abstract class.</typeparam>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        public static void StartBehavior<T>(int pedestrianIndex) where T : PedestrianBehaviour
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.AllPedestrians?.StartBehavior(pedestrianIndex, typeof(T).Name);
#endif
        }


        /// <summary>
        /// Stop a specific behaviour.
        /// </summary>
        /// <typeparam name="T">The behaviour to stop. An implementation of PedestrianBehaviour abstract class.</typeparam>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        public static void StopBehaviour<T>(int pedestrianIndex) where T : PedestrianBehaviour
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.AllPedestrians?.StopBehaviour(pedestrianIndex, typeof(T).Name);
#endif
        }


        /// <summary>
        /// Get a pedestrian behaviour.
        /// </summary>
        /// <typeparam name="T">The behaviour type. An implementation of PedestrianBehaviour abstract class.</typeparam>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <returns>The PedestrianBehaviour instance.</returns>
        public static PedestrianBehaviour GetPedestrianBehaviourOfType<T>(int pedestrianIndex) where T : PedestrianBehaviour
        {
#if GLEY_PEDESTRIAN_SYSTEM
            return PedestrianManager.Instance.AllPedestrians?.GetPedestrianBehaviour(pedestrianIndex, typeof(T).Name);
#else
            return null;
#endif
        }


        /// <summary>
        /// Get the current running behaviour of a pedestrian.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <returns>The PedestrianBehaviour instance.</returns>
        public static PedestrianBehaviour GetCurrentBehaviour(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            return PedestrianManager.Instance.AllPedestrians?.GetCurrentBehaviour(pedestrianIndex);
#else
            return null;
#endif
        }


        /// <summary>
        /// Convert the pedestrian index to the pedestrian script.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <returns>The script associated with the pedestrian GameObject.</returns>
        public static Pedestrian GetPedestrian(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            if (PedestrianManager.Instance.AllPedestrians != null)
            {
                return PedestrianManager.Instance.AllPedestrians?.GetPedestrianWithValidation(pedestrianIndex);
            }
#endif
            return null;
        }


        /// <summary>
        /// Remove the collider from the pedestrian obstacles list.
        /// </summary>
        /// <param name="collider">The removed collider.</param>
        public static void TriggerColliderRemovedEvent(Collider collider)
        {
#if GLEY_TRAFFIC_SYSTEM
            TriggerColliderRemovedEvent(new Collider[] { collider });
#endif
        }


        /// <summary>
        ///  Remove the colliders from the pedestrian obstacles list.
        /// </summary>
        /// <param name="colliders">The removed colliders.</param>
        public static void TriggerColliderRemovedEvent(Collider[] colliders)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.AllPedestrians?.TriggerColliderRemovedEvent(colliders);
#endif
        }

        internal static UrbanSystem.Internal.GridData GetGridDataHandler()
        {
#if GLEY_PEDESTRIAN_SYSTEM
            return PedestrianManager.Instance.GridData;
#else
            return null;
#endif
        }
        #endregion


        /// <summary>
        /// Disable all waypoints on the specified area to stop the pedestrians to go in a certain area for a limited amount of time.
        /// </summary>
        /// <param name="center">The center of the circle to disable waypoints from.</param>
        /// <param name="radius">The radius in meters of the circle to disable waypoints from.</param>
        public static void DisableAreaWaypoints(Vector3 center, float radius)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.WaypointActivator?.DisableAreaWaypoints(new Area(center, radius));
#endif
        }


        /// <summary>
        /// Enable all disabled area waypoints.
        /// </summary>
        public static void EnableAllWaypoints()
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianManager.Instance.WaypointActivator?.EnableAllWaypoints();
#endif
        }


        /// <summary>
        /// Check if the current waypoint of the pedestrian has the stop property set to true.
        /// </summary>
        /// <param name="pedestrianIndex">he index of the pedestrian.</param>
        /// <returns>True if the pedestrian is currently at a stop waypoint.</returns>
        public static bool IsCurrentWaypointAStopWaypoint(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            if (PedestrianManager.Instance.WaypointManager != null)
            {
                return PedestrianManager.Instance.WaypointManager.IsCurrentWaypointAStopWaypoint(pedestrianIndex);
            }
#endif
            return false;
        }
    }
}
