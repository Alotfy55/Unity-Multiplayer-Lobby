using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerCameraController : NetworkBehaviour
{
    private Camera cam;
    public override void OnNetworkSpawn()
    {
        if (cam == null) cam = GetComponentInChildren<Camera>(true);

        // Each client keeps only its local owner's vcam enabled
        bool enable = IsOwner;
        if (cam != null)
        {
            cam.enabled = enable;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (cam != null) cam.enabled = false;
    }
}
