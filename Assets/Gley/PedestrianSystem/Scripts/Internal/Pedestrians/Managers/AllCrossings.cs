using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Controls the Street Crossing Components.
    /// </summary>
    internal class AllCrossings
    {
        private readonly Dictionary<string, List<StreetCrossingComponent>> _allCrossings;


        /// <summary>
        /// Store and initialize all Crossing Components.
        /// </summary>
        internal AllCrossings(PedestrianWaypointsDataHandler pedestrianWaypointsDataHandler)
        {
            _allCrossings = new Dictionary<string, List<StreetCrossingComponent>>();

            var crossings = MonoBehaviourUtilities.FindObjectsByType<StreetCrossingComponent>();
            foreach (var crossing in crossings)
            {
                crossing.Initialize(pedestrianWaypointsDataHandler);

                if (_allCrossings.TryGetValue(crossing.CrossingName, out var result))
                {
                    result.Add(crossing);
                    _allCrossings[crossing.CrossingName] = result;
                }
                else
                {
                    _allCrossings.Add(crossing.CrossingName, new List<StreetCrossingComponent> { crossing });
                }
            }
        }


        /// <summary>
        /// Set the stop waypoints state for a specific crossing.
        /// </summary>
        internal void SetStopWaypointState(string crossingName, bool stopState)
        {
            if (_allCrossings.TryGetValue(crossingName, out var selectedCrossings))
            {
                foreach (var crossing in selectedCrossings)
                {
                    crossing.SetStopWaypointsState(stopState);
                }
            }
            else
            {
                Debug.LogError($"{crossingName} not found. Make sure a crossing with that name exists.");
            }
        }


        /// <summary>
        /// Get the stop waypoint state for a specific crossing.
        /// </summary>
        internal bool GetstopWaypointState(string crossingName)
        {
            if (_allCrossings.TryGetValue(crossingName, out var selectedCrossings))
            {
                return selectedCrossings[0].GetStopState();
            }
            return false;
        }

        internal Dictionary<string, List<StreetCrossingComponent>> GetAllCrossings()
        {
            return _allCrossings;
        }
    }
}
