using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    public static class WaypointExtensionMethods
    {
        public static PedestrianWaypoint ToPlayWaypoint(this PedestrianWaypointSettings editorWaypoint, PedestrianWaypointSettings[] allWaypoints)
        {
            return new PedestrianWaypoint(editorWaypoint.name,
                editorWaypoint.ToListIndex(allWaypoints),
                editorWaypoint.transform.position,
                editorWaypoint.neighbors.ToListIndex(allWaypoints),
                editorWaypoint.prev.ToListIndex(allWaypoints),
                editorWaypoint.AllowedPedestrians,
                editorWaypoint.Crossing,
                editorWaypoint.LaneWidth,
                editorWaypoint.Left,
                editorWaypoint.eventData,
                editorWaypoint.triggerEvent);
        }


        public static PedestrianWaypoint[] ToPlayWaypoints(this PedestrianWaypointSettings[] editorWaypoints, PedestrianWaypointSettings[] allWaypoints)
        {
            PedestrianWaypoint[] result = new PedestrianWaypoint[editorWaypoints.Length];
            for (int i = 0; i < editorWaypoints.Length; i++)
            {
                result[i] = editorWaypoints[i].ToPlayWaypoint(allWaypoints);
            }
            return result;
        }


        public static int[] ToListIndex(this List<PedestrianWaypointSettings> editorWaypoints, PedestrianWaypointSettings[] allWaypoints)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < editorWaypoints.Count; i++)
            {
                bool found=false; 
                for(int j=0;j<allWaypoints.Length;j++)
                {
                    if (editorWaypoints[i] == allWaypoints[j])
                    {
                        found=true;
                        result.Add(j);
                        break;
                    }
                }
                if(!found)
                {
                    Debug.LogError($"{editorWaypoints[i].name} not found in allWaypoints", editorWaypoints[i]);
                }
            }
            return result.ToArray();
        }


        public static int[] GetListIndex(PedestrianWaypointSettings[] editorWaypoints, PedestrianWaypointSettings[] allWaypoints)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < editorWaypoints.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < allWaypoints.Length; j++)
                {
                    if (editorWaypoints[i] == allWaypoints[j])
                    {
                        found = true;
                        result.Add(j);
                        break;
                    }
                }
                if (!found)
                {
                    Debug.LogError($"{editorWaypoints[i].name} not found in allWaypoints", editorWaypoints[i]);
                }
            }
            return result.ToArray();
        }


        public static int[] ToListIndex(this List<WaypointSettingsBase> editorWaypoints, PedestrianWaypointSettings[] allWaypoints)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < editorWaypoints.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < allWaypoints.Length; j++)
                {
                    if (editorWaypoints[i] == allWaypoints[j])
                    {
                        found = true;
                        result.Add(j);
                        break;
                    }
                }
                if (!found)
                {
                    Debug.LogError($"{editorWaypoints[i].name} not found in allWaypoints", editorWaypoints[i]);
                }
            }
            return result.ToArray();
        }


        public static int ToListIndex(this PedestrianWaypointSettings editorWaypoint, PedestrianWaypointSettings[] allWaypoints)
        {
            for (int i = 0; i < allWaypoints.Length;i++)
            {
                if(editorWaypoint == allWaypoints[i])
                {
                    return i;
                }
            }
            Debug.LogError($"{editorWaypoint.name} not found in allWaypoints", editorWaypoint);
            return PedestrianSystemConstants.INVALID_WAYPOINT_INDEX;
        }
    }
}