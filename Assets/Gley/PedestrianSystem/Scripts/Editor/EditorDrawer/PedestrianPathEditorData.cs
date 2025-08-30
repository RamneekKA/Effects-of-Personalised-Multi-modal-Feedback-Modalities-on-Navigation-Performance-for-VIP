using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;

namespace Gley.PedestrianSystem.Editor
{
    public class PedestrianPathEditorData : RoadEditorData<PedestrianPath>
    {
        public override PedestrianPath[] GetAllRoads()
        {
            return _allRoads;
        }
    }
}
