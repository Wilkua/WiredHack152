using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml.Linq;

namespace WiredHack2015.Controllers
{
    public class APIController : Controller
    {
        public const int RESULT_OK = 0;
        public const int RESULT_ZERO_RESULTS = 1;
        public const int RESULT_OVER_QUERY_LIMIT = 2;
        public const int RESULT_REQUEST_DENIED = 3;
        public const int RESULT_INVALID_REQUEST = 4;
        public const int RESULT_UNKNOWN_ERROR = 5;
        public const int RESULT_DATABASE_UNAVAILABLE = 6;
        public const int RESULT_INVALID_PARAMETERS = 7;

        public Dictionary<int, string> ResultStrings = new Dictionary<int, string>
        {
            { RESULT_OK, "OK" },
            { RESULT_ZERO_RESULTS, "No results found" },
            { RESULT_OVER_QUERY_LIMIT, "Maximum query allocation exceeded" },
            { RESULT_REQUEST_DENIED, "Query request was denied" },
            { RESULT_INVALID_REQUEST, "Query request was invalid" },
            { RESULT_UNKNOWN_ERROR, "An unknown error occured" },
            { RESULT_DATABASE_UNAVAILABLE, "The database server could not be reached" },
            { RESULT_INVALID_PARAMETERS, "The supplied parameters were invalid" }
        };

        private WiredHackEntities dbContext = new WiredHackEntities();
        private string gaKey = "AIzaSyBxucUVRrxS9TVmyuDPAx2v51KQWeufDG4";

        private int GetLatLngForPostalCode(string postalcode, out float postLat, out float postLng)
        {
            int result = RESULT_UNKNOWN_ERROR;

            postLat = 0.0f;
            postLng = 0.0f;

            PostalCodeLatLong postalCodeLocation = dbContext.PostalCodeLatLongs
                .Where(s => s.PostalCode == postalcode)
                .FirstOrDefault();

            // If there isn't any postal code data we need to create it or if the postal code data
            // is older than 30 days, we need to refresh it
            if ((postalCodeLocation == null)
                || ((DateTime.Now - postalCodeLocation.CacheDate).TotalDays > 30))
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://maps.googleapis.com/maps/api/geocode/xml?address=" + postalcode + "&key=" + gaKey);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                
                //WebHeaderCollection header = response.Headers;

                response.Close(); // Close 

                XElement geoData = XElement.Load(response.GetResponseStream());

                string status = geoData.Element("status").Value.ToLower();

                if (status == "ok")
                {
                    postLat = float.Parse(geoData.Element("result").Element("geometry").Element("location").Element("lat").Value);
                    postLng = float.Parse(geoData.Element("result").Element("geometry").Element("location").Element("lng").Value);

                    if (postalCodeLocation == null)
                    {
                        dbContext.PostalCodeLatLongs.Add(new PostalCodeLatLong {
                            PostalCode = postalcode,
                            Lat = postLat,
                            Lng = postLng,
                            CacheDate = DateTime.Now
                        });

                        dbContext.SaveChanges();
                    }
                    else
                    {
                        postalCodeLocation.Lat = postLat;
                        postalCodeLocation.Lng = postLng;
                        postalCodeLocation.CacheDate = DateTime.Now;

                        dbContext.SaveChanges();
                    }
                }
                else if (status == "zero_results") { result = RESULT_ZERO_RESULTS; }
                else if (status == "over_query_limit") { result = RESULT_OVER_QUERY_LIMIT; }
                else if (status == "request_denied") { result = RESULT_REQUEST_DENIED; }
                else if (status == "invalid_request") { result = RESULT_INVALID_REQUEST; }
                else { result = RESULT_UNKNOWN_ERROR; }
            }
            // Return the cached latitude and longitude data
            else
            {
                postLat = (float)postalCodeLocation.Lat;
                postLng = (float)postalCodeLocation.Lng;
                result = RESULT_OK;
            }

            return result;
        }

        // GET: API
        public string Index()
        {
            return "<!DOCTYPE html><html lang=\"en\"><head><title></title><meta charset=\"utf-8\" /></head><body></body></html>";
        }

