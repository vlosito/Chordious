// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;

namespace Chordious.Desktop;

public partial class DiagramEditorWindow : Window
{
    private readonly UnsavedChangesCloseController _closeController;

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
}
