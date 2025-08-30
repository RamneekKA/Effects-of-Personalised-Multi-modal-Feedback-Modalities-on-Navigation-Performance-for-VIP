using Gley.UrbanSystem.Editor;
using UnityEditor;
using UnityEngine;
namespace Gley.PedestrianSystem.Editor
{
    internal class ExternalToolsWindow : SetupWindowBase
    {
        protected override void TopPart()
        {
            base.TopPart();
            if (GUILayout.Button("Road Constructor"))
            {
                _window.SetActiveWindow(typeof(RoadConstructorSetup), true);
            }
            EditorGUILayout.Space();
        }
    }
}