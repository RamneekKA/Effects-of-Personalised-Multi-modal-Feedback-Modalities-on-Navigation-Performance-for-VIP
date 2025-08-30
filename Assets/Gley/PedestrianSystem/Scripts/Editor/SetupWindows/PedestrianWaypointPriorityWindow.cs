using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class PedestrianWaypointPriorityWindow : PedestrianSetupWindow
    {
        private readonly float _scrollAdjustment = 104;

        private List<int> _priorities;    
        private PedestrianWaypointEditorData _trafficWaypointData;
        private PedestrianWaypointDrawer _waypointDrawer;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            _trafficWaypointData = new PedestrianWaypointEditorData();
            _waypointDrawer = new PedestrianWaypointDrawer(_trafficWaypointData);
            _priorities = GetDifferentPriorities(_trafficWaypointData.GetAllWaypoints());
            if (_editorSave.PriorityRoutes.RoutesColor.Count < _priorities.Count)
            {
                int nrOfColors = _priorities.Count - _editorSave.PriorityRoutes.RoutesColor.Count;
                for (int i = 0; i < nrOfColors; i++)
                {
                    _editorSave.PriorityRoutes.RoutesColor.Add(Color.white);
                    _editorSave.PriorityRoutes.Active.Add(true);
                }
            }

            _waypointDrawer.OnWaypointClicked += WaypointClicked;
            return this;
        }


        public override void DrawInScene()
        {
            for (int i = 0; i < _priorities.Count; i++)
            {
                if (_editorSave.PriorityRoutes.Active[i])
                {
                    _waypointDrawer.ShowWaypointsWithPriority(_priorities[i], _editorSave.PriorityRoutes.RoutesColor[i]);
                }
            }

            base.DrawInScene();
        }


        protected override void ScrollPart(float width, float height)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.Width(width - SCROLL_SPACE), GUILayout.Height(height - _scrollAdjustment));
            EditorGUILayout.LabelField("Waypoint Priorities: ");
            for (int i = 0; i < _priorities.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(_priorities[i].ToString(), GUILayout.MaxWidth(50));
                _editorSave.PriorityRoutes.RoutesColor[i] = EditorGUILayout.ColorField(_editorSave.PriorityRoutes.RoutesColor[i]);
                Color oldColor = GUI.backgroundColor;
                if (_editorSave.PriorityRoutes.Active[i])
                {
                    GUI.backgroundColor = Color.green;
                }
                if (GUILayout.Button("View"))
                {
                    _editorSave.PriorityRoutes.Active[i] = !_editorSave.PriorityRoutes.Active[i];
                    SceneView.RepaintAll();
                }

                GUI.backgroundColor = oldColor;
                EditorGUILayout.EndHorizontal();
            }

            base.ScrollPart(width, height);
            EditorGUILayout.EndScrollView();
        }


        private List<int> GetDifferentPriorities(PedestrianWaypointSettings[] allWaypoints)
        {
            List<int> result = new List<int>();

            for (int i = 0; i < allWaypoints.Length; i++)
            {
                if (!result.Contains(allWaypoints[i].priority))
                {
                    result.Add(allWaypoints[i].priority);
                }
            }
            return result;
        }


        private void WaypointClicked(PedestrianWaypointSettings clickedWaypoint, bool leftClick)
        {
            _window.SetActiveWindow(typeof(EditPedestrianWaypointWindow), true);
        }


        public override void DestroyWindow()
        {
            if (_waypointDrawer != null)
            {
                _waypointDrawer.OnWaypointClicked -= WaypointClicked;
                _waypointDrawer.OnDestroy();
            }
            base.DestroyWindow();
        }
    }
}
