using AssimilationSoftware.TodoSort.CLI.Enums;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Export;
using AssimilationSoftware.TodoSort.Core.Import;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class ImportExportController
{
    private readonly IItemDisplay _view;

    public ImportExportController(IItemDisplay view)
    {
        _view = view;
    }

    public void Export(ExportSubOptions exportOptions, TodoService vm)
    {
        // TODO: Write to the console if the filename is empty?
        if (string.IsNullOrEmpty(exportOptions.Filename))
        {
            _view.WriteLine("Target filename is empty. You must specify a file name to write to.");
            return;
        }
        IExporter? exporter = null;
        if (!string.IsNullOrEmpty(exportOptions.TemplateFilename))
        {
            exporter = new TemplateExporter(exportOptions.Filename, exportOptions.TemplateFilename);
        }
        else
        {
            switch (exportOptions.Format)
            {
                case ExportFormat.html:
                    exporter = new HtmlExporter { Filename = exportOptions.Filename };
                    break;
                case ExportFormat.graphviz:
                    exporter = new GraphVizExporter { Filename = exportOptions.Filename };
                    break;
                case ExportFormat.todosort:
                    exporter = new TextExporter { Filename = exportOptions.Filename };
                    break;
                case ExportFormat.json:
                    exporter = new JsonExporter { Filename = exportOptions.Filename };
                    break;
                case ExportFormat.todotxt:
                    exporter = new TodoTxtExporter { Filename = exportOptions.Filename };
                    break;
                default:
                    _view.WriteLine("Unknown output file format.");
                    break;
            }
        }
        if (exporter != null)
        {
            var searchResults = vm.Search(exportOptions.GetSearchSpecification(), exportOptions.ShowAllItems);
            if (!string.IsNullOrEmpty(exportOptions.SortTag))
            {
                exporter.Export(_view.ApplySort(exportOptions.SortTag, searchResults).ToList());
            }
            else if (!string.IsNullOrEmpty(exportOptions.SortDescTag))
            {
                exporter.Export(_view.ApplySort(exportOptions.SortDescTag, searchResults, SortOrder.Descending).ToList());
            }
            else
            {
                exporter.Export(searchResults.ToList());
            }
        }

        vm.Save();
    }

    public void Import(ImportSubOptions importOptions, TodoService vm)
    {
        IImporter? importer = null;
        switch (importOptions.Format)
        {
            case ImportFormat.todosort:
                if (Directory.Exists(importOptions.Filename))
                {
                    importer = new TextFolderImporter { Folder = importOptions.Filename };
                }
                else
                {
                    importer = new TextImporter { Filename = importOptions.Filename };
                }
                break;
            case ImportFormat.instapaper:
                importer = new InstapaperImporter(importOptions.Filename);
                break;
            case ImportFormat.urls:
                importer = new RawUrlsImporter(importOptions.Filename);
                break;
            default:
                _view.WriteLine($"Unknown import format: {importOptions.Format}");
                break;
        }
        if (importer != null)
        {
            // Get everything from the source file.
            // Exclude anything already seen, according to its import hash field.
            vm.AddAllItems(importOptions.Context, true, importer.GetAllItems());
            _view.WriteLine(vm.StatusMessage);
        }

        vm.Save();
    }
}