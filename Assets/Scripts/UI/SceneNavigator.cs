using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    public void GoToCharacterSelect()
    {
        Debug.Log("Going to Character Select Scene");
        SceneManager.LoadScene("CharacterSelectScene");
    }


    public void GoToBattle()
    {
        Debug.Log("Going to Battle Scene");
        SceneManager.LoadScene("Match");
    }

    public void GoToMainMenu()
    {
        Debug.Log("Going to Main Menu Scene");
        SceneManager.LoadScene("MainMenuScene");
    }

    public void QuitGame()
    {
        Application.Quit();

        Debug.Log("Quit Game");
    }
}
