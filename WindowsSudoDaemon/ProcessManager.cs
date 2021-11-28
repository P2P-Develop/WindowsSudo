using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Authentication;

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
            Dictionary<string, string> env,
            string username,
            string password,
            string domain)
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


            // Check credential
            if (username != null)
            {
                if (CredentialHelper.ACAvailable())
                    if (CredentialHelper.DomainExist(domain))
                    {
                        Debug.WriteLine("Could not find domain " + domain);
                        throw new CredentialHelper.Exceptions.DomainNotFoundException("Could not find domain " + domain);
                    }

                if (!CredentialHelper.UserExists(username))
                {
                    Debug.WriteLine("Could not find user " + username);
                    throw new CredentialHelper.Exceptions.UserNotFoundException("Could not find user " + username);
                }

                if (password == null)
                {
                    // TODO: Password caching
                    Debug.WriteLine("Password is null");
                    throw new CredentialHelper.Exceptions.BadPasswordException("Password is null");
                }

                if (!CredentialHelper.ValidateAccount(username, password, domain))
                {
                    Debug.WriteLine("Could not check credential");
                    throw new CredentialHelper.Exceptions.BadPasswordException("Could not check credential");
                }
            }

            try
            {
                return ActuallyStart(path, args, workingDir, env, username, password, domain);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Could not start " + name + ": " + e.Message);
                throw new Exception("Unknown");
            }
        }

        private ProcessInfo ActuallyStart(string path, string[] args, string workingDir, Dictionary<string, string> env,
            string username, string password, string domain)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(path);

            startInfo.Arguments = Utils.escapeArgsAsString(args);
            startInfo.WorkingDirectory = workingDir;
            startInfo.UseShellExecute = false;

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            foreach (KeyValuePair<string, string> kv in env)
                startInfo.EnvironmentVariables[kv.Key] = kv.Value;

            if (username != null)
            {
                startInfo.UserName = username;
                startInfo.Password = new NetworkCredential("", password).SecurePassword;
                startInfo.Domain = domain;
                startInfo.LoadUserProfile = true;
            }

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
