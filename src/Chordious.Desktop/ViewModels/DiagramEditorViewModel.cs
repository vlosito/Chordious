// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;

using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;

namespace Chordious.Desktop.ViewModels;

internal sealed class DiagramEditorViewModel : Chordious.Core.ViewModel.DiagramEditorViewModel
{
    public static string SelectedEditorRenderBackgroundLabel => "Fundo do editor";

    public static string SelectedEditorRenderBackgroundToolTip =>
        "Fundo usado somente no preview deste e dos próximos diagramas editados.";

    public ObservableCollection<string> EditorRenderBackgrounds { get; } =
        new(["Nenhum", "Branco", "Transparência"]);

    public int SelectedEditorRenderBackgroundIndex
    {
        get => (int)GetEditorRenderBackground();
        set
        {
            if (!Enum.IsDefined((PreviewBackground)value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            AppVM.SetSetting("diagrameditor.renderbackground", (PreviewBackground)value);
            OnPropertyChanged(nameof(SelectedEditorRenderBackgroundIndex));
            RefreshPreview();
        }
    }

    public DiagramEditorViewModel(ObservableDiagram diagram, bool isNew)
        : base(diagram, isNew)
    {
    }

    private static PreviewBackground GetEditorRenderBackground()
    {
        try
        {
            return Enum.TryParse(
                AppVM.GetSetting("diagrameditor.renderbackground"),
                out PreviewBackground value)
                ? value
                : PreviewBackground.None;
        }
        catch (Chordious.Core.InheritableDictionaryKeyNotFoundException)
        {
            return PreviewBackground.None;
        }
    }
}
