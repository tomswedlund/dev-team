using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevTeam.Core.Interfaces;

/// <summary>
/// Manages Git repository operations — branch creation, file commits,
/// and pull request submission. This abstraction allows the orchestrator
/// to interact with any Git hosting platform (GitHub, GitLab, local
/// repos, etc.) through a single interface.
/// </summary>
public interface IRepoManager
{
    /// <summary>
    /// Creates a new branch in the repository with a specified base.
    /// Used when starting work on a new task or phase.
    /// </summary>
    /// <param name="branchName">Name of the new branch (e.g. "phase-a/task-A01").</param>
    /// <param name="baseBranch">Name of the branch to branch from (e.g. "main").</param>
    /// <returns>A task that completes when the branch has been created.</returns>
    Task CreateBranchAsync(string branchName, string baseBranch);

    /// <summary>
    /// Stages and commits the specified files to the current branch
    /// with a structured commit message.
    /// </summary>
    /// <param name="files">Collection of file paths and their contents to commit.</param>
    /// <param name="message">Commit message (should include task ID and description).</param>
    /// <returns>A task that completes when the commit has been made.</returns>
    Task CommitFilesAsync(IEnumerable<(string path, string content)> files, string message);

    /// <summary>
    /// Creates a pull request from the head branch to the base branch
    /// with a title and descriptive body.
    /// </summary>
    /// <param name="head">The source branch for the PR.</param>
    /// <param name="baseBranch">The target branch for the PR.</param>
    /// <param name="title">PR title.</param>
    /// <param name="body">PR description (should summarize all commits in the phase).</param>
    /// <returns>A task that completes when the PR has been created.</returns>
    Task CreatePullRequestAsync(string head, string baseBranch, string title, string body);
}