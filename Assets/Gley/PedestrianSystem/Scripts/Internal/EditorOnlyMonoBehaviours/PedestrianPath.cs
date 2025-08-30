using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Store path properties.
    /// </summary>
    public class PedestrianPath : RoadBase
    {
        [SerializeField] private bool[] _allowedPedestrians;

        public bool[] AllowedPedestrians
        {
            get
            {
                return _allowedPedestrians;
            }
            set
            {
                _allowedPedestrians = value;
            }
        }


        public override bool VerifyAssignments()
        {
            if (path == null || path.NumPoints < 4)
            {
                Debug.LogWarning($"{name} is corrupted and will be deleted");
                return false;
            }

            if (!justCreated)
            {
                if (waypointDistance <= 0)
                {
                    waypointDistance = 1;
                }
            }
            return true;
        }


        public void SetDefaults(int nrOfLanes, float laneWidth, float waypointDistance)
        {
            this.nrOfLanes = nrOfLanes;
            this.laneWidth = laneWidth;
            this.waypointDistance = waypointDistance;
            draw = true;
        }


        public List<int> GetAllowedPedestrians()
        {
            List<int> result = new List<int>();
            for (int i = 0; i <_allowedPedestrians.Length; i++)
            {
                if (_allowedPedestrians[i] == true)
                {
                    result.Add(i);
                }
            }
            return result;
        }
    }
}