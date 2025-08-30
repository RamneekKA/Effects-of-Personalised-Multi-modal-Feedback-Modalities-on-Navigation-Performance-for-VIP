using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using UnityEditor;

namespace Gley.PedestrianSystem.Editor
{
    public class PedestrianDebugWindow : SetupWindowBase
    {
        private PedestrianDebugSettings _save;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            _save = PedestrianDebugOptions.LoadOrCreateDebugSettings();
            return base.Initialize(windowProperties, window);
        }


        protected override void TopPart()
        {
            _save.DebugAI = EditorGUILayout.Toggle("Debug Pedestrian AI", _save.DebugAI);
            if(_save.DebugAI==false)
            {
                _save.DebugPathFinding = false;
            }
            _save.DebugStopWaypoints = EditorGUILayout.Toggle("Debug Street Crossings", _save.DebugStopWaypoints);
            _save.DebugWaypoints = EditorGUILayout.Toggle("Debug Waypoints", _save.DebugWaypoints);
            _save.DebugDisabledWaypoints = EditorGUILayout.Toggle("Disabled Waypoints", _save.DebugDisabledWaypoints);
            _save.DebugDensity = EditorGUILayout.Toggle("Debug Density", _save.DebugDensity);
            _save.DebugPathFinding = EditorGUILayout.Toggle("Debug Path Finding", _save.DebugPathFinding);
            if(_save.DebugPathFinding==true)
            {
                _save.DebugAI = true;
            }
            _save.DebugSpawnWaypoints = EditorGUILayout.Toggle("Spawn Waypoints", _save.DebugSpawnWaypoints);
            _save.DebugPlayModeWaypoints = EditorGUILayout.Toggle("Play Mode Waypoints", _save.DebugPlayModeWaypoints);

            if (_save.DebugPlayModeWaypoints == true)
            {
                _save.ShowIndex = EditorGUILayout.Toggle("Show Index", _save.ShowIndex);
                _save.ShowPosition = EditorGUILayout.Toggle("Show Position", _save.ShowPosition);
            }

            base.TopPart();
        }


        public override void DestroyWindow()
        {
            base.DestroyWindow();
            EditorUtility.SetDirty(_save);
        }
    }
}