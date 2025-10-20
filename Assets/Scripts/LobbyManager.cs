using System;
using System.Collections.Generic;
using System.Linq;
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
                    Profile = new PlayerProfile(GameConstants.Instance._userName)
                }
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(_lobbyName, MAX_PLAYERS_PER_ROOM, options);
            Debug.Log("Created room " + lobby.Name);

            Allocation alloc = await CreateRelayAllocation();
            StartConnection(alloc);
            await UpdateLobbyWithRelayCode(lobby, alloc);

            UpdatePlayersList(lobby);
            Debug.Log("Joined room");
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
                    Profile = new PlayerProfile(GameConstants.Instance._userName)
                }
            };
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);


            var alloc = await CreateRelayJoinAllocation(lobby);
            JoinConnection(alloc);

            var joinedLobby = await LobbyService.Instance.GetLobbyAsync(lobby.Id);
            UpdatePlayersList(joinedLobby);
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

    static void UpdatePlayersList(Lobby lobby)
    {
        // TODO: Check returned lobby players profiles
        var playerNames = lobby.Players.Select(p => p.Profile.Name).ToList();
        PlayerListManager.Instance.UpdatePlayerList(playerNames);
    }
}
