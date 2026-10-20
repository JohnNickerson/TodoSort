using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Enums;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using System.Collections.Generic;
using System.Linq;

namespace AssimilationSoftware.TodoSort.Core.Interfaces;

public interface IItemDisplay
{
    IOrderedEnumerable<ActionItem> ApplySort(string sortTag, IEnumerable<ActionItem> list, SortOrder sort = SortOrder.Ascending);
    ActionItem? Disambiguate(IEnumerable<ActionItem> todoList, TodoService api, bool autoAcceptOne = false, bool nsfw = false, bool verbose = false, bool includeCancel = false);
    string FormatTitle(ActionItem i, bool nsfw = false);
    void PrintItem(ActionItem i, int? index, TodoService api, bool nsfw = false, bool verbose = false);
    void PrintItems(string sortTag, IEnumerable<ActionItem> list, TodoService api, bool nsfw = false, bool verbose = false);
    void PrintTree(List<ActionItem> bold, List<ActionItem> strike, TodoService api, bool nsfw = false, bool verbose = false);
    void WriteLine(string message = "", params object?[]? args);
}