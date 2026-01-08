namespace Survivor.Runtime.Vfx
{
    using UnityEngine;
    using UnityEngine.VFX;

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
        
        public float BaseRadius => _baseRadius;
        public Vector3 BaseNormal => _baseNormal;
        public int RadiusAttributeId => _radiusAttributeId;
  
        protected override void OnValidate()
        {
            base.OnValidate();
           
            _radiusAttributeId = Shader.PropertyToID(_radiusAttribute);
        }

        public override void InitializeVfx(VisualEffect vfx)
        {
            base.InitializeVfx(vfx);
            
            vfx.SetFloat(_radiusAttributeId, _baseRadius);
        }
    }
}