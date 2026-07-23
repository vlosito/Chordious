// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Chordious.Core.ViewModel;

namespace Chordious.Desktop;

public partial class InstrumentManagerWindow : Window
{
    public InstrumentManagerWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InstrumentList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        ExecuteEditInstrument();
        e.Handled = true;
    }

    private void InstrumentList_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space)
        {
            ExecuteEditInstrument();
            e.Handled = true;
        }
    }

    private void TuningList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        ExecuteEditTuning();
        e.Handled = true;
    }

    private void TuningList_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space)
        {
            ExecuteEditTuning();
            e.Handled = true;
        }
    }

    private void ExecuteEditInstrument()
    {
        if (DataContext is InstrumentManagerViewModel viewModel &&
            viewModel.EditInstrument.CanExecute(null))
        {
            viewModel.EditInstrument.Execute(null);
        }
    }

    private void ExecuteEditTuning()
    {
        if (DataContext is InstrumentManagerViewModel viewModel &&
            viewModel.EditTuning.CanExecute(null))
        {
            viewModel.EditTuning.Execute(null);
        }
    }
}
