using Autodesk.ModelDerivative;
using Autodesk.ModelDerivative.Model;
using System.Net.Http.Headers;
using System.Transactions;

namespace APS_Automation_Server.Services
{
    public record TranslationStatus(string Status, string Progress, IEnumerable<string> Messages);
    public class ApsDerivativeService
    {
        private readonly ApsAuthService _tokenService;

        public ApsDerivativeService(ApsAuthService tokenService)
        {
            _tokenService = tokenService;
        }
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes).TrimEnd('=').Replace('+', '-')
        .Replace('/', '_');
        }

        public async Task<Job> TranslateModel(string objectId, string rootFilename)
        {
            var tokenResponse = await _tokenService.GetInternalToken();
            var modelDerivativeClient = new ModelDerivativeClient();
            var payload = new JobPayload
            {
                Input = new JobPayloadInput
                {
                    Urn = Base64Encode(objectId)
                },
                
                Output = new JobPayloadOutput
                {
                    Formats =
                    [
                        new JobPayloadFormatSVF2
                    {
                        Views = [View._2d, View._3d]
                    }
                    ]
                },
               
            };
            if (!string.IsNullOrEmpty(rootFilename))
            {
                payload.Input.RootFilename = rootFilename;
                payload.Input.CompressedUrn = true;
            }
            var job = await modelDerivativeClient.StartJobAsync(jobPayload: payload, region: Region.US, accessToken: tokenResponse.access_token);
            return job;
        }

        


        public async Task<TranslationStatus> GetTranslationStatus(string urn)
        {
            var tokenResponse = await _tokenService.GetInternalToken();
            var modelDerivativeClient = new ModelDerivativeClient();
            try
            {
                var manifest = await modelDerivativeClient.GetManifestAsync(urn, accessToken: tokenResponse.access_token);

                var messages = ExtractMessagesFromManifest(manifest);
       
                return new TranslationStatus(manifest.Status ?? "unknown", manifest.Progress ?? string.Empty, messages);
            }
            catch (ModelDerivativeApiException ex)
            {
                if (ex.HttpResponseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new TranslationStatus("n/a", string.Empty, System.Array.Empty<string>());
                }
                else
                {
                    throw;
                }
            }
        }

        private static IEnumerable<string> ExtractMessagesFromManifest(Manifest manifest)
        {
            return manifest.Derivatives?
                .SelectMany(d => d.Messages ?? [])
                .Select(m => $"[{m.Type}] {m.Code}: {m.Message}")
                .ToList() ?? [];
        }
    }
}
