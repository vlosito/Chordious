// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;

namespace Chordious.Desktop;

public partial class DiagramBarreEditorWindow : Window
{
    private readonly UnsavedChangesCloseController _closeController;

    public DiagramBarreEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _closeController = new UnsavedChangesCloseController(
            this,
            () => (DataContext as DiagramBarreEditorViewModel)?.Dirty == true,
            () => (DataContext as DiagramBarreEditorViewModel)?.ApplyChangesOnClose == true,
            () => (DataContext as DiagramBarreEditorViewModel)?.Accept.Execute(null));
    }

    internal static bool RequiresUnsavedChangesConfirmation(
        DiagramBarreEditorViewModel? viewModel,
        bool closeApproved)
    {
        return viewModel is not null &&
            UnsavedChangesCloseController.RequiresConfirmation(
                viewModel.Dirty,
                viewModel.ApplyChangesOnClose,
                closeApproved);
    }
}
