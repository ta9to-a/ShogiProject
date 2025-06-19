using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class ShogiEngineManager : MonoBehaviour
{
    private Process engineProcess;
    private StreamWriter engineStreamWriter;
    private StreamReader engineStreamReader;

    void Start()
    {
        string enginePath = Path.Combine(Application.streamingAssetsPath, "Shogi_Engine", "YaneuraOu_NNUE_halfKP256-V830Git_APPLEM1");
        string engineDirectory = Path.GetDirectoryName(enginePath);

        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = enginePath,
            WorkingDirectory = engineDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        engineProcess = new Process { StartInfo = startInfo };

        engineProcess.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                string engineResponse = args.Data;

                // ▼▼▼ 変更点 ▼▼▼
                // 'usiok' と 'readyok' の応答のみをコンソールに出力する
                if (engineResponse == "usiok" || engineResponse == "readyok")
                {
                    UnityEngine.Debug.Log("Engine > " + engineResponse);
                }
                // ▲▲▲ 変更点 ▲▲▲

                // TODO: ログ出力を抑制していても、ここで'bestmove'などの応答を処理する必要がある
                // 例： if (engineResponse.StartsWith("bestmove")) { ParseBestMove(engineResponse); }
            }
        };
        engineProcess.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                // エラーは常に表示するのが望ましい
                UnityEngine.Debug.LogError("Engine ERROR > " + args.Data);
            }
        };

        engineProcess.Start();
        engineProcess.BeginOutputReadLine();
        engineProcess.BeginErrorReadLine();

        engineStreamWriter = engineProcess.StandardInput;

        InitializeEngine();
    }

    async void InitializeEngine()
    {
        SendCommand("usi");
        await Task.Delay(1000); // エンジンの応答を待つ
        SendCommand("isready");
    }

    public void SendCommand(string command)
    {
        if (engineProcess != null && !engineProcess.HasExited && engineStreamWriter != null)
        {
            engineStreamWriter.WriteLine(command);
            engineStreamWriter.Flush();

            // ▼▼▼ 変更点 ▼▼▼
            // 'usi' と 'isready' コマンドのみをコンソールに出力する
            if (command == "usi" || command == "isready")
            {
                UnityEngine.Debug.Log("Client > " + command);
            }
            // ▲▲▲ 変更点 ▲▲▲
        }
    }

    void OnApplicationQuit()
    {
        if (engineProcess != null && !engineProcess.HasExited)
        {
            SendCommand("quit");
            engineProcess.WaitForExit(1000);
            engineProcess.Close();
        }
    }
}