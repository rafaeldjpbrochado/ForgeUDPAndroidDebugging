using UnityEngine;

public class SpectatorUIController : MonoBehaviour
{
    [SerializeField] private ServerListUIController _serverSelectionDropdown;

    private void Start ()
    {
        if (GameInfo.IsPlayer)
        {
            Destroy(gameObject);
            return;
        }

        _serverSelectionDropdown.PlayerSlecetedServer += OnPlayerSlecetedServer;
        _serverSelectionDropdown.Initialize();
    }

    void OnPlayerSlecetedServer (ConnectionManager.ServerInfo chosenServer)
    {
        if (chosenServer.Equals (ConnectionManager.Instance.ServerCurrentlyConnectedTo))
        {
            _serverSelectionDropdown.ToggleDropdownMenu ();
        }
        else
        {
            ConnectionManager.Instance.Client_ConnectToServer (_serverSelectionDropdown.ChosenServer.CorrespondingServer, true);
        }
    }
}
