using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;

public class ConnectionTestingMessages : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _messageText;
    private System.Text.StringBuilder _stringBuilder;
    private string _client_serverIp;

    private void Awake ()
    {
        _stringBuilder = new System.Text.StringBuilder();
        _stringBuilder.Append("My IP: ").Append(ConnectionManager.Instance.MyIpAddress).Append('\n');
        if (GameInfo.IsSpectator)
        {
            _stringBuilder.Append("Connected Host IP:\n");
            ConnectionManager.Instance.Client_ConnectionToServerSucceded += Client_OnConnectionToServerSucceded;
            ConnectionManager.Instance.Client_DisconnectedFromServer += Client_OnDisconnectedFromServer;
        }
        else
        {
            _stringBuilder.Append("Connected Client IPs:\n");
            NetworkManager.Instance.Networker.playerAccepted += Server_PlayerAccepted;
            NetworkManager.Instance.Networker.playerDisconnected += Server_PlayerDisconnected;
        }
        
        _messageText.SetText(_stringBuilder);
    }

    void Server_PlayerDisconnected (NetworkingPlayer player, NetWorker sender)
    {
        int startIndex = FindString(_stringBuilder, player.Ip);
        if (startIndex >= 0)
        {
            _stringBuilder.Remove(startIndex, player.Ip.Length + 1);
            _messageText.SetText(_stringBuilder);
        }
    }

    void Server_PlayerAccepted (NetworkingPlayer player, NetWorker sender)
    {
        _stringBuilder.Append(player.Ip).Append('\n');
        _messageText.SetText(_stringBuilder);
    }

    void Client_OnConnectionToServerSucceded ()
    {
        _client_serverIp = ConnectionManager.Instance.ServerCurrentlyConnectedTo.ip;
        _stringBuilder.Append(_client_serverIp);
        _messageText.SetText(_stringBuilder);
    }

    void Client_OnDisconnectedFromServer ()
    {
        int startIndex = FindString(_stringBuilder, _client_serverIp);
        if (startIndex >= 0)
        {
            _stringBuilder.Remove(startIndex, _client_serverIp.Length);
            _messageText.SetText(_stringBuilder);
        }
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
}
