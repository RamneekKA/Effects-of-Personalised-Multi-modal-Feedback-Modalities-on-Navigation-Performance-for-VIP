using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class CreatePathWindow : PedestrianSetupWindow
    {
        private List<PedestrianPath> _pathsOfInterest;
        private PedestrianPathCreator _pedestrianPathCreator;
        private PedestrianPathEditorData _pedestrianPathData;
        private PedestrianPathDrawer _pedestrianPathDrawer;
        private PedestrianLaneEditorData _pedestrianLaneData;
        private PedestrianLaneDrawer _pedestrianLaneDrawer;
        private Vector3 _firstClick;
        private Vector3 _secondClick;
        private int _nrOfPaths;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            _pedestrianPathData = new PedestrianPathEditorData();
            _pedestrianLaneData = new PedestrianLaneEditorData(_pedestrianPathData);

            _pedestrianPathCreator = new PedestrianPathCreator(_pedestrianPathData);

            _pedestrianPathDrawer = new PedestrianPathDrawer(_pedestrianPathData);
            _pedestrianLaneDrawer = new PedestrianLaneDrawer(_pedestrianLaneData);

            base.Initialize(windowProperties, window);
            return this;
        }


        public override void DrawInScene()
        {
            if (_firstClick != Vector3.zero)
            {
                Handles.SphereHandleCap(0, _firstClick, Quaternion.identity, 1, EventType.Repaint);
            }

            if (_editorSave.ViewOtherRoads)
            {
                _pathsOfInterest = _pedestrianPathDrawer.ShowAllRoads(MoveTools.None, _editorSave.EditorColors.RoadColor, _editorSave.EditorColors.AnchorPointColor, _editorSave.EditorColors.ControlPointColor, _editorSave.EditorColors.LabelColor, _editorSave.ViewLabels);
                if (_pathsOfInterest.Count != _nrOfPaths)
                {
                    _nrOfPaths = _pathsOfInterest.Count;
                    SettingsWindowBase.TriggerRefreshWindowEvent();
                }
                if (_editorSave.ViewRoadLanes)
                {
                    for (int i = 0; i < _nrOfPaths; i++)
                    {
                        if (_editorSave.ViewRoadWaypoints)
                        {
                            _pedestrianLaneDrawer.DrawLaneWaypoints(_pathsOfInterest[i], _editorSave.EditorColors.WaypointColor, _editorSave.EditorColors.DisconnectedColor);
                        }
                        else
                        {
                            _pedestrianLaneDrawer.DrawPathWidth(_pathsOfInterest[i], _editorSave.EditorColors.LaneColor);
                        }
                    }
                }

            }
            base.DrawInScene();
        }


        protected override void TopPart()
        {
            base.TopPart();
            EditorGUILayout.LabelField("Press SHIFT + Left Click to add a path point");
            EditorGUILayout.LabelField("Press SHIFT + Right Click to remove a path point");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("If you are not able to draw, make sure your ground/sidewalk is on the layer marked as Ground inside Layer Setup");
        }


        protected override void ScrollPart(float width, float height)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            _editorSave.ViewOtherRoads = EditorGUILayout.Toggle("View Other Paths", _editorSave.ViewOtherRoads, GUILayout.Width(TOGGLE_WIDTH));
            _editorSave.EditorColors.RoadColor = EditorGUILayout.ColorField(_editorSave.EditorColors.RoadColor);
            EditorGUILayout.EndHorizontal();

            if (_editorSave.ViewOtherRoads)
            {
                EditorGUILayout.BeginHorizontal();
                _editorSave.ViewLabels = EditorGUILayout.Toggle("View Labels", _editorSave.ViewLabels, GUILayout.Width(TOGGLE_WIDTH));
                _editorSave.EditorColors.LabelColor = EditorGUILayout.ColorField(_editorSave.EditorColors.LabelColor);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _editorSave.ViewRoadLanes = EditorGUILayout.Toggle("View Path Width", _editorSave.ViewRoadLanes, GUILayout.Width(TOGGLE_WIDTH));
                _editorSave.EditorColors.LaneColor = EditorGUILayout.ColorField(_editorSave.EditorColors.LaneColor);
                EditorGUILayout.EndHorizontal();

                if (_editorSave.ViewRoadLanes)
                {
                    EditorGUILayout.BeginHorizontal();
                    _editorSave.ViewRoadWaypoints = EditorGUILayout.Toggle("View Waypoints", _editorSave.ViewRoadWaypoints, GUILayout.Width(TOGGLE_WIDTH));
                    _editorSave.EditorColors.WaypointColor = EditorGUILayout.ColorField(_editorSave.EditorColors.WaypointColor);
                    _editorSave.EditorColors.DisconnectedColor = EditorGUILayout.ColorField(_editorSave.EditorColors.DisconnectedColor);
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUI.EndChangeCheck();

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
            base.ScrollPart(width, height);
        }


        public override void LeftClick(Vector3 mousePosition, bool clicked)
        {
            if (_firstClick == Vector3.zero)
            {
                _firstClick = mousePosition;
            }
            else
            {
                _secondClick = mousePosition;
                CreatePath();
            }
            base.LeftClick(mousePosition, clicked);
        }


        public override void UndoAction()
        {
            base.UndoAction();
            if (_secondClick == Vector3.zero)
            {
                if (_firstClick != Vector3.zero)
                {
                    _firstClick = Vector3.zero;
                }
            }
        }


        private void CreatePath()
        {
            PedestrianPath selectedRoad = _pedestrianPathCreator.Create(
                1,
                _editorSave.LaneWidth,
                _editorSave.WaypointDistance, PedestrianSystemConstants.PathName,
                _firstClick,
                _secondClick,
                0,
                System.Enum.GetValues(typeof(PedestrianTypes)).Length,
                false
                );
            PedestrianSettingsWindow.SetSelectedPath(selectedRoad);
            _window.SetActiveWindow(typeof(EditPathWindow), false);
            _firstClick = Vector3.zero;
            _secondClick = Vector3.zero;
        }


        public override void DestroyWindow()
        {
            _pedestrianPathDrawer?.OnDestroy();
            _pedestrianLaneDrawer?.OnDestroy();
            base.DestroyWindow();
        }
    }
}