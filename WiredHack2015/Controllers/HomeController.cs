using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using WiredHack2015.Models;

namespace WiredHack2015.Controllers
{
    public class HomeController : Controller
    {
        private WiredHackEntities db = new WiredHackEntities();

        public ActionResult Index()
        {
#if false
            double lat = 34.9037259;
            double lng = -81.0092062;
#endif
            MapViewModel viewModel = new MapViewModel();
            viewModel.MapJSON = "";
            //viewModel.DealersList = (IEnumerable<stgDealer>)db.sp_getDealersByLatLong(lat,lng,50).ToList();
            viewModel.DealersList = db.stgDealers.ToList();
            viewModel.BrandYearPieChartJSON = GetBrandYearPieChart(db.stgDealers.ToList());
            viewModel.YearPieChartJSON = GetYearBrandPieChart(db.stgDealers.ToList());
            viewModel.BrandYearLineChartJSON = GetLineChart(db.stgDealers.ToList());
            viewModel.HeatmapScript = GetHeatmapScript();
            viewModel.MarkerMapJSON = GetMarkerScript(db.stgDealers.ToList());
            viewModel.LatLongSearch = "var Templat = \"\";\nvar Templong = \"\";\nvar Zoomin = 3;";
            return View(viewModel);
        }


        [HttpPost]
        //public ActionResult Index(FormCollection form)
        public ActionResult Index([Bind(Include = "ZipCode,Brand,City,State,DateBefore,DateAfter,Radius")]MapViewModel viewModel)
        {
#if false
            string Zip = form["ZipCode"];
            string Brand = form["Brand"];
            String City = form["City"];
            String State = form["State"];

            DateTime Before = new DateTime();
            Before = DateTime.Parse(form["DateBefore"]);
            DateTime After = new DateTime();
            After = DateTime.Parse(form["DateAfter"]);            
#endif

            IEnumerable<stgDealer> list = db.stgDealers;
            List<stgDealer> transList = new List<stgDealer>();
            float lat;
            float lng;
            if (String.IsNullOrEmpty(viewModel.ZipCode)) { 
            if (!db.PostalCodeLatLongs.Any(o => o.PostalCode == viewModel.ZipCode))
            {
                    try {
                        WebRequest request = WebRequest.Create("https://maps.googleapis.com/maps/api/geocode/xml?address="
                            + viewModel.ZipCode + ",&key=AIzaSyAHHCY6JtRuBrmAh_KDOA4npMi1GgR4rGo");
                        WebResponse response = (WebResponse)request.GetResponse();

                        //WebHeaderCollection header = response.Headers;

                        XElement ele = XElement.Load(response.GetResponseStream());
                        lat = float.Parse(ele.Element("result").Element("geometry").Element("location").Element("lat").Value);
                        lng = float.Parse(ele.Element("result").Element("geometry").Element("location").Element("lng").Value);

                        db.PostalCodeLatLongs.Add(new PostalCodeLatLong()
                        {
                            Lat = lat,
                            Long = lng,
                            PostalCode = viewModel.ZipCode
                        });

                        db.SaveChanges();
                    }
                    catch(Exception e)
                    {

                    }
            }
            else {
                lat = float.Parse(db.PostalCodeLatLongs.FirstOrDefault(o => o.PostalCode == viewModel.ZipCode).Lat.ToString());
                lng = float.Parse(db.PostalCodeLatLongs.FirstOrDefault(o => o.PostalCode == viewModel.ZipCode).Long.ToString());
            }
            viewModel.LatLongSearch = "var Templat = \"" + lat + "\";\nvar Templong = \"" + lng + "\";\nvar Zoomin = 10;";
                            
                foreach (var item in db.sp_getDealersByLatLong(viewModel.Radius, lat, lng).ToList())
                {
                    transList.Add(new stgDealer
                    {
                        BrandName = item.BrandName,                
                        DealerName = item.DealerName,
                        SignedOn = item.SignedOn,
                        DealerCode = item.DealerCode,
                        ManfRegionCode = item.ManfRegionCode,
                        Address1 = item.Address1,
                        Address2 = item.Address2,
                        City = item.City,
                        State = item.State,
                        PostalCode = item.PostalCode,
                        Lat = item.Lat,
                        Long = item.Long,
                        id = item.id
                    });
                }

                list = transList.AsEnumerable();
            }
            else
            {
                viewModel.LatLongSearch = "var Templat = \"" + "" + "\";\nvar Templong = \"" + "" + "\";\nvar Zoomin = 3;";
            }

            if ( !String.IsNullOrEmpty(viewModel.Brand) )
            {
                list = list.Where(o => o.BrandName == viewModel.Brand);
            }

            if ( !String.IsNullOrEmpty(viewModel.City) )
            {
                list = list.Where(o => o.City == viewModel.City);
            }

            if ( !String.IsNullOrEmpty(viewModel.State) )
            {
                list = list.Where(o => o.State == viewModel.State);
            }

            if (viewModel.DateBefore != DateTime.MinValue)
            {
                list = list.Where(o => o.SignedOn >= viewModel.DateBefore);
            }

            if (viewModel.DateAfter != DateTime.MinValue)
            {
                list = list.Where(o => o.SignedOn <= viewModel.DateAfter);
            }

            viewModel.DealersList = list;

            viewModel.BrandYearPieChartJSON = GetBrandYearPieChart(list);
            viewModel.BrandYearLineChartJSON = GetLineChart(list);
            viewModel.YearPieChartJSON = GetYearBrandPieChart(list);

            viewModel.HeatmapScript = GetHeatmapScript();
            viewModel.MarkerMapJSON = GetMarkerScript(list);


            return View(viewModel);
        }

