// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input;

using Chordious.Core;
using Chordious.Core.ViewModel;
using Chordious.Desktop;
using Chordious.Desktop.Services;
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
        DiagramLibraryViewModel libraryViewModel = new();
        _ = libraryViewModel.Nodes.Count;

        try
        {
            DiagramCollectionSelectorViewModel selector = new((name, newCollection) =>
            {
                selectedName = name;
                created = newCollection;
            }, collectionName);
            bool callbackCompletedBeforeClose = false;
            selector.RequestClose += () =>
            {
                callbackCompletedBeforeClose =
                    selectedName == collectionName &&
                    library.TryGet(collectionName, out _);
            };

            selector.Accept.Execute(null);

            Assert.AreEqual(collectionName, selectedName);
            Assert.IsTrue(created);
            Assert.IsTrue(selector.WasAccepted);
            Assert.IsTrue(library.TryGet(collectionName, out _));
            Assert.IsTrue(callbackCompletedBeforeClose);

            libraryViewModel.RefreshNodes();

            Assert.IsTrue(libraryViewModel.Nodes.Any(node => node.Name == collectionName));
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
    public async Task LibraryDragPayload_RoundTripsOnlyInsideApplication()
    {
        DiagramLibrary library = GetUserLibrary();
        string sourceName = NewCollectionName("drag-payload");

        try
        {
            library.Add(sourceName).Add(CreateDiagram("C"));
            ObservableDiagramLibraryNode sourceNode =
                new(PathUtils.PathRoot, sourceName, library);
            DiagramLibraryDragPayload expected =
                new(sourceNode, UseSelectedDiagrams: false);

            using DataTransfer dataTransfer =
                await DiagramDragDropService.CreateLibraryDataAsync(
                    new Border(),
                    expected);
            DiagramLibraryDragPayload? actual =
                DiagramDragDropService.TryGetLibraryPayload(dataTransfer);

            Assert.AreSame(expected, actual);
            Assert.IsFalse(dataTransfer.Formats.Contains(DataFormat.Text));
            Assert.IsFalse(dataTransfer.Formats.Contains(DataFormat.File));
        }
        finally
        {
            RemoveCollectionIfPresent(library, sourceName);
        }
    }

    [TestMethod]
    public void LibraryDropModifiers_UseOptionOrControlForCopy()
    {
        Assert.AreEqual(
            DiagramLibraryDropAction.Move,
            DiagramDragDropService.GetLibraryDropAction(KeyModifiers.None));
        Assert.AreEqual(
            DiagramLibraryDropAction.Copy,
            DiagramDragDropService.GetLibraryDropAction(KeyModifiers.Control));
        Assert.AreEqual(
            DiagramLibraryDropAction.Copy,
            DiagramDragDropService.GetLibraryDropAction(KeyModifiers.Alt));
    }

    [TestMethod]
    public void LibraryDrop_SelectedDiagramsCopyAndMoveToDestination()
    {
        DiagramLibrary library = GetUserLibrary();
        string sourceName = NewCollectionName("drag-source");
        string destinationName = NewCollectionName("drag-destination");

        try
        {
            DiagramCollection sourceCollection = library.Add(sourceName);
            DiagramCollection destinationCollection = library.Add(destinationName);
            sourceCollection.Add(CreateDiagram("C"));
            sourceCollection.Add(CreateDiagram("G"));

            ObservableDiagramLibraryNode sourceNode =
                new(PathUtils.PathRoot, sourceName, library);
            ObservableDiagramLibraryNode destinationNode =
                new(PathUtils.PathRoot, destinationName, library);
            sourceNode.SelectedDiagrams.Add(sourceNode.Diagrams[0]);
            DiagramLibraryDragPayload payload =
                new(sourceNode, UseSelectedDiagrams: true);

            Assert.IsTrue(DiagramDragDropService.ApplyLibraryDrop(
                payload,
                destinationNode,
                DiagramLibraryDropAction.Copy));
            Assert.AreEqual(2, sourceCollection.Count);
            Assert.AreEqual(1, destinationCollection.Count);
            Assert.AreNotSame(
                sourceCollection.DiagramAt(0),
                destinationCollection.DiagramAt(0));

            Assert.IsTrue(DiagramDragDropService.ApplyLibraryDrop(
                payload,
                destinationNode,
                DiagramLibraryDropAction.Move));
            Assert.AreEqual(1, sourceCollection.Count);
            Assert.AreEqual(2, destinationCollection.Count);
        }
        finally
        {
            RemoveCollectionIfPresent(library, sourceName);
            RemoveCollectionIfPresent(library, destinationName);
        }
    }

    [TestMethod]
    public void LibraryDrop_CopyWithinSameCollectionClonesSelectedDiagram()
    {
        DiagramLibrary library = GetUserLibrary();
        string sourceName = NewCollectionName("drag-clone");

        try
        {
            DiagramCollection sourceCollection = library.Add(sourceName);
            sourceCollection.Add(CreateDiagram("C"));
            ObservableDiagramLibraryNode sourceNode =
                new(PathUtils.PathRoot, sourceName, library);
            sourceNode.SelectedDiagrams.Add(sourceNode.Diagrams[0]);
            DiagramLibraryDragPayload payload =
                new(sourceNode, UseSelectedDiagrams: true);

            Assert.IsTrue(DiagramDragDropService.ApplyLibraryDrop(
                payload,
                sourceNode,
                DiagramLibraryDropAction.Copy));
            Assert.AreEqual(2, sourceCollection.Count);

            Assert.IsFalse(DiagramDragDropService.ApplyLibraryDrop(
                payload,
                sourceNode,
                DiagramLibraryDropAction.Move));
            Assert.AreEqual(2, sourceCollection.Count);
        }
        finally
        {
            RemoveCollectionIfPresent(library, sourceName);
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

    [TestMethod]
    public void MainWindowSelection_RejectsStaleDiagramAfterCollectionChanges()
    {
        DiagramLibrary library = GetUserLibrary();
        string sourceName = NewCollectionName("stale-source");
        string destinationName = NewCollectionName("stale-destination");

        try
        {
            library.Add(sourceName).Add(CreateDiagram("Dm"));
            library.Add(destinationName);
            ObservableDiagramLibraryNode sourceNode =
                new(PathUtils.PathRoot, sourceName, library);
            ObservableDiagramLibraryNode destinationNode =
                new(PathUtils.PathRoot, destinationName, library);
            ObservableDiagram staleDiagram = sourceNode.Diagrams[0];
            MainWindowViewModel viewModel =
                new(_ => Task.FromResult<string?>(null))
                {
                    SelectedLibraryNode = sourceNode
                };

            viewModel.SetSelectedLibraryDiagrams([staleDiagram], staleDiagram);
            viewModel.SelectedLibraryNode = destinationNode;
            viewModel.SetSelectedLibraryDiagrams([staleDiagram], staleDiagram);

            Assert.IsNull(viewModel.SelectedLibraryDiagram);
            Assert.AreEqual(0, destinationNode.SelectedDiagrams.Count);
            Assert.IsFalse(viewModel.HasSelectedLibraryDiagram);
        }
        finally
        {
            RemoveCollectionIfPresent(library, sourceName);
            RemoveCollectionIfPresent(library, destinationName);
        }
    }

    [TestMethod]
    public void ChordFinderSelection_SynchronizesResultsAndCommands()
    {
        ChordFinderViewModel viewModel = new();
        ObservableDiagram first = new(CreateDiagram("C"), name: "C");
        ObservableDiagram second = new(CreateDiagram("G"), name: "G");

        ChordFinderWindow.SynchronizeSelection(viewModel, [first, second, first]);

        Assert.AreEqual(2, viewModel.SelectedResults.Count);
        Assert.AreSame(first, viewModel.SelectedResults[0]);
        Assert.AreSame(second, viewModel.SelectedResults[1]);
        Assert.IsTrue(viewModel.SaveSelected.CanExecute(null));
        Assert.IsTrue(viewModel.EditSelected.CanExecute(null));
        Assert.IsTrue(viewModel.SendSelectedImageToClipboard.CanExecute(null));

        ChordFinderWindow.SynchronizeSelection(viewModel, []);

        Assert.AreEqual(0, viewModel.SelectedResults.Count);
        Assert.IsFalse(viewModel.SaveSelected.CanExecute(null));
        Assert.IsFalse(viewModel.EditSelected.CanExecute(null));
        Assert.IsFalse(viewModel.SendSelectedImageToClipboard.CanExecute(null));
    }

    [TestMethod]
    public async Task ChordFinderSearch_DefaultTargetRendersDiagrams()
    {
        ChordFinderViewModel viewModel = new();

        Assert.IsNotNull(viewModel.SelectedInstrument);
        Assert.IsNotNull(viewModel.SelectedTuning);
        Assert.IsNotNull(viewModel.SelectedChordQuality);
        Assert.IsTrue(viewModel.SearchAsync.CanExecute(null));

        viewModel.SearchAsync.Execute(null);

        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        while (!viewModel.IsIdle && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        Assert.IsTrue(viewModel.IsIdle, "A busca padrão não terminou no limite de 30 segundos.");
        Assert.IsTrue(viewModel.Results.Count > 0);
        Assert.IsTrue(viewModel.Results[0].SvgText.Contains("<svg"));
        Assert.IsNotNull(viewModel.Results[0].ImageObject);
    }

    [TestMethod]
    public async Task ChordFinderSearch_CancelReturnsToIdleState()
    {
        ChordFinderViewModel viewModel = new();

        viewModel.SearchAsync.Execute(null);
        viewModel.CancelSearch.Execute(null);

        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (!viewModel.IsIdle && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        Assert.IsTrue(viewModel.IsIdle, "A busca cancelada não retornou ao estado ocioso.");
        Assert.IsTrue(viewModel.SearchAsync.CanExecute(null));
    }

    [TestMethod]
    public void ScaleFinderSelection_SynchronizesResultsAndCommands()
    {
        ScaleFinderViewModel viewModel = new();
        ObservableDiagram first = new(CreateDiagram("C major"), name: "C major");
        ObservableDiagram second = new(CreateDiagram("G major"), name: "G major");

        ScaleFinderWindow.SynchronizeSelection(viewModel, [first, second, first]);

        Assert.AreEqual(2, viewModel.SelectedResults.Count);
        Assert.AreSame(first, viewModel.SelectedResults[0]);
        Assert.AreSame(second, viewModel.SelectedResults[1]);
        Assert.IsTrue(viewModel.SaveSelected.CanExecute(null));
        Assert.IsTrue(viewModel.EditSelected.CanExecute(null));
        Assert.IsTrue(viewModel.SendSelectedImageToClipboard.CanExecute(null));

        ScaleFinderWindow.SynchronizeSelection(viewModel, []);

        Assert.AreEqual(0, viewModel.SelectedResults.Count);
        Assert.IsFalse(viewModel.SaveSelected.CanExecute(null));
        Assert.IsFalse(viewModel.EditSelected.CanExecute(null));
        Assert.IsFalse(viewModel.SendSelectedImageToClipboard.CanExecute(null));
    }

    [TestMethod]
    public async Task ScaleFinderSearch_DefaultTargetRendersDiagrams()
    {
        ScaleFinderViewModel viewModel = new();

        Assert.IsNotNull(viewModel.SelectedInstrument);
        Assert.IsNotNull(viewModel.SelectedTuning);
        Assert.IsNotNull(viewModel.SelectedScale);
        Assert.IsTrue(viewModel.SearchAsync.CanExecute(null));

        viewModel.SearchAsync.Execute(null);

        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        while (!viewModel.IsIdle && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        Assert.IsTrue(viewModel.IsIdle, "A busca padrão de escalas não terminou no limite de 30 segundos.");
        Assert.IsTrue(viewModel.Results.Count > 0);
        Assert.IsTrue(viewModel.Results[0].SvgText.Contains("<svg"));
        Assert.IsNotNull(viewModel.Results[0].ImageObject);
    }

    [TestMethod]
    public async Task ScaleFinderSearch_CancelReturnsToIdleState()
    {
        ScaleFinderViewModel viewModel = new();

        viewModel.SearchAsync.Execute(null);
        viewModel.CancelSearch.Execute(null);

        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (!viewModel.IsIdle && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        Assert.IsTrue(viewModel.IsIdle, "A busca de escalas cancelada não retornou ao estado ocioso.");
        Assert.IsTrue(viewModel.SearchAsync.CanExecute(null));
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
