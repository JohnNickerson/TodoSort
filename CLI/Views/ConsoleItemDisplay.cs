using System.Text;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Enums;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using Spectre.Console;

namespace AssimilationSoftware.TodoSort.CLI.Views;

public class ConsoleItemDisplay : IItemDisplay
{
    private bool _verbose;

    public ConsoleItemDisplay(bool verbose = false)
    {
        _verbose = verbose;
    }

    public IOrderedEnumerable<ActionItem> ApplySort(string sortTag, IEnumerable<ActionItem> list, SortOrder sort = SortOrder.Ascending)
    {
        var sortedList = sort == SortOrder.Descending ? list.OrderByDescending(a => a.Context) : from a in list orderby a.Context select a;
        if (sortTag == "done-date")
        {
            if (sort == SortOrder.Descending)
                sortedList = sortedList.ThenByDescending(i => i.DoneDate ?? DateTime.Now);
            else
                sortedList = sortedList.ThenBy(i => i.DoneDate ?? DateTime.Now);
        }
        else if (sortTag == "tickle-date")
        {
            if (sort == SortOrder.Descending)
                sortedList = sortedList.ThenByDescending(i => i.TickleDate ?? DateTime.Now);
            else
                sortedList = sortedList.ThenBy(i => i.TickleDate ?? DateTime.Now);
        }
        else if (sortTag == "upvotes")
        {
            if (sort == SortOrder.Descending)
                sortedList = sortedList.ThenByDescending(i => i.Upvotes);
            else
                sortedList = sortedList.ThenBy(i => i.Upvotes);
        }
        else if (sortTag == "title" || string.IsNullOrEmpty(sortTag))
        {
            if (sort == SortOrder.Descending)
                sortedList = sortedList.ThenByDescending(i => i.Title);
            else
                sortedList = sortedList.ThenBy(i => i.Title);
        }
        else
        {
            if (sort == SortOrder.Descending)
                sortedList = sortedList.ThenByDescending(a => a.Tags.ContainsKey(sortTag) ? a.Tags[sortTag] : "0", new SemiNumericComparer());
            else
                sortedList = sortedList.ThenBy(a => a.Tags.ContainsKey(sortTag) ? a.Tags[sortTag] : "0", new SemiNumericComparer());
        }
        return sortedList;
    }

    public string FormatTitle(ActionItem i, bool nsfw = false)
    {
        var maskTitle = (i.Tags?.ContainsKey("nsfw") ?? false && !nsfw) ? $"(nsfw) {Rot13.Transform(i.Title)}" : i.Title;
        maskTitle = maskTitle.EscapeMarkup();
        if (i.Tags?.TryGetValue("type", out var itemType) ?? false)
        {
            var typeIcon = i.Tags["type"].ToUpper() switch
            {
                // These don't seem to display on Windows by default, so commented out. :(
                // TODO: Use ascii art or something?
                // "MOVIE" => ":movie_camera:", // [ ]<
                // "TV" => ":television:", // [_]
                // "BOOK" => ":open_book:", // \/
                // "GAME" => ":video_game:", // <+>
                _ => $"[[{i.Tags["type"].ToUpper()}]]"
            };
            return string.Format("{0} {1}", typeIcon, maskTitle);
        }
        else
        {
            return maskTitle;
        }
    }

    public void PrintItem(ActionItem i, int? index, ITodoRepository repo, bool nsfw = false, bool verbose = false)
    {
        _verbose = verbose;
        var wrapWidth = Console.WindowWidth - 1;
        var title = FormatTitle(i, nsfw);
        if (i.DoneDate.HasValue)
        {
            title = string.Format("[green][[{0:yyyy-MM-dd}]][/] {1}", i.DoneDate.Value, title);
        }
        if (i.TickleDate.HasValue)
        {
            title = string.Format("[blue][[{0:yyyy-MM-dd}]][/] {1}", i.TickleDate.Value, title);
        }
        if (i.IsDeleted)
        {
            // NOTE: I think we only get here for conflict editing.
            title = $"[red][[DELETED]][/] {title}";
        }

        if (index.HasValue)
        {
            var prefix = new StringBuilder();
            prefix.Append(index);
            prefix.Append(':');
            prefix.Append(' ', Math.Max(4 - prefix.Length, 0));
            WrapOutput(prefix.ToString(), title, wrapWidth);
        }
        else
        {
            WrapOutput("-   ", title, wrapWidth);
        }
        if (_verbose)
        {
            if (i.Notes.Count > 0)
            {
                foreach (var n in i.Notes)
                {
                    WrapOutput("        - ", n, wrapWidth);
                }
            }
            if (i.Tags?.Count > 0)
            {
                foreach (var k in i.Tags)
                {
                    WrapOutput($"        #{k.Key}:", k.Value, wrapWidth);
                }
            }
            if (i.Upvotes > 0)
            {
                WrapOutput("        #upvotes:", i.Upvotes.ToString(), wrapWidth);
            }
            if (i.DoneDate.HasValue)
            {
                WrapOutput("        #done-date:", i.DoneDate.Value.ToString("yyyy-MM-dd"), wrapWidth);
            }
            if (i.TickleDate.HasValue)
            {
                WrapOutput("        #tickle-date:", i.TickleDate.Value.ToString("yyyy-MM-dd"), wrapWidth);
            }
            WrapOutput("        #ID:", i.ID.ToString(), wrapWidth);
            if (i.ProjectId != null)
            {
                WrapOutput("        #project:", string.Format("{0} - {1}", i.ProjectId, i.GetProject(repo).Title), wrapWidth);
            }
            WrapOutput("        #context:", i.Context, wrapWidth);
            WrapOutput("        #last-modified:", i.LastModified.ToString("yyyy-MM-dd"), wrapWidth);
        }
    }

    public void PrintItems(string sortTag, IEnumerable<ActionItem> list, ITodoRepository repo, bool nsfw = false, bool verbose = false)
    {
        var last_context = string.Empty;
        var sortedList = ApplySort(sortTag, list);
        foreach (var i in sortedList)
        {
            if (i.Context != last_context)
            {
                Console.WriteLine("@{0}", i.Context);
            }
            PrintItem(i, null, repo, nsfw, verbose);
            last_context = i.Context;
        }
    }

    private void WrapOutput(string indent, string content, int width)
    {
        var printWidth = width - indent.Length;
        var breaks = " \t-/=&+_";
        var line = new StringBuilder();
        line.Append(indent);
        while (content.Length > printWidth)
        {
            var snip = Math.Min(printWidth, content.Length);
            for (var i = snip; i > 0; i--)
            {
                if (breaks.Contains(content[i]))
                {
                    snip = i;
                    break;
                }
            }
            line.Append(content.Substring(0, snip));
            content = content.Remove(0, snip).TrimStart();
            AnsiConsole.MarkupLine(line.ToString());
            line.Clear();
            line.Append(' ', indent.Length);
        }
        AnsiConsole.Markup(line.ToString());
        AnsiConsole.MarkupLine(content);
    }
}