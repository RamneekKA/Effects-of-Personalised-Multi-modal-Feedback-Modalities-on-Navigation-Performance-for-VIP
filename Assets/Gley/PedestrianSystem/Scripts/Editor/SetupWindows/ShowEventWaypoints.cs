using Gley.UrbanSystem.Editor;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class ShowEventWaypoints : ShowWaypointsWindow
    {
        private readonly float _scrollAdjustment = 161;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            _waypointsOfInterest = _pedestrianWaypointData.GetEventWaypoints();
            return this;
        }


        public override void DrawInScene()
        {
            _pedestrianWaypointDrawer.ShowEventWaypoints(_editorSave.EditorColors.WaypointColor, _editorSave.EditorColors.LabelColor);
            base.DrawInScene();
        }


        protected override void TopPart()
        {
            EditorGUI.BeginChangeCheck();
            _editorSave.EditorColors.WaypointColor = EditorGUILayout.ColorField("Waypoint Color", _editorSave.EditorColors.WaypointColor);
            _editorSave.EditorColors.LabelColor = EditorGUILayout.ColorField("Label Color", _editorSave.EditorColors.LabelColor);
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