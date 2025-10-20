using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerListManager : NetworkBehaviour
{

    [SerializeField]
    Transform playerListContainer;
    [SerializeField]
    Transform playerItemPrefab;

    public NetworkList<FixedString64Bytes>
         playersList = new NetworkList<FixedString64Bytes>();


    public override void OnNetworkSpawn()
    { 
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            _ = RefreshLoop();
        }

    }

    private async Task RefreshLoop()
    {
        while (IsServer && IsSpawned && NetworkManager && NetworkManager.IsListening)
        {
            var players = await LobbyManager.GetPlayersList(GameConstants.Instance._lobbyId);
            var playerNames = players.Select(p => p.Data["username"].Value).ToList();
            UpdatePlayerList(playerNames);
            await Task.Delay(2000);
        }
    }

    public void UpdatePlayerList(List<string> newPlayerNames)
    {
        if (!IsServer) return;

        playersList.Clear();
        foreach (var playerName in newPlayerNames)
        {
            playersList.Add(new FixedString64Bytes(playerName));
        }
        Debug.Log("Updated player list with " + playersList.Count + " players.");
    }

}
