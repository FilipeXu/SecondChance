using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondChance.Controllers;
using SecondChance.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace TestProject1.Integration
{
    public class CommentsControllerIntegrationTests : IntegrationTestBase
    {
        private readonly CommentsController _controller;

        public CommentsControllerIntegrationTests() => 
            _controller = new CommentsController(_context, _userManager);

        private void SetupControllerUser(User user)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            }, "TestAuth");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        [Fact]
        public async Task Create_AddsNewComment()
        {
            var author = await CreateTestUser("author@test.com");
            var targetUser = await CreateTestUser("target@test.com");
            SetupControllerUser(author);

            var result = await _controller.Create(targetUser.Id, "Test comment") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("ProfileComments", result.ActionName);

            var savedComment = await _context.Comments.FirstOrDefaultAsync(c => c.ProfileId == targetUser.Id);
            Assert.NotNull(savedComment);
            Assert.Equal("Test comment", savedComment.Content);
            Assert.Equal(author.Id, savedComment.AuthorId);
        }

        [Fact]
        public async Task DeleteComment_RemovesComment()
        {
            var author = await CreateTestUser("author@test.com");
            var targetUser = await CreateTestUser("target@test.com");

            var comment = new Comment
            {
                Content = "Comment to delete",
                AuthorId = author.Id,
                Author = author,
                ProfileId = targetUser.Id,
                Profile = targetUser,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();

            SetupControllerUser(author);

            var result = await _controller.DeleteConfirmed(comment.Id) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("ProfileComments", result.ActionName);
            Assert.Null(await _context.Comments.FindAsync(comment.Id));
        }

        [Fact]
        public async Task GetComments_ReturnsProfileComments()
        {
            var author = await CreateTestUser("author@test.com");
            var targetUser = await CreateTestUser("target@test.com");

            await _context.Comments.AddRangeAsync(new[]
            {
                new Comment
                {
                    Content = "First comment",
                    AuthorId = author.Id,
                    Author = author,
                    ProfileId = targetUser.Id,
                    Profile = targetUser,
                    CreatedAt = DateTime.UtcNow
                },
                new Comment
                {
                    Content = "Second comment",
                    AuthorId = author.Id,
                    Author = author,
                    ProfileId = targetUser.Id,
                    Profile = targetUser,
                    CreatedAt = DateTime.UtcNow
                }
            });
            await _context.SaveChangesAsync();

            var result = await _controller.ProfileComments(targetUser.Id) as ViewResult;

            Assert.NotNull(result);
            var profileComments = Assert.IsAssignableFrom<IEnumerable<Comment>>(result.Model);
            Assert.Equal(2, profileComments.Count());
            Assert.Contains(profileComments, c => c.Content == "First comment");
            Assert.Contains(profileComments, c => c.Content == "Second comment");
        }
    }
}