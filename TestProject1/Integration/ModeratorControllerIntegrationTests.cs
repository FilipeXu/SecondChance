using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondChance.Controllers;
using SecondChance.Models;
using SecondChance.ViewModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace TestProject1.Integration
{
    public class ModeratorControllerIntegrationTests : IntegrationTestBase
    {
        private readonly ModeratorController _controller;

        public ModeratorControllerIntegrationTests() => 
            _controller = new ModeratorController(_userManagerMock.Object, _context);

        private void SignInAsAdmin(User admin)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id),
                new Claim(ClaimTypes.Name, admin.UserName ?? string.Empty),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
            
            _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(admin);
        }

        [Fact]
        public async Task Users_ReturnsAllSystemUsers()
        {
            var user1 = await CreateTestUser("user1@test.com");
            var user2 = await CreateTestUser("user2@test.com");
            var admin = await CreateTestUser("admin@test.com");
            admin.IsAdmin = true;
            await _context.SaveChangesAsync();

            SignInAsAdmin(admin);

            var result = await _controller.Users() as ViewResult;

            Assert.NotNull(result);
            var users = Assert.IsAssignableFrom<List<User>>(result.Model);
            Assert.Equal(3, users.Count);
            Assert.Contains(users, u => u.Id == user1.Id);
            Assert.Contains(users, u => u.Id == user2.Id);
            Assert.Contains(users, u => u.Id == admin.Id);
        }

        [Fact]
        public async Task Reports_ReturnsAllUserReports()
        {
            var reporter = await CreateTestUser("reporter@test.com");
            var reportedUser = await CreateTestUser("reported@test.com");
            var admin = await CreateTestUser("admin@test.com");
            admin.IsAdmin = true;
            await _context.SaveChangesAsync();

            var reports = new[]
            {
                new UserReport
                {
                    ReporterUserId = reporter.Id,
                    ReporterUser = reporter,
                    ReportedUserId = reportedUser.Id,
                    ReportedUser = reportedUser,
                    Reason = "Spam",
                    Details = "Muitas mensagens",
                    ReportDate = DateTime.UtcNow,
                    IsResolved = false
                },
                new UserReport
                {
                    ReporterUserId = reporter.Id,
                    ReporterUser = reporter,
                    ReportedUserId = reportedUser.Id,
                    ReportedUser = reportedUser,
                    Reason = "Spam",
                    Details = "Muitas mensagens",
                    ReportDate = DateTime.UtcNow,
                    IsResolved = true,
                    Resolution = "Corrigido",
                    ResolvedDate = DateTime.UtcNow,
                    ResolvedById = admin.Id,
                    ResolvedBy = admin
                }
            };

            await _context.UserReports.AddRangeAsync(reports);
            await _context.SaveChangesAsync();

            SignInAsAdmin(admin);

            var result = await _controller.Reports() as ViewResult;

            Assert.NotNull(result);
            var reportsList = Assert.IsAssignableFrom<List<UserReport>>(result.Model);
            Assert.Equal(2, reportsList.Count);
        }

        [Fact]
        public async Task ReportDetails_ValidId_ShowsReportDetails()
        {
            var reporter = await CreateTestUser("reporter@test.com");
            var reportedUser = await CreateTestUser("reported@test.com");
            var admin = await CreateTestUser("admin@test.com");
            admin.IsAdmin = true;
            await _context.SaveChangesAsync();

            var report = new UserReport
            {
                ReporterUserId = reporter.Id,
                ReporterUser = reporter,
                ReportedUserId = reportedUser.Id,
                ReportedUser = reportedUser,
                Reason = "Spam",
                Details = "Muitas mensagens",
                ReportDate = DateTime.UtcNow,
                IsResolved = false
            };

            await _context.UserReports.AddAsync(report);
            await _context.SaveChangesAsync();

            SignInAsAdmin(admin);

            var result = await _controller.ReportDetails(report.Id) as ViewResult;

            Assert.NotNull(result);
            var reportModel = Assert.IsType<UserReport>(result.Model);
            Assert.Equal(report.Id, reportModel.Id);
            Assert.Equal(reporter.Id, reportModel.ReporterUserId);
            Assert.Equal(reportedUser.Id, reportModel.ReportedUserId);
            Assert.Equal("Spam", reportModel.Reason);
        }
    }
}