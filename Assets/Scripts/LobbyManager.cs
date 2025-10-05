using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Random = UnityEngine.Random;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;
    private const int MAX_PLAYERS_PER_ROOM = 20;
    private const string CONNECTION_TYPE = "dtls";

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateLobby(string lobbyName, string lobbyPassword = "")
    {
        try
        {
            bool isPrivate = !string.IsNullOrEmpty(lobbyPassword);
            var options = new CreateLobbyOptions
            {
                Password = isPrivate ? lobbyPassword : null,
                IsPrivate = isPrivate,
                Data = new Dictionary<string, DataObject>
                {
                    { "roomName", new DataObject(DataObject.VisibilityOptions.Public, lobbyName) },
                }

            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PLAYERS_PER_ROOM, options);
            Debug.Log("Created room " + lobby.Name);

            Allocation alloc = await CreateRelayAllocation();
            UpdateLobbyWithRelayCode(lobby, alloc);
            StartConnection(alloc);

            Debug.Log("Joined room");
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }


    public async void JoinLobby(string lobbyName)
    {
        try
        {
            string roomId = await GetRoomIdFromName(lobbyName);
            if (roomId == null)
            {
                Debug.Log("Room Not Found");
                return;
            }

            JoinLobbyById(roomId);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }

    public async void JoinLobbyById(string lobbyId)
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            var alloc = await CreateRelayJoinAllocation(lobby);
            JoinConnection(alloc);
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
            foreach (var lobby in response.Results)
            {
                Debug.Log(lobby.Name);
            }
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
            int randomIndex = Random.Range(0, lobbies.Count - 1);

            JoinLobbyById(lobbies[randomIndex].Id);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }

    private async Task<string> GetRoomIdFromName(string roomName)
    {
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.S1, // S1 = Lobby name by default
                op: QueryFilter.OpOptions.EQ,
                value: roomName
                )
            }
            });

            return response.Results[0].Id;
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
            return null;
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
    static async void UpdateLobbyWithRelayCode(Lobby lobby, Allocation alloc)
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
}
