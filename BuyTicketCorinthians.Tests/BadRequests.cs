using Microsoft.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.Management.Logic;
using System.Threading.Tasks;
using Microsoft.Rest.Azure;
using Microsoft.Azure.Management.Logic.Models;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Text;
using System.Text.Json;
using BuyTicketCorinthians.Tests.Helper;
using System.Linq.Expressions;

namespace BuyTicketCorinthians.Tests
{
    [TestClass]
    public class BadRequests : TestBase
    {
        [TestMethod]
        public async Task BadRequest_Missing_RequiredFields_SimplerCode()
        {
            var tenantId = Environment.GetEnvironmentVariable("ACSUG_TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("ACSUG_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("ACSUG_CLIENT_SECRET");
            var subscriptionId = Environment.GetEnvironmentVariable("ACSUG_SUBSCRIPTION_ID");

            // Get credentials
            var context = new AuthenticationContext($"https://login.windows.net/{tenantId}");
            var credentials = new ClientCredential(clientId, clientSecret);
            var result = await context.AcquireTokenAsync("https://management.core.windows.net/", credentials);
            string token = result.CreateAuthorizationHeader().Substring("Bearer ".Length);
            var tokenCredentials = new TokenCredentials(token);

            // Use Logic Apps Management
            var logicAppsClient = new LogicManagementClient(tokenCredentials);
            logicAppsClient.SubscriptionId = subscriptionId;

            // Retrieve Logic Apps Uri
            var url = await logicAppsClient.WorkflowTriggers.ListCallbackUrlWithHttpMessagesAsync("ACSUG-LogicApps-Testing", "lapp-buyticketcorinthians-dev", "manual");

            // Create mock data to be tested
            var request = new Models.APIRequest
            {
                price = 0
            };

            // Serialize payload
            var serializerOptions = new JsonSerializerOptions { IgnoreNullValues = true };
            var requestContent = new StringContent(JsonSerializer.Serialize<Models.APIRequest>(
                request, serializerOptions), Encoding.UTF8, "application/json");

            var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(new Uri(url.Body.Value), requestContent);

            var responseContent = await response.Content.ReadAsStreamAsync();
            var logicAppsResponse = await JsonSerializer.DeserializeAsync<Models.LogicAppsErrorResponse>(responseContent, serializerOptions);

            // Verify that the Http code returned is 400 (BadRequest)
            Assert.IsTrue(response.StatusCode == System.Net.HttpStatusCode.BadRequest, $"Expected '{System.Net.HttpStatusCode.BadRequest}' but received '{response.StatusCode}. Details: {logicAppsResponse.error.message}'");
            Assert.AreEqual(logicAppsResponse.error.code, "TriggerInputSchemaMismatch", $"Expected 'TriggerInputSchemaMismatch' but received '{logicAppsResponse.error.code}. Details: {logicAppsResponse.error.message}'");
            Assert.IsTrue(logicAppsResponse.error.message.Contains("Required properties are missing from object") &&
                     logicAppsResponse.error.message.Contains("customerName") &&
                     logicAppsResponse.error.message.Contains("date") &&
                     logicAppsResponse.error.message.Contains("homeTeam") &&
                     logicAppsResponse.error.message.Contains("awayTeam"),
                $"Error message received not matching the expected: '{logicAppsResponse.error.message}'");
        }

        [TestMethod]
        public async Task BadRequest_Missing_RequiredFields()
        {
            // Create mock data to be tested
            var request = new Models.APIRequest();

            await BadRequest_TestBase<Models.APIRequest>(
                request,
                x => x.error.message.Contains("Required properties are missing from object") &&
                     x.error.message.Contains("customerName") &&
                     x.error.message.Contains("date") &&
                     x.error.message.Contains("homeTeam") &&
                     x.error.message.Contains("awayTeam"));
        }

        [TestMethod]
        public async Task BadRequest_WrongDataType_ExpectedBoolean()
        {
            // Create mock data to be tested
            var request = new Models.APIRequest
            {
                postTwitter = "true"
            };


            await BadRequest_TestBase<Models.APIRequest>(
                request,
                x => x.error.message.Contains("Invalid type") &&
                     x.error.message.Contains("Expected Boolean but got "));
        }

        [TestMethod]
        public async Task BadRequest_WrongDataType_ExpectedNumber()
        {
            // Create mock data to be tested
            var request = new Models.APIRequest
            {
                price = "1.00"
            };


            await BadRequest_TestBase<Models.APIRequest>(
                request,
                x => x.error.message.Contains("Invalid type") &&
                     x.error.message.Contains("Expected Number but got "));
        }

        [TestMethod]
        public async Task BadRequest_MinimumLengthNotMet_CustomerName()
        {
            // Create mock data to be tested
            var request = new Models.APIRequest
            {
                customerName = "X"
            };

            await BadRequest_TestBase<Models.APIRequest>(
                request,
                x => x.error.message.Contains("String 'X' is less than minimum length of 10"));
        }

        [TestMethod]
        public async Task BadRequest_NotInEnum_HomeTeam()
        {
            // Create mock data to be tested
            var request = new Models.APIRequest
            {
                homeTeam = "Invalid Team"
            };

            await BadRequest_TestBase<Models.APIRequest>(
                request,
                x => x.error.message.Contains("Value \"Invalid Team\" is not defined in enum."));
        }

        private async Task BadRequest_TestBase<T>(T request, Func<Models.LogicAppsErrorResponse, bool> condition)
        {
            // Call LogicApp
            var result = await CallLogicApp<T>(request, "any");

            // Receive LogicApp response
            var errorResponse = await GetHttpResponseMessage<Models.LogicAppsErrorResponse>(result);

            // Verify that the Http code returned is 400 (BadRequest)
            Assert.IsTrue(result.StatusCode == System.Net.HttpStatusCode.BadRequest, $"Expected '{System.Net.HttpStatusCode.BadRequest}' but received '{result.StatusCode}. Details: {errorResponse.error.message}'");
            Assert.AreEqual(errorResponse.error.code, "TriggerInputSchemaMismatch", $"Expected 'TriggerInputSchemaMismatch' but received '{errorResponse.error.code}. Details: {errorResponse.error.message}'");
            Assert.IsTrue(condition(errorResponse), $"Error message received not matching the expected: '{errorResponse.error.message}'");
        }
    }
}
