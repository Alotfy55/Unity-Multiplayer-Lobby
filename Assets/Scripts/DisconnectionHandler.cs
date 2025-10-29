using Unity.Netcode;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using System.Linq;
using Unity.Services.Vivox;
using UnityEngine.SceneManagement;
using System;

public class DisconnectHandler : MonoBehaviour
{

    void OnEnable()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    void OnDisable()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
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
                if (!string.IsNullOrEmpty(playerId))
                    await LobbyService.Instance.RemovePlayerAsync(GameConstants.Instance._lobbyId, playerId);
                Cursor.lockState = CursorLockMode.None;
                SceneManager.LoadScene("Lobby");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Lobby remove failed: {e.Message}");
            }
        }
    }

}
