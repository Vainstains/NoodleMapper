using System.Collections.Generic;
using System.IO;
using NoodleMapper.UI;
using NoodleMapper.UI.Components;
using NoodleMapper.Utils;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NoodleMapper.Managers.Windows;

/// <summary>
/// Simple window for cloning a Beat Saber map repo from a Git URL into CustomWIPLevels.
/// Lives on the Song Select screen (scene 1) and does not require a song to be loaded.
/// </summary>
public class GitCloneWindow : GenericWindow<GitCloneWindow>
{
    public override string WindowName => "Clone Map from Git";

    private string m_output      = "";
    private bool   m_running     = false;
    private volatile bool m_rebuildNeeded = false;

    // Preserve URL across rebuilds so the user doesn't lose their typing
    private static string s_lastUrl = "";
    
    private bool m_loadingRepos = false;
    private List<(string name, string cloneUrl)> m_maps = new();

    private void Update()
    {
        if (!m_rebuildNeeded) return;
        m_rebuildNeeded = false;
        SetUIDirty();
    }

    // ─────────────────────────────────────────────
    // BuildUI
    // ─────────────────────────────────────────────

    protected override void BuildUI(RectTransform content)
    {
        SetupScrolling(ref content);
        content.AddSizeFitter(vertical: ContentSizeFitter.FitMode.PreferredSize);
        var layout = content.AddVertical();

        var wipDir = global::Settings.Instance.CustomWIPSongsFolder;

        // Destination breadcrumb
        layout.AddRow(18).InsetLeft(4)
            .AddLabel($"→ {wipDir}", TextAlignmentOptions.Left, fontSize: 10,
                color: new Color(0.7f, 0.7f, 0.7f));
        
        if (GitRunner.HasGh() && !m_loadingRepos && m_maps.Count == 0)
        {
            LoadUserMaps();
        }
        if (GitRunner.HasGh())
        {
            layout.AddRow(20)
                .AddLabel("Your GitHub map repos", TextAlignmentOptions.Left, fontSize: 12);

            if (m_loadingRepos)
            {
                layout.AddRow(20)
                    .AddLabel("Loading…", TextAlignmentOptions.Left);
            }
            else
            {
                foreach (var map in m_maps)
                {
                    var row = layout.AddRow(22);
                    row.AddButton(map.name, () =>
                    {
                        if (!m_running)
                            CloneRepo(map.cloneUrl, wipDir);
                    }).MainColor = new Color(0.25f, 0.4f, 0.55f);
                }

                if (m_maps.Count == 0)
                {
                    layout.AddRow(18)
                        .AddLabel("(no repos with Info.dat found)", TextAlignmentOptions.Left, fontSize: 10);
                }
            }

            layout.AddRow(6);
        }

        // URL input row
        var urlRow     = layout.AddRow(26);
        var urlControl = urlRow.Field("Git URL");
        var urlBox     = urlControl.InsetRight(2).AddTextBox().SetValue(s_lastUrl);
        urlBox.Placeholder = "https://github.com/user/map-repo.git";
        urlBox.SetOnChange(v => s_lastUrl = v ?? "");

        layout.AddRow(4); // spacing

        // Clone button + status indicator
        var btnRow = layout.AddRow(26);
        var (btnRect, statusRect) = btnRow.SplitHorizontal(0.5f);

        btnRect.InsetRight(2).AddButton(
            m_running ? "Cloning…" : "Clone to WIP Levels",
            () => { if (!m_running) CloneRepo(urlBox.Value, wipDir); }
        ).MainColor = m_running
            ? new Color(0.3f, 0.3f, 0.3f)
            : new Color(0.2f, 0.5f, 0.3f);

        if (m_running)
        {
            statusRect.InsetLeft(6)
                .AddLabel("(check output below)", TextAlignmentOptions.Left, fontSize: 11,
                    color: new Color(0.75f, 0.75f, 0.4f));
        }

        // Output log
        if (!string.IsNullOrEmpty(m_output))
        {
            layout.AddRow(2).AddGetBorder(RectTransform.Edge.Top).Move(0, -1);
            var label = layout.AddRow(80).InsetLeft(4).InsetRight(4)
                .AddLabel(m_output, TextAlignmentOptions.TopLeft, fontSize: 11);
            label.overflowMode = TextOverflowModes.Overflow;
            label.color = m_running                      ? new Color(0.85f, 0.85f, 0.4f) :
                          m_output.StartsWith("Error")   ? new Color(1.0f,  0.4f,  0.4f) :
                                                           Color.white;
        }
    }
    
