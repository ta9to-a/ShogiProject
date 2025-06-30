using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;

public class ShogiEngineManager : MonoBehaviour
{
    private Process _engineProcess;
    private StreamWriter _engineStreamWriter;
    private StreamReader _engineStreamReader;
    
    [SerializeField] private int aiThinkTimeMs = 3000;
    
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

        _engineProcess = new Process { StartInfo = startInfo };
        _engineProcess.OutputDataReceived += (sender, args) =>
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
                    ShogiManager.CanSelect = true;
                }
                else if (engineResponse.StartsWith("bestmove"))
                {
                    ParseBestMove(engineResponse);
                }
            }
        };
        
        _engineProcess.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                Debug.LogError("Engine ERROR > " + args.Data);
            }
        };

        // やねうら王の使用を開始
        _engineProcess.Start();
        
        _engineProcess.BeginOutputReadLine(); // 通常時
        _engineProcess.BeginErrorReadLine(); // エラー出力を読み取る

        _engineStreamWriter = _engineProcess.StandardInput;

        InitializeEngine();
    }

    //-----エンジンの使用を開始する-----
    async void InitializeEngine()
    {
        SendCommand("usi");
        await Task.Delay(1000); // エンジンの応答を待つ
        SendCommand("isready");
    }

    //-----エンジンを終了する-----
    void OnApplicationQuit()
    {
        if (_engineProcess != null && !_engineProcess.HasExited)
        {
            SendCommand("quit");
            _engineProcess.WaitForExit(1000);
            _engineProcess.Close();
        }
    }
    
    //-----エンジンにコマンドを送信する-----
    public void SendCommand(string command)
    {
        if (_engineProcess != null && !_engineProcess.HasExited && _engineStreamWriter != null)
        {
            _engineStreamWriter.WriteLine(command);
            _engineStreamWriter.Flush();

            if (command == "usi" || command == "isready")
            {
                Debug.Log("Client > " + command);
            }
        }
    }
    
    //------------------------------
    //-----------対局状況-------------
    //------------------------------

    public void SetStartPosition()
    {
        SendCommand("position startpos");
    }
    
    public void StartThinking(int thinkTimeMs = -1)
    {
        int actualThinkTime = (thinkTimeMs == -1) ? aiThinkTimeMs : thinkTimeMs;
        SendCommand($"go byoyomi {actualThinkTime}");
    }

    // AIの最善手を解析する
    void ParseBestMove(string response)
    {
        Debug.Log("Engine > " + response);
        string[] parts = response.Split(' ');
        if (parts.Length > 1)
        {
            string bestMove = parts[1];
            
            string objectTag = ShogiManager.ActivePlayer? "☗" : "☖";
            
            Debug.Log(objectTag + " " + bestMove);
            shogiManager.ReceiveEngineMove(bestMove);
        }
    }

    //指し手履歴を管理
    List<string> _moveHistory = new ();

    public void AddMoveToHistory(string move)
    {
        _moveHistory.Add(move);
    }

    public void RequestBestMoveWithHistory()
    {
        string positionCommand = "position startpos";
        if (_moveHistory.Count > 0)
        {
            positionCommand += " moves " + string.Join(" ", _moveHistory);
        }
    
        SendCommand(positionCommand);
        SendCommand("go byoyomi " + aiThinkTimeMs);
       
        Debug.Log("positionCommand :" + positionCommand);
    }
}