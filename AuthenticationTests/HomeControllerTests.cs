using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SecondChance.Controllers;
using SecondChance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationTests
{
    public class HomeControllerTests
    {


        [Fact]
        public void Index_ReturnsViewResult()
        {
            var hc = new HomeController(null);

            var result = hc.Index();

            Assert.IsType<ViewResult>(result);
        }


        


        [Fact]
        public void Privacy_ReturnsViewResult()
        {
            var hc = new HomeController(null);

            var result = hc.Privacy();

            Assert.IsType<ViewResult>(result);
        }


        [Fact]
        public void Error_ReturnsViewResult()
        {
            var mockLogger = new Mock<ILogger<HomeController>>();
            var hc = new HomeController(null);

            var mockHttpContext = new Mock<Microsoft.AspNetCore.Http.HttpContext>();
            mockHttpContext.Setup(h => h.TraceIdentifier).Returns("Test");
            hc.ControllerContext = new ControllerContext();
            hc.ControllerContext.HttpContext = mockHttpContext.Object;

            var result = hc.Error();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<ErrorViewModel>(
                viewResult.ViewData.Model);
            Assert.NotNull(model);
        }
    }
}
