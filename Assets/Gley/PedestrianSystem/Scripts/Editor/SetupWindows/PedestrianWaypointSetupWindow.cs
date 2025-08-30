using Gley.UrbanSystem.Editor;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    internal class PedestrianWaypointSetupWindow : SetupWindowBase
    {
        protected override void ScrollPart(float width, float height)
        {
            EditorGUILayout.LabelField("Select action:");
            EditorGUILayout.Space();

            if (GUILayout.Button("Show All Waypoints"))
            {
                _window.SetActiveWindow(typeof(ShowAllWaypoints), true);

            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Show Disconnected Waypoints"))
            {
                _window.SetActiveWindow(typeof(ShowDisconnectedWaypoints), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Show Pedestrian Type Edited Waypoints"))
            {
                _window.SetActiveWindow(typeof(ShowPedestrianTypeEditedWaypoints), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Show Priority Edited Waypoints"))
            {
                _window.SetActiveWindow(typeof(ShowPriorityEditedWaypoints), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Show Event Waypoints"))
            {
                _window.SetActiveWindow(typeof(ShowEventWaypoints), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Show Pedestrian Path Problems"))
            {
                _window.SetActiveWindow(typeof(ShowPedestrianPathProblems), true);
            }
            EditorGUILayout.Space();

            base.ScrollPart(width, height);
        }
    }
}