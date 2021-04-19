using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyTicketCorinthians.Tests.Models
{
    class LogicAppsActionResponse<T>
    {
        public int statusCode { get; set; }
        public HttpHeader headers { get; set; }
        public T body { get; set; }
    }

    class HttpHeader
    {
        public string TransferEncoding { get; set; }
        public string Connection { get; set; }
        public string Vary { get; set; }
        public string Location { get; set; }
        public string Expires { get; set; }
        public string Date { get; set; }
        public string Pragma { get; set; }
    }
}
