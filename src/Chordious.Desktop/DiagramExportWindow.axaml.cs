// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Chordious.Desktop.ViewModels;

namespace Chordious.Desktop;

public partial class DiagramExportWindow : Window
{
    public DiagramExportWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is DiagramExportViewModel viewModel && !viewModel.IsIdle)
        {
            e.Cancel = true;
            viewModel.CancelOrClose.Execute(null);
        }

        base.OnClosing(e);
    }
}
