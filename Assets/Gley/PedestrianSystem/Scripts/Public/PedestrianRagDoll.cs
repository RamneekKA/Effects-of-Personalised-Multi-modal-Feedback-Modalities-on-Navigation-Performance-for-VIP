using UnityEngine;

namespace Gley.PedestrianSystem
{
    /// <summary>
    /// Reset ragdoll after it is used
    /// </summary>
    public class PedestrianRagDoll : MonoBehaviour
    {
        private Rigidbody[] _rigidbodies;
        private Collider[] _colliders;
        private Vector3[] _initialPositions;
        private Quaternion[] _initialRotations;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;


        public virtual PedestrianRagDoll Initialize()
        {
            StoreInitialTransforms();
            return this;
        }

        public Collider[] GetColliders()
        { 
            return _colliders; 
        }

        public Vector3 GetPosition()
        {
            return _rigidbodies[0].position;
        }

        public virtual void ResetRagDoll()
        {
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            int index = 0;
            foreach (Rigidbody rb in _rigidbodies)
            {
                rb.position = _initialPositions[index];
                rb.rotation = _initialRotations[index];
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
                index++;
            }

            foreach (Collider col in _colliders)
            {
                col.transform.position = _initialPositions[index];
                col.transform.rotation = _initialRotations[index];
                index++;
            }
        }


        private void StoreInitialTransforms()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _rigidbodies = GetComponentsInChildren<Rigidbody>();
            _colliders = GetComponentsInChildren<Collider>();

            _initialPositions = new Vector3[_rigidbodies.Length + _colliders.Length];
            _initialRotations = new Quaternion[_rigidbodies.Length + _colliders.Length];

            int index = 0;
            foreach (Rigidbody rb in _rigidbodies)
            {
                _initialPositions[index] = rb.position;
                _initialRotations[index] = rb.rotation;
                index++;
            }

            foreach (Collider col in _colliders)
            {
                _initialPositions[index] = col.transform.position;
                _initialRotations[index] = col.transform.rotation;
                index++;
            }
        }
    }
}
