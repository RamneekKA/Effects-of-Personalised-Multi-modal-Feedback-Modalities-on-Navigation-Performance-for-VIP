using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    public class WaypointActivator
    {
        private List<int> _disabledWaypoints;
        private GridData _gridData;
        private PedestrianWaypointsDataHandler _pedestrianWaypointsDataHandler;


        internal WaypointActivator(Area disableWaypointsArea, PedestrianWaypointsDataHandler pedestrianWaypointsDataHandler, GridData gridData)
        {
            _gridData = gridData;
            _pedestrianWaypointsDataHandler = pedestrianWaypointsDataHandler;
            _disabledWaypoints = new List<int>();

            if (disableWaypointsArea != default && disableWaypointsArea.Radius > 0)
            {
                DisableAreaWaypoints(new Area(disableWaypointsArea));
            }
        }


        /// <summary>
        /// Makes waypoints on a given radius unavailable.
        /// </summary>
        internal void DisableAreaWaypoints(Area area)
        {
            List<int> waypoints = _gridData.GetPedestrianWaypointsInArea(area);

            for (int j = 0; j < waypoints.Count; j++)
            {
                Debug.Log(Vector3.SqrMagnitude(area.Center - _pedestrianWaypointsDataHandler.GetPosition(waypoints[j])) + " " + area.SqrRadius);
                if (Vector3.SqrMagnitude(area.Center - _pedestrianWaypointsDataHandler.GetPosition(waypoints[j])) < area.SqrRadius)
                {
                    AddDisabledWaypoint(waypoints[j]);
                }
            }
        }


        internal List<int> GetDisabledWaypoints()
        {
            return _disabledWaypoints;
        }


        /// <summary>
        /// Mark a waypoint as disabled.
        /// </summary>
        private void AddDisabledWaypoint(int waypointIndex)
        {
            _disabledWaypoints.Add(waypointIndex);
            _pedestrianWaypointsDataHandler.SetTemperaryDisabledValue(waypointIndex, true);
        }


        /// <summary>
        /// Enables unavailable waypoints
        /// </summary>
        internal void EnableAllWaypoints()
        {
            _pedestrianWaypointsDataHandler.SetTemperaryDisabledValue(_disabledWaypoints, false);
            _disabledWaypoints = new List<int>();
        }
    }
}
