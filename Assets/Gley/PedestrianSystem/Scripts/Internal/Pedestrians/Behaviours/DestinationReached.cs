namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Make pedestrian stop after reaching the end of a path.
    /// </summary>
    public class DestinationReached : PedestrianBehaviour
    {
        /// <summary>
        /// Constructor
        /// Listen for destination reached event.
        /// </summary>
        public DestinationReached()
        {
            Events.OnDestinationReached += PerformDestinationReachedAction;
        }


        /// <summary>
        /// Executes on update when the behaviour is active.
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to.</param>
        /// <param name="moveSpeed">The walking speed.</param>
        internal override void Execute(float turnAngle, float moveSpeed)
        {
            // If pedestrian gets a new destination stop this behaviour.
            if (API.GetPathLength(PedestrianIndex) > 0)
            {
                Stop();
            }
            else
            {
                // Stop the pedestrian(or do any action you want here).
                AnimatorMethods.Stop();
            }
        }


        /// <summary>
        /// Listener for destination reached.
        /// </summary>
        /// <param name="pedestrianIndex"> the index of the pedestrian.</param>
        private void PerformDestinationReachedAction(int pedestrianIndex)
        {
            // If it is this pedestrian.
            if (PedestrianIndex == pedestrianIndex)
            {
                // If this behaviour is not already running.
                if (!CanRun())
                {
                    // Start behaviour.
                    Start();
                }
            }
        }


        /// <summary>
        /// Remove listener
        /// </summary>
        internal override void OnDestroy()
        {
            Events.OnDestinationReached -= PerformDestinationReachedAction;
        }
    }
}