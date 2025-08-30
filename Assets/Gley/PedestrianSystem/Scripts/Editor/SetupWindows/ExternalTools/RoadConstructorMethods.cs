#if GLEY_ROADCONSTRUCTOR_TRAFFIC
using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Internal;
using PampelGames.RoadConstructor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class RoadConstructorMethods : UnityEditor.Editor
    {
        private static string RoadConstructorWaypointsHolder
        {
            get
            {
                return $"{PedestrianSystemConstants.PACKAGE_NAME}/{UrbanSystemConstants.EDITOR_HOLDER}/RoadConstructorWaypoints";
            }
        }

        private static string RoadConstructorIntersectionHolder
        {
            get
            {
                return $"{PedestrianSystemConstants.PACKAGE_NAME}/{UrbanSystemConstants.EDITOR_HOLDER}/RoadConstructorIntersections";
            }
        }

        private static string RoadConstructorConnectionsHolder
        {
            get
            {
                return $"{PedestrianSystemConstants.PACKAGE_NAME}/{UrbanSystemConstants.EDITOR_HOLDER}/RoadConstructorConnections";
            }
        }

        public static void ExtractWaypoints(List<int> vehicleTypes)
        {
            Dictionary<PampelGames.RoadConstructor.Waypoint, PedestrianWaypointSettings> forwardConnections;
            Dictionary<PampelGames.RoadConstructor.Waypoint, PedestrianWaypointSettings> backwardConnections;

            Debug.Log("Extracting waypoints");
            forwardConnections = new Dictionary<PampelGames.RoadConstructor.Waypoint, PedestrianWaypointSettings>();
            backwardConnections = new Dictionary<PampelGames.RoadConstructor.Waypoint, PedestrianWaypointSettings>();
            DestroyImmediate(GameObject.Find(RoadConstructorWaypointsHolder));
            DestroyImmediate(GameObject.Find(RoadConstructorIntersectionHolder));
            DestroyImmediate(GameObject.Find(RoadConstructorConnectionsHolder));

            var roadConstructor = FindFirstObjectByType<RoadConstructor>();


            Transform intersectionHolder = MonoBehaviourUtilities.GetOrCreateGameObject(RoadConstructorIntersectionHolder, true).transform;
            Transform waypointsHolder = MonoBehaviourUtilities.GetOrCreateGameObject(RoadConstructorWaypointsHolder, true).transform;
            Transform connectorsHolder = MonoBehaviourUtilities.GetOrCreateGameObject(RoadConstructorConnectionsHolder, true).transform;

            var forwardWaypoints = new List<PampelGames.RoadConstructor.Waypoint>();
            var backwardWaypoints = new List<PampelGames.RoadConstructor.Waypoint>();

            //extract forward waypoints
            var roadObjects = roadConstructor.GetRoads();
            for (var i = 0; i < roadObjects.Count; i++)
            {
                var trafficLanes = roadObjects[i].GetTrafficLanes(TrafficLaneType.Pedestrian, TrafficLaneDirection.Forward);
                if (trafficLanes.Count == 0)
                {
                    continue;
                }
                var roadName = $"{PedestrianSystemConstants.PathName}_{roadObjects[i].name.Split("-")[1]}";
                Transform road = MonoBehaviourUtilities.CreateGameObject(roadName, waypointsHolder, trafficLanes[0].spline.Knots.First().Position, true).transform;
                for (var j = 0; j < trafficLanes.Count; j++)
                {
                    Transform lane = MonoBehaviourUtilities.CreateGameObject($"Lane_{j}", road, trafficLanes[j].spline.Knots.First().Position, true).transform;
                    var waypoints = trafficLanes[j].GetWaypoints();
                    forwardWaypoints.AddRange(waypoints);
                    if (waypoints.Count > 0)
                    {
                        CreatePedestrianWaypoints(lane, waypoints, trafficLanes[j].width, $"{road.name}-{lane.name}", false, vehicleTypes, trafficLanes[j].crossing, ref forwardConnections);
                    }
                }
            }

            //extract forward waypoints
            roadObjects = roadConstructor.GetRoads();
            for (var i = 0; i < roadObjects.Count; i++)
            {
                var trafficLanes = roadObjects[i].GetTrafficLanes(TrafficLaneType.Pedestrian, TrafficLaneDirection.Backwards);
                if (trafficLanes.Count == 0)
                {
                    continue;
                }
                Transform road = GameObject.Find($"{PedestrianSystemConstants.PathName}_{roadObjects[i].name.Split("-")[1]}").transform;
                for (var j = 0; j < trafficLanes.Count; j++)
                {
                    Transform lane = MonoBehaviourUtilities.CreateGameObject($"Back_{j}", road, trafficLanes[j].spline.Knots.First().Position, true).transform;
                    var waypoints = trafficLanes[j].GetWaypoints();
                    backwardWaypoints.AddRange(waypoints);
                    if (waypoints.Count > 0)
                    {
                        CreatePedestrianWaypoints(lane, waypoints, trafficLanes[j].width, $"{road.name}-{lane.name}-Back", false, vehicleTypes, trafficLanes[j].crossing, ref backwardConnections);
                    }
                }
            }


            //create intersection waypoints
            var intersectionObjects = roadConstructor.GetIntersections();
            for (var i = 0; i < intersectionObjects.Count; i++)
            {
                var trafficLanes = intersectionObjects[i].GetTrafficLanes(TrafficLaneType.Pedestrian, TrafficLaneDirection.Forward);
                if (trafficLanes.Count == 0)
                {
                    continue;
                }
                var roadName = intersectionObjects[i].name.Replace("-", "");
                Transform road = MonoBehaviourUtilities.CreateGameObject(roadName, connectorsHolder, trafficLanes[0].spline.Knots.First().Position, true).transform;
                for (var j = 0; j < trafficLanes.Count; j++)
                {
                    Transform lane = MonoBehaviourUtilities.CreateGameObject($"Lane_{j}", road, trafficLanes[j].spline.Knots.First().Position, true).transform;
                    var waypoints = trafficLanes[j].GetWaypoints();
                    forwardWaypoints.AddRange(waypoints);
                    if (waypoints.Count > 0)
                    {
                        CreatePedestrianWaypoints(lane, waypoints, trafficLanes[j].width, $"{road.name}-{lane.name}", true, vehicleTypes, trafficLanes[j].crossing, ref forwardConnections);
                    }
                }
            }

            //create intersection waypoints
            intersectionObjects = roadConstructor.GetIntersections();
            for (var i = 0; i < intersectionObjects.Count; i++)
            {
                var trafficLanes = intersectionObjects[i].GetTrafficLanes(TrafficLaneType.Pedestrian, TrafficLaneDirection.Backwards);
                if (trafficLanes.Count == 0)
                {
                    continue;
                }
                Transform road = GameObject.Find(intersectionObjects[i].name.Replace("-", "")).transform;
                for (var j = 0; j < trafficLanes.Count; j++)
                {
                    Transform lane = MonoBehaviourUtilities.CreateGameObject($"Back_{j}", road, trafficLanes[j].spline.Knots.First().Position, true).transform;
                    var waypoints = trafficLanes[j].GetWaypoints();
                    backwardWaypoints.AddRange(waypoints);
                    if (waypoints.Count > 0)
                    {
                        CreatePedestrianWaypoints(lane, waypoints, trafficLanes[j].width, $"{road.name}-{lane.name}-Back", true, vehicleTypes, trafficLanes[j].crossing, ref backwardConnections);
                    }
                }
            }


            //link
            for (var i = 0; i < forwardWaypoints.Count; i++)
            {
                if (forwardConnections.TryGetValue(forwardWaypoints[i], out var trafficWaypoint))
                {
                    for (int j = 0; j < forwardWaypoints[i].next.Count; j++)
                    {
                        if (forwardConnections.TryGetValue(forwardWaypoints[i].next[j], out var neighbor))
                        {
                            trafficWaypoint.neighbors.Add(neighbor);
                        }
                        else
                        {
                            if (backwardConnections.TryGetValue(forwardWaypoints[i].next[j], out neighbor))
                            {
                                trafficWaypoint.neighbors.Add(neighbor);
                            }
                            else
                            {
                                Debug.Log(forwardWaypoints[i].next[j], forwardWaypoints[i].next[j]);
                            }
                        }

                    }
                    for (int j = 0; j < forwardWaypoints[i].prev.Count; j++)
                    {
                        if (forwardConnections.TryGetValue(forwardWaypoints[i].prev[j], out var neighbor))
                        {
                            trafficWaypoint.prev.Add(neighbor);
                        }
                        else
                        {
                            if (backwardConnections.TryGetValue(forwardWaypoints[i].prev[j], out neighbor))
                            {
                                trafficWaypoint.prev.Add(neighbor);
                            }
                            else
                            {
                                Debug.Log(forwardWaypoints[i].prev[j], forwardWaypoints[i].prev[j]);
                            }
                        }
                    }
                }
            }

            for (var i = 0; i < backwardWaypoints.Count; i++)
            {
                if (backwardConnections.TryGetValue(backwardWaypoints[i], out var trafficWaypoint))
                {
                    for (int j = 0; j < backwardWaypoints[i].prev.Count; j++)
                    {
                        if (backwardConnections.TryGetValue(backwardWaypoints[i].prev[j], out var neighbor))
                        {
                            trafficWaypoint.neighbors.Add(neighbor);
                        }
                        else
                        {
                            if (forwardConnections.TryGetValue(backwardWaypoints[i].prev[j], out neighbor))
                            {
                                trafficWaypoint.neighbors.Add(neighbor);

                            }
                            else
                            {
                                Debug.Log(backwardWaypoints[i].prev[j], backwardWaypoints[i].prev[j]);
                            }
                        }

                    }
                    for (int j = 0; j < backwardWaypoints[i].next.Count; j++)
                    {
                        if (backwardConnections.TryGetValue(backwardWaypoints[i].next[j], out var neighbor))
                        {
                            trafficWaypoint.prev.Add(neighbor);
                        }
                        else
                        {
                            if (forwardConnections.TryGetValue(backwardWaypoints[i].next[j], out neighbor))
                            {
                                trafficWaypoint.prev.Add(neighbor);
                            }
                            else
                            {
                                Debug.Log(backwardWaypoints[i].next[j], backwardWaypoints[i].next[j]);
                            }
                        }
                    }
                }
            }

            //remove duplicate forward waypoints
            RemoveDuplicateWaypoints(ref forwardWaypoints, ref forwardConnections);
            RemoveDuplicateWaypoints(ref backwardWaypoints, ref backwardConnections);


            //associate backwards and forwards waypoints
            var association = new Dictionary<PampelGames.RoadConstructor.Waypoint, PampelGames.RoadConstructor.Waypoint>();
            var pedestrianWaypointAssociation = new Dictionary<WaypointSettingsBase, WaypointSettingsBase>();

            float max = 0;
            for (var i = 0; i < backwardWaypoints.Count; i++)
            {
                float min = Mathf.Infinity;
                int index = 0;
                for (int j = 0; j < forwardWaypoints.Count; j++)
                {
                    var distance = Vector3.SqrMagnitude(backwardWaypoints[i].transform.position - forwardWaypoints[j].transform.position);
                    if (min > distance)
                    {
                        min = distance;
                        index = j;
                    }
                }
                if (min != Mathf.Infinity)
                {
                    if (min > max)
                    {
                        max = min;
                    }
                    association.Add(backwardWaypoints[i], forwardWaypoints[index]);

                    pedestrianWaypointAssociation.Add(backwardConnections.GetValueOrDefault(backwardWaypoints[i]), forwardConnections.GetValueOrDefault(forwardWaypoints[index]));
                }
            }


            //link backwards to forward
            for (var i = 0; i < backwardWaypoints.Count; i++)
            {
                var forwardWaypoint = association[backwardWaypoints[i]];
                if (forwardConnections.TryGetValue(forwardWaypoint, out var trafficWaypoint))
                {
                    for (int j = 0; j < backwardWaypoints[i].prev.Count; j++)
                    {
                        if (backwardConnections.TryGetValue(backwardWaypoints[i].prev[j], out var prev))
                        {
                            trafficWaypoint.neighbors.Add(prev);
                        }
                    }
                }
            }


            //convert backwards neighbors to corresponding forward waypoints
            for (var i = 0; i < forwardWaypoints.Count; i++)
            {
                if (forwardConnections.TryGetValue(forwardWaypoints[i], out var trafficWaypoint))
                {
                    for (int j = trafficWaypoint.neighbors.Count - 1; j >= 0; j--)
                    {
                        //Debug.Log(trafficWaypoint, trafficWaypoint);
                        if (trafficWaypoint.neighbors[j].name.Contains("Back"))
                        {
                            //Debug.Log(trafficWaypoint.neighbors[j], trafficWaypoint);
                            trafficWaypoint.neighbors.Add(pedestrianWaypointAssociation[trafficWaypoint.neighbors[j]]);
                            trafficWaypoint.neighbors.RemoveAt(j);
                        }
                    }

                    for (int j = trafficWaypoint.prev.Count - 1; j >= 0; j--)
                    {
                        //Debug.Log(trafficWaypoint, trafficWaypoint);
                        if (trafficWaypoint.prev[j].name.Contains("Back"))
                        {
                            //Debug.Log(trafficWaypoint.neighbors[j], trafficWaypoint);
                            trafficWaypoint.prev.Add(pedestrianWaypointAssociation[trafficWaypoint.prev[j]]);
                            trafficWaypoint.prev.RemoveAt(j);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning(forwardWaypoints[i], forwardWaypoints[i]);
                }
            }


            //remove duplicates
            for (var i = 0; i < forwardWaypoints.Count; i++)
            {
                if (forwardConnections.TryGetValue(forwardWaypoints[i], out var trafficWaypoint))
                {
                    trafficWaypoint.neighbors.Remove(trafficWaypoint);
                    trafficWaypoint.prev.Remove(trafficWaypoint);
                    if (trafficWaypoint.neighbors.Count > 1)
                    {
                        for (int j = trafficWaypoint.neighbors.Count - 2; j >= 0; j--)
                        {
                            for (int k = trafficWaypoint.neighbors.Count - 1; k > 0; k--)
                            {
                                if (trafficWaypoint.neighbors[j] == trafficWaypoint.neighbors[k])
                                {
                                    trafficWaypoint.neighbors.RemoveAt(j);
                                    break;
                                }
                            }
                        }
                    }
                    if (trafficWaypoint.prev.Count > 1)
                    {
                        for (int j = trafficWaypoint.prev.Count - 2; j >= 0; j--)
                        {
                            for (int k = trafficWaypoint.prev.Count - 1; k > 0; k--)
                            {
                                if (trafficWaypoint.prev[j] == trafficWaypoint.prev[k])
                                {
                                    trafficWaypoint.prev.RemoveAt(j);
                                    break;
                                }
                            }
                        }
                    }
                }
            }


            //remove connections that skip a waypoint
            for (var i = 0; i < forwardWaypoints.Count; i++)
            {
                if (forwardConnections.TryGetValue(forwardWaypoints[i], out var trafficWaypoint))
                {
                    if (trafficWaypoint.neighbors.Count > 1)
                    {
                        for (int j = trafficWaypoint.neighbors.Count - 1; j >= 0; j--)
                        {
                            //Debug.Log(j + " " + trafficWaypoint, trafficWaypoint);
                            var reesult = trafficWaypoint.neighbors.Intersect(trafficWaypoint.neighbors[j].neighbors).ToList();
                            if (reesult.Count > 0)
                            {
                                for (int k = 0; k < reesult.Count; k++)
                                {
                                    trafficWaypoint.neighbors.Remove(reesult[k]);
                                }
                            }
                        }
                    }
                }
            }


            //remove back waypoints
            foreach (var pair in backwardConnections)
            {
                if (pair.Value != null)
                {
                    DestroyImmediate(pair.Value.transform.parent.gameObject);
                }
            }


            //remove circular connections
            for (var i = 0; i < forwardWaypoints.Count; i++)
            {
                if (forwardConnections.TryGetValue(forwardWaypoints[i], out var trafficWaypoint))
                {
                    for (int j = 0; j < trafficWaypoint.neighbors.Count; j++)
                    {
                        if (!trafficWaypoint.neighbors[j].prev.Contains(trafficWaypoint))
                        {
                            trafficWaypoint.neighbors[j].prev.Add(trafficWaypoint);
                        }
                    }
                    for (int j = 0; j < trafficWaypoint.prev.Count; j++)
                    {
                        if (!trafficWaypoint.prev[j].neighbors.Contains(trafficWaypoint))
                        {
                            trafficWaypoint.prev[j].neighbors.Add(trafficWaypoint);
                        }
                    }
                }
            }


            //remove circular connections
            for (var i = 0; i < forwardWaypoints.Count; i++)
            {
                if (forwardConnections.TryGetValue(forwardWaypoints[i], out var trafficWaypoint))
                {
                    List<int> indicesToRemove = new List<int>();

                    for (int j = 0; j < trafficWaypoint.prev.Count; j++)
                    {
                        if (trafficWaypoint.neighbors.Contains(trafficWaypoint.prev[j]))
                        {
                            indicesToRemove.Add(j);
                        }
                    }

                    // Remove in reverse order to avoid shifting issues
                    for (int x = indicesToRemove.Count - 1; x >= 0; x--)
                    {
                        trafficWaypoint.prev.RemoveAt(indicesToRemove[x]);
                    }

                    for (int j = trafficWaypoint.neighbors.Count - 1; j >= 0; j--)
                    {
                        if (trafficWaypoint.neighbors[j].neighbors.Contains(trafficWaypoint))
                        {
                            if (j < trafficWaypoint.neighbors[j].neighbors.Count)
                            {
                                trafficWaypoint.neighbors[j].neighbors.Remove(trafficWaypoint);
                            }
                            else
                            {
                                trafficWaypoint.neighbors.RemoveAt(j);
                            }
                        }
                    }
                }
            }

            //check
            //for (var i = 0; i < forwardWaypoints.Count; i++)
            //{
            //    if (forwardConnections.TryGetValue(forwardWaypoints[i], out var trafficWaypoint))
            //    {
            //        if (trafficWaypoint.neighbors.Count == 0)
            //        {
            //            Debug.Log("0 neighbors" + trafficWaypoint, trafficWaypoint);
            //        }
            //        if (trafficWaypoint.prev.Count == 0)
            //        {
            //            Debug.Log("0 prevs" + trafficWaypoint, trafficWaypoint);
            //        }

            //        for (int j = 0; j < trafficWaypoint.neighbors.Count; j++)
            //        {
            //            if (!trafficWaypoint.neighbors[j].prev.Contains(trafficWaypoint))
            //            {
            //                Debug.Log("Not linked " + trafficWaypoint.neighbors[j], trafficWaypoint);
            //            }
            //        }
            //    }
            //}

#if !GLEY_TRAFFIC_SYSTEM
            CreateIntersections(connectorsHolder, intersectionHolder);
#endif

            Debug.Log("Done");
        }


        private static void CreateIntersections(Transform holder, Transform intersectionHolder)
        {
            for (int i = 0; i < holder.childCount; i++)
            {
                if (holder.GetChild(i).childCount > 2)
                {
                    var intersection = MonoBehaviourUtilities.CreateGameObject(holder.GetChild(i).name, intersectionHolder, holder.GetChild(i).position, true);
                    var intersectionScript = intersection.AddComponent<StreetCrossingComponent>();
                    var stopWaypoints = new List<PedestrianWaypointSettings>();
                    var directionWaypoints = new List<PedestrianWaypointSettings>();
                    for (int j = 0; j < holder.GetChild(i).childCount; j++)
                    {
                        //iterate through lanes
                        for (int k = 0; k < holder.GetChild(i).GetChild(j).childCount; k++)
                        {
                            if (holder.GetChild(i).GetChild(j).GetChild(k).name.Contains(UrbanSystemConstants.ConnectionEdgeName))
                            {
                                var waypoint = holder.GetChild(i).GetChild(j).GetChild(k).GetComponent<PedestrianWaypointSettings>();
                                stopWaypoints.Add(waypoint);
                                for (int l = 0; l < waypoint.neighbors.Count; l++)
                                {
                                    if (waypoint.neighbors[l].name.Contains(UrbanSystemConstants.ConnectionWaypointName))
                                    {
                                        directionWaypoints.Add((PedestrianWaypointSettings)waypoint.neighbors[l]);
                                    }
                                }
                                for (int l = 0; l < waypoint.prev.Count; l++)
                                {
                                    if (waypoint.prev[l].name.Contains(UrbanSystemConstants.ConnectionWaypointName))
                                    {
                                        directionWaypoints.Add((PedestrianWaypointSettings)waypoint.prev[l]);
                                    }
                                }
                            }
                        }
                    }

                    intersectionScript.SetStopWaypoints(stopWaypoints);
                    intersectionScript.SetDirectionWaypoints(directionWaypoints);
                }
            }
        }


        static void RemoveDuplicateWaypoints(ref List<PampelGames.RoadConstructor.Waypoint> forwardWaypoints, ref Dictionary<PampelGames.RoadConstructor.Waypoint, PedestrianWaypointSettings> forwardConnections)
        {
            float minDistance = float.MaxValue;
            for (var i = forwardWaypoints.Count - 2; i > 0; i--)
            {
                for (var j = i - 1; j >= 0; j--)
                {
                    var distance = Vector3.SqrMagnitude(forwardWaypoints[i].transform.position - forwardWaypoints[j].transform.position);
                    if (distance < minDistance)
                    {
                        if (distance <= 0.1f)
                        {
                            var pedestrianWaypointToUpdate = forwardConnections.GetValueOrDefault(forwardWaypoints[j]);
                            var pedestrianWaypointToDelete = forwardConnections.GetValueOrDefault(forwardWaypoints[i]);

                            if (pedestrianWaypointToUpdate.neighbors.Contains(pedestrianWaypointToDelete))
                            {
                                pedestrianWaypointToUpdate.neighbors.Remove(pedestrianWaypointToDelete);
                                for (int k = 0; k < pedestrianWaypointToDelete.neighbors.Count; k++)
                                {
                                    if (!pedestrianWaypointToUpdate.neighbors.Contains(pedestrianWaypointToDelete.neighbors[k]) && pedestrianWaypointToDelete.neighbors[k] != pedestrianWaypointToUpdate)
                                    {
                                        pedestrianWaypointToUpdate.neighbors.Add(pedestrianWaypointToDelete.neighbors[k]);
                                    }

                                    pedestrianWaypointToDelete.neighbors[k].prev.Remove(pedestrianWaypointToDelete);
                                    pedestrianWaypointToDelete.neighbors[k].prev.Add(pedestrianWaypointToUpdate);
                                }
                            }
                            else
                            {
                                if (pedestrianWaypointToUpdate.prev.Contains(pedestrianWaypointToDelete))
                                {
                                    pedestrianWaypointToUpdate.prev.Remove(pedestrianWaypointToDelete);
                                    for (int k = 0; k < pedestrianWaypointToDelete.prev.Count; k++)
                                    {
                                        pedestrianWaypointToDelete.prev[k].neighbors.Remove(pedestrianWaypointToDelete);
                                        pedestrianWaypointToDelete.prev[k].neighbors.Add(pedestrianWaypointToUpdate);
                                        pedestrianWaypointToUpdate.prev.Add(pedestrianWaypointToDelete.prev[k]);
                                    }
                                }
                                else
                                {
                                    pedestrianWaypointToUpdate.prev.AddRange(pedestrianWaypointToDelete.prev);
                                    pedestrianWaypointToUpdate.neighbors.AddRange(pedestrianWaypointToDelete.neighbors);
                                    for (int k = 0; k < pedestrianWaypointToDelete.prev.Count; k++)
                                    {
                                        pedestrianWaypointToDelete.prev[k].neighbors.Remove(pedestrianWaypointToDelete);
                                        pedestrianWaypointToDelete.prev[k].prev.Remove(pedestrianWaypointToDelete);
                                        pedestrianWaypointToDelete.prev[k].neighbors.Add(pedestrianWaypointToUpdate);
                                    }

                                    for (int k = 0; k < pedestrianWaypointToDelete.neighbors.Count; k++)
                                    {
                                        pedestrianWaypointToDelete.neighbors[k].prev.Remove(pedestrianWaypointToDelete);
                                        pedestrianWaypointToDelete.neighbors[k].neighbors.Remove(pedestrianWaypointToDelete);
                                        pedestrianWaypointToDelete.neighbors[k].prev.Add(pedestrianWaypointToUpdate);
                                    }
                                }
                            }

                            forwardConnections.Remove(forwardWaypoints[i]);
                            forwardWaypoints.RemoveAt(i);
                            DestroyImmediate(pedestrianWaypointToDelete.gameObject);
                        }
                        else
                        {
                            minDistance = distance;
                        }
                    }
                }
            }
        }

        private static void CreatePedestrianWaypoints(Transform waypointsHolder, List<PampelGames.RoadConstructor.Waypoint> waypoints, float laneWidth, string name, bool intersection, List<int> vehicleTypes, bool crossing, ref Dictionary<PampelGames.RoadConstructor.Waypoint, PedestrianWaypointSettings> result)
        {
            PedestrianWaypointCreator waypointCreator = new PedestrianWaypointCreator();
            for (int i = 0; i < waypoints.Count; i++)
            {
                var waypointName = name;
                if (waypoints[i].startPoint)
                {
                    if (crossing)
                    {
                        waypointName += "-" + UrbanSystemConstants.ConnectionEdgeName + i;
                    }
                    else
                    {
                        waypointName += "-Waypoint_" + i;
                    }
                }
                else
                {
                    if (waypoints[i].endPoint)
                    {
                        if (crossing)
                        {
                            waypointName += "-" + UrbanSystemConstants.ConnectionEdgeName + i;
                        }
                        else
                        {
                            waypointName += "-Waypoint_" + i;
                        }
                    }
                    else
                    {
                        if (crossing)
                        {
                            waypointName += "-" + UrbanSystemConstants.ConnectionWaypointName + i;
                        }
                        else
                        {
                            waypointName += "-Waypoint_" + i;
                        }
                    }
                }

                //if(crossing)
                //{
                //    waypointName
                //}

                var transform = waypointCreator.CreateWaypoint(waypointsHolder, waypoints[i].transform.position, waypointName, vehicleTypes, laneWidth);
                result.Add(waypoints[i], transform.GetComponent<PedestrianWaypointSettings>());
            }
        }
    }
}
#endif