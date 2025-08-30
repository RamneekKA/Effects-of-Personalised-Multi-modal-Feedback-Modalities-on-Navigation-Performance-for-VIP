using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    internal class PedestrianConnectionEditorData : EditorData
    {
        private readonly PedestrianPathEditorData _roadData;

        private PedestrianConnectionPool[] _allConnectionPools;
        private PedestrianConnectionCurve[] _allConnections;
        private Dictionary<PedestrianConnectionCurve, PedestrianWaypointSettings[]> _connectionWaypoints;
        private Dictionary<PedestrianConnectionCurve, PedestrianConnectionPool> _pools;


        internal PedestrianConnectionEditorData (PedestrianPathEditorData data)
        {
            _roadData = data;
            LoadAllData();
        }


        internal PedestrianConnectionCurve[] GetAllConnections()
        {
            return _allConnections;
        }


        internal PedestrianConnectionPool[] GetAllConnectionPools()
        {
            return _allConnectionPools;
        }


        internal PedestrianWaypointSettings[] GetWaypoints(PedestrianConnectionCurve connection)
        {
            _connectionWaypoints.TryGetValue(connection, out var waypoints);
            return waypoints;
        }


        internal PedestrianConnectionPool GetConnectionPool(PedestrianConnectionCurve connection)
        {
            _pools.TryGetValue(connection, out var pool);
            return pool;
        }


        protected override void LoadAllData()
        {
            var tempConnections = new List<PedestrianConnectionCurve>();
            var connectionPools = new List<PedestrianConnectionPool>();
            _connectionWaypoints = new Dictionary<PedestrianConnectionCurve, PedestrianWaypointSettings[]>();
            _pools = new Dictionary<PedestrianConnectionCurve, PedestrianConnectionPool>();
            var allRoads = _roadData.GetAllRoads();
            for (int i = 0; i < allRoads.Length; i++)
            {
                if (allRoads[i].isInsidePrefab && !GleyPrefabUtilities.EditingInsidePrefab())
                {
                    continue;
                }
                PedestrianConnectionPool connectionsScript = allRoads[i].transform.parent.GetComponent<PedestrianConnectionPool>();
                if (connectionsScript == null)
                {
                    Debug.Log(allRoads[i].name, allRoads[i].transform.parent);
                    continue;
                }
                if (!connectionPools.Contains(connectionsScript))
                {
                    connectionPools.Add(connectionsScript);
                }
            }

            //verify
            for (int i = 0; i < connectionPools.Count; i++)
            {
                connectionPools[i].VerifyAssignments();
                var connectionCurves = connectionPools[i].GetAllConnections();
                for (int j = connectionCurves.Count - 1; j >= 0; j--)
                {
                    if (connectionCurves[j].VerifyAssignments() == false)
                    {
                        if (connectionCurves[j].Holder)
                        {
                            GleyPrefabUtilities.DestroyImmediate(connectionCurves[j].Holder.gameObject);
                        }
                        connectionCurves.RemoveAt(j);
                    }
                    else
                    {
                        connectionCurves[j].InPosition = connectionCurves[j].FromWaypoint.transform.position;
                        connectionCurves[j].OutPosition = connectionCurves[j].ToWaypoint.transform.position;
                       
                        //add waypoints
                        var waypoints = new List<PedestrianWaypointSettings>();
                        waypoints.Add(connectionCurves[j].FromWaypoint);
                        Transform waypointsHolder = connectionCurves[j].Holder;
                        for (int k = 0; k < waypointsHolder.childCount; k++)
                        {
                            var waypointScript = waypointsHolder.GetChild(k).GetComponent<PedestrianWaypointSettings>();
                            if (waypointScript != null)
                            {
                                waypointScript.VerifyAssignments(false);
                                waypointScript.position = waypointScript.transform.position;
                                waypoints.Add(waypointScript);
                            }
                        }
                        waypoints.Add(connectionCurves[j].ToWaypoint);
                        tempConnections.Add(connectionCurves[j]);
                        _connectionWaypoints.Add(connectionCurves[j], waypoints.ToArray());
                        _pools.Add(connectionCurves[j], connectionPools[i]);
                    }
                }
            }

            _allConnectionPools = connectionPools.ToArray();
            _allConnections = tempConnections.ToArray();
        }
    }
}
