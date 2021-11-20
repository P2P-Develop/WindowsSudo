using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace WindowsSudo
{
    public class ProcessManager
    {
        public enum ProcessCause
        {
            Success,
            ExecutableNotFound,
            ExecutableIsDirectory,
            ErrorUnk
        }

        private readonly Dictionary<int, ProcessInfo> processes;

        private MainService main;

        public ProcessManager(MainService service)
        {
            main = service;
            processes = new Dictionary<int, ProcessInfo>();
        }

        public ProcessInfo StartProcess(string workingDir,
            string name,
            string[] args,
            bool newWindow,
            Dictionary<string, string> env)
        {
            var path = Utils.ResolvePath(name, workingDir, Environment.GetEnvironmentVariable("PATH"),
                Environment.GetEnvironmentVariable("PATHEXT"));

            if (path == null)
            {
                Debug.WriteLine("Could not find " + name);
                throw new FileNotFoundException("Could not find" + name);
            }

            if (path.Length == 0)
            {
                Debug.WriteLine(name + " is a directory.");
                throw new TypeAccessException(name + " is a directory.");
            }

            try
            {
                return ActuallyStart(path, args, workingDir, env);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Could not start " + name + ": " + e.Message);
                throw new Exception("Unknown");
            }
        }

        private ProcessInfo ActuallyStart(string path, string[] args, string workingDir, Dictionary<string, string> env)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(path);
            startInfo.Arguments = string.Join(" ", args);
            startInfo.WorkingDirectory = workingDir;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            foreach (KeyValuePair<string, string> kv in env)
                startInfo.EnvironmentVariables[kv.Key] = kv.Value;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) => { processes.Remove(((Process)sender).Id); };

            process.Start();

            ProcessInfo info = new ProcessInfo(process.Id, path, workingDir, args, env);

            processes.Add(process.Id, info);

            return info;
        }

        public bool isControlled(int pid)
        {
            return processes.ContainsKey(pid);
        }

        public ProcessInfo GetProcess(int pid)
        {
            if (processes.ContainsKey(pid))
                return processes[pid];
            return null;
        }

        public class ProcessInfo
        {
            public ProcessInfo(int id, string fullPath, string workingDir, string[] args,
                Dictionary<string, string> env)
            {
                Id = id;
                FullPath = fullPath;
                WorkingDir = workingDir;
                Args = args;
                Env = env;
            }

            public int Id { get; set; }

            public string FullPath { get; set; }

            public string WorkingDir { get; set; }

            public string[] Args { get; set; }

            public Dictionary<string, string> Env { get; set; }
        }
    }
}
