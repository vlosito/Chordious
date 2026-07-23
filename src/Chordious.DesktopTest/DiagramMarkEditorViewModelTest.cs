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
public class DiagramMarkEditorViewModelTest
{
    [TestInitialize]
    public void InitializeAppViewModel()
    {
        if (AppViewModel.Instance is null)
        {
            AppViewModel.Init(typeof(DiagramMarkEditorViewModelTest).Assembly, new TestAppView());
        }
    }

    [TestMethod]
    public void ProcessClose_AcceptAppliesMarkAndStyleChanges()
    {
        DiagramMark mark = CreateMark("1");
        DiagramMarkEditorViewModel editor = new(mark, isNew: false);

        editor.Text = "R";
        editor.SelectedMarkTypeIndex = (int)DiagramMarkType.Root;
        editor.Style.MarkColorIsLocal = true;
        editor.Style.MarkColor = "#123456";
        editor.Style.MarkOpacityIsLocal = true;
        editor.Style.MarkOpacity = 0.75;
        editor.Accept.Execute(null);

        bool changed = editor.ProcessClose();

        Assert.IsTrue(changed);
        Assert.AreEqual("R", mark.Text);
        Assert.AreEqual(DiagramMarkType.Root, mark.Type);
        Assert.AreEqual("#123456", mark.MarkStyle.MarkColor);
        Assert.AreEqual(0.75, mark.MarkStyle.MarkOpacity, 0.001);
    }

    [TestMethod]
    public void ProcessClose_CancelKeepsOriginalMark()
    {
        DiagramMark mark = CreateMark("1");
        DiagramMarkType originalType = mark.Type;
        string originalColor = mark.MarkStyle.MarkColor;
        DiagramMarkEditorViewModel editor = new(mark, isNew: false);

        editor.Text = "Cancelado";
        editor.SelectedMarkTypeIndex = (int)DiagramMarkType.Muted;
        editor.Style.MarkColorIsLocal = true;
        editor.Style.MarkColor = "#654321";
        editor.Cancel.Execute(null);

        bool changed = editor.ProcessClose();

        Assert.IsFalse(changed);
        Assert.AreEqual("1", mark.Text);
        Assert.AreEqual(originalType, mark.Type);
        Assert.AreEqual(originalColor, mark.MarkStyle.MarkColor);
    }

    [TestMethod]
    public void RequiresUnsavedChangesConfirmation_ProtectsDirtyMark()
    {
        DiagramMarkEditorViewModel editor = new(CreateMark("1"), isNew: false);

        Assert.IsFalse(DiagramMarkEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));

        editor.Text = "2";

        Assert.IsTrue(DiagramMarkEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));
        Assert.IsFalse(DiagramMarkEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: true));

        editor.Accept.Execute(null);

        Assert.IsFalse(DiagramMarkEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));
    }

    [TestMethod]
    public void RequiresUnsavedChangesConfirmation_ProtectsNewMark()
    {
        DiagramMarkEditorViewModel editor = new(CreateMark(string.Empty), isNew: true);

        Assert.IsTrue(DiagramMarkEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));
    }

    private static DiagramMark CreateMark(string text)
    {
        Diagram diagram = new(ConfigFile.DefaultConfig.DiagramStyle, 6, 5);
        return new DiagramMark(diagram, new MarkPosition(1, 1), text);
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
