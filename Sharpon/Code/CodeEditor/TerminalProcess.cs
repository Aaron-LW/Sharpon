using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

public class TerminalProcess
{
    private static Process _process;
    private static bool _logOutput;
    private static string _outputLog;

    public void Start(bool silent = false, string startUpOption = "")
    {
        string shell = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) shell = "/bin/bash";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) shell = "cmd.exe";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) shell = "bin/zsh";

        if (!silent) NotificationManager.CreateNotification("Running shell: " + shell, 7);

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = shell,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        _process.OutputDataReceived += (s, e) => { if (e.Data != null) ProcessOutput(e.Data); };
        _process.ErrorDataReceived += (s, e) => { if (e.Data != null) ProcessOutput(e.Data); };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        
        if (startUpOption != string.Empty) SendCommand(startUpOption);
    }

    public void Stop()
    {
        try
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.Dispose();

                Console.WriteLine("Closed terminal succesfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error when closing terminal: {ex.Message}");
        }
    }
    
    public void Restart()
    {
        _logOutput = true;
        SendCommand("pwd");
        
        Stop();
        Start(true, "cd " + _outputLog);
        _logOutput = false;
    }
    
    public void SendCommand(string command)
    {
        if (_process != null && !_process.HasExited)
        {
            _process.StandardInput.WriteLine(command);
        }
    }
    
    private void ProcessOutput(string line)
    {
        if (_process != null && !_process.HasExited)
        {
            if (_logOutput)
            {
                _outputLog = line;
            }
            
            Terminal.Print(line);
            //NotificationManager.CreateNotification("Message from shell: " + line, 4);
        }
    }
}