using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public abstract class ShowWaypointsWindow : PedestrianSetupWindow
    {
        protected PedestrianWaypointSettings[] _waypointsOfInterest;
        protected PedestrianWaypointEditorData _pedestrianWaypointData;
        protected PedestrianWaypointDrawer _pedestrianWaypointDrawer;
        protected int _nrOfWaypoints;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            _pedestrianWaypointData = new PedestrianWaypointEditorData();
            _pedestrianWaypointDrawer = new PedestrianWaypointDrawer(_pedestrianWaypointData);
            _pedestrianWaypointDrawer.OnWaypointClicked += WaypointClicked;
            return this;
        }


        protected override void TopPart()
        {
            base.TopPart();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            _editorSave.ShowConnections = EditorGUILayout.Toggle("Show Connections", _editorSave.ShowConnections, GUILayout.Width(TOGGLE_WIDTH));
            _editorSave.EditorColors.WaypointColor = EditorGUILayout.ColorField(_editorSave.EditorColors.WaypointColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _editorSave.ShowVehicles = EditorGUILayout.Toggle("Show Pedestrian Type", _editorSave.ShowVehicles, GUILayout.Width(TOGGLE_WIDTH));
            _editorSave.EditorColors.AgentColor = EditorGUILayout.ColorField(_editorSave.EditorColors.AgentColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _editorSave.ShowPriority = EditorGUILayout.Toggle("Show Waypoint Priority", _editorSave.ShowPriority, GUILayout.Width(TOGGLE_WIDTH));
            _editorSave.EditorColors.PriorityColor = EditorGUILayout.ColorField(_editorSave.EditorColors.PriorityColor);
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndChangeCheck();
            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }


        protected override void ScrollPart(float width, float height)
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
                EditorGUILayout.LabelField("No " + GetWindowTitle());
            }
            base.ScrollPart(width, height);
        }


        protected void OpenEditWindow(int index)
        {
            PedestrianSettingsWindow.SetSelectedWaypoint(_waypointsOfInterest[index]);
            GleyUtilities.TeleportSceneCamera(_waypointsOfInterest[index].transform.position);
            _window.SetActiveWindow(typeof(EditPedestrianWaypointWindow), true);
        }


        protected virtual void WaypointClicked(WaypointSettingsBase clickedWaypoint, bool leftClick)
        {
            _window.SetActiveWindow(typeof(EditPedestrianWaypointWindow), true);
        }


        public override void DestroyWindow()
        {
            if (_pedestrianWaypointDrawer != null)
            {
                _pedestrianWaypointDrawer.OnWaypointClicked -= WaypointClicked;
                _pedestrianWaypointDrawer.OnDestroy();
            }
            base.DestroyWindow();
        }
    }
}