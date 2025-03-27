using Spectre.Console;

namespace Bloom.Filter;

internal static class Program
{
    private static void Main(string[] args)
    {
        AnsiConsole.Write(
            new FigletText("Bloom Filter")
                .LeftJustified()
                .Color(Color.Green));

        // Create a rule with a title
        var rule = new Rule("[green]Bloom Filter Demo[/]")
        {
            Justification = Justify.Left,
        };
        
        AnsiConsole.Write(rule);

        // Display explanation
        AnsiConsole.MarkupLine("[yellow]A Bloom filter is a space-efficient probabilistic data structure used to test whether an element is a member of a set.[/]");
        AnsiConsole.MarkupLine("[yellow]It may produce false positives (indicating an element is in the set when it's not), but never false negatives.[/]");

        // Get user input for Bloom filter parameters
        int capacity = AnsiConsole.Prompt(
            new TextPrompt<int>("[green]Enter the expected number of elements (capacity):[/]")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Please enter a valid number[/]")
                .Validate(capacity => capacity > 0 ? ValidationResult.Success() : ValidationResult.Error("Capacity must be greater than 0")));

        double falsePositiveRate = AnsiConsole.Prompt(
            new TextPrompt<double>("[green]Enter the desired false positive rate (between 0 and 1):[/]")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Please enter a valid number[/]")
                .Validate(rate => rate > 0 && rate < 1 ? ValidationResult.Success() : ValidationResult.Error("Rate must be between 0 and 1")));

        // Create Bloom filter with user parameters
        var bloomFilter = new BloomFilter(capacity, falsePositiveRate);

        // Display Bloom filter configuration
        AnsiConsole.MarkupLine($"[green]Bloom Filter created with:[/]");
        AnsiConsole.MarkupLine($"[green]- Bit array size: {bloomFilter.Size} bits[/]");
        AnsiConsole.MarkupLine($"[green]- Number of hash functions: {bloomFilter.HashFunctionCount}[/]");

        bool running = true;
        while (running)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]What would you like to do?[/]")
                    .PageSize(10)
                    .AddChoices("Add an element", "Check if element exists", "Add multiple elements", "View filter information", "Exit"));

            switch (choice)
            {
                case "Add an element":
                    var element = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]Enter element to add:[/]")
                            .PromptStyle("green"));
                    bloomFilter.Add(element);
                    AnsiConsole.MarkupLine($"[green]Added '{element}' to the Bloom filter[/]");
                    break;

                case "Check if element exists":
                    var checkElement = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]Enter element to check:[/]")
                            .PromptStyle("green"));
                    bool result = bloomFilter.MightContain(checkElement);
                    if (result)
                        AnsiConsole.MarkupLine($"[yellow]'{checkElement}' might be in the Bloom filter (could be a false positive)[/]");
                    else
                        AnsiConsole.MarkupLine($"[red]'{checkElement}' is definitely not in the Bloom filter[/]");
                    break;

                case "Add multiple elements":
                    var elementsToAdd = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]Enter elements to add (comma-separated):[/]")
                            .PromptStyle("green"));
                    var elements = elementsToAdd.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var e in elements)
                    {
                        bloomFilter.Add(e.Trim());
                    }
                    AnsiConsole.MarkupLine($"[green]Added {elements.Length} elements to the Bloom filter[/]");
                    break;

                case "View filter information":
                    AnsiConsole.MarkupLine($"[green]Bloom Filter Information:[/]");
                    AnsiConsole.MarkupLine($"[green]- Bit array size: {bloomFilter.Size} bits[/]");
                    AnsiConsole.MarkupLine($"[green]- Number of hash functions: {bloomFilter.HashFunctionCount}[/]");
                    AnsiConsole.MarkupLine($"[green]- Estimated element count: {bloomFilter.EstimatedElementCount}[/]");
                    AnsiConsole.MarkupLine($"[green]- Current estimated false positive rate: {bloomFilter.CurrentFalsePositiveRate:P6}[/]");
                    break;

                case "Exit":
                    running = false;
                    break;
            }
        }

        AnsiConsole.MarkupLine("[green]Thank you for using the Bloom Filter Demo![/]");
    }
}