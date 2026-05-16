using APS_Automation_Server.Services;
using Autodesk.DataManagement.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Collections.Generic;

namespace APS_Automation_Server.Controllers
{
    [ApiController]
    [Route("auth")]
    public class HubControllers : ControllerBase
    {
        private readonly APSHubService _apsHubService;
        private readonly ApsAuthService _apsAuthService;
        public HubControllers(APSHubService apsHubService, ApsAuthService apsAuthService)
        {
            _apsHubService = apsHubService;
            _apsAuthService = apsAuthService;
        }


        [HttpGet("hub")]
        public async Task<ActionResult> ListHubs()
        {
            var tokens = await _apsAuthService.PrepareTokens(Request, Response, _apsAuthService);
            if (tokens == null)
            {
               
                return Unauthorized(new { error = "User not authenticated. Please login first via /auth/login" });
            }
            try
            {
                var hubs = await _apsHubService.GetHubs(new TokenResponse("Bearer", tokens.internal_Token, tokens.expires_at));
                var hubList = (from hub in hubs
                              select new { type = "Hubs", id = hub.Id, name = hub.Attributes.Name }).ToList();
                return Ok(hubList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch hubs", details = ex.Message, type = ex.GetType().Name });
            }
        }



        [HttpGet("{hub}/projects")]
        public async Task<ActionResult> ListProjects(string hub)
        {
            var tokens = await _apsAuthService.PrepareTokens(Request, Response, _apsAuthService);
            if (tokens == null)
            {
                return Unauthorized(new { error = "User not authenticated. Please login first via /auth/login" });
            }
            
            try
            {
                var projects = await _apsHubService.GetProjects(hub, new TokenResponse("Bearer", tokens.internal_Token, tokens.expires_at));
                var projectList = (from project in projects
                                  select new { type = "Projects", id = project.Id, name = project.Attributes.Name }).ToList();
                return Ok(projectList);
            }
            catch (Exception ex)
            {
               
                return StatusCode(500, new { error = "Failed to fetch projects", details = ex.Message });
            }
        }

        [HttpGet("{hub}/projects/{project}/contents")]
        public async Task<ActionResult> ListItems(string hub, string project, [FromQuery] string? folder_id)
        {
            var tokens = await _apsAuthService.PrepareTokens(Request, Response, _apsAuthService);
            if (tokens == null)
            {
                return Unauthorized(new { error = "User not authenticated. Please login first via /auth/login" });
            }
            
            try
            {
                var tokenResponse = new TokenResponse("Bearer", tokens.internal_Token, tokens.expires_at);
                
                if (string.IsNullOrEmpty(folder_id))
                {
                  
                    var folders = await _apsHubService.GetTopFolders(hub, project, tokenResponse);
                    return Ok(
                        from folder in folders
                        select new { type = "folders", id = folder.Id, name = folder.Attributes.DisplayName, folder = true }
                    );
                }
                else
                {
                 
                    var contents = await _apsHubService.GetFolderContents(project, folder_id, tokenResponse);
                   
                    var folders = new List<object>();
                    var items = new List<object>();

                    foreach (var entry in contents)
                    {
                        var typeName = entry?.GetType().Name;
                        if (typeName == "FolderData")
                        {
                            var id = entry.GetType().GetProperty("Id")?.GetValue(entry) as string;
                            var attributes = entry.GetType().GetProperty("Attributes")?.GetValue(entry);
                            var displayName = attributes?.GetType().GetProperty("DisplayName")?.GetValue(attributes) as string;
                            folders.Add(new { type = "folders", id = id, name = displayName, folder = true });
                        }
                        else if (typeName == "ItemData")
                        {
                            var id = entry.GetType().GetProperty("Id")?.GetValue(entry) as string;
                            var attributes = entry.GetType().GetProperty("Attributes")?.GetValue(entry);
                            var displayName = attributes?.GetType().GetProperty("DisplayName")?.GetValue(attributes) as string;
                            items.Add(new { type = "items", id = id, name = displayName, folder = false });
                        }
                        // unknown types are ignored
                    }

                    return Ok(folders.Concat(items));
                }
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, new { error = "Failed to fetch folder contents", details = ex.Message });
            }
        }

        [HttpGet("{hub}/projects/{project}/contents/{folderId}")]
        public async Task<ActionResult> ListItem(string hub, string project, string folderId)
        {
            var tokens = await _apsAuthService.PrepareTokens(Request, Response, _apsAuthService);
            if (tokens == null)
            {
                return Unauthorized(new { error = "User not authenticated. Please login first via /auth/login" });
            }
            
            try
            {
                var tokenResponse = new TokenResponse("Bearer", tokens.internal_Token, tokens.expires_at);
                
                if (string.IsNullOrEmpty(folderId))
                {
                    return Ok(
                     from folder in await _apsHubService.GetTopFolders(hub, project, tokenResponse)
                     select new { id = folder.Id, name = folder.Attributes.DisplayName, folder = true }
                 );
                }
                else
                {
                    var contents = await _apsHubService.GetFolderContents(project, folderId, tokenResponse);

                    var folders = new List<object>();
                    var items = new List<object>();

                    foreach (var entry in contents)
                    {
                        var typeName = entry?.GetType().Name;
                        if (typeName == "FolderData")
                        {
                            var id = entry.GetType().GetProperty("Id")?.GetValue(entry) as string;
                            var attributes = entry.GetType().GetProperty("Attributes")?.GetValue(entry);
                            var displayName = attributes?.GetType().GetProperty("DisplayName")?.GetValue(attributes) as string;
                            folders.Add(new { id = id, name = displayName, folder = true });
                        }
                        else if (typeName == "ItemData")
                        {
                            var id = entry.GetType().GetProperty("Id")?.GetValue(entry) as string;
                            var attributes = entry.GetType().GetProperty("Attributes")?.GetValue(entry);
                            var displayName = attributes?.GetType().GetProperty("DisplayName")?.GetValue(attributes) as string;
                            items.Add(new { id = id, name = displayName, folder = false });
                        }
                    }

                    return Ok(folders.Concat(items));
                }
            }
            catch (Exception ex)
            {
               
                return StatusCode(500, new { error = "Failed to fetch folder item", details = ex.Message });
            }
        }


        [HttpGet("{hub}/projects/{project}/contents/{item}/versions")]
        public async Task<ActionResult> ListVersions(string hub, string project, string item)
        {
            var tokens = await _apsAuthService.PrepareTokens(Request, Response, _apsAuthService);
            if (tokens == null)
            {
                return Unauthorized(new { error = "User not authenticated. Please login first via /auth/login" });
            }
            
            try
            {
                var versions = await _apsHubService.GetVersions(project, item, new TokenResponse("Bearer", tokens.internal_Token, tokens.expires_at));
                var versionList = (from version in versions
                                  select new { id = ApsDerivativeService.Base64Encode(version.Id), name = version.Attributes.Name }).ToList();
                return Ok(versionList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch versions", details = ex.Message });
            }
        }
    }
}
