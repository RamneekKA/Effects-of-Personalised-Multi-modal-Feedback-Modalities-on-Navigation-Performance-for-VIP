using UnityEngine;

namespace Gley.PedestrianSystem
{
    public class Events
    {
        /// <summary>
        /// Triggered every time a pedestrian is activated inside the scene.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="position">The instantiation position.</param>
        public delegate void PedestrianAdded(int pedestrianIndex, Vector3 position);
        public static PedestrianAdded OnPedestrianAdded;
        public static void TriggerPedestrianAddedEvent(int pedestrianIndex, Vector3 position)
        {
            OnPedestrianAdded?.Invoke(pedestrianIndex, position);
        }


        /// <summary>
        /// Triggered every time a pedestrian is removed from the scene.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        public delegate void PedestrianRemoved(int pedestrianIndex);
        public static event PedestrianRemoved OnPedestrianRemoved;
        public static void TriggerPedestrianRemovedEvent(int pedestrianIndex)
        {
            OnPedestrianRemoved?.Invoke(pedestrianIndex);
        }


        /// <summary>
        /// Triggered every time a pedestrian changes a waypoint.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="newDestination">New destination.</param>
        public delegate void DestinationChanged(int pedestrianIndex, Vector3 newDestination);
        public static DestinationChanged OnDestinationChanged;
        public static void TriggerDestinationChangedEvent(int pedestrianIndex, Vector3 newDestination)
        {
            OnDestinationChanged?.Invoke(pedestrianIndex, newDestination);
        }


        /// <summary>
        /// Triggered every time a pedestrian that had a custom path reached destination.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        public delegate void DestinationReached(int pedestrianIndex);
        public static DestinationReached OnDestinationReached;
        public static void TriggerDestinationReachedEvent(int pedestrianIndex)
        {
            OnDestinationReached?.Invoke(pedestrianIndex);
        }


        /// <summary>
        /// Triggered every time a pedestrian is ready to cross the street at an intersection.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="intersection">The intersection component.</param>
        /// <param name="waypointIndex">The current waypoint of the pedestrian.</param>
        public delegate void StreetCrossing(int pedestrianIndex, UrbanSystem.Internal.IIntersection intersection, int waypointIndex);
        public static StreetCrossing OnStreetCrossing;
        public static void TriggerStreetCrossingEvent(int pedestrianIndex, UrbanSystem.Internal.IIntersection intersection, int waypointIndex)
        {
            OnStreetCrossing?.Invoke(pedestrianIndex, intersection, waypointIndex);
        }


        /// <summary>
        /// Triggered every time a pedestrian collides with something.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="collision">The collision object.</param>
        /// <param name="obstacleType">The type of the obstacle(Player, DynamicObject, StaticObject, Pedestrian)</param>
        public delegate void CollisionEnter(int pedestrianIndex, Collision collision, ObstacleTypes obstacleType);
        public static event CollisionEnter OnCollisionEnter;
        public static void TriggerCollisionEnterEvent(int pedestrianIndex, Collision collision, ObstacleTypes obstacleType)
        {
            OnCollisionEnter?.Invoke(pedestrianIndex, collision, obstacleType);
        }


        /// <summary>
        /// Triggered every time a pedestrian detects an object with the front trigger.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="other">The other collider.</param>
        /// <param name="obstacleType">The type of the obstacle(Player, DynamicObject, StaticObject, Pedestrian)</param>
        public delegate void ObjectTriggerEnter(int pedestrianIndex, Collider other, ObstacleTypes obstacleType);
        public static event ObjectTriggerEnter OnObjectTriggerEnter;
        public static void TriggerObjectTriggerEnterEvent(int pedestrianIndex, Collider other, ObstacleTypes obstacleType)
        {
            OnObjectTriggerEnter?.Invoke(pedestrianIndex, other, obstacleType);
        }


