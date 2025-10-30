using System;
using System.Collections.Generic;
using System.Linq;
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
        await IntiallizeServices();
    }

    private async Task IntiallizeServices()
    {
        LobbyUiManager.Instance.EnableLoadingScreen("Connecting to server...");
        await UnityServices.InitializeAsync();

        var newPlayerId = Guid.NewGuid().ToString("N").Substring(0, 8);

        while (!AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SwitchProfile(newPlayerId);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        await VivoxManager.Instance.Initiallize();

        LobbyUiManager.Instance.DisableLoadingScreen();
        Debug.Log("Unity Services initialized. Player ID: " + AuthenticationService.Instance.PlayerId);
    }

    public async Task CreateLobby(string lobbyName, string lobbyPassword = "")
    {
        try
        {
            var _lobbyName = string.IsNullOrEmpty(lobbyName) ? "Room" + Random.Range(1000, 9999) : lobbyName;
            var options = new CreateLobbyOptions
            {
                Password = string.IsNullOrEmpty(lobbyPassword) ? null : lobbyPassword,
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

            Allocation alloc = await CreateRelayAllocation();
            StartConnection(alloc);

            await VivoxManager.Instance.ConnectToChannel(lobby.Id);

            await UpdateLobbyWithRelayCode(lobby, alloc);
            StartHeartbeatLoop();
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
            ToastNotification.Show("Failed to create room");
        }
        finally
        {
            if (LobbyUiManager.Instance != null)
                LobbyUiManager.Instance.DisableLoadingScreen();
        }
    }

    public async Task JoinLobby(string lobbyId, string passwword = "")
    {
        try
        {
            var options = new JoinLobbyByIdOptions
            {
                Password = string.IsNullOrEmpty(passwword) ? null : passwword,
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "username", new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Member,
                            value: GameConstants.Instance._userName)
                        }
                    }
                }
            };
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);


            var alloc = await CreateRelayJoinAllocation(lobby);
            JoinConnection(alloc);
            await VivoxManager.Instance.ConnectToChannel(lobby.Id);
            GameConstants.Instance._lobbyId = lobby.Id;
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
            ToastNotification.Show("Failed to join room");
        }
    }
    public async Task<List<Lobby>> FetchLobbies()
    {
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();

            Debug.Log("Fetched " + response.Results.Count + " lobbies");
            return response.Results.Where(x => x.Created != x.LastUpdated).ToList();
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
            {
                ToastNotification.Show("No available rooms to join");
                LobbyUiManager.Instance.DisableLoadingScreen();
                return;
            }

            lobbies = lobbies.Where(x => x.Players.Count < x.MaxPlayers).ToList();
            int randomIndex = Random.Range(0, lobbies.Count);

            await JoinLobby(lobbies[randomIndex].Id);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
        finally
        {
            if (LobbyUiManager.Instance != null)
                LobbyUiManager.Instance.DisableLoadingScreen();
        }
    }

    static async Task<Allocation> CreateRelayAllocation()
    {
        try
        {
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS_PER_ROOM - 1);
            return alloc;
        }
        catch (RelayServiceException ex)
        {
            Debug.Log(ex);
            throw;
        }
    }

    static async Task<JoinAllocation> CreateRelayJoinAllocation(Lobby lobby)
    {
        try
        {
            var code = lobby.Data["relayCode"].Value;
            var joinAlloc = await RelayService.Instance.JoinAllocationAsync(code);
            return joinAlloc;
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
            await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
            Debug.LogError("Failed to join relay. You have been removed from the lobby.");
            ToastNotification.Show("Failed to join room");
            LobbyUiManager.Instance.DisableLoadingScreen();
            throw;
        }
    }
    static async Task UpdateLobbyWithRelayCode(Lobby lobby, Allocation alloc)
    {
        var code = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
        await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject> {
                {
                    "relayCode", new DataObject(DataObject.VisibilityOptions.Member, code)
                }
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
        try
        {
            var lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
            return lobby.Players;
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
            return new List<Player>();
        }
    }

    private async void StartHeartbeatLoop()
    {
        while (this != null && isHost && enabled && LobbyService.Instance != null)
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(GameConstants.Instance._lobbyId);
            await Task.Delay(15000);
        }
    }


    public async Task RemovePlayer()
    {
        try
        {
            await HostMigration();
            await LobbyService.Instance.RemovePlayerAsync(GameConstants.Instance._lobbyId, AuthenticationService.Instance.PlayerId);
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Left lobby.");
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }

    public async Task HostMigration()
    {
        try
        {
            var lobby = await LobbyService.Instance.GetLobbyAsync(GameConstants.Instance._lobbyId);
            var players = lobby.Players;
            var newHost = players.FirstOrDefault(p => p.Id != lobby.HostId);
            if (newHost != null)
            {
                await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
                {
                    HostId = newHost.Id
                });
                Debug.Log("Host migrated to " + newHost.Data["username"].Value);
            }
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }


}
