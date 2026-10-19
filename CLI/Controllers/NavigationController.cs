using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class NavigationController
{
    private readonly IItemDisplay _view;

    public NavigationController(IItemDisplay view)
    {
        _view = view;
    }

    private void OpenItemTag(string tagValue)
    {
        var p = new System.Diagnostics.Process();
        p.StartInfo.FileName = tagValue;
        p.Start();
    }

    public void OpenTag(OpenTagSubOptions openTagOptions, TodoService svc)
    {
        svc.ShowHeadOnly = !openTagOptions.ShowAllItems;

        // Read a tag and pass it through to the "start" argVerb. Intended for URLs and file names.
        svc.SearchSpecification = openTagOptions.SearchSpecification;
        var selected = _view.Disambiguate(svc.SearchResults, svc, !string.IsNullOrEmpty(openTagOptions.ItemId));
        if (selected != null)
        {
            if (selected.Tags.TryGetValue(openTagOptions.Tag ?? string.Empty, out string? tagValue))
            {
                if (openTagOptions.Copy)
                {
                    //System.Windows.Forms.Clipboard.SetText(tagValue);
                    // ^^ This doesn't work.
                }
                else
                {
                    OpenItemTag(tagValue);
                }
                if (openTagOptions.Rename)
                {
                    _view.WriteLine("What new title should this item have?");
                    var newTitle = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(newTitle))
                    {
                        svc.Rename(selected, newTitle);
                    }
                }
                if (openTagOptions.MarkAsDone)
                {
                    svc.MarkDone(null, selected);
                }
            }
            else
            {
                _view.WriteLine("Tag not found: {0}", openTagOptions.Tag);
            }
            _view.PrintItem(selected, null, svc, false, openTagOptions.Verbose);
            svc.Save();
        }
    }
}