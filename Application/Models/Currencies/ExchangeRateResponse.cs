using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Core.Application.Models.Currencies
{
    public class ExchangeRateResponse
    {
        // result="success" ili "error"
        public string result { get; set; }
        public string error_type { get; set; }
        public string documentation { get; set; }
        public string terms_of_use { get; set; }
        public int time_last_update_unix { get; set; }
        public DateTime time_last_update_utc { get; set; }
        public int time_next_update_unix { get; set; }
        public DateTime time_next_update_utc { get; set; }
        public string base_code { get; set; }
        public JObject conversion_rates { get; set; }
        public List<KeyValuePair<string, decimal>> conversionRates { get; set; } = new List<KeyValuePair<string, decimal>>();
    }
}
