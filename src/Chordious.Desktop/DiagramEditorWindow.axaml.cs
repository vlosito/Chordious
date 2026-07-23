// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;

namespace Chordious.Desktop;

public partial class DiagramEditorWindow : Window
{
    private const double DragThreshold = 6;

    private static readonly DataFormat<string> SvgDataFormat =
        DataFormat.CreateStringPlatformFormat(
            OperatingSystem.IsMacOS() ? "public.svg-image" : "image/svg+xml");

    private readonly UnsavedChangesCloseController _closeController;
    private PointerPressedEventArgs? _dragTrigger;
    private Avalonia.Point _dragStart;

    public DiagramEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _closeController = new UnsavedChangesCloseController(
            this,
            () => (DataContext as DiagramEditorViewModel)?.Dirty == true,
            () => (DataContext as DiagramEditorViewModel)?.ApplyChangesOnClose == true,
            () => (DataContext as DiagramEditorViewModel)?.Accept.Execute(null));
    }

    internal static bool RequiresUnsavedChangesConfirmation(
        DiagramEditorViewModel? viewModel,
        bool closeApproved)
    {
        return viewModel is not null &&
            UnsavedChangesCloseController.RequiresConfirmation(
                viewModel.Dirty,
                viewModel.ApplyChangesOnClose,
                closeApproved);
    }

    private void DiagramImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Image image || DataContext is not DiagramEditorViewModel viewModel)
        {
            return;
        }

        UpdateCursorPosition(image, e.GetPosition(image), viewModel.ObservableDiagram);

        if (e.GetCurrentPoint(image).Properties.IsLeftButtonPressed)
        {
            _dragTrigger = e;
            _dragStart = e.GetPosition(image);
        }
    }

    private async void DiagramImage_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Image image ||
            DataContext is not DiagramEditorViewModel viewModel ||
            _dragTrigger is null)
        {
            return;
        }

        PointerPoint currentPoint = e.GetCurrentPoint(image);
        if (!currentPoint.Properties.IsLeftButtonPressed)
        {
            ResetDrag();
            return;
        }

        Avalonia.Point currentPosition = e.GetPosition(image);
        if (!HasExceededDragThreshold(_dragStart, currentPosition))
        {
            return;
        }

        PointerPressedEventArgs trigger = _dragTrigger;
        ResetDrag();

        try
        {
            DataTransfer dragData = await CreateDragDataAsync(image, viewModel.ObservableDiagram);
            await DragDrop.DoDragDropAsync(trigger, dragData, DragDropEffects.Copy);
        }
        catch (Exception ex)
        {
            ExceptionUtils.HandleException(ex);
        }
    }

    private void DiagramImage_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        ResetDrag();
    }

    private void DiagramImage_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Image image || DataContext is not DiagramEditorViewModel viewModel)
        {
            return;
        }

        UpdateCursorPosition(image, e.GetPosition(image), viewModel.ObservableDiagram);
        if (viewModel.ObservableDiagram.EditElement.CanExecute(null))
        {
            viewModel.ObservableDiagram.EditElement.Execute(null);
        }
    }

    internal static void UpdateCursorPosition(
        Image image,
        Avalonia.Point position,
        ObservableDiagram diagram)
    {
        Avalonia.Point mapped = MapCursorPosition(
            position,
            image.Bounds.Size,
            new Avalonia.Size(diagram.TotalWidth, diagram.TotalHeight));

        diagram.CursorX = mapped.X;
        diagram.CursorY = mapped.Y;
    }

    internal static Avalonia.Point MapCursorPosition(
        Avalonia.Point position,
        Avalonia.Size displayedSize,
        Avalonia.Size diagramSize)
    {
        double widthScale = displayedSize.Width > 0
            ? diagramSize.Width / displayedSize.Width
            : 1;
        double heightScale = displayedSize.Height > 0
            ? diagramSize.Height / displayedSize.Height
            : 1;

        return new Avalonia.Point(position.X * widthScale, position.Y * heightScale);
    }

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

    private static async Task<DataTransfer> CreateDragDataAsync(
        Image image,
        ObservableDiagram diagram)
    {
        int width = Math.Max(1, diagram.TotalWidth);
        int height = Math.Max(1, diagram.TotalHeight);
        byte[] png = SvgRasterizer.RenderPng(diagram.SvgText, width, height);

        DataTransferItem diagramItem = new();
        diagramItem.SetText(diagram.SvgText);
        diagramItem.Set(SvgDataFormat, diagram.SvgText);
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
            TopLevel.GetTopLevel(image)?.StorageProvider is { } storageProvider)
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

    private void ResetDrag()
    {
        _dragTrigger = null;
        _dragStart = default;
    }
}
