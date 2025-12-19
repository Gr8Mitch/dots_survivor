using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Survivor.Runtime.Vfx
{
    using UnityEngine;

    /// <summary>
    /// Contains all the settings of all the VFX prefabs that are instantiable. 
    /// </summary>
    [CreateAssetMenu(fileName = "VfxPrefabsSettingsContainer", menuName = "Survivor/Vfx/VfxPrefabsSettingsContainer")]
    public class VfxPrefabsSettingsContainer : ScriptableObject
    {
        public static AsyncOperationHandle<VfxPrefabsSettingsContainer> LoadAsync()
        {
            return Addressables.LoadAssetAsync<VfxPrefabsSettingsContainer>("VfxPrefabsSettingsContainer");
        }
        
        [SerializeField]
        private VfxPrefabSettings[] _vfxPrefabsSettings;

        public VfxPrefabSettings GetPrefabSettings(VfxId id)
        {
            foreach (var prefabsSetting in _vfxPrefabsSettings)
            {
                if (prefabsSetting.ID == id)
                {
                    return prefabsSetting;
                }
            }
            
            return null;
        }
    }
}