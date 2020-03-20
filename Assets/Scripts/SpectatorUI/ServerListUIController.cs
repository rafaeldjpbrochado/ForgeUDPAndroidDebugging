using System.Text;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.UI;

public class ServerListUIController : MonoBehaviour
{
    private const string NO_AVALIABLE_LABS_TEXT =   "No Labs Found.";
    private const string SELECT_A_LAB_TEXT =        "Select a Lab";
    private const string SEARCHING_FOR_LABS_TEXT =  "Searching for Labs";//this should be around the maximum string length the button

    public event System.Action UIListUpdated;
    public event System.Action<ConnectionManager.ServerInfo> PlayerSlecetedServer;

    private List<ServerListUIButton> _buttons;
    public int NumActiveServerButtons { get; private set; }
    private StringBuilder gc_uiText;

    [SerializeField] private RectTransform _myRectTransform;
    public RectTransform MyRectTransform { get { return _myRectTransform; } }
    [SerializeField] private Button _refreshButton;
    [SerializeField] private Text _dropdownButtonText;
    [SerializeField] private Image _dropdownButtonChangingSprite;
    [SerializeField] private Sprite _dropdownButtonOnSprite;
    [SerializeField] private Sprite _dropdownButtonOffSprite;
    [SerializeField] private RectTransform _serverListScrollWindow;
    public RectTransform MyScrollWindow { get { return _serverListScrollWindow; } }
    [SerializeField] private RectTransform _buttonPane;
    [SerializeField] private ServerListUIButton _buttonPrefab;
    [SerializeField] private float _buttonHeight;
    [SerializeField] private float _buttonSpacing;

    private ServerListUIButton _chosenServer = null;
    public ServerListUIButton ChosenServer
    {
        get { return _chosenServer; }
        set
        {
            if (_chosenServer != null)
            {
                _chosenServer.SetButtonHighlight (false);
            }
            _chosenServer = value;

            if (value != null)
            {
                value.SetButtonHighlight (true);
                SetDropdownButtonText();
            }
        }
    }

    public void Initialize()
    {
        _buttons = new List<ServerListUIButton>();
        NumActiveServerButtons = 0;
        gc_uiText = new StringBuilder();

        ConnectionManager.Instance.Client_ServerListRefreshFinished += UpdateUI;
        ConnectionManager.Instance.Client_ServerListRefreshStarted += StartLoadingAnimation;
        ChosenServer = null;
        SetDropDownActiveState(false);

        StartCoroutine(ListRefreshLoop());
    }

    public ServerListUIButton GetActiveServerButton (int index)
    {
        if (index >= 0 && index <= NumActiveServerButtons)
        {
            return _buttons[index];
        }

        return null;
    }

    IEnumerator ListRefreshLoop()
    {
        WaitForSeconds waitTime = new WaitForSeconds(5f);

        while (true)
        {
            ConnectionManager.Instance.Client_RefreshServerList();
            yield return waitTime;
        }
    }

    public void UpdateUI()
    {
        ReadOnlyCollection<ConnectionManager.ServerInfo> activeServers = ConnectionManager.Instance.ActiveServers;

        float effectiveButtonHeight = _buttonHeight + _buttonSpacing;
        for (int i = 0; i < activeServers.Count; ++i)
        {
            //If this is true we need to spawn new buttons
            if (i >= _buttons.Count)
            {
                //spawn new button
                ServerListUIButton newButton = Instantiate (_buttonPrefab, _buttonPane);
                newButton.Initialize (i);
                newButton.ButtonPressed += OnServerSelect;

                //Figure out which yPos to place the button at
                float buttonY = i * -effectiveButtonHeight;
                newButton.GetComponent<RectTransform>().anchoredPosition = new Vector3(0f, buttonY, 0f);

                //add the button to the list
                _buttons.Add (newButton);
            }

            //enable button if it was disabled before
            _buttons[i].gameObject.SetActive(true);
            //setup newButtonScript
            _buttons[i].CorrespondingServer = activeServers[i];
            //update button text
            _buttons[i].Text.text = GetServerDescription(activeServers[i]).ToString();
        }
        //Disable the buttons that were previously active but now don't need to be active
        //Happens with the number of activeServers drops
        for (int i = activeServers.Count; i < NumActiveServerButtons; ++i)
        {
            _buttons[i].SetButtonHighlight(false);
            _buttons[i].gameObject.SetActive(false);
        }

        //Update the number of active buttons
        NumActiveServerButtons = activeServers.Count;
        //Expand or contract the button pane based on the number of buttons it contains
        _buttonPane.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, _buttonHeight + Mathf.Max(0, NumActiveServerButtons - 1) * effectiveButtonHeight);

        UIListUpdated?.Invoke();

        if (NumActiveServerButtons == 0)
        {
            ChosenServer = null;
        }
    }

    private void StartLoadingAnimation ()
    {
        _refreshButton.interactable = false;
        ConnectionManager.Instance.Client_RefreshServerList();

        StartCoroutine(AnimateLoadButton());
    }

    IEnumerator AnimateLoadButton ()
    {
        if (NumActiveServerButtons == 0)
        {
            _dropdownButtonText.text = SEARCHING_FOR_LABS_TEXT;
        }
        _refreshButton.interactable = false;

        Transform loadingSymbol = _refreshButton.transform.GetChild (0);
        float angle = 0f;
        while (ConnectionManager.Instance.IsRefreshingList)
        {
            angle -= Time.deltaTime * 360f;
            angle = angle < 0f ? 360f - angle : angle;
            loadingSymbol.eulerAngles = new Vector3 (0, 0, angle);

            yield return null;
        }
        loadingSymbol.eulerAngles = Vector3.zero;

        if (NumActiveServerButtons == 0)
        {
            SetDropdownButtonText();
        }
        _refreshButton.interactable = true;
    }

    private void SetDropdownButtonText ()
    {
        if (ChosenServer == null)
        {
            _dropdownButtonText.text = NumActiveServerButtons == 0 ? NO_AVALIABLE_LABS_TEXT : SELECT_A_LAB_TEXT;
        }
        else
        {
            _dropdownButtonText.text = GetServerDescription (ChosenServer.CorrespondingServer).ToString();
        }
    }

    public static void AppendServerDescription(StringBuilder stringBuilder, ConnectionManager.ServerInfo serverInfo)
    {
        stringBuilder.Append ("ip: ");
        stringBuilder.Append (serverInfo.ip);
    }

    private StringBuilder GetServerDescription (ConnectionManager.ServerInfo serverInfo)
    {
        gc_uiText.Clear ();
        gc_uiText.Append (serverInfo.ip).Append(": ").Append(serverInfo.port);

        return gc_uiText;
    }

    void OnServerSelect (ServerListUIButton pressedButton)
    {
        ChosenServer = pressedButton;
        PlayerSlecetedServer?.Invoke(pressedButton.CorrespondingServer);
    }

    public void UIButton_RefreshServerList ()
    {
        ConnectionManager.Instance.Client_RefreshServerList();
    }

    public void ToggleDropdownMenu ()
    {
        SetDropDownActiveState(!_serverListScrollWindow.gameObject.activeSelf);
    }

    private void SetDropDownActiveState(bool isActive)
    {
        _serverListScrollWindow.gameObject.SetActive (isActive);
        _dropdownButtonChangingSprite.sprite = isActive ? _dropdownButtonOnSprite : _dropdownButtonOffSprite;
        _dropdownButtonText.gameObject.SetActive (!isActive);
    }
}
