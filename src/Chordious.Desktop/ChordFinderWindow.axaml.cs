// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;

namespace Chordious.Desktop;

public partial class ChordFinderWindow : Window
{
    private readonly DragGestureTracker _resultDragGesture = new();

    public ChordFinderWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is ChordFinderViewModel viewModel)
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
        if (sender is ListBox listBox && DataContext is ChordFinderViewModel viewModel)
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

    private void ResultImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Image image)
        {
            _resultDragGesture.Track(image, e);
        }
    }

    private async void Window_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_resultDragGesture.TryTakeTrigger(
                e,
                out Control? source,
                out PointerPressedEventArgs? trigger) ||
            source is not Image { DataContext: ObservableDiagram diagram } image ||
            trigger is null)
        {
            return;
        }

        try
        {
            DataTransfer dragData =
                await DiagramDragDropService.CreateDiagramDataAsync(image, diagram);
            await DragDrop.DoDragDropAsync(
                trigger,
                dragData,
                DragDropEffects.Copy | DragDropEffects.Move);
            e.Handled = true;
        }
        catch (System.Exception ex)
        {
            ExceptionUtils.HandleException(ex);
        }
    }

    private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _resultDragGesture.Reset();
    }

    internal static void SynchronizeSelection(
        ChordFinderViewModel viewModel,
        IEnumerable<ObservableDiagram> selectedResults)
    {
        viewModel.SelectedResults.Clear();
        foreach (ObservableDiagram result in selectedResults.Distinct())
        {
            viewModel.SelectedResults.Add(result);
        }
    }
}