        /// <summary>
        /// Triggered every time a pedestrian detects another pedestrian with the front trigger.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian</param>
        /// <param name="other">The other collider.</param>
        public delegate void PedestrianTriggerEnter(int pedestrianIndex, Pedestrian other);
        public static event PedestrianTriggerEnter OnPedestrianTriggerEnter;
        public static void TriggerPedestrianTriggerEnterEvent(int pedestrianIndex, Pedestrian other)
        {
            OnPedestrianTriggerEnter?.Invoke(pedestrianIndex, other);
        }


        /// <summary>
        /// Trigger constantly when an object is inside the pedestrian's front trigger.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="other">The other collider.</param>
        /// <param name="obstacleType">The type of the obstacle(Player, DynamicObject, StaticObject, Pedestrian)</param>
        public delegate void ObjectTriggerStay(int pedestrianIndex, Collider other, ObstacleTypes obstacleType);
        public static event ObjectTriggerStay OnObjectTriggerStay;
        public static void TriggerObjectTriggerStayEvent(int pedestrianIndex, Collider other, ObstacleTypes obstacleType)
        {
            OnObjectTriggerStay?.Invoke(pedestrianIndex, other, obstacleType);
        }


        /// <summary>
        /// Trigger constantly when another pedestrian is inside the pedestrian's front trigger.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="otherPedestrian">The other pedestrian.</param>
        /// <param name="collider">The other collider.</param>
        public delegate void PedestrianTriggerStay(int pedestrianIndex, Pedestrian otherPedestrian, Collider collider);
        public static event PedestrianTriggerStay OnPedestrianTriggerStay;
        public static void TriggerPedestrianTriggerStayEvent(int pedestrianIndex, Pedestrian other, Collider collider)
        {
            OnPedestrianTriggerStay?.Invoke(pedestrianIndex, other, collider);
        }


        /// <summary>
        /// Trigger every time an object exists the front trigger.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        public delegate void ObjectTriggerExit(int pedestrianIndex);
        public static event ObjectTriggerExit OnObjectTriggerExit;
        public static void TriggerObjectTriggerExitEvent(int pedestrianIndex)
        {
            OnObjectTriggerExit?.Invoke(pedestrianIndex);
        }


        /// <summary>
        /// Triggered every time a pedestrian exists the front trigger.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="otherPedestrian">The pedestrian component of the other pedestrian.</param>
        public delegate void PedestrianTriggerExit(int pedestrianIndex, Pedestrian otherPedestrian);
        public static event PedestrianTriggerExit OnPedestrianTriggerExit;
        public static void TriggerPedestrianTriggerExitEvent(int pedestrianIndex, Pedestrian other)
        {
            OnPedestrianTriggerExit?.Invoke(pedestrianIndex, other);
        }


        /// <summary>
        /// Triggered every time the stop property of a waypoint changed.
        /// </summary>
        /// <param name="waypointIndex">The index of the waypoint that changed.</param>
        /// <param name="stop">The new stop state.</param>
        public delegate void StopStateChanged(int waypointIndex, bool stop);
        public static event StopStateChanged OnStopStateChanged;
        public static void TriggerStopStateChangedEvent(int waypointIndex, bool stop)
        {
            OnStopStateChanged?.Invoke(waypointIndex, stop);
        }


        /// <summary>
        /// Triggered every time a pedestrian behaviour changes.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="newBehaviour">The new behaviour.</param>
        public delegate void BehaviourChanged(int pedestrianIndex, PedestrianBehaviour newBehaviour);
        public static event BehaviourChanged OnBehaviourChanged;
        public static void TriggerBehaviourChangedEvent(int pedestrianIndex, PedestrianBehaviour newBehaviour)
        {
            OnBehaviourChanged?.Invoke(pedestrianIndex, newBehaviour);
        }

        /// <summary>
        /// Triggered every time a pedestrian behaviour changes.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian.</param>
        /// <param name="newBehaviour">The new behaviour.</param>
        public delegate void WaypointReached(int pedestrianIndex, int waypointIndex, string data);
        public static event WaypointReached OnWaypointReached;
        public static void TriggerWaypointReachedEvent(int pedestrianIndex, int waypointIndex, string data)
        {
            OnWaypointReached?.Invoke(pedestrianIndex, waypointIndex, data);
        }
    }
}
