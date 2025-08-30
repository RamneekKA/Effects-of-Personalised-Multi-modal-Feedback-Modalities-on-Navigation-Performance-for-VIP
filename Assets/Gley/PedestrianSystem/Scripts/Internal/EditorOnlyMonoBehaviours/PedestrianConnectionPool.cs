using Gley.UrbanSystem.Internal;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Stores pedestrian connection pools.
    /// A connection pool can contain multiple connection curves.
    /// </summary>
    public class PedestrianConnectionPool : GenericConnectionPool<PedestrianConnectionCurve>
    {
    }
}