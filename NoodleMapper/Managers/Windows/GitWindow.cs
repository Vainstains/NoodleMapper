using System.IO;
using NoodleMapper.UI;
using NoodleMapper.UI.Components;
using NoodleMapper.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NoodleMapper.Managers.Windows;

/// <summary>
/// Per-song git window shown in the Song Edit Menu (scene 2).
/// Handles init with .gitignore, remote management, and fetch/pull/push.
/// </summary>
public class GitWindow : GenericWindow<GitWindow>
{
    public override string WindowName => "NoodleMapper Git";

    private string m_gitOutput   = "";
    private bool   m_gitRunning  = false;
    private volatile bool m_rebuildNeeded = false;

    // Persists the URL input text across BuildUI rebuilds.
    private static string s_remoteUrlDraft = "";

    private void Update()
    {
        if (!m_rebuildNeeded) return;
        m_rebuildNeeded = false;
        SetUIDirty();
    }

    private static string? GetSongDirectory() =>
        BeatSaberSongContainer.Instance?.Info?.directory;

    // ─────────────────────────────────────────────
    // BuildUI
    // ─────────────────────────────────────────────

    protected override void BuildUI(RectTransform content)
    {
        SetupScrolling(ref content);
        content.AddSizeFitter(vertical: ContentSizeFitter.FitMode.PreferredSize);
        var layout = content.AddVertical();

        var songDir = GetSongDirectory();
        if (string.IsNullOrEmpty(songDir))
        {
            layout.AddRow(26).AddLabel("No song loaded.", TextAlignmentOptions.Center);
            return;
        }

        // Song directory breadcrumb
        layout.AddRow(18).InsetLeft(4).AddLabel(songDir, TextAlignmentOptions.Left, fontSize: 10,
            color: new Color(0.7f, 0.7f, 0.7f));

        bool isRepo = GitRunner.IsGitRepo(songDir);

        if (!isRepo)
        {
            BuildInitSection(layout, songDir);
        }
        else
        {
            BuildRemoteSection(layout, songDir);
            layout.AddRow(2);
            BuildOpsSection(layout, songDir);
        }

        BuildOutputSection(layout);
    }

    // ─────────────────────────────────────────────
    // Section builders
    // ─────────────────────────────────────────────

    private void BuildInitSection(NoodleVerticalLayout layout, string songDir)
    {
        layout.AddRow(26).AddLabel("Not a git repository.", TextAlignmentOptions.Center, fontSize: 14);

        var row = layout.AddRow(26);
        var (btnRect, _) = row.SplitHorizontal(0.55f);
        btnRect.InsetRight(2).AddButton("Initialize Git Repo", () =>
        {
            if (!m_gitRunning) InitRepo(songDir);
        }).MainColor = new Color(0.2f, 0.5f, 0.3f);
    }

    private void BuildRemoteSection(NoodleVerticalLayout layout, string songDir)
    {
        // Read current origin (fast, local)
        var remoteResult = GitRunner.RunSync(songDir, "remote get-url origin");
        if (remoteResult.Success)
            s_remoteUrlDraft = remoteResult.Output;

        var row        = layout.AddRow(26);
        var controlRect = row.Field("Remote (origin)");
        var (urlRect, btnsRect) = controlRect.SplitHorizontal(1f, bias: -58);

        var urlBox = urlRect.InsetRight(2).AddTextBox().SetValue(s_remoteUrlDraft);
        urlBox.Placeholder = "https://github.com/user/repo.git";
        urlBox.SetOnChange(v => s_remoteUrlDraft = v ?? "");

        var (setRect, clearRect) = btnsRect.SplitHorizontal(0.5f);
        setRect.InsetRight(1).AddButton("Set", () =>
        {
            if (!m_gitRunning) SetRemote(songDir, urlBox.Value);
        }).MainColor = new Color(0.25f, 0.45f, 0.65f);
        clearRect.AddButton("Clr", () =>
        {
            if (!m_gitRunning) ClearRemote(songDir);
        }).MainColor = new Color(0.5f, 0.2f, 0.2f);
    }

    private void BuildOpsSection(NoodleVerticalLayout layout, string songDir)
    {
        var row = layout.AddRow(26);

        var (commitRect, rest) = row.SplitHorizontal(0.25f);
        var (fetchRect, rest2) = rest.SplitHorizontal(0.333f);
        var (pullRect, pushRect) = rest2.SplitHorizontal(0.5f);
        
        // Commit
        commitRect.InsetRight(1).AddButton("Commit", () =>
        {
            if (m_gitRunning) return;

            PersistentUI.Instance.ShowInputBox(
                "Enter a commit message:",
                message =>
                {
                    if (!string.IsNullOrWhiteSpace(message))
                        Commit(songDir, message);
                },
                "");
        }).MainColor = new Color(0.35f, 0.45f, 0.25f);
        
        fetchRect.InsetRight(1).AddButton("Fetch", () =>
        {
            if (!m_gitRunning) RunGitAsync(songDir, "fetch origin");
        }).MainColor = new Color(0.3f, 0.35f, 0.5f);

        pullRect.InsetRight(1).AddButton("Pull", () =>
        {
            if (!m_gitRunning) RunGitAsync(songDir, "pull origin");
        }).MainColor = new Color(0.25f, 0.5f, 0.35f);

        pushRect.AddButton("Push", () =>
            {
                if (m_gitRunning) return;

                if (!HasRemote(songDir))
                {
                    if (GitRunner.HasGh())
                    {
                        PersistentUI.Instance.AskYesNo(
                            "No remote configured",
                            "This repository has no remote.\n\nCreate a GitHub repository and push using gh?",
                            () => CreateGithubRepo(songDir));
                    }
                    else
                    {
                        m_gitOutput = "Error: No remote configured and GitHub CLI (gh) is not installed.";
                        SetUIDirty();
                    }

                    return;
                }

                PersistentUI.Instance.AskYesNo(
                    "Push changes?",
                    "This will push HEAD to origin.",
                    () => RunGitAsync(songDir, "push origin"));
            })
            .MainColor = new Color(0.55f, 0.3f, 0.2f);
    }
    
