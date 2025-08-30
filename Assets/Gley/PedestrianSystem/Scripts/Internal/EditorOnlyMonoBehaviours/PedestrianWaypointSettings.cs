using Gley.UrbanSystem.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Store waypoint properties.
    /// </summary>
    public class PedestrianWaypointSettings : WaypointSettingsBase
    {
        [SerializeField] private List<PedestrianTypes> _allowedPedestrians;
        [SerializeField] private Vector3 _left;
        [SerializeField] private float _laneWidth;
        [SerializeField] private bool _pedestrianLocked;
        [SerializeField] private bool _inIntersection;
        [SerializeField] private bool _crossing;

        public List<PedestrianTypes> AllowedPedestrians
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

        public Vector3 Left
        {
            get
            {
                return _left;
            }
            set
            {
                _left = value;
            }
        }

        public float LaneWidth
        {
            get
            {
                return _laneWidth;
            }
            set
            {
                _laneWidth = value;
            }
        }

        public bool Crossing
        {
            get
            {
                return _crossing;
            }
            set
            {
                _crossing = value;
            }
        }

        public bool PedestrianLocked
        {
            get
            {
                return _pedestrianLocked;
            }
        }

        public bool InIntersection
        {
            get
            {
                return _inIntersection;
            }
            set
            {
                _inIntersection = value;
            }
        }


        public void ResetProperties()
        {
            InIntersection = false;
            Crossing = false;
        }


        public override void VerifyAssignments(bool showPrevsWarning)
        {
            base.VerifyAssignments(showPrevsWarning);
            if (_allowedPedestrians == null)
            {
                _allowedPedestrians = new List<PedestrianTypes>();
            }

            for (int i = _allowedPedestrians.Count - 1; i >= 0; i--)
            {
                if (!IsValid((int)_allowedPedestrians[i]))
                {
                    _allowedPedestrians.RemoveAt(i);
                }
            }
        }


        public void SetPedestrianTypesForAllNeighbors(List<PedestrianTypes> allowedPedestrians)
        {
            Queue<PedestrianWaypointSettings> queue = new Queue<PedestrianWaypointSettings>();
            HashSet<WaypointSettingsBase> visited = new HashSet<WaypointSettingsBase>();

            // Start with the current waypoint
            queue.Enqueue(this);
            visited.Add(this);
            _pedestrianLocked = false;

            while (queue.Count > 0)
            {
                PedestrianWaypointSettings current = queue.Dequeue();
                if (!current._pedestrianLocked)
                {
                    current._allowedPedestrians = allowedPedestrians;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(current);
#endif
                    // Enqueue all unvisited neighbors
                    foreach (WaypointSettingsBase neighbor in current.neighbors)
                    {
                        if (!visited.Contains(neighbor))
                        {
                            queue.Enqueue((PedestrianWaypointSettings)neighbor);
                            visited.Add(neighbor);
                        }
                    }
                }
            }
            if (allowedPedestrians.Count > 0)
            {
                _pedestrianLocked = true;
            }
            Debug.Log("Done");
        }


        private bool IsValid(int value)
        {
            return Enum.IsDefined(typeof(PedestrianTypes), value);
        }
    }
}
