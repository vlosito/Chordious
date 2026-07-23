// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Text;

using Chordious.Core;
using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chordious.DesktopTest;

[TestClass]
[DoNotParallelize]
public class OptionsConfigurationTest
{
    [TestInitialize]
    public void InitializeAppViewModel()
    {
        if (AppViewModel.Instance is null)
        {
            AppViewModel.Init(typeof(OptionsConfigurationTest).Assembly, new TestAppView());
        }

        AppViewModel.Instance!.ResetUserConfig();
    }

    [TestMethod]
    public void Options_AppliesAndCancelsDesktopSettings()
    {
        const string renderKey = "app.renderbackground";
        const string editorRenderKey = "diagrameditor.renderbackground";
        const string enhancedCopyKey = "integration.enhancedcopy";
        AppViewModel.Instance.SetSetting(renderKey, "None");
        AppViewModel.Instance.SetSetting(editorRenderKey, "Transparent");
        AppViewModel.Instance.SetSetting(enhancedCopyKey, false);

        try
        {
            Chordious.Desktop.ViewModels.OptionsViewModel applied = new()
            {
                SelectedRenderBackgroundIndex = (int)PreviewBackground.White,
                SelectedEditorRenderBackgroundIndex = (int)PreviewBackground.None,
                EnhancedCopy = true
            };

            Assert.IsTrue(applied.Dirty);
            Assert.IsTrue(applied.Apply.CanExecute(null));
            applied.Accept.Execute(null);
            Assert.IsTrue(applied.ProcessClose());
            Assert.AreEqual("White", AppViewModel.Instance.GetSetting(renderKey));
            Assert.AreEqual("None", AppViewModel.Instance.GetSetting(editorRenderKey));
            Assert.AreEqual("True", AppViewModel.Instance.GetSetting(enhancedCopyKey));

            Chordious.Desktop.ViewModels.OptionsViewModel cancelled = new()
            {
                SelectedRenderBackgroundIndex = (int)PreviewBackground.Transparent,
                EnhancedCopy = false
            };
            cancelled.Cancel.Execute(null);

            Assert.IsFalse(cancelled.ProcessClose());
            Assert.AreEqual("White", AppViewModel.Instance.GetSetting(renderKey));
            Assert.AreEqual("True", AppViewModel.Instance.GetSetting(enhancedCopyKey));
        }
        finally
        {
            AppViewModel.Instance.SetSetting(renderKey, "None");
            AppViewModel.Instance.SetSetting(editorRenderKey, "Transparent");
            AppViewModel.Instance.SetSetting(enhancedCopyKey, false);
        }
    }

    [TestMethod]
    public void AdvancedData_AppliesBufferedValueAndReportsChange()
    {
        const string key = "app.renderbackground";
        ChordiousSettings settingsBuffer = new(
            ConfigFile.DefaultConfig.ChordiousSettings,
            "Options test");
        settingsBuffer.Set(key, "None");
        bool? itemsChanged = null;
        ShowAdvancedDataMessage message = new(
            settingsBuffer,
            string.Empty,
            changed => itemsChanged = changed);
        AdvancedDataKVLT item = message.AdvancedDataVM.Items.Single(value => value.Key == key);

        item.Value = "White";
        message.AdvancedDataVM.Accept.Execute(null);
        message.Process();

        Assert.AreEqual(true, itemsChanged);
        Assert.AreEqual("White", settingsBuffer.Get(key));
    }

