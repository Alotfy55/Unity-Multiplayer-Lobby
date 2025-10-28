using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using UnityEngine;

public class VivoxManager : MonoBehaviour
{
    public static VivoxManager Instance;
    private string serverUri = Environment.GetEnvironmentVariable("VIVOX_API_KEY");
    private string domain = Environment.GetEnvironmentVariable("VIVOX_DOMAIN");
    private string issuer = Environment.GetEnvironmentVariable("VIVOX_ISSUER");
    private string tokenKey = Environment.GetEnvironmentVariable("VIVOX_TOKEN_KEY");
    public int tokenExpirySeconds = 120;


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public async Task Initiallize()
    {
        await VivoxService.Instance.InitializeAsync();

        Debug.Log("Using Mic: " + VivoxService.Instance.EffectiveInputDevice.DeviceName);
        Debug.Log("Using Output: " + VivoxService.Instance.EffectiveOutputDevice.DeviceName);
    }

    public async Task ConnectToChannel(string channelName, string channelPassword = "")
    {
        try
        {
            var loginOptions = new LoginOptions { DisplayName = GameConstants.Instance._userName };

            await VivoxService.Instance.LoginAsync(loginOptions);

            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);
        }
        catch (Exception e)
        {
            ToastNotification.Show("Voice chat connection failed");
            Debug.LogWarning($"Vivox connect failed (will timeout anyway): {e.Message}");
        }
    }

    public async Task Disconnect()
    {
        try
        {
            await VivoxService.Instance.LeaveAllChannelsAsync();

            await VivoxService.Instance.LogoutAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Vivox disconnect failed (will timeout anyway): {e.Message}");
        }
    }
}

