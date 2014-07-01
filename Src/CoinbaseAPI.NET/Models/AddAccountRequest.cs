﻿using Newtonsoft.Json;

namespace Bitlet.Coinbase.Models
{
    public class AddAccountRequest
    {
        public class Details
        {
            [JsonProperty("name"), Required]
            public string Name { get; set; }
        }

        [JsonProperty("account"), Required]
        public Details Account { get; set; }
    }
}
