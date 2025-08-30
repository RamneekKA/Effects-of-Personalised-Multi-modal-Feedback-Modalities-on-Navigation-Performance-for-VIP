using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;

namespace Gley.PedestrianSystem.Editor
{
    internal class PedestrianLaneEditorData : LaneEditorData<PedestrianPath, PedestrianWaypointSettings>
    {
        internal PedestrianLaneEditorData(RoadEditorData<PedestrianPath> roadData) : base(roadData)
        {
        }
    }
}