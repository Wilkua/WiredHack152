using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace WiredHack2015.Models
{
    public class MapViewModel
    {
        // Receiving data types
        public string ZipCode { get; set; }
        public string Brand { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime DateBefore { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime DateAfter { get; set; }
        public int Radius { get; set; }

        // Data being passed to the view
        public string MapJSON { get; set; }
        public string HeatMapJSON { get; set; }
        public string MarkerMapJSON { get; set; }
        public string BrandYearPieChartJSON { get; set; }
        public string BrandYearLineChartJSON { get; set; }
        public string YearPieChartJSON { get; set; }
        public string HeatmapScript { get; set; }
        public string LatLongSearch { get; set; }
        public IEnumerable<stgDealer> DealersList { get; set; }
    }
}