    [TestMethod]
    public void ConfigViewModels_ExportAndImportSelectedSettings()
    {
        const string key = "app.renderbackground";
        object outputRecipient = new();
        object confirmationRecipient = new();
        byte[]? exportedConfig = null;

        StrongReferenceMessenger.Default.Register<PromptForConfigOutputStreamMessage>(
            outputRecipient,
            (_, message) =>
            {
                MemoryStream output = new();
                message.Process(output);
                exportedConfig = output.ToArray();
            });
        StrongReferenceMessenger.Default.Register<ConfirmationMessage>(
            confirmationRecipient,
            (_, message) =>
            {
                message.ConfirmationVM.Accept.Execute(null);
                message.Process();
            });

        try
        {
            AppViewModel.Instance.SetSetting(key, "White");
            ConfigExportViewModel export = new();
            IncludeOnlySettings(export);
            export.Accept.Execute(null);

            Assert.IsNotNull(exportedConfig);
            StringAssert.Contains(Encoding.UTF8.GetString(exportedConfig), "app.renderbackground");

            AppViewModel.Instance.SetSetting(key, "Transparent");
            using ConfigImportViewModel import = new(new MemoryStream(exportedConfig));
            IncludeOnlySettings(import);
            import.Accept.Execute(null);

            Assert.AreEqual("White", AppViewModel.Instance.GetSetting(key));
        }
        finally
        {
            StrongReferenceMessenger.Default.UnregisterAll(outputRecipient);
            StrongReferenceMessenger.Default.UnregisterAll(confirmationRecipient);
            AppViewModel.Instance.SetSetting(key, "None");
        }
    }

    [TestMethod]
    public void DesktopLicenseCatalog_AddsCurrentDesktopDependencies()
    {
        LicensesViewModel licenses = new();

        DesktopLicenseCatalog.AddDesktopDependencies(licenses);

        Assert.AreEqual(5, licenses.Licenses.Count);
        Assert.IsTrue(licenses.Licenses.Any(license => license.Header.Contains("Avalonia")));
        Assert.IsTrue(licenses.Licenses.Any(license => license.Header.Contains("Svg.Skia")));
        Assert.IsTrue(licenses.Licenses.Any(license =>
            license.Header.Contains("SixLabors.ImageSharp") &&
            license.Body.Contains("Six Labors Split License")));
    }

    [TestMethod]
    public void Options_ImportsLegacyChordLineAndDisposesInputStream()
    {
        object fileRecipient = new();
        object nameRecipient = new();
        MemoryStream input = new(
            Encoding.UTF8.GetBytes("C;4;5;0;0;0;0;0;3"));

        StrongReferenceMessenger.Default.Register<PromptForLegacyImportMessage>(
            fileRecipient,
            (_, message) => message.Process("Classic.txt", input));
        StrongReferenceMessenger.Default.Register<PromptForTextMessage>(
            nameRecipient,
            (_, message) =>
            {
                message.TextPromptVM.Text = "Imported Classic";
                message.TextPromptVM.Accept.Execute(null);
            });

        try
        {
            Chordious.Desktop.ViewModels.OptionsViewModel options = new();
            options.LegacyImport.Execute(null);

            Assert.IsFalse(input.CanRead);
            DiagramLibraryViewModel library = new();
            ObservableDiagramLibraryNode imported = library.Nodes.Single(
                node => node.Name == "Imported Classic");
            Assert.AreEqual(1, imported.Diagrams.Count);
            Assert.AreEqual("C", imported.Diagrams[0].Title);
        }
        finally
        {
            StrongReferenceMessenger.Default.UnregisterAll(fileRecipient);
            StrongReferenceMessenger.Default.UnregisterAll(nameRecipient);
            input.Dispose();
        }
    }

    private static void IncludeOnlySettings(ConfigViewModelBase viewModel)
    {
        viewModel.IncludeSettings = true;
        viewModel.IncludeStyles = false;
        viewModel.IncludeInstruments = false;
        viewModel.IncludeChordQualities = false;
        viewModel.IncludeScales = false;
        viewModel.IncludeLibrary = false;
    }

    private sealed class TestAppView : IAppView
    {
        public void DoOnUIThread(Action action) => action();

        public object DoOnUIThread(Func<object> func) => func();

        public Stream GetAppConfigStream() => CreateEmptyConfigStream();

        public Stream GetUserConfigStreamToRead() => CreateEmptyConfigStream();

        public Stream GetUserConfigStreamToWrite() => new MemoryStream();

        public object SvgTextToImage(string svgText, int width, int height, bool editMode) =>
            new object();

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
