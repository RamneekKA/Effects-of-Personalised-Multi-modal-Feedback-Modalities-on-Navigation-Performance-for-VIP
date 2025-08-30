using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    internal class PedestrianConnectionDrawer : Drawer
    {
        private readonly PedestrianConnectionEditorData _connectionData;

        private List<PedestrianConnectionCurve> _selectedConnections = new List<PedestrianConnectionCurve>();


        internal PedestrianConnectionDrawer (PedestrianConnectionEditorData connectionData):base(connectionData)
        {
            _connectionData = connectionData;
        }


        internal List<PedestrianConnectionCurve> ShowAllConnections(List<PedestrianPath> allRoads, bool viewLabels, Color connectorLaneColor, Color anchorPointColor, Color roadConnectorColor, Color selectedRoadConnectorColor, Color disconnectedColor, float waypointDistance, Color textColor, Color waypointColor)
        {
            _style.normal.textColor = textColor;
            var allConnections = _connectionData.GetAllConnections();
            UpdateInViewProperty(allConnections);

            int nr = 0;
            for (int i = 0; i < allConnections.Length; i++)
            {
                if (allConnections[i].InView)
                {
                    nr++;
                    if (allConnections[i].Draw == true)
                    {
                        DrawConnection(allConnections[i], viewLabels, connectorLaneColor, anchorPointColor);
                    }
                    if (allConnections[i].DrawWaypoints == true)
                    {
                        Handles.color = waypointColor;
                        DrawWaypoints(allConnections[i]);
                    }
                }
            }
            if (nr != _selectedConnections.Count)
            {
                UpdateSelectedConnections(allConnections);
            }
            return _selectedConnections;
        }


        private void UpdateInViewProperty(PedestrianConnectionCurve[] connectionCurves)
        {
            GleyUtilities.SetCamera();
            if (_cameraMoved)
            {
                _cameraMoved = false;
                for (int i = 0; i < connectionCurves.Length; i++)
                {
                    if (GleyUtilities.IsPointInView(connectionCurves[i].InPosition) || GleyUtilities.IsPointInView(connectionCurves[i].OutPosition))
                    {
                        connectionCurves[i].InView = true;
                    }
                    else
                    {
                        connectionCurves[i].InView = false;
                    }
                }
            }
        }


        private void UpdateSelectedConnections(PedestrianConnectionCurve[] allConnections)
        {
            _selectedConnections = new List<PedestrianConnectionCurve>();
            for (int i = 0; i < allConnections.Length; i++)
            {
                if (allConnections[i].InView == true)
                {
                    _selectedConnections.Add(allConnections[i]);
                }
            }
        }


        private void DrawWaypoints(PedestrianConnectionCurve connectionCurve)
        {
            var allWaypoints = _connectionData.GetWaypoints(connectionCurve);
            Vector3[] positions = new Vector3[allWaypoints.Length + 2];
            positions[0] = connectionCurve.FromWaypoint.transform.position;
            positions[positions.Length - 1] = connectionCurve.ToWaypoint.transform.position;
            for (int i = 0; i < allWaypoints.Length; i++)
            {
                positions[i + 1] = allWaypoints[i].position;
                Handles.DrawWireDisc(allWaypoints[i].position, Vector3.up, 0.5f);
            }
            Handles.DrawPolyLine(positions);
        }


        private void DrawConnection(PedestrianConnectionCurve connection, bool viewLabel, Color connectorLaneColor, Color anchorPointColor)
        {
            Path curve = connection.Curve;
            for (int i = 0; i < curve.NumSegments; i++)
            {
                Vector3[] points = curve.GetPointsInSegment(i, connection.GetOffset());
                Handles.color = Color.black;
                Handles.DrawLine(points[1], points[0]);
                Handles.DrawLine(points[2], points[3]);
                Handles.DrawBezier(points[0], points[3], points[1], points[2], connectorLaneColor, null, 2);
            }

            for (int i = 0; i < curve.NumPoints; i++)
            {
                if (i % 3 != 0)
                {
                    float handleSize = Customizations.GetAnchorPointSize(SceneView.lastActiveSceneView.camera.transform.position, curve.GetPoint(i, connection.GetOffset()));
                    Handles.color = anchorPointColor;
                    Vector3 newPos = curve.GetPoint(i, connection.GetOffset());
#if UNITY_2019 || UNITY_2020 || UNITY_2021
                    newPos = Handles.FreeMoveHandle(curve.GetPoint(i, connection.GetOffset()), Quaternion.identity, handleSize, Vector2.zero, Handles.SphereHandleCap);
#else
                    newPos = Handles.FreeMoveHandle(curve.GetPoint(i, connection.GetOffset()), handleSize, Vector2.zero, Handles.SphereHandleCap);
#endif
                    newPos.y = curve.GetPoint(i, connection.GetOffset()).y;
                    if (curve.GetPoint(i, connection.GetOffset()) != newPos)
                    {
                        Undo.RecordObject(_connectionData.GetConnectionPool(connection), "Move point");
                        curve.MovePoint(i, newPos - connection.GetOffset());
                    }
                }
            }

            if (viewLabel)
            {
                Handles.Label(connection.ToWaypoint.gameObject.transform.position, connection.Name, _style);
            }
        }
    }
}