using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PocketSharp;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.Maroon.Repositories;
using PocketSharp.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AssimilationSoftware.TodoSort.Sync;

class Program
{
    static async Task Main()
    {
        // Replace with your Pocket API consumer key and access token
        string consumerKey = "103918-ef23adaea7e86b894500de8";

        PocketClient client = new PocketClient(consumerKey, callbackUri: "http://www.google.com/");
        string requestCode = await client.GetRequestCode();

        if (string.IsNullOrEmpty(Settings.Default.AccessCode))
        {
            Uri authenticationUri = client.GenerateAuthenticationUri();
            OpenUrl(authenticationUri.AbsoluteUri);
            Console.WriteLine("Sign in, then press a key to continue here.");

            PocketUser user = await client.GetUser(requestCode);

            string accessToken = user.Code; // TODO: Sign in properly.
            Settings.Default.AccessCode = accessToken;
            Settings.Default.Save();
        }
        client.AccessCode = Settings.Default.AccessCode;
        var savePath = Environment.ExpandEnvironmentVariables(@"%OneDrive%\Reading\");
        var todoMapper = new AssimilationSoftware.Maroon.Mappers.Text.ActionItemDiskMapper(savePath);
        var repository = new TodoRepository(todoMapper, savePath, Environment.MachineName);

        // Weekly review process:
        // 1. Empty the Pocket archive.
        // 1a. Get all items in the Pocket archive.
        var archivedItems = await client.Get(RetrieveFilter.Archive);
        // 1b. For each item, delete in Pocket.
        foreach (var item in archivedItems)
        {
            // if (repository.Find((i) => i.Tags["pocketId"] == item.ID)) //item exists in TodoSort, 
            // {
            //      mark as done in TodoSort
            // }
            // await client.Delete(item);
            Console.WriteLine($"Removing from archive: {item.ID} - {item.Title}");
            bool isSuccess = await client.Delete(item);
        }
        client.AfterRequest = responseString =>
        {
            Console.WriteLine("Raw JSON response is: " + responseString);
        };
        Console.WriteLine("Press a key to continue...");
        Console.ReadKey();
        // 2. Import all Pocket items to TodoSort.
        var allItems = await client.Get();
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        foreach (var item in allItems)
        {
            // if item exists in TodoSort,
            // {
            //      if it's marked as done,
            //      {
            //          await client.Archive(item);
            //      }
            //      else
            //      {
            //          await client.ReplaceTags(item, new string[] {todoSortItem.Context});
            //      }
            // }
            // else
            // {
            //      add to TodoSort including a "pocketId:{item.ID}" tag.
            // }
            if (item?.Title == null && item?.Uri == null)
            {
                Console.WriteLine($"Skipping item with no URL ({item.ID})");
                continue;
            }
            else
            {
                await CreateItemAsync(repository, client, item, true);
            }
        }
        repository.SaveChanges();

        /*
        In pocket:
        If no TodoSort ID, add to TodoSort@pocket and tag in Pocket with TodoSort ID or tag in TodoSort with Pocket ID.
        Else if archived in Pocket and not marked done in TodoSort, Mark as done in TodoSort
        Else if done in Todo, archive
        Else if not in Todo, delete from Pocket

        in TodoSort:
        If not in Pocket, add to Pocket with ID
        */
        return;
    }

    private static async Task CreateItemAsync(TodoRepository repository, PocketClient client, PocketSharp.Models.PocketItem item, bool archiveOnSuccess)
    {
        if (item.Title.Contains("\n"))
        {
            item.Title = item.Title.Split("\n")[0];
        }
        ActionItem actionItem = new()
        {
            ID = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(item.Title) ? item.Uri.AbsoluteUri : item.Title.Split("\n")[0],
            Context = "pocket",
            Tags = new()
                {
                    { "url", item.Uri.AbsoluteUri },
                    { "pocketId", item.ID }
                }
        };
        Debug.WriteLine($"TODO: Check {actionItem.ID}:{actionItem.Title} in save path");
        repository.Create(actionItem);
        // 3. Archive all items in Pocket.
        bool isSuccess = archiveOnSuccess ? await client.Archive(item) : true;
        if (isSuccess) { repository.SaveChanges(); }
    }

    static void OpenUrl(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }
    static async Task<Dictionary<string, string>> GetRequestTokenAsync(HttpClient httpClient, string consumerKey)
    {
        var content = new StringContent($"consumer_key={consumerKey}&redirect_uri=dummy", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await httpClient.PostAsync("https://getpocket.com/v3/oauth/request", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return ParseQueryString(responseContent);
    }

    static async Task AuthorizeRequestTokenAsync(string consumerKey, string requestToken)
    {
        Console.WriteLine($"Authorize the following URL: https://getpocket.com/auth/authorize?request_token={requestToken}&redirect_uri=dummy");

        // Wait for user input to continue after authorization
        Console.WriteLine("Press Enter after authorization...");
        Console.ReadLine();
    }

    static async Task<Dictionary<string, string>> GetAccessTokenAsync(HttpClient httpClient, string consumerKey, string requestToken)
    {
        var content = new StringContent($"consumer_key={consumerKey}&code={requestToken}", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await httpClient.PostAsync("https://getpocket.com/v3/oauth/authorize", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return ParseQueryString(responseContent);
    }

    static Dictionary<string, string> ParseQueryString(string queryString)
    {
        var parameters = new Dictionary<string, string>();
        var pairs = queryString.Split('&');

        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=');
            parameters.Add(keyValue[0], keyValue[1]);
        }

        return parameters;
    }
}
