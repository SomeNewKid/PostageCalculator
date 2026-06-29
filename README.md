# Postage Calculator

Postage Calculator is a small .NET command-line sample for exploring skills in
Microsoft's Agent Framework. It accepts a natural-language postage request,
parses the destination and package weight, chooses a domestic or international
skill, and runs a local Python script to calculate the postage cost.

> [!WARNING]
> This is an experimental project and should not be considered production-ready.

The project was created to test file-based Agent Framework skills from a C#
agent. The business domain is intentionally small: all packages are sent from
Perth, Australia, and only the cities listed in `Data/Cities.csv` are
supported.

## What It Does

The CLI accepts a prompt such as:

```powershell
dotnet run -- "I need to send a package weighing 2.5 kilograms to Paris in France."
```

The agent flow is:

1. Parse the user's natural-language request into a `PostageRequest`.
2. Give the structured request to a skill-enabled postage agent.
3. Let the agent choose the appropriate file-based skill:
   - `domestic-delivery` for destinations in Australia.
   - `international-shipping` for destinations outside Australia.
4. Run the Python script bundled with the selected skill.
5. Print the postage result returned by the script.

Example output:

```text
Parsed postage request:
{
  "City": "Paris",
  "Country": "France",
  "Weight": 2.5
}
Running skill script: international_shipping.py city=Paris country=France weight=2.5
weight_cost: 10.0, distance_cost: 285.56686048255165, base_rate: 25.0
The postage cost to Paris, France for a package weighing 2.5 kg is: $320.57
```

## Requirements

- .NET 8 SDK.
- Python available as `python` on the command path.
- PowerShell on Windows.
- An `OPENAI_API_KEY` environment variable for OpenAI model calls.

## Setup

Restore and build the project from the repository root:

```powershell
dotnet restore
dotnet build
```

The build copies the `skills` and `Data` folders to the output directory so the
Agent Framework skill provider and Python scripts can find their local files at
runtime.

## Running

Run the calculator from the repository root:

```powershell
dotnet run -- "How much to send a 2.5 kg package to Sydney?"
```

or:

```powershell
dotnet run -- "I need to send a package weighing 2.5 kilograms to Paris in France."
```

If no command-line prompt is supplied, the application asks for one
interactively.

## Development Checks

Build the project:

```powershell
dotnet build
```

There is not currently a separate automated test suite for this sample.

## Project Structure

```text
Program.cs                                      Agent setup, skill provider, script runner, and CLI
Models.cs                                       Shared request/result records
PostageCalculator.csproj                        .NET project file and content-copy configuration

Data/
  Cities.csv                                    Supported destination city coordinates

skills/
  domestic-delivery/
    SKILL.md                                    Domestic delivery skill instructions
    scripts/
      domestic_postage.py                       Domestic postage calculator

  international-shipping/
    SKILL.md                                    International shipping skill instructions
    scripts/
      international_shipping.py                 International shipping calculator
```

## Notes

The project uses C# for agent orchestration and Python only for the skill
scripts. The skill scripts are file-based Agent Framework skill assets, not
standalone application entry points.

`Microsoft.Agents.AI` marks file-based skills as evaluation-only in the package
version used by this project, so `MAAI001` is suppressed in the project file.

The sample includes a small custom script runner delegate because the
`AgentSkillsProvider` needs application code to define how file-based scripts
are executed. The runner launches Python, sets the working directory to the
application base directory, and passes the parsed postage request to the chosen
skill script.

Agent behavior and final skill selection are model-driven. OpenAI API calls may
incur usage costs.

## Third-Party Notices

This project has direct runtime dependencies on third-party NuGet packages,
including `Microsoft.Agents.AI`, `Microsoft.Agents.AI.OpenAI`, and `OpenAI`.
See each package's NuGet license metadata for full license and notice terms.

## License

GNU General Public License v3.0. See the `LICENSE` file for details.
