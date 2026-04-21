using Markdig;
using System.Net;
using System.Text.RegularExpressions;

namespace MedProject.Utilities;

/// <summary>
/// Конвертация Markdown в плоский текст для отображения в Avalonia (TextBlock).
/// Разбор выполняется через Markdig; HTML приводится к читаемому тексту.
/// </summary>
public static class MarkdownHelper
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>
    /// Преобразует Markdown в обычный текст без разметки.
    /// </summary>
    public static string MarkdownToPlainText(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var html = Markdown.ToHtml(markdown, Pipeline);
        return HtmlToPlainText(html);
    }

    private static string HtmlToPlainText(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        var s = html;

        s = Regex.Replace(s, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"</(p|div|section|article|blockquote|pre|table|tr|h[1-6])>", "\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"</li>", "\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<li[^>]*>", "• ", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<hr\s*/?>", "\n────────\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<[^>]+>", string.Empty);
        s = WebUtility.HtmlDecode(s);

        // Нормализация переводов строк и пробелов
        s = Regex.Replace(s, "\r\n|\r", "\n");
        s = Regex.Replace(s, "[ \t]+\n", "\n");
        s = Regex.Replace(s, "\n{3,}", "\n\n");

        return s.Trim();
    }
}
