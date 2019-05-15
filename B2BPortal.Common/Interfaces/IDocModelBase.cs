using B2BPortal.Common.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2BPortal.Common.Interfaces
{
    public interface IDocModelBase
    {
        [JsonProperty(PropertyName = "id")]
        string Id { get; set; }

        [JsonProperty(PropertyName = "docType")]
        DocTypes DocType { get; set; }
    }
}
