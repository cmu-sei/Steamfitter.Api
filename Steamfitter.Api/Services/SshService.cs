// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using STT = System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using Steamfitter.Api.Infrastructure.Options;

namespace Steamfitter.Api.Services
{
    public interface ISshService
    {
        STT.Task<string> SendLinuxRemoteCommand(string parameters);
        STT.Task<string> LinuxFileTouch(string parameters);
        STT.Task<string> LinuxRm(string parameters);
    }

    public class SshService : ISshService
    {
        private readonly ILogger<SshService> _logger;
        private readonly IOptionsMonitor<SshOptions> _sshOptions;

        public SshService(
            ILogger<SshService> logger,
            IOptionsMonitor<SshOptions> sshOptions)
        {
            _logger = logger;
            _sshOptions = sshOptions;
        }

        public STT.Task<string> SendLinuxRemoteCommand(string parameters)
        {
            var p = JsonSerializer.Deserialize<RemoteCommandParameters>(parameters);
            return RunOnHostsAsync(p.Hosts, p.Port, p.Username, p.PrivateKey, BuildRemoteCommand(p));
        }

        public STT.Task<string> LinuxFileTouch(string parameters)
        {
            var p = JsonSerializer.Deserialize<LinuxFileTouchParameters>(parameters);
            var sudo = ParseBool(p.Sudo);
            var command = BuildShellCommand(
                cwd: p.Cwd,
                env: p.Env,
                sudo: sudo,
                cmd: $"touch {ShellQuote(p.File)}");
            return RunOnHostsAsync(p.Hosts, p.Port, p.Username, p.PrivateKey, command);
        }

        public STT.Task<string> LinuxRm(string parameters)
        {
            var p = JsonSerializer.Deserialize<LinuxRmParameters>(parameters);
            var sudo = ParseBool(p.Sudo);
            var recursive = ParseBool(p.Recursive);
            var force = ParseBool(p.Force);

            var rmFlags = new StringBuilder("rm");
            if (recursive) rmFlags.Append(" -r");
            if (force) rmFlags.Append(" -f");
            if (!string.IsNullOrEmpty(p.Args)) rmFlags.Append(' ').Append(p.Args);

            var command = BuildShellCommand(
                cwd: p.Cwd,
                env: p.Env,
                sudo: sudo,
                cmd: $"{rmFlags} {ShellQuote(p.Target)}");
            return RunOnHostsAsync(p.Hosts, p.Port, p.Username, p.PrivateKey, command);
        }

        private async STT.Task<string> RunOnHostsAsync(string hostsCsv, string portStr, string username, string privateKeyOverride, string command)
        {
            if (string.IsNullOrWhiteSpace(hostsCsv))
                throw new ArgumentException("Hosts is required.");
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username is required.");

            var port = ParsePort(portStr);
            var keyFile = ResolvePrivateKey(privateKeyOverride);
            var hosts = hostsCsv.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var output = new StringBuilder();

            foreach (var host in hosts)
            {
                output.AppendLine($"[{host}]");
                try
                {
                    using var client = new SshClient(host.Trim(), port, username, keyFile);
                    client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(_sshOptions.CurrentValue.CommandTimeoutSeconds);
                    await STT.Task.Run(() => client.Connect());

                    using var cmd = client.CreateCommand(command);
                    cmd.CommandTimeout = TimeSpan.FromSeconds(_sshOptions.CurrentValue.CommandTimeoutSeconds);
                    var result = await STT.Task.Run(() => cmd.Execute());

                    output.AppendLine(result);
                    if (cmd.ExitStatus != 0)
                        output.AppendLine($"(exit {cmd.ExitStatus}) {cmd.Error}");

                    client.Disconnect();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SSH command failed on host {host}", host);
                    output.AppendLine($"(error) {ex.Message}");
                }
            }

            return output.ToString();
        }

