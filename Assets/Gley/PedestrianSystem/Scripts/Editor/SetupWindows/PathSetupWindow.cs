using Gley.UrbanSystem.Editor;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class PathSetupWindow : SetupWindowBase
    {
        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            return this;
        }


        protected override void TopPart()
        {
            base.TopPart();
            EditorGUILayout.LabelField("Select action:");
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Path"))
            {
                _window.SetActiveWindow(typeof(CreatePathWindow), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Connect Paths"))
            {
                _window.SetActiveWindow(typeof(ConnectPathsWindow), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("View Paths"))
            {
                _window.SetActiveWindow(typeof(ViewPathsWindow), true);
            }
        }
    }
}