using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WindowsSudo
{
    public class ProcessManager
    {
        private MainService main;
        private List<int> processes;

        public ProcessManager(MainService service)
        {
            main = service;
            processes = new List<int>();
        }

        public ProcessCause StartProcess(string workingDir,
            string name,
            string[] args,
            bool newWindow,
            Dictionary<string, string> env)
        {
            string path = Utils.ResolvePath(name, workingDir, Environment.GetEnvironmentVariable("PATH"),
                Environment.GetEnvironmentVariable("PATHEXT"));

            if (path == null)
            {
                Debug.WriteLine("Could not find " + name);
                return ProcessCause.ExecutableNotFound;
            }
            else if (path.Length == 0)
            {
                Debug.WriteLine(name + " is a directory.");
                return ProcessCause.ExecutableIsDirectory;
            }

            try
            {
                ActuallyStart(path, args, workingDir, env);
                return ProcessCause.Success;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Could not start " + name + ": " + e.Message);
                return ProcessCause.ErrorUnk;
            }
        }

        private void ActuallyStart(string path, string[] args, string workingDir, Dictionary<string, string> env)
        {

            ProcessStartInfo startInfo = new ProcessStartInfo(path);
            startInfo.Arguments = String.Join(" ", args);
            startInfo.WorkingDirectory = workingDir;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            foreach (KeyValuePair<string, string> kv in env)
                startInfo.EnvironmentVariables[kv.Key] = kv.Value;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) =>
            {
                processes.Remove(((Process)sender).Id);
            };

            process.Start();

            processes.Add(process.Id);

        }

        public class ProcessInfo
        {

        }

        public enum ProcessCause
        {
            Success,
            ExecutableNotFound,
            ExecutableIsDirectory,
            ErrorUnk,
        }
    }
}
