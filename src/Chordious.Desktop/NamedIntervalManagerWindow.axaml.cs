// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Chordious.Core.ViewModel;

namespace Chordious.Desktop;

public partial class NamedIntervalManagerWindow : Window
{
    public NamedIntervalManagerWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void NamedIntervalList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        ExecuteEdit();
        e.Handled = true;
    }

    private void NamedIntervalList_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space)
        {
            ExecuteEdit();
            e.Handled = true;
        }
    }

    private void ExecuteEdit()
    {
        if (DataContext is NamedIntervalManagerViewModel viewModel &&
            viewModel.EditNamedInterval.CanExecute(null))
        {
            viewModel.EditNamedInterval.Execute(null);
        }
    }
}
