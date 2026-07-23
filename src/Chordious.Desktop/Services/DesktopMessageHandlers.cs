// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.Messaging;

using Chordious.Core.ViewModel;

namespace Chordious.Desktop.Services;

internal sealed class DesktopMessageHandlers : IDisposable
{
    private readonly Window _owner;
    private Window _dialogOwner;
    private bool _disposed;

    public DesktopMessageHandlers(Window owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _dialogOwner = _owner;

        StrongReferenceMessenger.Default.Register<ChordiousMessage>(this, (_, message) =>
            _ = ShowInformationAsync(message));
        StrongReferenceMessenger.Default.Register<ExceptionMessage>(this, (_, message) =>
            _ = ShowExceptionAsync(message));
        StrongReferenceMessenger.Default.Register<ConfirmationMessage>(this, (_, message) =>
            _ = ShowConfirmationAsync(message));
        StrongReferenceMessenger.Default.Register<PromptForTextMessage>(this, (_, message) =>
            _ = ShowTextPromptAsync(message));
        StrongReferenceMessenger.Default.Register<LaunchUrlMessage>(this, (_, message) =>
            OpenExternalTarget(message));
        StrongReferenceMessenger.Default.Register<ShowChordFinderMessage>(this, (_, message) =>
            _ = ShowChordFinderAsync(message));
        StrongReferenceMessenger.Default.Register<ShowScaleFinderMessage>(this, (_, message) =>
            _ = ShowScaleFinderAsync(message));
        StrongReferenceMessenger.Default.Register<ShowInstrumentManagerMessage>(this, (_, message) =>
            _ = ShowInstrumentManagerAsync(message));
        StrongReferenceMessenger.Default.Register<ShowInstrumentEditorMessage>(this, (_, message) =>
            _ = ShowInstrumentEditorAsync(message));
        StrongReferenceMessenger.Default.Register<ShowTuningEditorMessage>(this, (_, message) =>
            _ = ShowTuningEditorAsync(message));
        StrongReferenceMessenger.Default.Register<ShowChordQualityManagerMessage>(this, (_, message) =>
            _ = ShowChordQualityManagerAsync(message));
        StrongReferenceMessenger.Default.Register<ShowChordQualityEditorMessage>(this, (_, message) =>
            _ = ShowChordQualityEditorAsync(message));
        StrongReferenceMessenger.Default.Register<ShowScaleManagerMessage>(this, (_, message) =>
            _ = ShowScaleManagerAsync(message));
        StrongReferenceMessenger.Default.Register<ShowScaleEditorMessage>(this, (_, message) =>
            _ = ShowScaleEditorAsync(message));
        StrongReferenceMessenger.Default.Register<ShowOptionsMessage>(this, (_, message) =>
            _ = ShowOptionsAsync(message));
        StrongReferenceMessenger.Default.Register<ShowLicensesMessage>(this, (_, message) =>
            _ = ShowLicensesAsync(message));
        StrongReferenceMessenger.Default.Register<ShowAdvancedDataMessage>(this, (_, message) =>
            _ = ShowAdvancedDataAsync(message));
        StrongReferenceMessenger.Default.Register<ShowDiagramEditorMessage>(this, (_, message) =>
            _ = ShowDiagramEditorAsync(message));
        StrongReferenceMessenger.Default.Register<ShowDiagramMarkEditorMessage>(this, (_, message) =>
            _ = ShowDiagramMarkEditorAsync(message));
        StrongReferenceMessenger.Default.Register<ShowDiagramFretLabelEditorMessage>(this, (_, message) =>
            _ = ShowDiagramFretLabelEditorAsync(message));
        StrongReferenceMessenger.Default.Register<ShowDiagramBarreEditorMessage>(this, (_, message) =>
            _ = ShowDiagramBarreEditorAsync(message));
        StrongReferenceMessenger.Default.Register<ShowDiagramStyleEditorMessage>(this, (_, message) =>
            _ = ShowDiagramStyleEditorAsync(message));
        StrongReferenceMessenger.Default.Register<ShowDiagramCollectionSelectorMessage>(this, (_, message) =>
            _ = ShowDiagramCollectionSelectorAsync(message));
        StrongReferenceMessenger.Default.Register<ShowDiagramExportMessage>(this, (_, message) =>
            _ = ShowDiagramExportAsync(message));
        StrongReferenceMessenger.Default.Register<ShowConfigImportMessage>(this, (_, message) =>
            _ = ShowConfigImportAsync(message));
        StrongReferenceMessenger.Default.Register<PromptForConfigInputStreamMessage>(this, (_, message) =>
            _ = PromptForConfigInputStreamAsync(message));
        StrongReferenceMessenger.Default.Register<ShowConfigExportMessage>(this, (_, message) =>
            _ = ShowConfigExportAsync(message));
        StrongReferenceMessenger.Default.Register<PromptForConfigOutputStreamMessage>(this, (_, message) =>
            _ = PromptForConfigOutputStreamAsync(message));
        StrongReferenceMessenger.Default.Register<PromptForLegacyImportMessage>(this, (_, message) =>
            _ = PromptForLegacyImportAsync(message));
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

        await ShowDialogAsync(dialog);
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

        await ShowDialogAsync(dialog);
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

        await ShowDialogAsync(dialog);
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

        await ShowDialogAsync(dialog);
        vm.RequestClose -= dialog.Close;
        PersistUserConfig();
    }

