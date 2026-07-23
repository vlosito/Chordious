// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Chordious.Desktop;

public partial class DiagramCollectionSelectorWindow : Window
{
    public DiagramCollectionSelectorWindow()
    {
        AvaloniaXamlLoader.Load(this);
        Opened += Window_Opened;
    }

    private void Window_Opened(object? sender, EventArgs e)
    {
        ComboBox collectionName = this.FindControl<ComboBox>("CollectionNameComboBox")!;
        collectionName.Focus();
    }
}
