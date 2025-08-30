using Gley.UrbanSystem.Editor;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class ShowPedestrianTypeEditedWaypoints : ShowWaypointsWindow
    {
        private readonly float _scrollAdjustment = 161;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            _waypointsOfInterest = _pedestrianWaypointData.GetPedestrianTypeEditedWaypoints();
            return this;
        }


        protected override void TopPart()
        {
            EditorGUI.BeginChangeCheck();
            _editorSave.EditorColors.WaypointColor = EditorGUILayout.ColorField("Waypoint Color", _editorSave.EditorColors.WaypointColor);
            _editorSave.EditorColors.AgentColor = EditorGUILayout.ColorField("Pedestrian Type Color", _editorSave.EditorColors.AgentColor);
            EditorGUI.EndChangeCheck();

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }


        public override void DrawInScene()
        {
            _pedestrianWaypointDrawer.ShowPedestrianTypeEditedWaypoints(_editorSave.EditorColors.WaypointColor, _editorSave.EditorColors.AgentColor);
            base.DrawInScene();
        }


        protected override void ScrollPart(float width, float height)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.Width(width - SCROLL_SPACE), GUILayout.Height(height - _scrollAdjustment));
            base.ScrollPart(width, height);
            GUILayout.EndScrollView();
        }
    }
}