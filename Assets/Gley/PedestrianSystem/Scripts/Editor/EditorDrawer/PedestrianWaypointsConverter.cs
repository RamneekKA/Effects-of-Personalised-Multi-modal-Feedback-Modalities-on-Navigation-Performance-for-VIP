using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if GLEY_TRAFFIC_SYSTEM
using Gley.TrafficSystem.Internal;
#endif

namespace Gley.PedestrianSystem.Editor
{
    internal class PedestrianWaypointsConverter
    {
        private readonly PedestrianWaypointEditorData _pedestrianWaypointEditorData;

        internal PedestrianWaypointsConverter()
        {
            _pedestrianWaypointEditorData = new PedestrianWaypointEditorData();
        }


        internal void ConvertWaypoints()
        {
            VerifyPedestrianWaypoints();
            SetIntersectionProperties();
            SetCrossingComponentProperties();
            ConvertPedestrianWaypoints();
            AssignPedestrianWaypointsToCell();
            GeneratePathFindingWaypoints();
        }


        private void VerifyPedestrianWaypoints()
        {
            PedestrianWaypointSettings[] allPedestrianEditorWaypoints = _pedestrianWaypointEditorData.GetAllWaypoints();

            if (allPedestrianEditorWaypoints.Length <= 0)
            {
                Debug.LogWarning("No waypoints found. Go to Tools->Gley->Pedestrian System->Path Setup and create a path");
                return;
            }

            for (int i = 0; i < allPedestrianEditorWaypoints.Length; i++)
            {
                allPedestrianEditorWaypoints[i].VerifyAssignments(false);
                allPedestrianEditorWaypoints[i].ResetProperties();
            }
        }


        private void SetCrossingComponentProperties()
        {
            List<StreetCrossingComponent> allIntersectionComponents = GleyPrefabUtilities.GetAllComponents<StreetCrossingComponent>().ToList();
            for (int i = 0; i < allIntersectionComponents.Count; i++)
            {
                if (!allIntersectionComponents[i].VerifyAssignments())
                    return;

                List<PedestrianWaypointSettings> intersectionWaypoints = allIntersectionComponents[i].GetPedestrianWaypoints();
                for (int j = intersectionWaypoints.Count - 1; j >= 0; j--)
                {
                    intersectionWaypoints[j].InIntersection = true;
                }

                List<PedestrianWaypointSettings> directionWaypoints = allIntersectionComponents[i].GetDirectionWaypoints();
                for (int j = directionWaypoints.Count - 1; j >= 0; j--)
                {
                    directionWaypoints[j].Crossing = true;
                }
            }
        }


        private void ConvertPedestrianWaypoints()
        {
            PedestrianWaypointSettings[] allPedestrianEditorWaypoints = _pedestrianWaypointEditorData.GetAllWaypoints();

            // Convert to play waypoints
            var pedestrianWaypointsData = MonoBehaviourUtilities.GetOrCreateObjectScript<PedestrianWaypointsData>(PedestrianSystemConstants.PlayHolder, false);
            pedestrianWaypointsData.SetPedestrianWaypoints(allPedestrianEditorWaypoints.ToPlayWaypoints(allPedestrianEditorWaypoints));
            SetParentTagsRecursively(pedestrianWaypointsData.gameObject);
        }


        private void SetParentTagsRecursively(GameObject obj)
        {
            Transform currentParent = obj.transform.parent;

            while (currentParent != null)
            {
                if (currentParent.gameObject.tag == UrbanSystemConstants.EDITOR_TAG)
                {
                    currentParent.gameObject.tag = "Untagged";
                }
                currentParent = currentParent.parent;
            }
        }

        private void AssignPedestrianWaypointsToCell()
        {
            PedestrianWaypointSettings[] allPedestrianEditorWaypoints = _pedestrianWaypointEditorData.GetAllWaypoints();

            GridData gridData;
            if (MonoBehaviourUtilities.TryGetSceneScript<GridData>(out var result))
            {
                gridData = result.Value;
            }
            else
            {
                Debug.LogError(result.Error);
                return;
            }

            
            // Assign pedestrian waypoint index to cell;
            for (int i = 0; i < allPedestrianEditorWaypoints.Length; i++)
            {
                if (allPedestrianEditorWaypoints[i].AllowedPedestrians.Count != 0)
                {
                    var cellData = gridData.GetCell(allPedestrianEditorWaypoints[i].transform.position);
                    gridData.AddPedestrianWaypoint(cellData, i);

                    if (allPedestrianEditorWaypoints[i].InIntersection == false)
                    {
                        if (!allPedestrianEditorWaypoints[i].name.Contains(UrbanSystemConstants.Connect))
                        {
                            gridData.AddPedestrianSpawnWaypoint(cellData, i, allPedestrianEditorWaypoints[i].AllowedPedestrians.Cast<int>().ToArray(), allPedestrianEditorWaypoints[i].priority);
                        }
                    }
                }
            }
        }


        private void SetIntersectionProperties()
        {
            //assign street crossing components
            var crossings = MonoBehaviourUtilities.FindObjectsByType<StreetCrossingComponent>();
            PedestrianWaypointSettings[] allPedestrianEditorWaypoints = _pedestrianWaypointEditorData.GetAllWaypoints();
            foreach (var crossing in crossings)
            {
                if (!crossing.VerifyAssignments())
                {
                    return;
                }
                crossing.ConvertWaypoints(allPedestrianEditorWaypoints);
            }

#if GLEY_TRAFFIC_SYSTEM
#if GLEY_PEDESTRIAN_SYSTEM
            TrafficSystem.Internal.GenericIntersectionSettings[] allEditorIntersections = new TrafficSystem.Editor.IntersectionEditorData().GetAllIntersections();
            for (int i = 0; i < allEditorIntersections.Length; i++)
            {
                if (!allEditorIntersections[i].VerifyAssignments())
                    return;

                List<PedestrianWaypointSettings> intersectionWaypoints = allEditorIntersections[i].GetPedestrianWaypoints();
                for (int j = intersectionWaypoints.Count - 1; j >= 0; j--)
                {
                    intersectionWaypoints[j].InIntersection = true;
                }

                List<PedestrianWaypointSettings> directionWaypoints = allEditorIntersections[i].GetDirectionWaypoints();
                for (int j = directionWaypoints.Count - 1; j >= 0; j--)
                {
                    directionWaypoints[j].Crossing = true;
                }
            }
#endif
#endif
        }


        private void GeneratePathFindingWaypoints()
        {
            // Generate path finding waypoints
            bool pathfindingEnabled = new SettingsLoader(PedestrianSystemConstants.WindowSettingsPath).LoadSettingsAsset<PedestrianSettingsWindowData>().PathFindingEnabled;
            var modules = MonoBehaviourUtilities.GetOrCreateObjectScript<PedestrianModules>(PedestrianSystemConstants.PlayHolder, false);
            if (pathfindingEnabled)
            {
                PedestrianWaypointSettings[] allPedestrianEditorWaypoints = _pedestrianWaypointEditorData.GetAllWaypoints();
                var pedestrianPathFindingCreator = new PedestrianPathFindingCreator();
                pedestrianPathFindingCreator.GenerateWaypoints(allPedestrianEditorWaypoints);
                modules.SetModules(true);
            }
            else
            {
                modules.SetModules(false);
                if (MonoBehaviourUtilities.TryGetObjectScript<PathFindingData>(PedestrianSystemConstants.PlayHolder, out var result))
                {
                    GleyPrefabUtilities.DestroyImmediate(result.Value);
                }
            }
        }
    }
}