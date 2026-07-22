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
public class DiagramEditorViewModelTest
{
    [TestInitialize]
    public void InitializeAppViewModel()
    {
        if (AppViewModel.Instance is null)
        {
            AppViewModel.Init(typeof(DiagramEditorViewModelTest).Assembly, new TestAppView());
        }
    }

    [TestMethod]
    public void ProcessClose_AcceptAppliesBasicDiagramChanges()
    {
        ObservableDiagram original = CreateDiagram("C", 6, 5);
        DiagramEditorViewModel editor = new(original, isNew: false);

        editor.ObservableDiagram.Title = "Dm";
        editor.ObservableDiagram.NumStrings = 4;
        editor.ObservableDiagram.NumFrets = 7;
        editor.Accept.Execute(null);

        bool changed = editor.ProcessClose();

        Assert.IsTrue(changed);
        Assert.AreEqual("Dm", original.Title);
        Assert.AreEqual(4, original.NumStrings);
        Assert.AreEqual(7, original.NumFrets);
    }

    [TestMethod]
    public void ProcessClose_CancelKeepsOriginalDiagram()
    {
        ObservableDiagram original = CreateDiagram("C", 6, 5);
        DiagramEditorViewModel editor = new(original, isNew: false);

        editor.ObservableDiagram.Title = "Cancelado";
        editor.ObservableDiagram.NumFrets = 9;
        editor.Cancel.Execute(null);

        bool changed = editor.ProcessClose();

        Assert.IsFalse(changed);
        Assert.AreEqual("C", original.Title);
        Assert.AreEqual(5, original.NumFrets);
    }

    [TestMethod]
    public void RequiresUnsavedChangesConfirmation_ProtectsDirtyDiscardFlow()
    {
        ObservableDiagram original = CreateDiagram("C", 6, 5);
        DiagramEditorViewModel editor = new(original, isNew: false);

        Assert.IsFalse(DiagramEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));

        editor.ObservableDiagram.Title = "Dm";

        Assert.IsTrue(DiagramEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));
        Assert.IsFalse(DiagramEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: true));

        editor.Accept.Execute(null);

        Assert.IsFalse(DiagramEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));
    }

    private static ObservableDiagram CreateDiagram(string title, int numStrings, int numFrets)
    {
        Diagram diagram = new(ConfigFile.DefaultConfig.DiagramStyle, numStrings, numFrets)
        {
            Title = title
        };
        return new ObservableDiagram(diagram);
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
