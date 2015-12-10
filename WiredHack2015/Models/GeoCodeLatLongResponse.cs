using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WiredHack2015.Models
{
    public class GeoCodeLatLongResponse
    {
        public String Status { get; set; }
        public float lat { get; set; }
        public float lng { get; set; }
    }
}