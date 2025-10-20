// RosterUI.cs (attach to a GameObject in the Room scene)
using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Text;
using Unity.Collections;
using System.Collections;

public class PlayerListUiManager : NetworkBehaviour
{
    [SerializeField] private PlayerListManager listManager;
    [SerializeField] private Transform listItem;
    [SerializeField] private Transform listContainer;


    private bool _hooked;
    private bool _waiting;

    public override void OnNetworkSpawn()
    {
        // Try once on spawn; if listManager isn’t ready, a waiter coroutine will hook later.
        TryHook();
        EnsureHookWhenReady();
    }

    void OnEnable()
    {
        TryHook();
        EnsureHookWhenReady();
    }

    void OnDisable()
    {
        Unhook();
    }

    private void EnsureHookWhenReady()
    {
        if (_hooked || _waiting) return;
        StartCoroutine(WaitAndHook());
    }

    private IEnumerator WaitAndHook()
    {
        _waiting = true;

        // Auto-locate listManager if not wired (optional but handy)
        if (!listManager)
            listManager = GetComponent<PlayerListManager>();

        // Wait until we have a spawned listManager with a live NetworkList
        while (listManager == null || !listManager.IsSpawned || listManager.playersList == null)
            yield return null;

        TryHook();
        _waiting = false;
    }
    void TryHook()
    {
        if (!listManager || listManager.playersList == null) return;

        listManager.playersList.OnListChanged += OnListChanged;
        Redraw();
    }

    void Unhook()
    {
        if (listManager?.playersList != null)
            listManager.playersList.OnListChanged -= OnListChanged ;
    }

    void OnListChanged(NetworkListEvent<FixedString64Bytes> _)
    {

        Redraw();
    }

    void Redraw()
    {
        if (listManager?.playersList == null) return;

        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var playerName in listManager.playersList)
        {
            var item = Instantiate(listItem, listContainer);
            var itemManager = item.GetComponentInChildren<PlayerItemManager>();
            if (itemManager != null)
            {
                itemManager.SetPlayerName( playerName.ToString());
            }
        }
    }
}
