
public enum GameType
{
    Player,
    Spectator
}

public static class GameInfo
{
    public static GameType GameType { get; private set; }
    public static bool IsPlayer { get { return GameType == GameType.Player; } }
    public static bool IsSpectator { get { return GameType == GameType.Spectator; } }

    //This will initialize all the values the first time another class
    //references this one
    static GameInfo ()
    {
        GameType = GameType.Player;

        #if UNITY_IOS || UNITY_ANDROID
            GameType = GameType.Spectator;
        #endif

        #if UNITY_EDITOR
            GameInfoDebugSettings debugSettings = UnityEngine.Object.FindObjectOfType<GameInfoDebugSettings>();
            if (debugSettings.ForceSpectatorMode)
            {
                GameType = GameType.Spectator;
            }
        #endif
    }
}
