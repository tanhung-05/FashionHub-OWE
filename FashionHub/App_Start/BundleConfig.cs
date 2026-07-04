
using System.Web.Optimization;

namespace FashionHub 
{
    public class BundleConfig
    {

        public static void RegisterBundles(BundleCollection bundles)
        {
            // --- JAVASCRIPT BUNDLES ---

            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-3.7.1.js"));
            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
          "~/Scripts/bootstrap.bundle.min.js",
          "~/Scripts/site.js"));

            // --- CSS BUNDLES ---
            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.min.css", 
                      "~/Content/site.css"));       

            BundleTable.EnableOptimizations =true;
        }
    }
}