using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections;
using System.Linq;

namespace MedProject.Views.TabViews;

public partial class FormView : UserControl
{
    private bool _suppressNumericFilter;

    public FormView()
    {
        InitializeComponent();
    }

    private static bool IsEditableAndVisible(DataGridColumn? col)
        => col is not null && !col.IsReadOnly && col.IsVisible;

    private static DataGridColumn? FindFirstEditableColumn(DataGrid grid)
        => grid.Columns.FirstOrDefault(IsEditableAndVisible);

    private static DataGridColumn? FindLastEditableColumn(DataGrid grid)
        => grid.Columns.Reverse().FirstOrDefault(IsEditableAndVisible);

    private static DataGridColumn? ColumnFromDisplayIndex(DataGrid grid, int displayIndex)
        => grid.Columns.FirstOrDefault(c => c.DisplayIndex == displayIndex);

    private static DataGridColumn? FindNextEditableColumn(DataGrid grid, int startDisplayIndex)
    {
        for (var di = startDisplayIndex + 1; di < grid.Columns.Count; di++)
        {
            var col = ColumnFromDisplayIndex(grid, di);
            if (IsEditableAndVisible(col))
                return col;
        }
        return FindFirstEditableColumn(grid);
    }

    private static DataGridColumn? FindPreviousEditableColumn(DataGrid grid, int startDisplayIndex)
    {
        for (var di = startDisplayIndex - 1; di >= 0; di--)
        {
            var col = ColumnFromDisplayIndex(grid, di);
            if (IsEditableAndVisible(col))
                return col;
        }
        return FindLastEditableColumn(grid);
    }

