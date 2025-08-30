#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Loads debug asset.
    /// </summary>
    public class PedestrianDebugOptions
    {
        //TODO remove absolute path.
        public static PedestrianDebugSettings LoadOrCreateDebugSettings()
        {
            PedestrianDebugSettings debugSettings = (PedestrianDebugSettings)AssetDatabase.LoadAssetAtPath("Assets/Gley/PedestrianSystem/Resources/DebugOptions.asset", typeof(PedestrianDebugSettings));
            if (debugSettings == null)
            {
                PedestrianDebugSettings asset = ScriptableObject.CreateInstance<PedestrianDebugSettings>();
                if (!AssetDatabase.IsValidFolder("Assets/Gley"))
                {
                    AssetDatabase.CreateFolder("Assets/", "Gley");
                    AssetDatabase.Refresh();
                }

                if (!AssetDatabase.IsValidFolder("Assets/Gley/PedestrianSystem"))
                {
                    AssetDatabase.CreateFolder("Assets/Gley", "PedestrianSystem");
                    AssetDatabase.Refresh();
                }

                if (!AssetDatabase.IsValidFolder("Assets/Gley/PedestrianSystem/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets/Gley/PedestrianSystem", "Resources");
                    AssetDatabase.Refresh();
                }

                AssetDatabase.CreateAsset(asset, "Assets/Gley/PedestrianSystem/Resources/DebugOptions.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                debugSettings = (PedestrianDebugSettings)AssetDatabase.LoadAssetAtPath("Assets/Gley/PedestrianSystem/Resources/DebugOptions.asset", typeof(PedestrianDebugSettings));
            }

            return debugSettings;
        }
    }
}
#endif