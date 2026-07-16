using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using MonSumo.Core;
using System.Collections;
using System.Collections.Generic;
using VContainer;

namespace MonSumo.UI
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Player Avatar")]
        [SerializeField] private Image _avatarImage;

        [Header("Hearts Life HUD")]
        [SerializeField] private Image[] _heartImages = new Image[3];
        [SerializeField] private Sprite _fullHeartSprite;
        [SerializeField] private Sprite _emptyHeartSprite;

        [Header("Stamina HUD")]
        [SerializeField] private Slider _staminaSlider;
        [SerializeField] private Image _staminaFillImage;
        [SerializeField] private TMP_Text _staminaText;
        [SerializeField] private Image _dashCooldownOverlay;

        [Header("Combat Cooldowns HUD")]
        [SerializeField] private GameObject _cooldownsPanel;
        [SerializeField] private Image _attackCooldownOverlay;
        [SerializeField] private TMP_Text _attackCooldownText;
        [SerializeField] private Image _dashCooldownNewOverlay;
        [SerializeField] private TMP_Text _dashCooldownNewText;
        [SerializeField] private Image _skillCooldownOverlay;
        [SerializeField] private TMP_Text _skillCooldownText;

        [Header("Alert HUD")]
        [SerializeField] private TMP_Text _alertText;
        [SerializeField] private float _alertDuration = 3f;

        [Header("Zone Alarm HUD")]
        [SerializeField] private TMP_Text _zoneAlarmText;

        [Header("End Game HUD")]
        [SerializeField] private GameObject _endGamePanel;
        [SerializeField] private TMP_Text _endGameTitleText;
        [SerializeField] private UnityEngine.UI.Button _returnToLobbyButton;

        private Player _localPlayer;
        private PlayerMovement _localMovement;
        private MonSumo.World.Zone.ZoneController _zoneController;
        private EventBus _eventBus;
        private float _alertTimer;

        private int _lastHpValue = 3;
        private int _currentAvatarCharacterId = -1;
        private Coroutine[] _heartCoroutines = new Coroutine[3];

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

            // Set stamina bar color to green by default
            if (_staminaFillImage != null)
            {
                _staminaFillImage.color = new Color(0.2f, 0.8f, 0.3f, 1f); // Vibrant Sumo green
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
                        InitializeHUD();
                    }
                }
            }

            // Locate active ZoneController if null
            if (_zoneController == null)
            {
                _zoneController = FindFirstObjectByType<MonSumo.World.Zone.ZoneController>();
            }

            // Update Player HP & Stamina UI
            UpdatePlayerUI();

            // Update Zone Alarm UI
            UpdateZoneAlarmUI();

            // Update Alert Timer
            UpdateAlertUI();
        }

        private void InitializeHUD()
        {
            if (_localPlayer == null) return;

            _currentAvatarCharacterId = _localPlayer.selectedCharacterId.Value;
            UpdateAvatarUI();

            _lastHpValue = _localPlayer.currentHP.Value;
            UpdateHeartsUI(_lastHpValue, false);

            if (_returnToLobbyButton != null)
            {
                _returnToLobbyButton.onClick.RemoveAllListeners();
                _returnToLobbyButton.onClick.AddListener(HandleReturnToLobbyClick);
            }

            if (_endGamePanel != null)
            {
                _endGamePanel.SetActive(false);
            }
        }

        private void UpdateAvatarUI()
        {
            if (_localPlayer == null || _avatarImage == null) return;

            if (_localPlayer.playerData != null && _localPlayer.playerData.lobbyIcon != null)
            {
                _avatarImage.sprite = _localPlayer.playerData.lobbyIcon;
                _avatarImage.color = Color.white;
                _avatarImage.enabled = true;
            }
            else
            {
                _avatarImage.sprite = null;
                _avatarImage.enabled = false;
            }

            // Set parent AvatarFrame background color to transparent as requested
            var parentImage = _avatarImage.transform.parent != null ? _avatarImage.transform.parent.GetComponent<UnityEngine.UI.Image>() : null;
            if (parentImage != null)
            {
                parentImage.color = new Color(0f, 0f, 0f, 0f); // transparent
            }
        }

        private void HandleReturnToLobbyClick()
        {
            if (_localPlayer != null)
            {
                _returnToLobbyButton.interactable = false; // Prevent double click
                _localPlayer.RequestReturnToLobbyServerRpc();
            }
        }

        private void UpdatePlayerUI()
        {
            if (_localPlayer == null) return;

            // Dynamically sync and update player avatar if selection has changed or loaded
            int charId = _localPlayer.selectedCharacterId.Value;
            if (charId != _currentAvatarCharacterId)
            {
                UpdateAvatarUI();
                _currentAvatarCharacterId = charId;
            }

            // HP Change Detection
            int currentHp = _localPlayer.currentHP.Value;
            if (currentHp != _lastHpValue)
            {
                UpdateHeartsUI(currentHp, true);
                _lastHpValue = currentHp;
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
                    _staminaText.text = $"{Mathf.RoundToInt(stamina)}/{Mathf.RoundToInt(maxStamina)}";
                }

                // Dash Cooldown Overlay (old HUD component)
                if (_dashCooldownOverlay != null)
                {
                    float dashMax = _localMovement.DashCooldown > 0.01f ? _localMovement.DashCooldown : 3f;
                    float ratio = _localMovement.DashCooldownTimer / dashMax;
                    _dashCooldownOverlay.fillAmount = Mathf.Clamp01(ratio);
                }

                // Dynamic Combat Cooldowns HUD
                UpdateCooldownHUD(_attackCooldownOverlay, _attackCooldownText, _localMovement.AttackCooldownTimer, _localMovement.AttackCooldown);
                UpdateCooldownHUD(_dashCooldownNewOverlay, _dashCooldownNewText, _localMovement.DashCooldownTimer, _localMovement.DashCooldown);
                UpdateCooldownHUD(_skillCooldownOverlay, _skillCooldownText, _localMovement.SkillCooldownTimer, _localMovement.SkillCooldown);
            }

            // End Game Popup Monitoring
            if (_endGamePanel != null)
            {
                if (_localPlayer.isDead.Value && !_endGamePanel.activeSelf)
                {
                    _endGamePanel.SetActive(true);
                    if (_endGameTitleText != null)
                    {
                        _endGameTitleText.text = "DEFEATED";
                        _endGameTitleText.color = new Color(0.85f, 0.15f, 0.15f, 1f); // Dark red
                    }
                    if (_returnToLobbyButton != null)
                    {
                        _returnToLobbyButton.interactable = true;
                    }
                }
                else if (_localPlayer.isWinner.Value && !_endGamePanel.activeSelf)
                {
                    _endGamePanel.SetActive(true);
                    if (_endGameTitleText != null)
                    {
                        _endGameTitleText.text = "VICTORY!";
                        _endGameTitleText.color = new Color(0.95f, 0.75f, 0.3f, 1f); // Gold
                    }
                    if (_returnToLobbyButton != null)
                    {
                        _returnToLobbyButton.interactable = true;
                    }
                }
            }
        }

        private void UpdateCooldownHUD(Image overlay, TMP_Text textComp, float currentTimer, float totalDuration)
        {
            if (overlay != null)
            {
                float ratio = totalDuration > 0.01f ? currentTimer / totalDuration : 0f;
                overlay.fillAmount = Mathf.Clamp01(ratio);
            }

            if (textComp != null)
            {
                if (currentTimer > 0.05f)
                {
                    textComp.text = Mathf.CeilToInt(currentTimer).ToString();
                }
                else
                {
                    textComp.text = "";
                }
            }
        }

        private void UpdateHeartsUI(int currentHp, bool animate)
        {
            for (int i = 0; i < _heartImages.Length; i++)
            {
                Image heartImg = _heartImages[i];
                if (heartImg == null) continue;

                bool shouldBeFull = i < currentHp;

                if (shouldBeFull)
                {
                    if (heartImg.sprite != _fullHeartSprite)
                    {
                        heartImg.sprite = _fullHeartSprite;
                        heartImg.rectTransform.localScale = Vector3.one;
                        heartImg.color = Color.white;
                    }
                }
                else
                {
                    // If it just became empty, play the bounce-shrink animation
                    if (heartImg.sprite == _fullHeartSprite && animate)
                    {
                        if (_heartCoroutines[i] != null)
                        {
                            StopCoroutine(_heartCoroutines[i]);
                        }
                        _heartCoroutines[i] = StartCoroutine(AnimateHeartLoss(heartImg));
                    }
                    else if (!animate)
                    {
                        heartImg.sprite = _emptyHeartSprite;
                        heartImg.rectTransform.localScale = Vector3.one;
                        heartImg.color = new Color(1f, 1f, 1f, 0.4f); // Semi-transparent for empty heart look
                    }
                }
            }
        }

        private IEnumerator AnimateHeartLoss(Image heartImg)
        {
            float duration = 0.5f;
            float elapsed = 0f;

            Vector3 startScale = Vector3.one;
            // Scale up first (pop), then shrink down
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Simple bouncy curve: pop up to 1.4x, then shrink to 0
                float scale;
                if (t < 0.3f)
                {
                    float tPop = t / 0.3f;
                    scale = Mathf.Lerp(1f, 1.4f, tPop);
                }
                else
                {
                    float tShrink = (t - 0.3f) / 0.7f;
                    scale = Mathf.Lerp(1.4f, 0f, tShrink);
                }

                heartImg.rectTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            // Once shrunk, swap sprite to empty, scale back to 1.0, and fade in slightly
            heartImg.sprite = _emptyHeartSprite;
            
            elapsed = 0f;
            float fadeInDuration = 0.2f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                heartImg.rectTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                heartImg.color = Color.Lerp(new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0.4f), t);
                yield return null;
            }

            heartImg.rectTransform.localScale = Vector3.one;
            heartImg.color = new Color(1f, 1f, 1f, 0.4f);
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

        private void UpdateZoneAlarmUI()
        {
            if (_zoneAlarmText == null || _zoneController == null) return;

            float countdown = _zoneController.shrinkCountdown.Value;

            if (countdown > 0.01f)
            {
                if (countdown <= 30f)
                {
                    _zoneAlarmText.gameObject.SetActive(true);
                    int seconds = Mathf.CeilToInt(countdown);
                    
                    if (seconds <= 10)
                    {
                        // Flashing red
                        float flashValue = Mathf.Abs(Mathf.Sin(Time.time * 8f));
                        _zoneAlarmText.color = Color.Lerp(Color.red, new Color(1f, 0.5f, 0.5f), flashValue);
                        _zoneAlarmText.text = $"WARNING! RING SHRINKING IN: {seconds}s";
                        _zoneAlarmText.transform.localScale = Vector3.one * (1f + 0.05f * flashValue); // slight pulsing
                    }
                    else
                    {
                        // Flat gold/orange warning
                        _zoneAlarmText.color = new Color(0.95f, 0.6f, 0.15f, 1f); // Orange-gold
                        _zoneAlarmText.text = $"RING PREPARATION: {seconds}s";
                        _zoneAlarmText.transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    _zoneAlarmText.gameObject.SetActive(false);
                }
            }
            else
            {
                // Active shrinking & moving
                _zoneAlarmText.gameObject.SetActive(true);
                _zoneAlarmText.color = Color.red;
                _zoneAlarmText.text = "SAFE ZONE SHRINKING & MOVING!";
                
                // Pulsing warning
                float pulse = Mathf.Abs(Mathf.Sin(Time.time * 4f));
                _zoneAlarmText.transform.localScale = Vector3.one * (1f + 0.03f * pulse);
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
