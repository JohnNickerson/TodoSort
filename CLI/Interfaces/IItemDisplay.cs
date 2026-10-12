using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Data;

namespace AssimilationSoftware.TodoSort.CLI.Interfaces;

public interface IItemDisplay
{
    string FormatTitle(ActionItem i, bool nsfw = false);
    void PrintItem(ActionItem i, int? index, ITodoRepository repo, bool nsfw = false, bool verbose = false);
}