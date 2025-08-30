using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Saves debug settings
    /// </summary>
    public class PedestrianDebugSettings : ScriptableObject
    {
        public bool DebugAI = false;
        public bool DebugStopWaypoints = false;
        public bool DebugWaypoints = false;
        public bool DebugDisabledWaypoints = false;
        public bool DebugDensity = false;
        public bool DebugPathFinding = false;
        public bool DebugSpawnWaypoints = false;
        public bool DebugPlayModeWaypoints = false;
        public bool ShowIndex = false;
        public bool ShowPosition = false;
    }
}