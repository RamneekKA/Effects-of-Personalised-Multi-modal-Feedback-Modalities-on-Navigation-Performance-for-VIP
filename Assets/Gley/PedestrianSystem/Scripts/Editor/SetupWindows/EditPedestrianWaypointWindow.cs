using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class EditPedestrianWaypointWindow : PedestrianSetupWindow
    {
        protected struct PedestrianDisplay
        {
            public Color Color;
            public int Pedestrian;
            public bool Active;
            public bool View;

            public PedestrianDisplay(bool active, int pedestrian, Color color)
            {
                Active = active;
                Pedestrian = pedestrian;
                Color = color;
                View = false;
            }
        }


        protected enum ListToAdd
        {
            None,
            Neighbors,
        }

        private readonly float _scrollAdjustment = 202;

        private PedestrianDisplay[] _carDisplay;
        private PedestrianWaypointEditorData _pedestrianWaypointData;
        private PedestrianWaypointDrawer _pedestrianWaypointDrawer;
        private PedestrianWaypointSettings _selectedWaypoint;
        private PedestrianWaypointSettings _clickedWaypoint;
        private ListToAdd _selectedList;
        private int _nrOfCars;
        private int _priority;
        private int _penalty;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            _selectedWaypoint = PedestrianSettingsWindow.GetSelectedWaypoint();
            _carDisplay = SetPedestrianDisplay();
            _priority = _selectedWaypoint.priority;
            _penalty = _selectedWaypoint.penalty;
            _pedestrianWaypointData = new PedestrianWaypointEditorData();
            _pedestrianWaypointDrawer = new PedestrianWaypointDrawer(_pedestrianWaypointData);
            _pedestrianWaypointDrawer.OnWaypointClicked += WaypointClicked;

            base.Initialize(windowProperties, window);
            return this;
        }


        public override void DrawInScene()
        {
            base.DrawInScene();

            if (_selectedList != ListToAdd.None)
            {
                _pedestrianWaypointDrawer.DrawWaypointsForLink(_selectedWaypoint, _selectedWaypoint.neighbors, _editorSave.EditorColors.WaypointColor);
            }

            _pedestrianWaypointDrawer.DrawCurrentWaypoint(_selectedWaypoint, _editorSave.EditorColors.SelectedWaypointColor, _editorSave.EditorColors.WaypointColor, _editorSave.EditorColors.PrevWaypointColor);

            for (int i = 0; i < _carDisplay.Length; i++)
            {
                if (_carDisplay[i].View)
                {
                    _pedestrianWaypointDrawer.ShowWaypointsWithPedestrian((PedestrianTypes)_carDisplay[i].Pedestrian, _carDisplay[i].Color);
                }
            }

            if (_clickedWaypoint)
            {
                _pedestrianWaypointDrawer.DrawSelectedWaypoint(_clickedWaypoint, _editorSave.EditorColors.SelectedRoadConnectorColor);
            }
        }


        protected override void TopPart()
        {
            base.TopPart();
            EditorGUI.BeginChangeCheck();
            _editorSave.EditorColors.SelectedWaypointColor = EditorGUILayout.ColorField("Selected Color ", _editorSave.EditorColors.SelectedWaypointColor);
            _editorSave.EditorColors.WaypointColor = EditorGUILayout.ColorField("Neighbor Color ", _editorSave.EditorColors.WaypointColor);
            _editorSave.EditorColors.PrevWaypointColor = EditorGUILayout.ColorField("Previous Color ", _editorSave.EditorColors.PrevWaypointColor);

            EditorGUI.EndChangeCheck();
            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Select Waypoint"))
            {
                Selection.activeGameObject = _selectedWaypoint.gameObject;
            }

            base.TopPart();
        }


        protected override void ScrollPart(float width, float height)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.Width(width - SCROLL_SPACE), GUILayout.Height(height - _scrollAdjustment));
            EditorGUI.BeginChangeCheck();
            if (_selectedList == ListToAdd.None)
            {
                _selectedWaypoint.triggerEvent = EditorGUILayout.Toggle(new GUIContent("Trigger Event", "If a pedestrian reaches this, it will trigger an event"), _selectedWaypoint.triggerEvent);
                if (_selectedWaypoint.triggerEvent == true)
                {
                    _selectedWaypoint.eventData = EditorGUILayout.TextField(new GUIContent("Event Data", "This string will be sent as a parameter for the event"), _selectedWaypoint.eventData);
                }

                EditorGUILayout.BeginHorizontal();
                _priority = EditorGUILayout.IntField(new GUIContent("Spawn priority", "If the priority is higher, the vehicles will have higher chances to spawn on this waypoint"), _priority);
                if (GUILayout.Button("Set Priority"))
                {
                    _selectedWaypoint.SetPriorityForAllNeighbors(_priority);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _penalty = EditorGUILayout.IntField(new GUIContent("Waypoint penalty", "Used for path finding. If penalty is higher vehicles are less likely to pick this route"), _penalty);
                if (GUILayout.Button("Set Penalty "))
                {
                    _selectedWaypoint.SetPenaltyForAllNeighbors(_penalty);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(new GUIContent("Allowed pedestrians: ", "Only the following pedestrians can pass through this waypoint"), EditorStyles.boldLabel);
                EditorGUILayout.Space();

                for (int i = 0; i < _nrOfCars; i++)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    _carDisplay[i].Active = EditorGUILayout.Toggle(_carDisplay[i].Active, GUILayout.MaxWidth(20));
                    EditorGUILayout.LabelField(((PedestrianTypes)i).ToString());
                    _carDisplay[i].Color = EditorGUILayout.ColorField(_carDisplay[i].Color, GUILayout.MaxWidth(80));
                    Color oldColor = GUI.backgroundColor;
                    if (_carDisplay[i].View)
                    {
                        GUI.backgroundColor = Color.green;
                    }
                    if (GUILayout.Button("View", GUILayout.MaxWidth(64)))
                    {
                        _carDisplay[i].View = !_carDisplay[i].View;
                    }
                    GUI.backgroundColor = oldColor;

                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("Set"))
                {
                    SetPedestrians();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space();
            MakeListOperations("Neighbors", "From this waypoint a moving agent can continue to the following ones", _selectedWaypoint.neighbors, ListToAdd.Neighbors);

            EditorGUI.EndChangeCheck();
            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }

            base.ScrollPart(width, height);
            GUILayout.EndScrollView();
        }


        private void DeleteWaypoint(WaypointSettingsBase waypoint, ListToAdd list)
        {
            switch (list)
            {
                case ListToAdd.Neighbors:
                    waypoint.prev.Remove(_selectedWaypoint);
                    _selectedWaypoint.neighbors.Remove(waypoint);
                    break;
            }
            _clickedWaypoint = null;
            SceneView.RepaintAll();
        }


        private void AddNeighbor(WaypointSettingsBase neighbor)
        {
            if (!_selectedWaypoint.neighbors.Contains(neighbor))
            {
                _selectedWaypoint.neighbors.Add(neighbor);
                neighbor.prev.Add(_selectedWaypoint);
            }
            else
            {
                neighbor.prev.Remove(_selectedWaypoint);
                _selectedWaypoint.neighbors.Remove(neighbor);
            }
        }


        private void WaypointClicked(WaypointSettingsBase clickedWaypoint, bool leftClick)
        {
            if (leftClick)
            {
                if (_selectedList == ListToAdd.Neighbors)
                {
                    AddNeighbor(clickedWaypoint);
                }

                if (_selectedList == ListToAdd.None)
                {
                    OpenEditWaypointWindow();
                }
            }
            SettingsWindowBase.TriggerRefreshWindowEvent();
        }


        private void MakeListOperations(string title, string description, List<WaypointSettingsBase> listToEdit, ListToAdd listType)
        {
            if (_selectedList == listType || _selectedList == ListToAdd.None)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(new GUIContent(title, description), EditorStyles.boldLabel);
                EditorGUILayout.Space();
                for (int i = 0; i < listToEdit.Count; i++)
                {
                    if (listToEdit[i] == null)
                    {
                        listToEdit.RemoveAt(i);
                        i--;
                        continue;
                    }
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(listToEdit[i].name);
                    Color oldColor = GUI.backgroundColor;
                    if (listToEdit[i] == _clickedWaypoint)
                    {
                        GUI.backgroundColor = Color.green;
                    }
                    if (GUILayout.Button("View", GUILayout.MaxWidth(64)))
                    {
                        if (listToEdit[i] == _clickedWaypoint)
                        {
                            _clickedWaypoint = null;
                        }
                        else
                        {
                            ViewWaypoint(listToEdit[i]);
                        }
                    }
                    GUI.backgroundColor = oldColor;
                    if (GUILayout.Button("Delete", GUILayout.MaxWidth(64)))
                    {
                        DeleteWaypoint(listToEdit[i], listType);
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.Space();
                if (_selectedList == ListToAdd.None)
                {
                    if (GUILayout.Button("Add/Remove " + title))
                    {
                        //baseWaypointDrawer.Initialize();
                        _selectedList = listType;
                    }
                }
                else
                {
                    if (GUILayout.Button("Done"))
                    {
                        _selectedList = ListToAdd.None;
                        SceneView.RepaintAll();
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
            }
        }


        private PedestrianDisplay[] SetPedestrianDisplay()
        {
            _nrOfCars = System.Enum.GetValues(typeof(PedestrianTypes)).Length;
            PedestrianDisplay[] carDisplay = new PedestrianDisplay[_nrOfCars];
            for (int i = 0; i < _nrOfCars; i++)
            {
                carDisplay[i] = new PedestrianDisplay(_selectedWaypoint.AllowedPedestrians.Contains((PedestrianTypes)i), i, Color.white);
            }
            return carDisplay;
        }


        private void ViewWaypoint(WaypointSettingsBase waypoint)
        {
            _clickedWaypoint = (PedestrianWaypointSettings)waypoint;
            GleyUtilities.TeleportSceneCamera(waypoint.transform.position);
        }


        private void SetPedestrians()
        {
            List<PedestrianTypes> result = new List<PedestrianTypes>();
            for (int i = 0; i < _carDisplay.Length; i++)
            {
                if (_carDisplay[i].Active)
                {
                    result.Add((PedestrianTypes)_carDisplay[i].Pedestrian);
                }
            }
            _selectedWaypoint.SetPedestrianTypesForAllNeighbors(result);
        }


        private void OpenEditWaypointWindow()
        {
            _window.SetActiveWindow(typeof(EditPedestrianWaypointWindow), false);
        }


        public override void DestroyWindow()
        {
            if (_pedestrianWaypointDrawer != null)
            {
                _pedestrianWaypointDrawer.OnWaypointClicked -= WaypointClicked;
                _pedestrianWaypointDrawer.OnDestroy();
            }

            if (_selectedWaypoint)
            {
                EditorUtility.SetDirty(_selectedWaypoint);
            }
            base.DestroyWindow();
        }
    }
}
