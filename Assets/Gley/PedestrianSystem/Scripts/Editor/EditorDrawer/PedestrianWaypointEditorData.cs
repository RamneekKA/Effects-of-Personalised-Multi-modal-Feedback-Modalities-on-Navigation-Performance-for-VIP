using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using System.Collections.Generic;

namespace Gley.PedestrianSystem.Editor
{
    public class PedestrianWaypointEditorData : EditorData
    {
        private PedestrianWaypointSettings[] _allWaypoints;
        private PedestrianWaypointSettings[] _disconnectedWaypoints;
        private PedestrianWaypointSettings[] _pedestrianTypeEditedWaypoints;
        private PedestrianWaypointSettings[] _priorityEditedWaypoints;
        private PedestrianWaypointSettings[] _eventWaypoints;
        private PedestrianWaypointSettings[] _penaltyEditedWaypoints;

        public PedestrianWaypointEditorData()
        {
            LoadAllData();
        }


        public PedestrianWaypointSettings[] GetAllWaypoints()
        {
            return _allWaypoints;
        }


        public PedestrianWaypointSettings[] GetDisconnectedWaypoints()
        {
            return _disconnectedWaypoints;
        }


        public PedestrianWaypointSettings[] GetPedestrianTypeEditedWaypoints()
        {
            return _pedestrianTypeEditedWaypoints;
        }


        public PedestrianWaypointSettings[] GetPriorityEditedWaypoints()
        {
            return _priorityEditedWaypoints;
        }


        public PedestrianWaypointSettings[] GetEventWaypoints()
        {
            return _eventWaypoints;
        }


        public PedestrianWaypointSettings[] GetPenlatyEditedWaypoints()
        {
            return _penaltyEditedWaypoints;
        }


        protected override void LoadAllData()
        {
            _allWaypoints = GleyPrefabUtilities.GetAllComponents<PedestrianWaypointSettings>();

            List<PedestrianWaypointSettings> disconnectedWaypoints = new List<PedestrianWaypointSettings>();
            List<PedestrianWaypointSettings> pedestrianTypeEditedWaypoints = new List<PedestrianWaypointSettings>();
            List<PedestrianWaypointSettings> priorityEditedWaypoints = new List<PedestrianWaypointSettings>();
            List<PedestrianWaypointSettings> eventWaypoints = new List<PedestrianWaypointSettings>();
            List<PedestrianWaypointSettings> penaltyEditedWaypoints = new List<PedestrianWaypointSettings>();

            for (int i = 0; i < _allWaypoints.Length; i++)
            {
                _allWaypoints[i].position = _allWaypoints[i].transform.position;
                _allWaypoints[i].VerifyAssignments(false);

                if (_allWaypoints[i].neighbors.Count == 0 )
                {
                    disconnectedWaypoints.Add(_allWaypoints[i]);
                }

                if (_allWaypoints[i].PedestrianLocked)
                {
                    pedestrianTypeEditedWaypoints.Add(_allWaypoints[i]);
                }


                if (_allWaypoints[i].priorityLocked)
                {
                    priorityEditedWaypoints.Add(_allWaypoints[i]);
                }

                if (_allWaypoints[i].triggerEvent)
                {
                    eventWaypoints.Add(_allWaypoints[i]);
                }

                if (_allWaypoints[i].penaltyLocked)
                {
                    penaltyEditedWaypoints.Add(_allWaypoints[i]);
                }
            }

            _disconnectedWaypoints = disconnectedWaypoints.ToArray();
            _pedestrianTypeEditedWaypoints = pedestrianTypeEditedWaypoints.ToArray();
            _priorityEditedWaypoints = priorityEditedWaypoints.ToArray();
            _eventWaypoints = eventWaypoints.ToArray();
            _penaltyEditedWaypoints = penaltyEditedWaypoints.ToArray();
        }
    }
}
