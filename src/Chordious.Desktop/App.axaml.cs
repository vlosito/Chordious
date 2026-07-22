// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;

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
}
