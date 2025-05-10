using LibGit2Sharp;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SemanticKernelPlayground.Plugins;

public class GitPlugin
{
    private Repository? _repo;

    [KernelFunction, Description("Set the working repository path")]
    public string SetRepo(
        [Description("Absolute path to a local Git repository")] string path)
    {
        if (!Repository.IsValid(path))
            throw new ArgumentException("Not a valid git repo 🔥");

        _repo = new Repository(path);
        return $"👍 Repo set to {path}";
    }

    [KernelFunction, Description("Get latest commits as JSON")]
    public string GetCommits(
        [Description("How many commits to retrieve")] int count = 10)
    {
        if (_repo == null) throw new InvalidOperationException("Repo not set");

        var commits = _repo.Commits.Take(count).Select(c => new
        {
            sha = c.Sha[..7],
            message = c.MessageShort,
            author = c.Author.Name,
            dateUtc = c.Author.When.UtcDateTime
        });

        return System.Text.Json.JsonSerializer.Serialize(commits);
    }

    // OPTIONAL ➜ simple patch‑bump stored in /Data/version.json
    [KernelFunction, Description("Increment patch version and return new semver")]
    public string BumpPatchVersion()
    {
        const string path = "Data/version.json";
        var semver = File.Exists(path) ? File.ReadAllText(path) : "0.0.0";
        var parts = semver.Split('.').Select(int.Parse).ToArray();
        parts[2]++;                           // bump patch
        var newVer = $"{parts[0]}.{parts[1]}.{parts[2]}";
        File.WriteAllText(path, newVer);
        return newVer;
    }
}

