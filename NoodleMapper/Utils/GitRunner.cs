using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace NoodleMapper.Utils;

public static class GitRunner
{
    public class GitResult
    {
        public bool Success;
        public string Output = "";
        public string Error  = "";

        public string Display =>
            !string.IsNullOrEmpty(Output) ? Output :
            !string.IsNullOrEmpty(Error)  ? Error  :
            "(no output)";
    }

    // ─────────────────────────────────────────────
    // Generic command runner
    // ─────────────────────────────────────────────

    public static Task<GitResult> RunCommandAsync(string exe, string workingDir, string args) =>
        Task.Run(() => RunCommandSync(exe, workingDir, args));

    public static GitResult RunCommandSync(string exe, string workingDir, string args)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute = false,
            CreateNoWindow  = true
        };

        try
        {
            using var proc = Process.Start(psi)!;

            string output = proc.StandardOutput.ReadToEnd();
            string error  = proc.StandardError.ReadToEnd();

            proc.WaitForExit();

            return new GitResult
            {
                Success = proc.ExitCode == 0,
                Output  = output.Trim(),
                Error   = error.Trim()
            };
        }
        catch (Exception e)
        {
            return new GitResult
            {
                Success = false,
                Error = $"Failed to run {exe}: {e.Message}"
            };
        }
    }

    // ─────────────────────────────────────────────
    // Git helpers (existing API stays intact)
    // ─────────────────────────────────────────────

    public static Task<GitResult> RunAsync(string workingDir, string arguments) =>
        RunCommandAsync("git", workingDir, arguments);

    public static GitResult RunSync(string workingDir, string arguments) =>
        RunCommandSync("git", workingDir, arguments);

    // ─────────────────────────────────────────────
    // gh helpers
    // ─────────────────────────────────────────────

    public static Task<GitResult> RunGhAsync(string workingDir, string arguments) =>
        RunCommandAsync("gh", workingDir, arguments);

    public static GitResult RunGhSync(string workingDir, string arguments) =>
        RunCommandSync("gh", workingDir, arguments);

    public static bool HasGh()
    {
        var result = RunGhSync(Environment.CurrentDirectory, "--version");
        return result.Success;
    }

    public static bool IsGitRepo(string dir) =>
        Directory.Exists(Path.Combine(dir, ".git"));
}