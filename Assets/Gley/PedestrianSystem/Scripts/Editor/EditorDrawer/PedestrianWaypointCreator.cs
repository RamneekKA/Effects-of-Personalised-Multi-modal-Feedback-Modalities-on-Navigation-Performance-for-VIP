using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    internal class PedestrianWaypointCreator
    {
        internal Transform CreateWaypoint(Transform parent, Vector3 waypointPosition, string name, List<int> allowedCars, float laneWidth)
        {
            GameObject go = MonoBehaviourUtilities.CreateGameObject(name, parent, waypointPosition, true);
            PedestrianWaypointSettings waypointScript = go.AddComponent<PedestrianWaypointSettings>();
            waypointScript.Initialize();
            waypointScript.AllowedPedestrians = allowedCars.Cast<PedestrianTypes>().ToList();
            waypointScript.LaneWidth = laneWidth;
            return go.transform;
        }
    }
}
