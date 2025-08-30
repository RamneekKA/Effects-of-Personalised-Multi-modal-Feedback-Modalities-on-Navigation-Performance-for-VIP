using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class ViewPathsWindow : PedestrianSetupWindow
    {
        private readonly float _scrollAdjustment = 251;

        private List<PedestrianPath> _roadsOfInterest;
        private PedestrianPathCreator _pedestrianPathCreator;
        private PedestrianPathDrawer _pedestrianPathDrawer;
        private PedestrianPathEditorData _pedestrianPathData;
        private PedestrianLaneEditorData _pedestrianLaneData;
        private PedestrianLaneDrawer _pedestrianLaneDrawer;
        private PedestrianConnectionCreator _pedestrianConnectionCreator;
        private PedestrianConnectionEditorData _pedestrianConnectionData;
        private PedestrianWaypointCreator _pedestrianWaypointCreator;
        private string _drawButton = "Draw All Paths";
        private bool _drawAllRoads;
        private int _nrOfRoads;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            _pedestrianPathData = new PedestrianPathEditorData();
            _pedestrianLaneData = new PedestrianLaneEditorData(_pedestrianPathData);
            _pedestrianConnectionData = new PedestrianConnectionEditorData(_pedestrianPathData);

            _pedestrianWaypointCreator =new PedestrianWaypointCreator();
            _pedestrianPathCreator = new PedestrianPathCreator(_pedestrianPathData);
            _pedestrianConnectionCreator = new PedestrianConnectionCreator(_pedestrianConnectionData, _pedestrianWaypointCreator);

            _pedestrianPathDrawer = new PedestrianPathDrawer(_pedestrianPathData);
            _pedestrianLaneDrawer = new PedestrianLaneDrawer(_pedestrianLaneData);

            return base.Initialize(windowProperties, window);
        }


        public override void DrawInScene()
        {
            base.DrawInScene();

            _roadsOfInterest = _pedestrianPathDrawer.ShowAllRoads(MoveTools.None, _editorSave.EditorColors.RoadColor, _editorSave.EditorColors.AnchorPointColor, _editorSave.EditorColors.ControlPointColor, _editorSave.EditorColors.LabelColor, _editorSave.ViewLabels);

            if (_roadsOfInterest.Count != _nrOfRoads)
            {
                _nrOfRoads = _roadsOfInterest.Count;
                SettingsWindowBase.TriggerRefreshWindowEvent();
            }


            for (int i = 0; i < _nrOfRoads; i++)
            {
                if (_editorSave.ViewRoadWaypoints)
                {
                    _pedestrianLaneDrawer.DrawLaneWaypoints(_roadsOfInterest[i], _editorSave.EditorColors.WaypointColor, _editorSave.EditorColors.DisconnectedColor);
                }
                if (_editorSave.ViewRoadLanes)
                {
                    _pedestrianLaneDrawer.DrawPathWidth(_roadsOfInterest[i], _editorSave.EditorColors.LaneColor);
                }
            }
        }


        protected override void TopPart()
        {
            base.TopPart();
            if (GUILayout.Button(_drawButton))
            {
                _drawAllRoads = !_drawAllRoads;
                if (_drawAllRoads == true)
                {
                    _drawButton = "Clear All";
                }
                else
                {
                    _drawButton = "Draw All Paths";
                }

                _pedestrianPathDrawer.SetDrawProperty(_drawAllRoads);
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            _editorSave.EditorColors.RoadColor = EditorGUILayout.ColorField("Path Color", _editorSave.EditorColors.RoadColor);

            if (_editorSave.ViewLabels)
            {
                EditorGUILayout.BeginHorizontal();
                _editorSave.ViewLabels = EditorGUILayout.Toggle("View Labels", _editorSave.ViewLabels, GUILayout.Width(TOGGLE_WIDTH));
                _editorSave.EditorColors.LabelColor = EditorGUILayout.ColorField(_editorSave.EditorColors.LabelColor);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                _editorSave.ViewLabels = EditorGUILayout.Toggle("View Labels", _editorSave.ViewLabels);
            }

            if (_editorSave.ViewRoadLanes)
            {
                EditorGUILayout.BeginHorizontal();
                _editorSave.ViewRoadLanes = EditorGUILayout.Toggle("View Path Width", _editorSave.ViewRoadLanes, GUILayout.Width(TOGGLE_WIDTH));
                _editorSave.EditorColors.LaneColor = EditorGUILayout.ColorField(_editorSave.EditorColors.LaneColor);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                _editorSave.ViewRoadLanes = EditorGUILayout.Toggle("View Path Width", _editorSave.ViewRoadLanes);
            }


            if (_editorSave.ViewRoadWaypoints)
            {
                EditorGUILayout.BeginHorizontal();
                _editorSave.ViewRoadWaypoints = EditorGUILayout.Toggle("View Waypoints", _editorSave.ViewRoadWaypoints, GUILayout.Width(TOGGLE_WIDTH));
                _editorSave.EditorColors.WaypointColor = EditorGUILayout.ColorField(_editorSave.EditorColors.WaypointColor);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                _editorSave.ViewRoadWaypoints = EditorGUILayout.Toggle("View Waypoints", _editorSave.ViewRoadWaypoints);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select All"))
            {
                foreach (var conn in _roadsOfInterest)
                {
                    conn.draw = true;
                }
            }

            if (GUILayout.Button("Deselect All"))
            {
                foreach (var conn in _roadsOfInterest)
                {
                    conn.draw = false;
                }
            }

            if (GUILayout.Button("Inverse Select"))
            {
                foreach (var conn in _roadsOfInterest)
                {
                    conn.draw = !conn.draw;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndChangeCheck();

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
            EditorGUILayout.Space();
        }


        protected override void ScrollPart(float width, float height)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.Width(width - SCROLL_SPACE), GUILayout.Height(height - _scrollAdjustment));

            if (_roadsOfInterest != null)
            {
                if (_roadsOfInterest.Count == 0)
                {
                    EditorGUILayout.LabelField("Nothing in view");
                }
                for (int i = 0; i < _roadsOfInterest.Count; i++)
                {
                    DisplayPath(_roadsOfInterest[i]);
                }
            }
            GUILayout.EndScrollView();
        }


        private void DisplayPath(PedestrianPath path)
        {
            if (path == null)
                return;

            EditorGUILayout.BeginHorizontal();
            path.draw = EditorGUILayout.Toggle(path.draw, GUILayout.Width(TOGGLE_DIMENSION));
            GUILayout.Label(path.gameObject.name);

            if (GUILayout.Button("View"))
            {
                GleyUtilities.TeleportSceneCamera(path.transform.position);
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("Select"))
            {
                SelectWaypoint(path);
            }
            if (GUILayout.Button("Delete"))
            {
                EditorGUI.BeginChangeCheck();
                if (EditorUtility.DisplayDialog("Delete " + path.name + "?", "Are you sure you want to delete " + path.name + "? \nYou cannot undo this operation.", "Delete", "Cancel"))
                {
                    DeleteCurrentPath(path);
                }
                EditorGUI.EndChangeCheck();
            }

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }


        private void SelectWaypoint(PedestrianPath road)
        {
            PedestrianSettingsWindow.SetSelectedPath(road);
            _window.SetActiveWindow(typeof(EditPathWindow), true);
        }


        private void DeleteCurrentPath(PedestrianPath path)
        {
            _pedestrianConnectionCreator.DeleteConnectionsWithThisRoad(path);
            _pedestrianPathCreator.DeleteCurrentRoad(path);
            Undo.undoRedoPerformed += UndoPerformed;
        }


        private void UndoPerformed()
        {
            _pedestrianPathData.TriggerOnModifiedEvent();
            Undo.undoRedoPerformed -= UndoPerformed;
        }


        public override void DestroyWindow()
        {
            Undo.undoRedoPerformed -= UndoPerformed;
            _pedestrianPathDrawer?.OnDestroy();
            _pedestrianLaneDrawer?.OnDestroy();

            base.DestroyWindow();
        }
    }
}