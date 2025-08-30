using UnityEngine;

namespace Gley.PedestrianSystem
{
    /// <summary>
    /// Stores the pedestrian prefabs used in scene
    /// </summary>
    [CreateAssetMenu(fileName = "PedestrianPool", menuName = "PedestrianSystem/Pedestrian Pool", order = 1)]
    public class PedestrianPool : ScriptableObject
    {
        [SerializeField] private PedestrianType[] _pedestrians;
        public PedestrianType[] Pedestrians => _pedestrians;

        public PedestrianPool()
        {
            PedestrianType pedestrianType = new PedestrianType();
            _pedestrians = new PedestrianType[] { pedestrianType };
        }
    }


    [System.Serializable]
    public class PedestrianType
    {
        [SerializeField] private GameObject _pedestrianPrefab;
        [Range(1, 100)]
        [SerializeField] private int _percent;
        [SerializeField] private bool _dontInstantiate;

        public GameObject PedestrianPrefab => _pedestrianPrefab;
        public int Percent => _percent;
        public bool DontInstantiate => _dontInstantiate;

        public PedestrianType()
        {
            _percent = 1;
        }
    }
}
