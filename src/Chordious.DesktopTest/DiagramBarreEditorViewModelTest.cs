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
public class DiagramBarreEditorViewModelTest
{
    [TestInitialize]
    public void InitializeAppViewModel()
    {
        if (AppViewModel.Instance is null)
        {
            AppViewModel.Init(typeof(DiagramBarreEditorViewModelTest).Assembly, new TestAppView());
        }
    }

    [TestMethod]
    public void ProcessClose_AcceptAppliesBarreStyleChanges()
    {
        DiagramBarre barre = CreateBarre();
        bool? callbackChanged = null;
        ShowDiagramBarreEditorMessage message = new(barre, isNew: false, changed => callbackChanged = changed);
        DiagramBarreEditorViewModel editor = message.DiagramBarreEditorVM;

        editor.Style.BarreLineColorIsLocal = true;
        editor.Style.BarreLineColor = "#123456";
        editor.Style.BarreOpacityIsLocal = true;
        editor.Style.BarreOpacity = 0.75;
        editor.Style.BarreArcRatioIsLocal = true;
        editor.Style.BarreArcRatio = 1.5;
        editor.Style.BarreStackIsLocal = true;
        editor.Style.SelectedBarreStackIndex = (int)DiagramBarreStack.OverMarks;
        editor.Accept.Execute(null);

        message.Process();

        Assert.IsTrue(callbackChanged);
        Assert.AreEqual("#123456", barre.Style.BarreLineColor);
        Assert.AreEqual(0.75, barre.Style.BarreOpacity, 0.001);
        Assert.AreEqual(1.5, barre.Style.BarreArcRatio, 0.001);
        Assert.AreEqual(DiagramBarreStack.OverMarks, barre.Style.BarreStack);
    }

    [TestMethod]
    public void ProcessClose_CancelKeepsOriginalBarreStyle()
    {
        DiagramBarre barre = CreateBarre();
        string originalColor = barre.Style.BarreLineColor;
        bool? callbackChanged = null;
        ShowDiagramBarreEditorMessage message = new(barre, isNew: false, changed => callbackChanged = changed);
        DiagramBarreEditorViewModel editor = message.DiagramBarreEditorVM;

        editor.Style.BarreLineColorIsLocal = true;
        editor.Style.BarreLineColor = "#654321";
        editor.Cancel.Execute(null);

        message.Process();

        Assert.IsFalse(callbackChanged);
        Assert.AreEqual(originalColor, barre.Style.BarreLineColor);
    }

    [TestMethod]
    public void RequiresUnsavedChangesConfirmation_ProtectsDirtyBarre()
    {
        DiagramBarreEditorViewModel editor = new(CreateBarre(), isNew: false);

        Assert.IsFalse(DiagramBarreEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));

        editor.Style.BarreLineColorIsLocal = true;

        Assert.IsTrue(DiagramBarreEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));
        Assert.IsFalse(DiagramBarreEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: true));

        editor.Accept.Execute(null);

        Assert.IsFalse(DiagramBarreEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));
    }

    [TestMethod]
    public void RequiresUnsavedChangesConfirmation_ProtectsNewBarre()
    {
        DiagramBarreEditorViewModel editor = new(CreateBarre(), isNew: true);

        Assert.IsTrue(DiagramBarreEditorWindow.RequiresUnsavedChangesConfirmation(editor, closeApproved: false));
    }

    private static DiagramBarre CreateBarre()
    {
        Diagram diagram = new(ConfigFile.DefaultConfig.DiagramStyle, 6, 5);
        return new DiagramBarre(diagram, new BarrePosition(1, 1, 6));
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