        // GET: API/HeatmapData?postalcode=xxxxx&brand=xxxx&city=xxxx&state=xxxxx&datebefore=xxxx&dateafter=xxxxx&distance=0000
        public string LatLngData(string postalcode, string brand, string city, string state, string datebefore, string dateafter, int? distance)
        {
            int result = RESULT_OK;

            if (!dbContext.Database.Exists())
            {
                var errOutput = new
                {
                    result = RESULT_DATABASE_UNAVAILABLE,
                    resultmessage = ResultStrings[RESULT_DATABASE_UNAVAILABLE],
                };
                return new JavaScriptSerializer().Serialize(errOutput);
            }

            List<stgDealer> dealerList;

            if (distance != null)
            {
                // Here we're working with a radius search
                // IMPORTANT(william): We need a way of getting lat/long for zip code
                // The idea here is the initial list of dealers will be cut down by
                // the function to retrieve them by radius
                float postLat = 0.0f;
                float postLng = 0.0f;

                result = GetLatLngForPostalCode(postalcode, out postLat, out postLng);
                //dealers = dbContext.sp_getDealersByLatLong(postLat, postLng, distance);
                dealerList = dbContext.stgDealers.ToList();
            }
            else
            {
                dealerList = dbContext.stgDealers.ToList();
            }

            // Process the dealer list for postal codes
            if (!string.IsNullOrEmpty(postalcode))
            {
                dealerList = dealerList.Where(s => s.PostalCode == postalcode).ToList();
            }

            if (!string.IsNullOrEmpty(brand))
            {
                dealerList = dealerList.Where(s => s.BrandName == brand).ToList();
            }

            if (!string.IsNullOrEmpty(state))
            {
                dealerList = dealerList.Where(s => s.State == state).ToList();
            }

            if (!string.IsNullOrEmpty(city))
            {
                dealerList = dealerList.Where(s => s.City == city).ToList();
            }

            if (!string.IsNullOrEmpty(datebefore))
            {
                DateTime dtDateBefore = Convert.ToDateTime(datebefore);
                dealerList = dealerList.Where(s => s.SignedOn <= dtDateBefore).ToList();
            }

            if (!string.IsNullOrEmpty(dateafter))
            {
                DateTime dtDateAfter = Convert.ToDateTime(dateafter);
                dealerList = dealerList.Where(s => s.SignedOn >= dtDateAfter).ToList();
            }

            var finalList = dealerList.Select(s => new
            {
                BrandName = s.BrandName,
                DealerName = s.DealerName,
                SignedOn = s.SignedOn.ToString(),
                DealerCode = s.DealerCode,
                ManfRegionCode = s.ManfRegionCode,
                Address1 = s.Address1,
                Address2 = s.Address2,
                City = s.City,
                State = s.State,
                PostalCode = s.PostalCode,
                Lat = s.Lat,
                Lng = s.Lng
            });

            var output = new
            {
                result = (dealerList.Count == 0) ? RESULT_ZERO_RESULTS : result,
                resultmessage = ResultStrings[result],
                data = finalList
            };

            return new JavaScriptSerializer().Serialize(output);
        }

        // GET: API/Brands
        public string Brands()
        {
            if (!dbContext.Database.Exists())
            {
                var errOutput = new
                {
                    result = RESULT_DATABASE_UNAVAILABLE,
                    resultmessage = ResultStrings[RESULT_DATABASE_UNAVAILABLE]
                };
                return new JavaScriptSerializer().Serialize(errOutput);
            }

            var brandList = dbContext.stgDealers.Select(s => s.BrandName).Distinct().ToList();
            int iResult = RESULT_OK;
            if (brandList.Count == 0)
            {
                iResult = RESULT_ZERO_RESULTS;
            }

            var output = new
            {
                result = iResult,
                resultmessage = ResultStrings[iResult],
                data = brandList
            };

            return new JavaScriptSerializer().Serialize(output);
        }

        // GET: API/States
        public string States()
        {
            if (!dbContext.Database.Exists())
            {
                var errOutput = new
                {
                    result = RESULT_DATABASE_UNAVAILABLE,
                    resultmessage = ResultStrings[RESULT_DATABASE_UNAVAILABLE]
                };
                return new JavaScriptSerializer().Serialize(errOutput);
            }

            var states = dbContext.stgDealers.Select(s => s.State).Distinct().ToList();
            int iResult = RESULT_OK;
            if (states.Count == 0)
            {
                iResult = RESULT_ZERO_RESULTS;
            }

            var output = new
            {
                result = iResult,
                resultmessage = ResultStrings[iResult],
                data = states
            };

            return new JavaScriptSerializer().Serialize(output);
        }

