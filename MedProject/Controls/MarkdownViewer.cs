using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Linq;
using System.Text;
using AvLineBreak = Avalonia.Controls.Documents.LineBreak;
using AvRun = Avalonia.Controls.Documents.Run;
using AvSpan = Avalonia.Controls.Documents.Span;

namespace MedProject.Controls;

/// <summary>
/// Рендерит Markdown в нативные элементы Avalonia с форматированием:
/// заголовки, жирный/курсив, списки, таблицы, блоки кода.
/// </summary>
public class MarkdownViewer : UserControl
{
    private StackPanel? _rootPanel;

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MarkdownViewer, string?>(nameof(Text));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly IBrush TableGridLineBrush = new SolidColorBrush(Color.Parse("#C5C5C5"));

    /// <summary>
    /// Пропорции колонок для типичной справочной таблицы (показатель / описание / формула / комментарий), без доминирования одной колонки.
    /// </summary>
    private static void AddMarkdownTableColumnDefinitions(Grid grid, int colCount)
    {
        if (colCount == 4)
        {
            foreach (var weight in new[] { 1.15, 1.2, 1.25, 1.15 })
                grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(weight, GridUnitType.Star)) { MinWidth = 0 });
            return;
        }

        for (int c = 0; c < colCount; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star) { MinWidth = 0 });
    }

    static MarkdownViewer()
    {
        TextProperty.Changed.AddClassHandler<MarkdownViewer>((viewer, _) => viewer.Render());
    }

    public MarkdownViewer()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;
        MinWidth = 0;
        SizeChanged += OnViewerSizeChanged;
        Render();
    }

    /// <summary>
    /// Ограничивает ширину контента областью просмотра, иначе TextBlock с Inlines
    /// может измеряться по длине строки без переноса и обрезаться при отключённой горизонтальной прокрутке.
    /// Нельзя задавать MaxWidth = ∞: тогда колонки Grid со звёздами получают бесконечную ширину и перенос не срабатывает.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_rootPanel != null)
        {
            var w = availableSize.Width;
            if (!double.IsFinite(w) || w <= 0 || double.IsPositiveInfinity(w))
            {
                var bw = Bounds.Width;
                if (double.IsFinite(bw) && bw > 0)
                    w = bw;
                else
                    w = 600; // первый проход до Arrange; затем подхватится реальная ширина через SizeChanged
            }

            var inner = Math.Max(0, w - Padding.Left - Padding.Right);
            _rootPanel.MaxWidth = inner;
        }

        return base.MeasureOverride(availableSize);
    }

    private void OnViewerSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (_rootPanel == null || !double.IsFinite(e.NewSize.Width) || e.NewSize.Width <= 0)
            return;
        var inner = Math.Max(0, e.NewSize.Width - Padding.Left - Padding.Right);
        _rootPanel.MaxWidth = inner;
    }

    private void Render()
    {
        var text = Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            _rootPanel = null;
            Content = null;
            return;
        }

        var document = Markdown.Parse(text, Pipeline);
        var panel = new StackPanel
        {
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 0,
        };

        foreach (var block in document)
            panel.Children.Add(RenderBlock(block, listItemLeadParagraph: false));

        _rootPanel = panel;
        Content = panel;
    }

    // ──────────────── Block renderers ────────────────

    /// <param name="listItemLeadParagraph">
    /// Первый абзац внутри пункта списка: без верхних отступов, чтобы текст совпадал по вертикали с маркером (иначе «падающий» список).
    /// </param>
    private static Control RenderBlock(Block block, bool listItemLeadParagraph)
    {
        return block switch
        {
            HeadingBlock h => RenderHeading(h),
            ParagraphBlock p => RenderParagraph(p, listItemLeadParagraph),
            ListBlock l => RenderList(l, 0),
            FencedCodeBlock fc => RenderCodeBlock(fc),
            CodeBlock c => RenderCodeBlock(c),
            ThematicBreakBlock => RenderThematicBreak(),
            QuoteBlock q => RenderQuote(q),
            Table t => RenderTable(t),
            _ => new Border { Height = 0 }
        };
    }

    private static Control RenderHeading(HeadingBlock h)
    {
        var (fontSize, fontWeight, topMargin, color) = h.Level switch
        {
            1 => (26.0, FontWeight.Bold, 16.0, "#1A237E"),
            2 => (20.0, FontWeight.SemiBold, 12.0, "#1565C0"),
            3 => (16.0, FontWeight.SemiBold, 8.0, "#1976D2"),
            _ => (14.0, FontWeight.SemiBold, 4.0, "#1E88E5")
        };

        // Явный LineHeight: иначе у жирного/полужирного текста нижние выносные (р, у, д…) визуально обрезаются.
        var lineHeight = fontSize * 1.45;
        var tb = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = fontSize,
            FontWeight = fontWeight,
            LineHeight = lineHeight,
            Foreground = new SolidColorBrush(Color.Parse(color)),
            Margin = new Thickness(0, topMargin, 0, h.Level <= 2 ? 6 : 4),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 0,
        };

        if (h.Inline != null)
            PopulateInlines(tb.Inlines!, h.Inline);

        if (h.Level <= 2)
        {
            var lineColor = h.Level == 1 ? "#3F51B5" : "#90CAF9";
            return new StackPanel
            {
                Children =
                {
                    tb,
                    new Border
                    {
                        Height = h.Level == 1 ? 2 : 1,
                        Background = new SolidColorBrush(Color.Parse(lineColor)),
                        Margin = new Thickness(0, 0, 0, 8),
                        CornerRadius = new CornerRadius(1),
                    }
                }
            };
        }

        return tb;
    }

    private static Control RenderParagraph(ParagraphBlock p, bool listItemLeadParagraph = false)
    {
        var topMargin = listItemLeadParagraph ? 0 : 2;
        var topPad = listItemLeadParagraph ? 0 : 3;
        var tb = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.Parse("#212121")),
            LineHeight = 24,
            Margin = new Thickness(0, topMargin, 0, 4),
            Padding = new Thickness(0, topPad, 0, 5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 0,
        };

        if (p.Inline != null)
            PopulateInlines(tb.Inlines!, p.Inline);

        return tb;
    }

    private static Control RenderList(ListBlock list, int depth, bool inTableCell = false)
    {
        var panel = new StackPanel { Spacing = 1 };
        int counter = list.IsOrdered
            ? (int.TryParse(list.OrderedStart, out var s) ? s : 1)
            : 0;

        foreach (var item in list.OfType<ListItemBlock>())
        {
            var bulletText = list.IsOrdered ? $"{counter}." : "•";
            counter++;

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                Margin = new Thickness(depth * 14, 1, 0, 1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinWidth = 0,
            };

            var bulletBlock = new TextBlock
            {
                Text = bulletText,
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 14,
                LineHeight = 24,
                Foreground = new SolidColorBrush(Color.Parse("#1565C0")),
                VerticalAlignment = VerticalAlignment.Top,
                MinWidth = list.IsOrdered ? 22 : 12,
                FontWeight = list.IsOrdered ? FontWeight.SemiBold : FontWeight.Normal,
            };
            Grid.SetColumn(bulletBlock, 0);
            grid.Children.Add(bulletBlock);

            var content = new StackPanel { Spacing = 1 };
            var blockIndex = 0;
            foreach (var subBlock in item)
            {
                var leadParagraph =
                    blockIndex == 0 && subBlock is ParagraphBlock;
                blockIndex++;

                if (subBlock is ListBlock subList)
                    content.Children.Add(RenderList(subList, depth + 1, inTableCell));
                else if (inTableCell)
                    content.Children.Add(RenderBlockForTableCell(subBlock, leadParagraph));
                else
                    content.Children.Add(RenderBlock(subBlock, leadParagraph));
            }

            Grid.SetColumn(content, 1);
            grid.Children.Add(content);
            panel.Children.Add(grid);
        }

        return new Border
        {
            Margin = new Thickness(depth == 0 ? 6 : 0, 2, 0, 2),
            Child = panel
        };
    }

    private static Control RenderCodeBlock(LeafBlock code)
    {
        var sb = new StringBuilder();
        var lines = code.Lines;
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0) sb.AppendLine();
            sb.Append(lines.Lines[i].ToString());
        }

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#F5F5F5")),
            BorderBrush = new SolidColorBrush(Color.Parse("#DEDEDE")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(14, 8),
            Margin = new Thickness(0, 4, 0, 6),
            Child = new TextBlock
            {
                Text = sb.ToString().TrimEnd(),
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                FontSize = 13,
                LineHeight = 20,
                Padding = new Thickness(0, 2, 0, 3),
                TextWrapping = TextWrapping.Wrap,
            }
        };
    }

    private static Control RenderThematicBreak()
    {
        return new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.Parse("#BDBDBD")),
            Margin = new Thickness(0, 10, 0, 10)
        };
    }

    private static Control RenderQuote(QuoteBlock q)
    {
        var inner = new StackPanel { Spacing = 4 };
        foreach (var block in q)
            inner.Children.Add(RenderBlock(block, listItemLeadParagraph: false));

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#1565C0")),
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(12, 6, 0, 6),
            Margin = new Thickness(0, 4, 0, 4),
            Background = new SolidColorBrush(Color.Parse("#E3F2FD")),
            CornerRadius = new CornerRadius(0, 4, 4, 0),
            Child = inner
        };
    }

    private static Control RenderTable(Table table)
    {
        var rows = table.OfType<TableRow>().ToList();
        if (rows.Count == 0) return new Border { Height = 0 };

        int colCount = rows.Max(r => r.Count);
        if (colCount == 0) return new Border { Height = 0 };

        var grid = new Grid
        {
            MinWidth = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            UseLayoutRounding = true,
        };
        AddMarkdownTableColumnDefinitions(grid, colCount);

        for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var row = rows[rowIdx];

            for (int col = 0; col < row.Count; col++)
            {
                if (row[col] is not TableCell cell) continue;

                bool isHeader = row.IsHeader;
                bool isEvenRow = rowIdx % 2 == 0;

                var border = new TableCellBorder
                {
                    BorderBrush = TableGridLineBrush,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Background = isHeader
                        ? new SolidColorBrush(Color.Parse("#E8F4FC"))
                        : new SolidColorBrush(isEvenRow ? Colors.White : Color.Parse("#FAFAFA")),
                    Padding = new Thickness(9, 8),
                    MinWidth = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    // Stretch: ячейка заполняет высоту строки — нижняя граница совпадает во всех колонках (при Top линии «рвались»).
                    VerticalAlignment = VerticalAlignment.Stretch,
                    ClipToBounds = false,
                };

                var cellContent = new StackPanel
                {
                    Spacing = 2,
                    MinWidth = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                };

                foreach (var cellBlock in cell)
                    cellContent.Children.Add(RenderBlockForTableCell(cellBlock));

                if (isHeader)
                    ApplySemiBoldToTextBlocks(cellContent);

                border.Child = cellContent;

                Grid.SetRow(border, rowIdx);
                Grid.SetColumn(border, col);
                grid.Children.Add(border);
            }
        }

        return new Border
        {
            BorderBrush = TableGridLineBrush,
            BorderThickness = new Thickness(1, 1, 0, 0),
            CornerRadius = new CornerRadius(5),
            // ClipToBounds обрезал многострочный текст в ячейках при измерении строк Grid
            ClipToBounds = false,
            Margin = new Thickness(0, 6, 0, 8),
            MinWidth = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Child = grid
        };
    }

    /// <summary>
    /// Абзацы в ячейке таблицы: без фиксированного LineHeight — иначе при FontSize 14 строка
    /// визуально обрезалась по нижнему краю (descenders, многострочный текст).
    /// </summary>
    private static Control RenderBlockForTableCell(Block block, bool listItemLeadParagraph = false)
    {
        return block switch
        {
            ParagraphBlock p => RenderParagraphForTableCell(p, listItemLeadParagraph),
            ListBlock l => RenderList(l, 0, inTableCell: true),
            _ => RenderBlock(block, listItemLeadParagraph: false)
        };
    }

    private static Control RenderParagraphForTableCell(ParagraphBlock p, bool listItemLeadParagraph = false)
    {
        var topMargin = listItemLeadParagraph ? 0 : 2;
        var topPad = listItemLeadParagraph ? 0 : 3;
        var tb = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.Parse("#212121")),
            LineHeight = 24,
            Margin = new Thickness(0, topMargin, 0, 4),
            Padding = new Thickness(0, topPad, 0, 5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 0,
        };

        if (p.Inline != null)
            PopulateInlines(tb.Inlines!, p.Inline);

        return tb;
    }

    /// <summary>
    /// Задаёт ограничение ширины на этапе Measure/Arrange — иначе при «бесконечной» ширине колонки TextBlock с Inlines не переносится.
    /// </summary>
    private sealed class TableCellBorder : Border
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            ConstrainChild(InnerWidth(availableSize));
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            ConstrainChild(InnerWidth(finalSize));
            return base.ArrangeOverride(finalSize);
        }

        private double InnerWidth(Size size)
        {
            var w = size.Width - Padding.Left - Padding.Right;
            return w > 0 && double.IsFinite(w) ? w : 0;
        }

        private void ConstrainChild(double inner)
        {
            if (Child is not Control c || inner <= 0)
                return;
            c.MaxWidth = inner;
            ApplyMaxWidthForTableCellText(c, inner);
            EnsureTableCellTextWrap(c);
        }
    }

    /// <summary>
    /// TextBlock с Inlines в Avalonia не всегда наследует ограничение ширины от StackPanel; без MaxWidth измеряется в одну длинную строку и обрезается при отключённой горизонтальной прокрутке.
    /// </summary>
    private static void ApplyMaxWidthForTableCellText(Control root, double innerWidth)
    {
        switch (root)
        {
            case TextBlock tb:
                tb.MaxWidth = innerWidth;
                return;
            case Panel panel:
                foreach (var child in panel.Children)
                {
                    if (child is Control c)
                        ApplyMaxWidthForTableCellText(c, innerWidth);
                }
                return;
            case Border b when b.Child is Control ch:
                ApplyMaxWidthForTableCellText(ch, innerWidth);
                return;
            case ContentControl cc when cc.Content is Control chc:
                ApplyMaxWidthForTableCellText(chc, innerWidth);
                return;
            default:
                return;
        }
    }

    /// <summary>
    /// Дублируем ограничение через <see cref="ApplyMaxWidthForTableCellText"/> для TextBlock с Inlines;
    /// для абзацев с LineHeight это по-прежнему не задаём в ячейках таблицы (см. <see cref="RenderParagraphForTableCell"/>).
    /// </summary>
    private static void EnsureTableCellTextWrap(Control root)
    {
        switch (root)
        {
            case TextBlock tb:
                tb.TextWrapping = TextWrapping.Wrap;
                tb.MinWidth = 0;
                tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                return;
            case Panel panel:
                foreach (var child in panel.Children)
                {
                    if (child is Control c)
                        EnsureTableCellTextWrap(c);
                }
                return;
            case Border b when b.Child is Control ch:
                EnsureTableCellTextWrap(ch);
                return;
            case ContentControl cc when cc.Content is Control chc:
                EnsureTableCellTextWrap(chc);
                return;
            default:
                return;
        }
    }

    private static void ApplySemiBoldToTextBlocks(Control root)
    {
        switch (root)
        {
            case TextBlock tb:
                tb.FontWeight = FontWeight.SemiBold;
                return;
            case Panel panel:
                foreach (var child in panel.Children)
                {
                    if (child is Control c)
                        ApplySemiBoldToTextBlocks(c);
                }
                return;
            case Border b when b.Child is Control ch:
                ApplySemiBoldToTextBlocks(ch);
                return;
            case ContentControl cc when cc.Content is Control chc:
                ApplySemiBoldToTextBlocks(chc);
                return;
            default:
                return;
        }
    }

    // ──────────────── Inline renderers ────────────────

    private static void PopulateInlines(InlineCollection target, ContainerInline container)
    {
        foreach (var inline in container)
            AddInline(target, inline);
    }

    private static void AddInline(InlineCollection target, Markdig.Syntax.Inlines.Inline inline)
    {
        switch (inline)
        {
            case LiteralInline lit:
                target.Add(new AvRun { Text = lit.Content.ToString() });
                break;

            case EmphasisInline em:
                {
                    var span = new AvSpan();
                    if (em.DelimiterCount >= 2)
                        span.FontWeight = FontWeight.Bold;
                    else
                        span.FontStyle = FontStyle.Italic;

                    foreach (var child in em)
                        AddInline(span.Inlines!, child);

                    target.Add(span);
                }
                break;

            case CodeInline code:
                target.Add(new AvRun
                {
                    Text = $"\u00A0{code.Content}\u00A0",
                    FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                    Background = new SolidColorBrush(Color.Parse("#EDE7F6")),
                    Foreground = new SolidColorBrush(Color.Parse("#512DA8")),
                    FontSize = 13,
                });
                break;

            case LineBreakInline lb:
                if (lb.IsHard)
                    target.Add(new AvLineBreak());
                else
                    target.Add(new AvRun { Text = " " });
                break;

            // LinkInline — подтип ContainerInline: обрабатываем отдельно, иначе попадаем в общий случай без URL.
            case LinkInline link:
                AddLinkInline(target, link);
                break;

            case AutolinkInline auto:
                {
                    var urlRun = new AvRun
                    {
                        Text = auto.Url,
                        Foreground = new SolidColorBrush(Color.Parse("#1565C0")),
                        TextDecorations = TextDecorations.Underline,
                    };
                    target.Add(urlRun);
                }
                break;

            case HtmlEntityInline entity:
                target.Add(new AvRun { Text = entity.Transcoded.ToString() });
                break;

            case ContainerInline ci:
                foreach (var child in ci)
                    AddInline(target, child);
                break;

            default:
                break;
        }
    }

    private static void AddLinkInline(InlineCollection target, LinkInline link)
    {
        if (link.IsImage)
        {
            var caption = new AvSpan { FontStyle = FontStyle.Italic, Foreground = new SolidColorBrush(Color.Parse("#616161")) };
            foreach (var child in link)
                AddInline(caption.Inlines!, child);

            var url = link.GetDynamicUrl?.Invoke() ?? link.Url;
            if (!string.IsNullOrWhiteSpace(url))
                caption.Inlines!.Add(new AvRun { Text = $" ({url})", FontSize = 12 });

            target.Add(caption);
            return;
        }

        var resolvedUrl = link.GetDynamicUrl?.Invoke() ?? link.Url;

        var span = new AvSpan
        {
            Foreground = new SolidColorBrush(Color.Parse("#1565C0")),
            TextDecorations = TextDecorations.Underline,
        };

        foreach (var child in link)
            AddInline(span.Inlines!, child);

        // Inline Run/Span не поддерживают ToolTip как у Control — показываем URL текстом для http(s).
        if (!string.IsNullOrWhiteSpace(resolvedUrl) &&
            resolvedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            span.Inlines!.Add(new AvRun
            {
                Text = $" ({resolvedUrl})",
                Foreground = new SolidColorBrush(Color.Parse("#757575")),
                FontSize = 11,
            });
        }

        target.Add(span);
    }
}
