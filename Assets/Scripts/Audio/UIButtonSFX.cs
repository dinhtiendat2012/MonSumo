using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSFX : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClick);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(PlayClick);
    }

    private void PlayClick()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlayUIClickSFX();
    }
}
