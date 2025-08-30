using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    /// <summary>
    /// Custom editor for configuring the pedestrian prefabs.
    /// </summary>
    [CustomEditor(typeof(Pedestrian))]
    internal class PedestrianEditor : UnityEditor.Editor
    {
        private readonly string _frontTriggerName = "FrontTrigger";
        // Private variables names from Pedestrian
        private readonly string _visibilityScriptFieldName = "_visibilityScript";
        private readonly string _minWalkSpeedFieldName = "_minWalkSpeed";
        private readonly string _maxWalkSpeedFieldName = "_maxWalkSpeed";
        private readonly string _triggerLengthFieldName = "_triggerLength";
        private readonly string _hasRagdollFieldName = "_hasRagdoll";
        private readonly string _ragDollPrefabFieldName = "_ragDollPrefab";

        private Pedestrian _targetScript;
        private FieldInfo _hasRagdoll;
        private FieldInfo _ragDollPrefab;


        private void OnEnable()
        {
            _targetScript = target as Pedestrian;
            _hasRagdoll = CustomEditorMethods.GetPrivateField<Pedestrian>(_hasRagdollFieldName);
            _ragDollPrefab = CustomEditorMethods.GetPrivateField<Pedestrian>(_ragDollPrefabFieldName);
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            _hasRagdoll.SetValue(_targetScript, EditorGUILayout.Toggle("Has Ragdoll", (bool)_hasRagdoll.GetValue(_targetScript)));
            if ((bool)_hasRagdoll.GetValue(_targetScript))
            {
                _ragDollPrefab.SetValue(_targetScript, (GameObject)EditorGUILayout.ObjectField("Ragdoll", (GameObject)_ragDollPrefab.GetValue(_targetScript), typeof(GameObject), false));
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Assign Pedestrian"))
            {
                ConfigurePedestrian(_targetScript);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("View Tutorial"))
            {
                Application.OpenURL("");
            }
        }


        private void ConfigurePedestrian(Pedestrian targetScript)
        {
            bool correct = true;

            SetupRigidbody(targetScript, ref correct);
            SetupAnimator(targetScript, ref correct);
            CheckColliders(targetScript, ref correct);
            AssignVisibilityScript(targetScript);
            CheckForZero(targetScript, _minWalkSpeedFieldName, ref correct);
            CheckForZero(targetScript, _maxWalkSpeedFieldName, ref correct);
            CheckForZero(targetScript, _triggerLengthFieldName, ref correct);
            CheckLayer(targetScript);
            if ((bool)_hasRagdoll.GetValue(targetScript))
            {
                CheckRagdoll(targetScript, ref correct);
            }
            CreateFrontTrigger(targetScript, ref correct);

            if (correct)
            {
                Debug.Log("Success! All references for " + targetScript.name + " ware correct.");
                Save(targetScript);
            }
            else
            {
                Debug.LogError(targetScript.name + " will not work correctly. See above messages for details.");
            }
        }


        private void CreateFrontTrigger(Pedestrian targetScript, ref bool correct)
        {
            if (!correct)
                return;

            Transform frontTrigger = targetScript.transform.Find(_frontTriggerName);
            GleyPrefabUtilities.DestroyTransform(frontTrigger);
            if (frontTrigger == null)
            {
                if (GleyPrefabUtilities.EditingInsidePrefab())
                {
                    GameObject prefabRoot = GleyPrefabUtilities.GetScenePrefabRoot();
                    frontTrigger = new GameObject(_frontTriggerName).transform;
                    frontTrigger.SetParent(prefabRoot.transform, false);
                }
                else
                {
                    frontTrigger = new GameObject(_frontTriggerName).transform;
                    frontTrigger.SetParent(targetScript.transform, false);
                }
            }
            Collider[] allColliders = targetScript.GetComponentsInChildren<Collider>();
            Vector2 size = CalculateTotalBounds(allColliders);

            float triggerLength = (float)CustomEditorMethods.GetPrivateField<Pedestrian>(_triggerLengthFieldName).GetValue(targetScript);

            BoxCollider boxCollider = frontTrigger.gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(size.x, size.y, triggerLength);
            boxCollider.center = new Vector3(0, 0, triggerLength / 2);
            boxCollider.isTrigger = true;
            frontTrigger.gameObject.SetLayer(targetScript.gameObject.layer);
            frontTrigger.transform.localPosition = new Vector3(0, size.y / 2, 0);
        }


        private Vector2 CalculateTotalBounds(Collider[] colliders)
        {
            Bounds totalBounds = colliders[0].bounds;

            for (int i = 1; i < colliders.Length; i++)
            {
                totalBounds.Encapsulate(colliders[i].bounds);
            }

            return new Vector2(totalBounds.size.x, totalBounds.size.y);
        }


        private void CheckRagdoll(Pedestrian targetScript, ref bool correct)
        {
            if (_ragDollPrefab.GetValue(targetScript) == null)
            {
                LogError(ref correct, $"Ragdoll prefab not assigned -> Please assign a ragdoll on {targetScript.name} or disable HasRagdoll.");
            }
        }


        private void CheckLayer(Pedestrian targetScript)
        {
            var layerSetup = FileCreator.LoadScriptableObject<LayerSetup>(Internal.PedestrianSystemConstants.LayerPath);
            if (!IsLayerInLayerMask(targetScript.gameObject.layer, layerSetup.PedestrianLayers))
            {
                Debug.LogWarning($"Layer: {LayerMask.LayerToName(targetScript.gameObject.layer)} is not included inside Pedestrian Layers mask(Layers: {PrintLayerNames(layerSetup.PedestrianLayers)} ) from scene setup. Change the layer of {targetScript.name} and its colliders to be part of Pedestrian Layer mask.");
            }
        }


        private string PrintLayerNames(LayerMask mask)
        {
            string result = string.Empty;
            for (int i = 0; i < 32; i++)
            {
                if (((1 << i) & mask) != 0)
                {
                    result += LayerMask.LayerToName(i) + " ";
                }
            }
            return result;
        }


        private bool IsLayerInLayerMask(int layer, LayerMask layerMask)
        {
            // Use bitwise AND operation to check if the layer is included in the layer mask
            return (layerMask & (1 << layer)) != 0;
        }


        private void CheckColliders(Pedestrian targetScript, ref bool correct)
        {
            if (!correct)
                return;

            Collider[] allColliders = targetScript.GetComponentsInChildren<Collider>();
            for (int i = 0; i < allColliders.Length; i++)
            {
                if (!allColliders[i].isTrigger)
                {
                    return;
                }
            }

            LogError(ref correct, $"No collider found -> Please assign a collider on {targetScript.name}.");
        }


        private void SetupRigidbody(Pedestrian targetScript, ref bool correct)
        {
            if (!correct)
                return;

            Rigidbody rb = targetScript.GetComponent<Rigidbody>();
            if (rb == null)
            {
                LogError(ref correct, "RigidBody not found on " + targetScript.name);
            }
            else
            {
#if UNITY_6000_0_OR_NEWER
                if (rb.linearDamping == 0)
                {
                    rb.linearDamping = 10f;
                    rb.angularDamping = 10;
                }
#else
                if (rb.drag == 0)
                {
                    rb.drag = 10f;
                    rb.angularDrag = 10;
                }
#endif
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }


        private void SetupAnimator(Pedestrian targetScript, ref bool correct)
        {
            if (!correct)
                return;
            Animator anim = targetScript.GetComponent<Animator>();
            if (anim.avatar == null)
            {
                LogError(ref correct, $"Animator of {targetScript.name} has no avatar");
                return;
            }
            if (anim.runtimeAnimatorController == null)
            {
                LogError(ref correct, $"Animator of {targetScript.name} has no controller");
                return;
            }

            if (!HasAnimatorParameter(anim, Internal.PedestrianSystemConstants.AnimatorAngleID))
            {
                LogError(ref correct, $"Animator of {targetScript.name} does not define the a Float parameter named {Internal.PedestrianSystemConstants.AnimatorAngleID}. This is required.");
                return;
            }

            if (!HasAnimatorParameter(anim, Internal.PedestrianSystemConstants.AnimatorSpeedID))
            {
                LogError(ref correct, $"Animator of {targetScript.name} does not define the a Float parameter named {Internal.PedestrianSystemConstants.AnimatorSpeedID}. This is required.");
                return;
            }

            anim.applyRootMotion = true;
        }


        private bool HasAnimatorParameter(Animator animator, string parameterName)
        {
            // Iterate through all parameters and check if the parameterName matches
            for (int i = 0; i < animator.parameterCount; i++)
            {
                AnimatorControllerParameter parameter = animator.GetParameter(i);
                if (parameter.name == parameterName)
                {
                    if (parameter.type == AnimatorControllerParameterType.Float)
                    {
                        return true;
                    }
                }

            }
            return false;
        }


        private void AssignVisibilityScript(Pedestrian targetScript)
        {
            var visiblityScript = CustomEditorMethods.GetPrivateField<Pedestrian>(_visibilityScriptFieldName);
            if (visiblityScript == null)
            {
                return;
            }

            VisibilityScript[] allComponents = targetScript.gameObject.GetComponentsInChildren<VisibilityScript>();
            if (allComponents.Length > 1)
            {
                for (int i = 1; i < allComponents.Length; i++)
                {
                    DestroyImmediate(allComponents[i]);
                }
            }

            if (allComponents.Length == 1)
            {
                if (allComponents[0].GetComponent<Renderer>() != null)
                {
                    visiblityScript.SetValue(targetScript, allComponents[0]);
                    return;
                }
                else
                {
                    DestroyImmediate(allComponents[0]);
                }
            }

            Renderer renderer = targetScript.GetComponentInChildren<Renderer>();
            visiblityScript.SetValue(targetScript, renderer.gameObject.AddComponent<VisibilityScript>());
        }


        private void Save(Pedestrian targetScript)
        {
            EditorUtility.SetDirty(targetScript);
            AssetDatabase.SaveAssets();
        }


        private void CheckForZero(Pedestrian targetScript, string propertyName, ref bool correct)
        {
            var property = CustomEditorMethods.GetPrivateField<Pedestrian>(propertyName);

            if (property == null)
            {
                LogError(ref correct, propertyName + " does not exist");
            }

            if ((float)property.GetValue(targetScript) <= 0)
            {
                LogError(ref correct, propertyName + " needs to be > 0");
            }
        }


        private void LogError(ref bool correct, string message)
        {
            correct = false;
            Debug.LogError(message + " Auto assign will stop now.");
            Debug.Log("Please fix the above errors and press Assign Pedestrian again or assign missing references and values manually.");
        }
    }
}