using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyTicketCorinthians.Tests.Models
{
    class APIRequest
    {
        public string homeTeam { get; set; }
        public string awayTeam { get; set; }
        public string customerName { get; set; }
        public string date { get; set; }
        public object postTwitter { get; set; }
        public object price { get; set; }
    }
}