        private PrivateKeyFile ResolvePrivateKey(string overrideKey)
        {
            var key = string.IsNullOrWhiteSpace(overrideKey) ? _sshOptions.CurrentValue.DefaultPrivateKey : overrideKey;
            var keyPath = _sshOptions.CurrentValue.DefaultPrivateKeyPath;
            var passphrase = _sshOptions.CurrentValue.DefaultPrivateKeyPassphrase;

            if (!string.IsNullOrEmpty(key))
            {
                var bytes = Encoding.UTF8.GetBytes(key);
                using var ms = new MemoryStream(bytes);
                return string.IsNullOrEmpty(passphrase) ? new PrivateKeyFile(ms) : new PrivateKeyFile(ms, passphrase);
            }

            if (!string.IsNullOrEmpty(keyPath))
            {
                return string.IsNullOrEmpty(passphrase) ? new PrivateKeyFile(keyPath) : new PrivateKeyFile(keyPath, passphrase);
            }

            throw new InvalidOperationException("No SSH private key configured. Set Ssh:DefaultPrivateKey or Ssh:DefaultPrivateKeyPath, or pass PrivateKey in task parameters.");
        }

        private static int ParsePort(string portStr) =>
            int.TryParse(portStr, out var p) && p > 0 ? p : 22;

        private static bool ParseBool(string s) =>
            !string.IsNullOrEmpty(s) && string.Equals(s.Trim(), "true", StringComparison.OrdinalIgnoreCase);

        private static string ShellQuote(string s)
        {
            if (string.IsNullOrEmpty(s)) return "''";
            return "'" + s.Replace("'", "'\\''") + "'";
        }

        private static string BuildRemoteCommand(RemoteCommandParameters p) =>
            BuildShellCommand(p.Cwd, p.Env, sudo: false, cmd: p.Cmd);

        private static string BuildShellCommand(string cwd, string env, bool sudo, string cmd)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(env) && env.Trim() != "{}")
            {
                // Accept either KEY=val,KEY2=val2 or JSON-like {"KEY":"val"}
                var pairs = ParseEnvPairs(env);
                foreach (var (k, v) in pairs)
                    sb.Append(k).Append('=').Append(ShellQuote(v)).Append(' ');
            }

            if (!string.IsNullOrWhiteSpace(cwd))
                sb.Append("cd ").Append(ShellQuote(cwd)).Append(" && ");

            if (sudo) sb.Append("sudo ");
            sb.Append(cmd);
            return sb.ToString();
        }

        private static System.Collections.Generic.IEnumerable<(string Key, string Value)> ParseEnvPairs(string env)
        {
            env = env.Trim();
            if (env.StartsWith("{") && env.EndsWith("}"))
            {
                using var doc = JsonDocument.Parse(env);
                foreach (var prop in doc.RootElement.EnumerateObject())
                    yield return (prop.Name, prop.Value.ToString());
            }
            else
            {
                foreach (var pair in env.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var idx = pair.IndexOf('=');
                    if (idx <= 0) continue;
                    yield return (pair.Substring(0, idx).Trim(), pair.Substring(idx + 1).Trim());
                }
            }
        }

        // ---- Parameter DTOs (mirror legacy DTO field names) ----

        private class RemoteCommandParameters
        {
            public string Hosts { get; set; }
            public string Port { get; set; }
            public string Username { get; set; }
            public string PrivateKey { get; set; }
            public string Cmd { get; set; }
            public string Cwd { get; set; }
            public string Env { get; set; }
        }

        private class LinuxFileTouchParameters
        {
            public string Hosts { get; set; }
            public string Port { get; set; }
            public string Username { get; set; }
            public string PrivateKey { get; set; }
            public string File { get; set; }
            public string Cwd { get; set; }
            public string Env { get; set; }
            public string Sudo { get; set; }
        }

        private class LinuxRmParameters
        {
            public string Hosts { get; set; }
            public string Port { get; set; }
            public string Username { get; set; }
            public string PrivateKey { get; set; }
            public string Target { get; set; }
            public string Cwd { get; set; }
            public string Env { get; set; }
            public string Args { get; set; }
            public string Sudo { get; set; }
            public string Recursive { get; set; }
            public string Force { get; set; }
        }
    }
}
