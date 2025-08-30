using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class LayerSetupWindow : SetupWindowBase
    {
        private LayerSetup _layerSetup;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            _layerSetup = FileCreator.LoadOrCreateLayers<LayerSetup>(PedestrianSystemConstants.LayerPath);
            return base.Initialize(windowProperties, window);
        }


        protected override void TopPart()
        {
            _layerSetup.GroundLayers = LayerMaskField(new GUIContent("Ground Layers", "Pedestrians will walk only on these layers"), _layerSetup.GroundLayers);
            _layerSetup.PedestrianLayers = LayerMaskField(new GUIContent("Pedestrian Layers", "All pedestrians should be on this layer"), _layerSetup.PedestrianLayers);
            _layerSetup.BuildingsLayers = LayerMaskField(new GUIContent("Buildings Layers", "Vehicles will try to avoid objects on these layers"), _layerSetup.BuildingsLayers);
            _layerSetup.ObstaclesLayers = LayerMaskField(new GUIContent("Obstacle Layers", "Vehicles will stop when objects on these layers are seen"), _layerSetup.ObstaclesLayers);
            _layerSetup.PlayerLayers = LayerMaskField(new GUIContent("Player Layers", "Vehicles will stop when objects on these layers are seen"), _layerSetup.PlayerLayers);

            EditorGUILayout.Space();
            if (GUILayout.Button("Open Tags and Layers Settings"))
            {
                SettingsService.OpenProjectSettings("Project/Tags and Layers");
            }

            base.TopPart();
        }


        private LayerMask LayerMaskField(GUIContent label, LayerMask layerMask)
        {
            LayerMask tempMask = EditorGUILayout.MaskField(label, InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask), InternalEditorUtility.layers);
            layerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
            return layerMask;
        }


        public override void DestroyWindow()
        {
            _layerSetup.Edited = true;
            EditorUtility.SetDirty(_layerSetup);
            AssetDatabase.SaveAssets();
            PedestrianSettingsWindow.UpdateLayers();
            base.DestroyWindow();
        }
    }
}