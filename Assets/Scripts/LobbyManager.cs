using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;
    private const int MAX_PLAYERS_PER_ROOM = 20;
    private const string CONNECTION_TYPE = "udp";
    private bool isHost = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async Task CreateLobby(string lobbyName, string lobbyPassword = "")
    {
        try
        {
            var _lobbyName = string.IsNullOrEmpty(lobbyName) ? "Room" + Random.Range(1000, 9999) : lobbyName;
            bool isPrivate = !string.IsNullOrEmpty(lobbyPassword);
            var options = new CreateLobbyOptions
            {
                Password = isPrivate ? lobbyPassword : null,
                IsPrivate = isPrivate,
                Data = new Dictionary<string, DataObject>
                {
                    { "roomName", new DataObject(DataObject.VisibilityOptions.Public, _lobbyName) },
                },
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "username", new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Member,
                            value: GameConstants.Instance._userName) }
                    }
                }
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(_lobbyName, MAX_PLAYERS_PER_ROOM, options);
            Debug.Log("Created room " + lobby.Name);

            isHost = true;
            GameConstants.Instance._lobbyId = lobby.Id;
            StartHeartbeatLoop();

            Allocation alloc = await CreateRelayAllocation();
            StartConnection(alloc);
            await UpdateLobbyWithRelayCode(lobby, alloc);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }

    public async void JoinLobby(string lobbyId)
    {
        try
        {
            var options = new JoinLobbyByIdOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "username", new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Member,
                            value: GameConstants.Instance._userName) }
                    }
                }
            };
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);


            var alloc = await CreateRelayJoinAllocation(lobby);
            JoinConnection(alloc);

            GameConstants.Instance._lobbyId = lobby.Id;
            var joinedLobby = await LobbyService.Instance.GetLobbyAsync(lobby.Id);
            Debug.Log("Joined Lobby " + lobby.Name);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }
    public async Task<List<Lobby>> FetchLobbies()
    {
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();

            Debug.Log("Fetched " + response.Results.Count + " lobbies");
            return response.Results;
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
            return new List<Lobby>();
        }
    }

    public async void JoinRandomRoom()
    {
        try
        {
            var lobbies = await FetchLobbies();
            if (lobbies.Count == 0)
                return;

            lobbies = lobbies.Where(x => x.Players.Count < x.MaxPlayers).ToList();
            int randomIndex = Random.Range(0, lobbies.Count);

            JoinLobby(lobbies[randomIndex].Id);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }

    static async Task<Allocation> CreateRelayAllocation()
    {
        Allocation alloc = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS_PER_ROOM - 1);
        return alloc;
    }
    
    static async Task<JoinAllocation> CreateRelayJoinAllocation(Lobby lobby)
    {
        var code = lobby.Data["relayCode"].Value;
        var joinAlloc = await RelayService.Instance.JoinAllocationAsync(code);
        return joinAlloc;
    }
    
    static async Task UpdateLobbyWithRelayCode(Lobby lobby, Allocation alloc)
    {
        var code = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
        await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject> {
            { "relayCode", new DataObject(DataObject.VisibilityOptions.Member, code) }
        }
        });
    }

    static void StartConnection(Allocation alloc)
    {
        RelayServerData serverData = alloc.ToRelayServerData(CONNECTION_TYPE);
        ConfigureRelay(serverData);
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("Room", LoadSceneMode.Single);
    }

    static void JoinConnection(JoinAllocation joinAllocation)
    {
        RelayServerData serverData = joinAllocation.ToRelayServerData(CONNECTION_TYPE);
        ConfigureRelay(serverData);
        NetworkManager.Singleton.StartClient();
    }

    static void ConfigureRelay(RelayServerData data)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(data);
    }

    public static async Task<List<Player>> GetPlayersList(string lobbyId)
    {
        var lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
        return lobby.Players;
    }

    private async void StartHeartbeatLoop()
    {
        while (isHost && enabled)
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(GameConstants.Instance._lobbyId);
            await Task.Delay(15000);
        }
    }

    private 
        async void OnApplicationQuit()
    {
        //if (!isHost) await LeaveLobby();
        //else await DeleteLobby();
    }

    private async void OnDestroy()
    {
        //if (!isHost) await LeaveLobby();
        //else await DeleteLobby();
    }

    public async Task RemovePlayer(string playerId)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(GameConstants.Instance._lobbyId,playerId);
            Debug.Log("Left lobby.");
            
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }

    private async Task DeleteLobby()
    {
        try
        {
            if (!string.IsNullOrEmpty(GameConstants.Instance._lobbyId))
            {
                await LobbyService.Instance.DeleteLobbyAsync(GameConstants.Instance._lobbyId);
                Debug.Log("Deleted lobby on quit.");
            }
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }
}
