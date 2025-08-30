using System.Collections.Generic;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// The behaviours listed here will be used by the pedestrians if no other behaviour list is specified
    /// </summary>
    public class DefaultBehaviours : IBehaviourList
    {
        public List<PedestrianBehaviour> GetBehaviours()
        {
            return new List<PedestrianBehaviour>
            {
                // Order all behaviours.
                // The top one is the most important.
                // Only a behaviour can be active at the time.
                new Dead(),
                new Hit(),
                new DestinationReached(),
                new StopAtCrossing(),
                new HasCollided(),
                new AvoidStaticObstacles(),
                new AvoidDynamicObstacles(),
                new AvoidOtherPedestrians(),
                new TimeRun(1,3),
                new RunForever(1),
                new Crossing(),
                new Walk(),
            };
        }
    }
}