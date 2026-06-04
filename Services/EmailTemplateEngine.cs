using System.Text.RegularExpressions;

namespace TicketSupport.Services;

public partial class EmailTemplateEngine
{
    private readonly IWebHostEnvironment environment;
    private readonly IConfiguration configuration;
    private readonly ILogger<EmailTemplateEngine> logger;

    public EmailTemplateEngine(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<EmailTemplateEngine> logger)
    {
        this.environment = environment;
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task<string> RenderAsync(string templateName, Dictionary<string, string> variables)
    {
        var template = await LoadTemplateAsync(templateName);
        if (template is null)
        {
            logger.LogWarning("Email template '{TemplateName}' not found", templateName);
            return string.Empty;
        }

        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }

        // Replace unreplaced placeholders with empty string
        result = UnresolvedPlaceholderRegex().Replace(result, string.Empty);

        return result;
    }

    public async Task<string> RenderLoopAsync(string templateName, Dictionary<string, string> variables, string loopKey, IEnumerable<Dictionary<string, string>> loopItems)
    {
        var template = await LoadTemplateAsync(templateName);
        if (template is null)
        {
            logger.LogWarning("Email template '{TemplateName}' not found", templateName);
            return string.Empty;
        }

        // Find loop section: {{#each LoopKey}} ... {{/each}}
        var loopPattern = $@"{{{{#each\s+{Regex.Escape(loopKey)}}}}}(.*?){{{{/each}}}}";
        var loopMatch = Regex.Match(template, loopPattern, RegexOptions.Singleline);

        string loopReplacement;
        if (loopMatch.Success)
        {
            var rowTemplate = loopMatch.Groups[1].Value;
            var renderedRows = new List<string>();

            foreach (var item in loopItems)
            {
                var row = rowTemplate;
                foreach (var (key, value) in item)
                {
                    row = row.Replace($"{{{{{key}}}}}", value);
                }

                renderedRows.Add(row);
            }

            loopReplacement = string.Join(Environment.NewLine, renderedRows);
        }
        else
        {
            loopReplacement = string.Empty;
        }

        var result = Regex.Replace(template, loopPattern, loopReplacement, RegexOptions.Singleline);

        // Replace simple variables
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }

        // Replace unreplaced placeholders with empty string
        result = UnresolvedPlaceholderRegex().Replace(result, string.Empty);

        return result;
    }

    public string GetBaseUrl()
    {
        return (configuration.GetSection("Smtp")["BaseUrl"] ?? "https://localhost:5001").TrimEnd('/');
    }

    private async Task<string?> LoadTemplateAsync(string templateName)
    {
        var templatePath = Path.Combine(environment.WebRootPath ?? environment.ContentRootPath, "Views", "Emails", templateName);
        if (!File.Exists(templatePath))
        {
            // Try content root path
            templatePath = Path.Combine(environment.ContentRootPath, "Views", "Emails", templateName);
        }

        if (!File.Exists(templatePath))
        {
            return null;
        }

        return await File.ReadAllTextAsync(templatePath);
    }

    [GeneratedRegex(@"\{\{.*?\}\}")]
    private static partial Regex UnresolvedPlaceholderRegex();
}
