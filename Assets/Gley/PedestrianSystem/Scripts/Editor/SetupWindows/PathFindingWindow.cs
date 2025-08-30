using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class PathFindingWindow : PedestrianSetupWindow
    {
        private readonly float _scrollAdjustment = 171;

        private PedestrianWaypointSettings[] _waypointsOfInterest;
        private List<int> _penalties;
        private PedestrianWaypointEditorData _pedestrianWaypointData;
        private PedestrianWaypointDrawer _pedestrianWaypointDrawer;
        private bool _showPenaltyEditedWaypoints;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            _pedestrianWaypointData = new PedestrianWaypointEditorData();
            _pedestrianWaypointDrawer = new PedestrianWaypointDrawer(_pedestrianWaypointData);
            _waypointsOfInterest = _pedestrianWaypointData.GetPenlatyEditedWaypoints();
            _penalties = GetDifferentPenalties(_pedestrianWaypointData.GetAllWaypoints());
            if (_editorSave.PathFindingRoutes.RoutesColor.Count < _penalties.Count)
            {
                int nrOfColors = _penalties.Count - _editorSave.PathFindingRoutes.RoutesColor.Count;
                for (int i = 0; i < nrOfColors; i++)
                {
                    _editorSave.PathFindingRoutes.RoutesColor.Add(Color.white);
                    _editorSave.PathFindingRoutes.Active.Add(true);
                }
            }
            _pedestrianWaypointDrawer.OnWaypointClicked += WaypointClicked;
            return this;
        }


        public override void DrawInScene()
        {
            if (_showPenaltyEditedWaypoints)
            {
                _pedestrianWaypointDrawer.ShowPenaltyEditedWaypoints(_editorSave.EditorColors.WaypointColor);
            }
            else
            {
                for (int i = 0; i < _penalties.Count; i++)
                {
                    if (_editorSave.PathFindingRoutes.Active[i])
                    {
                        _pedestrianWaypointDrawer.ShowWaypointsWithPenalty(_penalties[i], _editorSave.PathFindingRoutes.RoutesColor[i]);
                    }
                }
            }
            base.DrawInScene();
        }


        protected override void TopPart()
        {
            base.TopPart();
            _editorSave.PathFindingEnabled = EditorGUILayout.Toggle("Enable Path Finding", _editorSave.PathFindingEnabled);

            EditorGUI.BeginChangeCheck();
            _showPenaltyEditedWaypoints = EditorGUILayout.Toggle("Show Edited Waypoints", _showPenaltyEditedWaypoints);

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }


        protected override void ScrollPart(float width, float height)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.Width(width - SCROLL_SPACE), GUILayout.Height(height - _scrollAdjustment));
            if (_showPenaltyEditedWaypoints)
            {
                if (_waypointsOfInterest != null)
                {
                    if (_waypointsOfInterest.Length == 0)
                    {
                        EditorGUILayout.LabelField("No " + GetWindowTitle());
                    }
                    for (int i = 0; i < _waypointsOfInterest.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        EditorGUILayout.LabelField(_waypointsOfInterest[i].name);
                        if (GUILayout.Button("View", GUILayout.Width(BUTTON_DIMENSION)))
                        {
                            GleyUtilities.TeleportSceneCamera(_waypointsOfInterest[i].transform.position);
                            SceneView.RepaintAll();
                        }
                        if (GUILayout.Button("Edit", GUILayout.Width(BUTTON_DIMENSION)))
                        {
                            OpenEditWindow(i);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No priority edited waypoints");
                }
            }
            else
            {
                EditorGUILayout.LabelField("Waypoint Penalties: ");
                for (int i = 0; i < _penalties.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(_penalties[i].ToString(), GUILayout.MaxWidth(50));
                    _editorSave.PathFindingRoutes.RoutesColor[i] = EditorGUILayout.ColorField(_editorSave.PathFindingRoutes.RoutesColor[i]);
                    Color oldColor = GUI.backgroundColor;
                    if (_editorSave.PathFindingRoutes.Active[i])
                    {
                        GUI.backgroundColor = Color.green;
                    }
                    if (GUILayout.Button("View"))
                    {
                        _editorSave.PathFindingRoutes.Active[i] = !_editorSave.PathFindingRoutes.Active[i];
                        SceneView.RepaintAll();
                    }

                    GUI.backgroundColor = oldColor;
                    EditorGUILayout.EndHorizontal();
                }
            }
            base.ScrollPart(width, height);
            EditorGUILayout.EndScrollView();
        }


        protected override void BottomPart()
        {
            if (GUILayout.Button("Save"))
            {
                Save();
            }
            base.BottomPart();
        }


        private void OpenEditWindow(int index)
        {
            PedestrianSettingsWindow.SetSelectedWaypoint((PedestrianWaypointSettings)_waypointsOfInterest[index]);
            GleyUtilities.TeleportSceneCamera(_waypointsOfInterest[index].transform.position);
            _window.SetActiveWindow(typeof(EditPedestrianWaypointWindow), true);
        }


        private List<int> GetDifferentPenalties(PedestrianWaypointSettings[] allWaypoints)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (!result.Contains(allWaypoints[i].penalty))
                {
                    result.Add(allWaypoints[i].penalty);
                }
            }
            return result;
        }


        private void WaypointClicked(WaypointSettingsBase clickedWaypoint, bool leftClick)
        {
            _window.SetActiveWindow(typeof(EditPedestrianWaypointWindow), true);
        }


        private void Save()
        {
            Debug.Log("Save");
        }


        public override void DestroyWindow()
        {
            if (_pedestrianWaypointDrawer != null)
            {
                _pedestrianWaypointDrawer.OnWaypointClicked -= WaypointClicked;
            }
            base.DestroyWindow();
        }
    }
}
