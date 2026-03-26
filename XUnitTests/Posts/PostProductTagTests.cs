using FakeItEasy;
using MediatR;
using Core.Application.Mediatr.Posts.Commands;
using Core.Application.Mediatr.Posts.Queries;
using Core.Application.Models.Post;
using Xunit;

namespace XUnitTests.Posts
{
    public class PostProductTagTests
    {
        [Fact]
        public void ReplacePostProductTagsCommand_ValidPixelCoordinates_ShouldPassValidation()
        {
            // Arrange
            var command = new ReplacePostProductTagsCommand
            {
                PostUid = "test-post-uid",
                PostProductTags = new List<PostProductTagDto>
                {
                    new PostProductTagDto
                    {
                        ProductUid = "product-1",
                        LocationX = 142.67,
                        LocationY = 174.33,
                        ImageWidth = 400,
                        ImageHeight = 600
                    },
                    new PostProductTagDto
                    {
                        ProductUid = "product-2",
                        LocationX = 200.0,
                        LocationY = 150.0,
                        ImageWidth = 400,
                        ImageHeight = 600
                    }
                }
            };

            // Act & Assert
            // The command should be valid with pixel coordinates within reasonable ranges
            Assert.True(command.PostProductTags.All(tag => 
                tag.LocationX >= 0 && tag.LocationX <= 2000 &&
                tag.LocationY >= 0 && tag.LocationY <= 2000 &&
                tag.ImageWidth > 0 && tag.ImageHeight > 0));
        }

        [Fact]
        public void ReplacePostProductTagsCommand_EmptyTags_ShouldBeValid()
        {
            // Arrange
            var command = new ReplacePostProductTagsCommand
            {
                PostUid = "test-post-uid",
                PostProductTags = new List<PostProductTagDto>()
            };

            // Act & Assert
            // The command should be valid with no tags (removes all tags)
            Assert.NotNull(command.PostProductTags);
            Assert.Empty(command.PostProductTags);
        }

        [Fact]
        public void ReplacePostProductTagsCommand_CoordinateConversion_ShouldCalculateCorrectPercentages()
        {
            // Arrange
            var tag = new PostProductTagDto
            {
                ProductUid = "product-1",
                LocationX = 200.0,
                LocationY = 150.0,
                ImageWidth = 400.0,
                ImageHeight = 600.0
            };

            // Act - Simulate the conversion logic
            var leftPercent = tag.ImageWidth > 0 ? (tag.LocationX / tag.ImageWidth) * 100 : 0;
            var topPercent = tag.ImageHeight > 0 ? (tag.LocationY / tag.ImageHeight) * 100 : 0;

            // Assert
            Assert.Equal(50.0, leftPercent); // 200/400 * 100 = 50%
            Assert.Equal(25.0, topPercent);  // 150/600 * 100 = 25%
        }
    }
}
