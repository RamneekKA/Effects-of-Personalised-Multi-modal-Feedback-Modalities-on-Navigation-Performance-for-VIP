using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Stores all runtime pedestrian waypoints.
    /// </summary>
    public class PedestrianWaypointsData : MonoBehaviour
    {
        [SerializeField] private PedestrianWaypoint[] _allPedestrianWaypoints;

        public PedestrianWaypoint[] AllPedestrianWaypoints => _allPedestrianWaypoints;


        public void SetPedestrianWaypoints(PedestrianWaypoint[] waypoints)
        {
            _allPedestrianWaypoints = waypoints;
        }


        internal bool IsValid(out string error)
        {
            error = string.Empty;
            if (_allPedestrianWaypoints == null)
            {
                error = PedestrianSystemErrors.NullWaypointData;
                return false;
            }

            if (_allPedestrianWaypoints.Length <= 0)
            {
                error = PedestrianSystemErrors.NoWaypointsFound;
                return false;
            }
            return true;
        }
    }
}