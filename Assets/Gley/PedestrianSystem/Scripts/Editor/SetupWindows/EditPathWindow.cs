using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class EditPathWindow : PedestrianSetupWindow
    {
        private const float maxValue = 449;
        private const float minValue = 277;

        private bool[] _allowedPedestrianIndex;
        private PedestrianPathEditorData _pedestrianPathData;
        private PedestrianLaneEditorData _pedestrianLaneData;
        private PedestrianConnectionEditorData _pedestrianConnectionData;
        private PedestrianPathDrawer _pedestrianPathDrawer;
        private PedestrianLaneDrawer _pedestrianLaneDrawer;
        private PedestrianWaypointCreator _pedestrianWaypointCreator;
        private PedestrianLaneCreator _pedestrianLaneCreator;
        private PedestrianConnectionCreator _pedestrianConnectionCreator;
        private PedestrianPath _selectedPath;
        private MoveTools _moveTool;
        private string _closePathButtonText;
        private float _scrollAdjustment;
        private int _nrOfPedestrians;
        private bool _changeColors;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);

            _pedestrianPathData = new PedestrianPathEditorData();
            _pedestrianLaneData = new PedestrianLaneEditorData(_pedestrianPathData);
            _pedestrianConnectionData = new PedestrianConnectionEditorData(_pedestrianPathData);

            _pedestrianPathDrawer = new PedestrianPathDrawer(_pedestrianPathData);
            _pedestrianLaneDrawer = new PedestrianLaneDrawer(_pedestrianLaneData);

            _pedestrianWaypointCreator = new PedestrianWaypointCreator();
            _pedestrianLaneCreator = new PedestrianLaneCreator(_pedestrianLaneData, _pedestrianWaypointCreator);
            _pedestrianConnectionCreator = new PedestrianConnectionCreator(_pedestrianConnectionData, _pedestrianWaypointCreator);

            _selectedPath = PedestrianSettingsWindow.GetSelectedRoad();
            _selectedPath.justCreated = false;
            _moveTool = _editorSave.MoveTool;
            _nrOfPedestrians = System.Enum.GetValues(typeof(PedestrianTypes)).Length;
            if (_selectedPath.AllowedPedestrians == null || _selectedPath.AllowedPedestrians.Length != _nrOfPedestrians)
            {
                _allowedPedestrianIndex = new bool[_nrOfPedestrians];
                for (int i = 0; i < _allowedPedestrianIndex.Length; i++)
                {
                    if (_editorSave.GlobalPedestrianList.Contains((PedestrianTypes)i))
                    {
                        _allowedPedestrianIndex[i] = true;
                    }
                    else
                    {
                        _allowedPedestrianIndex[i] = false;
                    }
                }
                _selectedPath.AllowedPedestrians = _allowedPedestrianIndex;
            }
            else
            {
                _allowedPedestrianIndex = _selectedPath.AllowedPedestrians;
            }
            return this;
        }


        public override void DrawInScene()
        {
            if (_selectedPath == null)
            {
                Debug.LogWarning("No path selected");
                return;
            }

            _pedestrianPathDrawer.DrawPath(_selectedPath, _moveTool, _editorSave.EditorColors.RoadColor, _editorSave.EditorColors.AnchorPointColor, _editorSave.EditorColors.ControlPointColor, _editorSave.EditorColors.LabelColor, true);
            if (_editorSave.ViewRoadWaypoints)
            {
                _pedestrianLaneDrawer.DrawLaneWaypoints(_selectedPath, _editorSave.EditorColors.WaypointColor, _editorSave.EditorColors.DisconnectedColor);
            }

            if (_editorSave.ViewRoadLanes)
            {
                _pedestrianLaneDrawer.DrawPathWidth(_selectedPath, _editorSave.EditorColors.LaneColor);
            }
            base.DrawInScene();
        }


        protected override void TopPart()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Press SHIFT + Left Click to add a path point");
            EditorGUILayout.LabelField("Press SHIFT + Right Click to remove a path point");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            _editorSave.ViewRoadWaypoints = EditorGUILayout.Toggle("View Waypoints", _editorSave.ViewRoadWaypoints);
            _editorSave.ViewRoadLanes = EditorGUILayout.Toggle("View Path Width", _editorSave.ViewRoadLanes);
            EditorGUILayout.EndHorizontal();

            _selectedPath.laneWidth = EditorGUILayout.FloatField("Path width (m)", _selectedPath.laneWidth);
            _selectedPath.waypointDistance = EditorGUILayout.FloatField("Waypoint distance ", _selectedPath.waypointDistance);

            EditorGUI.BeginChangeCheck();
            _moveTool = (MoveTools)EditorGUILayout.EnumPopup("Select move tool ", _moveTool);

            _changeColors = EditorGUILayout.Toggle("Change Colors ", _changeColors);
            if (_changeColors == true)
            {
                _scrollAdjustment = maxValue;
                _editorSave.EditorColors.RoadColor = EditorGUILayout.ColorField("Road Color", _editorSave.EditorColors.RoadColor);
                _editorSave.EditorColors.WaypointColor = EditorGUILayout.ColorField("Waypoint Color", _editorSave.EditorColors.WaypointColor);
                _editorSave.EditorColors.DisconnectedColor = EditorGUILayout.ColorField("Disconnected Color", _editorSave.EditorColors.DisconnectedColor);
                _editorSave.EditorColors.LaneColor = EditorGUILayout.ColorField("Lane Width Color", _editorSave.EditorColors.LaneColor);
                _editorSave.EditorColors.ControlPointColor = EditorGUILayout.ColorField("Control Point Color", _editorSave.EditorColors.ControlPointColor);
                _editorSave.EditorColors.AnchorPointColor = EditorGUILayout.ColorField("Anchor Point Color", _editorSave.EditorColors.AnchorPointColor);
            }
            else
            {
                _scrollAdjustment = minValue;
            }
            EditorGUI.EndChangeCheck();
            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }

            base.TopPart();
        }


        protected override void ScrollPart(float width, float height)
        {
            if (_selectedPath == null)
            {
                Debug.LogWarning("No road selected");
                return;
            }
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.Width(width - SCROLL_SPACE), GUILayout.Height(height - _scrollAdjustment));

            EditorGUILayout.LabelField("Path Settings", EditorStyles.boldLabel);
            GUILayout.Label("Allowed Pedestrian Types:");
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < _nrOfPedestrians; i++)
            {
                _allowedPedestrianIndex[i] = EditorGUILayout.Toggle(((PedestrianTypes)i).ToString(), _allowedPedestrianIndex[i]);
            }
            EditorGUI.EndChangeCheck();
            if (GUI.changed)
            {
                ApplyGlobalCarSettings();
            }
            EditorGUILayout.Space();
            if (!_selectedPath.path.IsClosed)
            {
                _closePathButtonText = "Close Path";
            }
            else
            {
                _closePathButtonText = "Open Path";
            }

            if (GUILayout.Button(_closePathButtonText))
            {
                _pedestrianPathDrawer.ToggleClosed(_selectedPath);
            }

            GUILayout.EndScrollView();

            base.ScrollPart(width, height);
        }


        protected override void BottomPart()
        {
            if (_selectedPath == null)
            {
                Debug.LogWarning("No road selected");
                return;
            }

            if (GUILayout.Button("Generate waypoints"))
            {
                _editorSave.ViewRoadWaypoints = true;

                if (_selectedPath.nrOfLanes <= 0)
                {
                    Debug.LogError("Nr of lanes has to be >0");
                    return;
                }

                if (_selectedPath.waypointDistance <= 0)
                {
                    Debug.LogError("Waypoint distance needs to be >0");
                    return;
                }

                if (_selectedPath.laneWidth <= 0)
                {
                    Debug.LogError("Lane width has to be >0");
                    return;
                }


                _pedestrianConnectionCreator.DeleteConnectionsWithThisRoad(_selectedPath);
                _pedestrianLaneCreator.GenerateWaypoints(_selectedPath, _window.GetGroundLayer());

                EditorUtility.SetDirty(_selectedPath);
                AssetDatabase.SaveAssets();
            }

            base.BottomPart();
        }


        public override void MouseMove(Vector3 mousePosition)
        {
            if (_selectedPath == null)
            {
                Debug.LogWarning("No road selected");
                return;
            }
            base.MouseMove(mousePosition);
            _pedestrianPathDrawer.SelectSegmentIndex(_selectedPath, mousePosition);
        }


        public override void LeftClick(Vector3 mousePosition, bool clicked)
        {
            if (_selectedPath == null)
            {
                Debug.LogWarning("No road selected");
                return;
            }

            if (clicked == true && _selectedPath.path.IsClosed == false)
            {
                _pedestrianPathDrawer.CloseLoop(_selectedPath);
                return;
            }

            _pedestrianPathDrawer.AddPathPoint(mousePosition, _selectedPath, clicked);
            base.LeftClick(mousePosition, clicked);
        }


        public override void RightClick(Vector3 mousePosition)
        {
            if (_selectedPath == null)
            {
                Debug.LogWarning("No road selected");
                return;
            }
            _pedestrianPathDrawer.Delete(_selectedPath, mousePosition);
            base.RightClick(mousePosition);
        }


        private void ApplyGlobalCarSettings()
        {
            _selectedPath.AllowedPedestrians = _allowedPedestrianIndex;
        }


        public override void DestroyWindow()
        {
            _editorSave.MoveTool = _moveTool;
            _editorSave.GlobalPedestrianList = new List<PedestrianTypes>();
            for (int i = 0; i < _allowedPedestrianIndex.Length; i++)
            {
                if (_allowedPedestrianIndex[i] == true)
                {
                    _editorSave.GlobalPedestrianList.Add((PedestrianTypes)i);
                }
            }
            _editorSave.LaneWidth = _selectedPath.laneWidth;
            _editorSave.WaypointDistance = _selectedPath.waypointDistance;
            
            _pedestrianPathDrawer?.OnDestroy();
            _pedestrianLaneDrawer?.OnDestroy();

            base.DestroyWindow();
        }
    }
}