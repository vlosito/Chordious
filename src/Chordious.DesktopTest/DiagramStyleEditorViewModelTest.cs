// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Chordious.Core;
using Chordious.Core.ViewModel;
using Chordious.Desktop;
using Chordious.Desktop.ViewModels;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chordious.DesktopTest;

[TestClass]
[DoNotParallelize]
public class DiagramStyleEditorViewModelTest
{
    [TestInitialize]
    public void InitializeAppViewModel()
    {
        if (AppViewModel.Instance is null)
        {
            AppViewModel.Init(typeof(DiagramStyleEditorViewModelTest).Assembly, new TestAppView());
        }
    }

    [TestMethod]
    public void Presentation_CoversEveryLocalStyleProperty()
    {
        ObservableDiagramStyle style = CreateObservableStyle();
        using DiagramStyleEditorPresentation presentation = DiagramStyleEditorPresentation.Create(style);

        HashSet<string> expectedLocalProperties = typeof(ObservableDiagramStyle)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType == typeof(bool) && property.Name.EndsWith("IsLocal", StringComparison.Ordinal))
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> actualLocalProperties = presentation.Rows
            .Select(row => row.LocalPropertyName)
            .Where(propertyName => propertyName is not null)
            .Select(propertyName => propertyName!)
            .ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(expectedLocalProperties.ToArray(), actualLocalProperties.ToArray());
        Assert.AreEqual(61, actualLocalProperties.Count);
        Assert.AreEqual(62, presentation.Rows.Count());
        Assert.AreEqual(4, presentation.DiagramSections.Count);
        Assert.AreEqual(5, presentation.GridSections.Count);
        Assert.AreEqual(2, presentation.TitleSections.Count);
        Assert.AreEqual(4, presentation.MarkSections.Count);
        Assert.AreEqual(2, presentation.FretLabelSections.Count);
        Assert.AreEqual(2, presentation.BarreSections.Count);
        Assert.AreEqual(1, presentation.Rows.Count(row => row.ValuePropertyName == nameof(ObservableDiagramStyle.SelectedMarkTypeIndex)));
    }

    [TestMethod]
    public void Presentation_UpdatesTextNumericAndChoiceProperties()
    {
        ObservableDiagramStyle style = CreateObservableStyle();
        using DiagramStyleEditorPresentation presentation = DiagramStyleEditorPresentation.Create(style);

        DiagramStyleEditorPropertyRow color = FindRow(presentation, nameof(ObservableDiagramStyle.DiagramColor));
        color.IsLocal = true;
        color.TextValue = "#123456";

        DiagramStyleEditorPropertyRow opacity = FindRow(presentation, nameof(ObservableDiagramStyle.DiagramOpacity));
        opacity.IsLocal = true;
        opacity.NumericValue = 0.75m;

        DiagramStyleEditorPropertyRow orientation = FindRow(presentation, nameof(ObservableDiagramStyle.SelectedOrientationIndex));
        orientation.IsLocal = true;
        orientation.SelectedIndex = 1;

        Assert.AreEqual("#123456", style.DiagramColor);
        Assert.AreEqual(0.75, style.DiagramOpacity, 0.001);
        Assert.AreEqual(1, style.SelectedOrientationIndex);
    }

    [TestMethod]
    public void ProcessClose_AcceptAppliesStyleChanges()
    {
        DiagramStyle original = CreateStyle();
        bool? callbackChanged = null;
        ShowDiagramStyleEditorMessage message = new(new ObservableDiagramStyle(original), changed => callbackChanged = changed);
        DiagramStyleEditorViewModel editor = message.DiagramStyleEditorVM;

        editor.Style.DiagramColorIsLocal = true;
        editor.Style.DiagramColor = "#123456";
        editor.Style.GridFretSpacingIsLocal = true;
        editor.Style.GridFretSpacing = 42;
        editor.Accept.Execute(null);

        message.Process();

        Assert.IsTrue(callbackChanged);
        Assert.AreEqual("#123456", original.DiagramColor);
        Assert.AreEqual(42, original.GridFretSpacing, 0.001);
    }

    [TestMethod]
    public void ProcessClose_CancelKeepsOriginalStyle()
    {
        DiagramStyle original = CreateStyle();
        string originalColor = original.DiagramColor;
        bool? callbackChanged = null;
        ShowDiagramStyleEditorMessage message = new(new ObservableDiagramStyle(original), changed => callbackChanged = changed);
        DiagramStyleEditorViewModel editor = message.DiagramStyleEditorVM;

        editor.Style.DiagramColorIsLocal = true;
        editor.Style.DiagramColor = "#654321";
        editor.Cancel.Execute(null);

        message.Process();

        Assert.IsFalse(callbackChanged);
        Assert.AreEqual(originalColor, original.DiagramColor);
    }

    [TestMethod]
    public void RequiresUnsavedChangesConfirmation_ProtectsDirtyStyle()
    {
        DiagramStyleEditorViewModel editor = new(CreateObservableStyle());

        Assert.IsFalse(DiagramStyleEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));

        editor.Style.DiagramColorIsLocal = true;

        Assert.IsTrue(DiagramStyleEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));
        Assert.IsFalse(DiagramStyleEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: true));

        editor.Accept.Execute(null);

        Assert.IsFalse(DiagramStyleEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));
    }

    private static DiagramStyleEditorPropertyRow FindRow(
        DiagramStyleEditorPresentation presentation,
        string valuePropertyName)
    {
        return presentation.Rows.Single(row => row.ValuePropertyName == valuePropertyName);
    }

    private static ObservableDiagramStyle CreateObservableStyle()
    {
        return new ObservableDiagramStyle(CreateStyle());
    }

    private static DiagramStyle CreateStyle()
    {
        return new DiagramStyle(ConfigFile.DefaultConfig.DiagramStyle, "Desktop style test");
    }

    private sealed class TestAppView : IAppView
    {
        public void DoOnUIThread(Action action) => action();

        public object DoOnUIThread(Func<object> func) => func();

        public Stream GetAppConfigStream() => CreateEmptyConfigStream();

        public Stream GetUserConfigStreamToRead() => CreateEmptyConfigStream();

        public Stream GetUserConfigStreamToWrite() => new MemoryStream();

        public object SvgTextToImage(string svgText, int width, int height, bool editMode) => new object();

        public void TextToClipboard(string text)
        {
        }

        public void DiagramToClipboard(ObservableDiagram diagram, float scaleFactor)
        {
        }

        public IEnumerable<string> GetSystemFonts() => Array.Empty<string>();

        private static Stream CreateEmptyConfigStream()
        {
            return new MemoryStream("<chordious />"u8.ToArray());
        }
    }
}
