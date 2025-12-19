using Unity.Entities;
using UnityEngine.VFX;

namespace Survivor.Runtime.Vfx
{
    using UnityEngine;

    /// <summary>
    /// A vfx settings used for vfxs that appear as a circle.
    /// </summary>
    [CreateAssetMenu(fileName = "CircleVfxSettings", menuName = "Survivor/Vfx/VfxSettings/CircleVfxSettings")]
    public class CircleVfxSettings : IVfxSettings
    {
        [SerializeField]
        private float _baseRadius = 10f;
        
        [SerializeField] 
        private string _radiusAttribute = "Radius";
        
        [HideInInspector]
        [SerializeField] 
        private int _radiusAttributeId;
        
        [SerializeField]
        private Vector3 _baseNormal = Vector3.up;
        
        [SerializeField]
        private string _normalAttribute = "Normal";
        
        [HideInInspector]
        [SerializeField] 
        private int _normalAttributeId;
        
        public float BaseRadius => _baseRadius;
        public Vector3 BaseNormal => _baseNormal;
        public int RadiusAttributeId => _radiusAttributeId;
        public int NormalAttributeId => _normalAttributeId;
        
        protected override void OnValidate()
        {
            base.OnValidate();
           
            _radiusAttributeId = Shader.PropertyToID(_radiusAttribute);
            _normalAttributeId = Shader.PropertyToID(_normalAttribute);
        }

        public override void InitializeVfx(VisualEffect vfx)
        {
            base.InitializeVfx(vfx);
            
            vfx.SetFloat(_radiusAttributeId, _baseRadius);
            vfx.SetVector3(_normalAttributeId, _baseNormal);
        }
    }
}