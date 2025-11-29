namespace Survivor.Runtime.Character
{
    using UnityEngine;
    using UnityEngine.Serialization;
    using Unity.Entities;

    /// <summary>
    /// A component to identify the enemy characters.
    /// </summary>
    public struct EnemyCharacterComponent : IComponentData
    {
        public int EnemyTypeId;
    }
    
    class EnemyCharacterComponentAuthoring : MonoBehaviour
    {
        [FormerlySerializedAs("EnemyId")] public int EnemyTypeId;
        
        class EnemyCharacterComponentAuthoringBaker : Baker<EnemyCharacterComponentAuthoring>
        {
            public override void Bake(EnemyCharacterComponentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemyCharacterComponent()
                {
                    EnemyTypeId = authoring.EnemyTypeId
                });
            }
        }
    }
}