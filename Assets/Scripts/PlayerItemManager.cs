using TMPro;
using UnityEngine;

public class PlayerItemManager : MonoBehaviour
{
    [SerializeField]
    TMP_Text playerNameTextField;

    public void SetPlayerName(string playerName)
    {
        playerNameTextField.text = playerName;
    }
}
