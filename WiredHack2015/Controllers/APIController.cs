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

#if false
        public ActionResult Test()
        {
            return View();
        }
#endif

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

                XElement geoData = XElement.Load(response.GetResponseStream());

                response.Close(); // Close 

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
                var spDealers = dbContext.sp_getDealersByLatLong(distance, postLat, postLng).ToList();

                dealerList = new List<stgDealer>();

                foreach (var dealer in spDealers)
                {
                    dealerList.Add(new stgDealer()
                    {
                        Address1 = dealer.Address1,
                        Address2 = dealer.Address2,
                        BrandName = dealer.BrandName,
                        City = dealer.City,
                        DealerCode = dealer.DealerCode,
                        DealerName = dealer.DealerName,
                        id = dealer.id,
                        Lat = dealer.Lat,
                        Lng = dealer.Lng,
                        ManfRegionCode = dealer.ManfRegionCode,
                        PostalCode = dealer.PostalCode,
                        SignedOn = dealer.SignedOn,
                        State = dealer.State
                    });
                }
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

            if (dealerList.Count == 0)
                result = RESULT_ZERO_RESULTS;

            var output = new
            {
                result = result,
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

        public string DealerGrowthByBrandOverTimeChart()
        {
            List<ChartSeriesData<int>> chartSeries = new List<ChartSeriesData<int>>();

            var brands = dbContext.stgDealers.Select(s => s.BrandName).Distinct().ToList();
            var years = dbContext.stgDealers.Select(s => s.SignedOn.Value.Year).Distinct().ToList();

            foreach (var brand in brands)
            {
                var newSeries = new ChartSeriesData<int>();
                newSeries.name = brand;

                foreach (var year in years)
                {
                    var dealerCount = dbContext.stgDealers
                        .Where(s => s.BrandName == brand)
                        .Where(s => s.SignedOn.Value.Year == year)
                        .Count();

                    newSeries.data.Add(dealerCount);
                }

                chartSeries.Add(newSeries);
            }

            var chartOutput = new
            {
                title = new
                {
                    text = "Dealer Growth By Brand Over Time"
                },
                subtitle = new
                {
                    text = ""
                },
                xAxis = new
                {
                    categories = years
                },
                yAxis = new {
                    title = new {
                        text = "Dealerships Signed"
                    },
                },
                legend = new {
                    layout =  "vertical",
                    align = "right",
                    verticalAlign = "middle",
                    borderWidth = 0
                },
                series = chartSeries
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
        public List<Tx> data { get; set; }
        public string drilldown { get; set; }

        public ChartSeriesData()
        {
            data = new List<Tx>();
        }
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
    title: {
        text: 'Dealer Growth By Brand Over Time',
        x: -20 //center
    },
    subtitle: {
        text: '',
        x: -20
    },
    xAxis: {
        categories: [
            '2008',
            '2006',
            '2007',
            '2013',
            '2009',
            '2004',
            '2014',
            '2010',
            '2005',
            '2011'
        ]
    },
    yAxis: {
        title: {
            text: 'Dealerships Signed'
        },
        plotLines: [
            {
                value: 0,
                width: 1,
                color: '#808080'
            }
        ]
    },
    legend: {
        layout: 'vertical',
        align: 'right',
        verticalAlign: 'middle',
        borderWidth: 0
    },
    series: [
        {
            name: 'Toyota',
            data: [
                26,
                12,
                14,
                3
            ]
        },
        {
            name: 'GM',
            data: [
                50,
                99,
                25,
                15,
                34,
                6,
                1,
                1
            ]
        },
        {
            name: 'Mopar',
            data: [57,8,7,9,4,2,1]},{name: 'Ford',data: [7,2,4,3,1]}]
}
*/
