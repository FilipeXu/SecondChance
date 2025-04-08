using Microsoft.AspNetCore.Mvc;
using SecondChance.Controllers;
using SecondChance.Models;
using SecondChance.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;
using Moq;

namespace TestProject1.Integration
{
    public class ReportControllerIntegrationTests : IntegrationTestBase
    {
        private readonly ReportController _controller;

        public ReportControllerIntegrationTests() => 
            _controller = new ReportController(_userManager, _context);

        private void SetupControllerUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task ReportUser_Get_ReturnsViewWithModel()
        {
            var reporter = await CreateTestUser("reporter@test.com");
            var reportedUser = await CreateTestUser("reported@test.com");
            SetupControllerUser(reporter);

            var result = await _controller.ReportUser(reportedUser.Id) as ViewResult;

            Assert.NotNull(result);
            var viewModel = Assert.IsType<ReportUserViewModel>(result.Model);
            Assert.Equal(reportedUser.Id, viewModel.ReportedUserId);
            Assert.Equal(reportedUser.FullName, viewModel.ReportedUserName);
        }

        [Fact]
        public async Task ReportUser_ValidatesInput()
        {
            var reporter = await CreateTestUser("reporter@test.com");
            var reportedUser = await CreateTestUser("reported@test.com");
            SetupControllerUser(reporter);

            var invalidReport = new ReportUserViewModel
            {
                ReportedUserId = reportedUser.Id,
                ReportedUserName = reportedUser.FullName,
                Reason = "",
                Details = "Details"
            };

            _controller.ModelState.AddModelError("Reason", "O motivo é obrigatório");
            var result = await _controller.ReportUser(invalidReport) as ViewResult;

            Assert.NotNull(result);
            Assert.False(_controller.ModelState.IsValid);
            var reasonErrors = _controller.ModelState["Reason"]?.Errors;
            Assert.NotNull(reasonErrors);
            Assert.Contains(reasonErrors, error => error.ErrorMessage == "O motivo é obrigatório");
        }

        [Fact]
        public async Task ReportUser_CreatesNewReport()
        {
            var reporter = await CreateTestUser("reporter@test.com");
            var reportedUser = await CreateTestUser("reported@test.com");
            SetupControllerUser(reporter);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.ControllerContext.HttpContext.User
                }
            };
            _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                _controller.ControllerContext.HttpContext,
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()
            );

            var validReport = new ReportUserViewModel
            {
                ReportedUserId = reportedUser.Id,
                ReportedUserName = reportedUser.FullName,
                Reason = "Comportamento inadequado",
                Details = "Detalhes do comportamento inadequado"
            };

            var result = await _controller.ReportUser(validReport) as RedirectToPageResult;

            Assert.NotNull(result);
            Assert.Equal("/Account/Manage/Index", result.PageName);
            Assert.NotNull(result.RouteValues);
            
            Assert.Equal("Identity", result.RouteValues["area"].ToString());
            Assert.Equal(reportedUser.Id, result.RouteValues["userId"].ToString());

            var report = await _context.UserReports.FirstOrDefaultAsync(r => 
                r.ReporterUserId == reporter.Id && 
                r.ReportedUserId == reportedUser.Id);
            
            Assert.NotNull(report);
            Assert.Equal(validReport.Reason, report.Reason);
            Assert.Equal(validReport.Details, report.Details);
            Assert.False(report.IsResolved);
        }
    }
}