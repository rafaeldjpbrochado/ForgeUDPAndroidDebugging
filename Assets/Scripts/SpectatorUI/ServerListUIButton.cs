using UnityEngine;
using UnityEngine.UI;

public class ServerListUIButton : MonoBehaviour
{
    [SerializeField] private Button _buttonObject;
    [SerializeField] private Sprite _buttonColor;
    [SerializeField] private Sprite _buttonColorAlt;
    [SerializeField] private Sprite _buttonColorHighlight;

    [SerializeField] private Text _buttonText;
    [SerializeField] private Color _textColor;
    [SerializeField] private Color _textColorHighlight;

    [SerializeField] private Image _myImage;

    private int _index;

    public Button Button { get { return _buttonObject; } }
    public Text Text { get { return _buttonText; } }
    public Image MyImage { get { return _myImage; } }

    public event System.Action<ServerListUIButton> ButtonPressed;
    public ConnectionManager.ServerInfo CorrespondingServer;

    public void Initialize (int index)
    {
        _index = index;
        MyImage.sprite = GetDefaultSprite();
    }

    private Sprite GetDefaultSprite ()
    {
        return (_index & 0x1) == 0 ? _buttonColor : _buttonColorAlt;
    }

    public void OnUIButtonPress ()
    {
        ButtonPressed.Invoke(this);
    }

    public void SetButtonHighlight(bool isButtonHighlighted)
    {
        _buttonText.color = isButtonHighlighted ? _textColorHighlight : _textColor;
        MyImage.sprite = isButtonHighlighted ? _buttonColorHighlight : GetDefaultSprite();
    }
}
