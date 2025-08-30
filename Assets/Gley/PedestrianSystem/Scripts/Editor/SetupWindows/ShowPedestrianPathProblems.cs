using Gley.UrbanSystem.Editor;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class ShowPedestrianPathProblems : ShowWaypointsWindow
    {
        private readonly float _scrollAdjustment = 150;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            return this;
        }


        public override void DrawInScene()
        {
            _waypointsOfInterest = _pedestrianWaypointDrawer.ShowVehicleProblems(_editorSave.EditorColors.WaypointColor, _editorSave.EditorColors.AgentColor);

            if (_waypointsOfInterest.Length != _nrOfWaypoints)
            {
                _nrOfWaypoints = _waypointsOfInterest.Length;
                SettingsWindowBase.TriggerRefreshWindowEvent();
            }
            base.DrawInScene();
        }


        protected override void TopPart()
        {
            EditorGUI.BeginChangeCheck();
            _editorSave.EditorColors.WaypointColor = EditorGUILayout.ColorField("Waypoint Color",_editorSave.EditorColors.WaypointColor);
            _editorSave.EditorColors.AgentColor = EditorGUILayout.ColorField("Pedestrian Type Color",_editorSave.EditorColors.AgentColor);
            EditorGUI.EndChangeCheck();

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }


        protected override void ScrollPart(float width, float height)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.Width(width - SCROLL_SPACE), GUILayout.Height(height - _scrollAdjustment));
            base.ScrollPart(width, height);
            GUILayout.EndScrollView();
        }
    }
}