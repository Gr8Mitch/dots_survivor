namespace Survivor.Runtime.Vfx
{
    using UnityEngine;
    using Unity.Entities;
    using UnityEngine.VFX;
    
    /// <summary>
    /// The base class to be able to manage a <see cref="VisualEffect"/> on a GameObjectVfx prefab.
    /// </summary>
    public abstract class IVfxSettings : ScriptableObject
    {
        #region Fields
        
        [SerializeField]
        private Gradient _particlesGradient;

        [SerializeField] 
        private string _particlesGradientAttribute = "ParticlesGradient";
        
        [HideInInspector]
        [SerializeField]
        private int _particlesGradientAttributeId;
        
        #endregion Fields

        #region Properties

        public Gradient Gradient => _particlesGradient;
        
        public int ParticlesGradientAttributeId => _particlesGradientAttributeId;

        #endregion Properties

        protected virtual void OnValidate()
        {
            _particlesGradientAttributeId = Shader.PropertyToID(_particlesGradientAttribute);
        }

        public virtual void InitializeVfx(VisualEffect vfx)
        {
            vfx.SetGradient(_particlesGradientAttributeId, _particlesGradient);
        }
    }
}