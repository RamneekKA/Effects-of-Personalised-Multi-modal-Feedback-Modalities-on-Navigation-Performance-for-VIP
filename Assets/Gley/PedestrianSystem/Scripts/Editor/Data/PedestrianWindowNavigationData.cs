using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    /// <summary>
    /// Used to communicate between settings windows.
    /// </summary>
    internal class PedestrianWindowNavigationData
    {
        private PedestrianPath _selectedRoad;
        private PedestrianWaypointSettings _selectedWaypoint;
        private LayerMask _groundLayers;


        internal PedestrianPath GetSelectedRoad()
        {
            return _selectedRoad;
        }


        internal void SetSelectedRoad(PedestrianPath road)
        {
            _selectedRoad = road;
        }


        internal PedestrianWaypointSettings GetSelectedWaypoint()
        {
            return _selectedWaypoint;
        }


        internal void SetSelectedWaypoint(PedestrianWaypointSettings waypoint)
        {
            _selectedWaypoint = waypoint;
        }


        internal void InitializeData()
        {
            UpdateLayers();
            _selectedRoad = null;
        }


        internal void UpdateLayers()
        {
            var layers = FileCreator.LoadScriptableObject<LayerSetup>(PedestrianSystemConstants.LayerPath);
            if (layers != null)
            {               
                _groundLayers = layers.GroundLayers;
            }
        }


        internal LayerMask GetGroundLayers()
        {
            return _groundLayers;
        }
    }
}
