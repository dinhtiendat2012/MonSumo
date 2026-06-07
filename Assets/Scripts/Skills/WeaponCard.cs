using UnityEngine;

public class Card : MonoBehaviour
{
    public enum BuffType
    {
        Stamina,
        Speed,
        PushForce
    }

    [SerializeField] private BuffType buffType;

    private Cards cards;

    private void Start()
    {
        cards = GetComponentInParent<Cards>();
    }

    private void OnMouseDown()
    {
        switch (buffType)
        {
            case BuffType.Stamina:
                Debug.Log("Stamina Upgraded");
                break;
            case BuffType.Speed:
                Debug.Log("Speed Upgraded");
                break;
            case BuffType.PushForce:
                Debug.Log("Push Force Upgraded");
                break;
        }

        if (cards != null)
        {
            cards.HideAllCards();
        }
    }
}
