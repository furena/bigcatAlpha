using B2BPortal.Common.Enums;
using Newtonsoft.Json;

namespace B2BPortal.Data.Models
{
    public class DocModelBase
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "docType")]
        public DocTypes DocType { get; set; }
    }
}