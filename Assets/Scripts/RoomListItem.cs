using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class RoomListItem : MonoBehaviour
{
    [SerializeField]
    TMP_Text _roomName;
    [SerializeField]
    TMP_Text _playerCount;

    private string roomId;
    public void SetValues(Lobby lobby)
    {
        _roomName.text = lobby.Name;
        _playerCount.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
        roomId = lobby.Id;
    }

    public async void JoinLobby()
    {
        LobbyUiManager.Instance.EnableLoadingScreen("Joining Room...");
        await LobbyManager.Instance.JoinLobby(roomId);
    }
    
}
