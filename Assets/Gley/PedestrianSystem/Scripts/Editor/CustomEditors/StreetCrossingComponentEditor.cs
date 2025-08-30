using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    /// <summary>
    /// Custom editor for the street crossing component.
    /// </summary>
    [CustomEditor(typeof(StreetCrossingComponent))]
    internal class StreetCrossingComponentEditor : UnityEditor.Editor
    {
        // Private variable names from StreetCrossingComponent
        private readonly string _stopWaypointsFieldName = "_stopWaypoints";
        private readonly string _directionWaypointsFieldName = "_directionWaypoints";
        private readonly string _crossingNameFieldName = "_crossingName";

        private List<PedestrianWaypointSettings> _stopWaypoints;
        private List<PedestrianWaypointSettings> _directionWaypoints;
        private PedestrianWaypointEditorData _pedestrianWaypointData;
        private PedestrianWaypointDrawer _pedestrianWaypointDrawer;
        private StreetCrossingComponent _targetScript;
        private PedestrianSettingsWindowData _editorSave;
        private bool _addStopWaypoints;
        private bool _addDirectionWaypoints;


        private void OnEnable()
        {
            _targetScript = (StreetCrossingComponent)target;
            _editorSave = new SettingsLoader(PedestrianSystemConstants.WindowSettingsPath).LoadSettingsAsset<PedestrianSettingsWindowData>();
            _pedestrianWaypointData = new PedestrianWaypointEditorData();
            _pedestrianWaypointDrawer = new PedestrianWaypointDrawer(_pedestrianWaypointData);

            _pedestrianWaypointDrawer.OnWaypointClicked += WaypointClicked;
            if (string.IsNullOrEmpty(_targetScript.CrossingName))
            {
                CustomEditorMethods.GetPrivateField<StreetCrossingComponent>(_crossingNameFieldName).SetValue(_targetScript, _targetScript.gameObject.name);
            }

            _stopWaypoints = (List<PedestrianWaypointSettings>)CustomEditorMethods.GetPrivateField<StreetCrossingComponent>(_stopWaypointsFieldName).GetValue(_targetScript);
            _directionWaypoints = (List<PedestrianWaypointSettings>)CustomEditorMethods.GetPrivateField<StreetCrossingComponent>(_directionWaypointsFieldName).GetValue(_targetScript);
        }


        private void OnDisable()
        {
            _pedestrianWaypointDrawer.OnWaypointClicked -= WaypointClicked;
        }


        private void OnSceneGUI()
        {
            if (EditorApplication.isPlaying)
                return;
            if (_addStopWaypoints)
            {
                _pedestrianWaypointDrawer.ShowIntersectionWaypoints(_editorSave.EditorColors.WaypointColor);
            }
            else
            {
                if (_addDirectionWaypoints)
                {
                    _pedestrianWaypointDrawer.DrawPossibleDirectionWaypoints(_stopWaypoints, _editorSave.EditorColors.WaypointColor);

                    for (int j = 0; j < _directionWaypoints.Count; j++)
                    {
                        _pedestrianWaypointDrawer.DrawDirectionWaypoint(_directionWaypoints[j], _editorSave.EditorColors.WaypointColor);
                    }
                }
                Handles.color = _editorSave.EditorColors.WaypointColor;
            }

            for (int i = _stopWaypoints.Count - 1; i >= 0; i--)
            {
                if (_stopWaypoints[i] != null)
                {
                    if (_stopWaypoints[i].draw)
                    {
                        Handles.color = Color.red;
                        Handles.DrawSolidDisc(_stopWaypoints[i].transform.position, Vector3.up, 1);
                    }
                }
                else
                {
                    _stopWaypoints.RemoveAt(i);
                }
            }

            for (int i = _directionWaypoints.Count - 1; i >= 0; i--)
            {
                if (_directionWaypoints[i] != null)
                {
                    if (_directionWaypoints[i].draw)
                    {
                        Handles.color = Color.green;
                        Handles.DrawSolidDisc(_directionWaypoints[i].transform.position, Vector3.up, 1);
                    }
                }
                else
                {
                    _directionWaypoints.RemoveAt(i);
                }
            }
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (_addStopWaypoints == false && _addDirectionWaypoints == false)
            {
                if (GUILayout.Button("Add Stop Waypoints"))
                {
                    AddStopWaypoints();
                }

                if (GUILayout.Button("Add Direction Waypoints"))
                {
                    AddDirectionWaypoints();
                }
            }
            else
            {
                if (GUILayout.Button("Done"))
                {
                    _addStopWaypoints = _addDirectionWaypoints = false;
                    CustomEditorMethods.GetPrivateField<StreetCrossingComponent>(_stopWaypointsFieldName).SetValue(_targetScript, _stopWaypoints);
                    CustomEditorMethods.GetPrivateField<StreetCrossingComponent>(_directionWaypointsFieldName).SetValue(_targetScript, _directionWaypoints);
                    EditorUtility.SetDirty(_targetScript);
                    SceneView.RepaintAll();
                }
            }
        }


        private void WaypointClicked(WaypointSettingsBase clickedWaypoint, bool leftClick)
        {
            if (_addStopWaypoints)
            {
                if (!_stopWaypoints.Contains((PedestrianWaypointSettings)clickedWaypoint))
                {
                    clickedWaypoint.draw = true;
                    _stopWaypoints.Add((PedestrianWaypointSettings)clickedWaypoint);
                }
                else
                {
                    _stopWaypoints.Remove((PedestrianWaypointSettings)clickedWaypoint);
                    _targetScript.VerifyAssignments();
                }
            }
            if (_addDirectionWaypoints)
            {
                if (!_directionWaypoints.Contains((PedestrianWaypointSettings)clickedWaypoint))
                {
                    clickedWaypoint.draw = true;
                    _directionWaypoints.Add((PedestrianWaypointSettings)clickedWaypoint);
                }
                else
                {
                    _directionWaypoints.Remove((PedestrianWaypointSettings)clickedWaypoint);
                }
            }
        }


        private void AddStopWaypoints()
        {
            _addStopWaypoints = true;
            for (int i = _stopWaypoints.Count - 1; i >= 0; i--)
            {
                if (_stopWaypoints[i] == null)
                {
                    DeleteStop(i);
                }
            }
            SceneView.RepaintAll();
        }


        private void AddDirectionWaypoints()
        {
            _addDirectionWaypoints = true;
            for (int i = _directionWaypoints.Count - 1; i >= 0; i--)
            {
                if (_directionWaypoints[i] == null)
                {
                    DeleteDirection(i);
                }
            }
            SceneView.RepaintAll();
        }


        private void DeleteStop(int i)
        {
            _stopWaypoints.RemoveAt(i);
            EditorUtility.SetDirty(_targetScript);
            SceneView.RepaintAll();
        }


        private void DeleteDirection(int i)
        {
            _directionWaypoints.RemoveAt(i);
            EditorUtility.SetDirty(_targetScript);
            SceneView.RepaintAll();
        }


        private void OnDestroy()
        {
            _pedestrianWaypointDrawer.OnDestroy();
        }
    }
}
