
public enum GameType
{
    Server,
    Client
}

public static class GameInfo
{
    public static GameType GameType { get; private set; }
    public static bool IsServer { get { return GameType == GameType.Server; } }
    public static bool IsClient { get { return GameType == GameType.Client; } }

    //This will initialize all the values the first time another class
    //references this one
    static GameInfo ()
    {
        GameType = GameType.Server;

        #if UNITY_IOS || UNITY_ANDROID
            GameType = GameType.Client;
        #endif

        #if UNITY_EDITOR
            GameType = GameType.Server;
            
            GameInfoDebugSettings debugSettings = UnityEngine.Object.FindObjectOfType<GameInfoDebugSettings>();
            if (debugSettings.LaunchAsClient)
            {
                GameType = GameType.Client;
            }
        #endif
    }
}
