﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WiredHack2015
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    
    public partial class WiredHackEntities : DbContext
    {
        public WiredHackEntities()
            : base("name=WiredHackEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<loadDealer> loadDealers { get; set; }
        public virtual DbSet<tblBrand> tblBrands { get; set; }
        public virtual DbSet<tblCity> tblCities { get; set; }
        public virtual DbSet<tblState> tblStates { get; set; }
        public virtual DbSet<stgDealer> stgDealers { get; set; }
    
        public virtual ObjectResult<GetMissingRecordsLatAndLong_Result> GetMissingRecordsLatAndLong()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<GetMissingRecordsLatAndLong_Result>("GetMissingRecordsLatAndLong");
        }
    
        public virtual ObjectResult<GetRecordsByBrand_Result> GetRecordsByBrand(string brand)
        {
            var brandParameter = brand != null ?
                new ObjectParameter("Brand", brand) :
                new ObjectParameter("Brand", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<GetRecordsByBrand_Result>("GetRecordsByBrand", brandParameter);
        }
    
        public virtual int UpdateLatandLong(Nullable<int> id, Nullable<double> lat, Nullable<double> lng)
        {
            var idParameter = id.HasValue ?
                new ObjectParameter("Id", id) :
                new ObjectParameter("Id", typeof(int));
    
            var latParameter = lat.HasValue ?
                new ObjectParameter("lat", lat) :
                new ObjectParameter("lat", typeof(double));
    
            var lngParameter = lng.HasValue ?
                new ObjectParameter("lng", lng) :
                new ObjectParameter("lng", typeof(double));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("UpdateLatandLong", idParameter, latParameter, lngParameter);
        }
    
        [DbFunction("WiredHackEntities", "sp_getDealersByLatLong")]
        public virtual IQueryable<sp_getDealersByLatLong_Result> sp_getDealersByLatLong(Nullable<double> lat, Nullable<double> @long, Nullable<int> distance)
        {
            var latParameter = lat.HasValue ?
                new ObjectParameter("lat", lat) :
                new ObjectParameter("lat", typeof(double));
    
            var longParameter = @long.HasValue ?
                new ObjectParameter("long", @long) :
                new ObjectParameter("long", typeof(double));
    
            var distanceParameter = distance.HasValue ?
                new ObjectParameter("distance", distance) :
                new ObjectParameter("distance", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<sp_getDealersByLatLong_Result>("[WiredHackEntities].[sp_getDealersByLatLong](@lat, @long, @distance)", latParameter, longParameter, distanceParameter);
        }
    }
}
