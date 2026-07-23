// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Chordious.Core;
using Chordious.Core.ViewModel;
using Chordious.Desktop.ViewModels;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chordious.DesktopTest;

[TestClass]
[DoNotParallelize]
public class DiagramLibraryOperationsTest
{
    [TestInitialize]
    public void InitializeAppViewModel()
    {
        if (AppViewModel.Instance is null)
        {
            AppViewModel.Init(typeof(DiagramLibraryOperationsTest).Assembly, new TestAppView());
        }
    }

    [TestMethod]
    public void CollectionSelector_AcceptCreatesAndReturnsNewCollection()
    {
        DiagramLibrary library = GetUserLibrary();
        string collectionName = NewCollectionName("selector");
        string? selectedName = null;
        bool? created = null;

        try
        {
            DiagramCollectionSelectorViewModel selector = new((name, newCollection) =>
            {
                selectedName = name;
                created = newCollection;
            }, collectionName);

            selector.Accept.Execute(null);

            Assert.AreEqual(collectionName, selectedName);
            Assert.IsTrue(created);
            Assert.IsTrue(selector.WasAccepted);
            Assert.IsTrue(library.TryGet(collectionName, out _));
        }
        finally
        {
            RemoveCollectionIfPresent(library, collectionName);
        }
    }

    [TestMethod]
    public void CollectionSelector_CancelDoesNotInvokeCallback()
    {
        bool callbackInvoked = false;
        DiagramCollectionSelectorViewModel selector = new((_, _) => callbackInvoked = true);

        selector.Cancel.Execute(null);

        Assert.IsFalse(selector.WasAccepted);
        Assert.IsFalse(callbackInvoked);
    }

    [TestMethod]
    public void LibraryCommands_CopyMoveAndMergePreserveExpectedDiagrams()
    {
        DiagramLibrary library = GetUserLibrary();
        string copySourceName = NewCollectionName("copy-source");
        string copyDestinationName = NewCollectionName("copy-destination");
        string moveSourceName = NewCollectionName("move-source");
        string moveDestinationName = NewCollectionName("move-destination");

        try
        {
            DiagramCollection copySource = library.Add(copySourceName);
            DiagramCollection copyDestination = library.Add(copyDestinationName);
            copySource.Add(CreateDiagram("C"));
            copySource.Add(CreateDiagram("G"));

            ObservableDiagramLibraryNode copyNode = new(PathUtils.PathRoot, copySourceName, library);
            copyNode.CopyNode.Execute(copyDestinationName);

            Assert.AreEqual(2, copySource.Count);
            Assert.AreEqual(2, copyDestination.Count);
            Assert.AreNotSame(copySource.DiagramAt(0), copyDestination.DiagramAt(0));

            DiagramCollection moveSource = library.Add(moveSourceName);
            DiagramCollection moveDestination = library.Add(moveDestinationName);
            moveSource.Add(CreateDiagram("Am"));
            moveSource.Add(CreateDiagram("F"));

            ObservableDiagramLibraryNode moveNode = new(PathUtils.PathRoot, moveSourceName, library);
            moveNode.SelectedDiagrams.Add(moveNode.Diagrams[0]);
            moveNode.MoveSelected.Execute(moveDestinationName);

            Assert.AreEqual(1, moveSource.Count);
            Assert.AreEqual(1, moveDestination.Count);

            ObservableDiagramLibraryNode mergeNode = new(PathUtils.PathRoot, moveSourceName, library);
            mergeNode.MergeNode.Execute(moveDestinationName);

            Assert.AreEqual(0, moveSource.Count);
            Assert.AreEqual(2, moveDestination.Count);
        }
        finally
        {
            RemoveCollectionIfPresent(library, copySourceName);
            RemoveCollectionIfPresent(library, copyDestinationName);
            RemoveCollectionIfPresent(library, moveSourceName);
            RemoveCollectionIfPresent(library, moveDestinationName);
        }
    }

    [TestMethod]
    public void MainWindowSelection_SynchronizesMultipleSelectedDiagrams()
    {
        DiagramLibrary library = GetUserLibrary();
        string collectionName = NewCollectionName("selection");

        try
        {
            DiagramCollection collection = library.Add(collectionName);
            collection.Add(CreateDiagram("Dm"));
            collection.Add(CreateDiagram("A"));
            ObservableDiagramLibraryNode node = new(PathUtils.PathRoot, collectionName, library);
            MainWindowViewModel viewModel = new(_ => Task.FromResult<string?>(null))
            {
                SelectedLibraryNode = node
            };

            viewModel.SetSelectedLibraryDiagrams([node.Diagrams[0], node.Diagrams[1]], node.Diagrams[1]);

            Assert.AreEqual(2, node.SelectedDiagrams.Count);
            Assert.AreSame(node.Diagrams[1], viewModel.SelectedLibraryDiagram);
            Assert.IsTrue(node.CopySelected.CanExecute(null));
            Assert.IsTrue(node.MoveSelected.CanExecute(null));
            Assert.IsTrue(node.CloneSelected.CanExecute(null));
            Assert.IsTrue(node.DeleteSelected.CanExecute(null));

            viewModel.SelectedLibraryNode = null;

            Assert.AreEqual(0, node.SelectedDiagrams.Count);
            Assert.IsNull(viewModel.SelectedLibraryDiagram);
            Assert.IsFalse(viewModel.HasSelectedLibraryDiagram);

            viewModel.SetSelectedLibraryDiagrams([node.Diagrams[0]], node.Diagrams[0]);
            viewModel.SelectedLibraryNode = null;

            Assert.IsNull(viewModel.SelectedLibraryDiagram);
            Assert.IsFalse(viewModel.HasSelectedLibraryDiagram);
        }
        finally
        {
            RemoveCollectionIfPresent(library, collectionName);
        }
    }

    private static Diagram CreateDiagram(string title)
    {
        return new Diagram(ConfigFile.DefaultConfig.DiagramStyle, 6, 5)
        {
            Title = title
        };
    }

    private static DiagramLibrary GetUserLibrary()
    {
        PropertyInfo userConfigProperty = typeof(AppViewModel).GetProperty(
            "UserConfig",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        ConfigFile userConfig = (ConfigFile)userConfigProperty.GetValue(AppViewModel.Instance)!;
        return userConfig.DiagramLibrary;
    }

    private static string NewCollectionName(string purpose)
    {
        return $"Desktop {purpose} {Guid.NewGuid():N}";
    }

    private static void RemoveCollectionIfPresent(DiagramLibrary library, string name)
    {
        if (library.TryGet(name, out _))
        {
            library.Remove(PathUtils.PathRoot, name);
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

        public IEnumerable<string> GetSystemFonts() => Array.Empty<string>();

        private static Stream CreateEmptyConfigStream()
        {
            return new MemoryStream("<chordious />"u8.ToArray());
        }
    }
}
