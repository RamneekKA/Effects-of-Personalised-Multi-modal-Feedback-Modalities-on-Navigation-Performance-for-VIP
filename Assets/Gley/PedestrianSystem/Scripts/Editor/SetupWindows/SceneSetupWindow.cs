using Gley.UrbanSystem.Editor;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    internal class SceneSetupWindow : SetupWindowBase
    {
        private const string URP_PACKAGE = "Gley/PedestrianSystem/URP/PedestrianSystem-URP.unitypackage";

        protected override void ScrollPart(float width, float height)
        {
            EditorGUILayout.LabelField("Select action:");
            EditorGUILayout.Space();

            if (GUILayout.Button("Layer Setup"))
            {
                _window.SetActiveWindow(typeof(LayerSetupWindow), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Grid Setup"))
            {
                _window.SetActiveWindow(typeof(GridSetupWindow), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Pedestrian Type Setup"))
            {
                _window.SetActiveWindow(typeof(PedestrianTypesWindow), true);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Convert Materials to URP"))
            {
                AssetDatabase.ImportPackage($"{Application.dataPath}/{URP_PACKAGE}", false);
            }
            EditorGUILayout.Space();

            base.ScrollPart(width, height);
        }
    }
}
