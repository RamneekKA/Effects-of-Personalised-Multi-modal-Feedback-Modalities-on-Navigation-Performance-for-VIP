using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;

namespace Gley.PedestrianSystem.Editor
{
    public class PedestrianPathFindingCreator
    {
        public void GenerateWaypoints(PedestrianWaypointSettings[] allEditorWaypoints)
        {
            var allPathFindingWaypoints = new List<PathFindingWaypoint>();
            for (int i = 0; i < allEditorWaypoints.Length; i++)
            {
                var penalties = new List<int>();
                var neighbors = new List<WaypointSettingsBase>();

                for (int j = 0; j < allEditorWaypoints[i].neighbors.Count; j++)
                {
                    neighbors.Add(allEditorWaypoints[i].neighbors[j]);
                    penalties.Add(allEditorWaypoints[i].neighbors[j].penalty);
                }

                for (int j = 0; j < allEditorWaypoints[i].prev.Count; j++)
                {
                    neighbors.Add(allEditorWaypoints[i].prev[j]);
                    penalties.Add(allEditorWaypoints[i].prev[j].penalty);
                }

                allPathFindingWaypoints.Add(new PathFindingWaypoint(i, allEditorWaypoints[i].transform.position, 0, 0, -1, neighbors.ToListIndex(allEditorWaypoints), penalties.ToArray(), allEditorWaypoints[i].AllowedPedestrians.Cast<int>().ToArray()));
            }

            PathFindingData data = MonoBehaviourUtilities.GetOrCreateObjectScript<PathFindingData>(PedestrianSystemConstants.PlayHolder, false);
            data.SetPathFindingWaypoints(allPathFindingWaypoints.ToArray());
        }
    }
}