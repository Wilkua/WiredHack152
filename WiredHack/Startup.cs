using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WiredHack.Startup))]
namespace WiredHack
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
