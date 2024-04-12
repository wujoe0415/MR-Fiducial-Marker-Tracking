using System;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;

public class AnacondaStarter : MonoBehaviour
{
    private Process _pythonProcess;
    [Tooltip("Register the target environment name in Anaconda")]
    public string AnacondaEnvironment = "IDVR23";
    private string _arucoDetecterPath = "";
    private string _anacondaPath = "";

    private ConcurrentQueue<string> _terminateAnacondaSignal =
        new ConcurrentQueue<string>();

    private void Awake()
    {
        _arucoDetecterPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\aruco_detection";
        if (!Directory.Exists(_arucoDetecterPath))
        {
            UnityEngine.Debug.LogError(_arucoDetecterPath + " directory not found!");
            return;
        }
        _anacondaPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\anaconda3\\Scripts\\activate.bat";
        if (!File.Exists(_anacondaPath))
        {
            UnityEngine.Debug.LogError(_anacondaPath + " file not found!");
            return;
        }
        StartAnaconda();
        GetComponent<MarkerPredictionReciver>().AddCloseServer(CloseProcess);
    }
    public void StartAnaconda()
    {
        if (_arucoDetecterPath == null)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogError("Fail to find Anaconda path");
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return;
        }

        // Create a process start info
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = System.IO.Path.GetDirectoryName(_arucoDetecterPath)
        };

        // Start the Python process
        Process _pythonProcess = new Process { StartInfo = startInfo };


            _pythonProcess.Exited += (sender, e) =>
            {
                UnityEngine.Debug.Log("CMD process exited");
            };

        // Event handler for process error output
        _pythonProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                UnityEngine.Debug.LogError($"Error: {e.Data}");
            }
        };
        _pythonProcess.Start();
        _pythonProcess.BeginErrorReadLine();

        using (var _sw = _pythonProcess.StandardInput)
        {
            if (_sw.BaseStream.CanWrite)
            {
                _sw.WriteLine(_anacondaPath);
                _sw.WriteLine("activate " + AnacondaEnvironment);
                if (_terminateAnacondaSignal.TryDequeue(out string signal))
                    return;
                _sw.WriteLine("cd " + _arucoDetecterPath);
                _sw.WriteLine("python aruco_detecter.py");
                _sw.Close();
            }

        }
    }

    public void CloseProcess()
    {
        if(_pythonProcess != null && !_pythonProcess.HasExited)
        {
            _pythonProcess.WaitForExit();
        }
    }
}
