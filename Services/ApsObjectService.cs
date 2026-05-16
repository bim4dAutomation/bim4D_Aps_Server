using Autodesk.Oss;
using Autodesk.Oss.Model;


namespace APS_Automation_Server.Services
{

    public class ApsObjectService
    {
    
        private readonly ApsAuthService _tokenService;
        

        public ApsObjectService( ApsAuthService tokenService)
        {
            _tokenService = tokenService;
            
        }

        private async Task EnsureBucketExists(string bucketKey)
        {
            var tokenResponse = await _tokenService.GetInternalToken();
            var ossClient = new OssClient();
            try
            {
                await ossClient.GetBucketDetailsAsync(bucketKey, accessToken: tokenResponse.access_token);
            }
            catch (OssApiException ex)
            {
                if (ex.HttpResponseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var payload = new CreateBucketsPayload
                    {
                        BucketKey = bucketKey,
                        PolicyKey = PolicyKey.Persistent
                    };
                    await ossClient.CreateBucketAsync(Region.US, payload, tokenResponse.access_token);
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<ObjectDetails> UploadModel(
            string bucketKey, string objectName, Stream fileStream)
        {
            await EnsureBucketExists(bucketKey);
            var tokenResponse = await _tokenService.GetInternalToken();
            var ossClient = new OssClient();
            var objectDetails = await ossClient.UploadObjectAsync(bucketKey, objectName, fileStream, accessToken: tokenResponse.access_token);
            return objectDetails;
        }

        public async Task<IEnumerable<ObjectDetails>> GetObjects(string bucketKey)
        {
            await EnsureBucketExists(bucketKey);
            var tokenResponse = await _tokenService.GetInternalToken();
            var ossClient = new OssClient();
            const int PageSize = 64;
            var results = new List<ObjectDetails>();
            var response = await ossClient.GetObjectsAsync(bucketKey, PageSize, accessToken: tokenResponse.access_token);
            results.AddRange(response.Items);
            while (!string.IsNullOrEmpty(response.Next))
            {
                var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(response.Next).Query);
                response = await ossClient.GetObjectsAsync(bucketKey, PageSize, startAt: queryParams["startAt"], accessToken: tokenResponse.access_token);
                results.AddRange(response.Items);
            }
            return results;
        }
    }

}
