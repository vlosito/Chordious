// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Chordious.Core;
using Chordious.Core.ViewModel;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chordious.DesktopTest;

[TestClass]
[DoNotParallelize]
public class InstrumentManagementTest
{
    [TestInitialize]
    public void InitializeAppViewModel()
    {
        if (AppViewModel.Instance is null)
        {
            AppViewModel.Init(typeof(InstrumentManagementTest).Assembly, new TestAppView());
        }
    }

    [TestMethod]
    public void InstrumentEditor_ValidatesNewAndReadOnlyInstruments()
    {
        string? acceptedName = null;
        int acceptedStrings = 0;
        InstrumentEditorViewModel editor = new((name, strings) =>
        {
            acceptedName = name;
            acceptedStrings = strings;
        });

        Assert.IsFalse(editor.Accept.CanExecute(null));

        editor.Name = "Test instrument";
        editor.NumStrings = 7;

        Assert.IsTrue(editor.Accept.CanExecute(null));
        editor.Accept.Execute(null);
        Assert.AreEqual("Test instrument", acceptedName);
        Assert.AreEqual(7, acceptedStrings);

        InstrumentEditorViewModel readOnly = new(
            "Default instrument",
            4,
            true,
            (_, _) => Assert.Fail("A callback não deve executar para instrumentos somente leitura."));

        Assert.IsFalse(readOnly.Accept.CanExecute(null));
    }

    [TestMethod]
    public void InstrumentManager_DefaultItemsEnforceReadOnlyCommands()
    {
        InstrumentManagerViewModel manager = new();

        Assert.IsTrue(manager.DefaultInstruments.Count > 0);
        manager.SelectedDefaultInstrumentIndex = 0;

        Assert.IsNotNull(manager.SelectedInstrument);
        Assert.IsTrue(manager.SelectedInstrument.ReadOnly);
        Assert.IsTrue(manager.EditInstrument.CanExecute(null));
        Assert.IsFalse(manager.DeleteInstrument.CanExecute(null));
        Assert.IsFalse(manager.AddTuning.CanExecute(null));

        if (manager.Tunings.Count > 0)
        {
            Assert.IsNotNull(manager.SelectedTuning);
            Assert.IsTrue(manager.EditTuning.CanExecute(null));
            Assert.IsTrue(manager.CopyTuning.CanExecute(null));
            Assert.IsFalse(manager.DeleteTuning.CanExecute(null));
        }
    }

    [TestMethod]
    public void InstrumentManager_AddsInstrumentAndTuningThroughSharedMessages()
    {
        ConfigFile userConfig = GetUserConfig();
        string instrumentName = $"Desktop instrument {Guid.NewGuid():N}";
        string tuningName = $"Desktop tuning {Guid.NewGuid():N}";
        object recipient = new();

        StrongReferenceMessenger.Default.Register<ShowInstrumentEditorMessage>(recipient, (_, message) =>
        {
            message.InstrumentEditorVM.Name = instrumentName;
            message.InstrumentEditorVM.NumStrings = 5;
            message.InstrumentEditorVM.Accept.Execute(null);
        });
        StrongReferenceMessenger.Default.Register<ShowTuningEditorMessage>(recipient, (_, message) =>
        {
            message.TuningEditorVM.Name = tuningName;
            message.TuningEditorVM.RootNotes[0].SelectedNoteIndex = (int)Note.G;
            message.TuningEditorVM.RootNotes[0].Octave = 3;
            message.TuningEditorVM.Accept.Execute(null);
            message.Process();
        });

        try
        {
            InstrumentManagerViewModel manager = new();
            manager.AddInstrument.Execute(null);

            ObservableInstrument addedInstrument =
                manager.UserInstruments.Single(instrument => instrument.Name == instrumentName);
            Assert.AreEqual(5, addedInstrument.NumStrings);
            Assert.AreSame(addedInstrument, manager.SelectedInstrument);
            Assert.AreEqual(manager.UserInstruments.IndexOf(addedInstrument), manager.SelectedUserInstrumentIndex);
            Assert.AreEqual(-1, manager.SelectedDefaultInstrumentIndex);
            Assert.IsFalse(manager.CopyTuning.CanExecute(null));

            Assert.IsTrue(manager.AddTuning.CanExecute(null));
            manager.AddTuning.Execute(null);

            ObservableTuning addedTuning =
                manager.Tunings.Single(tuning => tuning.Name == tuningName);
            Assert.AreEqual(5, addedTuning.Notes.Count);
            Assert.AreEqual((int)Note.G, addedTuning.Notes[0].SelectedNoteIndex);
            Assert.AreEqual(3, addedTuning.Notes[0].Octave);
            Assert.IsTrue(manager.CopyTuning.CanExecute(null));
        }
        finally
        {
            StrongReferenceMessenger.Default.UnregisterAll(recipient);
            RemoveInstrumentIfPresent(userConfig.Instruments, instrumentName);
        }
    }

    [TestMethod]
    public void InstrumentManager_CopiesDefaultTuningIntoEditableInstrument()
    {
        ConfigFile userConfig = GetUserConfig();
        InstrumentManagerViewModel manager = new();
        object recipient = new();

        ObservableInstrument sourceInstrument = manager.DefaultInstruments
            .First(instrument => instrument.GetTunings().Count > 0);
        string targetInstrumentName = sourceInstrument.Name;
        manager.SelectedDefaultInstrumentIndex = manager.DefaultInstruments.IndexOf(sourceInstrument);
        string sourceTuningName = manager.SelectedTuning.Name;

        StrongReferenceMessenger.Default.Register<ShowTuningEditorMessage>(recipient, (_, message) =>
        {
            message.TuningEditorVM.Accept.Execute(null);
            message.Process();
        });

        try
        {
            manager.CopyTuning.Execute(null);

            ObservableInstrument copiedInstrument =
                manager.UserInstruments.Single(instrument => instrument.Name == targetInstrumentName);
            Assert.IsFalse(copiedInstrument.ReadOnly);
            Assert.IsTrue(copiedInstrument.GetTunings().Any(tuning => tuning.Name == sourceTuningName));
            Assert.AreSame(copiedInstrument, manager.SelectedInstrument);
            Assert.AreEqual(manager.UserInstruments.IndexOf(copiedInstrument), manager.SelectedUserInstrumentIndex);
            Assert.AreEqual(-1, manager.SelectedDefaultInstrumentIndex);
        }
        finally
        {
            StrongReferenceMessenger.Default.UnregisterAll(recipient);
            RemoveInstrumentIfPresent(userConfig.Instruments, targetInstrumentName);
        }
    }

    private static ConfigFile GetUserConfig()
    {
        PropertyInfo userConfigProperty = typeof(AppViewModel).GetProperty(
            "UserConfig",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (ConfigFile)userConfigProperty.GetValue(AppViewModel.Instance)!;
    }

    private static void RemoveInstrumentIfPresent(InstrumentSet instruments, string name)
    {
        if (instruments.TryGet(name, out _))
        {
            instruments.Remove(name);
        }
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

        public System.Collections.Generic.IEnumerable<string> GetSystemFonts() =>
            Array.Empty<string>();

        private static Stream CreateEmptyConfigStream()
        {
            return new MemoryStream("<chordious />"u8.ToArray());
        }
    }
}
