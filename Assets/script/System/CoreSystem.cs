using UnityEngine;

public class CoreSystem : MonoBehaviour
{
    public static CoreSystem Instance { get; private set; }

    public GameMode selectGameMode = GameMode.PlayerVsPlayer;
    public enum GameMode
    {
        PlayerVsPlayer,
        PlayerVsAI,
        詰将棋
    }
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetMode(selectGameMode);
    }

    public void SetMode(GameMode gameMode)
    {
        Debug.Log("Game Mode : " + gameMode);
        
        var shogiManager = gameObject.AddComponent<ShogiManager>();
        shogiManager.PrepareMatch(gameMode);
    }
}
