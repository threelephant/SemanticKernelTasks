using LibGit2Sharp;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using LibGit2Sharp.Handlers;

namespace SemanticKernelPlayground.Plugins;

/// <summary>
/// A single SK plugin that exposes Git + semver helpers to the LLM.
/// </summary>
public class GitPlugin
{
    private Repository? _repo;
    private const string _verFile = "Data/version.json";

    // ─────────────────────────────────────────────
    // Repo bootstrap
    // ─────────────────────────────────────────────
    [KernelFunction, Description("Set the working repository path")]
    public string SetRepo(
        [Description("Absolute path to a local Git repository")] string path)
    {
        if (!Repository.IsValid(path))
            throw new ArgumentException("Not a valid git repo");

        _repo = new Repository(path);
        return $"Repo set to {path}";
    }

    // ─────────────────────────────────────────────
    // Commit helpers
    // ─────────────────────────────────────────────
    [KernelFunction, Description("Get latest commits as JSON")]
    public string GetCommits(
        [Description("How many commits to retrieve")] int count = 10)
    {
        EnsureRepo();
        var commits = _repo!.Commits.Take(count).Select(c => new
        {
            sha = c.Sha[..7],
            message = c.MessageShort,
            author = c.Author.Name,
            dateUtc = c.Author.When.UtcDateTime
        });

        return System.Text.Json.JsonSerializer.Serialize(commits);
    }

    [KernelFunction, Description("Find commits whose message contains a keyword")]
    public string FindCommits(
        [Description("Keyword to search")] string keyword,
        [Description("Max results")] int limit = 10)
    {
        EnsureRepo();
        var found = _repo!.Commits
            .Where(c => c.Message
                .Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .Select(c => new
            {
                sha = c.Sha[..7],
                message = c.MessageShort,
                author = c.Author.Name,
                dateUtc = c.Author.When.UtcDateTime
            });

        return System.Text.Json.JsonSerializer.Serialize(found);
    }

    [KernelFunction, Description("Show diff-stats between two commits")]
    public string CompareCommits(
        [Description("Older commit SHA")] string @base,
        [Description("Newer commit SHA")] string head)
    {
        EnsureRepo();
        var baseCommit = _repo!.Lookup<Commit>(@base)
            ?? throw new ArgumentException("Base commit not found");
        var headCommit = _repo!.Lookup<Commit>(head)
            ?? throw new ArgumentException("Head commit not found");

        var patch = _repo.Diff.Compare<Patch>(baseCommit.Tree, headCommit.Tree);
        var summary = new
        {
            files = patch.Count(),
            added = patch.LinesAdded,
            deleted = patch.LinesDeleted
        };
        return System.Text.Json.JsonSerializer.Serialize(summary);
    }

    [KernelFunction, Description("Stage *all* changes and create a commit")]
    public string CommitAll(
        [Description("Commit message")] string message,
        [Description("Committer name")] string author = "ReleaseNotesBot",
        [Description("Committer e-mail")] string email = "bot@example.com")
    {
        EnsureRepo();
        Commands.Stage(_repo!, "*");

        var sig = new Signature(author, email, DateTimeOffset.Now);
        var commit = _repo!.Commit(message, sig, sig);

        return $"✅ Created commit {commit.Sha[..7]}";
    }

    // ─────────────────────────────────────────────
    // Git networking (pull / push)
    // ─────────────────────────────────────────────
    private static Credentials Creds =>
        new UsernamePasswordCredentials
        {
            Username = Environment.GetEnvironmentVariable("GIT_USER") ?? "git",
            Password = Environment.GetEnvironmentVariable("GIT_PAT") ?? ""
        };

    [KernelFunction, Description("Pull latest changes from origin")]
    public string Pull()
    {
        EnsureRepo();
        var sig = new Signature("ReleaseNotesBot", "bot@example.com", DateTimeOffset.Now);
        var result = Commands.Pull(
            _repo!, sig,
            new PullOptions
            {
                FetchOptions = new FetchOptions { CredentialsProvider = (_url, _user, _types) => Creds }
            });

        return $"⬇️  Pull result: {result.Status}";
    }

    [KernelFunction, Description("Push current branch to origin")]
    public string Push(
        [Description("Branch to push (defaults to current)")] string? branch = null)
    {
        EnsureRepo();
        var b = branch ?? _repo!.Head.FriendlyName;
        var opts = new PushOptions { CredentialsProvider = (_url, _user, _types) => Creds };
        _repo?.Network.Push(_repo.Branches[b], opts);

        return $"⬆️  Pushed {b} to origin";
    }

    // ─────────────────────────────────────────────
    // SemVer utilities
    // ─────────────────────────────────────────────
    [KernelFunction, Description("Increment patch version and return new semver")]
    public string BumpPatchVersion()
    {
        EnsureVersionFile();
        var semver = GetCurrentVersion();
        var parts = semver.Split('.').Select(int.Parse).ToArray();
        parts[2]++;
        var newVer = $"{parts[0]}.{parts[1]}.{parts[2]}";
        File.WriteAllText(_verFile, newVer);
        return newVer;
    }

    [KernelFunction, Description("Retrieve the currently stored version")]
    public string GetCurrentVersion()
    {
        EnsureVersionFile();
        return File.ReadAllText(_verFile);
    }

    [KernelFunction, Description("Force-set the version to MAJOR.MINOR.PATCH")]
    public string SetVersion(
        [Description("Version in semantic-version format")] string semver)
    {
        File.WriteAllText(_verFile, semver);
        return $"📌 Version set to {semver}";
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────
    private void EnsureRepo()
    {
        if (_repo is null)
            throw new InvalidOperationException("Repository not set – call SetRepo first.");
    }

    private static void EnsureVersionFile()
    {
        Directory.CreateDirectory("Data");
        if (!File.Exists(_verFile))
            File.WriteAllText(_verFile, "0.0.0");
    }
}
