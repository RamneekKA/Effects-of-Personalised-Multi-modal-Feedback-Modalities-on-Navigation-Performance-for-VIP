namespace Gley.PedestrianSystem
{
    /// <summary>
    /// Initialization options and default values.
    /// </summary>
    public class PedestrianSystemOptions
    {
        public Area DisableWaypointsArea = default;
        public float MinDistanceToAdd = -1;
        public float DistanceToRemove = -1;
        public int ActiveSquaresLevel = 1;
        public int InitialDensity = -1; //all pedestrians are available from the start
        public int DefaultPathLength = 2;
        public bool UseWaypointPriority = false;


        private IBehaviourList _pedestrianBehaviours;
        public IBehaviourList PedestrianBehaviours
        {
            get
            {
                if (_pedestrianBehaviours == null)
                {
                    _pedestrianBehaviours = new Internal.DefaultBehaviours();
                }
                return _pedestrianBehaviours;
            }
            set
            {
                _pedestrianBehaviours = value;
            }
        }

        private PedestrianSpawnWaypointSelector _spawnWaypointSelector;
        public PedestrianSpawnWaypointSelector SpawnWaypointSelector
        {
            get
            {
                if (_spawnWaypointSelector == null)
                {
                    _spawnWaypointSelector = DefaultPedestrianDelegates.GetRandomSpawnWaypoint;
                }
                return _spawnWaypointSelector;
            }
            set
            {
                _spawnWaypointSelector = value;
            }
        }
    }
}