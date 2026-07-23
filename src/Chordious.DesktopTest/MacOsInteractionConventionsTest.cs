// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Avalonia.Input;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chordious.DesktopTest;

[TestClass]
public sealed class MacOsInteractionConventionsTest
{
    [TestMethod]
    public void NativeMenus_ExposeMacOsApplicationAndWindowShortcuts()
    {
        XDocument app = LoadAxaml("App.axaml");
        XDocument mainWindow = LoadAxaml("MainWindow.axaml");

        AssertMenuGesture(app, "Ajustes…", "Meta+OemComma");
        AssertMenuItem(app, "Licenças…");

        string[] topLevelMenus = mainWindow
            .Root!
            .Elements(AvaloniaNamespace + "NativeMenu.Menu")
            .Elements(AvaloniaNamespace + "NativeMenu")
            .Elements(AvaloniaNamespace + "NativeMenuItem")
            .Select(item => item.Attribute("Header")?.Value)
            .OfType<string>()
            .ToArray();

        CollectionAssert.AreEquivalent(
            new[] { "Arquivo", "Editar", "Biblioteca", "Ferramentas", "Ajuda" },
            topLevelMenus);

        AssertMenuGesture(mainWindow, "Fechar janela", "Meta+W");
        AssertMenuGesture(mainWindow, "Novo diagrama", "Meta+N");
        AssertMenuGesture(mainWindow, "Editar diagrama selecionado", "Meta+E");
        AssertMenuGesture(mainWindow, "Exportar diagrama selecionado…", "Meta+Shift+E");
        AssertMenuGesture(mainWindow, "Copiar SVG do diagrama", "Meta+Shift+C");
        AssertMenuGesture(mainWindow, "Copiar imagem do diagrama", "Meta+Alt+C");
        AssertMenuGesture(mainWindow, "Chord Finder", "Meta+F");
        AssertMenuGesture(mainWindow, "Scale Finder", "Meta+Shift+F");
    }

    [TestMethod]
    public void NativeMenuGestures_AreAcceptedByAvalonia()
    {
        foreach (string fileName in new[] { "App.axaml", "MainWindow.axaml" })
        {
            XDocument document = LoadAxaml(fileName);
            foreach (string gesture in document
                         .Descendants(AvaloniaNamespace + "NativeMenuItem")
                         .Select(item => item.Attribute("Gesture")?.Value)
                         .OfType<string>())
            {
                Assert.IsNotNull(
                    KeyGesture.Parse(gesture),
                    $"O gesto '{gesture}' em {fileName} não foi aceito pelo Avalonia.");
            }
        }
    }

    [TestMethod]
    public void MainWindow_ExposesStableAccessibilityMetadata()
    {
        XDocument mainWindow = LoadAxaml("MainWindow.axaml");
        string[] automationIds = mainWindow
            .Descendants()
            .Select(element => element.Attribute("AutomationProperties.AutomationId")?.Value)
            .OfType<string>()
            .ToArray();

        string[] expectedAutomationIds =
        [
            "MainTabs",
            "ShowChordFinderButton",
            "ShowScaleFinderButton",
            "ShowInstrumentManagerButton",
            "ShowChordQualityManagerButton",
            "ShowScaleManagerButton",
            "ShowOptionsButton",
            "LaunchWebsiteButton",
            "ShowHelpButton",
            "ShowLicensesButton",
            "ExportPreviewSvgButton",
            "LibraryCollectionsList",
            "LibraryDiagramsList",
            "OverviewDiagramPreview",
            "LibraryDiagramPreview"
        ];

        foreach (string automationId in expectedAutomationIds)
        {
            CollectionAssert.Contains(automationIds, automationId);
        }

        Assert.IsTrue(mainWindow
            .Descendants()
            .Any(element =>
                element.Attribute("AutomationProperties.LiveSetting")?.Value == "Polite"));
    }

    [TestMethod]
    public void DialogButtons_DeclareDefaultAndCancelActions()
    {
        int defaultButtonCount = 0;
        int cancelButtonCount = 0;

        foreach (string path in Directory.EnumerateFiles(
                     DesktopProjectDirectory,
                     "*Window.axaml",
                     SearchOption.TopDirectoryOnly))
        {
            XDocument document = XDocument.Load(path);
            foreach (XElement button in document.Descendants(AvaloniaNamespace + "Button"))
            {
                string? command = button.Attribute("Command")?.Value;
                if (command is "{Binding Accept}" or "{Binding ExportAsync}")
                {
                    Assert.AreEqual(
                        "True",
                        button.Attribute("IsDefault")?.Value,
                        $"{Path.GetFileName(path)} deve executar a ação principal com Enter.");
                    defaultButtonCount++;
                }

                if (command is "{Binding Cancel}" or "{Binding CancelOrClose}" or "{Binding Close}")
                {
                    Assert.AreEqual(
                        "True",
                        button.Attribute("IsCancel")?.Value,
                        $"{Path.GetFileName(path)} deve executar cancelar/fechar com Escape.");
                    cancelButtonCount++;
                }
            }

            foreach (XElement buttonGroup in document.Descendants().Where(element =>
                         element.Elements(AvaloniaNamespace + "Button").Any()))
            {
                XElement[] buttons = buttonGroup
                    .Elements(AvaloniaNamespace + "Button")
                    .ToArray();
                int cancelIndex = Array.FindIndex(
                    buttons,
                    button => button.Attribute("Command")?.Value == "{Binding Cancel}");
                int acceptIndex = Array.FindIndex(
                    buttons,
                    button => button.Attribute("Command")?.Value == "{Binding Accept}");

                if (cancelIndex >= 0 && acceptIndex >= 0)
                {
                    Assert.IsTrue(
                        cancelIndex < acceptIndex,
                        $"{Path.GetFileName(path)} deve manter Cancelar à esquerda da ação principal.");
                }
            }
        }

        XElement licensesAcceptButton = LoadAxaml("LicensesWindow.axaml")
            .Descendants(AvaloniaNamespace + "Button")
            .Single(button => button.Attribute("Command")?.Value == "{Binding Accept}");
        Assert.AreEqual("True", licensesAcceptButton.Attribute("IsCancel")?.Value);

        Assert.IsTrue(defaultButtonCount >= 12);
        Assert.IsTrue(cancelButtonCount >= 16);
    }

    private static void AssertMenuGesture(XDocument document, string header, string gesture)
    {
        XElement item = AssertMenuItem(document, header);
        Assert.AreEqual(gesture, item.Attribute("Gesture")?.Value);
        Assert.IsTrue(
            item.Attribute("Command") is not null || item.Attribute("Click") is not null,
            $"O item de menu '{header}' precisa executar uma ação.");
    }

    private static XElement AssertMenuItem(XDocument document, string header)
    {
        XElement? item = document
            .Descendants(AvaloniaNamespace + "NativeMenuItem")
            .FirstOrDefault(candidate => candidate.Attribute("Header")?.Value == header);

        Assert.IsNotNull(item, $"O item de menu '{header}' não foi encontrado.");
        return item;
    }

    private static XDocument LoadAxaml(string fileName)
    {
        return XDocument.Load(Path.Combine(DesktopProjectDirectory, fileName));
    }

    private static string DesktopProjectDirectory
    {
        get
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                string candidate = Path.Combine(directory.FullName, "src", "Chordious.Desktop");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                candidate = Path.Combine(directory.FullName, "Chordious.Desktop");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "Não foi possível localizar o projeto Chordious.Desktop.");
        }
    }

    private static readonly XNamespace AvaloniaNamespace = "https://github.com/avaloniaui";
}
