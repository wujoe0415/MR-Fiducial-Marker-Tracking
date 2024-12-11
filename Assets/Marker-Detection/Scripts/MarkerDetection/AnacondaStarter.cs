using System;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine.Networking.Types;

public class AnacondaStarter : MonoBehaviour
{
    private Process _pythonProcess;
    [Tooltip("Register the target environment name in Anaconda")]
    public string AnacondaEnvironment = "aruco_detection";
    private string _arucoDetecterPath = "";
    private string _anacondaPath = ""; // Default path is "C:\Users\<user>\anaconda3\Scripts\activate.bat"

    private ConcurrentQueue<string> _terminateAnacondaSignal =
        new ConcurrentQueue<string>();

    private void Awake()
    {
        #region PersistentData
        _arucoDetecterPath = Application.persistentDataPath + "\\aruco_detection";
#if UNITY_EDITOR
        if(Directory.Exists(_arucoDetecterPath))
            Directory.Delete(_arucoDetecterPath, true);
        
        string[] files = Directory.GetFiles(Application.dataPath + "\\Resources\\aruco_detection");
        string[] subDirs = Directory.GetDirectories(Application.dataPath + "\\Resources\\aruco_detection");
        string destinationFolderPath = Application.persistentDataPath + "\\aruco_detection";

        if (!Directory.Exists(destinationFolderPath))
            Directory.CreateDirectory(destinationFolderPath);

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string destinationFilePath = Path.Combine(destinationFolderPath, fileName);
            File.Copy(file, destinationFilePath, true); // Set the last parameter to true to overwrite existing files
        }
        foreach (string subDir in subDirs)
        {
            string destSubDir = destinationFolderPath + "\\" + new DirectoryInfo(subDir).Name;
            if (!Directory.Exists(destSubDir))
                Directory.CreateDirectory(destSubDir);

            string[] subFiles = Directory.GetFiles(subDir);
            foreach (string file in subFiles)
            {
                string fileName = Path.GetFileName(file);
                string destinationFilePath = Path.Combine(destSubDir, fileName);
                File.Copy(file, destinationFilePath, true); // Set the last parameter to true to overwrite existing files
            }
        }
        UnityEngine.Debug.Log("Files copied successfully.");
        
#else
            Application.Quit();
#endif

        #endregion
        if (_anacondaPath == "")
            _anacondaPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\anaconda3\\Scripts\\activate.bat"; // Default path
        if (!File.Exists(_anacondaPath))
        {
            UnityEngine.Debug.LogError("Anaconda activate.bat, the " + _anacondaPath + " file was not found!");
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
            UnityEngine.Debug.LogError("Fail to find aruco_detection dictionary!");
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
