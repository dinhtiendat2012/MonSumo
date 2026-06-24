using UnityEngine;
using UnityEngine.UI;

public class SelectionButton : MonoBehaviour
{
    [SerializeField] private CharacterType characterSelection;
    [SerializeField] private GameObject UIManager;
    [SerializeField] private Sprite spriteRenderer;

    private void Awake()
    {
        if (UIManager == null)
        {
            UIManager = GameObject.Find("UIManager");
        }
    }
    public void Select()
    {
        if (GetComponentInChildren<Lock>() != null)
        {
            Debug.Log("Character is locked!");
            return;
        }
        CharacterSelection.SelectionCharacterId = (int)characterSelection;
        UIManager.GetComponent<CharacterUISelection>().UpdateSelectedUI(spriteRenderer);

        
        Debug.Log($"Selected character: {characterSelection}");
    }
}