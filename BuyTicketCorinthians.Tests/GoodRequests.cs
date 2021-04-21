﻿using Microsoft.Rest;
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
    public class GoodRequests: TestBase
    {
        [TestMethod]
        public async Task GoodRequest_Success()
        {
            // Create mock data to be tested
            var request = new Models.APIRequest
            {
                homeTeam = "Corinthians",
                awayTeam = "Chelsea",
                customerName = "Alessandro Moura",
                date = DateTime.Now.ToString("2012-12-16T21:00:00"),
                postTwitter = false,
                price = 150
            };

            // Call Logic App
            var result = await CallLogicApp<Models.APIRequest>(request, "any");

            // Receive Logic App response
            var response = await GetHttpResponseMessage<Models.LogicAppsErrorResponse>(result);

            // Verify that the http code returned is 202 (Accepted)
            string details = response.error != null ? response.error.message : string.Empty;
            Assert.IsTrue(result.StatusCode == System.Net.HttpStatusCode.Accepted, 
                $"Expected '{System.Net.HttpStatusCode.Accepted}' but received '{result.StatusCode}. Details: {details}'");

            // Obtain LogicApps RunId to investigate its actions
            var runId = result.Headers.GetValues("x-ms-workflow-run-id").First();

            while (true)
            {
                // Get Logic App execution status
                var workflowStatus = await LogicAppClient.WorkflowRuns.GetAsync(ResourceGroupName, LogicAppName, runId);

                // Await workflow completion to retrieve the action result
                if (workflowStatus.Status == WorkflowStatus.Succeeded || workflowStatus.Status == WorkflowStatus.Failed)
                {
                    // Get HTTP call status
                    var httpAction = await LogicAppClient.WorkflowRunActions.GetAsync(ResourceGroupName, LogicAppName, runId, "Buy_Ticket");
                    Assert.IsTrue(httpAction.Code == "OK", "Response not expected!", 
                        $"Expected 'OK' but received '{httpAction.Code}'");

                    var httpActionResponse = await LogicAppClient.HttpClient.GetAsync(httpAction.OutputsLink.Uri);
                    var httpActionResponseContent = await GetHttpResponseMessage<Models.APIResponse>(httpActionResponse, true);
                    Assert.IsTrue(httpActionResponseContent.mensagem == "Vai Corinthians!!!");

                    // Get Log Storage account
                    var storageAction = await LogicAppClient.WorkflowRunActions.GetAsync(ResourceGroupName, LogicAppName, runId, "Log_Buy_Ticket_Success");
                    Assert.IsTrue(storageAction.Code == "Created", "Response not expected!",
                        $"Expected 'Created' but received '{storageAction.Code}'");

                    break;
                }
                Thread.Sleep(500);
            }
        }

    }
}
