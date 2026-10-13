using AssimilationSoftware.TodoSort.CLI.Enums;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Export;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class ImportExportController
{
    public void Export(ExportSubOptions exportOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = false;

        // TODO: Work with Mustache# to externalise the formatting.
        // Write to the console if the filename is empty.
        if (string.IsNullOrEmpty(exportOptions.Filename))
        {
            view.WriteLine("Target filename is empty. You must specify a file name to write to.");
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
                    view.WriteLine("Unknown output file format.");
                    break;
            }
        }
        if (exporter != null)
        {
            vm.SearchSpecification = exportOptions.GetSearchSpecification(repo);
            if (!string.IsNullOrEmpty(exportOptions.SortTag))
            {
                exporter.Export(view.ApplySort(exportOptions.SortTag, vm.SearchResults).ToList());
            }
            else if (!string.IsNullOrEmpty(exportOptions.SortDescTag))
            {
                exporter.Export(view.ApplySort(exportOptions.SortDescTag, vm.SearchResults, SortOrder.Descending).ToList());
            }
            else
            {
                exporter.Export(vm.SearchResults.ToList());
            }
        }

        vm.Save();
    }
}