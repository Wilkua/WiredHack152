using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Mvc;

namespace WiredHack2015.Controllers
{
    public class APIController : Controller
    {
        private WiredHackEntities dbContext = new WiredHackEntities();

        // GET: API
        public string Index()
        {
            return "<!DOCTYPE html><html lang=\"en\"><head><title></title><meta charset=\"utf-8\" /></head><body></body></html>";
        }

        // GET: API/HeatmapData?
        public string HeatmapData()
        {
            return "";
        }

        // GET: API/CitiesFromState?state=XX
        public string CitiesFromState(string state)
        {
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

            var JSONSerializer = new JavaScriptSerializer();

            return JSONSerializer.Serialize(cities);
        }
    }
}