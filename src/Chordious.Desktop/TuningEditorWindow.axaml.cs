// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Chordious.Core.ViewModel;

namespace Chordious.Desktop;

public partial class TuningEditorWindow : Window
{
    public TuningEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Window_Opened(object? sender, System.EventArgs e)
    {
        TextBox nameTextBox = this.FindControl<TextBox>("NameTextBox")!;
        nameTextBox.Focus();

        if (DataContext is TuningEditorViewModel { IsNew: true })
        {
            nameTextBox.SelectAll();
        }
    }
}
