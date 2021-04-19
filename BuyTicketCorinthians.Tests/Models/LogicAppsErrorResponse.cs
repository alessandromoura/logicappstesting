using System;

namespace BuyTicketCorinthians.Tests.Models
{
    class LogicAppsErrorResponse
    {
        public Error error { get; set; }
    }

    class Error
    {
        public string code { get; set; }
        public string message { get; set; }
    }
}
