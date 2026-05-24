using UnityEngine;

public class WeaponRotate : MonoBehaviour
{
    [Header("Current Object")]
    public GameObject currentObject;

    [Header("Default Item")]
    public GameObject defaultItem;

    void Start()
    {
        // Spawn item mặc định nếu chưa có
        if (defaultItem != null && currentObject == null)
        {
            Equip(defaultItem);
        }
    }

    void Update()
    {
        RotateToMouse();

        // E để đổi item
        if (Input.GetKeyDown(KeyCode.E))
        {
            ChangeItem();
        }
    }

    void RotateToMouse()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector2 direction = mousePos - transform.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle+90);
    }

    // Equip weapon/item mới
    public void Equip(GameObject prefab)
    {
        if (currentObject != null)
        {
            Destroy(currentObject);
        }

        currentObject = Instantiate(
            prefab,
            transform.position,
            transform.rotation,
            transform
        );
    }

    // E để đổi item
    void ChangeItem()
    {
        Debug.Log("Change Item");

        // Demo:
        // sau này đổi inventory ở đây
    }
}