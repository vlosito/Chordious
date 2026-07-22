// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Chordious.Desktop.Services;

internal enum UnsavedChangesChoice
{
    Cancel,
    Discard,
    Save
}

internal static class UnsavedChangesDialog
{
    public static async Task<UnsavedChangesChoice> ShowAsync(Window owner)
    {
        Grid content = new()
        {
            Margin = new Thickness(20),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 20
        };
        content.Children.Add(new TextBlock
        {
            Text = "Há alterações não salvas. Deseja salvá-las antes de fechar?",
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 440
        });

        StackPanel buttons = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        Grid.SetRow(buttons, 1);

        Window dialog = new()
        {
            Title = "Alterações não salvas",
            Width = 500,
            MinHeight = 150,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = content
        };

        Button cancel = new() { Content = "Cancelar", MinWidth = 90 };
        cancel.Click += (_, _) => dialog.Close(UnsavedChangesChoice.Cancel);

        Button discard = new() { Content = "Não salvar", MinWidth = 100 };
        discard.Click += (_, _) => dialog.Close(UnsavedChangesChoice.Discard);

        Button save = new() { Content = "Salvar", MinWidth = 90 };
        save.Click += (_, _) => dialog.Close(UnsavedChangesChoice.Save);

        buttons.Children.Add(cancel);
        buttons.Children.Add(discard);
        buttons.Children.Add(save);
        content.Children.Add(buttons);

        return await dialog.ShowDialog<UnsavedChangesChoice>(owner);
    }
}