        public ActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload(HttpPostedFileBase xlsx_file)
        {
            if (xlsx_file != null && xlsx_file.ContentLength > 0)
            {
                try
                {
                    string path = Path.Combine(Server.MapPath("~/Content/dealer_files/uploads"),
                                               Path.GetFileName(xlsx_file.FileName));
                    xlsx_file.SaveAs(path);
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "ERROR:" + ex.Message.ToString();
                    return View();
                }
            }
            else
            {
                ViewBag.Message = "You have not specified a file.";
                return View();
            }

            return RedirectToAction("Index");
        }

        public string GetHeatmapScript()
        {
            string heatmapScript = "var heatmapData = [";

            List<stgDealer> HeatmapDataList = db.stgDealers.ToList();

            foreach (var item in HeatmapDataList)
            {
                heatmapScript += "new google.maps.LatLng(" + item.Lat + "," + item.Long + ")";

                if (item != HeatmapDataList.Last())
                {
                    heatmapScript += ",";
                }
            }

            heatmapScript += "]";

            return heatmapScript;
        }

        public string GetMarkerScript(IEnumerable<stgDealer> MarkerDataList)
        {
            string InfoWindows = "var infoWindowContent = [";

                foreach (var item in MarkerDataList)
                {
                    InfoWindows += "['<div class=\"info_content\">" +
                        "<p><b>Dealer:</b> " + item.DealerName.Replace("'", "") + "</p>" +
                        "<p><b>Brand:</b> "+item.BrandName+"</p>" +
                        "<p><b>Address:</b></p>" +
                        "<p>" + item.Address1 + ", " + item.City + ", " + item.State + ", " + item.PostalCode + "</p>" +
                        "</div>']"; 
                    if(item != MarkerDataList.Last()){
                        InfoWindows += ",";
                    }
                }
                InfoWindows += "];";
            

            string MarkerScript = "var markers = [";


            foreach (var item in MarkerDataList)
            {
                MarkerScript += "['"+item.DealerName.Replace("'","")+"',"+item.Lat+","+item.Long+"]";            

                if (item != MarkerDataList.Last())
                {
                    MarkerScript += ",";
                }
            }

            MarkerScript += "];\n";

            MarkerScript = MarkerScript+InfoWindows;
            return MarkerScript;
        }



        public string GetBrandYearPieChart(IEnumerable<stgDealer> list)
        {
            String BrandYearPieChart = "series: [{\n" +
                "name: 'Dealers',\n" +
                "colorByPoint: true,\n" +
                "data: [\n";

            foreach (String i in list.Select(o => o.BrandName).Distinct())
            {
                BrandYearPieChart +=
                    "{\n" +
                    "name: '" + i + "',\n" +
                    "y: " + list.Where(o => o.BrandName == i).Count() + ",\n" +
                    "drilldown: '" + i + "Years'\n" +
                    "}";

                if (list.Select(o => o.BrandName).Distinct().Last() != i)
                {
                    BrandYearPieChart += ",\n";
                }
                else { BrandYearPieChart += "\n"; }
            }

            BrandYearPieChart += "]\n" +
                "}],\n" +
                "drilldown: {\n" +
                "series: [\n";

            foreach (String i in list.Select(o => o.BrandName).Distinct())
            {
                BrandYearPieChart += "{id: '" + i + "Years',\n" +
                    "data: [\n";

                foreach (int foo in list.Select(o => o.SignedOn.Value.Year).Distinct())
                {
                    BrandYearPieChart += "['" + foo + "', " + list.Where(o => o.BrandName == i).Where(z => z.SignedOn.Value.Year == foo).Count() + "]\n";

                    if (list.Select(o => o.SignedOn.Value.Year).Distinct().Last() != foo)
                    {
                        BrandYearPieChart += ",\n";
                    }
                }

                BrandYearPieChart += "]\n" +
                    "}\n";
                if (list.Select(o => o.BrandName).Distinct().Last() != i)
                {
                    BrandYearPieChart += ",\n";
                }
                else { BrandYearPieChart += "\n"; }
            }
            BrandYearPieChart += "]\n" +
                "}\n";

            return BrandYearPieChart;
        }

