using Unity.Netcode;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using System.Linq;

public class DisconnectHandler : MonoBehaviour
{
    [SerializeField] PlayerListManager roster; // your NetworkList-backed roster UI

    void OnEnable()
    {
        var nm = NetworkManager.Singleton;
        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;
    }

    void OnDisable()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            nm.OnClientConnectedCallback -= OnClientConnected;
            nm.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected");
    }

    async void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected");

        if (!NetworkManager.Singleton.IsServer) return;

        if (!string.IsNullOrEmpty(GameConstants.Instance._lobbyId))
        {
            try
            {
                var players = await LobbyManager.GetPlayersList(GameConstants.Instance._lobbyId);

                var index = (int)clientId;
                var player = players[index];
                var playerId = player.Id;
                Debug.Log($"Removing player {playerId} from lobby {GameConstants.Instance._lobbyId}");
                if (!string.IsNullOrEmpty(playerId))
                    await LobbyService.Instance.RemovePlayerAsync(GameConstants.Instance._lobbyId, playerId);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Lobby remove failed (will timeout anyway): {e.Message}");
            }
        }
    }

}
