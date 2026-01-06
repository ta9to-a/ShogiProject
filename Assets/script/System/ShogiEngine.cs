using System;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Debug = UnityEngine.Debug;

public class ShogiEngine
{
    bool isPonder;         // 先読みを行うかどうか
    bool useBook;          // 定跡を使用するかどうか
    int aiThinkTimeMs;     // AIの思考時間（ミリ秒）
    int depthLimit;        // 探索深さの制限
    int nodesLimit;        // ノード数の制限
    
    private string _initialBoard;
    private List<string> _moveHistory = new ();

    private Process _engineProcess;
    private StreamWriter _engineStreamWriter;
    private StreamReader _engineStreamReader;
    
    private Stopwatch _thinkingStopwatch = new(); // 思考時間計測用ストップウォッチ
    
    private TaskCompletionSource<bool> _usiOkReceived = new();       // usiok応答待ち
    private TaskCompletionSource<bool> _readyOkReceived = new();     // readyok応答待ち
    
    public ShogiEngine() { AppDomain.CurrentDomain.ProcessExit += (_, _) => Stop(); }
    
    public async UniTask Start()
    {
        // エンジンのパスを取得
        var enginePath =
            Path.Combine(Application.streamingAssetsPath, "Shogi_Engine", "YaneuraOu_NNUE_halfKP256-V830Git_APPLEM1");
        var engineDirectory = Path.GetDirectoryName(enginePath);
        
        if (engineDirectory == null) // エンジンの実行ファイルが存在しない場合
        {
            Debug.LogError("エンジンの実行ファイルが見つかりません: " + enginePath);
            return;
        }
        
        // エンジンのプロセスを設定
        var startInfo = new ProcessStartInfo
        {
            FileName = enginePath,
            WorkingDirectory = engineDirectory,
            UseShellExecute = false,       // シェルを使用しない
            RedirectStandardInput = true,  // 送信許可
            RedirectStandardOutput = true, // 受け取り許可
            RedirectStandardError = true,  // エラー出力
            CreateNoWindow = false         // ウィンドウを表示しない
        };

        _engineProcess = new Process { StartInfo = startInfo };
        
        _engineProcess.OutputDataReceived += (_, args) =>
        {
            if (args.Data == null) return;
            
            var engineResponse = args.Data; // エンジンからの応答を取得
            Debug.Log("<color=#93E6B4>Engine</color> > " + engineResponse);
            
            switch (engineResponse)
            {
                case "usiok":
                    _usiOkReceived.SetResult(true);
                    break;
                case "readyok":
                    _readyOkReceived.SetResult(true);
                    break;
            }

            if (engineResponse.StartsWith("bestmove"))
            {
                ParseBestMove(engineResponse);
            }
        };
        _engineProcess.ErrorDataReceived += (_, args) =>
        {
            if (args.Data == null) return;
            Debug.LogError("Engine Error > " + args.Data);
        };
        _engineProcess.Start();
        
        _engineProcess.BeginOutputReadLine(); // 通常時
        _engineProcess.BeginErrorReadLine(); // エラー出力を読み取る
        
        _engineStreamWriter = _engineProcess.StandardInput;

        Setup();
        await _readyOkReceived.Task;
    }
    
    public void Stop()
    {
        if (_engineProcess is not { HasExited: false }) return;
        SendCommand("quit");
        _engineProcess.WaitForExit(1000);
        _engineProcess.Close();
    }
    
    private void SendCommand(string command)
    {
        if (_engineProcess is not { HasExited: false } || _engineStreamWriter == null) return;
        
        _engineStreamWriter.WriteLine(command);
        _engineStreamWriter.Flush();
        Debug.Log("<color=#9FC7D8>Client</color> < " + command);
    }
    
    private async void Setup()
    {
        try
        {
            SendCommand("usi");
            await _usiOkReceived.Task;
        
            SendCommand($"setoption name USI_Ponder value {isPonder}"); // 先読みの設定
            SendCommand($"setoption name USI_OwnBook value {useBook}"); // 定跡の使用設定
            SendCommand($"setoption name DepthLimit value {depthLimit}"); // 探索深さの制限
            SendCommand($"setoption name NodesLimit value {nodesLimit}"); // ノード数の制限
            
            SendCommand("isready");
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
    
    private void CleanUp()
    {
        _engineStreamWriter?.Close();
        _engineStreamReader?.Close();
        _engineProcess?.Close();
        
        _usiOkReceived?.SetResult(false);
        _readyOkReceived?.SetResult(false);
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
        try
        {
            _thinkingStopwatch.Stop();
            var elapsedMilliseconds = _thinkingStopwatch.ElapsedMilliseconds;

            var parts = response.Split(' ');
            if (parts.Length <= 1) return;
            
            var bestMove = parts[1];

            var delayMs = 375 - elapsedMilliseconds;
            if (delayMs > 0)
            {
                await Task.Delay((int)delayMs);
            }
            
            ShogiManager.Instance.ReceiveEngineMove(bestMove);
        }
        catch (Exception e)
        {
            Debug.LogError(" " + e.Message);
        }
    }

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
        var positionCommand = $"position {_initialBoard} moves {string.Join(" ", _moveHistory)}";
        
        SendCommand(positionCommand);
        StartThinking();
    }
}