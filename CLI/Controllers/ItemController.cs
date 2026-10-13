using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core.Data;

namespace AssimilationSoftware.TodoSort.Core.Controllers;

public class ItemController
{
    public void AddItem(AddSubOptions a, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = false;
        var selected = new ActionItem { Context = a.Context, Title = a.ActionTitle };
        if (!string.IsNullOrWhiteSpace(a.Note))
        {
            selected.Notes.Add(a.Note);
        }
        view.WriteLine("Adding action '{0}' to context @{1}", a.ActionTitle, a.Context);
        vm.AddItem(selected);
        // Allow tagging right away.
        TagItem(vm, selected, view);
        view.PrintItem(selected, null, repo, false, true);
        vm.Save();
    }


    private void TagItem(ViewModel vm, ActionItem selected, IItemDisplay view)
    {
        string? tagName;
        do
        {
            view.WriteLine("What should this new tag be called? (ENTER to quit)");
            tagName = Console.ReadLine()?.ToLower()?.Trim();
            if (!string.IsNullOrEmpty(tagName))
            {
                view.WriteLine("What is the value of the tag?");
                var value = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    vm.SetTag(selected, tagName, value);
                }
                else if (selected.Tags.ContainsKey(tagName))
                {
                    vm.RemoveTag(selected, tagName);
                }
                view.WriteLine();
            }
        } while (!string.IsNullOrEmpty(tagName));
    }
}