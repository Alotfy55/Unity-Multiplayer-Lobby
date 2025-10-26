using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePauseMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu(!pauseMenuUI.activeSelf);
        }
    }

    private void TogglePauseMenu(bool pause)
    {
        PlayerMovement.Instance.SetGamePaused(pause);
        pauseMenuUI.SetActive(pause);
        Debug.Log("Toggling Pause Menu");
    }

    public void ResumeGame()
    {
        TogglePauseMenu(false);
        Debug.Log("Resuming Game");
    }

    public async void QuitGame()
    {
        await VivoxManager.Instance.Disconnect();
        await LobbyManager.Instance.RemovePlayer();
        SceneManager.LoadScene("Lobby");
    }
}
