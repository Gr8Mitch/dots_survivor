namespace Survivor.Runtime.UI
{
    using UnityEngine;
    using UnityEngine.UIElements;
    using Survivor.Runtime.Character;
    using Survivor.Runtime.Lifecycle;
    using Unity.Entities;

    /// <summary>
    /// Handles the HUD panel.
    /// </summary>
    public class HudPanel : MonoBehaviour
    {
        #region Inner Structs

        [System.Serializable]
        private class ProgressBarSettings
        {
            public Color HealthyColor = Color.green;
            public Color DeadColor = Color.red;
        }
        
        #endregion Inner Structs
        
        #region Fields
        
        [SerializeField]
        private UIDocument _uiDocument;
        
        [SerializeField]
        private ProgressBarSettings _progressBarSettings;
        
        private ProgressBar _healthBar;
        private VisualElement _healthBarProgress;
        private bool _wasHealthBarInitialized = false;
        private EntityQuery _playerQuery;
        
        #endregion Fields
        
        private void Awake()
        {
            _healthBar = _uiDocument.rootVisualElement.Q<ProgressBar>("PlayerHealthBar");
            _healthBarProgress = _healthBar.Q<VisualElement>(className: "unity-progress-bar__progress");
            
            World world = World.DefaultGameObjectInjectionWorld;
            _playerQuery = world.EntityManager
                .CreateEntityQuery(ComponentType.ReadOnly<AvatarCharacterComponent>(), 
                ComponentType.ReadOnly<HealthComponent>());
        }

        private void Update()
        {
            UpdatePlayerHealthBar();
        }

        private void UpdatePlayerHealthBar()
        {
            // TODO_IMPROVEMENT: use a MaxHealth component (just for the player entity ?) to be cleaner.
            if (_playerQuery.IsEmpty)
            {
                return;
            }
            
            _playerQuery.CompleteDependency();
            var playerHitPoints = _playerQuery.GetSingleton<HealthComponent>().HitPoints;
            if (!_wasHealthBarInitialized)
            {
                _healthBar.highValue = playerHitPoints;
                _wasHealthBarInitialized = true;
                _healthBarProgress.style.backgroundColor = _progressBarSettings.HealthyColor;
            }
            
            if (playerHitPoints != Mathf.RoundToInt(_healthBar.value))
            {
                // The value has changed, we must update the UI.
                _healthBar.value = playerHitPoints;
                Color barColor = Color.Lerp(_progressBarSettings.DeadColor, _progressBarSettings.HealthyColor, playerHitPoints / _healthBar.highValue);
                _healthBarProgress.style.backgroundColor = barColor;
            }
        }
    }
}