using APS_Automation_Server.Models;
using APS_Automation_Server.Services;
using Autodesk.Oss.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http.Headers;
using System.Text.Json;

[ApiController]
[Route("auth")]
public class ModelControllers : ControllerBase
{
    public record BucketObject(string name, string urn);
    private readonly IConfiguration _config;
    private readonly ApsObjectService _objectService;
    private readonly ApsDerivativeService _translationService;
    private readonly ApsAuthService _apsAuthService;
    
    public ModelControllers(IConfiguration config ,ApsObjectService apsObjectService, ApsDerivativeService translationService,ApsAuthService apsAuthService)
    { 
        _config = config;
        _objectService = apsObjectService;
        _translationService = translationService;
        _apsAuthService = apsAuthService;
       
    }

    
    [HttpGet("login")]
    public IActionResult Login()
    {
        var redirectUri = _apsAuthService.GetAuthorizationURL();
        return Redirect(redirectUri);
    }

    [HttpGet("logout")]
    public ActionResult Logout()
    {
        Response.Cookies.Delete("public_token");
        Response.Cookies.Delete("internal_token");
        Response.Cookies.Delete("refresh_token");
        Response.Cookies.Delete("expires_at");
        return Redirect("/");
    }

    [HttpGet("callback")]
    public async Task<ActionResult> Callback(string code)
    {
        var tokens = await _apsAuthService.GenerateThreeLeggedTokens(code);
        Response.Cookies.Append("public_token", tokens.public_token);
        Response.Cookies.Append("internal_token", tokens.internal_Token);
        Response.Cookies.Append("refresh_token", tokens.refresh_Token);
        Response.Cookies.Append("expires_at", tokens.expires_at.ToString());
        return Redirect("http://localhost:5173/Viewer");
    }
    

    [HttpGet("profile")]
    public async Task<ActionResult> GetProfile()
    {
        var tokens = await _apsAuthService.PrepareTokens(Request, Response, _apsAuthService);
        if (tokens == null)
        {
            return Unauthorized();
        }
        var profile = await _apsAuthService.GetUserProfile(tokens);
        return Ok(new { name = profile.Name,About= profile.AboutMe });
    }

    [HttpGet("token")]
    public async Task<ActionResult> GetPublicToken()
    {
        var tokens = await _apsAuthService.GetInternalToken();
        if (tokens == null)
        {
            return Unauthorized();
        }
        var expiresIn = Math.Floor((tokens.expires_in - DateTime.Now.ToUniversalTime()).TotalSeconds);
        return Ok(new { tokens.access_token, expires_in = expiresIn });
    }




    [HttpGet("models")]
    public async Task<IEnumerable<BucketObject>> GetModels()
    {

        var bucketKey = _config["Autodesk:BucketKey"]!;
        var objects = await _objectService.GetObjects(bucketKey);
        return from o in objects
               select new BucketObject(o.ObjectKey, ApsDerivativeService.Base64Encode(o.ObjectId));
    }





    [HttpPost("upload")]
    public async Task<IActionResult> UploadAndTranslate(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var bucketKey = _config["Autodesk:BucketKey"]!;
        var objectName = file.FileName;
        using var stream = file.OpenReadStream();
        var obj = await _objectService.UploadModel(
            bucketKey,
            objectName,
            stream
        );
        await _translationService.TranslateModel(
            obj.ObjectId,
            objectName
        );

        var urn = ApsDerivativeService.Base64Encode(obj.ObjectId);
        return Ok(new
        {
            urn,
            objectId = obj.ObjectId,
            objectKey = obj.ObjectKey
        });
    }


    [HttpGet("{urn}/status")]
    public async Task<TranslationStatus> GetModelStatus(string urn)
    {
        var status = await _translationService.GetTranslationStatus(urn);
        return status;
    }

    
}
