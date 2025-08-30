using Gley.UrbanSystem.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Access properties of pedestrian waypoints.
    /// </summary>
    public class PedestrianWaypointsDataHandler : IPedestrianWaypointsDataHandler
    {
        private readonly PedestrianWaypointsData _pedestrianWaypointsData;

        public PedestrianWaypointsDataHandler(PedestrianWaypointsData data)
        {
            _pedestrianWaypointsData = data;
        }


        #region Set
        public void SetIntersection(int[] waypointIndexes, IIntersection intersection)
        {
            for (int i = 0; i < waypointIndexes.Length; i++)
            {
                SetIntersection(waypointIndexes[i], intersection);
            }
        }


        internal void SetIntersection(int waypointIndex, IIntersection intersection)
        {
            GetWaypoint(waypointIndex).SetIntersection(intersection);
        }


        internal void SetStopValue(int waypointIndex, bool newValue)
        {
            if (IsStop(waypointIndex) != newValue)
            {
                GetWaypoint(waypointIndex).Stop = newValue;
            }
        }


        internal void SetTemperaryDisabledValue(int waypointIndex, bool value)
        {
            GetWaypoint(waypointIndex).TemporaryDisabled = value;
        }


        internal void SetTemperaryDisabledValue(List<int> waypointIndexes, bool value)
        {
            for (int i = 0; i < waypointIndexes.Count; i++)
            {
                SetTemperaryDisabledValue(waypointIndexes[i], value);
            }
        }
        #endregion


        #region Get
        internal Vector3 GetPosition(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).Position;
        }


        internal float GetLaneWidth(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).LaneWidth;
        }


        internal Vector3 GetLeftDirection(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).LeftDirection;
        }


        internal PedestrianWaypoint GetWaypointWithValidation(int waypointIndex)
        {
            if (IsWaypointIndexValid(waypointIndex))
            {
                return GetWaypoint(waypointIndex);
            }
            return null;
        }


        internal List<int> GetPossibleWaypoints(int waypointIndex, PedestrianTypes pedestrianType)
        {
            var possibleWaypoints = new List<int>();

            if (HasPrev(waypointIndex))
            {
                possibleWaypoints.AddRange(GetPrevsWithConditions(waypointIndex, pedestrianType));
            }
            if (HasNeighbors(waypointIndex))
            {
                possibleWaypoints.AddRange(GetNeighborsWithConditions(waypointIndex, pedestrianType));
            }

            return possibleWaypoints;
        }


        internal int[] GetNeighbors(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).Neighbors;
        }


        internal int[] GetPrevs(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).Prevs;
        }


        internal List<int> GetNeighborsWithConditions(int waypointIndex, PedestrianTypes pedestrianType)
        {
            var result = new List<int>();
            var allNeighbors = GetNeighbors(waypointIndex);
            for (int i = 0; i < allNeighbors.Length; i++)
            {
                if (GetAllowedPedestrians(allNeighbors[i]).Contains(pedestrianType) && !IsTemporaryDisabled(allNeighbors[i]))
                {
                    result.Add(allNeighbors[i]);
                }
            }
            return result;
        }


        internal List<int> GetPrevsWithConditions(int waypointIndex, PedestrianTypes pedestrianType)
        {
            var result = new List<int>();
            var allNeighbors = GetPrevs(waypointIndex);
            for (int i = 0; i < allNeighbors.Length; i++)
            {
                if (GetAllowedPedestrians(allNeighbors[i]).Contains(pedestrianType) && !IsTemporaryDisabled(allNeighbors[i]))
                {
                    result.Add(allNeighbors[i]);
                }
            }
            return result;
        }


        internal List<int> GetPrevWithConditions(int waypointIndex, PedestrianTypes pedestrianType)
        {
            var result = new List<int>();
            var allNeighbors = GetNeighbors(waypointIndex);
            for (int i = 0; i < allNeighbors.Length; i++)
            {
                if (GetAllowedPedestrians(allNeighbors[i]).Contains(pedestrianType) && !IsTemporaryDisabled(allNeighbors[i]))
                {
                    result.Add(allNeighbors[i]);
                }
            }
            return result;
        }


        internal PedestrianTypes[] GetAllowedPedestrians(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).AllowedPedestrians;
        }


        internal string GetName(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).Name;
        }


        internal IIntersection GetAssociatedIntersection(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).AssociatedIntersection;
        }


        internal bool HasPrev(int waypointIndex)
        {
            return GetPrevs(waypointIndex).Length > 0;
        }


        internal bool HasNeighbors(int waypointIndex)
        {
            return GetNeighbors(waypointIndex).Length > 0;
        }


        internal bool HasIntersection(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).AssociatedIntersection != null;
        }


        internal bool IsStop(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).Stop;
        }


        internal bool IsTemporaryDisabled(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).TemporaryDisabled;
        }


        internal bool IsCrossing(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).Crossing;
        }

        internal bool IsTriggerEvent(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).TriggerEvent;
        }

        internal string GetEventData(int waypointIndex)
        {
            return GetWaypoint(waypointIndex).EventData;
        }

        #endregion


        private PedestrianWaypoint GetWaypoint(int waypointIndex)
        {
            return _pedestrianWaypointsData.AllPedestrianWaypoints[waypointIndex];
        }


        private bool IsWaypointIndexValid(int waypointIndex)
        {
            if (waypointIndex < 0)
            {
                Debug.LogError($"Waypoint index {waypointIndex} should be >= 0");
                return false;
            }

            if (waypointIndex >= _pedestrianWaypointsData.AllPedestrianWaypoints.Length)
            {
                Debug.LogError($"Waypoint index {waypointIndex} should be < {_pedestrianWaypointsData.AllPedestrianWaypoints.Length}");
                return false;
            }

            if (_pedestrianWaypointsData.AllPedestrianWaypoints[waypointIndex] == null)
            {
                Debug.LogError($"Waypoint at {waypointIndex} is null, Verify the setup");
                return false;
            }

            return true;
        }
    }
}