    private static void OpenExternalTarget(LaunchUrlMessage message)
    {
        try
        {
            ProcessStartInfo startInfo;
            if (OperatingSystem.IsMacOS())
            {
                startInfo = new ProcessStartInfo("/usr/bin/open")
                {
                    UseShellExecute = false
                };
                startInfo.ArgumentList.Add(message.Url);
            }
            else
            {
                startInfo = new ProcessStartInfo(message.Url)
                {
                    UseShellExecute = true
                };
            }

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            ExceptionUtils.HandleException(ex);
        }
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

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowChordFinderAsync(ShowChordFinderMessage message)
    {
        ChordFinderViewModel vm = message.ChordFinderVM;
        ChordFinderWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowScaleFinderAsync(ShowScaleFinderMessage message)
    {
        ScaleFinderViewModel vm = message.ScaleFinderVM;
        ScaleFinderWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowInstrumentManagerAsync(ShowInstrumentManagerMessage message)
    {
        InstrumentManagerViewModel vm = message.InstrumentManagerVM;
        InstrumentManagerWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowInstrumentEditorAsync(ShowInstrumentEditorMessage message)
    {
        InstrumentEditorViewModel vm = message.InstrumentEditorVM;
        InstrumentEditorWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        PersistUserConfig();
    }

    private async Task ShowTuningEditorAsync(ShowTuningEditorMessage message)
    {
        TuningEditorViewModel vm = message.TuningEditorVM;
        TuningEditorWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowChordQualityManagerAsync(ShowChordQualityManagerMessage message)
    {
        ChordQualityManagerViewModel vm = message.ChordQualityManagerVM;
        NamedIntervalManagerWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowChordQualityEditorAsync(ShowChordQualityEditorMessage message)
    {
        ChordQualityEditorViewModel vm = message.ChordQualityEditorVM;
        ChordQualityEditorWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        PersistUserConfig();
    }

    private async Task ShowScaleManagerAsync(ShowScaleManagerMessage message)
    {
        ScaleManagerViewModel vm = message.ScaleManagerVM;
        NamedIntervalManagerWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowScaleEditorAsync(ShowScaleEditorMessage message)
    {
        ScaleEditorViewModel vm = message.ScaleEditorVM;
        ScaleEditorWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        PersistUserConfig();
    }

    private async Task ShowOptionsAsync(ShowOptionsMessage message)
    {
        ViewModels.OptionsViewModel vm = new();
        message.OptionsVM = vm;

        OptionsWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        if (vm.ItemsChanged)
        {
            RefreshMainLibrary();
        }
        PersistUserConfig();
    }

    private async Task ShowLicensesAsync(ShowLicensesMessage message)
    {
        LicensesViewModel vm = message.LicensesVM;
        DesktopLicenseCatalog.AddDesktopDependencies(vm);

        LicensesWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
    }

    private async Task ShowAdvancedDataAsync(ShowAdvancedDataMessage message)
    {
        AdvancedDataViewModel vm = message.AdvancedDataVM;
        AdvancedDataWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowDiagramMarkEditorAsync(ShowDiagramMarkEditorMessage message)
    {
        DiagramMarkEditorViewModel vm = message.DiagramMarkEditorVM;
        DiagramMarkEditorWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowDiagramFretLabelEditorAsync(ShowDiagramFretLabelEditorMessage message)
    {
        DiagramFretLabelEditorViewModel vm = message.DiagramFretLabelEditorVM;
        DiagramFretLabelEditorWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowDiagramBarreEditorAsync(ShowDiagramBarreEditorMessage message)
    {
        DiagramBarreEditorViewModel vm = message.DiagramBarreEditorVM;
        DiagramBarreEditorWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowDiagramStyleEditorAsync(ShowDiagramStyleEditorMessage message)
    {
        DiagramStyleEditorViewModel vm = message.DiagramStyleEditorVM;
        DiagramStyleEditorWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowDiagramCollectionSelectorAsync(ShowDiagramCollectionSelectorMessage message)
    {
        DiagramCollectionSelectorViewModel vm = message.DiagramCollectionSelectorVM;
        DiagramCollectionSelectorWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        if (vm.WasAccepted && _owner.DataContext is ViewModels.MainWindowViewModel mainWindowViewModel)
        {
            mainWindowViewModel.SelectedLibraryNode = null;
            mainWindowViewModel.Library.RefreshNodes();
        }
        PersistUserConfig();
    }

    private async Task ShowDiagramExportAsync(ShowDiagramExportMessage message)
    {
        ViewModels.DiagramExportViewModel vm = new(
            message.DiagramsToExport,
            message.CollectionName,
            ChooseOutputPathAsync);
        message.DiagramExportVM = vm;

        DiagramExportWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task ShowConfigImportAsync(ShowConfigImportMessage message)
    {
        using ConfigImportViewModel vm = message.ConfigImportVM;
        ConfigPartsWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        RefreshMainLibrary();
        PersistUserConfig();
    }

    private async Task ShowConfigExportAsync(ShowConfigExportMessage message)
    {
        ConfigExportViewModel vm = message.ConfigExportVM;
        ConfigPartsWindow dialog = new()
        {
            DataContext = vm
        };
        vm.RequestClose += dialog.Close;

        await ShowDialogAsync(dialog);

        vm.RequestClose -= dialog.Close;
        message.Process();
        PersistUserConfig();
    }

    private async Task PromptForConfigInputStreamAsync(PromptForConfigInputStreamMessage message)
    {
        await Task.Yield();

        IStorageFile? file = (await _dialogOwner.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Importar configuração do Chordious",
                AllowMultiple = false,
                SuggestedStartLocation = await GetSuggestedStartLocationAsync(),
                FileTypeFilter = [ConfigFileType]
            })).FirstOrDefault();

        if (file is null)
        {
            return;
        }

        RememberLastPath(file);
        Stream inputStream = await file.OpenReadAsync();
        try
        {
            message.Process(inputStream);
        }
        catch
        {
            inputStream.Dispose();
            throw;
        }
    }

    private async Task PromptForConfigOutputStreamAsync(PromptForConfigOutputStreamMessage message)
    {
        await Task.Yield();

        IStorageFile? file = await _dialogOwner.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Exportar configuração do Chordious",
                SuggestedFileName = "Chordious.Config.xml",
                DefaultExtension = "xml",
                ShowOverwritePrompt = true,
                SuggestedStartLocation = await GetSuggestedStartLocationAsync(),
                FileTypeChoices = [ConfigFileType]
            });

        if (file is null)
        {
            return;
        }

        RememberLastPath(file);
        Stream outputStream = await file.OpenWriteAsync();
        outputStream.SetLength(0);
        try
        {
            message.Process(outputStream);
        }
        catch
        {
            outputStream.Dispose();
            throw;
        }
    }

    private async Task PromptForLegacyImportAsync(PromptForLegacyImportMessage message)
    {
        await Task.Yield();

        IStorageFile? file = (await _dialogOwner.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Importar documento ChordLine",
                AllowMultiple = false,
                SuggestedStartLocation = await GetSuggestedStartLocationAsync(),
                FileTypeFilter = [ChordLineFileType, FilePickerFileTypes.All]
            })).FirstOrDefault();

        if (file is null)
        {
            return;
        }

        RememberLastPath(file);
        Stream inputStream = await file.OpenReadAsync();
        try
        {
            message.Process(file.Name, inputStream);
        }
        catch
        {
            inputStream.Dispose();
            throw;
        }
    }

    private async Task<string?> ChooseOutputPathAsync(string currentPath)
    {
        IStorageFolder? suggestedFolder = null;
        if (Directory.Exists(currentPath))
        {
            suggestedFolder = await _dialogOwner.StorageProvider.TryGetFolderFromPathAsync(
                new Uri(Path.GetFullPath(currentPath)));
        }

        var folders = await _dialogOwner.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Escolha a pasta para exportar os diagramas",
                AllowMultiple = false,
                SuggestedStartLocation = suggestedFolder
            });

        return folders.FirstOrDefault()?.Path.LocalPath;
    }

    private async Task<IStorageFolder?> GetSuggestedStartLocationAsync()
    {
        string lastPath;
        try
        {
            lastPath = AppViewModel.Instance.GetSetting("app.lastpath");
        }
        catch (Chordious.Core.InheritableDictionaryKeyNotFoundException)
        {
            lastPath = string.Empty;
        }

        if (!Directory.Exists(lastPath))
        {
            lastPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        return Directory.Exists(lastPath)
            ? await _dialogOwner.StorageProvider.TryGetFolderFromPathAsync(
                new Uri(Path.GetFullPath(lastPath)))
            : null;
    }

    private static void RememberLastPath(IStorageItem storageItem)
    {
        try
        {
            string? directory = Path.GetDirectoryName(storageItem.Path.LocalPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                AppViewModel.Instance.SetSetting("app.lastpath", directory);
            }
        }
        catch (Exception)
        {
            // Non-local storage providers do not expose a filesystem path.
        }
    }

    private void RefreshMainLibrary()
    {
        if (_owner.DataContext is ViewModels.MainWindowViewModel mainWindowViewModel)
        {
            mainWindowViewModel.SelectedLibraryNode = null;
            mainWindowViewModel.Library.RefreshNodes();
        }
    }

    private async Task ShowDialogAsync(Window dialog)
    {
        Window previousOwner = _dialogOwner;
        _dialogOwner = dialog;
        try
        {
            await dialog.ShowDialog(previousOwner);
        }
        finally
        {
            _dialogOwner = previousOwner;
        }
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

    private static readonly FilePickerFileType ConfigFileType =
        new("Chordious Config")
        {
            Patterns = ["*.xml"]
        };

    private static readonly FilePickerFileType ChordLineFileType =
        new("ChordLine")
        {
            Patterns = ["*.txt"]
        };
}
