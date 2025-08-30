using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    internal class PedestrianLaneCreator
    {
        private readonly PedestrianWaypointCreator _waypointCreator;
        private readonly PedestrianLaneEditorData _laneData;


        internal PedestrianLaneCreator (PedestrianLaneEditorData laneData, PedestrianWaypointCreator waypointCreator)
        {
            _waypointCreator = waypointCreator;
            _laneData = laneData;
        }


        internal void GenerateWaypoints(PedestrianPath road, int groundLayerMask)
        {
            ClearOldWaypointConnections(road.transform);

            GleyPrefabUtilities.ClearAllChildObjects(road.transform);

            List<Transform> helpingPoints = SplitBezierIntoPoints.CreatePoints(road);

            AddFinalWaypoints(road, helpingPoints, groundLayerMask);

            LinkNeighbors(road);

            SetPerpendicularDirection(road);

            DeleteHelpingPoints(helpingPoints);
            GleyPrefabUtilities.ApplyPrefabInstance(road.gameObject);
            _laneData.TriggerOnModifiedEvent();
        }


        private void SetPerpendicularDirection(PedestrianPath road)
        {
            Transform lanesHolder = road.transform.Find("Lanes");
            if (lanesHolder)
            {
                for (int i = 0; i < lanesHolder.childCount; i++)
                {
                    for (int j = 0; j < lanesHolder.GetChild(i).childCount; j++)
                    {
                        PedestrianWaypointSettings waypoint = lanesHolder.GetChild(i).GetChild(j).GetComponent<PedestrianWaypointSettings>();
                        Vector3 forward = Vector3.zero;
                        foreach (PedestrianWaypointSettings neighbor in waypoint.neighbors)
                        {
                            if (waypoint != null)
                            {
                                forward += neighbor.transform.position - waypoint.transform.position;
                            }
                        }
                        foreach (PedestrianWaypointSettings prev in waypoint.prev)
                        {
                            forward += waypoint.transform.position - prev.transform.position;
                        }
                        forward.Normalize();

                        waypoint.Left = Vector3.Cross(Vector3.up, forward).normalized;
                    }

                }
            }
        }


        private Transform AddLaneHolder(Transform parent, string laneName)
        {
            return MonoBehaviourUtilities.CreateGameObject(laneName, parent, parent.position, true).transform;
        }


        private bool PositionIsValid(List<Transform> helpingPoints, Vector3 waypointPosition, float limit)
        {
            for (int i = 0; i < helpingPoints.Count; i++)
            {
                if (Vector3.Distance(helpingPoints[i].position, waypointPosition) < limit)
                {
                    return false;
                }
            }
            return true;
        }


        private Vector3 PutWaypointOnRoad(Vector3 waypointPosition, Vector3 perpendicular, int groundLayermask)
        {
            if (GleyPrefabUtilities.EditingInsidePrefab())
            {
                if (GleyPrefabUtilities.GetScenePrefabRoot().scene.GetPhysicsScene().Raycast(waypointPosition + 5 * perpendicular, -perpendicular, out RaycastHit hitInfo, Mathf.Infinity, groundLayermask))
                {
                    return hitInfo.point;
                }
            }
            else
            {
                if (Physics.Raycast(waypointPosition + 5 * perpendicular, -perpendicular, out RaycastHit hitInfo, Mathf.Infinity, groundLayermask))
                {
                    return hitInfo.point;
                }
            }
            return waypointPosition;
        }


        private void DeleteHelpingPoints(List<Transform> helpingPoints)
        {
            GleyPrefabUtilities.DestroyImmediate(helpingPoints[0].transform.parent.gameObject);
        }


        private void LinkNeighbors(PedestrianPath road)
        {
            for (int i = 0; i < road.nrOfLanes; i++)
            {
                Transform laneHolder = road.transform.Find(UrbanSystemConstants.LanesHolderName).Find(UrbanSystemConstants.LaneNamePrefix + i);
                WaypointSettingsBase previousWaypoint = laneHolder.GetChild(0).GetComponent<WaypointSettingsBase>();
                for (int j = 1; j < laneHolder.childCount; j++)
                {
                    string waypointName = laneHolder.GetChild(j).name;
                    WaypointSettingsBase waypointScript = laneHolder.GetChild(j).GetComponent<WaypointSettingsBase>();
                    if (previousWaypoint != null)
                    {
                        previousWaypoint.neighbors.Add(waypointScript);
                        waypointScript.prev.Add(previousWaypoint);
                    }

                    if (!waypointName.Contains("Output"))
                    {
                        previousWaypoint = waypointScript;
                    }
                    else
                    {
                        previousWaypoint = null;
                    }
                }
                if (road.path.IsClosed)
                {
                    WaypointSettingsBase first = laneHolder.GetChild(0).GetComponent<WaypointSettingsBase>();
                    WaypointSettingsBase last = laneHolder.GetChild(laneHolder.childCount - 1).GetComponent<WaypointSettingsBase>();
                    last.neighbors.Add(first);
                    first.prev.Add(last);
                }
            }
        }


        private void AddFinalWaypoints(PedestrianPath road, List<Transform> helpingPoints, int roadLayerMask)
        {
            float startPosition;
            if (road.nrOfLanes % 2 == 0)
            {
                startPosition = -road.laneWidth / 2;
            }
            else
            {
                startPosition = 0;
            }

            int laneModifier = 0;

            Transform lanesHolder = MonoBehaviourUtilities.CreateGameObject(UrbanSystemConstants.LanesHolderName, road.transform, road.transform.position, true).transform;

            for (int i = 0; i < road.nrOfLanes; i++)
            {
                Transform laneHolder = AddLaneHolder(lanesHolder, UrbanSystemConstants.LaneNamePrefix + i);
                if (i % 2 == 0)
                {
                    laneModifier = -laneModifier;
                }
                else
                {
                    laneModifier = Mathf.Abs(laneModifier) + 1;
                }

                List<Transform> finalPoints = new List<Transform>();
                string waypointName;
                Vector3 waypointPosition;

                for (int j = 0; j < helpingPoints.Count - 1; j++)
                {
                    waypointPosition = helpingPoints[j].position + (startPosition + laneModifier * road.laneWidth) * helpingPoints[j].right;
                    if (PositionIsValid(helpingPoints, waypointPosition, Mathf.Abs(startPosition + laneModifier * road.laneWidth) - 0.1f))
                    {
                        waypointPosition = PutWaypointOnRoad(waypointPosition, helpingPoints[j].up, roadLayerMask);
                        if (PositionIsValid(finalPoints, waypointPosition, road.waypointDistance))
                        {
                            waypointName = road.name + "-" + UrbanSystemConstants.LaneNamePrefix + i + "-" + UrbanSystemConstants.WaypointNamePrefix + j;
                            finalPoints.Add(_waypointCreator.CreateWaypoint(laneHolder, waypointPosition, waypointName, road.GetAllowedPedestrians(), road.laneWidth));
                        }
                    }
                }

                //add last point from the list
                if (!road.path.IsClosed)
                {
                    waypointPosition = helpingPoints[helpingPoints.Count - 1].position + (startPosition + laneModifier * road.laneWidth) * helpingPoints[helpingPoints.Count - 1].right;
                    waypointPosition = PutWaypointOnRoad(waypointPosition, helpingPoints[helpingPoints.Count - 1].up, roadLayerMask);
                    waypointName = road.name + "-" + UrbanSystemConstants.LaneNamePrefix + i + "-" + UrbanSystemConstants.WaypointNamePrefix + helpingPoints.Count;
                    finalPoints.Add(_waypointCreator.CreateWaypoint(laneHolder, waypointPosition, waypointName, road.GetAllowedPedestrians(), road.laneWidth));
                }
            }
        }


        private void ClearOldWaypointConnections(Transform holder)
        {
            WaypointSettingsBase[] allWaypoints = holder.GetComponentsInChildren<WaypointSettingsBase>();
            for (int i = 0; i < allWaypoints.Length; i++)
            {
                WaypointSettingsBase waypoint = allWaypoints[i];
                for (int j = 0; j < waypoint.neighbors.Count; j++)
                {
                    if (waypoint.neighbors[j] != null)
                    {
                        waypoint.neighbors[j].prev.Remove(waypoint);
                    }
                    else
                    {
                        Debug.LogError(waypoint.name + " has null neighbors", waypoint);
                    }
                }
                for (int j = 0; j < waypoint.prev.Count; j++)
                {
                    if (waypoint.prev[j] != null)
                    {
                        waypoint.prev[j].neighbors.Remove(waypoint);
                    }
                    else
                    {
                        Debug.LogError(waypoint.name + " has null prevs", waypoint);
                    }
                }
            }
        }
    }
}
