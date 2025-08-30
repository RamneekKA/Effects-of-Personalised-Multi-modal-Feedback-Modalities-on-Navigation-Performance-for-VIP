using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    [System.Serializable]
    public class PedestrianWaypoint : Waypoint
    {
        [SerializeField] private PedestrianTypes[] _allowedPedestrians;
        [SerializeField] private IIntersection _associatedIntersection;
        [SerializeField] private Vector3 _leftDirection;
        [SerializeField] private string _eventData;
        [SerializeField] private float _laneWidth;
        [SerializeField] private bool _stop;
        [SerializeField] private bool _crossing;
        [SerializeField] private bool _triggerEvent;

        public PedestrianTypes[] AllowedPedestrians => _allowedPedestrians;
        public IIntersection AssociatedIntersection => _associatedIntersection;
        public Vector3 LeftDirection => _leftDirection;
        public float LaneWidth => _laneWidth;
        public bool Crossing => _crossing;
        public bool Stop
        {
            get
            {
                return _stop;
            }
            set
            {
                _stop = value;
            }
        }

        public bool TriggerEvent
        {
            get
            {
                return _triggerEvent;
            }
            set
            {
                _triggerEvent = value;
            }
        }

        public string EventData
        {
            get
            {
                return _eventData;
            }
            set
            {
                _eventData = value;
            }
        }

        public PedestrianWaypoint(string name, int listIndex, Vector3 position, int[] neighbors, int[] prev, List<PedestrianTypes> allowedPedestrianTypes, bool crossing, float laneWidth, Vector3 leftDirection, string eventData, bool triggerEvent)
            : base(name, listIndex, position, neighbors, prev)
        {
            _crossing = crossing;
            _laneWidth = laneWidth;
            _leftDirection = leftDirection;
            _allowedPedestrians = allowedPedestrianTypes.ToArray();
            _eventData = eventData;
            _triggerEvent = triggerEvent;
        }


        public void SetIntersection(IIntersection associatedIntersection)
        {
            _associatedIntersection = associatedIntersection;
        }
    }
}
