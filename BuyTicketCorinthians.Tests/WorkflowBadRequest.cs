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
using System.Net;

namespace BuyTicketCorinthians.Tests
{
    [TestClass]
    public class WorkflowBadRequest: TestBase
    {
        [TestMethod]
        public async Task WorkflowBadRequest_ErrorAPI()
        {
            // Create mock data to be tested
            var request = new Models.APIRequest
            {
                awayTeam = "Palmeiras",
                homeTeam = "Corinthians",
                customerName = "Alessandro Moura",
                date = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss"),
                postTwitter = false,
                price = 150
            };

            await WorkflowBadRequest_TestBase(
                request,
                x => x.message.Contains("Palmeiras does not have a Clubs World Cup"),
                    "1951 by fax does not matter");
        }



        private async Task WorkflowBadRequest_TestBase<T>(T request, Func<Models.APIResponse, bool> condition, string lastAssertMessage)
        {
            // Call LogicApp
            var result = await CallLogicApp<T>(request, "any");

            // Receive LogicApp response
            var response = await GetHttpResponseMessage<Models.LogicAppsErrorResponse>(result);

            // Verify that the Http code returned is 202 (Accepted)
            string details = response.error != null ? response.error.message : string.Empty;
            Assert.IsTrue(result.StatusCode == System.Net.HttpStatusCode.Accepted, $"Expected '{System.Net.HttpStatusCode.Accepted}' but received '{result.StatusCode}. Details: {details}'");

            // Obtain LogicApps RunId to investigate its actions
            var runId = result.Headers.GetValues("x-ms-workflow-run-id").First();

            while (true)
            {
                // Get Logic App execution status
                var workflowStatus = await LogicAppClient.WorkflowRuns.GetAsync(ResourceGroupName, LogicAppName, runId);

                // Await workflow completion to retrieve the action result
                if (workflowStatus.Status == WorkflowStatus.Succeeded || workflowStatus.Status == WorkflowStatus.Failed)
                {
                    // Get action status
                    var action = await LogicAppClient.WorkflowRunActions.GetAsync(ResourceGroupName, LogicAppName, runId, "Buy_Ticket");
                    Assert.IsTrue(action.Code == "NotFound", "Response not expected!", $"Expected 'BadRequest' but received '{action.Code}'");

                    var actionResponse = await LogicAppClient.HttpClient.GetAsync(action.OutputsLink.Uri);
                    var actionResponseContent = await GetHttpResponseMessage<Models.APIResponse>(actionResponse, true);

                    Assert.IsTrue(actionResponseContent.mensagem == "Palmeiras nao tem Mundial", "1951 por fax nao conta");
                    Assert.IsTrue(condition(actionResponseContent), lastAssertMessage);

                    break;
                }
                Thread.Sleep(500);
            }
        }
    }
}
