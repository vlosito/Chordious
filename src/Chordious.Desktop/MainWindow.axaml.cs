// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Controls.Selection;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

using Chordious.Desktop.ViewModels;
using Chordious.Desktop.Services;
using Chordious.Core.ViewModel;

namespace Chordious.Desktop;

public partial class MainWindow : Window
{
    private readonly DragGestureTracker _libraryNodeDragGesture = new();
    private readonly DragGestureTracker _libraryDiagramDragGesture = new();

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    internal void InitializeViewModel()
    {
        DataContext = new MainWindowViewModel(SaveSvgAsync);
    }

    private void DiagramsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.SetSelectedLibraryDiagrams(
            listBox.SelectedItems?.OfType<ObservableDiagram>() ?? Enumerable.Empty<ObservableDiagram>(),
            listBox.SelectedItem as ObservableDiagram);
    }

    private void LibraryNodesListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedLibraryNode = listBox.SelectedItem as ObservableDiagramLibraryNode;
        }
    }

    private void LibraryNode_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control)
        {
            _libraryNodeDragGesture.Track(control, e);
        }
    }

    private void LibraryDiagram_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control)
        {
            _libraryDiagramDragGesture.Track(control, e);
        }
    }

    private async void Window_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_libraryDiagramDragGesture.TryTakeTrigger(
                e,
                out Control? diagramSource,
                out PointerPressedEventArgs? diagramTrigger) &&
            diagramSource is { DataContext: ObservableDiagram diagram } &&
            diagramTrigger is not null &&
            DataContext is MainWindowViewModel viewModel &&
            viewModel.SelectedLibraryNode is { } sourceNode)
        {
            if (!sourceNode.SelectedDiagrams.Contains(diagram))
            {
                viewModel.SetSelectedLibraryDiagrams([diagram], diagram);
            }

            await StartLibraryDragAsync(
                diagramSource,
                diagramTrigger,
                new DiagramLibraryDragPayload(
                    sourceNode,
                    UseSelectedDiagrams: true));
            e.Handled = true;
            return;
        }

        if (_libraryNodeDragGesture.TryTakeTrigger(
                e,
                out Control? nodeSource,
                out PointerPressedEventArgs? nodeTrigger) &&
            nodeSource is
            {
                DataContext: ObservableDiagramLibraryNode sourceLibraryNode
            } &&
            nodeTrigger is not null)
        {
            await StartLibraryDragAsync(
                nodeSource,
                nodeTrigger,
                new DiagramLibraryDragPayload(
                    sourceLibraryNode,
                    UseSelectedDiagrams: false));
            e.Handled = true;
        }
    }

    private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _libraryNodeDragGesture.Reset();
        _libraryDiagramDragGesture.Reset();
    }

    private static async Task StartLibraryDragAsync(
        Control source,
        PointerPressedEventArgs trigger,
        DiagramLibraryDragPayload payload)
    {
        try
        {
            DataTransfer dragData =
                await DiagramDragDropService.CreateLibraryDataAsync(source, payload);
            await DragDrop.DoDragDropAsync(
                trigger,
                dragData,
                DragDropEffects.Copy | DragDropEffects.Move);
        }
        catch (System.Exception ex)
        {
            ExceptionUtils.HandleException(ex);
        }
    }

    private void LibraryDropTarget_DragOver(object? sender, DragEventArgs e)
    {
        DiagramLibraryDragPayload? payload =
            DiagramDragDropService.TryGetLibraryPayload(e.DataTransfer);
        e.DragEffects = payload is null
            ? DragDropEffects.None
            : DiagramDragDropService.GetDragDropEffect(
                DiagramDragDropService.GetLibraryDropAction(e.KeyModifiers));
        e.Handled = true;
    }

    private void LibraryNode_Drop(object? sender, DragEventArgs e)
    {
        ObservableDiagramLibraryNode? destinationNode =
            (sender as Control)?.DataContext as ObservableDiagramLibraryNode;
        ApplyLibraryDrop(e, destinationNode);
    }

    private void LibraryNodesListBox_Drop(object? sender, DragEventArgs e)
    {
        ApplyLibraryDrop(e, destinationNode: null);
    }

    private void DiagramsListBox_Drop(object? sender, DragEventArgs e)
    {
        ObservableDiagramLibraryNode? destinationNode =
            (DataContext as MainWindowViewModel)?.SelectedLibraryNode;
        ApplyLibraryDrop(e, destinationNode);
    }

    private static void ApplyLibraryDrop(
        DragEventArgs e,
        ObservableDiagramLibraryNode? destinationNode)
    {
        try
        {
            DiagramLibraryDragPayload? payload =
                DiagramDragDropService.TryGetLibraryPayload(e.DataTransfer);
            if (payload is null)
            {
                e.DragEffects = DragDropEffects.None;
                return;
            }

            DiagramLibraryDropAction action =
                DiagramDragDropService.GetLibraryDropAction(e.KeyModifiers);
            bool applied = DiagramDragDropService.ApplyLibraryDrop(
                payload,
                destinationNode,
                action);
            e.DragEffects = applied
                ? DiagramDragDropService.GetDragDropEffect(action)
                : DragDropEffects.None;
        }
        catch (System.Exception ex)
        {
            e.DragEffects = DragDropEffects.None;
            ExceptionUtils.HandleException(ex);
        }
        finally
        {
            e.Handled = true;
        }
    }

    private async Task<string?> SaveSvgAsync(string svgText)
    {
        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Chordious diagram",
            SuggestedFileName = "Chordious-C.svg",
            DefaultExtension = "svg",
            FileTypeChoices =
            [
                new FilePickerFileType("Scalable Vector Graphics")
                {
                    Patterns = ["*.svg"],
                    MimeTypes = ["image/svg+xml"]
                }
            ]
        });

        if (file is null)
        {
            return null;
        }

        await using Stream stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        await using StreamWriter writer = new(stream, new UTF8Encoding(false));
        await writer.WriteAsync(svgText);

        return file.Path.LocalPath;
    }
}