    private async void CreateGithubRepo(string dir)
    {
        m_gitRunning = true;
        m_gitOutput = "Creating GitHub repository via gh...";
        m_rebuildNeeded = true;

        var result = await GitRunner.RunGhAsync(
            dir,
            "repo create --source=. --remote=origin --push --private --confirm");

        m_gitOutput = result.Success
            ? result.Display
            : $"Error:\n{result.Error}";

        m_gitRunning = false;
        m_rebuildNeeded = true;
    }

    private void BuildOutputSection(NoodleVerticalLayout layout)
    {
        if (string.IsNullOrEmpty(m_gitOutput)) return;

        layout.AddRow(2).AddGetBorder(RectTransform.Edge.Top).Move(0, -1);

        var label = layout.AddRow(72).InsetLeft(4).InsetRight(4)
            .AddLabel(m_gitOutput, TextAlignmentOptions.TopLeft, fontSize: 11);
        label.overflowMode = TextOverflowModes.Overflow;
        label.color = m_gitRunning                           ? new Color(0.85f, 0.85f, 0.4f) :
                      m_gitOutput.StartsWith("Error")        ? new Color(1.0f,  0.4f,  0.4f) :
                                                               Color.white;
    }

    // ─────────────────────────────────────────────
    // Git operations
    // ─────────────────────────────────────────────

    private void InitRepo(string dir)
    {
        var result = GitRunner.RunSync(dir, "init");
        if (!result.Success)
        {
            m_gitOutput = $"Error: {result.Error}";
            SetUIDirty();
            return;
        }
        WriteGitignore(dir);
        GitRunner.RunSync(dir, "add .gitignore");
        m_gitOutput = "Initialized repo and wrote .gitignore.";
        SetUIDirty();
    }

    private static void WriteGitignore(string dir)
    {
        const string content =
            "# ChroMapper autosaves\n"
          + "autosaves/\n"
          + "\n"
          + "# Zip exports\n"
          + "*.zip\n"
          + "\n"
          + "# OS / editor junk\n"
          + ".DS_Store\n"
          + "Thumbs.db\n"
          + "desktop.ini\n";
        File.WriteAllText(Path.Combine(dir, ".gitignore"), content);
    }

    private void SetRemote(string dir, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            m_gitOutput = "Error: URL cannot be empty.";
            SetUIDirty();
            return;
        }
        // try set-url first (origin already exists), fall back to add
        var result = GitRunner.RunSync(dir, $"remote set-url origin {url}");
        if (!result.Success)
            result = GitRunner.RunSync(dir, $"remote add origin {url}");

        m_gitOutput = result.Success ? $"Remote set to:\n{url}" : $"Error: {result.Error}";
        SetUIDirty();
    }

    private void ClearRemote(string dir)
    {
        var result = GitRunner.RunSync(dir, "remote remove origin");
        m_gitOutput = result.Success ? "Remote cleared." : $"Error: {result.Error}";
        s_remoteUrlDraft = "";
        SetUIDirty();
    }

    private async void RunGitAsync(string dir, string args)
    {
        m_gitRunning  = true;
        m_gitOutput   = $"git {args}…";
        m_rebuildNeeded = true;

        var result = await GitRunner.RunAsync(dir, args);

        m_gitOutput = result.Success ? result.Display : $"Error:\n{result.Error}";
        m_gitRunning    = false;
        m_rebuildNeeded = true;
    }
    
    private bool HasGh()
    {
        var result = GitRunner.RunSync("", "gh --version");
        return result.Success;
    }
    
    private bool HasRemote(string dir)
    {
        var result = GitRunner.RunSync(dir, "remote get-url origin");
        return result.Success && !string.IsNullOrWhiteSpace(result.Output);
    }
    
    private async void Commit(string dir, string message)
    {
        m_gitRunning = true;
        m_gitOutput = "Staging files...";
        m_rebuildNeeded = true;

        var addResult = await GitRunner.RunAsync(dir, "add -A");

        if (!addResult.Success)
        {
            m_gitOutput = $"Error staging files:\n{addResult.Error}";
            m_gitRunning = false;
            m_rebuildNeeded = true;
            return;
        }

        m_gitOutput = "Creating commit...";
        m_rebuildNeeded = true;

        var commitResult = await GitRunner.RunAsync(dir, $"commit -m \"{message}\"");

        m_gitOutput = commitResult.Success
            ? commitResult.Display
            : $"Error:\n{commitResult.Error}";

        m_gitRunning = false;
        m_rebuildNeeded = true;
    }
}