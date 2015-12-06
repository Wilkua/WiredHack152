using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WiredHack2015.Startup))]
namespace WiredHack2015
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
