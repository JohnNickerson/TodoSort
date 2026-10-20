using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Enums;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Export;
using AssimilationSoftware.TodoSort.Core.Import;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class ImportExportController
{
    private readonly IItemDisplay _view;
    private readonly TodoService _svc;

    public ImportExportController(TodoService svc, IItemDisplay view)
    {
        _view = view;
        _svc = svc;
    }

    public void Export(string? filename, string? templateFilename, ExportFormat? format, bool showAllItems, string? sortTag, string? sortDescTag, Core.Search.ISearchSpecification<ActionItem> searchSpecification)
    {
        // TODO: Write to the console if the filename is empty?
        if (string.IsNullOrEmpty(filename))
        {
            _view.WriteLine("Target filename is empty. You must specify a file name to write to.");
            return;
        }
        IExporter? exporter = null;
        if (!string.IsNullOrEmpty(templateFilename))
        {
            exporter = new TemplateExporter(filename, templateFilename);
        }
        else
        {
            switch (format)
            {
                case ExportFormat.html:
                    exporter = new HtmlExporter { Filename = filename };
                    break;
                case ExportFormat.graphviz:
                    exporter = new GraphVizExporter { Filename = filename };
                    break;
                case ExportFormat.todosort:
                    exporter = new TextExporter { Filename = filename };
                    break;
                case ExportFormat.json:
                    exporter = new JsonExporter { Filename = filename };
                    break;
                case ExportFormat.todotxt:
                    exporter = new TodoTxtExporter { Filename = filename };
                    break;
                default:
                    _view.WriteLine("Unknown output file format.");
                    break;
            }
        }
        if (exporter != null)
        {
            var searchResults = _svc.Search(searchSpecification, showAllItems);
            if (!string.IsNullOrEmpty(sortTag))
            {
                exporter.Export(_view.ApplySort(sortTag, searchResults).ToList());
            }
            else if (!string.IsNullOrEmpty(sortDescTag))
            {
                exporter.Export(_view.ApplySort(sortDescTag, searchResults, SortOrder.Descending).ToList());
            }
            else
            {
                exporter.Export(searchResults.ToList());
            }
        }

        _svc.Save();
    }

    public void Import(ImportSubOptions importOptions)
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
            _svc.AddAllItems(importOptions.Context, true, importer.GetAllItems());
            _view.WriteLine(_svc.StatusMessage);
        }

        _svc.Save();
    }
}