        public string GetYearBrandPieChart(IEnumerable<stgDealer> list)
        {
            String YearBrandPieChart = "series: [{\n" +
                "name: 'Dealers',\n" +
                "colorByPoint: true,\n" +
                "data: [\n";

            foreach (int i in list.Select(o => o.SignedOn.Value.Year).Distinct())
            {
                YearBrandPieChart +=
                    "{\n" +
                    "name: '" + i + "',\n" +
                    "y: " + list.Where(o => o.SignedOn.Value.Year == i).Count() + ",\n" +
                    "drilldown: '" + i + "Brands'\n" +
                    "}";

                if (list.Select(o => o.SignedOn.Value.Year).Distinct().Last() != i)
                {
                    YearBrandPieChart += ",\n";
                }
                else { YearBrandPieChart += "\n"; }
            }

            YearBrandPieChart += "]\n" +
                "}],\n" +
                "drilldown: {\n" +
                "series: [\n";

            foreach (int i in list.Select(o => o.SignedOn.Value.Year).Distinct())
            {
                YearBrandPieChart += "{id: '" + i + "Brands',\n" +
                    "data: [\n";

                foreach (string foo in list.Select(o => o.BrandName).Distinct())
                {
                    YearBrandPieChart += "['" + foo + "', " + list.Where(o => o.SignedOn.Value.Year == i).Where(z => z.BrandName == foo).Count() + "]\n";

                    if (list.Select(o => o.BrandName).Distinct().Last() != foo)
                    {
                        YearBrandPieChart += ",\n";
                    }
                }

                YearBrandPieChart += "]\n" +
                    "}\n";
                if (list.Select(o => o.SignedOn.Value.Year).Distinct().Last() != i)
                {
                    YearBrandPieChart += ",\n";
                }
                else { YearBrandPieChart += "\n"; }
            }
            YearBrandPieChart += "]\n" +
                "}\n";

            return YearBrandPieChart;
        }

        public string GetLineChart(IEnumerable<stgDealer> list)
        {
            String LineChart = "xAxis: {"+
                "categories: [";
            
            foreach(int i in list.Select(o=>o.SignedOn.Value.Year).Distinct()){
                LineChart += "'"+i+"'";

                if(list.Select(o=>o.SignedOn.Value.Year).Distinct().Last() != i){
                    LineChart += ",";
                }
            }
            
            LineChart += "]" +
        "}," +
        "yAxis: {" +
            "title: {" +
                "text: 'Dealerships Signed'" +
            "}," +
            "plotLines: [{" +
                "value: 0,"+
                "width: 1,"+
                "color: '#808080'"+
            "}]" +
        "}," +
        "legend: {" +
            "layout: 'vertical'," +
            "align: 'right'," +
            "verticalAlign: 'middle'," +
            "borderWidth: 0"+
        "},"+
        "series: [";
            foreach(string i in list.Select(o => o.BrandName).Distinct()){ 
                LineChart += "{"+
                    "name: '"+i+"'," +
                    "data: [";
                foreach(int foo in list.Where(o => o.BrandName == i).Select(f => f.SignedOn.Value.Year).Distinct()){
                    LineChart += list.Where(o => o.BrandName == i).Where(f => f.SignedOn.Value.Year == foo).Count();
                    
                    if(list.Where(o => o.BrandName == i).Select(f => f.SignedOn.Value.Year).Distinct().Last() != foo){
                        LineChart += ",";
                    }
                }

                LineChart += "]}";

                if(list.Select(o => o.BrandName).Distinct().Last() != i){
                    LineChart += ",";
                }
            }
                LineChart += "]";

            return LineChart;
        }
    }
}