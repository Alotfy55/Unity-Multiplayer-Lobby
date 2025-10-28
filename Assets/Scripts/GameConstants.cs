using System;
using UnityEngine;

public class GameConstants : MonoBehaviour
{
    public static GameConstants Instance;
    [NonSerialized]public string _userName;
    [NonSerialized]public string _lobbyId;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void SetUserName(string userName)
    {
        if (string.IsNullOrEmpty(userName))
            _userName = "Player " + UnityEngine.Random.Range(1000, 9999);
        else
            _userName = userName;
        ToastNotification.Show("Welcome " + _userName);
        Debug.Log("Username set to: " + _userName);
    }
}
