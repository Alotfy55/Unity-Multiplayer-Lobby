using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;

public class LobbyUiManager : MonoBehaviour
{
    public static LobbyUiManager Instance;
    [SerializeField] List<MenuObject> SceneMenus;


    [SerializeField]
    TMP_InputField usernameInputField;


    [SerializeField]
    TMP_InputField RoomName;
    [SerializeField]
    TMP_InputField RoomPassword;

    [SerializeField]
    Transform roomsListContainer;
    [SerializeField]
    Transform roomListItem;
    [SerializeField]
    GameObject loadingScreen;
    [SerializeField]
    TMP_Text _loadingText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public async void CreateRoom()
    {
        var roomName = RoomName.text;
        var roomPassword = RoomPassword.text;
        if (string.IsNullOrEmpty(roomName))
            ToastNotification.Show("Room name cannot be empty");
        
        if (string.IsNullOrEmpty(roomPassword) || roomPassword.Length < 8)
        {
            ToastNotification.Show("Room password must be at least 8 characters long");
            return;
        }
        
        EnableLoadingScreen("Creating Room...");
        await LobbyManager.Instance.CreateLobby(roomName, roomPassword);
    }
    
    public async void JoinRoom()
    {
        var roomName = RoomName.text;
        var roomPassword = RoomPassword.text;
        if (string.IsNullOrEmpty(roomName))
            ToastNotification.Show("Room name cannot be empty");
        
        if (string.IsNullOrEmpty(roomPassword) || roomPassword.Length < 8)
        {
            ToastNotification.Show("Room password must be at least 8 characters long");
            return;
        }

        var lobbies = await LobbyManager.Instance.FetchLobbies();

        var lobby = lobbies.Where(l => l.Name == roomName).FirstOrDefault();
        if (lobby == null)
        {
            ToastNotification.Show("Room not found");
            return;
        }

        EnableLoadingScreen("Joining Room...");
        await LobbyManager.Instance.JoinLobby(lobby.Id, roomPassword);
    }

    public async void CreateRandomRoom()
    {
        EnableLoadingScreen("Creating Room...");
        await LobbyManager.Instance.CreateLobby("Room " + UnityEngine.Random.Range(1000, 9999));
    }

    public async void ListRooms()
    {
        List<Lobby> lobbies = await LobbyManager.Instance.FetchLobbies();
        lobbies = lobbies.Where(l => !l.HasPassword).ToList();

        foreach (Transform child in roomsListContainer)
        {
                Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbies)
        {
            Transform child = Instantiate(roomListItem, roomsListContainer);
            child.gameObject.SetActive(true);
            child.GetComponent<RoomListItem>().SetValues(lobby);
        }
    }

    public void JoinRandomRoom()
    {
        EnableLoadingScreen("Joining Room...");
        LobbyManager.Instance.JoinRandomRoom();
    }

    public void NavigateToRoomList()
    {
        ChangeMenu(MainSceneMenus.RoomListMenu);
    }

    public void NavigateToCustomRoomsMenu()
    {
        ChangeMenu(MainSceneMenus.CustomRoomMenu);
    }

    public void NavigateToMainMenu()
    {
        ChangeMenu(MainSceneMenus.MainMenu);
    }

    public void CloseApplication()
    {
        Application.Quit();
    }
    private void ChangeMenu(MainSceneMenus newMenu)
    {
        foreach (var menu in SceneMenus)
            menu.gameObject.SetActive(menu.menuID == newMenu);
    }

    public void SetUsername()
    {
        var username = usernameInputField.text;
        GameConstants.Instance.SetUserName(username);
    }

    public void EnableLoadingScreen(string loadingText)
    {
        if (_loadingText == null || loadingScreen == null)
            return;
        _loadingText.text = loadingText;
        loadingScreen.SetActive(true);
    }

    public void DisableLoadingScreen()
    { 
        if (_loadingText == null || loadingScreen == null)
            return; 
        _loadingText.text = "";
        loadingScreen.SetActive(false);
    }


}

public enum MainSceneMenus
{
    MainMenu = 0,
    RoomListMenu = 1, 
    CustomRoomMenu = 2,
}

[Serializable]
public struct MenuObject
{
    public MainSceneMenus menuID;
    public GameObject gameObject;

    public MenuObject(MainSceneMenus menu , GameObject obj)
    {
        menuID = menu;
        gameObject = obj;
    }
}