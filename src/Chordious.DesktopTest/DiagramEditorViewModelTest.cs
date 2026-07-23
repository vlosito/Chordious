// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;

using Avalonia;

using CommunityToolkit.Mvvm.Messaging;

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

    [TestMethod]
    public void MapCursorPosition_MapsScaledPreviewToDiagramCoordinates()
    {
        Point mapped = DiagramEditorWindow.MapCursorPosition(
            new Point(150, 200),
            new Size(300, 400),
            new Size(600, 800));

        Assert.AreEqual(300, mapped.X, 0.001);
        Assert.AreEqual(400, mapped.Y, 0.001);
    }

    [TestMethod]
    public void DragThreshold_RequiresIntentionalPointerMovement()
    {
        Assert.IsFalse(DiagramEditorWindow.HasExceededDragThreshold(
            new Point(10, 10),
            new Point(13, 14)));
        Assert.IsTrue(DiagramEditorWindow.HasExceededDragThreshold(
            new Point(10, 10),
            new Point(16, 10)));
    }

    [DataTestMethod]
    [DataRow("C", "C.png")]
    [DataRow("../C:maj", ".._C_maj.png")]
    [DataRow("", "diagram.png")]
    [DataRow("..", "diagram.png")]
    public void GetSafeDragFileName_BlocksPathTraversal(
        string title,
        string expected)
    {
        Assert.AreEqual(expected, DiagramEditorWindow.GetSafeDragFileName(title));
    }

    [TestMethod]
    public void StyleEditor_AcceptedChangeMarksEditorDirtyAndRefreshesPreview()
    {
        ObservableDiagram original = CreateDiagram("C", 6, 5);
        DiagramEditorViewModel editor = new(original, isNew: false);
        object recipient = new();

        editor.ObservableDiagram.ResetStyles();
        editor.Apply.Execute(null);
        Assert.IsFalse(editor.Dirty);

        StrongReferenceMessenger.Default.Register<ShowDiagramStyleEditorMessage>(recipient, (_, message) =>
        {
            ObservableDiagramStyle editedStyle = message.DiagramStyleEditorVM.Style;
            editedStyle.GridFretSpacingIsLocal = true;
            editedStyle.GridFretSpacing = 42;
            message.DiagramStyleEditorVM.Accept.Execute(null);
            message.Process();
        });

        try
        {
            editor.Style.ShowEditor.Execute(null);

            Assert.IsTrue(editor.Dirty);
            Assert.AreEqual(42, editor.Style.GridFretSpacing, 0.001);
            Assert.IsTrue(editor.ResetStyles.CanExecute(null));
        }
        finally
        {
            StrongReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [TestMethod]
    public void RefreshPreview_DoesNotDirtyDiagram()
    {
        ObservableDiagram original = CreateDiagram("C", 6, 5);
        TestDiagramEditorViewModel editor = new(original);

        editor.RefreshForTest();

        Assert.IsFalse(editor.Dirty);
    }

    [TestMethod]
    public void AddMark_RefreshesCommandsAfterEditorAccepts()
    {
        Diagram diagram = new(ConfigFile.DefaultConfig.DiagramStyle, 6, 5);
        ObservableDiagram observable = new(diagram)
        {
            CursorX = diagram.GridLeftEdge(),
            CursorY = diagram.GridTopEdge() + (diagram.Style.GridFretSpacing / 2)
        };
        object recipient = new();

        StrongReferenceMessenger.Default.Register<ShowDiagramMarkEditorMessage>(recipient, (_, message) =>
        {
            message.DiagramMarkEditorVM.Text = "M";
            message.DiagramMarkEditorVM.Accept.Execute(null);
            message.Process();
        });

        try
        {
            Assert.IsTrue(observable.CanAddMark);

            observable.AddMark.Execute(null);

            Assert.IsFalse(observable.CanAddMark);
            Assert.IsTrue(observable.CanEditMark);
            Assert.IsTrue(observable.CanRemoveMark);
        }
        finally
        {
            StrongReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [TestMethod]
    public void AddFretLabel_RefreshesCommandsAfterEditorAccepts()
    {
        Diagram diagram = new(ConfigFile.DefaultConfig.DiagramStyle, 6, 5);
        ObservableDiagram observable = new(diagram)
        {
            CursorX = 0,
            CursorY = diagram.GridTopEdge() + (diagram.Style.GridFretSpacing / 2)
        };
        object recipient = new();

        StrongReferenceMessenger.Default.Register<ShowDiagramFretLabelEditorMessage>(recipient, (_, message) =>
        {
            message.DiagramFretLabelEditorVM.Text = "III";
            message.DiagramFretLabelEditorVM.Accept.Execute(null);
            message.Process();
        });

        try
        {
            Assert.IsTrue(observable.CanAddFretLabel);

            observable.AddFretLabel.Execute(null);

            Assert.IsFalse(observable.CanAddFretLabel);
            Assert.IsTrue(observable.CanEditFretLabel);
            Assert.IsTrue(observable.CanRemoveFretLabel);

            observable.RemoveFretLabel.Execute(null);

            Assert.IsTrue(observable.CanAddFretLabel);
            Assert.IsFalse(observable.CanEditFretLabel);
            Assert.IsFalse(observable.CanRemoveFretLabel);
        }
        finally
        {
            StrongReferenceMessenger.Default.UnregisterAll(recipient);
        }
    }

    [TestMethod]
    public void AddBarre_RefreshesCommandsAfterEditorAccepts()
    {
        Diagram diagram = new(ConfigFile.DefaultConfig.DiagramStyle, 6, 5);
        ObservableDiagram observable = new(diagram)
        {
            CursorX = diagram.GridLeftEdge(),
            CursorY = diagram.GridTopEdge() + (diagram.Style.GridFretSpacing / 2)
        };
        object recipient = new();

        StrongReferenceMessenger.Default.Register<PromptForTextMessage>(recipient, (_, message) =>
        {
            message.TextPromptVM.Text = "4";
            message.TextPromptVM.Accept.Execute(null);
        });
        StrongReferenceMessenger.Default.Register<ShowDiagramBarreEditorMessage>(recipient, (_, message) =>
        {
            message.DiagramBarreEditorVM.Accept.Execute(null);
            message.Process();
        });

        try
        {
            Assert.IsTrue(observable.CanAddBarre);

            observable.AddBarre.Execute(null);

            Assert.IsFalse(observable.CanAddBarre);
            Assert.IsTrue(observable.CanEditBarre);
            Assert.IsTrue(observable.CanRemoveBarre);

            observable.RemoveBarre.Execute(null);

            Assert.IsTrue(observable.CanAddBarre);
            Assert.IsFalse(observable.CanEditBarre);
            Assert.IsFalse(observable.CanRemoveBarre);
        }
        finally
        {
            StrongReferenceMessenger.Default.UnregisterAll(recipient);
        }
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

    private sealed class TestDiagramEditorViewModel : DiagramEditorViewModel
    {
        public TestDiagramEditorViewModel(ObservableDiagram diagram)
            : base(diagram, isNew: false)
        {
        }

        public void RefreshForTest() => RefreshPreview();
    }
}
