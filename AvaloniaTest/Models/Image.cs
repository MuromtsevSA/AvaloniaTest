using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ScreenShotAvalonia.Model
{
    public class Image 
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonProperty("screenshot")]
        public string ScreenShot { get; set; }
        [JsonProperty("date")]
        public string Date { get; set; }
    }
}