        // GET: API/CitiesFromState?state=XX
        public string CitiesFromState(string state)
        {
            if (!dbContext.Database.Exists())
            {
                var errOutput = new
                {
                    result = RESULT_DATABASE_UNAVAILABLE,
                    resultmessage = ResultStrings[RESULT_DATABASE_UNAVAILABLE]
                };
                return new JavaScriptSerializer().Serialize(errOutput);
            }

            // The state should only be 2 letters
            if ((!String.IsNullOrEmpty(state)) && (state.Length > 2))
            {
                var emptyOutput = new
                {
                    result = RESULT_INVALID_PARAMETERS,
                    resultmessage = ResultStrings[RESULT_INVALID_PARAMETERS]
                };
                return new JavaScriptSerializer().Serialize(emptyOutput);
            }

            List<String> cities;

            if (String.IsNullOrEmpty(state))
            {
                cities = dbContext.stgDealers.Select(s => s.City).Distinct().ToList();
            }
            else
            {
                cities = dbContext.stgDealers.Where(s => s.State == state).Select(s => s.City).Distinct().ToList();
            }

            int iResult = RESULT_OK;
            if (cities.Count == 0)
            {
                iResult = RESULT_ZERO_RESULTS;
            }

            var output = new
            {
                result = iResult,
                resultmessage = ResultStrings[iResult],
                data = cities
            };

            return new JavaScriptSerializer().Serialize(output);
        }

        // GET: /API/DealerCountByBranchChart
        public string DealerCountByBrandChart()
        {
            List<ChartSeries<ChartSeriesData<int>>> chartSeries = new List<ChartSeries<ChartSeriesData<int>>>();
            List<DrillDownSeriesItem<dynamic []>> drillDownSeries = new List<DrillDownSeriesItem<dynamic[]>>();

            var brands = dbContext.stgDealers.Select(s => s.BrandName).Distinct().ToList();
            var brandYears = dbContext.stgDealers.Select(s => s.SignedOn.Value.Year).Distinct().ToList();

            var newSeries = new ChartSeries<ChartSeriesData<int>>();
            newSeries.name = "Dealers";
            newSeries.colorByPoint = true;

            foreach (var brand in brands)
            {
                int dealerCount = dbContext.stgDealers.Where(s => s.BrandName == brand).Count();
                newSeries.data.Add(new ChartSeriesData<int>
                {
                    name = brand,
                    y = dealerCount,
                    drilldown = brand + "Years"
                });

                var dditem = new DrillDownSeriesItem<dynamic[]>();

                foreach (var year in brandYears)
                {
                    int brandYearCount = dbContext.stgDealers
                        .Where(s => s.BrandName == brand)
                        .Where(s => s.SignedOn.Value.Year == year)
                        .Count();

                    dditem.id = brand + "Years";
                    dditem.data.Add(new dynamic[] { year.ToString(), brandYearCount });
                }

                drillDownSeries.Add(dditem);
            }

            chartSeries.Add(newSeries);

            var chartOutput = new
            {
                chart = new
                {
                    height = 400,
                    width = 400,
                    type = "pie"
                },
                title = new
                {
                    text = "Dealer Count by Brand"
                },
                xAxis = new
                {
                    type = "category"
                },
                legend = new
                {
                    enabled = false
                },
                plotOptions = new
                {
                    series = new
                    {
                        borderWidth = 0,
                        dataLabels = new
                        {
                            enabled = true
                        }
                    }
                },
                series = chartSeries,
                drilldown = new
                {
                    series = drillDownSeries
                }
            };

            return new JavaScriptSerializer().Serialize(chartOutput);
        }

