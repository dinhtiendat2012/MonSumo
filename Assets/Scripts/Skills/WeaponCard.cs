using UnityEngine;

public class Card : MonoBehaviour
{
    public GameObject weaponPrefab;

    private WeaponRotate weaponPivot;
    private Cards cards;

    void Start()
    {
        weaponPivot = FindFirstObjectByType<WeaponRotate>();

        cards = GetComponentInParent<Cards>();
    }

    private void OnMouseDown()
    {
        // Equip weapon
        weaponPivot.Equip(weaponPrefab);

        // Ẩn toàn bộ card
        cards.HideAllCards();
    }
}