// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Chordious.Core;

namespace Chordious.CoreTest
{
    [TestClass]
    public class DiagramLibraryCompatibilityTest
    {
        [TestMethod]
        public void ConfigFile_RoundTripsDiagramLibraryWithoutDataLoss()
        {
            AppInfo.Assembly = typeof(DiagramLibraryCompatibilityTest).Assembly;

            ConfigFile source = new ConfigFile(ConfigFile.DefaultConfig, ConfigFile.UserLevelKey);
            DiagramCollection collection = source.DiagramLibrary.Add("Favoritos macOS");
            Diagram diagram = new Diagram(collection.Style, 6, 5)
            {
                Title = "C"
            };
            diagram.NewMark(new MarkPosition(2, 1));
            diagram.NewMark(new MarkPosition(4, 2));
            diagram.NewMark(new MarkPosition(5, 3));
            collection.Add(diagram);

            using MemoryStream stream = new MemoryStream();
            source.SaveFile(stream);
            stream.Position = 0;

            ConfigFile restored = new ConfigFile(ConfigFile.DefaultConfig, stream, ConfigFile.UserLevelKey);
            DiagramCollection restoredCollection = restored.DiagramLibrary.Get("Favoritos macOS");
            Diagram restoredDiagram = restoredCollection.DiagramAt(0);

            Assert.AreEqual(1, restoredCollection.Count);
            Assert.AreEqual("C", restoredDiagram.Title);
            Assert.AreEqual(6, restoredDiagram.NumStrings);
            Assert.AreEqual(5, restoredDiagram.NumFrets);
            StringAssert.Contains(restoredDiagram.ToImageMarkup(ImageMarkupType.SVG), "<svg");
        }
    }
}
