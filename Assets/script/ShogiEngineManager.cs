/*using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;

public class ShogiEngineManager : MonoBehaviour
{
    private Process engineProcess;
    private StreamWriter engineStreamWriter;
    private StreamReader engineStreamReader;
    
    public ShogiManager shogiManager;

    void Start()
    {
        // エンジンのパスを取得
        string enginePath = Path.Combine(Application.streamingAssetsPath, "Shogi_Engine", "YaneuraOu_NNUE_halfKP256-V830Git_APPLEM1");
        string engineDirectory = Path.GetDirectoryName(enginePath);

        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = enginePath, // エンジンの実行ファイルパス
            WorkingDirectory = engineDirectory, // エンジンのディレクトリ
            
            UseShellExecute = false, // 直接制御するようにする
            // 送信設定
            RedirectStandardInput = true,  // 送信許可
            RedirectStandardOutput = true, // 受け取り許可
            RedirectStandardError = true, // エラー出力
            CreateNoWindow = true // ウィンドウを表示しない
        };

        engineProcess = new Process { StartInfo = startInfo };

        engineProcess.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                string engineResponse = args.Data;
        
                if (engineResponse == "usiok")
                {
                    Debug.Log("Engine > " + engineResponse);
                }
                else if (engineResponse == "readyok")
                {
                    Debug.Log("Engine > " + engineResponse);
                }
                else if (engineResponse.StartsWith("bestmove"))
                {
                    Debug.Log("Engine > " + engineResponse);
                    ParseBestMove(engineResponse);
                }
            }
        };
        
        engineProcess.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                Debug.LogError("Engine ERROR > " + args.Data);
            }
        };

        // やねうら王の使用を開始
        engineProcess.Start();
        
        engineProcess.BeginOutputReadLine(); // 通常時
        engineProcess.BeginErrorReadLine(); // エラー出力を読み取る

        engineStreamWriter = engineProcess.StandardInput;

        InitializeEngine();
    }

    // エンジンの使用を開始する
    async void InitializeEngine()
    {
        SendCommand("usi");
        await Task.Delay(1000); // エンジンの応答を待つ
        SendCommand("isready");
    }

    // エンジンにコマンドを送信する
    public void SendCommand(string command)
    {
        if (engineProcess != null && !engineProcess.HasExited && engineStreamWriter != null)
        {
            engineStreamWriter.WriteLine(command);
            engineStreamWriter.Flush();

            if (command == "usi" || command == "isready")
            {
                Debug.Log("Client > " + command);
            }
        }
    }

    // エンジンを終了する
    void OnApplicationQuit()
    {
        if (engineProcess != null && !engineProcess.HasExited)
        {
            SendCommand("quit");
            engineProcess.WaitForExit(1000);
            engineProcess.Close();
        }
    }
    
    //------------------------------
    //-----------対局状況-------------
    //------------------------------

    public void SetStartPosition()
    {
        SendCommand("position startpos");
    }
    
    public void StartThinking(int thinkTimeMs = 1000)
    {
        SendCommand($"go byoyomi {thinkTimeMs}");
    }

    // AIの最善手を解析する
    void ParseBestMove(string response)
    {
        string[] parts = response.Split(' ');
        if (parts.Length > 1)
        {
            string bestMove = parts[1];
            Debug.Log($"{bestMove}");
            
            shogiManager.ReceiveEngineMove(bestMove);
        }
    }

    // ✅ 新しいメソッド：指し手履歴を管理
    private List<string> moveHistory = new List<string>();

    public void AddMoveToHistory(string move)
    {
        moveHistory.Add(move);
        Debug.Log($"📝 Move history: {string.Join(" ", moveHistory)}");
    }

    public void RequestBestMoveWithHistory()
    {
        string positionCommand = "position startpos";
        if (moveHistory.Count > 0)
        {
            positionCommand += " moves " + string.Join(" ", moveHistory);
        }
    
        SendCommand(positionCommand);
        SendCommand("go byoyomi 1000");
    }
}*/