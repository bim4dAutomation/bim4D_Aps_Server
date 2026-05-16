using Newtonsoft.Json.Linq;
using Autodesk.Forge.Core;
using Autodesk.DataManagement;
using Autodesk.DataManagement.Client;
using Autodesk.DataManagement.Model;

namespace APS_Automation_Server.Services
{
    public class APSHubService
    {
        public async Task<IEnumerable<HubData>> GetHubs(TokenResponse tokens)
        {
            try
            {
                Console.WriteLine($"GetHubs called with token: {tokens.access_token.Substring(0, Math.Min(20, tokens.access_token.Length))}...");
                var dataManagementClient = new DataManagementClient();
                var hubs = await dataManagementClient.GetHubsAsync(accessToken: tokens.access_token);
                
                Console.WriteLine($"API Response - Total hubs: {hubs.Data?.Count() ?? 0}");
                
                if (hubs.Data != null && hubs.Data.Any())
                {
                    foreach (var hub in hubs.Data)
                    {
                        Console.WriteLine($"  Hub: {hub.Attributes?.Name} (ID: {hub.Id})");
                    }
                }
                else
                {
                    Console.WriteLine("  No hubs returned from API");
                }
                
                return hubs.Data ?? Enumerable.Empty<HubData>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetHubs: {ex.GetType().Name} - {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task<IEnumerable<ProjectData>> GetProjects(string hubId, TokenResponse tokens)
        {
            var dataManagementClient = new DataManagementClient();
            var projects = await dataManagementClient.GetHubProjectsAsync(hubId, accessToken: tokens.access_token);
            return projects.Data;
        }

        public async Task<IEnumerable<TopFolderData>> GetTopFolders(string hubId, string projectId, TokenResponse tokens )
        {
            var dataManagementClient = new DataManagementClient();
            var folders = await dataManagementClient.GetProjectTopFoldersAsync(hubId, projectId, accessToken: tokens.access_token);
            return folders.Data;
        }

        public async Task<IEnumerable<IFolderContentsData>> GetFolderContents(string projectId, string folderId, TokenResponse tokens)
        {
            var dataManagementClient = new DataManagementClient();
            var contents = await dataManagementClient.GetFolderContentsAsync(projectId, folderId, accessToken: tokens.access_token);
            return contents.Data;
        }

        public async Task<IEnumerable<VersionData>> GetVersions(string projectId, string itemId, TokenResponse tokens)
        {
            var dataManagementClient = new DataManagementClient();
            var versions = await dataManagementClient.GetItemVersionsAsync(projectId, itemId, accessToken: tokens.access_token);
            return versions.Data;
        }
    }
}
