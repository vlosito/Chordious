// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using Chordious.Core.ViewModel;

namespace Chordious.Desktop.Services;

internal sealed class DesktopAppView : IAppView
{
    private const string AppConfigResourceName = "Chordious.Desktop.Chordious.Desktop.xml";
    private const string UserConfigFileName = "Chordious.User.xml";

    private MainWindow? _mainWindow;
    private DesktopMessageHandlers? _messageHandlers;
    private bool _closed;

    public string UserConfigPath { get; }

    public DesktopAppView()
    {
        string applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string configDirectory = Path.Combine(applicationData, "Chordious");
        Directory.CreateDirectory(configDirectory);
        UserConfigPath = Path.Combine(configDirectory, UserConfigFileName);
    }

    public void Initialize()
    {
        Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(App).Assembly;
        AppViewModel.Init(assembly, this, UserConfigPath);

        AppViewModel.Instance.LoadAppConfig();

        if (!File.Exists(UserConfigPath))
        {
            AppViewModel.Instance.SaveUserConfig();
        }

        AppViewModel.Instance.LoadUserConfig(RecoverInvalidUserConfig);
    }

    public void Attach(MainWindow mainWindow)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        _messageHandlers = new DesktopMessageHandlers(mainWindow);
    }

    public void Close()
    {
        if (_closed)
        {
            return;
        }

        _messageHandlers?.Dispose();
        _messageHandlers = null;
        AppViewModel.Instance.Close();
        _closed = true;
    }

    public void DoOnUIThread(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.InvokeAsync(action).GetAwaiter().GetResult();
    }

    public object DoOnUIThread(Func<object> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (Dispatcher.UIThread.CheckAccess())
        {
            return func();
        }

        return Dispatcher.UIThread.InvokeAsync(func).GetAwaiter().GetResult();
    }

    public Stream GetAppConfigStream()
    {
        return typeof(App).Assembly.GetManifestResourceStream(AppConfigResourceName)
            ?? throw new InvalidOperationException($"Embedded configuration {AppConfigResourceName} was not found.");
    }

    public Stream GetUserConfigStreamToRead()
    {
        return new FileStream(UserConfigPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public Stream GetUserConfigStreamToWrite()
    {
        return new FileStream(UserConfigPath, FileMode.Create, FileAccess.Write, FileShare.None);
    }

    public object SvgTextToImage(string svgText, int width, int height, bool editMode)
    {
        byte[] png = SvgRasterizer.RenderPng(svgText, width, height);
        using MemoryStream stream = new(png);
        return new Bitmap(stream);
    }

    public void TextToClipboard(string text)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            if (_mainWindow?.Clipboard is not null)
            {
                await _mainWindow.Clipboard.SetTextAsync(text);
            }
        });
    }

    public void DiagramToClipboard(ObservableDiagram diagram, float scaleFactor)
    {
        ArgumentNullException.ThrowIfNull(diagram);

        int width = Math.Max(1, (int)Math.Ceiling(diagram.TotalWidth * scaleFactor));
        int height = Math.Max(1, (int)Math.Ceiling(diagram.TotalHeight * scaleFactor));
        byte[] png = SvgRasterizer.RenderPng(diagram.SvgText, width, height);

        Dispatcher.UIThread.Post(async () =>
        {
            if (_mainWindow?.Clipboard is null)
            {
                return;
            }

            using MemoryStream stream = new(png);
            using Bitmap bitmap = new(stream);
            await _mainWindow.Clipboard.SetBitmapAsync(bitmap);
        });
    }

    public IEnumerable<string> GetSystemFonts()
    {
        return FontManager.Current.SystemFonts
            .Select(font => font.Name)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase);
    }

    private void RecoverInvalidUserConfig(Exception exception)
    {
        string backupPath = $"{UserConfigPath}.{DateTime.UtcNow:yyyy.MM.dd.HH.mm.ss}.invalid";

        if (File.Exists(UserConfigPath))
        {
            File.Move(UserConfigPath, backupPath);
        }

        AppViewModel.Instance.ResetUserConfig();
        AppViewModel.Instance.SaveUserConfig();

        System.Diagnostics.Trace.TraceError(
            "Invalid user configuration was moved to {0}: {1}",
            backupPath,
            exception);
    }
}
