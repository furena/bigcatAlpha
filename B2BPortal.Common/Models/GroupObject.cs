using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace B2BPortal.Common.Models
{
    public class GroupObject
    {
        [JsonProperty(PropertyName = "groupName")]
        [DisplayName("Group Name")]
        public string GroupName { get; set; }

        [JsonProperty(PropertyName = "groupId")]
        [DisplayName("Group Id")]
        public string GroupId { get; set; }
        
        public GroupObject()
        {

        }
        public GroupObject(string groupName, string groupId)
        {
            GroupName = groupName;
            GroupId = groupId;
        }
    }
}