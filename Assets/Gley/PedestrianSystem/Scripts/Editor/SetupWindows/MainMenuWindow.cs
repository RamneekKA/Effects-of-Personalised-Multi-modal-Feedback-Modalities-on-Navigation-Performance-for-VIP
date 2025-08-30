using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using System.IO;
using UnityEditor;
using UnityEngine;
#if GLEY_TRAFFIC_SYSTEM
using Gley.TrafficSystem.Editor;
#endif

namespace Gley.PedestrianSystem.Editor
{
    public class MainMenuWindow : SetupWindowBase
    {
        private const string SAVING = "GleyPedestrianSaving";
        private const string STEP = "GleyPedestrianStep";
#if GLEY_PEDESTRIAN_SYSTEM
        private const int _scrollAdjustment = 140;
#else
        private const int _scrollAdjustment = 90;
#endif
        private int _step;
        private bool _saving;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            _saving = EditorPrefs.GetBool(SAVING);
            _step = EditorPrefs.GetInt(STEP);
            return base.Initialize(windowProperties, window);
        }


        public override void InspectorUpdate()
        {
            if (_saving)
            {
                if (EditorApplication.isCompiling == false)
                {
                    _saving = false;
                    EditorPrefs.SetBool(SAVING, false);
                    SaveSettings();
                }
            }
        }


        protected override void ScrollPart(float width, float height)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.Width(width - SCROLL_SPACE), GUILayout.Height(height - _scrollAdjustment));

#if GLEY_PEDESTRIAN_SYSTEM
            InitializedScrollButtons();
#else
            NotInitializedScrollButtons();
#endif

            EditorGUILayout.EndScrollView();
            base.ScrollPart(width, height);
        }

        private void NotInitializedScrollButtons()
        {
            GUILayout.Label("Installation Instructions");
            GUILayout.Label("Step 1: Import Packages -> This will import the latest version of burst compiler", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            if (GUILayout.Button("Import Required Packages"))
            {
                _window.SetActiveWindow(typeof(ImportPackagesWindow), true);
            }
            EditorGUILayout.Space();

            GUILayout.Label("Step 2: Enable Pedestrian System -> This will enable the Pedestrian System scripts:", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            if (GUILayout.Button("Enable Pedestrian System"))
            {
                if (!File.Exists($"{Application.dataPath}{Internal.PedestrianSystemConstants.AgentTypesPath}/PedestrianTypes.cs"))
                {
                    FileCreator.CreateAgentTypesFile<PedestrianTypes>(null, Internal.PedestrianSystemConstants.GLEY_PEDESTRIAN_SYSTEM, Internal.PedestrianSystemConstants.PedestrianNamespace, Internal.PedestrianSystemConstants.AgentTypesPath);
                }

                Common.PreprocessorDirective.AddToCurrent(PedestrianSystemConstants.GLEY_PEDESTRIAN_SYSTEM, false);
            }
        }

        void InitializedScrollButtons()
        {
            EditorGUILayout.Space();

            if (GUILayout.Button("Import Required Packages"))
            {
                _window.SetActiveWindow(typeof(ImportPackagesWindow), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Scene Setup"))
            {
                _window.SetActiveWindow(typeof(SceneSetupWindow), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Path Setup"))
            {
                _window.SetActiveWindow(typeof(PathSetupWindow), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Waypoint Setup"))
            {
                _window.SetActiveWindow(typeof(PedestrianWaypointSetupWindow), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Pedestrian Routes Setup"))
            {
                _window.SetActiveWindow(typeof(PedestrianRoutesSetupWindow), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Waypoint Priority Setup"))
            {
                _window.SetActiveWindow(typeof(PedestrianWaypointPriorityWindow), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Path Finding"))
            {
                _window.SetActiveWindow(typeof(PathFindingWindow), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("External Tools"))
            {
                _window.SetActiveWindow(typeof(ExternalToolsWindow), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Debug"))
            {
                _window.SetActiveWindow(typeof(PedestrianDebugWindow), true);
            }
            EditorGUILayout.Space();
        }

        protected override void BottomPart()
        {
#if GLEY_PEDESTRIAN_SYSTEM
            InitializedBottomButtons();
#endif

            if (GUILayout.Button("Documentation"))
            {
                Application.OpenURL("https://gley.gitbook.io/mobile-pedestrian-system/quick-start");
            }

            base.BottomPart();
        }

        private void InitializedBottomButtons()
        {
            if (GUILayout.Button("Apply Settings"))
            {
                if (FileCreator.LoadOrCreateLayers<LayerSetup>(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.LayerPath).Edited == false)
                {
                    Debug.LogWarning("Layers are not configured. Go to Window->Gley->Pedestrian System->Scene Setup->Layer Setup");
                }

                _step = 0;
                SaveSettings();
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Disable Pedestrian System"))
            {
                Common.PreprocessorDirective.AddToCurrent(PedestrianSystemConstants.GLEY_PEDESTRIAN_SYSTEM, true);
                Common.PreprocessorDirective.AddToCurrent(UrbanSystemConstants.GLEY_ROADCONSTRUCTOR_TRAFFIC, true);
            }
        }

        private void SaveSettings()
        {
            Debug.Log($"Saving {_step + 1}/4");
            switch (_step)
            {
                case 0:
                    if (!File.Exists($"{Application.dataPath}{Internal.PedestrianSystemConstants.AgentTypesPath}/PedestrianTypes.cs"))
                    {
                        FileCreator.CreateAgentTypesFile<PedestrianTypes>(null, Internal.PedestrianSystemConstants.GLEY_PEDESTRIAN_SYSTEM, Internal.PedestrianSystemConstants.PedestrianNamespace, Internal.PedestrianSystemConstants.AgentTypesPath);
                    }
                    _saving = true;
                    EditorPrefs.SetBool(SAVING, _saving);
                    _step++;
                    EditorPrefs.SetInt(STEP, _step);
                    break;
                case 1:
                    Common.PreprocessorDirective.AddToCurrent(Internal.PedestrianSystemConstants.GLEY_PEDESTRIAN_SYSTEM, false);
                    _saving = true;
                    EditorPrefs.SetBool(SAVING, _saving);
                    _step++;
                    EditorPrefs.SetInt(STEP, _step);
                    break;
                case 2:
                    ApplyPedestrianSettings();
                    _saving = true;
                    EditorPrefs.SetBool(SAVING, _saving);
                    _step++;
                    EditorPrefs.SetInt(STEP, _step);
                    break;
                default:
                    Debug.Log("Save Done");
                    break;
            }
        }


        private void ApplyPedestrianSettings()
        {
            var gridData = new GridEditorData();
            var gridCreator = new GridCreator(gridData);
            gridCreator.GenerateGrid(gridData.GetGridCellSize());

            var pedestrianWaypointsConverter = new PedestrianWaypointsConverter();
            pedestrianWaypointsConverter.ConvertWaypoints();

#if GLEY_TRAFFIC_SYSTEM
            var trafficWaypointsConverter = new TrafficWaypointsConverter();
            trafficWaypointsConverter.ConvertWaypoints();

            var intersectionConverter = new IntersectionConverter();
            intersectionConverter.ConvertAllIntersections();
#endif
        }
    }
}
