
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
            selectedName.text = ((CharacterType)CharacterSelection.SelectionCharacterId).ToString();
            selectedName.gameObject.SetActive(true);
        }
        if (selectedImage != null )
        {
            selectedImage.sprite = characterSprite;
            selectedImage.gameObject.SetActive(true);
            
        }

    }

}


