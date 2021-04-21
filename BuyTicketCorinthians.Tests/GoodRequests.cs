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


        }

    }
}
