using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Store the state of optional pedestrian modules.
    /// </summary>
    public class PedestrianModules : MonoBehaviour
    {
        [SerializeField] private bool _pathFinding;

        public bool PathFinding => _pathFinding;

        public void SetModules(bool enablePathFinding)
        {
            _pathFinding = enablePathFinding;
        }
    }
}