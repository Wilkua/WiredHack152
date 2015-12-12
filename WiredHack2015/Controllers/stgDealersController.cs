using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WiredHack2015;

namespace WiredHack2015.Controllers
{
    public class stgDealersController : Controller
    {
        private WiredHackEntities db = new WiredHackEntities();

        // GET: stgDealers
        public ActionResult Index()
        {  
            return View(db.stgDealers.ToList());
        }
        [HttpPost]
        public ActionResult Index(FormCollection form)
        {
            string Brand = form["Brand"];
            String City = form["City"];
            String State = form["State"];
            
            DateTime Before = new DateTime();
            Before = DateTime.Parse(form["DateBefore"]);
            DateTime After = new DateTime();
            After = DateTime.Parse(form["DateAfter"]);

            IEnumerable<stgDealer> list = db.stgDealers.ToList();

            if (Brand != "")
            {
                list = list.Where(o => o.BrandName == Brand);
            }
            if (City != "")
            {
                list = list.Where(o => o.City == City);
            }
            if (State != "")
            {
                list = list.Where(o => o.State == State);
            }
            if (form["DateBefore"] != "")
            {
                list = list.Where(o => o.SignedOn >= Before);
            }
            if (form["DateAfter"] != "")
            {
                list = list.Where(o => o.SignedOn <= After);
            }


            
            return View(list);
        }
        // GET: stgDealers/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            stgDealer stgDealer = db.stgDealers.Find(id);
            if (stgDealer == null)
            {
                return HttpNotFound();
            }
            return View(stgDealer);
        }

        // GET: stgDealers/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: stgDealers/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "BrandName,DealerName,SignedOn,DealerCode,ManfRegionCode,Address1,Address2,City,State,PostalCode,Lat,Long,id")] stgDealer stgDealer)
        {
            if (ModelState.IsValid)
            {
                db.stgDealers.Add(stgDealer);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(stgDealer);
        }

        // GET: stgDealers/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            stgDealer stgDealer = db.stgDealers.Find(id);
            if (stgDealer == null)
            {
                return HttpNotFound();
            }
            return View(stgDealer);
        }

        // POST: stgDealers/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "BrandName,DealerName,SignedOn,DealerCode,ManfRegionCode,Address1,Address2,City,State,PostalCode,Lat,Lng,id")] stgDealer stgDealer)
        {
            if (ModelState.IsValid)
            {
                db.Entry(stgDealer).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(stgDealer);
        }

        // GET: stgDealers/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            stgDealer stgDealer = db.stgDealers.Find(id);
            if (stgDealer == null)
            {
                return HttpNotFound();
            }
            return View(stgDealer);
        }

        // POST: stgDealers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            stgDealer stgDealer = db.stgDealers.Find(id);
            db.stgDealers.Remove(stgDealer);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
