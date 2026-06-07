using UnityEngine;

public class Cards : MonoBehaviour
{
    public static Cards Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        HideAllCards();
    }

    public void ShowForLevelUp(int level)
    {
        gameObject.SetActive(true);
        Debug.Log("Choose a buff card for Level " + level);
    }

    public void HideAllCards()
    {
        gameObject.SetActive(false);
    }
}
