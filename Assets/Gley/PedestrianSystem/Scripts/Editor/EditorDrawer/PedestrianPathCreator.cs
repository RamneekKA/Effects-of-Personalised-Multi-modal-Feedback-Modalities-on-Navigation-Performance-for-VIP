using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    internal class PedestrianPathCreator : RoadCreator<PedestrianPath, PedestrianConnectionPool, PedestrianConnectionCurve>
    {
        public PedestrianPathCreator(RoadEditorData<PedestrianPath> data) : base(data)
        {
        }


        internal PedestrianPath Create(int nrOfLanes, float laneWidth, float waypointDistance, string prefix, Vector3 firstClick, Vector3 secondClick, int globalMaxSpeed, int nrOfAgents, bool leftSideTraffic)
        {
            Transform roadParent = MonoBehaviourUtilities.GetOrCreateSceneInstance<PedestrianConnectionPool>(PedestrianSystemConstants.EditorWaypointsHolder,true).transform;
            int roadNumber = GleyUtilities.GetFreeRoadNumber(roadParent);
            GameObject roadHolder = MonoBehaviourUtilities.CreateGameObject(prefix + "_" + roadNumber, roadParent, firstClick, true);
            roadHolder.transform.SetSiblingIndex(roadNumber-1);
            var road = roadHolder.AddComponent<PedestrianPath>();
            road.SetDefaults(nrOfLanes, laneWidth, waypointDistance);
            road.CreatePath(firstClick, secondClick);
            road.justCreated = true;
            EditorUtility.SetDirty(road);
            AssetDatabase.SaveAssets();
            _data.TriggerOnModifiedEvent();
            return road;
        }
    }
}
