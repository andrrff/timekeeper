using System.Net.Http.Headers;
using System.Text;
using Timekeeper.Infrastructure.DevOps.GitHub.Models;

namespace Timekeeper.Infrastructure.DevOps.GitHub;

public class SimpleGitHubService : IDisposable
{
    private readonly HttpClient _httpClient;

    public SimpleGitHubService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Timekeeper", "1.0"));
    }

    public async Task<bool> TestConnectionAsync(string organizationUrl, string personalAccessToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(personalAccessToken))
            {
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization = null;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
            
            var response = await _httpClient.GetAsync("https://api.github.com/user");
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<string> GetCurrentUserAsync(string organizationUrl, string personalAccessToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(personalAccessToken))
            {
                return "Unknown";
            }

            _httpClient.DefaultRequestHeaders.Authorization = null;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
            
            var response = await _httpClient.GetAsync("https://api.github.com/user");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Simple JSON parsing to extract username
                if (content.Contains("\"login\""))
                {
                    var startIndex = content.IndexOf("\"login\":\"") + 9;
                    var endIndex = content.IndexOf("\"", startIndex);
                    if (startIndex > 8 && endIndex > startIndex)
                    {
                        return content.Substring(startIndex, endIndex - startIndex);
                    }
                }
            }
            
            return "Unknown";
        }
        catch (Exception)
        {
            return "Unknown";
        }
    }

    public async Task<List<string>> GetRepositoriesAsync(string organizationUrl, string personalAccessToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(personalAccessToken))
            {
                return new List<string>();
            }

            _httpClient.DefaultRequestHeaders.Authorization = null;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
            
            var response = await _httpClient.GetAsync("https://api.github.com/user/repos?per_page=100");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return ParseRepositoryNames(content);
            }
            
            return new List<string>();
        }
        catch (Exception)
        {
            return new List<string>();
        }
    }

    private List<string> ParseRepositoryNames(string jsonContent)
    {
        var repositories = new List<string>();
        
        try
        {
            // Simple JSON parsing to extract repository names
            var namePattern = "\"name\":\"";
            var startIndex = 0;
            
            while ((startIndex = jsonContent.IndexOf(namePattern, startIndex)) != -1)
            {
                startIndex += namePattern.Length;
                var endIndex = jsonContent.IndexOf("\"", startIndex);
                
                if (endIndex > startIndex)
                {
                    var repoName = jsonContent.Substring(startIndex, endIndex - startIndex);
                    repositories.Add(repoName);
                }
                
                startIndex = endIndex;
            }
        }
        catch (Exception)
        {
            // Return empty list on parsing error
        }
        
        return repositories;
    }

    public async Task<List<GitHubIssue>> GetAssignedIssuesAsync(string organizationUrl, string personalAccessToken, List<string> repositoryNames)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(personalAccessToken) || repositoryNames == null || !repositoryNames.Any())
            {
                return new List<GitHubIssue>();
            }

            _httpClient.DefaultRequestHeaders.Authorization = null;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
            
            var allIssues = new List<GitHubIssue>();
            var username = await GetCurrentUserAsync(organizationUrl, personalAccessToken);
            
            foreach (var repoName in repositoryNames.Take(10)) // Limit to avoid rate limiting
            {
                try
                {
                    var url = $"https://api.github.com/repos/{username}/{repoName}/issues?assignee={username}&state=open&per_page=50";
                    var response = await _httpClient.GetAsync(url);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var issues = ParseGitHubIssues(content, repoName);
                        allIssues.AddRange(issues);
                    }
                    
                    // Small delay to avoid rate limiting
                    await Task.Delay(100);
                }
                catch (Exception)
                {
                    // Continue with next repository on error
                    continue;
                }
            }
            
            return allIssues;
        }
        catch (Exception)
        {
            return new List<GitHubIssue>();
        }
    }

    private List<GitHubIssue> ParseGitHubIssues(string jsonContent, string repositoryName)
    {
        var issues = new List<GitHubIssue>();
        
        try
        {
            // Simple JSON parsing to extract issue information
            var issueBlocks = SplitJsonArray(jsonContent);
            
            foreach (var issueBlock in issueBlocks)
            {
                try
                {
                    var issue = new GitHubIssue
                    {
                        Number = int.TryParse(ExtractJsonValue(issueBlock, "number"), out var number) ? number : 0,
                        Id = long.TryParse(ExtractJsonValue(issueBlock, "id"), out var id) ? id : 0,
                        Title = ExtractJsonValue(issueBlock, "title"),
                        Body = ExtractJsonValue(issueBlock, "body"),
                        State = ExtractJsonValue(issueBlock, "state"),
                        HtmlUrl = ExtractJsonValue(issueBlock, "html_url"),
                        CreatedAt = ParseDateSafely(ExtractJsonValue(issueBlock, "created_at")),
                        UpdatedAt = ParseDateSafely(ExtractJsonValue(issueBlock, "updated_at"))
                    };
                    
                    if (issue.Number > 0 && !string.IsNullOrEmpty(issue.Title))
                    {
                        issues.Add(issue);
                    }
                }
                catch (Exception)
                {
                    // Continue with next issue on parsing error
                    continue;
                }
            }
        }
        catch (Exception)
        {
            // Return empty list on parsing error
        }
        
        return issues;
    }

    public async Task<List<GitHubIssue>> GetAssignedIssuesAsync(string personalAccessToken)
    {
        try
        {
            var repositories = await GetRepositoriesAsync(string.Empty, personalAccessToken);
            return await GetAssignedIssuesAsync(string.Empty, personalAccessToken, repositories);
        }
        catch (Exception)
        {
            return new List<GitHubIssue>();
        }
    }

    public async Task<List<GitHubIssue>> GetAssignedIssuesAsync(string organizationUrl, string personalAccessToken, string? repositoryName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(personalAccessToken))
            {
                return new List<GitHubIssue>();
            }

            // If a specific repository is provided, use only that one
            List<string> repositoriesToCheck;
            if (!string.IsNullOrWhiteSpace(repositoryName))
            {
                repositoriesToCheck = new List<string> { repositoryName };
            }
            else
            {
                // Get all repositories for the user
                repositoriesToCheck = await GetRepositoriesAsync(organizationUrl, personalAccessToken);
            }

            // Use the existing implementation that works with repository list
            return await GetAssignedIssuesAsync(organizationUrl, personalAccessToken, repositoriesToCheck);
        }
        catch (Exception)
        {
            return new List<GitHubIssue>();
        }
    }

    public async Task<string> DebugGitHubIssuesAsync(string organizationUrl, string personalAccessToken, string? repositoryName = null)
    {
        try
        {
            var issues = await GetAssignedIssuesAsync(organizationUrl, personalAccessToken, repositoryName);
            
            var debugInfo = new StringBuilder();
            debugInfo.AppendLine($"GitHub Issues Debug Information");
            debugInfo.AppendLine($"Repository: {repositoryName ?? "All repositories"}");
            debugInfo.AppendLine($"Found {issues.Count} issues");
            debugInfo.AppendLine();
            
            foreach (var issue in issues)
            {
                debugInfo.AppendLine($"Issue #{issue.Number}: {issue.Title}");
                debugInfo.AppendLine($"  State: {issue.State}");
                debugInfo.AppendLine($"  URL: {issue.HtmlUrl}");
                debugInfo.AppendLine($"  Created: {issue.CreatedAt}");
                debugInfo.AppendLine();
            }
            
            return debugInfo.ToString();
        }
        catch (Exception ex)
        {
            return $"Error retrieving GitHub issues: {ex.Message}";
        }
    }

    private List<string> SplitJsonArray(string jsonArray)
    {
        var items = new List<string>();
        
        try
        {
            if (string.IsNullOrWhiteSpace(jsonArray) || !jsonArray.Trim().StartsWith("["))
            {
                return items;
            }
            
            var content = jsonArray.Trim();
            if (content.StartsWith("[")) content = content.Substring(1);
            if (content.EndsWith("]")) content = content.Substring(0, content.Length - 1);
            
            var braceCount = 0;
            var currentItem = new StringBuilder();
            
            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                
                if (c == '{')
                {
                    braceCount++;
                    currentItem.Append(c);
                }
                else if (c == '}')
                {
                    braceCount--;
                    currentItem.Append(c);
                    
                    if (braceCount == 0)
                    {
                        items.Add(currentItem.ToString());
                        currentItem.Clear();
                        
                        // Skip comma and whitespace
                        while (i + 1 < content.Length && (content[i + 1] == ',' || char.IsWhiteSpace(content[i + 1])))
                        {
                            i++;
                        }
                    }
                }
                else if (braceCount > 0)
                {
                    currentItem.Append(c);
                }
            }
        }
        catch (Exception)
        {
            // Return whatever items were parsed
        }
        
        return items;
    }

    private string ExtractJsonValue(string json, string key)
    {
        try
        {
            var pattern = $"\"{key}\":";
            var startIndex = json.IndexOf(pattern);
            
            if (startIndex == -1) return string.Empty;
            
            startIndex += pattern.Length;
            
            // Skip whitespace
            while (startIndex < json.Length && char.IsWhiteSpace(json[startIndex]))
            {
                startIndex++;
            }
            
            if (startIndex >= json.Length) return string.Empty;
            
            // Handle string values (quoted)
            if (json[startIndex] == '"')
            {
                startIndex++; // Skip opening quote
                var endIndex = startIndex;
                
                // Find closing quote, handling escaped quotes
                while (endIndex < json.Length)
                {
                    if (json[endIndex] == '"' && (endIndex == 0 || json[endIndex - 1] != '\\'))
                    {
                        break;
                    }
                    endIndex++;
                }
                
                if (endIndex < json.Length)
                {
                    return json.Substring(startIndex, endIndex - startIndex);
                }
            }
            // Handle numeric values
            else
            {
                var endIndex = startIndex;
                while (endIndex < json.Length && char.IsDigit(json[endIndex]))
                {
                    endIndex++;
                }
                
                if (endIndex > startIndex)
                {
                    return json.Substring(startIndex, endIndex - startIndex);
                }
            }
        }
        catch (Exception)
        {
            // Return empty string on error
        }
        
        return string.Empty;
    }

    private DateTime ParseDateSafely(string dateString)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dateString))
            {
                return DateTime.MinValue;
            }
            
            if (DateTime.TryParse(dateString, out DateTime result))
            {
                return result;
            }
        }
        catch (Exception)
        {
            // Return MinValue on parsing error
        }
        
        return DateTime.MinValue;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
