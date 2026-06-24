
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterUISelection : MonoBehaviour
{
    [SerializeField] private TMP_Text selectedName;
    [SerializeField] private Image selectedImage;

    public void UpdateSelectedUI(Sprite characterSprite)
    {
        if (selectedName !=null)
        {   
            selectedName.gameObject.SetActive(true);
            selectedName.text = ((CharacterType)CharacterSelection.SelectionCharacterId).ToString();
        }
        if (selectedImage != null )
        {
            selectedImage.gameObject.SetActive(true);
            selectedImage.sprite = characterSprite;
        }

    }

}


