using System.Collections.Generic;

namespace Gley.PedestrianSystem
{
    /// <summary>
    /// Must be implemented by the class that stores the pedestrian behaviours.
    /// </summary>
    public interface IBehaviourList
    {
        List<PedestrianBehaviour> GetBehaviours();
    }
}
