using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    internal class PedestrianConnectionCreator
    {
        private readonly PedestrianConnectionEditorData _connectionData;
        private readonly PedestrianWaypointCreator _waypointCreator;


        internal PedestrianConnectionCreator(PedestrianConnectionEditorData connectionData, PedestrianWaypointCreator waypointCreator)
        {
            _connectionData = connectionData;
            _waypointCreator = waypointCreator;
        }


        internal void MakePathConnection(PedestrianConnectionPool connectionPool, PedestrianWaypointSettings start, PedestrianWaypointSettings end, float waypointDistance, float laneWidth)
        {
            var offset = Vector3.zero;
            if (!GleyPrefabUtilities.EditingInsidePrefab())
            {
                if (GleyPrefabUtilities.IsInsidePrefab(start.gameObject) && GleyPrefabUtilities.GetInstancePrefabRoot(start.gameObject) == GleyPrefabUtilities.GetInstancePrefabRoot(end.gameObject))
                {
                    //connectionPool = GetConnectionPool();
                    //offset = fromRoad.positionOffset;
                }
                else
                {
                    //connectionPool = GetConnectionPool();
                    //offset = fromRoad.positionOffset;
                }
            }
            Path newConnection = new Path(start.transform.position - offset, end.transform.position - offset);
            var fromRoad = start.GetComponentInParent<PedestrianPath>();
            var toRoad = end.GetComponentInParent<PedestrianPath>();
            var name = fromRoad.name + "_" + start.name.ToString().Split('-').Last() + "->" + toRoad.name + "_" + end.name.ToString().Split('-').Last();
            var parent = MonoBehaviourUtilities.GetOrCreateGameObject(PedestrianSystemConstants.EditorConnectionsHolder, true).transform;
            GameObject connectorsHolder = MonoBehaviourUtilities.CreateGameObject(name, parent, parent.position, true);
            var curve = new PedestrianConnectionCurve(newConnection, fromRoad, toRoad, connectorsHolder.transform, start, end);

            connectionPool.AddConnection(curve);
            GenerateConnectorWaypoints(curve, waypointDistance, laneWidth);

            _connectionData.TriggerOnModifiedEvent();

            EditorUtility.SetDirty(connectionPool);
            AssetDatabase.SaveAssets();
        }


        internal void DeleteConnection(PedestrianConnectionCurve connectingCurve)
        {
            RemoveConnectionHolder(connectingCurve.Holder);
            var connectionPools = _connectionData.GetAllConnectionPools();
            for (int i = 0; i < connectionPools.Length; i++)
            {
                if (connectionPools[i].ContainsConnection(connectingCurve))
                {
                    connectionPools[i].RemoveConnection(connectingCurve);
                    EditorUtility.SetDirty(connectionPools[i]);
                }
            }
            _connectionData.TriggerOnModifiedEvent();
            AssetDatabase.SaveAssets();
        }


        internal void DeleteConnectionsWithThisRoad(PedestrianPath road)
        {
            var connections = _connectionData.GetAllConnections();
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i].ContainsRoad(road))
                {
                    DeleteConnection(connections[i]);
                }
            }
        }


        internal void GenerateSelectedConnections(List<PedestrianConnectionCurve> connectionCurves, float waypointDistance, float laneWidth)
        {
            for (int i = 0; i < connectionCurves.Count; i++)
            {
                if (connectionCurves[i].Draw)
                {
                    GenerateConnectorWaypoints(connectionCurves[i], waypointDistance, laneWidth);
                }
            }
            _connectionData.TriggerOnModifiedEvent();
        }


        private void GenerateConnectorWaypoints(PedestrianConnectionCurve connection, float waypointDistance, float laneWidth)
        {
            RemoveConnectionWaipoints(connection.Holder);
            AddLaneConnectionWaypoints(connection, waypointDistance, laneWidth);
            SetPerpendicularDirection(connection.Holder);
            AssetDatabase.SaveAssets();
        }


        private void SetPerpendicularDirection(Transform holder)
        {
            if (holder)
            {
                for (int i = 0; i < holder.childCount; i++)
                {
                    var waypoint = holder.GetChild(i).GetComponent<PedestrianWaypointSettings>();
                    Vector3 forward = Vector3.zero;
                    foreach (var neighbor in waypoint.neighbors)
                    {
                        if (waypoint != null)
                        {
                            forward += neighbor.transform.position - waypoint.transform.position;
                        }
                    }
                    foreach (var prev in waypoint.prev)
                    {
                        forward += waypoint.transform.position - prev.transform.position;
                    }
                    forward.Normalize();

                    waypoint.Left = Vector3.Cross(Vector3.up, forward).normalized;
                }
            }
        }


        private void AddLaneConnectionWaypoints(PedestrianConnectionCurve connection, float waypointDistance, float laneWidth)
        {
            var roadName = connection.GetOriginRoad().name;
            List<int> allowedCars = connection.FromWaypoint.AllowedPedestrians.Cast<int>().ToList();

            var curve = connection.Curve;
            Vector3[] p = curve.GetPointsInSegment(0, connection.GetOffset());
            var estimatedCurveLength = Vector3.Distance(p[0], p[3]);
            var nrOfWaypoints = estimatedCurveLength / waypointDistance;
            if (nrOfWaypoints < 1.5f)
            {
                nrOfWaypoints = 1.5f;
            }
            float step = 1 / nrOfWaypoints;
            float t = 0;
            int nr = 0;
            var connectorWaypoints = new List<Transform>();
            while (t < 1)
            {
                t += step;
                if (t < 1)
                {
                    var waypointName = roadName + "-" + UrbanSystem.Internal.UrbanSystemConstants.LaneNamePrefix + 0 + "-" + UrbanSystem.Internal.UrbanSystemConstants.ConnectionWaypointName + (++nr);
                    connectorWaypoints.Add(_waypointCreator.CreateWaypoint(connection.Holder, BezierCurve.CalculateCubicBezierPoint(t, p[0], p[1], p[2], p[3]), waypointName, allowedCars, laneWidth));
                }
            }

            WaypointSettingsBase currentWaypoint;
            WaypointSettingsBase connectedWaypoint;

            //set names
            connectorWaypoints[0].name = roadName + "-" + Gley.UrbanSystem.Internal.UrbanSystemConstants.LaneNamePrefix + 0 + "-" + Gley.UrbanSystem.Internal.UrbanSystemConstants.ConnectionEdgeName + nr;
            connectorWaypoints[connectorWaypoints.Count - 1].name = roadName + "-" + Gley.UrbanSystem.Internal.UrbanSystemConstants.LaneNamePrefix + 0 + "-" + Gley.UrbanSystem.Internal.UrbanSystemConstants.ConnectionEdgeName + (connectorWaypoints.Count - 1);

            //link middle waypoints
            for (int j = 0; j < connectorWaypoints.Count - 1; j++)
            {
                currentWaypoint = connectorWaypoints[j].GetComponent<WaypointSettingsBase>();
                connectedWaypoint = connectorWaypoints[j + 1].GetComponent<WaypointSettingsBase>();
                currentWaypoint.neighbors.Add(connectedWaypoint);
                connectedWaypoint.prev.Add(currentWaypoint);
            }

            //link first waypoint
            connectedWaypoint = connectorWaypoints[0].GetComponent<WaypointSettingsBase>();
            currentWaypoint = connection.FromWaypoint;

            currentWaypoint.neighbors.Add(connectedWaypoint);
            connectedWaypoint.prev.Add(currentWaypoint);
            EditorUtility.SetDirty(currentWaypoint);
            EditorUtility.SetDirty(connectedWaypoint);

            //link last waypoint
            connectedWaypoint = connection.ToWaypoint;

            currentWaypoint = connectorWaypoints[connectorWaypoints.Count - 1].GetComponent<WaypointSettingsBase>();
            currentWaypoint.neighbors.Add(connectedWaypoint);
            connectedWaypoint.prev.Add(currentWaypoint);
            EditorUtility.SetDirty(currentWaypoint);
            EditorUtility.SetDirty(connectedWaypoint);
        }


        private void RemoveConnectionHolder(Transform holder)
        {
            RemoveConnectionWaipoints(holder);
            GleyPrefabUtilities.DestroyTransform(holder);
        }


        private void RemoveConnectionWaipoints(Transform holder)
        {
            if (holder)
            {
                for (int i = holder.childCount - 1; i >= 0; i--)
                {
                    WaypointSettingsBase waypoint = holder.GetChild(i).GetComponent<WaypointSettingsBase>();
                    for (int j = 0; j < waypoint.neighbors.Count; j++)
                    {
                        if (waypoint.neighbors[j] != null)
                        {
                            waypoint.neighbors[j].prev.Remove(waypoint);
                        }
                        else
                        {
                            Debug.LogError(waypoint.name + " has null neighbors", waypoint);
                        }
                    }
                    for (int j = 0; j < waypoint.prev.Count; j++)
                    {
                        if (waypoint.prev[j] != null)
                        {
                            waypoint.prev[j].neighbors.Remove(waypoint);
                        }
                        else
                        {
                            Debug.LogError(waypoint.name + " has null prevs", waypoint);
                        }
                    }
                    GleyPrefabUtilities.DestroyImmediate(waypoint.gameObject);
                }
            }
        }
    }
}