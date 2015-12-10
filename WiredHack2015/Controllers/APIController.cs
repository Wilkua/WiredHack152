using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.IO;
using System.Json;


namespace WiredHack2015.Controllers
{
    public class APIController : Controller
    {
        private WiredHackEntities dbContext = new WiredHackEntities();
        private string gaKey = "AIzaSyBxucUVRrxS9TVmyuDPAx2v51KQWeufDG4";

        private bool GetLatLongForPostalCode(string postalcode, out float postLat, out float postLng)
        {
            postLat = 0.0f;
            postLng = 0.0f;

            var postalCodeLocation = dbContext.PostalCodeLatLongs
                .Where(s => s.PostalCode == postalcode)
                .Select(s => new { s.Lat, s.Long, s.CacheDate }).FirstOrDefault();

            // If there isn't any postal code data we need to create it
            if ((postalCodeLocation == null)
                || ((DateTime.Now - postalCodeLocation.CacheDate).TotalDays > 30))
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://maps.googleapis.com/maps/api/geocode/json?address=" + postalcode + "&key=" + gaKey);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                
                //WebHeaderCollection header = response.Headers;

                StreamReader reader = new StreamReader(response.GetResponseStream());
                string responseString = reader.ReadToEnd();

                reader.Close(); // Also closes the WebResponse stream object
                response.Close();

                //XElement ele = XElement.Load(response.GetResponseStream());
                var geoData = new JavaScriptSerializer().Deserialize(responseString);

                float lat = geoData.result.geometry.location.lat;
                float lng = geoData.result.geometry.location.lng;

                //float lat = float.Parse(ele.Element("result").Element("geometry").Element("location").Element("lat").Value);
                //float lng = float.Parse(ele.Element("result").Element("geometry").Element("location").Element("lng").Value);
            }
            // Return the cached latitude and longitude data
            else
            {
                postLat = (float)postalCodeLocation.Lat;
                postLng = (float)postalCodeLocation.Long;
            }

            return true;
        }

        // GET: API
        public string Index()
        {
            return "<!DOCTYPE html><html lang=\"en\"><head><title></title><meta charset=\"utf-8\" /></head><body></body></html>";
        }

        // GET: API/HeatmapData?postalcode=xxxxx&brand=xxxx&city=xxxx&state=xxxxx&datebefore=xxxx&dateafter=xxxxx&distance=0000
        public string HeatmapData(string postalcode, string brand, string city, string state, string datebefore, string dateafter, int? distance)
        {
            if (!dbContext.Database.Exists())
            {
                return "";
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

                GetLatLongForPostalCode(postalcode, out postLat, out postLng);
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

            var finalList = dealerList.Select(s => new { s.Lat, s.Long }).ToList();

            return new JavaScriptSerializer().Serialize(finalList);
        }

        // GET: API/States
        public string States()
        {
            if (!dbContext.Database.Exists())
            {
                return "";
            }

            var states = dbContext.stgDealers.Select(s => s.State).Distinct().ToList();

            return new JavaScriptSerializer().Serialize(states);
        }

        // GET: API/CitiesFromState?state=XX
        public string CitiesFromState(string state)
        {
            if (!dbContext.Database.Exists())
            {
                return "";
            }

            // The state should only be 2 letters
            if ((!String.IsNullOrEmpty(state)) && (state.Length > 2))
            {
                return "";
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

            return new JavaScriptSerializer().Serialize(cities);
        }
    }
}