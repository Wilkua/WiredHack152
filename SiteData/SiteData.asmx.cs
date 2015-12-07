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
        private string gaKey = "AIzaSyDXDzf1gPVF0cQIsA2iLaVYCpXA0FNPXm0";

        [WebMethod]
        public bool UpdateDataTable()
        {
            try
            {
                Dictionary<int, String> Records = GetAddresss();

                foreach (var i in Records)
                {
                    WebRequest request = WebRequest.Create("https://maps.googleapis.com/maps/api/geocode/xml?address=" + i.Value + "&key=" + gaKey);
                    WebResponse response = (WebResponse)request.GetResponse();

                    WebHeaderCollection header = response.Headers;

                    XElement ele = XElement.Load(response.GetResponseStream());

                    float lat = float.Parse(ele.Element("result").Element("geometry").Element("location").Element("lat").Value);
                    float lng = float.Parse(ele.Element("result").Element("geometry").Element("location").Element("lng").Value);
                    Thread.Sleep(500);
                    UpdateRecord(i.Key, lat, lng);
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
