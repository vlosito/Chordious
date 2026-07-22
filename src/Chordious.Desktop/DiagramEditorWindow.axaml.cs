// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;

namespace Chordious.Desktop;

public partial class DiagramEditorWindow : Window
{
    private bool _closeApproved;
    private bool _promptOpen;

    public DiagramEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);
        Closing += DiagramEditorWindow_Closing;
    }

    internal static bool RequiresUnsavedChangesConfirmation(
        DiagramEditorViewModel? viewModel,
        bool closeApproved)
    {
        return !closeApproved &&
            viewModel is not null &&
            viewModel.Dirty &&
            !viewModel.ApplyChangesOnClose;
    }

    private void DiagramEditorWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (!RequiresUnsavedChangesConfirmation(DataContext as DiagramEditorViewModel, _closeApproved))
        {
            return;
        }

        e.Cancel = true;
        if (_promptOpen)
        {
            return;
        }

        _promptOpen = true;
        _ = ConfirmCloseAsync((DiagramEditorViewModel)DataContext!);
    }

    private async Task ConfirmCloseAsync(DiagramEditorViewModel viewModel)
    {
        try
        {
            UnsavedChangesChoice choice = await UnsavedChangesDialog.ShowAsync(this);
            switch (choice)
            {
                case UnsavedChangesChoice.Save:
                    _closeApproved = true;
                    viewModel.Accept.Execute(null);
                    break;
                case UnsavedChangesChoice.Discard:
                    _closeApproved = true;
                    Close();
                    break;
            }
        }
        catch (Exception ex)
        {
            ExceptionUtils.HandleException(ex);
        }
        finally
        {
            _promptOpen = false;
        }
    }
}
