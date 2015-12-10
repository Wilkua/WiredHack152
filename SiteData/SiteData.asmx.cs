using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Threading;
using System.Web.Services;
using System.Xml.Linq;

namespace SiteData
{
    /// <summary>
    /// Summary description for SiteData
    /// </summary>
    [WebService(Namespace = "http://drescherdigital.com")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class SiteData : System.Web.Services.WebService
    {
        private string gaKey = "AIzaSyBxucUVRrxS9TVmyuDPAx2v51KQWeufDG4";

        /* Read these notes please 
            I need you to add this function to the api and class for the response it will return in json or whatever. If json we can deserialize it using JSON.net as a library if your still having issues with System.Web.Json
            I also want to see about moving all of this service to it. So its just the one service. 
            */

        public class GeoCodeLatLongResponse
        {
            public String Status { get; set; }
            public float lat { get; set; }
            public float lng { get; set; }
        }

        public GeoCodeLatLongResponse GetLatLongResponse(string address)
        {
            try
            {
                WebRequest request = WebRequest.Create("https://maps.googleapis.com/maps/api/geocode/xml?address="
                    + address + ",&key=AIzaSyBxucUVRrxS9TVmyuDPAx2v51KQWeufDG4");
                WebResponse response = (WebResponse)request.GetResponse();

                //WebHeaderCollection header = response.Headers;

                XElement ele = XElement.Load(response.GetResponseStream());
                var status = ele.Element("status").Value;

                switch (status)
                {
                    case "OK":
                        return new GeoCodeLatLongResponse()
                        {
                            lat = float.Parse(ele.Element("result").Element("geometry").Element("location").Element("lat").Value),
                            lng = float.Parse(ele.Element("result").Element("geometry").Element("location").Element("lng").Value),
                            Status = status
                        };
                    case "UNKNOWN_ERROR":
                        var latlong = GetLatLongResponse(address);
                        if (latlong.Status == "OK")
                        {

                            return new GeoCodeLatLongResponse()
                            {
                                lat = float.Parse(ele.Element("result").Element("geometry").Element("location").Element("lat").Value),
                                lng = float.Parse(ele.Element("result").Element("geometry").Element("location").Element("lng").Value),
                                Status = status
                            };
                        }
                        break;
                }
            }
            catch (Exception e)
            {

            }

            return new GeoCodeLatLongResponse()
            {
                lat = 0,
                lng = 0,
                Status = "Error"
            };
        }

        [WebMethod]
        public bool UpdateDataTable()
        {
            try
            {
                Dictionary<int, String> Records = GetAddresss();

                foreach (var i in Records)
                {
                    var response = GetLatLongResponse(i.Value);

                    if (response.Status == "OK")
                    {                  
                        Thread.Sleep(500);
                        UpdateRecord(i.Key, response.lat, response.lng);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private Dictionary<int,String> GetAddresss()
        {
            Dictionary<int, String> records = new Dictionary<int, string>();

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["WiredHack"].ConnectionString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.GetMissingRecordsLatAndLong", con))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        records.Add(Convert.ToInt32(rdr[0]),  rdr[1].ToString().Replace(' ','+') + 
                            ","+ rdr[2].ToString().Replace(' ','+') +
                            "," + rdr[3].ToString());
                    }
                }
            }

            return records;
        }

        private bool UpdateRecord(int id, float lat, float lng)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["WiredHack"].ConnectionString))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.UpdateLatandLong", con))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;
                        cmd.Parameters.Add("@lat", System.Data.SqlDbType.Float).Value = lat;
                        cmd.Parameters.Add("@lng", System.Data.SqlDbType.Float).Value = lng;

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        

    }
}