    private async void LoadUserMaps()
{
    Debug.Log("[GitCloneWindow] Loading user GitHub map repos...");

    m_loadingRepos = true;
    m_rebuildNeeded = true;

    // ─────────────────────────
    // Get logged in username
    // ─────────────────────────
    Debug.Log("[GitCloneWindow] Fetching logged-in GitHub user...");

    var userResult = await GitRunner.RunGhAsync("", "api user");

    if (!userResult.Success)
    {
        Debug.LogError($"[GitCloneWindow] Failed to get GitHub user:\n{userResult.Error}");
        m_loadingRepos = false;
        m_rebuildNeeded = true;
        return;
    }

    var userJson = JSON.Parse(userResult.Output);
    var username = userJson?["login"]?.Value;

    Debug.Log($"[GitCloneWindow] Logged in as: {username}");

    if (string.IsNullOrEmpty(username))
    {
        Debug.LogError("[GitCloneWindow] Username was null or empty.");
        m_loadingRepos = false;
        m_rebuildNeeded = true;
        return;
    }

    // ─────────────────────────
    // Get repo list
    // ─────────────────────────
    Debug.Log("[GitCloneWindow] Fetching repo list...");

    var repoResult = await GitRunner.RunGhAsync(
        "",
        $"repo list {username} --limit 1000 --json name"
    );

    if (!repoResult.Success)
    {
        Debug.LogError($"[GitCloneWindow] Failed to list repos:\n{repoResult.Error}");
        m_loadingRepos = false;
        m_rebuildNeeded = true;
        return;
    }

    var reposJson = JSON.Parse(repoResult.Output);

    Debug.Log($"[GitCloneWindow] Found {reposJson.Count} repos.");

    // ─────────────────────────
    // Check each repo
    // ─────────────────────────
    foreach (var repoNode in reposJson.AsArray)
    {
        var repoName = repoNode.Value["name"]?.Value;

        if (string.IsNullOrEmpty(repoName))
        {
            Debug.LogWarning("[GitCloneWindow] Encountered repo with missing name.");
            continue;
        }

        Debug.Log($"[GitCloneWindow] Checking repo: {repoName}");

        // Get repo contents
        var contentsResult = await GitRunner.RunGhAsync(
            "",
            $"api repos/{username}/{repoName}/contents"
        );

        if (!contentsResult.Success)
        {
            Debug.LogWarning($"[GitCloneWindow] Failed to fetch contents for {repoName}.");
            continue;
        }

        var contentsJson = JSON.Parse(contentsResult.Output);

        bool hasInfoDat = false;

        foreach (var fileNode in contentsJson.AsArray)
        {
            var fileName = fileNode.Value["name"]?.Value;

            if (fileName == "Info.dat")
            {
                hasInfoDat = true;
                break;
            }
        }

        if (!hasInfoDat)
        {
            Debug.Log($"[GitCloneWindow] {repoName} skipped (no Info.dat)");
            continue;
        }

        Debug.Log($"[GitCloneWindow] {repoName} contains Info.dat, fetching clone URL...");

        // Get repo info for clone URL
        var repoInfo = await GitRunner.RunGhAsync(
            "",
            $"api repos/{username}/{repoName}"
        );

        if (!repoInfo.Success)
        {
            Debug.LogWarning($"[GitCloneWindow] Failed to fetch repo info for {repoName}");
            continue;
        }

        var repoJson = JSON.Parse(repoInfo.Output);
        var cloneUrl = repoJson?["clone_url"]?.Value;

        if (string.IsNullOrEmpty(cloneUrl))
        {
            Debug.LogWarning($"[GitCloneWindow] Clone URL missing for {repoName}");
            continue;
        }

        Debug.Log($"[GitCloneWindow] Map repo detected: {repoName}");

        m_maps.Add((repoName, cloneUrl));
        m_rebuildNeeded = true;
    }

    Debug.Log($"[GitCloneWindow] Finished scanning repos. Found {m_maps.Count} map repos.");

    m_loadingRepos = false;
    m_rebuildNeeded = true;
}

    // ─────────────────────────────────────────────
    // Clone
    // ─────────────────────────────────────────────

    private async void CloneRepo(string url, string targetDir)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            m_output = "Error: URL cannot be empty.";
            SetUIDirty();
            return;
        }
        if (!Directory.Exists(targetDir))
        {
            m_output = $"Error: WIP Levels folder not found:\n{targetDir}";
            SetUIDirty();
            return;
        }

        m_running       = true;
        m_output        = "Cloning…";
        m_rebuildNeeded = true;

        var result = await GitRunner.RunAsync(targetDir, $"clone {url}");

        if (result.Success)
        {
            // Best-effort: derive the folder name git would have used.
            var repoName = Path.GetFileNameWithoutExtension(url.TrimEnd('/'));
            if (repoName.EndsWith(".git"))
                repoName = repoName.Substring(0, repoName.Length - ".git".Length);
            m_output = $"Done!\nCloned into: {Path.Combine(targetDir, repoName)}";
        }
        else
        {
            m_output = $"Error:\n{result.Error}";
        }

        m_running       = false;
        m_rebuildNeeded = true;
    }
}