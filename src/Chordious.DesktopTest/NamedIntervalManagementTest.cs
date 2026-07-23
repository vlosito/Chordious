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
public class NamedIntervalManagementTest
{
    [TestInitialize]
    public void InitializeAppViewModel()
    {
        if (AppViewModel.Instance is null)
        {
            AppViewModel.Init(typeof(NamedIntervalManagementTest).Assembly, new TestAppView());
        }
    }

    [TestMethod]
    public void NamedIntervalEditors_ValidateIntervalsAndReadOnlyItems()
    {
        string? acceptedQualityName = null;
        string? acceptedAbbreviation = null;
        int[]? acceptedQualityIntervals = null;
        ChordQualityEditorViewModel qualityEditor = new((name, abbreviation, intervals) =>
        {
            acceptedQualityName = name;
            acceptedAbbreviation = abbreviation;
            acceptedQualityIntervals = intervals;
        });

        qualityEditor.Name = "Test quality";
        qualityEditor.Abbreviation = "tq";
        Assert.IsFalse(qualityEditor.Accept.CanExecute(null));

        AddIntervals(qualityEditor, 0, 4, 7);

        Assert.IsTrue(qualityEditor.Accept.CanExecute(null));
        Assert.IsFalse(string.IsNullOrWhiteSpace(qualityEditor.Example));
        qualityEditor.Accept.Execute(null);
        Assert.AreEqual("Test quality", acceptedQualityName);
        Assert.AreEqual("tq", acceptedAbbreviation);
        CollectionAssert.AreEqual(new[] { 0, 4, 7 }, acceptedQualityIntervals);

        ScaleEditorViewModel readOnlyScale = new(
            "Default scale",
            new[] { 0, 2, 4, 5, 7, 9, 11, 12 },
            true,
            (_, _) => Assert.Fail("A callback não deve executar para escalas somente leitura."));

        Assert.IsFalse(readOnlyScale.Accept.CanExecute(null));
        Assert.IsFalse(readOnlyScale.AddInterval.CanExecute(null));
        Assert.IsFalse(readOnlyScale.RemoveInterval.CanExecute(null));
    }

    [TestMethod]
    public void NamedIntervalManagers_DefaultItemsEnforceReadOnlyCommands()
    {
        ChordQualityManagerViewModel qualityManager = new();
        ScaleManagerViewModel scaleManager = new();

        Assert.IsTrue(qualityManager.DefaultNamedIntervals.Count > 0);
        qualityManager.SelectedDefaultNamedIntervalIndex = 0;
        Assert.IsNotNull(qualityManager.SelectedNamedInterval);
        Assert.IsTrue(qualityManager.SelectedNamedInterval.ReadOnly);
        Assert.IsTrue(qualityManager.EditNamedInterval.CanExecute(null));
        Assert.IsFalse(qualityManager.DeleteNamedInterval.CanExecute(null));

        Assert.IsTrue(scaleManager.DefaultNamedIntervals.Count > 0);
        scaleManager.SelectedDefaultNamedIntervalIndex = 0;
        Assert.IsNotNull(scaleManager.SelectedNamedInterval);
        Assert.IsTrue(scaleManager.SelectedNamedInterval.ReadOnly);
        Assert.IsTrue(scaleManager.EditNamedInterval.CanExecute(null));
        Assert.IsFalse(scaleManager.DeleteNamedInterval.CanExecute(null));
    }

    [TestMethod]
    public void ChordQualityManager_AddsQualityAndPreservesSortedSelection()
    {
        ConfigFile userConfig = GetUserConfig();
        string qualityName = $"Desktop quality {Guid.NewGuid():N}";
        object recipient = new();

        StrongReferenceMessenger.Default.Register<ShowChordQualityEditorMessage>(recipient, (_, message) =>
        {
            message.ChordQualityEditorVM.Name = qualityName;
            message.ChordQualityEditorVM.Abbreviation = "dq";
            AddIntervals(message.ChordQualityEditorVM, 0, 3, 7);
            message.ChordQualityEditorVM.Accept.Execute(null);
        });

        try
        {
            ChordQualityManagerViewModel manager = new();
            manager.AddNamedInterval.Execute(null);

            ObservableNamedInterval added =
                manager.UserNamedIntervals.Single(item => item.Name == qualityName);
            Assert.AreSame(added, manager.SelectedNamedInterval);
            Assert.AreEqual(
                manager.UserNamedIntervals.IndexOf(added),
                manager.SelectedUserNamedIntervalIndex);
            Assert.AreEqual(-1, manager.SelectedDefaultNamedIntervalIndex);
            Assert.AreEqual("dq", ((ObservableChordQuality)added).Abbreviation);
            CollectionAssert.AreEqual(new[] { 0, 3, 7 }, added.Intervals);
        }
        finally
        {
            StrongReferenceMessenger.Default.UnregisterAll(recipient);
            RemoveNamedIntervalIfPresent(userConfig.ChordQualities, qualityName);
        }
    }

