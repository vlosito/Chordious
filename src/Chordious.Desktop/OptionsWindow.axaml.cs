// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Chordious.Desktop;

public partial class OptionsWindow : Window
{
    public OptionsWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
