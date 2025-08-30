using UnityEngine;
using Gley.UrbanSystem.Editor;
using UnityEditor;

namespace Gley.PedestrianSystem.Editor
{
    public class ShowDisconnectedWaypoints : ShowWaypointsWindow
    {
        private readonly float _scrollAdjustment = 141;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            _waypointsOfInterest = _pedestrianWaypointData.GetDisconnectedWaypoints();
            return this;
        }


        public override void DrawInScene()
        {
            _pedestrianWaypointDrawer.ShowDisconnectedWaypoints(_editorSave.EditorColors.WaypointColor);
            base.DrawInScene();
        }


        protected override void TopPart()
        {
            EditorGUI.BeginChangeCheck();
            _editorSave.EditorColors.WaypointColor = EditorGUILayout.ColorField("Waypoint Color", _editorSave.EditorColors.WaypointColor);
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