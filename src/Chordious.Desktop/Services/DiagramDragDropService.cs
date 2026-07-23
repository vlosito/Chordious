// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

using Chordious.Core.ViewModel;

namespace Chordious.Desktop.Services;

internal enum DiagramLibraryDropAction
{
    None,
    Copy,
    Move,
}

internal sealed record DiagramLibraryDragPayload(
    ObservableDiagramLibraryNode SourceNode,
    bool UseSelectedDiagrams);

internal static class DiagramDragDropService
{
    private const double DragThreshold = 6;

    private static readonly DataFormat<string> SvgDataFormat =
        DataFormat.CreateStringPlatformFormat(
            OperatingSystem.IsMacOS() ? "public.svg-image" : "image/svg+xml");

    private static readonly DataFormat<DiagramLibraryDragPayload> LibraryPayloadDataFormat =
        DataFormat.CreateInProcessFormat<DiagramLibraryDragPayload>(
            "Chordious.DiagramLibraryDragPayload");

    internal static bool HasExceededDragThreshold(
        Avalonia.Point start,
        Avalonia.Point current,
        double threshold = DragThreshold)
    {
        double deltaX = current.X - start.X;
        double deltaY = current.Y - start.Y;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY)) >= threshold;
    }

    internal static string GetSafeDragFileName(string? title)
    {
        string safeTitle = string.Concat((title ?? string.Empty)
            .Trim()
            .Select(character =>
                character < 32 || "<>:\"/\\|?*".Contains(character)
                    ? '_'
                    : character));

        if (string.IsNullOrWhiteSpace(safeTitle) ||
            safeTitle is "." or "..")
        {
            safeTitle = "diagram";
        }
        else if (safeTitle.Length > 100)
        {
            safeTitle = safeTitle[..100].TrimEnd(' ', '.');
        }

        return safeTitle + ".png";
    }

    internal static async Task<DataTransfer> CreateDiagramDataAsync(
        Control source,
        ObservableDiagram diagram,
        DiagramLibraryDragPayload? libraryPayload = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(diagram);

        int width = Math.Max(1, diagram.TotalWidth);
        int height = Math.Max(1, diagram.TotalHeight);
        byte[] png = SvgRasterizer.RenderPng(diagram.SvgText, width, height);

        DataTransferItem diagramItem = new();
        diagramItem.SetText(diagram.SvgText);
        diagramItem.Set(SvgDataFormat, diagram.SvgText);
        if (libraryPayload is not null)
        {
            diagramItem.Set(LibraryPayloadDataFormat, libraryPayload);
        }

        using (MemoryStream stream = new(png))
        {
            diagramItem.SetBitmap(new Bitmap(stream));
        }

        DataTransfer dragData = new();
        dragData.Add(diagramItem);

        if (bool.TryParse(
                AppViewModel.Instance.GetSetting("integration.enhancedcopy"),
                out bool enhancedCopy) &&
            enhancedCopy &&
            TopLevel.GetTopLevel(source)?.StorageProvider is { } storageProvider)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "Chordious");
            Directory.CreateDirectory(tempDirectory);
            string tempFilePath = Path.Combine(
                tempDirectory,
                GetSafeDragFileName(diagram.Title));
            await File.WriteAllBytesAsync(tempFilePath, png);

            IStorageFile? storageFile =
                await storageProvider.TryGetFileFromPathAsync(tempFilePath);
            if (storageFile is not null)
            {
                dragData.Add(DataTransferItem.CreateFile(storageFile));
            }
        }

        return dragData;
    }

    internal static async Task<DataTransfer> CreateLibraryDataAsync(
        Control source,
        DiagramLibraryDragPayload payload)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(payload);

        ObservableDiagram? diagram = payload.UseSelectedDiagrams
            ? payload.SourceNode.SelectedDiagrams.FirstOrDefault()
            : null;

        if (diagram is not null)
        {
            return await CreateDiagramDataAsync(source, diagram, payload);
        }

        DataTransferItem item = new();
        item.Set(LibraryPayloadDataFormat, payload);

        DataTransfer dragData = new();
        dragData.Add(item);
        return dragData;
    }

    internal static DiagramLibraryDragPayload? TryGetLibraryPayload(
        IDataTransfer dataTransfer)
    {
        ArgumentNullException.ThrowIfNull(dataTransfer);
        return dataTransfer.TryGetValue(LibraryPayloadDataFormat);
    }

    internal static DiagramLibraryDropAction GetLibraryDropAction(
        KeyModifiers modifiers)
    {
        if (modifiers.HasFlag(KeyModifiers.Control) ||
            modifiers.HasFlag(KeyModifiers.Alt))
        {
            return DiagramLibraryDropAction.Copy;
        }

        return DiagramLibraryDropAction.Move;
    }

    internal static DragDropEffects GetDragDropEffect(
        DiagramLibraryDropAction action)
    {
        return action switch
        {
            DiagramLibraryDropAction.Copy => DragDropEffects.Copy,
            DiagramLibraryDropAction.Move => DragDropEffects.Move,
            _ => DragDropEffects.None,
        };
    }

    internal static bool ApplyLibraryDrop(
        DiagramLibraryDragPayload payload,
        ObservableDiagramLibraryNode? destinationNode,
        DiagramLibraryDropAction action)
    {
        ArgumentNullException.ThrowIfNull(payload);

        ObservableDiagramLibraryNode sourceNode = payload.SourceNode;
        if (ReferenceEquals(sourceNode, destinationNode))
        {
            if (action == DiagramLibraryDropAction.Copy &&
                payload.UseSelectedDiagrams &&
                sourceNode.CloneSelected.CanExecute(null))
            {
                sourceNode.CloneSelected.Execute(null);
                return true;
            }

            return false;
        }

        string? destinationName = destinationNode?.Name;
        if (action == DiagramLibraryDropAction.Copy)
        {
            if (payload.UseSelectedDiagrams)
            {
                if (!sourceNode.CopySelected.CanExecute(destinationName))
                {
                    return false;
                }

                sourceNode.CopySelected.Execute(destinationName);
                return true;
            }

            if (!sourceNode.CopyNode.CanExecute(destinationName))
            {
                return false;
            }

            sourceNode.CopyNode.Execute(destinationName);
            return true;
        }

        if (action == DiagramLibraryDropAction.Move)
        {
            if (payload.UseSelectedDiagrams)
            {
                if (!sourceNode.MoveSelected.CanExecute(destinationName))
                {
                    return false;
                }

                sourceNode.MoveSelected.Execute(destinationName);
                return true;
            }

            if (!sourceNode.MergeNode.CanExecute(destinationName))
            {
                return false;
            }

            sourceNode.MergeNode.Execute(destinationName);
            return true;
        }

        return false;
    }
}

internal sealed class DragGestureTracker
{
    private Control? _source;
    private PointerPressedEventArgs? _trigger;
    private Avalonia.Point _start;

    internal void Track(Control source, PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(e);

        if (e.GetCurrentPoint(source).Properties.IsLeftButtonPressed)
        {
            _source = source;
            _trigger = e;
            _start = e.GetPosition(source);
        }
    }

    internal bool TryTakeTrigger(
        PointerEventArgs e,
        out Control? source,
        out PointerPressedEventArgs? trigger)
    {
        ArgumentNullException.ThrowIfNull(e);

        source = null;
        trigger = null;
        if (_source is null || _trigger is null)
        {
            return false;
        }

        if (!e.GetCurrentPoint(_source).Properties.IsLeftButtonPressed)
        {
            Reset();
            return false;
        }

        if (!DiagramDragDropService.HasExceededDragThreshold(
                _start,
                e.GetPosition(_source)))
        {
            return false;
        }

        source = _source;
        trigger = _trigger;
        _source = null;
        _trigger = null;
        _start = default;
        return true;
    }

    internal void Reset()
    {
        _source = null;
        _trigger = null;
        _start = default;
    }
}
