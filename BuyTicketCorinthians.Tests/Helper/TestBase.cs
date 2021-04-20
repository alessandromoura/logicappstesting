using Microsoft.Azure.Management.Logic;
using AD = Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Microsoft.Azure.Management.Logic.Models;
using System.Net.Http;

namespace BuyTicketCorinthians.Tests.Helper
{
    public abstract class TestBase
    {
        const string TRIGGER_NAME = "manual";
        LogicManagementClient _client;
        string _resourceGroupName;
        string _logicAppName;
        string _opportunityId;
        string _tenantId;
        string _clientId;
        string _clientSecret;
        string _subscriptionId;
        string _environment;
        JsonSerializerOptions _serializerOptions;
        HttpClient _httpClient;

        public string ResourceGroupName { get { return _resourceGroupName; } }
        public string LogicAppName { get { return _logicAppName; } }


        [TestInitialize]
        public async Task Initialize()
        {
            _serializerOptions = new JsonSerializerOptions
            {
                IgnoreNullValues = true
            };

            // Read these settings from an environment variable for local test or from Key Vault for Azure DevOps
            /* Get local environment variables using powershell:
               Get-ChildItem -Path Env:AZURE*
               
               Set system or user environment variables using Powershell:
               [System.Environment]::SetEnvironmentVariable('AZURE_TENANT_ID','XXX',[System.EnvironmentVariableTarget]::Machine|User)
               [System.Environment]::SetEnvironmentVariable('AZURE_CLIENT_ID','YYY',[System.EnvironmentVariableTarget]::Machine)
               [System.Environment]::SetEnvironmentVariable('AZURE_CLIENT_SECRET','ZZZ',[System.EnvironmentVariableTarget]::Machine)
               [System.Environment]::SetEnvironmentVariable('AZURE_SUBSCRIPTION_ID','XYZ',[System.EnvironmentVariableTarget]::Machine)
            */
            _tenantId = Environment.GetEnvironmentVariable("ACSUG_TENANT_ID");
            _clientId = Environment.GetEnvironmentVariable("ACSUG_CLIENT_ID");
            _clientSecret = Environment.GetEnvironmentVariable("ACSUG_CLIENT_SECRET");
            _subscriptionId = Environment.GetEnvironmentVariable("ACSUG_SUBSCRIPTION_ID");

            if (string.IsNullOrEmpty(_tenantId) ||
                string.IsNullOrEmpty(_clientId) ||
                string.IsNullOrEmpty(_clientSecret) ||
                string.IsNullOrEmpty(_subscriptionId))
            {
                throw new Exception("authentication credentials are invalid!");
            }

            _resourceGroupName = $"ACSUG-LogicApps-Testing";
            _logicAppName = $"lapp-buyticketcorinthians-dev";

            ServiceClientCredentials credentials = await GetCredentials(_tenantId, _clientId, _clientSecret);
            _client = new LogicManagementClient(credentials);
            _client.SubscriptionId = _subscriptionId;

            _httpClient = new HttpClient();
        }

        protected LogicManagementClient LogicAppClient { get { return _client; } }
        protected JsonSerializerOptions SerializerOptions { get { return _serializerOptions; } }

        protected async Task<T> GetHttpResponseMessage<T>(HttpResponseMessage responseMessage, bool extractBody = false)
        {
            var responseContent = responseMessage.Content;
            var streamContent = await responseContent.ReadAsStreamAsync();
            if (extractBody)
            {
                var bodyContent = await JsonSerializer.DeserializeAsync<Models.LogicAppsActionResponse<T>>(streamContent, SerializerOptions);
                return bodyContent.body;
            }
            else
            {
                return await JsonSerializer.DeserializeAsync<T>(streamContent, SerializerOptions);
            }
        }

        protected async Task<HttpResponseMessage> CallLogicApp<T>(T input, string opportunityId)
        {
            var requestContent = new StringContent(JsonSerializer.Serialize<T>(
                input, SerializerOptions), Encoding.UTF8, "application/json");
            return await _httpClient.PostAsync(await GetLogicAppUri(opportunityId), requestContent);
        }

        private async Task<Uri> GetLogicAppUri(string opportunityId)
        {
            // Retrieve Logic App URI
            var url = await GetCallbackUrl(TRIGGER_NAME);
            var _httpClient = new HttpClient();
            // Use the below when you have query parameters in the URL
            //return new Uri(url.Body.Value.Replace("invoke", $"invoke/{anyparam}"));
            return new Uri(url.Body.Value);
        }

        private async Task<AzureOperationResponse<WorkflowTriggerCallbackUrl>> GetCallbackUrl(string triggerName)
        {
            return await _client.WorkflowTriggers.ListCallbackUrlWithHttpMessagesAsync(_resourceGroupName, _logicAppName, TRIGGER_NAME);
        }

        private async Task<ServiceClientCredentials> GetCredentials(string tenantId, string clientId, string clientSecret)
        {
            var context = new AD.AuthenticationContext($"https://login.windows.net/{tenantId}");
            var credential = new AD.ClientCredential(clientId, clientSecret);
            var result = await context.AcquireTokenAsync("https://management.core.windows.net/", credential);
            string token = result.CreateAuthorizationHeader().Substring("Bearer ".Length);

            return new TokenCredentials(token);
        }
    }
}
