// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;
using Chordious.Desktop.ViewModels;

namespace Chordious.Desktop;

public partial class App : Application
{
    private DesktopAppView? _appView;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _appView = new DesktopAppView();
            _appView.Initialize();

            MainWindow mainWindow = new();
            _appView.Attach(mainWindow);
            mainWindow.InitializeViewModel();

            desktop.MainWindow = mainWindow;
            desktop.Exit += (_, _) => _appView.Close();

            AppViewModel.Instance.TryHandleFailedUserConfigLoad();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void AppLicenses_OnClick(object? sender, EventArgs args)
    {
        ExecuteMainWindowCommand(viewModel => viewModel.ShowLicenses.Execute(null));
    }

    private void AppOptions_OnClick(object? sender, EventArgs args)
    {
        ExecuteMainWindowCommand(viewModel => viewModel.ShowOptions.Execute(null));
    }

    private void ExecuteMainWindowCommand(Action<MainWindowViewModel> execute)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
            {
                MainWindow: MainWindow
                {
                    DataContext: MainWindowViewModel viewModel
                } mainWindow
            })
        {
            return;
        }

        mainWindow.Activate();
        execute(viewModel);
    }
}
