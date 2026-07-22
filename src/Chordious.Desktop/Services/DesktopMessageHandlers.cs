// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Layout;

using CommunityToolkit.Mvvm.Messaging;

using Chordious.Core.ViewModel;

namespace Chordious.Desktop.Services;

internal sealed class DesktopMessageHandlers : IDisposable
{
    private readonly Window _owner;
    private bool _disposed;

    public DesktopMessageHandlers(Window owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));

        StrongReferenceMessenger.Default.Register<ChordiousMessage>(this, (_, message) =>
            _ = ShowInformationAsync(message));
        StrongReferenceMessenger.Default.Register<ExceptionMessage>(this, (_, message) =>
            _ = ShowExceptionAsync(message));
        StrongReferenceMessenger.Default.Register<ConfirmationMessage>(this, (_, message) =>
            _ = ShowConfirmationAsync(message));
        StrongReferenceMessenger.Default.Register<PromptForTextMessage>(this, (_, message) =>
            _ = ShowTextPromptAsync(message));
        StrongReferenceMessenger.Default.Register<ShowDiagramEditorMessage>(this, (_, message) =>
            _ = ShowDiagramEditorAsync(message));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StrongReferenceMessenger.Default.UnregisterAll(this);
        _disposed = true;
    }

    private async Task ShowInformationAsync(ChordiousMessage message)
    {
        InformationViewModel vm = message.InformationVM;
        Window dialog = CreateDialog(vm.Title, new TextBlock
        {
            Text = vm.Message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        Button accept = CreateButton("OK");
        accept.Click += (_, _) => dialog.Close();
        AddButtons(dialog, accept);

        await dialog.ShowDialog(_owner);
        message.Process();
    }

    private async Task ShowExceptionAsync(ExceptionMessage message)
    {
        ExceptionViewModel vm = message.ExceptionVM;
        StackPanel content = new() { Spacing = 10 };
        content.Children.Add(new TextBlock
        {
            Text = vm.Message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });
        content.Children.Add(new TextBox
        {
            Text = vm.Details,
            IsReadOnly = true,
            AcceptsReturn = true,
            Height = 160,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        Window dialog = CreateDialog(ExceptionViewModel.Title, content, 560);
        Button accept = CreateButton("OK");
        accept.Click += (_, _) => dialog.Close();
        AddButtons(dialog, accept);

        await dialog.ShowDialog(_owner);
    }

    private async Task ShowConfirmationAsync(ConfirmationMessage message)
    {
        ConfirmationViewModel vm = message.ConfirmationVM;
        if (!vm.DisplayDialog)
        {
            message.Process();
            PersistUserConfig();
            return;
        }

        Window dialog = CreateDialog(ConfirmationViewModel.Title, new TextBlock
        {
            Text = vm.Message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        vm.RequestClose += dialog.Close;

        Button reject = CreateButton("Não");
        reject.Command = vm.Reject;
        Button accept = CreateButton("Sim");
        accept.Command = vm.Accept;

        if (vm.ShowAcceptAndRemember)
        {
            Button acceptAndRemember = CreateButton("Sim e lembrar");
            acceptAndRemember.Command = vm.AcceptAndRemember;
            AddButtons(dialog, reject, acceptAndRemember, accept);
        }
        else
        {
            AddButtons(dialog, reject, accept);
        }

        await dialog.ShowDialog(_owner);
        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowTextPromptAsync(PromptForTextMessage message)
    {
        TextPromptViewModel vm = message.TextPromptVM;
        TextBox textBox = new()
        {
            Text = vm.Text ?? string.Empty,
            MinWidth = 360
        };

        StackPanel content = new() { Spacing = 10 };
        content.Children.Add(new TextBlock
        {
            Text = vm.Prompt,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });
        content.Children.Add(textBox);

        Window dialog = CreateDialog(TextPromptViewModel.Title, content);
        vm.RequestClose += dialog.Close;

        Button cancel = CreateButton("Cancelar");
        cancel.Command = vm.Cancel;
        Button accept = CreateButton("OK");
        accept.Command = vm.Accept;
        accept.IsEnabled = vm.Accept.CanExecute(null);

        textBox.TextChanged += (_, _) =>
        {
            vm.Text = textBox.Text ?? string.Empty;
            accept.IsEnabled = vm.Accept.CanExecute(null);
        };

        AddButtons(dialog, cancel, accept);
        dialog.Opened += (_, _) => textBox.Focus();

        await dialog.ShowDialog(_owner);
        vm.RequestClose -= dialog.Close;
        PersistUserConfig();
    }

    private async Task ShowDiagramEditorAsync(ShowDiagramEditorMessage message)
    {
        DiagramEditorViewModel vm = new(message.Diagram, message.IsNew);
        message.DiagramEditorVM = vm;

        DiagramEditorWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await dialog.ShowDialog(_owner);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private static Window CreateDialog(string title, Control content, double width = 460)
    {
        Grid grid = new()
        {
            Margin = new Avalonia.Thickness(20),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 18
        };
        grid.Children.Add(content);

        return new Window
        {
            Title = title,
            Width = width,
            SizeToContent = SizeToContent.Height,
            MinHeight = 160,
            MaxHeight = 560,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = grid
        };
    }

    private static void AddButtons(Window dialog, params Button[] buttons)
    {
        Grid grid = (Grid)dialog.Content!;
        StackPanel panel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        Grid.SetRow(panel, 1);

        foreach (Button button in buttons)
        {
            panel.Children.Add(button);
        }

        grid.Children.Add(panel);
    }

    private static Button CreateButton(string label)
    {
        return new Button
        {
            Content = label,
            MinWidth = 88,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
    }

    private static void PersistUserConfig()
    {
        AppViewModel.Instance.SaveUserConfig();
    }
}
