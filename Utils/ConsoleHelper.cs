using Spectre.Console;

namespace Scribe.Utils;

/// <summary>
/// Helper methods for safely displaying external text that might contain
/// special characters that would be interpreted as Spectre.Console markup.
/// </summary>
public static class ConsoleHelper
{
    /// <summary>
    /// Safely writes text that might contain markup characters.
    /// Use this for error messages, file paths, API responses, or any external text.
    /// </summary>
    public static void WriteSafe(string text)
    {
        AnsiConsole.Write(text.EscapeMarkup());
    }

    /// <summary>
    /// Safely writes a line of text that might contain markup characters.
    /// Use this for error messages, file paths, API responses, or any external text.
    /// </summary>
    public static void WriteLineSafe(string text)
    {
        AnsiConsole.WriteLine(text.EscapeMarkup());
    }

    /// <summary>
    /// Writes markup followed by safe text.
    /// Example: WriteMarkupWithSafeText("[red]Error:[/] ", exceptionMessage)
    /// </summary>
    public static void WriteMarkupWithSafeText(string markup, string safeText)
    {
        AnsiConsole.Markup(markup);
        AnsiConsole.WriteLine(safeText.EscapeMarkup());
    }

    /// <summary>
    /// Safely formats an error message with consistent styling.
    /// </summary>
    public static void WriteError(string message)
    {
        WriteMarkupWithSafeText("[red]Error:[/] ", message);
    }

    /// <summary>
    /// Safely formats a warning message with consistent styling.
    /// </summary>
    public static void WriteWarning(string message)
    {
        WriteMarkupWithSafeText("[yellow]Warning:[/] ", message);
    }

    /// <summary>
    /// Safely formats a success message with consistent styling.
    /// </summary>
    public static void WriteSuccess(string message)
    {
        WriteMarkupWithSafeText("[green]Success:[/] ", message);
    }
}
