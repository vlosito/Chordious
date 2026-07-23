// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.IO;

using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Chordious.Desktop.ViewModels;

internal sealed class OptionsViewModel : Chordious.Core.ViewModel.OptionsViewModel
{
    public static string SettingsBackgroundGroupLabel => "Fundos";

    public static string SelectedRenderBackgroundLabel => "Preview normal";

    public static string SelectedRenderBackgroundToolTip =>
        "Fundo usado ao mostrar diagramas fora do editor.";

    public static string SelectedEditorRenderBackgroundLabel => "Editor de diagrama";

    public static string SelectedEditorRenderBackgroundToolTip =>
        "Fundo usado no preview do editor de diagramas.";

    public ObservableCollection<string> RenderBackgrounds { get; } =
        new(["Nenhum", "Branco", "Transparência"]);

    public ObservableCollection<string> EditorRenderBackgrounds => RenderBackgrounds;

    public int SelectedRenderBackgroundIndex
    {
        get => (int)GetBackground("app.renderbackground");
        set
        {
            SetSetting("app.renderbackground", (PreviewBackground)value);
            OnPropertyChanged(nameof(SelectedRenderBackgroundIndex));
        }
    }

    public int SelectedEditorRenderBackgroundIndex
    {
        get => (int)GetBackground("diagrameditor.renderbackground");
        set
        {
            SetSetting("diagrameditor.renderbackground", (PreviewBackground)value);
            OnPropertyChanged(nameof(SelectedEditorRenderBackgroundIndex));
        }
    }

    public static string SettingsIntegrationGroupLabel => "Integração";

    public static string EnhancedCopyLabel => "Cópia e arraste avançados";

    public static string EnhancedCopyToolTip =>
        "Inclui formatos adicionais ao copiar ou arrastar diagramas.";

    public bool EnhancedCopy
    {
        get => bool.TryParse(GetSetting("integration.enhancedcopy"), out bool value) && value;
        set
        {
            SetSetting("integration.enhancedcopy", value);
            OnPropertyChanged(nameof(EnhancedCopy));
        }
    }

    public static string OpenTempFolderLabel => "Abrir arquivos temporários";

    public static string OpenTempFolderToolTip =>
        "Abre no Finder a pasta usada pelos arquivos temporários do Chordious.";

    public RelayCommand OpenTempFolder =>
        _openTempFolder ??= new RelayCommand(() =>
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "Chordious");
                Directory.CreateDirectory(tempPath);
                StrongReferenceMessenger.Default.Send(new LaunchUrlMessage(tempPath));
            }
            catch (Exception ex)
            {
                ExceptionUtils.HandleException(ex);
            }
        });
    private RelayCommand? _openTempFolder;

    public OptionsViewModel()
    {
        IsIdle = true;
    }

    public override void RefreshProperties()
    {
        base.RefreshProperties();
        OnPropertyChanged(nameof(SelectedRenderBackgroundIndex));
        OnPropertyChanged(nameof(SelectedEditorRenderBackgroundIndex));
        OnPropertyChanged(nameof(EnhancedCopy));
    }

    private PreviewBackground GetBackground(string key)
    {
        return Enum.TryParse(GetSetting(key), out PreviewBackground value)
            ? value
            : PreviewBackground.None;
    }
}
