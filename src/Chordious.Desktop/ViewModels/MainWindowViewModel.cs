// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using Chordious.Core;
using Chordious.Core.ViewModel;

namespace Chordious.Desktop.ViewModels;

public sealed class MainWindowViewModel : MainViewModel
{
    private readonly Func<string, Task<string?>> _saveSvgAsync;
    private readonly ObservableDiagram _fallbackPreviewDiagram;
    private ObservableDiagramLibraryNode? _selectedLibraryNode;
    private ObservableDiagram? _selectedLibraryDiagram;
    private string _exportStatus = "O diagrama ainda não foi exportado.";

    public DiagramLibraryViewModel Library { get; }

    public ObservableDiagram PreviewDiagram => SelectedLibraryDiagram ?? _fallbackPreviewDiagram;

    public ObservableDiagramLibraryNode? SelectedLibraryNode
    {
        get => _selectedLibraryNode;
        set
        {
            if (ReferenceEquals(_selectedLibraryNode, value))
            {
                if (value is null)
                {
                    ClearDiagramSelection();
                }
                return;
            }

            ClearDiagramSelection();
            Library.SelectedNode = value;
            SetProperty(ref _selectedLibraryNode, value);
            OnPropertyChanged(nameof(HasSelectedLibraryNode));
        }
    }

    public ObservableDiagram? SelectedLibraryDiagram
    {
        get => _selectedLibraryDiagram;
        set
        {
            if (ReferenceEquals(_selectedLibraryDiagram, value))
            {
                return;
            }

            SetSelectedLibraryDiagrams(
                value is null ? Enumerable.Empty<ObservableDiagram>() : [value],
                value);
        }
    }

    public bool HasSelectedLibraryNode => SelectedLibraryNode is not null;

    public bool HasSelectedLibraryDiagram => SelectedLibraryDiagram is not null;

    public IAsyncRelayCommand ExportPreviewSvgCommand { get; }

    public IAsyncRelayCommand ExportSelectedDiagramCommand { get; }

    public IRelayCommand CopySelectedDiagramSvgCommand { get; }

    public IRelayCommand CopySelectedDiagramImageCommand { get; }

    public string ExportStatus
    {
        get => _exportStatus;
        private set => SetProperty(ref _exportStatus, value);
    }

    public MainWindowViewModel(Func<string, Task<string?>> saveSvgAsync)
    {
        _saveSvgAsync = saveSvgAsync ?? throw new ArgumentNullException(nameof(saveSvgAsync));

        Diagram diagram = CreatePreviewDiagram();
        _fallbackPreviewDiagram = new ObservableDiagram(diagram, name: "C major");

        Library = new DiagramLibraryViewModel();
        Library.PropertyChanged += Library_PropertyChanged;

        ExportPreviewSvgCommand = new AsyncRelayCommand(ExportPreviewSvgAsync);
        ExportSelectedDiagramCommand = new AsyncRelayCommand(
            ExportSelectedDiagramAsync,
            () => SelectedLibraryDiagram is not null);
        CopySelectedDiagramSvgCommand = new RelayCommand(
            CopySelectedDiagramSvg,
            () => SelectedLibraryDiagram is not null);
        CopySelectedDiagramImageCommand = new RelayCommand(
            CopySelectedDiagramImage,
            () => SelectedLibraryDiagram is not null);

        SelectedLibraryNode = Library.Nodes.FirstOrDefault();
    }

    public void SetSelectedLibraryDiagrams(
        IEnumerable<ObservableDiagram> diagrams,
        ObservableDiagram? primaryDiagram = null)
    {
        ArgumentNullException.ThrowIfNull(diagrams);

        HashSet<ObservableDiagram> availableDiagrams =
            SelectedLibraryNode?.Diagrams.ToHashSet() ?? [];
        List<ObservableDiagram> selection = diagrams
            .Where(availableDiagrams.Contains)
            .Distinct()
            .ToList();
        ObservableDiagram? primary = primaryDiagram is not null && selection.Contains(primaryDiagram)
            ? primaryDiagram
            : selection.FirstOrDefault();

        SelectedLibraryNode?.SelectedDiagrams.Clear();
        foreach (ObservableDiagram diagram in selection)
        {
            SelectedLibraryNode?.SelectedDiagrams.Add(diagram);
        }

        SetProperty(ref _selectedLibraryDiagram, primary, nameof(SelectedLibraryDiagram));
        OnPropertyChanged(nameof(PreviewDiagram));
        OnPropertyChanged(nameof(HasSelectedLibraryDiagram));
        RefreshSelectedDiagramCommands();
    }

    private async Task ExportPreviewSvgAsync()
    {
        string? path = await _saveSvgAsync(PreviewDiagram.SvgText);
        ExportStatus = path is null
            ? "Exportação cancelada."
            : $"SVG exportado para {path}";
    }

    private async Task ExportSelectedDiagramAsync()
    {
        ObservableDiagram? diagram = SelectedLibraryDiagram;
        if (diagram is null)
        {
            return;
        }

        string? path = await _saveSvgAsync(diagram.SvgText);
        ExportStatus = path is null
            ? "Exportação cancelada."
            : $"SVG exportado para {path}";
    }

    private void CopySelectedDiagramSvg()
    {
        SelectedLibraryDiagram?.SendTextToClipboard.Execute(null);
        ExportStatus = "SVG copiado para a área de transferência.";
    }

    private void CopySelectedDiagramImage()
    {
        SelectedLibraryDiagram?.SendImageToClipboard.Execute(null);
        ExportStatus = "Imagem copiada para a área de transferência.";
    }

    private void Library_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DiagramLibraryViewModel.Nodes))
        {
            SelectedLibraryNode = null;
        }
    }

    private void ClearDiagramSelection()
    {
        if (_selectedLibraryDiagram is null)
        {
            return;
        }

        _selectedLibraryNode?.SelectedDiagrams.Clear();
        _selectedLibraryDiagram = null;
        OnPropertyChanged(nameof(SelectedLibraryDiagram));
        OnPropertyChanged(nameof(PreviewDiagram));
        OnPropertyChanged(nameof(HasSelectedLibraryDiagram));
        RefreshSelectedDiagramCommands();
    }

    private void RefreshSelectedDiagramCommands()
    {
        ExportSelectedDiagramCommand?.NotifyCanExecuteChanged();
        CopySelectedDiagramSvgCommand?.NotifyCanExecuteChanged();
        CopySelectedDiagramImageCommand?.NotifyCanExecuteChanged();
    }

    private static Diagram CreatePreviewDiagram()
    {
        Diagram diagram = new(ConfigFile.DefaultConfig.DiagramStyle, 6, 5)
        {
            Title = "C"
        };

        diagram.NewMark(new MarkPosition(1, 0));
        diagram.NewMark(new MarkPosition(2, 1));
        diagram.NewMark(new MarkPosition(3, 0));
        diagram.NewMark(new MarkPosition(4, 2));
        diagram.NewMark(new MarkPosition(5, 3));
        diagram.NewMark(new MarkPosition(6, 0));

        return diagram;
    }
}
