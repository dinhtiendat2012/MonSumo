using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using MonSumo.Core;
using VContainer;

namespace MonSumo.UI
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Player HUD")]
        [SerializeField] private Slider _hpSlider; // or separate heart icons
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private Slider _staminaSlider;
        [SerializeField] private TMP_Text _staminaText;
        [SerializeField] private Image _dashCooldownOverlay;

        [Header("Alert HUD")]
        [SerializeField] private TMP_Text _alertText;
        [SerializeField] private float _alertDuration = 3f;

        private Player _localPlayer;
        private PlayerMovement _localMovement;
        private EventBus _eventBus;
        private float _alertTimer;

        private void Start()
        {
            // Resolve EventBus from VContainer
            var scope = VContainer.Unity.LifetimeScope.Find<MonSumo.Networking.Scopes.GameLifetimeScope>();
            _eventBus = scope?.Container.Resolve<EventBus>();

            if (_eventBus != null)
            {
                _eventBus.OnZoneShrinkStarted += HandleZoneShrink;
                _eventBus.OnZoneMoveStarted += HandleZoneMove;
            }

            if (_alertText != null)
            {
                _alertText.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_eventBus != null)
            {
                _eventBus.OnZoneShrinkStarted -= HandleZoneShrink;
                _eventBus.OnZoneMoveStarted -= HandleZoneMove;
            }
        }

        private void Update()
        {
            // Try to find the local player if not already assigned
            if (_localPlayer == null && NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
            {
                var localObj = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (localObj != null)
                {
                    _localPlayer = localObj.GetComponent<Player>();
                    if (_localPlayer != null)
                    {
                        _localMovement = _localPlayer.GetComponent<PlayerMovement>();
                    }
                }
            }

            // Update Player HP & Stamina UI
            UpdatePlayerUI();

            // Update Alert Timer
            UpdateAlertUI();
        }

        private void UpdatePlayerUI()
        {
            if (_localPlayer == null) return;

            // HP
            int currentHp = _localPlayer.currentHP.Value;
            if (_hpSlider != null)
            {
                _hpSlider.maxValue = 3;
                _hpSlider.value = currentHp;
            }
            if (_hpText != null)
            {
                _hpText.text = $"HP: {currentHp}/3";
            }

            // Stamina
            if (_localMovement != null)
            {
                float stamina = _localMovement.CurrentStamina;
                float maxStamina = _localMovement.MaxStamina;

                if (_staminaSlider != null)
                {
                    _staminaSlider.maxValue = maxStamina;
                    _staminaSlider.value = stamina;
                }
                if (_staminaText != null)
                {
                    _staminaText.text = $"STAMINA: {Mathf.RoundToInt(stamina)}/{Mathf.RoundToInt(maxStamina)}";
                }

                // Dash Cooldown Overlay (fillAmount = remaining time / 3s)
                if (_dashCooldownOverlay != null)
                {
                    float ratio = _localMovement.DashCooldownTimer / 3f;
                    _dashCooldownOverlay.fillAmount = Mathf.Clamp01(ratio);
                }
            }
        }

        private void UpdateAlertUI()
        {
            if (_alertText != null && _alertText.gameObject.activeSelf)
            {
                _alertTimer -= Time.deltaTime;
                if (_alertTimer <= 0f)
                {
                    _alertText.gameObject.SetActive(false);
                }
            }
        }

        private void HandleZoneShrink(float targetRadius)
        {
            ShowAlert("Cảnh báo: Vòng bo đang thu nhỏ lại!");
        }

        private void HandleZoneMove(Vector2 targetCenter)
        {
            ShowAlert("Cảnh báo: Vòng bo đang dịch chuyển!");
        }

        private void ShowAlert(string text)
        {
            if (_alertText != null)
            {
                _alertText.text = text;
                _alertText.gameObject.SetActive(true);
                _alertTimer = _alertDuration;
            }
        }
    }
}
