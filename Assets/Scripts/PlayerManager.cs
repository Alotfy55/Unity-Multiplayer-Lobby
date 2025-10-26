using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>(string.Empty, writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] TMP_Text nameText;

    private Camera targetCamera;

    public override void OnNetworkSpawn()
    {
        if (nameText == null)
            nameText = GetComponentInChildren<TextMeshPro>(true);

        PlayerName.OnValueChanged += OnNameChanged;

        if (IsOwner)
        {
            var username = (GameConstants.Instance != null && !string.IsNullOrEmpty(GameConstants.Instance._userName))
                ? GameConstants.Instance._userName
                : $"Player{OwnerClientId}";

            PlayerName.Value = username;

            nameText.gameObject.SetActive(false);
        }

        OnNameChanged(default, PlayerName.Value);
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            var localPlayer = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                targetCamera = localPlayer.GetComponentInChildren<Camera>();
            }

            if (targetCamera == null && Camera.main != null)
                targetCamera = Camera.main;

            if (targetCamera == null)
                return;
        }

        Vector3 dir = nameText.transform.position - targetCamera.transform.position;
        nameText.transform.forward = dir.normalized;
    }


    private void OnNameChanged(FixedString64Bytes oldVal, FixedString64Bytes newVal)
    {
        if (nameText) nameText.text = newVal.ToString();
    }

    public override void OnNetworkDespawn()
    {
        PlayerName.OnValueChanged -= OnNameChanged;
    }
}
