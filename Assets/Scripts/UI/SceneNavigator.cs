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
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene("Match", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                Debug.Log("[SceneNavigator] Client clicked confirm, waiting for host to start.");
            }
        }
        else
        {
            SceneManager.LoadScene("Match");
        }
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
