using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Enums;
using AssimilationSoftware.TodoSort.Core.Data;

namespace AssimilationSoftware.TodoSort.CLI.Interfaces;

public interface IItemDisplay
{
    IOrderedEnumerable<ActionItem> ApplySort(string sortTag, IEnumerable<ActionItem> list, SortOrder sort = SortOrder.Ascending);
    ActionItem? Disambiguate(IEnumerable<ActionItem> todoList, ITodoRepository repo, IItemDisplay view, bool autoAcceptOne = false, bool nsfw = false, bool verbose = false, bool includeCancel = false);
    string FormatTitle(ActionItem i, bool nsfw = false);
    void PrintItem(ActionItem i, int? index, ITodoRepository repo, bool nsfw = false, bool verbose = false);
    void PrintItems(string sortTag, IEnumerable<ActionItem> list, ITodoRepository repo, bool nsfw = false, bool verbose = false);
    void PrintTree(List<ActionItem> bold, List<ActionItem> strike, ITodoRepository repo, bool nsfw = false, bool verbose = false);
    void WriteLine(string message = "", params object?[]? args);
}