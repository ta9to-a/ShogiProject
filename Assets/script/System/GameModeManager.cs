using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    public enum GameMode
    {
        PlayerVsPlayer,
        PlayerVsAI,
        詰将棋
    }
    public GameMode CurrentGameMode { get; private set; } = GameMode.PlayerVsPlayer;
    
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

    public void SetMode(GameMode gameMode)
    {
        CurrentGameMode = gameMode;
        Debug.Log($"Game mode set to: {CurrentGameMode}");
    }
}
