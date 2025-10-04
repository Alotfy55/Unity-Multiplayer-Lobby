using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Random = UnityEngine.Random;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;
    private const int MAX_PLAYERS_PER_ROOM = 20;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;

    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateLobby(string lobbyName, string lobbyPassword)
    {
        try
        {
            var options = new CreateLobbyOptions
            {
                Password = lobbyPassword,
                IsPrivate = true,
                Data = new Dictionary<string, DataObject>
                {
                    { "roomName", new DataObject(DataObject.VisibilityOptions.Public, lobbyName) },
                }

            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PLAYERS_PER_ROOM, options);

            Debug.Log("Created Lobby " +lobby.Id + " " + lobby.Name);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }

    public async void CreatePasswordlessLobby(string lobbyName)
    {
        try
        {
            var options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "roomName", new DataObject(DataObject.VisibilityOptions.Public, lobbyName) },
                }

            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PLAYERS_PER_ROOM, options);

            Debug.Log("Created Lobby " + lobby.Id + " " + lobby.Name);

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
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(roomId);
            Debug.Log("Joined Lobby " + lobby.Name);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }

    private async void JoinLobbyById(string lobbyId)
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
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
}
