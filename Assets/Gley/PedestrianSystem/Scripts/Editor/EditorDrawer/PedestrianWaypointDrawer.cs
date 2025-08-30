using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class PedestrianWaypointDrawer : Drawer
    {
        private readonly Dictionary<PedestrianTypes, string> _pedestrianTypesToString = new Dictionary<PedestrianTypes, string>();
        private readonly Dictionary<int, string> _priorityToString = new Dictionary<int, string>();
        private readonly PedestrianWaypointEditorData _pedestrianWaypointData;
        private readonly GUIStyle _carsStyle;
        private readonly GUIStyle _priorityStyle;

        private List<PedestrianWaypointSettings> _pathProblems;
        private Quaternion _towardsCamera;
        private bool _colorChanged;


        public delegate void WaypointClicked(PedestrianWaypointSettings clickedWaypoint, bool leftClick);
        public event WaypointClicked OnWaypointClicked;
        void TriggerWaypointClickedEvent(PedestrianWaypointSettings clickedWaypoint, bool leftClick)
        {
            PedestrianSettingsWindow.SetSelectedWaypoint(clickedWaypoint);
            OnWaypointClicked?.Invoke(clickedWaypoint, leftClick);
        }


        public PedestrianWaypointDrawer (PedestrianWaypointEditorData pedestrianWaypointData):base (pedestrianWaypointData)
        {
            _pedestrianWaypointData = pedestrianWaypointData;

            _carsStyle = new GUIStyle();
            _priorityStyle = new GUIStyle();
            _pedestrianTypesToString = new Dictionary<PedestrianTypes, string>();
            var allTypes = Enum.GetValues(typeof(PedestrianTypes)).Cast<PedestrianTypes>();

            foreach (var vehicleType in allTypes)
            {
                _pedestrianTypesToString.Add(vehicleType, vehicleType.ToString());
            }

            var allWaypoints = pedestrianWaypointData.GetAllWaypoints();
            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (!_priorityToString.ContainsKey(allWaypoints[i].priority))
                {
                    _priorityToString.Add(allWaypoints[i].priority, allWaypoints[i].priority.ToString());
                }
            }
        }


        public void DrawWaypointsForLink(PedestrianWaypointSettings currentWaypoint, List<WaypointSettingsBase> neighborsList, Color waypointColor)
        {
            _colorChanged = true;
            var allWaypoints = _pedestrianWaypointData.GetAllWaypoints();
            UpdateInViewProperty(allWaypoints);
            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (allWaypoints[i].inView)
                {
                    if (allWaypoints[i] != currentWaypoint && !neighborsList.Contains(allWaypoints[i]))
                    {
                        DrawCompleteWaypoint(allWaypoints[i], true, waypointColor);
                    }
                }
            }
        }


        public void ShowIntersectionWaypoints(Color waypointColor)
        {
            ShowAllWaypoints(waypointColor, true, false, default, false, default);
        }


        public void ShowAllWaypoints(Color waypointColor, bool showConnections, bool showPedestrians, Color pedestriansColor, bool showPriority, Color priorityColor)
        {
            var allWaypoints = _pedestrianWaypointData.GetAllWaypoints();
            _colorChanged = true;
            UpdateInViewProperty(allWaypoints);
            if (showPedestrians)
            {
                _carsStyle.normal.textColor = pedestriansColor;
            }
            if (showPriority)
            {
                _priorityStyle.normal.textColor = priorityColor;
            }

            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (allWaypoints[i].inView)
                {
                    DrawCompleteWaypoint(allWaypoints[i], showConnections, waypointColor, showPedestrians, showPriority, false, default);
                }
            }
        }


        public void ShowWaypointsWithPenalty(int penalty, Color color)
        {
            var allWaypoints = _pedestrianWaypointData.GetAllWaypoints();
            _colorChanged = true;
            UpdateInViewProperty(allWaypoints);

            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (allWaypoints[i].inView)
                {
                    if (allWaypoints[i].penalty == penalty)
                    {
                        DrawCompleteWaypoint(allWaypoints[i], true, color);
                    }
                }
            }
        }


        public void ShowWaypointsWithPriority(int priority, Color color)
        {
            var allWaypoints = _pedestrianWaypointData.GetAllWaypoints();
            _colorChanged = true;
            UpdateInViewProperty(allWaypoints);

            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (allWaypoints[i].inView)
                {
                    if (allWaypoints[i].priority == priority)
                    {
                        DrawCompleteWaypoint(allWaypoints[i], true, color);
                    }
                }
            }
        }


        public void ShowWaypointsWithPedestrian(PedestrianTypes pedestrian, Color color)
        {
            var allWaypoints = _pedestrianWaypointData.GetAllWaypoints();
            _colorChanged = true;
            UpdateInViewProperty(allWaypoints);
            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (allWaypoints[i].inView)
                {
                    if (allWaypoints[i].AllowedPedestrians.Contains(pedestrian))
                    {
                        DrawCompleteWaypoint(allWaypoints[i], true, color);
                    }
                }
            }
        }


        public void ShowDisconnectedWaypoints(Color waypointColor)
        {
            var allWaypoints = _pedestrianWaypointData.GetDisconnectedWaypoints();
            _colorChanged = true;
            UpdateInViewProperty(allWaypoints);
            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (allWaypoints[i].inView)
                {
                    DrawCompleteWaypoint(allWaypoints[i], false, waypointColor);
                }
            }
        }


        public void ShowPedestrianTypeEditedWaypoints(Color waypointColor, Color carsColor)
        {
            var allWaypoints = _pedestrianWaypointData.GetPedestrianTypeEditedWaypoints();
            _colorChanged = true;
            UpdateInViewProperty(allWaypoints);
            _carsStyle.normal.textColor = carsColor;
            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (allWaypoints[i].inView)
                {
                    DrawCompleteWaypoint(allWaypoints[i], false, waypointColor, true);
                }
            }
        }


        public void ShowPriorityEditedWaypoints(Color waypointColor, Color priorityColor)
        {
            var allWaypoints = _pedestrianWaypointData.GetPriorityEditedWaypoints();
            _colorChanged = true;
            UpdateInViewProperty(allWaypoints);
            _priorityStyle.normal.textColor = priorityColor;
            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (allWaypoints[i].inView)
                {
                    DrawCompleteWaypoint(allWaypoints[i], false, waypointColor, false, true);
                }
            }
        }


        public void ShowEventWaypoints(Color waypointColor, Color labelColor)
        {
            var allWaypoints = _pedestrianWaypointData.GetEventWaypoints();
            _colorChanged = true;
            _carsStyle.normal.textColor = labelColor;
            UpdateInViewProperty(allWaypoints);
            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (allWaypoints[i].inView)
                {
                    DrawCompleteWaypoint(allWaypoints[i], false, waypointColor);
                    Handles.color = labelColor;
                    Handles.Label(allWaypoints[i].position, (allWaypoints[i]).eventData, _carsStyle);
                }
            }
        }


        public PedestrianWaypointSettings[] ShowVehicleProblems(Color waypointColor, Color carsColor)
        {
            var allWaypoints = _pedestrianWaypointData.GetAllWaypoints();
            _colorChanged = true;
            UpdateInViewProperty(allWaypoints);
            _carsStyle.normal.textColor = carsColor;
            _pathProblems = new List<PedestrianWaypointSettings>();
            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (allWaypoints[i].inView)
                {
                    int nr = allWaypoints[i].AllowedPedestrians.Count;
                    for (int j = 0; j < allWaypoints[i].AllowedPedestrians.Count; j++)
                    {
                        for (int k = 0; k < allWaypoints[i].neighbors.Count; k++)
                        {
                            if (((PedestrianWaypointSettings)allWaypoints[i].neighbors[k]).AllowedPedestrians.Contains(allWaypoints[i].AllowedPedestrians[j]))
                            {
                                nr--;
                                break;
                            }
                        }
                    }
                    if (nr != 0)
                    {
                        _pathProblems.Add(allWaypoints[i]);
                        DrawCompleteWaypoint(allWaypoints[i], true, waypointColor, true);

                        for (int k = 0; k < allWaypoints[i].neighbors.Count; k++)
                        {
                            for (int j = 0; j < ((PedestrianWaypointSettings)allWaypoints[i].neighbors[k]).AllowedPedestrians.Count; j++)
                            {
                                DrawCompleteWaypoint(allWaypoints[i], true, waypointColor, true);
                            }
                        }
                    }
                }

            }
            return _pathProblems.ToArray();
        }


        internal void DrawCurrentWaypoint(PedestrianWaypointSettings waypoint, Color selectedColor, Color waypointColor, Color prevColor)
        {
            _colorChanged = true;

            DrawCompleteWaypoint(waypoint, true, selectedColor, false, false, true, prevColor);

            for (int i = 0; i < waypoint.neighbors.Count; i++)
            {
                DrawCompleteWaypoint((PedestrianWaypointSettings)waypoint.neighbors[i], false, waypointColor);
            }

            _colorChanged = true;
            for (int i = 0; i < waypoint.prev.Count; i++)
            {

                DrawCompleteWaypoint((PedestrianWaypointSettings)waypoint.prev[i], false, prevColor);
            }
        }


        public void DrawSelectedWaypoint(PedestrianWaypointSettings selectedWaypoint, Color color)
        {
            _colorChanged = true;
            DrawCompleteWaypoint(selectedWaypoint, false, color);
        }


        public void ShowPenaltyEditedWaypoints(Color waypointColor)
        {
            var allWaypoints = _pedestrianWaypointData.GetPenlatyEditedWaypoints();
            _colorChanged = true;
            UpdateInViewProperty(allWaypoints);
            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (allWaypoints[i].inView)
                {
                    DrawCompleteWaypoint(allWaypoints[i], false, waypointColor);
                }
            }
        }


        public void DrawDirectionWaypoint(WaypointSettingsBase stopWaypoint, Color stopWaypointsColor)
        {
            Handles.color = stopWaypointsColor;
            if (stopWaypoint != null)
            {
                Handles.DrawSolidDisc(stopWaypoint.transform.position, Vector3.up, 1);
            }
        }


        public void DrawAllPathConnectors(Color connectorLaneColor, bool showConnections, Color connectionColor, bool showCars, Color carsColor)
        {
            var allWaypoints = _pedestrianWaypointData.GetAllWaypoints();
            for (int i = 0; i < allWaypoints.Length; i++)
            {
                DrawCompleteWaypoint(allWaypoints[i], showConnections, connectorLaneColor);
            }
        }


        public void DrawPossibleDirectionWaypoints(List<PedestrianWaypointSettings> waypoints, Color waypointColor)
        {
            _colorChanged = true;

            for (int i = 0; i < waypoints.Count; i++)
            {
                for (int j = 0; j < waypoints[i].neighbors.Count; j++)
                {
                    DrawCompleteWaypoint((PedestrianWaypointSettings)waypoints[i].neighbors[j], false, waypointColor);
                    DrawConnection(waypoints[i].position, waypoints[i].neighbors[j].position);
                }
                for (int j = 0; j < waypoints[i].prev.Count; j++)
                {
                    DrawCompleteWaypoint((PedestrianWaypointSettings)waypoints[i].prev[j], false, waypointColor);
                    DrawConnection(waypoints[i].position, waypoints[i].prev[j].position);
                }
            }
        }


        public void UpdateInViewProperty(PedestrianWaypointSettings[] selectedWaypoints)
        {
            GleyUtilities.SetCamera();
            if (_cameraMoved)
            {
                _cameraMoved = false;
                for (int i = 0; i < selectedWaypoints.Length; i++)
                {
                    if (GleyUtilities.IsPointInView(selectedWaypoints[i].position))
                    {
                        selectedWaypoints[i].inView = true;
                    }
                    else
                    {
                        selectedWaypoints[i].inView = false;
                    }
                }
                if (Camera.current != null)
                {
                    _towardsCamera = Quaternion.LookRotation(Camera.current.transform.forward, Camera.current.transform.up);
                }
            }
        }


        private void DrawCompleteWaypoint(PedestrianWaypointSettings waypoint, bool showConnections, Color waypointColor, bool showPedestrians = false, bool showPriority = false, bool drawPrev = false, Color prevColor = default)
        {
            SetHandleColor(waypointColor, _colorChanged);
            if (_colorChanged == true)
            {
                _colorChanged = false;
            }
            //clickable button
            if (Handles.Button(waypoint.position, _towardsCamera, 0.5f, 0.5f, Handles.DotHandleCap))
            {
                TriggerWaypointClickedEvent(waypoint, Event.current.button == 0);
            }

            if (showConnections)
            {
                for (int i = 0; i < waypoint.neighbors.Count; i++)
                {
                    DrawConnection(waypoint.position, waypoint.neighbors[i].position);
                }
            }

            if (drawPrev)
            {
                _colorChanged = true;
                SetHandleColor(prevColor, _colorChanged);
                for (int i = 0; i < waypoint.prev.Count; i++)
                {
                    DrawConnection(waypoint.position, waypoint.prev[i].position);
                }
            }

            if (showPedestrians)
            {
                ShowPedestrians(waypoint);
            }
            if (showPriority)
            {
                ShowPriority(waypoint);
            }
        }


        private void ShowPriority(PedestrianWaypointSettings waypoint)
        {
            Handles.Label(waypoint.position, _priorityToString[waypoint.priority], _priorityStyle);
        }


        private void ShowPedestrians(PedestrianWaypointSettings waypoint)
        {
            string text = string.Empty;
            for (int j = 0; j < waypoint.AllowedPedestrians.Count; j++)
            {
                text += _pedestrianTypesToString[waypoint.AllowedPedestrians[j]] + "\n";
            }
            Handles.Label(waypoint.position, text, _carsStyle);
        }


        private void DrawConnection(Vector3 start, Vector3 end)
        {
            Handles.DrawLine(start, end);
        }


        private void SetHandleColor(Color newColor, bool colorChanged)
        {
            if (colorChanged)
            {
                Handles.color = newColor;
            }
        }
    }
}