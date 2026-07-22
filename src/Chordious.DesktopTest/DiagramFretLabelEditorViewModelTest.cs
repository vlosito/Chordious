// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;

using Chordious.Core;
using Chordious.Core.ViewModel;
using Chordious.Desktop;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chordious.DesktopTest;

[TestClass]
[DoNotParallelize]
public class DiagramFretLabelEditorViewModelTest
{
    [TestInitialize]
    public void InitializeAppViewModel()
    {
        if (AppViewModel.Instance is null)
        {
            AppViewModel.Init(typeof(DiagramFretLabelEditorViewModelTest).Assembly, new TestAppView());
        }
    }

    [TestMethod]
    public void ProcessClose_AcceptAppliesFretLabelAndStyleChanges()
    {
        DiagramFretLabel fretLabel = CreateFretLabel("1");
        DiagramFretLabelEditorViewModel editor = new(fretLabel, isNew: false);

        editor.Text = "III";
        editor.Style.FretLabelTextColorIsLocal = true;
        editor.Style.FretLabelTextColor = "#123456";
        editor.Style.FretLabelTextOpacityIsLocal = true;
        editor.Style.FretLabelTextOpacity = 0.75;
        editor.Style.FretLabelGridPaddingIsLocal = true;
        editor.Style.FretLabelGridPadding = 8;
        editor.Accept.Execute(null);

        bool changed = editor.ProcessClose();

        Assert.IsTrue(changed);
        Assert.AreEqual("III", fretLabel.Text);
        Assert.AreEqual("#123456", fretLabel.Style.FretLabelTextColor);
        Assert.AreEqual(0.75, fretLabel.Style.FretLabelTextOpacity, 0.001);
        Assert.AreEqual(8, fretLabel.Style.FretLabelGridPadding, 0.001);
    }

    [TestMethod]
    public void ProcessClose_CancelKeepsOriginalFretLabel()
    {
        DiagramFretLabel fretLabel = CreateFretLabel("1");
        string originalColor = fretLabel.Style.FretLabelTextColor;
        DiagramFretLabelEditorViewModel editor = new(fretLabel, isNew: false);

        editor.Text = "Cancelado";
        editor.Style.FretLabelTextColorIsLocal = true;
        editor.Style.FretLabelTextColor = "#654321";
        editor.Cancel.Execute(null);

        bool changed = editor.ProcessClose();

        Assert.IsFalse(changed);
        Assert.AreEqual("1", fretLabel.Text);
        Assert.AreEqual(originalColor, fretLabel.Style.FretLabelTextColor);
    }

    [TestMethod]
    public void RequiresUnsavedChangesConfirmation_ProtectsDirtyFretLabel()
    {
        DiagramFretLabelEditorViewModel editor = new(CreateFretLabel("1"), isNew: false);

        Assert.IsFalse(DiagramFretLabelEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));

        editor.Text = "2";

        Assert.IsTrue(DiagramFretLabelEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));
        Assert.IsFalse(DiagramFretLabelEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: true));

        editor.Accept.Execute(null);

        Assert.IsFalse(DiagramFretLabelEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));
    }

    [TestMethod]
    public void RequiresUnsavedChangesConfirmation_ProtectsNewFretLabel()
    {
        DiagramFretLabelEditorViewModel editor = new(CreateFretLabel(string.Empty), isNew: true);

        Assert.IsTrue(DiagramFretLabelEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));
    }

    private static DiagramFretLabel CreateFretLabel(string text)
    {
        Diagram diagram = new(ConfigFile.DefaultConfig.DiagramStyle, 6, 5);
        return new DiagramFretLabel(diagram, new FretLabelPosition(FretLabelSide.Left, 1), text);
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