    private static void FocusAndSelectEditorIfPossible(Control? scope)
    {
        if (scope is null)
            return;

        // После BeginEdit редактор появляется не сразу — делаем это асинхронно.
        Dispatcher.UIThread.Post(() =>
        {
            var tb = scope.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.IsFocused)
                     ?? scope.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
            if (tb is null)
                return;

            tb.Focus();
            tb.SelectAll();
        }, DispatcherPriority.Background);
    }

    private void NumericOnlyTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressNumericFilter)
            return;

        if (sender is not TextBox tb)
            return;

        var text = tb.Text ?? string.Empty;

        // Разрешаем только цифры. Пустая строка допустима (nullable int).
        var filtered = new string(text.Where(char.IsDigit).ToArray());
        if (filtered == text)
            return;

        var oldCaret = tb.CaretIndex;
        if (oldCaret < 0) oldCaret = 0;
        if (oldCaret > text.Length) oldCaret = text.Length;

        // Сдвигаем каретку так, чтобы она осталась "на том же месте" относительно цифр.
        var newCaret = text.Take(oldCaret).Count(char.IsDigit);

        try
        {
            _suppressNumericFilter = true;
            tb.Text = filtered;
            tb.CaretIndex = Math.Clamp(newCaret, 0, filtered.Length);
        }
        finally
        {
            _suppressNumericFilter = false;
        }
    }

    private void ReportDataGrid_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;

        if (e.KeyModifiers != KeyModifiers.None)
            return;

        // Backspace/Delete оставляем стандартному редактору.
        if (e.Key is Key.Back or Key.Delete)
            return;

        if (grid.ItemsSource is not IList list || list.Count <= 0)
            return;

        // ВАЖНО: CommitEdit делаем только для навигационных клавиш.
        // Если коммитить на любую клавишу (включая цифры), ввод "съедается" и фокус кажется пропавшим.
        if (e.Key is not (Key.Enter or Key.Left or Key.Right or Key.Up or Key.Down))
            return;

        e.Handled = HandleGridNavigation(grid, list, e.Key);
    }

    private bool HandleGridNavigation(DataGrid grid, IList list, Key key)
    {
        try { grid.CommitEdit(DataGridEditingUnit.Cell, true); } catch { }

        var currentRowIndex = grid.SelectedIndex >= 0
            ? grid.SelectedIndex
            : (grid.SelectedItem is null ? 0 : list.IndexOf(grid.SelectedItem));
        if (currentRowIndex < 0) currentRowIndex = 0;
        if (currentRowIndex >= list.Count) currentRowIndex = list.Count - 1;

        var currentDisplayIndex = grid.CurrentColumn?.DisplayIndex ?? 0;

        int nextRowIndex = currentRowIndex;
        int nextDisplayIndex = currentDisplayIndex;

        switch (key)
        {
            case Key.Enter:
                nextRowIndex = Math.Min(currentRowIndex + 1, list.Count - 1);
                break;
            case Key.Left:
                if (nextDisplayIndex > 0) nextDisplayIndex--;
                else if (nextRowIndex > 0) { nextRowIndex--; nextDisplayIndex = grid.Columns.Count - 1; }
                break;
            case Key.Right:
                if (nextDisplayIndex < grid.Columns.Count - 1) nextDisplayIndex++;
                else if (nextRowIndex < list.Count - 1) { nextRowIndex++; nextDisplayIndex = 0; }
                break;
            case Key.Up:
                if (nextRowIndex > 0) nextRowIndex--;
                break;
            case Key.Down:
                if (nextRowIndex < list.Count - 1) nextRowIndex++;
                break;
            default:
                return false;
        }

        var targetItem = list[nextRowIndex];
        var candidate = ColumnFromDisplayIndex(grid, nextDisplayIndex);

        DataGridColumn? targetColumn;
        if (IsEditableAndVisible(candidate))
        {
            targetColumn = candidate;
        }
        else
        {
            targetColumn = key switch
            {
                Key.Left => FindPreviousEditableColumn(grid, nextDisplayIndex),
                Key.Right => FindNextEditableColumn(grid, nextDisplayIndex),
                Key.Up or Key.Down or Key.Enter => FindNextEditableColumn(grid, nextDisplayIndex) ?? FindPreviousEditableColumn(grid, nextDisplayIndex),
                _ => null
            };
        }

        // Если редактируемых колонок нет — просто прокручиваем строку.
        if (targetColumn is null)
        {
            grid.SelectedIndex = nextRowIndex;
            grid.ScrollIntoView(targetItem, null);
            return true;
        }

        grid.SelectedIndex = nextRowIndex;
        grid.CurrentColumn = targetColumn;
        grid.ScrollIntoView(targetItem, targetColumn);
        grid.BeginEdit();
        FocusAndSelectEditorIfPossible(grid);

        return true;
    }

    private void ReportDataGrid_OnPreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
    {
        // После начала редактирования фокус/редактор появляется не сразу.
        // Делаем мягко: попробуем сфокусировать и выделить текст в редакторе.
        if (sender is Control scope)
            FocusAndSelectEditorIfPossible(scope);
        else
            FocusAndSelectEditorIfPossible(this);
    }

    private void ReportDataGrid_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;

        if (grid.IsReadOnly)
            return;

        if (!e.GetCurrentPoint(grid).Properties.IsLeftButtonPressed)
            return;

        // Определяем, по какой ячейке кликнули, и выставляем CurrentColumn.
        var source = e.Source as Visual;
        var cell = source?.FindAncestorOfType<DataGridCell>();
        var row = source?.FindAncestorOfType<DataGridRow>();
        if (row is null || cell is null)
            return;

        var cells = row.GetVisualDescendants()
            .OfType<DataGridCell>()
            .OrderBy(c => c.Bounds.X)
            .ToList();

        var displayIndex = cells.IndexOf(cell);
        if (displayIndex < 0)
            return;

        var col = ColumnFromDisplayIndex(grid, displayIndex);
        if (!IsEditableAndVisible(col))
            return;

        grid.SelectedItem = row.DataContext;
        grid.CurrentColumn = col;

        // Даем гриду обработать клик/выбор, потом включаем редактирование.
        Dispatcher.UIThread.Post(() =>
        {
            grid.ScrollIntoView(grid.SelectedItem, grid.CurrentColumn);
            try { grid.BeginEdit(); } catch { }
            FocusAndSelectEditorIfPossible(grid);
        }, DispatcherPriority.Background);

        e.Handled = true;
    }

    private void EditorTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.None)
            return;

        if (e.Key is not (Key.Enter or Key.Left or Key.Right or Key.Up or Key.Down))
            return;

        if (sender is not Control editor)
            return;

        var grid = editor.FindAncestorOfType<DataGrid>();
        if (grid is null)
            return;

        if (grid.ItemsSource is not IList list || list.Count <= 0)
            return;

        // Перехватываем стрелки в редакторе (TextBox), чтобы они перемещали по ячейкам,
        // а не только двигали каретку внутри TextBox.
        e.Handled = HandleGridNavigation(grid, list, e.Key);
    }
}
