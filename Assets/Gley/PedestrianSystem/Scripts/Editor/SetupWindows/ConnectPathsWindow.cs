using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class ConnectPathsWindow : PedestrianSetupWindow
    {
        private const float maxValue = 396;
        private const float minValue = 296;

        private List<PedestrianConnectionCurve> _connectionsOfInterest;
        private List<PedestrianPath> _roadsOfInterest;
        private bool[] _allowedPedestrianIndex;
        private PedestrianPathEditorData _pedestrianPathData;
        private PedestrianLaneEditorData _pedestrianLaneData;
        private PedestrianWaypointEditorData _pedestrianWaypointData;
        private PedestrianConnectionEditorData _pedestrianConnectionData;
        private PedestrianPathDrawer _pedestrianPathDrawer;
        private PedestrianLaneDrawer _pedestrianLaneDrawer;
        private PedestrianWaypointDrawer _pedestrianWaypointDrawer;
        private PedestrianConnectionDrawer _pedestrianConnectionDrawer;
        private PedestrianWaypointCreator _pedestrianWaypointCreator;
        private PedestrianConnectionCreator _pedestrianConnectionCreator;
        private PedestrianWaypointSettings _selectedWaypoint;
        private float _scrollAdjustment;
        private int _nrOfRoads;
        private int _nrOfConnections;
        private int _nrOfPedestrians;
        private bool _drawAllConnections;
        private bool _showCustomizations;
        private bool _drawSecondConnectors;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            _pedestrianPathData = new PedestrianPathEditorData();
            _pedestrianLaneData = new PedestrianLaneEditorData(_pedestrianPathData);;
            _pedestrianWaypointData = new PedestrianWaypointEditorData();
            _pedestrianConnectionData = new PedestrianConnectionEditorData(_pedestrianPathData);

            _pedestrianPathDrawer = new PedestrianPathDrawer(_pedestrianPathData);
            _pedestrianLaneDrawer = new PedestrianLaneDrawer(_pedestrianLaneData);
            _pedestrianWaypointDrawer = new PedestrianWaypointDrawer(_pedestrianWaypointData);
            _pedestrianConnectionDrawer = new PedestrianConnectionDrawer(_pedestrianConnectionData);

            _pedestrianWaypointCreator = new PedestrianWaypointCreator();
            _pedestrianConnectionCreator = new PedestrianConnectionCreator(_pedestrianConnectionData, _pedestrianWaypointCreator);

            _pedestrianLaneDrawer.OnWaypointClicked += WaipointClicked;

            _nrOfPedestrians = System.Enum.GetValues(typeof(PedestrianTypes)).Length;
            _allowedPedestrianIndex = new bool[_nrOfPedestrians];
            for (int i = 0; i < _allowedPedestrianIndex.Length; i++)
            {
                _allowedPedestrianIndex[i] = true;
            }

            return this;
        }


        public override void DrawInScene()
        {
            _roadsOfInterest = _pedestrianPathDrawer.ShowAllRoads(MoveTools.None, _editorSave.EditorColors.RoadColor, _editorSave.EditorColors.AnchorPointColor, _editorSave.EditorColors.ControlPointColor, _editorSave.EditorColors.LabelColor, _editorSave.ViewLabels);

            if (_roadsOfInterest.Count != _nrOfRoads)
            {
                _nrOfRoads = _roadsOfInterest.Count;
                SettingsWindowBase.TriggerRefreshWindowEvent();
            }

            for (int i = 0; i < _nrOfRoads; i++)
            {
                if (_editorSave.ViewRoadLanes)
                {
                    _pedestrianLaneDrawer.DrawPathWidth(_roadsOfInterest[i], _editorSave.EditorColors.LaneColor);
                }

                
            }
            _pedestrianWaypointDrawer.UpdateInViewProperty(_pedestrianWaypointData.GetAllWaypoints());
            _pedestrianLaneDrawer.DrawConnectWaypoints(_pedestrianPathData, _selectedWaypoint, _editorSave.EditorColors.SelectedWaypointColor, _editorSave.EditorColors.RoadConnectorColor);


            _connectionsOfInterest = _pedestrianConnectionDrawer.ShowAllConnections(_roadsOfInterest, _editorSave.ViewLabels, _editorSave.EditorColors.ConnectorLaneColor, _editorSave.EditorColors.AnchorPointColor,
              _editorSave.EditorColors.RoadConnectorColor, _editorSave.EditorColors.SelectedRoadConnectorColor, _editorSave.EditorColors.DisconnectedColor, _editorSave.WaypointDistance, _editorSave.EditorColors.LabelColor, _editorSave.EditorColors.WaypointColor);

            if (_connectionsOfInterest.Count != _nrOfConnections)
            {
                _nrOfConnections = _connectionsOfInterest.Count;
                SettingsWindowBase.TriggerRefreshWindowEvent();
            }

            for (int i = 0; i < _nrOfConnections; i++)
            {
                if (_connectionsOfInterest[i].DrawWidth)
                {
                    _pedestrianLaneDrawer.DrawWidth(_pedestrianConnectionData.GetWaypoints(_connectionsOfInterest[i]), _editorSave.EditorColors.LaneColor, false);
                }
            }
            base.DrawInScene();
        }


        protected override void TopPart()
        {
            base.TopPart();
            string drawButton = "Draw All Connections";
            if (_drawAllConnections == true)
            {
                drawButton = "Clear All";
            }

            if (GUILayout.Button(drawButton))
            {
                DrawButton();
            }

            EditorGUI.BeginChangeCheck();
            if (_showCustomizations == false)
            {
                _scrollAdjustment = minValue;
                _showCustomizations = EditorGUILayout.Toggle("Change Colors ", _showCustomizations);
                _editorSave.ViewLabels = EditorGUILayout.Toggle("View Labels", _editorSave.ViewLabels);
                _editorSave.ViewRoadLanes = EditorGUILayout.Toggle("View Path Width", _editorSave.ViewRoadLanes);
            }
            else
            {
                _scrollAdjustment = maxValue;
                _showCustomizations = EditorGUILayout.Toggle("Change Colors ", _showCustomizations);
                EditorGUILayout.BeginHorizontal();
                _editorSave.ViewLabels = EditorGUILayout.Toggle("View Labels", _editorSave.ViewLabels, GUILayout.Width(TOGGLE_WIDTH));
                _editorSave.EditorColors.LabelColor = EditorGUILayout.ColorField(_editorSave.EditorColors.LabelColor);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _editorSave.ViewRoadLanes = EditorGUILayout.Toggle("View Path Width", _editorSave.ViewRoadLanes, GUILayout.Width(TOGGLE_WIDTH));
                _editorSave.EditorColors.LaneColor = EditorGUILayout.ColorField(_editorSave.EditorColors.LaneColor);
                EditorGUILayout.EndHorizontal();

                _editorSave.EditorColors.RoadColor = EditorGUILayout.ColorField("Road Color", _editorSave.EditorColors.RoadColor);
                _editorSave.EditorColors.ConnectorLaneColor = EditorGUILayout.ColorField("Connector Lane Color", _editorSave.EditorColors.ConnectorLaneColor);
                _editorSave.EditorColors.AnchorPointColor = EditorGUILayout.ColorField("Anchor Point Color", _editorSave.EditorColors.AnchorPointColor);
                _editorSave.EditorColors.RoadConnectorColor = EditorGUILayout.ColorField("Road Connector Color", _editorSave.EditorColors.RoadConnectorColor);
                _editorSave.EditorColors.SelectedRoadConnectorColor = EditorGUILayout.ColorField("Selected Connector Color", _editorSave.EditorColors.SelectedRoadConnectorColor);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select All"))
            {
                foreach (var conn in _connectionsOfInterest)
                {
                    conn.Draw = true;
                }
            }

            if (GUILayout.Button("Deselect All"))
            {
                foreach (var conn in _connectionsOfInterest)
                {
                    conn.Draw = false;
                }
            }

            if (GUILayout.Button("Inverse Select"))
            {
                foreach (var conn in _connectionsOfInterest)
                {
                    conn.Draw = !conn.Draw;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndChangeCheck();

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }


        protected override void ScrollPart(float width, float height)
        {
            base.ScrollPart(width, height);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.Width(width - SCROLL_SPACE), GUILayout.Height(height - _scrollAdjustment));
            if (_connectionsOfInterest != null)
            {
                for (int i = 0; i < _connectionsOfInterest.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    _connectionsOfInterest[i].Draw = EditorGUILayout.Toggle(_connectionsOfInterest[i].Draw, GUILayout.Width(TOGGLE_DIMENSION));
                    EditorGUILayout.LabelField(_connectionsOfInterest[i].Name);
                    Color oldColor = GUI.backgroundColor;
                    if (_connectionsOfInterest[i].DrawWaypoints == true)
                    {
                        GUI.backgroundColor = Color.green;
                    }

                    if (GUILayout.Button("Waypoints", GUILayout.Width(BUTTON_DIMENSION)))
                    {
                        _connectionsOfInterest[i].DrawWaypoints = !_connectionsOfInterest[i].DrawWaypoints;
                    }

                    if (_connectionsOfInterest[i].DrawWidth == true)
                    {
                        GUI.backgroundColor = Color.green;
                    }
                    else
                    {
                        GUI.backgroundColor = oldColor;
                    }

                    if (GUILayout.Button("Width", GUILayout.Width(BUTTON_DIMENSION)))
                    {
                        _connectionsOfInterest[i].DrawWidth = !_connectionsOfInterest[i].DrawWidth;
                    }
                    GUI.backgroundColor = oldColor;

                    if (GUILayout.Button("View", GUILayout.Width(BUTTON_DIMENSION)))
                    {
                        View(i);
                    }

                    if (GUILayout.Button("Delete", GUILayout.Width(BUTTON_DIMENSION)))
                    {
                        if (EditorUtility.DisplayDialog("Delete " + _connectionsOfInterest[i].Name + "?", "Are you sure you want to delete " + _connectionsOfInterest[i].Name + "? \nYou cannot undo this operation.", "Delete", "Cancel"))
                        {
                            _pedestrianConnectionCreator.DeleteConnection(_connectionsOfInterest[i]);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

           

            EditorGUILayout.Space();
            GUILayout.EndScrollView();
            EditorGUILayout.Space();

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();
        }


        protected override void BottomPart()
        {
            _editorSave.LaneWidth = EditorGUILayout.FloatField("Path width ", _editorSave.LaneWidth);
            _editorSave.WaypointDistance = EditorGUILayout.FloatField("Waypoint distance ", _editorSave.WaypointDistance);
            if (_editorSave.WaypointDistance <= 0)
            {
                Debug.LogWarning("Waypoint distance needs to be >0. will be set to 1 by default");
                _editorSave.WaypointDistance = 1;
            }

            if (GUILayout.Button("Generate Selected Connections"))
            {
                _pedestrianConnectionCreator.GenerateSelectedConnections(_connectionsOfInterest, _editorSave.WaypointDistance, _editorSave.LaneWidth);
                SceneView.RepaintAll();
            }
            base.BottomPart();
        }


        private void WaipointClicked(PedestrianWaypointSettings clickedWaypoint, PedestrianPath path)
        {
            if (_drawSecondConnectors == true)
            {
                if (_selectedWaypoint != clickedWaypoint)
                {
                    _pedestrianConnectionCreator.MakePathConnection(path.transform.parent.GetComponent<PedestrianConnectionPool>(), _selectedWaypoint, clickedWaypoint, _editorSave.WaypointDistance, _editorSave.LaneWidth);
                }
                _selectedWaypoint = null;
                _drawSecondConnectors = false;
            }
            else
            {
                _selectedWaypoint = clickedWaypoint;
                _drawSecondConnectors = true;
            }
        }


        private void DrawButton()
        {
            _drawAllConnections = !_drawAllConnections;
            for (int i = 0; i < _connectionsOfInterest.Count; i++)
            {
                _connectionsOfInterest[i].Draw = _drawAllConnections;
                if (_drawAllConnections == false)
                {
                    _connectionsOfInterest[i].DrawWaypoints = false;
                }
            }
        }


        private void View(int curveIndex)
        {
            GleyUtilities.TeleportSceneCamera(_connectionsOfInterest[curveIndex].FromWaypoint.gameObject.transform.position);
        }


        public override void DestroyWindow()
        {
            if (_pedestrianWaypointDrawer != null)
            {
                _pedestrianLaneDrawer.OnWaypointClicked += WaipointClicked;
            }

            _pedestrianPathDrawer?.OnDestroy();
            _pedestrianLaneDrawer?.OnDestroy();
            _pedestrianWaypointDrawer?.OnDestroy();
            _pedestrianConnectionDrawer?.OnDestroy();

            base.DestroyWindow();
        }
    }
}