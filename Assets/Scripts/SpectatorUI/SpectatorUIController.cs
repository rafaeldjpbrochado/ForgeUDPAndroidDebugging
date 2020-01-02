using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpectatorUIController : MonoBehaviour
{
    private enum UIState { start, playingGame }
    private UIState _state = UIState.start;
    private UIState State
    {
        get { return _state; }
        set
        {
            //This was written with the assumption that there's only 2 ui states to switch between.
            _state = value;

            bool isPlaying = _state == UIState.playingGame;

            _startOnlyUI.SetActive (!isPlaying);

            RectTransform chosenAnchor = isPlaying ? _serverListPlayAnchor : _serverListStartAnchor;
            _serverSelectionDropdown.MyRectTransform.anchorMin = chosenAnchor.anchorMin;
            _serverSelectionDropdown.MyRectTransform.anchorMax = chosenAnchor.anchorMax;
            _serverSelectionDropdown.MyRectTransform.anchoredPosition = chosenAnchor.anchoredPosition;
            _serverSelectionDropdown.MyScrollWindow.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, chosenAnchor.rect.height);

            _serverSelectHeader.SetText (isPlaying ? "NOW VIEWING" : "SELECT LAB");

            _joinButton.gameObject.SetActive(isPlaying ? false : _joinButton.gameObject.activeSelf);
        }
    }

    [SerializeField] private GameObject _startOnlyUI;
    [SerializeField] private GameObject _pauseUI;
    [SerializeField] private Image _pauseUIBackgroundOverlay;
    [SerializeField] private TextMeshProUGUI _pauseUIText;
    [SerializeField] private ServerListUIController _serverSelectionDropdown;
    [SerializeField] private TextMeshProUGUI _serverSelectHeader;
    [SerializeField] private RectTransform _serverListStartAnchor;
    [SerializeField] private RectTransform _serverListPlayAnchor;
    [SerializeField] private Button _joinButton;

    private bool isDisconnectExpected = false;

    private void Start ()
    {
        State = UIState.start;
        _joinButton.gameObject.SetActive(false);
        _pauseUI.SetActive (false);

        ConnectionManager.Instance.Client_ConnectionToServerFailed += OnConnectionFail;
        ConnectionManager.Instance.Client_ConnectionToServerSucceded += OnConnectionSucceded;
        ConnectionManager.Instance.Client_DisconnectedFromServer += CheckForUnexpectedDisconnect;

        _serverSelectionDropdown.PlayerSlecetedServer += OnPlayerSlecetedServer;
        _serverSelectionDropdown.UIListUpdated += OnDropdownListUpdate;
        _serverSelectionDropdown.Initialize();
    }

    //This catches if the player disconnected their game while we are spectating
    void CheckForUnexpectedDisconnect ()
    {
        if (!isDisconnectExpected)
        {
            TogglePauseUI (true, disableBackgroundOverlay: true);
            _pauseUIText.SetText ("You've been Disconnected from the Game");
        }
    }

    void OnPlayerSlecetedServer (ConnectionManager.ServerInfo chosenServer)
    {
        switch (_state)
        {
            case UIState.start:
                _joinButton.gameObject.SetActive(true);
                break;
            case UIState.playingGame:
                if (chosenServer.Equals (ConnectionManager.Instance.ServerCurrentlyConnectedTo))
                {
                    _serverSelectionDropdown.ToggleDropdownMenu ();
                }
                else
                {
                    isDisconnectExpected = true;
                    ConnectionManager.Instance.Client_ConnectToServer (_serverSelectionDropdown.ChosenServer.CorrespondingServer, true);
                }
                break;
        }
    }

    void OnDropdownListUpdate ()
    {
        if (_state == UIState.start)
        {
            if (_serverSelectionDropdown.NumActiveServerButtons == 0)
            {
                _joinButton.gameObject.SetActive(false);
            }
            else
            {
                //Select a server if one isn't already chosen
                if (_serverSelectionDropdown.ChosenServer == null)
                {
                    _serverSelectionDropdown.ChosenServer = _serverSelectionDropdown.GetActiveServerButton(0);
                }
                _joinButton.gameObject.SetActive(true);
            }
        }
    }

    private void TogglePauseUI(bool enable, bool disableBackgroundOverlay = false)
    {
        _pauseUI.SetActive(enable);
        _pauseUIBackgroundOverlay.gameObject.SetActive(!disableBackgroundOverlay);
    }

    private void OnConnectionFail ()
    {
        ConnectionManager.Instance.Client_RefreshServerList();
    }

    private void OnConnectionSucceded ()
    {
        isDisconnectExpected = false;
        TogglePauseUI(true);

        _pauseUIText.SetText ("\n\nLoading...\n\nThe player might be currently paused");
    }

    public void JoinSelectedServer ()
    {
        ConnectionManager.Instance.Client_ConnectionToServerSucceded += OnSpectatingStart;
        ConnectionManager.Instance.Client_ConnectToServer (_serverSelectionDropdown.ChosenServer.CorrespondingServer, false);
    }

    private void OnSpectatingStart ()
    {
        ConnectionManager.Instance.Client_ConnectionToServerSucceded -= OnSpectatingStart;

        State = UIState.playingGame;
    }
}
