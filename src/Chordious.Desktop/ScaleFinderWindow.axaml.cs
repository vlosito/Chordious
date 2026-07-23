// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Chordious.Core.ViewModel;

namespace Chordious.Desktop;

public partial class ScaleFinderWindow : Window
{
    public ScaleFinderWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is ScaleFinderViewModel viewModel)
        {
            viewModel.CancelSearch.Execute(null);
        }

        base.OnClosing(e);
    }

    private void HeaderComboBox_ContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (sender is not ItemsControl itemsControl ||
            e.Container is not ComboBoxItem container ||
            e.Index < 0 ||
            e.Index >= itemsControl.ItemsView.Count ||
            itemsControl.ItemsView[e.Index] is not ObservableHeaderObject item)
        {
            return;
        }

        container.IsEnabled = !item.IsHeader;
        container.FontWeight = item.IsHeader ? FontWeight.SemiBold : FontWeight.Normal;
    }

    private void ResultsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && DataContext is ScaleFinderViewModel viewModel)
        {
            SynchronizeSelection(
                viewModel,
                listBox.SelectedItems?.OfType<ObservableDiagram>() ??
                    Enumerable.Empty<ObservableDiagram>());
        }
    }

    private void ResultImage_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Image { DataContext: ObservableDiagram diagram } &&
            diagram.ShowEditor.CanExecute(null))
        {
            diagram.ShowEditor.Execute(null);
            e.Handled = true;
        }
    }

    internal static void SynchronizeSelection(
        ScaleFinderViewModel viewModel,
        IEnumerable<ObservableDiagram> selectedResults)
    {
        viewModel.SelectedResults.Clear();
        foreach (ObservableDiagram result in selectedResults.Distinct())
        {
            viewModel.SelectedResults.Add(result);
        }
    }
}
