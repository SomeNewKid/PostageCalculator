using System.Diagnostics;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

namespace PostageCalculator;

internal static class Program
{
    private static PostageRequest? CurrentPostageRequest { get; set; }
    private static string? LastSkillOutput { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private static async Task Main(string[] args)
    {
        string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OPENAI_API_KEY is not set.");

        string model = "gpt-4o-mini";

        OpenAIClient client = new(apiKey);

        AIAgent parserAgent = client
            .GetChatClient(model)
            .AsAIAgent(
                name: "PostageRequestParser",
                instructions: """
                    You extract postage request details from natural language.

                    The origin is always Perth, Australia.

                    Return only valid JSON matching this exact shape:

                    {
                      "city": "destination city",
                      "country": "destination country",
                      "weight": 2.5
                    }

                    Rules:
                    - city must be the destination city.
                    - country must be the destination country.
                    - weight must be the package weight in kilograms.
                    - If the country is not explicitly stated but the city is a well-known Australian city,
                      use "Australia".
                    - If the user gives weight in kilograms or kg, use that number directly.
                    - Do not include markdown.
                    - Do not include explanation.
                    - Do not include extra properties.
                    - If city, country, or weight cannot be determined, return an empty string for missing text
                      fields and 0 for missing weight.
                    """);

        string skillsPath = Path.Combine(AppContext.BaseDirectory, "skills");

        var skillsProvider = new AgentSkillsProvider(
            skillsPath,
            RunPythonSkillScript);

        AIAgent postageAgent = client
            .GetChatClient(model)
            .AsAIAgent(new ChatClientAgentOptions
            {
                Name = "PostageCalculator",
                ChatOptions = new()
                {
                    Instructions = """
                        You calculate postage for packages sent from Perth, Australia.

                        You will receive a parsed postage request as JSON with city, country,
                        and weight properties.

                        Choose exactly one available skill:
                        - If country is Australia, use the domestic-delivery skill.
                        - If country is not Australia, use the international-shipping skill.

                        When calling a skill script, pass the exact city, country, and weight
                        values from the parsed JSON. Never invent, substitute, normalize, or
                        use example values as script arguments.

                        Do not calculate postage yourself. Always use one of the available
                        postage skills to determine the cost.

                        Respond with a concise sentence containing the destination, package
                        weight, and postage cost in Australian dollars.
                        """,
                    Temperature = 0
                },
                AIContextProviders = [skillsProvider]
            });

        AIAgent approvedPostageAgent = new ToolApprovalAgent(
            postageAgent,
            new ToolApprovalAgentOptions
            {
                AutoApprovalRules = [AgentSkillsProvider.AllToolsAutoApprovalRule]
            });

        string userRequest;

        if (args.Length > 0)
        {
            userRequest = string.Join(' ', args);
        }
        else
        {
            Console.WriteLine("Enter a postage request:");
            userRequest = Console.ReadLine() ?? string.Empty;
        }

        string responseJson = (await parserAgent.RunAsync(userRequest)).ToString();

        PostageRequest? postageRequest = JsonSerializer.Deserialize<PostageRequest>(
            responseJson,
            JsonOptions);

        if (postageRequest is null)
        {
            throw new InvalidOperationException("The agent did not return a valid postage request.");
        }

        if (string.IsNullOrWhiteSpace(postageRequest.City)
            || string.IsNullOrWhiteSpace(postageRequest.Country)
            || postageRequest.Weight <= 0)
        {
            Console.WriteLine("Please provide the destination city, destination country, and package weight in kilograms.");
            return;
        }

        Console.WriteLine("Parsed postage request:");
        Console.WriteLine(JsonSerializer.Serialize(postageRequest, JsonOptions));

        CurrentPostageRequest = postageRequest;

        string parsedRequestJson = JsonSerializer.Serialize(postageRequest, JsonOptions);

        await approvedPostageAgent.RunAsync(
            [
                new Microsoft.Extensions.AI.ChatMessage(
                ChatRole.User,
                $"""
                Use the appropriate skill to calculate postage for this parsed request.
                Use these exact values as the skill script arguments:

                {parsedRequestJson}
                """)
            ],
            session: null,
            options: null);

        Console.WriteLine(LastSkillOutput ?? "The postage skill did not return a result.");
    }

    private static async Task<object?> RunPythonSkillScript(
        AgentFileSkill skill,
        AgentFileSkillScript script,
        JsonElement? arguments,
        IServiceProvider? serviceProvider,
        CancellationToken cancellationToken)
    {
        using Process process = new();

        process.StartInfo = new ProcessStartInfo("python")
        {
            WorkingDirectory = AppContext.BaseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.StartInfo.ArgumentList.Add(script.FullPath);

        if (CurrentPostageRequest is not null)
        {
            process.StartInfo.ArgumentList.Add($"city={CurrentPostageRequest.City}");

            if (!string.Equals(CurrentPostageRequest.Country, "Australia", StringComparison.OrdinalIgnoreCase))
            {
                process.StartInfo.ArgumentList.Add($"country={CurrentPostageRequest.Country}");
            }

            process.StartInfo.ArgumentList.Add($"weight={CurrentPostageRequest.Weight}");
        }
        else if (arguments is { ValueKind: JsonValueKind.Array } json)
        {
            foreach (JsonElement element in json.EnumerateArray())
            {
                string? argument = element.GetString();

                if (!string.IsNullOrWhiteSpace(argument))
                {
                    process.StartInfo.ArgumentList.Add(argument);
                }
            }
        }

        Console.WriteLine(
            $"Running skill script: {Path.GetFileName(script.FullPath)} {string.Join(' ', process.StartInfo.ArgumentList.Skip(1))}");

        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        string error = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Skill script '{script.FullPath}' failed with exit code {process.ExitCode}: {error}");
        }

        LastSkillOutput = output.Trim();

        return LastSkillOutput;
    }
}
