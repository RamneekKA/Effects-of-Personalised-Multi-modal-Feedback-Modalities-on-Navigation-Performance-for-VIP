using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Jobs;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Store all pedestrians used and performs operations on pedestrian lists.
    /// </summary>
    internal class AllPedestrians : IDestroyable
    {
        private readonly List<Pedestrian> _allPedestrians = new List<Pedestrian>();
        private readonly List<Pedestrian> _idlePedestrians = new List<Pedestrian>();

        private TransformAccessArray _allPedestrianTransforms;


        internal AllPedestrians(Transform parent, PedestrianPool pedestrianPool, int maxNumberOfPedestrians, LayerMask buildingLayers, LayerMask obstacleLayers, LayerMask playerLayers, IBehaviourList pedestrianBehaviours)
        {
            _allPedestrianTransforms = new TransformAccessArray(maxNumberOfPedestrians);

            var pedestriansHolder = MonoBehaviourUtilities.CreateGameObject(PedestrianSystemConstants.PedestriansHolderName, parent, parent.position, false).transform;

            // Transform percent into numbers.
            int pedestriansToInstantiate = pedestrianPool.Pedestrians.Length;
            if (pedestriansToInstantiate > maxNumberOfPedestrians)
            {
                pedestriansToInstantiate = maxNumberOfPedestrians;
            }
            for (int i = 0; i < pedestriansToInstantiate; i++)
            {
                LoadPedestrian(pedestrianPool.Pedestrians[i].PedestrianPrefab, pedestriansHolder, buildingLayers, obstacleLayers, playerLayers, pedestrianPool.Pedestrians[i].DontInstantiate, pedestrianBehaviours.GetBehaviours());
            }
            maxNumberOfPedestrians -= pedestriansToInstantiate;

            float sum = 0;
            List<float> thresholds = new List<float>();
            for (int i = 0; i < pedestrianPool.Pedestrians.Length; i++)
            {
                sum += pedestrianPool.Pedestrians[i].Percent;
                thresholds.Add(sum);
            }
            float perPedestrianValue = sum / maxNumberOfPedestrians;

            // Load pedestrians.
            int pedestrianIndex = 0;
            for (int i = 0; i < maxNumberOfPedestrians; i++)
            {
                while ((i + 1) * perPedestrianValue > thresholds[pedestrianIndex])
                {
                    pedestrianIndex++;
                    if (pedestrianIndex >= pedestrianPool.Pedestrians.Length)
                    {
                        pedestrianIndex = pedestrianPool.Pedestrians.Length - 1;
                        break;
                    }
                }

                LoadPedestrian(pedestrianPool.Pedestrians[pedestrianIndex].PedestrianPrefab, pedestriansHolder, buildingLayers, obstacleLayers, playerLayers, pedestrianPool.Pedestrians[pedestrianIndex].DontInstantiate, pedestrianBehaviours.GetBehaviours());
            }

            if (_allPedestrians.Count < 1)
            {
                Debug.LogError("The Pedestrian Pool is empty, please add at least one valid pedestrian GameObject");
            }
            Assign();
        }

        public void Assign()
        {
            DestroyableManager.Instance.Register(this);
        }


        #region All
        internal List<Pedestrian> GetAllPedestrians()
        {
            return _allPedestrians;
        }


        internal Pedestrian GetPedestrianWithValidation(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return GetPedestrian(pedestrianIndex);
            }
            return null;
        }


        internal void ActivatePedestrian(Pedestrian pedestrian, Vector3 position, Quaternion rotation)
        {
            pedestrian.ActivatePedestrian(position, rotation);
        }


        internal void DisablePedestrian(int pedestrianIndex)
        {
            if (!IsPedestrianIndexValid(pedestrianIndex))
                return;

            if (!GetPedestrian(pedestrianIndex).Excluded)
            {
                _idlePedestrians.Add(GetPedestrian(pedestrianIndex));
            }
            GetPedestrian(pedestrianIndex).DeactivatePedestrian();
        }


        internal PedestrianTypes GetPedestrianType(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return GetPedestrian(pedestrianIndex).Type;
            }
            return default;
        }


        internal void SetPedestrianBehaviours(int pedestrianIndex, List<PedestrianBehaviour> pedestrianBehaviours)
        {
            GetPedestrianWithValidation(pedestrianIndex)?.SetPedestrianBehaviours(pedestrianBehaviours);
        }


        internal void SetAllPedestriansBehaviours(IBehaviourList pedestrianBehaviours)
        {
            if (pedestrianBehaviours == null)
            {
                Debug.LogError("PedestrianBehaviours not implemented");
                return;
            }
            for (int i = 0; i < _allPedestrians.Count; i++)
            {
                SetPedestrianBehaviours(i, pedestrianBehaviours.GetBehaviours());
            }
        }


        internal void StartBehavior(int pedestrianIndex, string behaviourName)
        {
            GetPedestrianWithValidation(pedestrianIndex)?.StartBehavior(behaviourName);
        }


        internal void StopBehaviour(int pedestrianIndex, string behaviourName)
        {
            GetPedestrianWithValidation(pedestrianIndex)?.StopBehaviour(behaviourName);
        }


        internal PedestrianBehaviour GetPedestrianBehaviour(int pedestrianIndex, string behaviourName)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return GetPedestrian(pedestrianIndex).GetBehaviour(behaviourName);
            }
            return null;
        }


        internal bool CanBeRemoved(int pedestrianIndex)
        {
            return GetPedestrian(pedestrianIndex).CanBeRemoved();
        }


        internal void TriggerColliderRemovedEvent(Collider[] colliders)
        {
            for (int i = 0; i < _allPedestrians.Count; i++)
            {
                if (_allPedestrians[i])
                {
                    _allPedestrians[i].ColliderRemoved(colliders);
                }
            }
        }


        internal Vector3 GetPosition(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                if (_allPedestrians[pedestrianIndex].RagDollIsActive)
                {
                    return _allPedestrians[pedestrianIndex].GetRagDollPosition();
                }
                return _allPedestrianTransforms[pedestrianIndex].position;
            }
            return default;
        }


        internal Vector3 GetForwardVector(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _allPedestrianTransforms[pedestrianIndex].forward;
            }
            return default;
        }


        internal PedestrianBehaviour GetCurrentBehaviour(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _allPedestrians[pedestrianIndex].CurrentBehaviour;
            }
            return default;
        }


        internal void UpdateOffset(int pedestrianIndex, float offset)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                _allPedestrians[pedestrianIndex].Offset = offset;
            }
        }


        internal bool IsActive(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                return _allPedestrians[pedestrianIndex].IsActive;
            }
            return false;
        }
        #endregion


        #region Idle
        internal Pedestrian GetIdlePedestrianWithVaidation(int idlePedestrianIndex)
        {
            if (IsIdlePedestrianIndexValid(idlePedestrianIndex))
            {
                return GetIdlePedestrian(idlePedestrianIndex);
            }
            return null;
        }


        internal Pedestrian GetIdlePedestrianOfType(PedestrianTypes type)
        {
            var possiblePedestrians = _idlePedestrians.Where(cond => cond.Type == type).ToArray();
            if (possiblePedestrians.Length > 0)
            {
                return possiblePedestrians[Random.Range(0, possiblePedestrians.Length)];
            }
            return null;
        }


        internal PedestrianTypes GetIdlePedestrianType(int idlePedestrianIndex)
        {
            if (IsPedestrianIndexValid(idlePedestrianIndex))
            {
                return GetIdlePedestrian(idlePedestrianIndex).Type;
            }
            return default;
        }


        internal int GetRandomIdlePedestrianIndex()
        {
            if (_idlePedestrians.Count <= 0)
            {
                return PedestrianSystemConstants.INVALID_PEDESTRIAN_INDEX;
            }
            return Random.Range(0, _idlePedestrians.Count);
        }


        internal void RemoveIdlePedestrian(Pedestrian pedestrian)
        {
            _idlePedestrians.Remove(pedestrian);
        }
        #endregion


        #region Excluded
        internal List<Pedestrian> GetExcludedPedestrianList()
        {
            return _allPedestrians.Where(cond => cond.Excluded == true).ToList();
        }


        internal void ExcludePedestrianFromSystem(int pedestrianIndex)
        {
            if (IsPedestrianIndexValid(pedestrianIndex))
            {
                GetPedestrian(pedestrianIndex).Excluded = true;
                RemoveIdlePedestrian(GetPedestrian(pedestrianIndex));
            }
        }


        internal void AddExcludedPedestrianToSystem(int pedestrainIndex)
        {
            if (IsPedestrianIndexValid(pedestrainIndex))
            {
                GetPedestrian(pedestrainIndex).Excluded = false;
                // If the pedestrian is not already in idle list, and it is not active -> add it to idle list.
                if (!_idlePedestrians.Contains(GetPedestrian(pedestrainIndex)) && !GetPedestrian(pedestrainIndex).gameObject.activeSelf)
                {
                    _idlePedestrians.Add(GetPedestrian(pedestrainIndex));
                }
            }
        }
        #endregion


        private void LoadPedestrian(GameObject pedestrianPrefab, Transform pedestriansHolder, LayerMask buildingLayers, LayerMask obstacleLayers, LayerMask playerLayers, bool excluded, List<PedestrianBehaviour> pedestrianBehaviours)
        {
            if (pedestrianPrefab == null)
                return;
            Pedestrian pedestrian = MonoBehaviourUtilities.Instantiate(pedestrianPrefab, Vector3.zero, Quaternion.identity, pedestriansHolder).GetComponent<Pedestrian>()
                .Initialize(buildingLayers, obstacleLayers, playerLayers, _allPedestrians.Count, excluded, pedestrianBehaviours);
            _allPedestrians.Add(pedestrian);
            _allPedestrianTransforms.Add(pedestrian.transform);
            DisablePedestrian(pedestrian.PedestrianIndex);
        }


        private Pedestrian GetPedestrian(int pedestrianIndex)
        {
            return _allPedestrians[pedestrianIndex];
        }


        private bool IsPedestrianIndexValid(int pedestrianIndex)
        {
            if (pedestrianIndex < 0 || pedestrianIndex >= _allPedestrians.Count)
            {
                Debug.LogError($"PedestrianIndex {pedestrianIndex} is not valid. Must be in the interval [0,{_allPedestrians.Count - 1}]");
                return false;
            }
            if (_allPedestrians[pedestrianIndex] == null)
            {
                Debug.LogError($"Pedestrian at position {pedestrianIndex} is null");
                return false;
            }
            return true;
        }


        private bool IsIdlePedestrianIndexValid(int idlePedestrianIndex)
        {
            if (idlePedestrianIndex < 0 || idlePedestrianIndex >= _idlePedestrians.Count)
            {
                Debug.LogError($"IdlePedestrianIndex {idlePedestrianIndex} is not valid. Must be in the interval [0,{_idlePedestrians.Count - 1}]");
                return false;
            }

            if (_idlePedestrians[idlePedestrianIndex] == null)
            {
                Debug.LogError($"Idle pedestrian at position {idlePedestrianIndex} is null");
                return false;
            }

            return true;
        }


        private Pedestrian GetIdlePedestrian(int idlePedestrianIndex)
        {
            return _idlePedestrians[idlePedestrianIndex];
        }


        public void OnDestroy()
        {
            _allPedestrianTransforms.Dispose();
        }
    }
}
