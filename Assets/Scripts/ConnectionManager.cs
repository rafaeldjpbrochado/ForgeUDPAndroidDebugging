using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    public struct ServerInfo : IComparer<ServerInfo>, System.IComparable<ServerInfo>, System.IEquatable<ServerInfo>
    {
        public ushort port;
        public string ip;

        public ServerInfo(string ip, ushort port)
        {
            this.ip = ip;
            this.port = port;
        }

        public static ServerInfo Null { get { return new ServerInfo (null, 0); } }

        public int Compare (ServerInfo x, ServerInfo y)
        {
            int ipResult = string.CompareOrdinal (x.ip, y.ip);
            int portResult = x.port.CompareTo(y.port);
            return ipResult == 0 ? portResult : ipResult;
        }
        public int CompareTo (ServerInfo other) { return Compare (this, other); }
        public bool Equals (ServerInfo other) { return ip == other.ip && port == other.port; }
    }

    public const float TIME_TO_REFRESH_SERVER_LIST = 5f;
    private const int MAX_CONNECTED_SPECTATORS = 30;

    public string MyIpAddress { get; private set; }
    private ushort _portNumber = 15937;
    private NetWorker _myNetWorker;
    private List<ServerInfo> _activeServers;
    private ServerInfo _reconnectToServer = ServerInfo.Null;

    [SerializeField] private NetworkManager _networkManager;

    public static ConnectionManager Instance { get; private set; }

    public event System.Action Client_ConnectionToServerAttempted;
    public event System.Action Client_ConnectionToServerSucceded;
    public event System.Action Client_ConnectionToServerFailed;
    public event System.Action Client_DisconnectionFromServerStarted;
    public event System.Action Client_DisconnectedFromServer;
    public event System.Action Client_ServerListRefreshStarted;
    public event System.Action Client_ServerListRefreshFinished;

    public bool IsRefreshingList { get; private set; } = false;
    public ReadOnlyCollection<ServerInfo> ActiveServers { get { return _activeServers.AsReadOnly(); } }
    public bool IsCurrentlyConnectingToServer { get; private set; } = false;
    public ServerInfo ServerCurrentlyConnectedTo { get; private set; }

    public void Awake ()
    {
        Instance = this;

        IPAddress foundLocalIp = GetLocalIP();
        Debug.Assert(foundLocalIp != IPAddress.None, "A Local IP Address was not found for some reason");
        MyIpAddress = foundLocalIp.ToString();

        // Do any firewall opening requests on the operating system
        NetWorker.PingForFirewall(_portNumber);
        //Ensure that all RPCs are automatically run on the main thread to avoid errors
        Rpc.MainThreadRunner = MainThreadManager.Instance;

        if (GameInfo.IsClient)
        {
            _activeServers = new List<ServerInfo>();
        }
        else
        {
            Server_HostGame();
        }
        ServerCurrentlyConnectedTo = ServerInfo.Null;
    }

    #region Server Connection

    void Server_HostGame ()
    {
        UDPServer udpServer = new UDPServer(MAX_CONNECTED_SPECTATORS);
        udpServer.Connect(MyIpAddress, _portNumber);
        udpServer.playerTimeout += Server_OnClientTimeout;
        _myNetWorker = udpServer;

        OnConnectionMade(udpServer);

        udpServer.playerDisconnected += Server_OnClientDisconnection;
    }

    private void Server_OnClientDisconnection (NetworkingPlayer client, NetWorker sender)
    {
        MainThreadManager.Run (() =>
        {
            //loop through all network objects do delete the ones owned by the disconnected player
            for (int i = sender.NetworkObjectList.Count - 1; i >= 0; --i)
            {
                NetworkObject networkObj = sender.NetworkObjectList[i];
                if (networkObj.Owner == client)
                {
                    networkObj.Destroy();
                }
            }
        });
    }

    void Server_OnClientTimeout (NetworkingPlayer client, NetWorker sender)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log ("Player " + client.NetworkId + " timed out");
        #endif
    }

    #endregion

    private void OnConnectionMade (NetWorker networker)
    {
        Debug.Assert(networker.IsBound, "NetWorker failed to build");

        _networkManager.Initialize(networker);
        NetworkObject.Flush(networker); //Called because we are already in the correct scene!
    }

    #region Client Connect and Disconnect

    public void Client_ConnectToServer (ServerInfo hostInfo, bool forceDisconnectFromCurrentServer)
    {
        if (IsCurrentlyConnectingToServer)
        {
            Debug.Assert (false, "You tried to connect to a server when you're already in the process of connecting to one");
        }
        else if (ServerCurrentlyConnectedTo.Equals (ServerInfo.Null))
        {
            //If we aren't connected to a host then the second parameter doesn't matter
            Client_ConnectToServer(hostInfo);
        }
        else if (forceDisconnectFromCurrentServer)
        {
            _myNetWorker.disconnected += Client_ReconnectionHandler;
            _reconnectToServer = hostInfo;
            Client_DisconnectFromServer ();
        }
        else
        {
            Debug.Assert (false, "You are already connected to a server!");
        }
    }

    private void Client_ReconnectionHandler (NetWorker sender)
    {
        sender.disconnected -= Client_ReconnectionHandler;

        Debug.Assert (!_reconnectToServer.Equals(ServerInfo.Null), "Client_ReconnectionHandler given a null server");
        Client_ConnectToServer (_reconnectToServer);

        _reconnectToServer = ServerInfo.Null;
    }

    private void Client_ConnectToServer (ServerInfo hostInfo)
    {
        UDPClient udpClient = new UDPClient ();
        udpClient.Connect (hostInfo.ip, hostInfo.port);
        _myNetWorker = udpClient;

        OnConnectionMade (udpClient);
        IsCurrentlyConnectingToServer = true;
        ServerCurrentlyConnectedTo = hostInfo;

        udpClient.disconnected += Client_OnDisconnected;
        udpClient.serverAccepted += Client_OnConnectionSuccess;
        udpClient.connectAttemptFailed += Client_OnConnectionFailure;

        Client_ConnectionToServerAttempted?.Invoke();
    }

    public void Client_DisconnectFromServer ()
    {
        Debug.Assert (GameInfo.IsClient, "The server should not be calling DisconnectFromPlayer()");
        if (GameInfo.IsClient && !IsCurrentlyConnectingToServer)
        {
            ServerCurrentlyConnectedTo = ServerInfo.Null;
            _myNetWorker.Disconnect (false);
            Client_DisconnectionFromServerStarted?.Invoke();
        }
        Debug.Assert(!IsCurrentlyConnectingToServer, "You tried to disconnect from a server you're in the proces of connecting to. Probably not kosher");
    }

    #endregion

    #region Client Connect and Disconnect Callbacks

    private void Client_OnConnectionSuccess (NetWorker sender)
    {
        sender.serverAccepted -= Client_OnConnectionSuccess;
        IsCurrentlyConnectingToServer = false;

        MainThreadManager.Run( () =>
        {
            Client_ConnectionToServerSucceded?.Invoke();
        });
    }

    private void Client_OnConnectionFailure (NetWorker sender)
    {
        ((UDPClient)sender).connectAttemptFailed -= Client_OnConnectionFailure;
        ServerCurrentlyConnectedTo = ServerInfo.Null;

        MainThreadManager.Run (() => 
        {
            Client_ConnectionToServerFailed?.Invoke();
        });
    }

    private void Client_OnDisconnected (NetWorker sender)
    {
        sender.disconnected -= Client_OnDisconnected;
        ServerCurrentlyConnectedTo = ServerInfo.Null;
        _myNetWorker = null;

        MainThreadManager.Run (() =>
        {
            //loop through and delete all network objects
            for (int i = sender.NetworkObjectList.Count - 1; i >= 0; --i)
            {
                sender.NetworkObjectList[i].Destroy();
            }

            Client_DisconnectedFromServer?.Invoke();
        });
    }

    #endregion

    #region UDP Search

    public void Client_RefreshServerList()
    {
        if (IsRefreshingList) { return; }

        IsRefreshingList = true;
        NetWorker.RefreshLocalUdpListings (_portNumber);
        Client_ServerListRefreshStarted?.Invoke();

        StartCoroutine(InvokeEventWhenListRefreshed(Time.time));
    }

    IEnumerator InvokeEventWhenListRefreshed (float startTime)
    {
        while(Time.time - startTime < TIME_TO_REFRESH_SERVER_LIST)
        {
            yield return null;
        }

        _activeServers.Clear();
        foreach (NetWorker.BroadcastEndpoints endpoint in NetWorker.LocalEndpoints)
        {
            if (endpoint.IsServer)
            {
                _activeServers.Add(new ServerInfo (endpoint.Address, endpoint.Port));
            }
        }
        _activeServers.Sort();

        for (int i = _activeServers.Count - 2; i >= 0; --i)
        {
            if (_activeServers[i].Equals (_activeServers[i + 1]))
            {
                _activeServers.RemoveAt(i + 1);
            }
        }

        IsRefreshingList = false;
        Client_ServerListRefreshFinished?.Invoke();
    }

    #endregion

    private static IPAddress GetLocalIP ()
    {
        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces ())
        {
            switch (nic.NetworkInterfaceType)
            {
                case NetworkInterfaceType.Wireless80211:
                case NetworkInterfaceType.Ethernet:
                    break;
                default:
                    continue;
            }

            if (nic.OperationalStatus != OperationalStatus.Up) continue;

            foreach (UnicastIPAddressInformation ip in nic.GetIPProperties ().UnicastAddresses)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.Address;
                }
            }
        }

        return IPAddress.None;
    }

    private void OnDestroy ()
    {
        OnApplicationQuit();
    }

    private void OnApplicationQuit()
    {
        //Cleaning up things that were opened up for UDP discovery
        if (GameInfo.IsClient)
        {
            NetWorker.EndSession ();
        }
        else
        {
            //Tell other players to disconnect before you quit
            _myNetWorker.Disconnect(true);
        }
    }
}
