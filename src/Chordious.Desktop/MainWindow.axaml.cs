// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

using Chordious.Desktop.ViewModels;
using Chordious.Core.ViewModel;

namespace Chordious.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    internal void InitializeViewModel()
    {
        DataContext = new MainWindowViewModel(SaveSvgAsync);
    }

    private void DiagramsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.SetSelectedLibraryDiagrams(
            listBox.SelectedItems?.OfType<ObservableDiagram>() ?? Enumerable.Empty<ObservableDiagram>(),
            listBox.SelectedItem as ObservableDiagram);
    }

    private void LibraryNodesListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedLibraryNode = listBox.SelectedItem as ObservableDiagramLibraryNode;
        }
    }

    private async Task<string?> SaveSvgAsync(string svgText)
    {
        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Chordious diagram",
            SuggestedFileName = "Chordious-C.svg",
            DefaultExtension = "svg",
            FileTypeChoices =
            [
                new FilePickerFileType("Scalable Vector Graphics")
                {
                    Patterns = ["*.svg"],
                    MimeTypes = ["image/svg+xml"]
                }
            ]
        });

        if (file is null)
        {
            return null;
        }

        await using Stream stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        await using StreamWriter writer = new(stream, new UTF8Encoding(false));
        await writer.WriteAsync(svgText);

        return file.Path.LocalPath;
    }
}
