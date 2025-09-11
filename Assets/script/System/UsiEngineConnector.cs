using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;

public class UsiEngineConnector : MonoBehaviour
{
    public Turn EngineTurn {get; private set;}  // エンジンのターン（先手 or 後手）
    
    [SerializeField] int aiThinkTimeMs;     // AIの思考時間（ミリ秒）
    [SerializeField] int depthLimit;        // 探索深さの制限
    [SerializeField] int nodesLimit;        // ノード数の制限

    private Process _engineProcess;             // エンジンのプロセス
    private StreamWriter _engineStreamWriter;   // エンジンへのコマンド送信用
    private StreamReader _engineStreamReader;   // エンジンからの応答受信用
    
    private Stopwatch _thinkingStopwatch;

    /// <summary>
    /// エンジンの使用を開始する
    /// </summary>
    public void StartEngin()
    {
        _thinkingStopwatch = new Stopwatch();

        // エンジンのパスを取得
        string enginePath =
            Path.Combine(Application.streamingAssetsPath, "Shogi_Engine", "PLEM1raOu_NNUE_halfKP256-V830Git_APPLEM1");
        string engineDirectory = Path.GetDirectoryName(enginePath);
        Debug.Log(engineDirectory);
        
        if (engineDirectory != null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
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
        }

        _engineProcess.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                string engineResponse = args.Data;
                
                if (engineResponse == "usiok" || engineResponse == "readyok") Debug.Log("Engine > " + engineResponse);
                else if (engineResponse.StartsWith("bestmove")) ParseBestMove(engineResponse);
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

        // ストリームの取得
        _engineStreamWriter = _engineProcess.StandardInput;

        InitializeEngine();
    }

    /// <summary>
    /// エンジンの初期化
    /// </summary>
    private async void InitializeEngine()
    {
        SendCommand("usi");
        await Task.Delay(1000); // エンジンの応答を待つ
        
        SendCommand("setoption name Depth value " + depthLimit);
        SendCommand("setoption name Nodes value " + nodesLimit);
        
        Debug.Log("読み手: " + (depthLimit > 0 ? depthLimit + "手" : "無制限"));
        Debug.Log("ノード数制限: " + (nodesLimit > 0 ? nodesLimit.ToString() : "無制限"));
        Debug.Log("AI思考時間: " + aiThinkTimeMs + "ms");
        
        SendCommand("isready");
    }
    
    /// <summary>
    /// エンジンの使用を終了する
    /// </summary>
    private void OnApplicationQuit()
    {
        if (_engineProcess != null && !_engineProcess.HasExited)
        {
            SendCommand("quit");
            _engineProcess.WaitForExit(1000);
            _engineProcess.Close();
        }
    }
    
    /// <summary>
    /// エンジンにコマンドを送信する
    /// </summary>
    private void SendCommand(string command)
    {
        if (_engineProcess == null)
        {
            Debug.LogError("エンジンプロセスが初期化されていません。");
        }
        else if (_engineProcess.HasExited)
        {
            Debug.LogError("エンジンプロセスが終了しています。");
        }
        else if (_engineStreamWriter == null)
        {
            Debug.LogError("エンジンへのストリームが初期化されていません。");
        }
        
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

    /// <summary>
    /// 初期局面を設定
    /// </summary>
    public void SetStartPosition()
    {
        SendCommand("position startpos");
    }
    
    /// <summary>
    /// AIの思考を開始する
    /// </summary>
    private void StartThinking(int timeMs = -1)
    {
        _thinkingStopwatch.Restart();
        int actualThinkTime = timeMs == -1 ? aiThinkTimeMs : timeMs;
        SendCommand($"go byoyomi {actualThinkTime}");
    }

    /// <summary>
    /// エンジンからのbestmove応答を解析し、指し手を取得する
    /// </summary>
    private async void ParseBestMove(string response)
    {
        _thinkingStopwatch.Stop();
        long elapsedMilliseconds = _thinkingStopwatch.ElapsedMilliseconds;

        string[] parts = response.Split(' ');
        if (parts.Length > 1)
        {
            string bestMove = parts[1];

            long delayMs = 375 - elapsedMilliseconds;
            if (delayMs > 0)
            {
                await Task.Delay((int)delayMs);
            }
            
            ShogiManager.Instance.ReceiveEngineMove(bestMove);
        }
    }

    //指し手履歴を管理
    private List<string> _moveHistory = new ();

    /// <summary>
    /// 指し手履歴に手を追加する
    /// </summary>
    public void AddMoveToHistory(string move)
    {
        _moveHistory.Add(move);
    }

    /// <summary>
    /// 指し手履歴に最後の手を追加する
    /// </summary>
    public void RequestBestMoveWithHistory()
    {
        string positionCommand = "position startpos";
        if (_moveHistory.Count > 0)
        {
            positionCommand += " moves " + string.Join(" ", _moveHistory);
        }
    
        SendCommand(positionCommand);
        StartThinking();
    }
}