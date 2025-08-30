using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Gley.PedestrianSystem.Editor
{
    internal class PedestrianLaneDrawer : Drawer
    {
        private readonly PedestrianLaneEditorData _laneData;
        private readonly Quaternion _up = Quaternion.LookRotation(Vector3.up);

        private PedestrianWaypointSettings _waypointScript;

        internal delegate void WaypointClicked(PedestrianWaypointSettings clickedWaypoint, PedestrianPath path);
        internal event WaypointClicked OnWaypointClicked;
        void TriggerWaypointClickedEvent(PedestrianWaypointSettings clickedWaypoint, PedestrianPath path)
        {
            OnWaypointClicked?.Invoke(clickedWaypoint, path);
        }


        internal PedestrianLaneDrawer (PedestrianLaneEditorData laneData):base (laneData)
        {
            _laneData = laneData;
        }


        internal void DrawLaneWaypoints(PedestrianPath road, Color waypointColor, Color disconnectedColor)
        {
            var lanes = _laneData.GetRoadLanes(road);
            if (lanes == null)
            {
                return;
            }

            for (int i = 0; i < lanes.Length; i++)
            {
                DrawSingleLane(lanes[i], waypointColor, disconnectedColor);
            }
        }


        internal void DrawConnectWaypoints(PedestrianPathEditorData allPaths, PedestrianWaypointSettings selectedWaypoint, Color selectedWaypointColor, Color waypointColor)
        {
            foreach (var path in allPaths.GetAllRoads())
            {
                float size;
                var lanes = _laneData.GetRoadLanes(path);
                if (lanes == null)
                {
                    return;
                }

                for (int i = 0; i < lanes.Length; i++)
                {
                    for (int j = 0; j < lanes[i].Waypoints.Length; j++)
                    {
                        if (lanes[i].Waypoints[j].inView)
                        {
                            if (lanes[i].Waypoints[j] == selectedWaypoint)
                            {
                                Handles.color = selectedWaypointColor;
                            }
                            else
                            {
                                Handles.color = waypointColor;
                            }
                            size = Customizations.GetRoadConnectorSize(SceneView.lastActiveSceneView.camera.transform.position, lanes[i].Waypoints[j].position) / 2;
                            if (Handles.Button(lanes[i].Waypoints[j].position, Quaternion.LookRotation(Camera.current.transform.forward, Camera.current.transform.up), size, size, Handles.DotHandleCap))
                            {
                                TriggerWaypointClickedEvent(lanes[i].Waypoints[j], path);
                            }
                        }
                    }
                }
            }
        }


        internal void DrawPathWidth(PedestrianPath path, Color roadColor)
        {
            var lanes = _laneData.GetRoadLanes(path);
            if (lanes == null)
            {
                return;
            }

            for (int i = 0; i < lanes.Length; i++)
            {
                DrawWidth(lanes[i].Waypoints, roadColor, lanes[i].IsClosed);
            }
        }


        internal void DrawWidth(PedestrianWaypointSettings[] waypoints, Color roadColor, bool isClosed)
        {
            List<Vector3> points = new List<Vector3>();
            List<Vector3> left = new List<Vector3>();
            List<Vector3> right = new List<Vector3>();

            Handles.color = roadColor;

            for (int i = 0; i < waypoints.Length; i++)
            {
                points.Add(waypoints[i].position);
            }

            Vector3 forward = Vector3.zero;
            Vector3 leftVector;
            for (int i = 0; i < points.Count; i++)
            {
                forward = Vector3.zero;
                if (i < points.Count - 1)
                {
                    forward += points[i + 1] - points[i];
                }
                else
                {
                    if(isClosed)
                    {
                        forward += points[0] - points[i];
                    }
                }
                if (i > 0)
                {
                    forward += points[i] - points[i - 1];
                }
                forward.Normalize();

                leftVector = Vector3.Cross(Vector3.up, forward).normalized;

                left.Add(points[i] + leftVector * waypoints[i].LaneWidth * 0.5f);
                right.Add(points[i] - leftVector * waypoints[i].LaneWidth * 0.5f);
            }

            if (isClosed)
            {
                left.Add(left[0]);
                right.Add(right[0]);
            }

            Handles.DrawPolyLine(left.ToArray());
            Handles.DrawPolyLine(right.ToArray());
        }


        private void DrawSingleLane(LaneHolder<PedestrianWaypointSettings> laneHolder, Color waypointColor, Color disconnectedColor)
        {
            Vector3[] positions;
            if (laneHolder.IsClosed)
            {
                positions = new Vector3[laneHolder.Waypoints.Length + 1];
            }
            else
            {
                positions = new Vector3[laneHolder.Waypoints.Length];
            }
            for (int i = 0; i < laneHolder.Waypoints.Length; i++)
            {
                positions[i] = laneHolder.Waypoints[i].position;
                _waypointScript = laneHolder.Waypoints[i];

                if (_waypointScript.neighbors.Count == 0 || _waypointScript.prev.Count == 0)
                {
                    Handles.color = disconnectedColor;
                    DrawUnconnectedWaypoint(_waypointScript.position);
                }

                Handles.color = waypointColor;
                Handles.DrawWireDisc(_waypointScript.position, Vector3.up, 0.5f);
            }
            if (laneHolder.IsClosed)
            {
                positions[positions.Length - 1] = positions[0];
            }
            Handles.DrawPolyLine(positions);
        }


        private void DrawUnconnectedWaypoint(Vector3 position)
        {
            Handles.ArrowHandleCap(0, position, _up, 10, EventType.Repaint);
        }
    }
}
