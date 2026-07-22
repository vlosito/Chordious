// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;
using Chordious.Desktop.ViewModels;

namespace Chordious.Desktop;

public partial class DiagramStyleEditorWindow : Window
{
    private readonly UnsavedChangesCloseController _closeController;
    private readonly ItemsControl _diagramSections;
    private readonly ItemsControl _gridSections;
    private readonly ItemsControl _titleSections;
    private readonly ItemsControl _markSections;
    private readonly ItemsControl _fretLabelSections;
    private readonly ItemsControl _barreSections;

    private DiagramStyleEditorViewModel? _viewModel;
    private DiagramStyleEditorPresentation? _presentation;

    public DiagramStyleEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);

        _diagramSections = this.FindControl<ItemsControl>("DiagramSections")!;
        _gridSections = this.FindControl<ItemsControl>("GridSections")!;
        _titleSections = this.FindControl<ItemsControl>("TitleSections")!;
        _markSections = this.FindControl<ItemsControl>("MarkSections")!;
        _fretLabelSections = this.FindControl<ItemsControl>("FretLabelSections")!;
        _barreSections = this.FindControl<ItemsControl>("BarreSections")!;

        _closeController = new UnsavedChangesCloseController(
            this,
            () => (DataContext as DiagramStyleEditorViewModel)?.Dirty == true,
            () => (DataContext as DiagramStyleEditorViewModel)?.ApplyChangesOnClose == true,
            () => (DataContext as DiagramStyleEditorViewModel)?.Accept.Execute(null));

        DataContextChanged += (_, _) => AttachViewModel();
        Closed += (_, _) => DetachViewModel();
    }

    internal static bool RequiresUnsavedChangesConfirmation(
        DiagramStyleEditorViewModel? viewModel,
        bool closeApproved)
    {
        return viewModel is not null &&
            UnsavedChangesCloseController.RequiresConfirmation(
                viewModel.Dirty,
                viewModel.ApplyChangesOnClose,
                closeApproved);
    }

    private void AttachViewModel()
    {
        DetachViewModel();

        _viewModel = DataContext as DiagramStyleEditorViewModel;
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        RebuildPresentation();
    }

    private void DetachViewModel()
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel = null;
        }

        _presentation?.Dispose();
        _presentation = null;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DiagramStyleEditorViewModel.SelectedStyleIndex) ||
            e.PropertyName == nameof(DiagramStyleEditorViewModel.Style))
        {
            RebuildPresentation();
        }
    }

    private void RebuildPresentation()
    {
        _presentation?.Dispose();
        _presentation = _viewModel is null
            ? null
            : DiagramStyleEditorPresentation.Create(_viewModel.Style);

        _diagramSections.ItemsSource = _presentation?.DiagramSections;
        _gridSections.ItemsSource = _presentation?.GridSections;
        _titleSections.ItemsSource = _presentation?.TitleSections;
        _markSections.ItemsSource = _presentation?.MarkSections;
        _fretLabelSections.ItemsSource = _presentation?.FretLabelSections;
        _barreSections.ItemsSource = _presentation?.BarreSections;
    }
}
