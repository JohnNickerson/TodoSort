using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class NavigationController
{
    private void OpenItemTag(string tagValue)
    {
        var p = new System.Diagnostics.Process();
        p.StartInfo.FileName = tagValue;
        p.Start();
    }

    public void OpenTag(OpenTagSubOptions openTagOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !openTagOptions.ShowAllItems;

        // Read a tag and pass it through to the "start" argVerb. Intended for URLs and file names.
        vm.SearchSpecification = openTagOptions.SearchSpecification;
        var selected = view.Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(openTagOptions.ItemId));
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
                    view.WriteLine("What new title should this item have?");
                    var newTitle = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(newTitle))
                    {
                        vm.Rename(selected, newTitle);
                    }
                }
                if (openTagOptions.MarkAsDone)
                {
                    vm.MarkDone(null, selected);
                }
            }
            else
            {
                view.WriteLine("Tag not found: {0}", openTagOptions.Tag);
            }
            view.PrintItem(selected, null, repo, false, openTagOptions.Verbose);
            repo.SaveChanges();
        }
    }
}