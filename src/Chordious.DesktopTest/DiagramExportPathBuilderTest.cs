// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.IO;

using Chordious.Core;
using Chordious.Core.ViewModel;
using Chordious.Desktop.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chordious.DesktopTest;

[TestClass]
public class DiagramExportPathBuilderTest
{
    [TestMethod]
    public void BuildPath_ExpandsWindowsCompatibleNestedFilenameFormat()
    {
        ObservableCollection<ObservableDiagram> diagrams = CreateDiagrams("C:maj");
        DiagramExportPathBuilder builder = new();
        string outputPath = CreateUniqueOutputPath();

        string path = builder.BuildPath(
            outputPath,
            "%c\\diagram (%1 of %#) - %t %wx%h.%x",
            "My: Collection",
            diagrams,
            0,
            DiagramExportFormat.PNG,
            overwriteFiles: true);

        string expected = Path.Combine(
            outputPath,
            "My Collection",
            $"diagram (1 of 1) - Cmaj {diagrams[0].TotalWidth}x{diagrams[0].TotalHeight}.png");
        Assert.AreEqual(Path.GetFullPath(expected), path);
    }

    [TestMethod]
    public void BuildPath_GeneratesUniqueNamesWithinOneBatch()
    {
        ObservableCollection<ObservableDiagram> diagrams = CreateDiagrams("C");
        DiagramExportPathBuilder builder = new();
        string outputPath = CreateUniqueOutputPath();

        string first = builder.BuildPath(
            outputPath,
            "%t.%x",
            "",
            diagrams,
            0,
            DiagramExportFormat.SVG,
            overwriteFiles: true);
        builder.MarkCreated(first);

        string second = builder.BuildPath(
            outputPath,
            "%t.%x",
            "",
            diagrams,
            0,
            DiagramExportFormat.SVG,
            overwriteFiles: true);

        Assert.AreEqual(Path.Combine(outputPath, "C.svg"), first);
        Assert.AreEqual(Path.Combine(outputPath, "C (1).svg"), second);
    }

    [TestMethod]
    public void BuildPath_RespectsOverwriteChoiceForExistingFiles()
    {
        ObservableCollection<ObservableDiagram> diagrams = CreateDiagrams("C");
        DiagramExportPathBuilder builder = new();
        string outputPath = CreateUniqueOutputPath();
        Directory.CreateDirectory(outputPath);
        string existingPath = Path.Combine(outputPath, "C.svg");
        File.WriteAllText(existingPath, "existing");

        try
        {
            string overwritePath = builder.BuildPath(
                outputPath,
                "%t.%x",
                "",
                diagrams,
                0,
                DiagramExportFormat.SVG,
                overwriteFiles: true);
            string preservePath = builder.BuildPath(
                outputPath,
                "%t.%x",
                "",
                diagrams,
                0,
                DiagramExportFormat.SVG,
                overwriteFiles: false);

            Assert.AreEqual(existingPath, overwritePath);
            Assert.AreEqual(Path.Combine(outputPath, "C (1).svg"), preservePath);
        }
        finally
        {
            Directory.Delete(outputPath, recursive: true);
        }
    }

    [TestMethod]
    public void BuildPath_RejectsTraversalOutsideOutputFolder()
    {
        ObservableCollection<ObservableDiagram> diagrams = CreateDiagrams("C");
        DiagramExportPathBuilder builder = new();

        Assert.ThrowsException<InvalidOperationException>(() => builder.BuildPath(
            Path.Combine(Path.GetTempPath(), "Chordious export"),
            "../outside.%x",
            "",
            diagrams,
            0,
            DiagramExportFormat.SVG,
            overwriteFiles: true));
    }

    private static ObservableCollection<ObservableDiagram> CreateDiagrams(string title)
    {
        AppInfo.Assembly = typeof(DiagramExportPathBuilderTest).Assembly;
        Diagram diagram = new(ConfigFile.DefaultConfig.DiagramStyle, 6, 5)
        {
            Title = title
        };

        return [new ObservableDiagram(diagram, name: title)];
    }

    private static string CreateUniqueOutputPath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"Chordious-export-path-test-{Guid.NewGuid():N}");
    }
}
