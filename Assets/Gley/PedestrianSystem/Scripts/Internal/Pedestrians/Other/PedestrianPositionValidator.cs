using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Used to check if a pedestrian can be instantiated in a given position
    /// </summary>
    internal class PedestrianPositionValidator
    {
        private Collider[] _results;
        private Transform[] _activeCameras;
        private LayerMask _trafficLayer;
        private LayerMask _playerLayer;
        private LayerMask _buildingsLayers;
        private float _minDistanceToAdd;
        private bool _debugDensity;


        /// <summary>
        /// Setup dependencies
        /// </summary>
        internal PedestrianPositionValidator(Transform[] activeCameras, LayerMask trafficLayer, LayerMask playerLayer, LayerMask buildingsLayers, float minDistanceToAdd, bool debugDensity)
        {
            UpdateCamera(activeCameras);
            _trafficLayer = trafficLayer;
            _playerLayer = playerLayer;
            _minDistanceToAdd = minDistanceToAdd * minDistanceToAdd;
            _buildingsLayers = buildingsLayers;
            _debugDensity = debugDensity;
        }


        /// <summary>
        /// Checks if a pedestrian can be instantiated in a given position
        /// </summary>
        internal bool IsValid(Vector3 position, bool ignoreLineOfSight)
        {
            for (int i = 0; i < _activeCameras.Length; i++)
            {
                if (!ignoreLineOfSight)
                {
                    // If position if far enough from the player.
                    if (Vector3.SqrMagnitude(_activeCameras[i].position - position) < _minDistanceToAdd)
                    {
                        if (!Physics.Linecast(position, _activeCameras[i].position, _buildingsLayers))
                        {
#if UNITY_EDITOR
                            if (_debugDensity)
                            {
                                Debug.Log("Density: Direct view of the camera");
                                Debug.DrawLine(_activeCameras[i].position, position, Color.red, 0.1f);
                            }
#endif
                            return false;
                        }
                        else
                        {
#if UNITY_EDITOR
                            if (_debugDensity)
                            {
                                Debug.DrawLine(_activeCameras[i].position, position, Color.green, 0.1f);
                            }
#endif
                        }
                    }
                }
            }
            // Check if the final position is free. 
            return IsPositionFree(position, 1, 1, 1);
        }


        /// <summary>
        /// Check if a given position if free
        /// </summary>
        internal bool IsPositionFree(Vector3 position, float length, float height, float width)
        {
            if (Physics.CheckBox(position, new Vector3(width / 2, height / 2, length / 2), Quaternion.identity, _trafficLayer | _playerLayer))
            {
#if UNITY_EDITOR
                if (_debugDensity)
                {
                    Debug.Log("Density: Other obstacle is blocking the waypoint");
                }
#endif
                return false;
            }
            return true;
        }


        /// <summary>
        /// Update player camera transform.
        /// </summary>
        internal void UpdateCamera(Transform[] activeCameras)
        {
            _activeCameras = activeCameras;
        }
    }
}
