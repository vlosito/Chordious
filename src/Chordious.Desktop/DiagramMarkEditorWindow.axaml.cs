// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;

namespace Chordious.Desktop;

public partial class DiagramMarkEditorWindow : Window
{
    private readonly UnsavedChangesCloseController _closeController;

    public DiagramMarkEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _closeController = new UnsavedChangesCloseController(
            this,
            () => (DataContext as DiagramMarkEditorViewModel)?.Dirty == true,
            () => (DataContext as DiagramMarkEditorViewModel)?.ApplyChangesOnClose == true,
            () => (DataContext as DiagramMarkEditorViewModel)?.Accept.Execute(null));
    }

    internal static bool RequiresUnsavedChangesConfirmation(
        DiagramMarkEditorViewModel? viewModel,
        bool closeApproved)
    {
        return viewModel is not null &&
            UnsavedChangesCloseController.RequiresConfirmation(
                viewModel.Dirty,
                viewModel.ApplyChangesOnClose,
                closeApproved);
    }
}
