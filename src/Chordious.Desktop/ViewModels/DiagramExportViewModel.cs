// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;

namespace Chordious.Desktop.ViewModels;

internal sealed class DiagramExportViewModel : DiagramExportViewModelBase
{
    private static readonly string[] DefaultFilenameFormats =
    [
        "%t.%x",
        "%1.%x",
        "diagram (%1 of %#).%x",
        $"%c{Path.DirectorySeparatorChar}%t.%x",
        $"%c{Path.DirectorySeparatorChar}%1.%x",
        $"%c{Path.DirectorySeparatorChar}diagram (%1 of %#).%x"
    ];

    private readonly Func<string, Task<string?>> _chooseOutputPathAsync;
    private readonly DiagramExportPathBuilder _pathBuilder = new();
    private ObservableCollection<string>? _exportFormats;

    public string OutputPath
    {
        get
        {
            try
            {
                string configuredPath = GetSetting("diagramexport.outputpath");
                if (!string.IsNullOrWhiteSpace(configuredPath))
                {
                    return configuredPath;
                }
            }
            catch
            {
                // Older configurations do not contain this platform-specific setting.
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                SetSetting("diagramexport.outputpath", value.Trim());
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(ExampleFilename));
        }
    }

    public string SelectedFilenameFormat
    {
        get => GetSetting("diagramexport.filenameformat");
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (!FilenameFormats.Contains(value))
                {
                    FilenameFormats.Add(value);
                }

                SetSetting("diagramexport.filenameformat", value.Trim());
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(ExampleFilename));
        }
    }

    public ObservableCollection<string> FilenameFormats { get; }

    public int SelectedExportFormatIndex
    {
        get => (int)ExportFormat;
        set
        {
            if (!Enum.IsDefined(typeof(DiagramExportFormat), value))
            {
                return;
            }

            ExportFormat = (DiagramExportFormat)value;
        }
    }

    public ObservableCollection<string> ExportFormats =>
        _exportFormats ??=
        [
            "SVG",
            "PNG",
            "GIF",
            "JPG"
        ];

    public bool OverwriteFiles
    {
        get => bool.TryParse(GetSetting("diagramexport.overwritefiles"), out bool value) && value;
        set
        {
            SetSetting("diagramexport.overwritefiles", value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(ExampleFilename));
        }
    }

    public bool CanScale => ExportFormat != DiagramExportFormat.SVG;

    public float ScaleFactor
    {
        get
        {
            string rawValue = GetSetting("diagramexport.scalefactor");
            if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float invariant) ||
                float.TryParse(rawValue, out invariant))
            {
                return invariant;
            }

            return 1.0f;
        }
        set
        {
            if (value <= 0 || value > MaxScaleFactor)
            {
                return;
            }

            SetSetting("diagramexport.scalefactor", value.ToString(CultureInfo.InvariantCulture));
            OnPropertyChanged();
        }
    }

    public float MaxScaleFactor => DiagramFileExporter.GetMaxScaleFactor(MaxWidth, MaxHeight);

    public string ExampleFilename
    {
        get
        {
            try
            {
                return _pathBuilder.BuildPath(
                    OutputPath,
                    SelectedFilenameFormat,
                    CollectionName,
                    DiagramsToExport,
                    0,
                    ExportFormat,
                    OverwriteFiles);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public IAsyncRelayCommand ChooseOutputPathCommand { get; }

    public DiagramExportViewModel(
        ObservableCollection<ObservableDiagram> diagramsToExport,
        string collectionName,
        Func<string, Task<string?>> chooseOutputPathAsync)
        : base(diagramsToExport, collectionName)
    {
        _chooseOutputPathAsync = chooseOutputPathAsync
            ?? throw new ArgumentNullException(nameof(chooseOutputPathAsync));

        FilenameFormats = [SelectedFilenameFormat];
        foreach (string filenameFormat in DefaultFilenameFormats)
        {
            if (!FilenameFormats.Contains(filenameFormat))
            {
                FilenameFormats.Add(filenameFormat);
            }
        }

        ChooseOutputPathCommand = new AsyncRelayCommand(ChooseOutputPathAsync);
        ExportStart += (_, _) => _pathBuilder.Reset();
        ExportEnd += (_, _) => _pathBuilder.Reset();
    }

    public override void ProcessClose()
    {
        SaveSettingsAsDefault();
    }

    protected override async Task ExportDiagramAsync(int diagramIndex)
    {
        ObservableDiagram diagram = DiagramsToExport[diagramIndex];
        string filePath = _pathBuilder.BuildPath(
            OutputPath,
            SelectedFilenameFormat,
            CollectionName,
            DiagramsToExport,
            diagramIndex,
            ExportFormat,
            OverwriteFiles);

        await DiagramFileExporter.ExportAsync(
            diagram.SvgText,
            diagram.TotalWidth,
            diagram.TotalHeight,
            ExportFormat,
            ScaleFactor,
            filePath);
        _pathBuilder.MarkCreated(filePath);
    }

    private DiagramExportFormat ExportFormat
    {
        get
        {
            return Enum.TryParse(
                GetSetting("diagramexport.exportformat"),
                ignoreCase: true,
                out DiagramExportFormat result)
                ? result
                : DiagramExportFormat.SVG;
        }
        set
        {
            SetSetting("diagramexport.exportformat", value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedExportFormatIndex));
            OnPropertyChanged(nameof(CanScale));
            OnPropertyChanged(nameof(ExampleFilename));
        }
    }

    private async Task ChooseOutputPathAsync()
    {
        string? outputPath = await _chooseOutputPathAsync(OutputPath);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            OutputPath = outputPath;
        }
    }
}
