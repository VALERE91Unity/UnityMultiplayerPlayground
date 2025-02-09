using DilmerGames.Core.Singletons;
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : NetworkSingleton<RelayManager>
{
    public enum RelayType
    {
        Server,
        Client
    }

    [SerializeField]
    private RelayType relayType = RelayType.Client;

    private const string ENVIRONMENT = "production";

    [SerializeField]
    private int maxNumberOfConnections = 10;

    public async Task JoinGame(string joinCode)
    {
        try
        {
            var relayJoinData = await JoinRelayServer(joinCode);

            UnityTransport transport = NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();
            transport.SetRelayServerData(relayJoinData.IPv4Address, relayJoinData.Port, relayJoinData.AllocationIDBytes,
                relayJoinData.Key, relayJoinData.ConnectionData, relayJoinData.HostConnectionData);

            Logger.Instance.LogInfo($"Joined Game With Join Code: {joinCode}");
        }
        catch (Exception e)
        {
            Logger.Instance.LogError(e.Message);
        }
    }

    public static async Task<RelayHostData> SetupRelayServer(int maxConnections = 2)
    {
        InitializationOptions options = new InitializationOptions()
            .SetEnvironmentName(ENVIRONMENT);

        await UnityServices.InitializeAsync(options);

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxConnections);

        RelayHostData relayHostData = new RelayHostData
        {
            Key = allocation.Key,
            Port = (ushort) allocation.RelayServer.Port,
            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            IPv4Address = allocation.RelayServer.IpV4,
            ConnectionData = allocation.ConnectionData
        };

        relayHostData.JoinCode = await Relay.Instance.GetJoinCodeAsync(relayHostData.AllocationID);
        return relayHostData;
    }

    public static async Task<RelayJoinData> JoinRelayServer(string joinCode)
    {
        InitializationOptions options = new InitializationOptions()
            .SetEnvironmentName(ENVIRONMENT);

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);

        RelayJoinData relayJoinData = new RelayJoinData
        {
            Key = allocation.Key,
            Port = (ushort)allocation.RelayServer.Port,
            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            HostConnectionData = allocation.HostConnectionData,
            IPv4Address = allocation.RelayServer.IpV4,
            JoinCode = joinCode
        };
        
        return relayJoinData;
    }
}
