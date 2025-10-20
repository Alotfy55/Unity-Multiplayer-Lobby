using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;

public class LobbyUiManager : MonoBehaviour
{
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


    public async void CreateRoom()
    {
        var roomName = RoomName.text;
        var roomPassword = RoomPassword.text;
        if (roomName.Length == 0 || roomPassword.Length == 0)
        {
            return;
        }

        await LobbyManager.Instance.CreateLobby(roomName, roomPassword);
    }

    public async void CreateRandomRoom()
    {
        await LobbyManager.Instance.CreateLobby("Room " + UnityEngine.Random.Range(1000, 9999));
    }

    public async void ListRooms()
    {
        List<Lobby> lobbies = await LobbyManager.Instance.FetchLobbies();

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