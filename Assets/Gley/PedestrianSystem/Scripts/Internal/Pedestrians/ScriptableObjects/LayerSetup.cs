using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Saves layer settings
    /// </summary>
    public class LayerSetup : ScriptableObject
    {
        public bool Edited;
        public LayerMask GroundLayers = 256;
        public LayerMask PedestrianLayers = 512;
        public LayerMask BuildingsLayers = 1024;
        public LayerMask ObstaclesLayers = 2048;
        public LayerMask PlayerLayers = 4096;
    }
}
