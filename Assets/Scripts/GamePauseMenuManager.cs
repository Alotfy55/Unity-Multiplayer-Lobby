using StarterAssets;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePauseMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;
    ThirdPersonController controller;
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu(!pauseMenuUI.activeSelf);
        }
    }

    private void TogglePauseMenu(bool pause)
    {
        if (controller == null)
        {
            GetLocalPlayerController();
        }
        controller.SetGamePaused(pause);
        pauseMenuUI.SetActive(pause);
        Debug.Log("Toggling Pause Menu");
    }

    public void ResumeGame()
    {
        if (controller == null)
        {
            GetLocalPlayerController();
        }

        controller.SetGamePaused(false);
        TogglePauseMenu(false);
        Debug.Log("Resuming Game");
    }

    public async void QuitGame()
    {
        await VivoxManager.Instance.Disconnect();
        await LobbyManager.Instance.RemovePlayer();
        SceneManager.LoadScene("Lobby");
    }

    private void GetLocalPlayerController()
    {
        controller = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<ThirdPersonController>();
    }
}
