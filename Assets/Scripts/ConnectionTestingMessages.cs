using UnityEngine;
using UnityEngine.UI;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;

public class ConnectionTestingMessages : MonoBehaviour
{
    [SerializeField] private Text _messageText;
    private System.Text.StringBuilder _stringBuilder;
    private string _client_serverNetworkInfo;
    private string _client_myNetworkInfo;

    public Text forgeDebugLogText;
    public float slowUpdateCooldown;

    private void Awake ()
    {
        string myNetInfo = FormatMyNetworkInfo();

        _stringBuilder = new System.Text.StringBuilder();
        _stringBuilder.Append("My IP- ").Append(myNetInfo).Append('\n');
        if (GameInfo.IsClient)
        {
            _client_myNetworkInfo = myNetInfo;
            _stringBuilder.Append("Connected Host IP-\n");
            ConnectionManager.Instance.Client_ConnectionToServerSucceded += Client_OnConnectionToServerSucceded;
            ConnectionManager.Instance.Client_DisconnectedFromServer += Client_OnDisconnectedFromServer;
        }
        else
        {
            _stringBuilder.Append("Connected Client IPs-\n");
            NetworkManager.Instance.Networker.playerAccepted += Server_PlayerAccepted;
            NetworkManager.Instance.Networker.playerDisconnected += Server_PlayerDisconnected;
        }
        
        _messageText.text = _stringBuilder.ToString();
    }

    void Server_PlayerAccepted (NetworkingPlayer player, NetWorker sender)
    {
        MainThreadManager.Run(
            () => {
                _stringBuilder.Append(FormatNetworkInfo(player.Ip, player.Port)).Append('\n');
                _messageText.text = _stringBuilder.ToString();
            },
            MainThreadManager.UpdateType.Update
        );
    }

    void Server_PlayerDisconnected (NetworkingPlayer player, NetWorker sender)
    {
        MainThreadManager.Run(
            () => {
                string netInfo = FormatNetworkInfo(player.Ip, player.Port);

                int startIndex = FindString(_stringBuilder, netInfo);
                if (startIndex >= 0)
                {
                    _stringBuilder.Remove(startIndex, netInfo.Length + 1);
                    _messageText.text = _stringBuilder.ToString();
                }
            },
            MainThreadManager.UpdateType.Update
        );
    }

    void Client_OnConnectionToServerSucceded ()
    {
        ConnectionManager.ServerInfo server = ConnectionManager.Instance.ServerCurrentlyConnectedTo;
        _client_serverNetworkInfo = FormatNetworkInfo (server.ip, server.port);

        string currentNetInfo = FormatMyNetworkInfo();
        if (_client_myNetworkInfo != currentNetInfo)
        {
            int startIndex = FindString(_stringBuilder, _client_myNetworkInfo);
            _stringBuilder.Remove(startIndex, _client_myNetworkInfo.Length);
            _stringBuilder.Insert(startIndex, currentNetInfo);

            _client_myNetworkInfo = currentNetInfo;
        }

        _stringBuilder.Append(_client_serverNetworkInfo);
        _messageText.text = _stringBuilder.ToString();
    }

    void Client_OnDisconnectedFromServer ()
    {
        int startIndex = FindString(_stringBuilder, _client_serverNetworkInfo);
        if (startIndex >= 0)
        {
            _stringBuilder.Remove(startIndex, _client_serverNetworkInfo.Length);
            _messageText.text = _stringBuilder.ToString();
        }
    }

    private string FormatMyNetworkInfo()
    {
        return FormatNetworkInfo(ConnectionManager.Instance.MyIpAddress, ConnectionManager.Instance.MyPortNumber);
    }

    private string FormatNetworkInfo(string ipAddress, ushort port)
    {
        return ipAddress + (": " + port);
    }

    int FindString (System.Text.StringBuilder stringBuilder, string findThis)
    {
        for (int i = 0; i < stringBuilder.Length - findThis.Length; ++i)
        {
            bool isMatch = true;
            for (int j = 0; j < findThis.Length; ++j)
            {
                if (stringBuilder[i+j] != findThis[j])
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch)
            {
                return i;
            }
        }

        return -1;
    }

    public void Update()
    {
        slowUpdateCooldown -= Time.deltaTime;
        if(slowUpdateCooldown < 0)
        {
            slowUpdateCooldown = 1.0f;
            slowUpdate();
        }
    }

    public void slowUpdate()
    {
        forgeDebugLogText.text = "Debug Log at: " + Application.persistentDataPath + "/" + "Logs/bmslog.txt" + "\n";
        System.IO.StreamReader reader = new System.IO.StreamReader(Application.persistentDataPath + "/" + "Logs/bmslog.txt");
        while (!reader.EndOfStream)
        {
            forgeDebugLogText.text += reader.ReadLine();
        }
        reader.Close();
    }
}
