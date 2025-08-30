using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using System;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class PedestrianSettingsWindow : SettingsWindowBase
    {
        private const string WINDOW_NAME = "Pedestrian - v.";
        private const string PATH = "Assets//Gley/PedestrianSystem/Scripts/Version.txt";
        private const int MIN_WIDTH = 400;
        private const int MIN_HEIGHT = 500;

        private static PedestrianSettingsWindow _pedestrianWindow;
        private static PedestrianWindowNavigationData _windowNavigationData;

        [MenuItem("Tools/Gley/Pedestrian System", false)]
        private static void Initialize()
        {
            _pedestrianWindow = WindowLoader.LoadWindow<PedestrianSettingsWindow>(PATH, WINDOW_NAME, MIN_WIDTH, MIN_HEIGHT);
            _pedestrianWindow.Init(_pedestrianWindow, typeof(MainMenuWindow), AllWindowsData.GetWindowsData(), new AllSettingsWindows());
        }


        private void OnInspectorUpdate()
        {
            if (_activeSetupWindow != null)
            {
                _activeSetupWindow.InspectorUpdate();
            }
        }


        protected override void Reinitialize()
        {
            if (_pedestrianWindow == null)
            {
                Initialize();
            }
            else
            {
                _pedestrianWindow.Init(_pedestrianWindow, typeof(MainMenuWindow), AllWindowsData.GetWindowsData(), new AllSettingsWindows());
            }
        }


        protected override void ResetToHomeScreen(Type defaultWindow, bool now)
        {
            _windowNavigationData = new PedestrianWindowNavigationData();
            _windowNavigationData.InitializeData();
            base.ResetToHomeScreen(defaultWindow, now);
        }


        protected override void MouseMove(Vector3 point)
        {
            if (_activeSetupWindow.GetType() == typeof(EditPathWindow))
            {
                _activeSetupWindow.MouseMove(point);
            }
        }


        public override LayerMask GetGroundLayer()
        {
            return _windowNavigationData.GetGroundLayers();
        }


        //TODO these should not be static methods
        public static void SetSelectedPath(PedestrianPath path)
        {
            _windowNavigationData.SetSelectedRoad(path);
        }


        public static void UpdateLayers()
        {
            _windowNavigationData.UpdateLayers();
        }


        public static PedestrianPath GetSelectedRoad()
        {
            return _windowNavigationData.GetSelectedRoad();
        }


        public static void SetSelectedWaypoint(PedestrianWaypointSettings pedestrianWaypointSettings)
        {
            if (_windowNavigationData != null)
            {
                _windowNavigationData.SetSelectedWaypoint(pedestrianWaypointSettings);
            }
        }


        public static PedestrianWaypointSettings GetSelectedWaypoint()
        {
            return _windowNavigationData.GetSelectedWaypoint();
        }
    }
}
