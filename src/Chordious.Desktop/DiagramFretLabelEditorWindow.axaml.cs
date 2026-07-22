// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;

namespace Chordious.Desktop;

public partial class DiagramFretLabelEditorWindow : Window
{
    private readonly UnsavedChangesCloseController _closeController;

    public DiagramFretLabelEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _closeController = new UnsavedChangesCloseController(
            this,
            () => (DataContext as DiagramFretLabelEditorViewModel)?.Dirty == true,
            () => (DataContext as DiagramFretLabelEditorViewModel)?.ApplyChangesOnClose == true,
            () => (DataContext as DiagramFretLabelEditorViewModel)?.Accept.Execute(null));
    }

    internal static bool RequiresUnsavedChangesConfirmation(
        DiagramFretLabelEditorViewModel? viewModel,
        bool closeApproved)
    {
        return viewModel is not null &&
            UnsavedChangesCloseController.RequiresConfirmation(
                viewModel.Dirty,
                viewModel.ApplyChangesOnClose,
                closeApproved);
    }
}