        public string DealerGrowthByYearChart()
        {
            List<ChartSeries<ChartSeriesData<int>>> chartSeries = new List<ChartSeries<ChartSeriesData<int>>>();
            List<DrillDownSeriesItem<dynamic[]>> drillDownSeries = new List<DrillDownSeriesItem<dynamic[]>>();

            var brands = dbContext.stgDealers.Select(s => s.BrandName).Distinct().ToList();
            var years = dbContext.stgDealers.Select(s => s.SignedOn.Value.Year).Distinct().ToList();

            var newSeries = new ChartSeries<ChartSeriesData<int>>();
            newSeries.name = "Dealers";
            newSeries.colorByPoint = true;

            foreach (var year in years)
            {
                var dealerCount = dbContext.stgDealers.Where(s => s.SignedOn.Value.Year == year).Count();
                newSeries.data.Add(new ChartSeriesData<int>
                {
                    name = Convert.ToString(year),
                    y = dealerCount,
                    drilldown = Convert.ToString(year) + "Brands"
                });

                var dditem = new DrillDownSeriesItem<dynamic[]>();

                foreach (var brand in brands)
                {
                    int brandCount = dbContext.stgDealers
                        .Where(s => s.BrandName == brand)
                        .Where(s => s.SignedOn.Value.Year == year)
                        .Count();

                    dditem.id = Convert.ToString(year) + "Brands";
                    dditem.data.Add(new dynamic[] { brand, brandCount });
                }

                drillDownSeries.Add(dditem);
            }

            chartSeries.Add(newSeries);

            var chartOutput = new
            {
                chart = new
                {
                    height = 400,
                    width = 400,
                    type = "pie"
                },
                title = new
                {
                    text = "Dealer Count by Brand"
                },
                xAxis = new
                {
                    type = "category"
                },
                legend = new
                {
                    enabled = false
                },
                plotOptions = new
                {
                    series = new
                    {
                        borderWidth = 0,
                        dataLabels = new
                        {
                            enabled = true
                        }
                    }
                },
                series = chartSeries,
                drilldown = new
                {
                    series = drillDownSeries
                }
            };

            return new JavaScriptSerializer().Serialize(chartOutput);
        }
    } // end class APIController

    public class ChartSeries<Tx>
    {
        public string name { get; set; }
        public bool colorByPoint { get; set; }
        public List<Tx> data { get; set; }

        public ChartSeries()
        {
            data = new List<Tx>();
        }
    }

    public class ChartSeriesData<Tx>
    {
        public string name { get; set; }
        public Tx y { get; set; }
        public string drilldown { get; set; }
    }

    public class DrillDownSeriesItem<Tx>
    {
        public string id { get; set; }
        public List<Tx> data { get; set; }

        public DrillDownSeriesItem()
        {
            data = new List<Tx>();
        }
    }
} // end namespace

/*
{
                chart: {
                    height: 400,
                    width: 400,
                    type: 'pie'
                },
                title: {
                    text: 'Dealer Growth by Year'
                },
                xAxis: {
                    type: 'category'
                },

                legend: {
                    enabled: false
                },

                plotOptions: {
                    series: {
                        borderWidth: 0,
                        dataLabels: {
                            enabled: true
                        }
                    }
                },

                series: [{
name: 'Dealers',
colorByPoint: true,
data: [
{
name: '2008',
y: 58,
drilldown: '2008Brands'
},
{
name: '2006',
y: 57,
drilldown: '2006Brands'
},
{
name: '2007',
y: 123,
drilldown: '2007Brands'
},
{
name: '2013',
y: 59,
drilldown: '2013Brands'
},
{
name: '2009',
y: 25,
drilldown: '2009Brands'
},
{
name: '2004',
y: 15,
drilldown: '2004Brands'
},
{
name: '2014',
y: 9,
drilldown: '2014Brands'
},
{
name: '2010',
y: 9,
drilldown: '2010Brands'
},
{
name: '2005',
y: 34,
drilldown: '2005Brands'
},
{
name: '2011',
y: 2,
drilldown: '2011Brands'
}
]
}],
drilldown: {
series: [
{id: '2008Brands',
data: [
['Toyota', 26]
,
['GM', 25]
,
['Mopar', 7]
,
['Ford', 0]
]
}
,
{id: '2006Brands',
data: [
['Toyota', 0]
,
['GM', 50]
,
['Mopar', 4]
,
['Ford', 3]
]
}
,
{id: '2007Brands',
data: [
['Toyota', 14]
,
['GM', 99]
,
['Mopar', 8]
,
['Ford', 2]
]
}
,
{id: '2013Brands',
data: [
['Toyota', 0]
,
['GM', 1]
,
['Mopar', 57]
,
['Ford', 1]
]
}
,
{id: '2009Brands',
data: [
['Toyota', 12]
,
['GM', 6]
,
['Mopar', 0]
,
['Ford', 7]
]
}
,
{id: '2004Brands',
data: [
['Toyota', 0]
,
['GM', 15]
,
['Mopar', 0]
,
['Ford', 0]
]
}
,
{id: '2014Brands',
data: [
['Toyota', 0]
,
['GM', 0]
,
['Mopar', 9]
,
['Ford', 0]
]
}
,
{id: '2010Brands',
data: [
['Toyota', 3]
,
['GM', 1]
,
['Mopar', 1]
,
['Ford', 4]
]
}
,
{id: '2005Brands',
data: [
['Toyota', 0]
,
['GM', 34]
,
['Mopar', 0]
,
['Ford', 0]
]
}
,
{id: '2011Brands',
data: [
['Toyota', 0]
,
['GM', 0]
,
['Mopar', 2]
,
['Ford', 0]
]
}

]
}
*/
