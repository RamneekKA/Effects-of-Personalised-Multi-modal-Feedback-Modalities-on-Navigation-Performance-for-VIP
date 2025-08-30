using UnityEngine;

namespace Gley.PedestrianSystem
{
    /// <summary>
    /// Area definition from urban system.
    /// </summary>
    [System.Serializable]
    public class Area : UrbanSystem.Internal.Area
    {
        public Area(Vector3 center, float radius) : base(center, radius)
        {

        }

        public Area(Area area) : base(area)
        {

        }
    }
}
