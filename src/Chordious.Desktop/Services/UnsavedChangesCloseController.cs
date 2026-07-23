// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Avalonia.Controls;

using Chordious.Core.ViewModel;

namespace Chordious.Desktop.Services;

internal sealed class UnsavedChangesCloseController
{
    private readonly Window _window;
    private readonly Func<bool> _isDirty;
    private readonly Func<bool> _applyChangesOnClose;
    private readonly Action _save;
    private bool _closeApproved;
    private bool _promptOpen;

    public UnsavedChangesCloseController(
        Window window,
        Func<bool> isDirty,
        Func<bool> applyChangesOnClose,
        Action save)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _isDirty = isDirty ?? throw new ArgumentNullException(nameof(isDirty));
        _applyChangesOnClose = applyChangesOnClose ?? throw new ArgumentNullException(nameof(applyChangesOnClose));
        _save = save ?? throw new ArgumentNullException(nameof(save));

        _window.Closing += Window_Closing;
    }

    internal static bool RequiresConfirmation(
        bool isDirty,
        bool applyChangesOnClose,
        bool closeApproved)
    {
        return !closeApproved && isDirty && !applyChangesOnClose;
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (!RequiresConfirmation(_isDirty(), _applyChangesOnClose(), _closeApproved))
        {
            return;
        }

        e.Cancel = true;
        if (_promptOpen)
        {
            return;
        }

        _promptOpen = true;
        _ = ConfirmCloseAsync();
    }

    private async Task ConfirmCloseAsync()
    {
        try
        {
            UnsavedChangesChoice choice = await UnsavedChangesDialog.ShowAsync(_window);
            switch (choice)
            {
                case UnsavedChangesChoice.Save:
                    _closeApproved = true;
                    _save();
                    break;
                case UnsavedChangesChoice.Discard:
                    _closeApproved = true;
                    _window.Close();
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
