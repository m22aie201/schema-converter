using Microsoft.AspNetCore.Mvc;
using Schema_Converters.Models;
using Schema_Converters.Services;
using System.Diagnostics;

namespace Schema_Converters.Controllers
{
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RelationshipTransformation _rt;
        private readonly InheritanceTransformation _inheritance;

        public HomeController(ILogger<HomeController> logger, RelationshipTransformation rt, InheritanceTransformation inheritance)
        {
            _logger = logger;
            _rt = rt;
            _inheritance = inheritance;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// This method returns results view based on action, Rule 1: One-to-One Association Relationship Transformation.
        /// </summary>
        /// <returns></returns>
        public IActionResult SchemaTransformOneToOne()
        {
            _rt.RelationshipTransform(false);
            return View("Results");
        }

        /// <summary>
        /// This method returns results view based on transformation, Rule 2: One-to-Many Association Relationship Transformation and Rule 3: Many-to-Many Association Relationship
        /// </summary>
        /// <returns></returns>
        public IActionResult SchemaTransform()
        {
            _rt.RelationshipTransform(true);
            return View("Results");
        }

        /// <summary>
        /// This method returns results view based on transformation, Rule 4: Specialization in Inheritance Relationship
        /// </summary>
        /// <returns></returns>
        public IActionResult InheritenceMapping()
        {
            _inheritance.InheritenceMapping();
            return View("Results");
        }
    }
}
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.