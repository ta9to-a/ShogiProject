using System;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class UsiEngineConnector : MonoBehaviour
{
    [SerializeField] bool isPonder;         // 先読みを行うかどうか
    [SerializeField] bool useBook;          // 定跡を使用するかどうか
    [SerializeField] int aiThinkTimeMs;     // AIの思考時間（ミリ秒）
    [SerializeField] int depthLimit;        // 探索深さの制限
    [SerializeField] int nodesLimit;        // ノード数の制限

    private Process _engineProcess;             // エンジンのプロセス
    private StreamWriter _engineStreamWriter;   // エンジンへのコマンド送信用
    private StreamReader _engineStreamReader;   // エンジンからの応答受信用
    
    private Stopwatch _thinkingStopwatch = new(); // 思考時間計測用ストップウォッチ
    
    private TaskCompletionSource<bool> _usiOkReceived = new();       // usiok応答待ち
    private TaskCompletionSource<bool> _readyOkReceived = new();     // readyok応答待ち

    private string _initialBoard; // 初期局面の文字列

    /// <summary>
    /// エンジンの使用を開始する
    /// </summary>
    public async UniTask StartEngin()
    {
        // エンジンのパスを取得
        string enginePath =
            Path.Combine(Application.streamingAssetsPath, "Shogi_Engine", "YaneuraOu_NNUE_halfKP256-V830Git_APPLEM1");
        string engineDirectory = Path.GetDirectoryName(enginePath);
        
        if (engineDirectory == null) // エンジンの実行ファイルが存在しない場合
        {
            Debug.LogError("エンジンの実行ファイルが見つかりません: " + enginePath);
            return;
        }
        
        // エンジンのプロセスを設定
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = enginePath, // エンジンの実行ファイルパス
            WorkingDirectory = engineDirectory, // エンジンのディレクトリ
            
            UseShellExecute = false, // シェルを使用しない
                
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
                    _usiOkReceived.SetResult(true);
                }
                if (engineResponse == "readyok")
                {
                    Debug.Log("Engine > " + engineResponse);
                    _readyOkReceived.SetResult(true);
                }
                if (engineResponse.StartsWith("bestmove"))
                {
                    Debug.Log("Engine > " + engineResponse);
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

        // ストリームの取得
        _engineStreamWriter = _engineProcess.StandardInput;

        SetupUsiEngine();
        await _readyOkReceived.Task;
    }

    /// <summary>
    /// エンジンの初期化
    /// </summary>
    private async void SetupUsiEngine()
    {
        // usiコマンド送信
        SendCommand("usi");
        await _usiOkReceived.Task;
        
        // オプション設定
        SendCommand($"setoption name USI_Ponder value {isPonder}"); // 先読みの設定
        SendCommand($"setoption name USI_OwnBook value {useBook}"); // 定跡の使用設定
        SendCommand($"setoption name DepthLimit value {depthLimit}"); // 探索深さの制限
        SendCommand($"setoption name NodesLimit value {nodesLimit}"); // ノード数の制限
        
        // readyコマンド送信
        SendCommand("isready");
    }
    
    private void OnApplicationQuit()
    {
        StopEngine();
    }
    
    /// <summary>
    /// エンジンの使用を終了する
    /// </summary>
    public void StopEngine()
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
    public void SetStartPosition(string startMassage)
    {
        _initialBoard = startMassage;
        SendCommand("usinewgame");
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
        string positionCommand = $"position {_initialBoard} moves {string.Join(" ", _moveHistory)}";
        Debug.Log(positionCommand);
        
        SendCommand(positionCommand);
        StartThinking();
    }
}