    [TestMethod]
    public void ScaleManager_AddsScaleAndPreservesSortedSelection()
    {
        ConfigFile userConfig = GetUserConfig();
        string scaleName = $"Desktop scale {Guid.NewGuid():N}";
        object recipient = new();

        StrongReferenceMessenger.Default.Register<ShowScaleEditorMessage>(recipient, (_, message) =>
        {
            message.ScaleEditorVM.Name = scaleName;
            AddIntervals(message.ScaleEditorVM, 0, 2, 4, 7, 9, 12);
            message.ScaleEditorVM.Accept.Execute(null);
        });

        try
        {
            ScaleManagerViewModel manager = new();
            manager.AddNamedInterval.Execute(null);

            ObservableNamedInterval added =
                manager.UserNamedIntervals.Single(item => item.Name == scaleName);
            Assert.AreSame(added, manager.SelectedNamedInterval);
            Assert.AreEqual(
                manager.UserNamedIntervals.IndexOf(added),
                manager.SelectedUserNamedIntervalIndex);
            Assert.AreEqual(-1, manager.SelectedDefaultNamedIntervalIndex);
            CollectionAssert.AreEqual(new[] { 0, 2, 4, 7, 9, 12 }, added.Intervals);
        }
        finally
        {
            StrongReferenceMessenger.Default.UnregisterAll(recipient);
            RemoveNamedIntervalIfPresent(userConfig.Scales, scaleName);
        }
    }

    [TestMethod]
    public void ScaleManager_DeletePromptIdentifiesScale()
    {
        ConfigFile userConfig = GetUserConfig();
        string scaleName = $"Desktop scale {Guid.NewGuid():N}";
        object recipient = new();
        string? prompt = null;

        userConfig.Scales.Add(scaleName, new[] { 0, 2, 4, 7, 9, 12 });
        StrongReferenceMessenger.Default.Register<ConfirmationMessage>(recipient, (_, message) =>
        {
            prompt = message.ConfirmationVM.Message;
        });

        try
        {
            ScaleManagerViewModel manager = new();
            manager.SelectedUserNamedIntervalIndex = manager.UserNamedIntervals
                .Select((item, index) => (item, index))
                .Single(pair => pair.item.Name == scaleName)
                .index;

            manager.DeleteNamedInterval.Execute(null);

            StringAssert.Contains(prompt, "delete the scale");
            StringAssert.DoesNotMatch(prompt, new System.Text.RegularExpressions.Regex("chord quality"));
        }
        finally
        {
            StrongReferenceMessenger.Default.UnregisterAll(recipient);
            RemoveNamedIntervalIfPresent(userConfig.Scales, scaleName);
        }
    }

    private static void AddIntervals(NamedIntervalEditorViewModel editor, params int[] values)
    {
        foreach (int value in values)
        {
            editor.AddInterval.Execute(null);
            editor.Intervals[^1].Value = value;
        }
    }

    private static ConfigFile GetUserConfig()
    {
        PropertyInfo userConfigProperty = typeof(AppViewModel).GetProperty(
            "UserConfig",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (ConfigFile)userConfigProperty.GetValue(AppViewModel.Instance)!;
    }

    private static void RemoveNamedIntervalIfPresent(NamedIntervalSet set, string name)
    {
        NamedInterval? item = set.FirstOrDefault(interval => interval.Name == name);
        if (item is not null)
        {
            set.Remove(item);
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
