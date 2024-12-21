
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace SyncPLWUsers
{
    public static class SyncPLWUsers
    {
        [FunctionName("SyncPLWUsers")]
        public static async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"SyncPLWUsers function executed at: {DateTime.Now}");

            var apiUrl = Environment.GetEnvironmentVariable("PLW_API_URL");
            var username = Environment.GetEnvironmentVariable("PLW_API_USERNAME");
            var password = Environment.GetEnvironmentVariable("PLW_API_PASSWORD");
            var connectionString = Environment.GetEnvironmentVariable("BLOB_CONNECTION_STRING");
            var containerName = Environment.GetEnvironmentVariable("BLOB_CONTAINER_NAME");

            var usersData = await FetchPLWUsers(apiUrl, username, password);
            var fileName = $"users_{DateTime.UtcNow:yyyyMMddHHmmss}.json";

            await StoreDataInBlob(connectionString, containerName, fileName, usersData);

            log.LogInformation($"User data successfully stored in blob: {fileName}");
        }

        private static async Task<string> FetchPLWUsers(string apiUrl, string username, string password)
        {
            using var client = new HttpClient();
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");

            var response = await client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private static async Task StoreDataInBlob(string connectionString, string containerName, string fileName, string content)
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainerClient.GetBlobClient(fileName);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            await blobClient.UploadAsync(stream, overwrite: true);
        }
    }
}
