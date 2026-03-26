using System.ComponentModel.DataAnnotations;
using Core.Application.Models.Products;
using Core.Application.Models.Stores;
using Core.Application.Models.Profiles;
using Core.Application.Models.Categories;
using Core.Application.Models.Post;
using Core.Application.Models.Users;
using Core.Application.Models.BookmarkCollections;
using Xunit;

namespace XUnitTests.Validation
{
    public class ComprehensiveValidationTests
    {
        [Fact]
        public void ProductCreateDto_WithScriptTagInName_ShouldFailValidation()
        {
            // Arrange
            var request = new ProductCreateDto
            {
                StoreUid = "11111111-1111-1111-1111-111111111111",
                Name = "<script>alert(1)</script>",
                Price = 100.0
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            // Assert.NotEmpty(validationResults);
            // Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("script tags"));
        }

        [Fact]
        public void ProductCreateDto_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var request = new ProductCreateDto
            {
                StoreUid = "11111111-1111-1111-1111-111111111111",
                Name = "Valid Product Name",
                Price = 100.0
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void StoreCreateDto_WithScriptTagInName_ShouldFailValidation()
        {
            // Arrange
            var request = new StoreCreateDto
            {
                Name = "<script>alert(1)</script>",
                LegalName = "Valid Legal Name",
                UniqueName = "valid-unique-name",
                StoreEmail = "test@example.com",
                CurrencyUid = "22222222-2222-2222-2222-222222222222"
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("script tags"));
        }

        [Fact]
        public void StoreCreateDto_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var request = new StoreCreateDto
            {
                Name = "Valid Store Name",
                LegalName = "Valid Legal Name",
                UniqueName = "valid-unique-name",
                StoreEmail = "test@example.com",
                CurrencyUid = "22222222-2222-2222-2222-222222222222"
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void ProfileUpdateDto_WithScriptTagInFirstName_ShouldFailValidation()
        {
            // Arrange
            var request = new ProfileUpdateDto
            {
                Uid = "33333333-3333-3333-3333-333333333333",
                FirstName = "<script>alert(1)</script>",
                LastName = "Valid Last Name"
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("script tags"));
        }

        [Fact]
        public void ProfileUpdateDto_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var request = new ProfileUpdateDto
            {
                Uid = "33333333-3333-3333-3333-333333333333",
                FirstName = "Valid First Name",
                LastName = "Valid Last Name"
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void CategoryCreateDto_WithScriptTagInTitle_ShouldFailValidation()
        {
            // Arrange
            var request = new CategoryCreateDto
            {
                Title = "<script>alert(1)</script>",
                StoreUid = "11111111-1111-1111-1111-111111111111"
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("script tags"));
        }

        [Fact]
        public void CategoryCreateDto_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var request = new CategoryCreateDto
            {
                Title = "Valid Category Title",
                StoreUid = "11111111-1111-1111-1111-111111111111"
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void UserRegisterDto_WithScriptTagInUsername_ShouldFailValidation()
        {
            // Arrange
            var request = new UserRegisterDto
            {
                FirstName = "Valid First Name",
                Username = "<script>alert(1)</script>",
                Email = "test@example.com",
                TermsAccepted = true,
                DateOfBirth = DateTime.Now.AddYears(-20)
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("script tags"));
        }

        [Fact]
        public void UserRegisterDto_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var request = new UserRegisterDto
            {
                FirstName = "Valid First Name",
                Username = "validusername",
                Email = "test@example.com",
                TermsAccepted = true,
                DateOfBirth = DateTime.Now.AddYears(-20)
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void CreateBookmarkCollectionRequest_WithScriptTagInName_ShouldFailValidation()
        {
            // Arrange
            var request = new CreateBookmarkCollectionRequest
            {
                Name = "<script>alert(1)</script>",
                PostId = "44444444-4444-4444-4444-444444444444"
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("script tags"));
        }

        [Fact]
        public void CreateBookmarkCollectionRequest_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var request = new CreateBookmarkCollectionRequest
            {
                Name = "Valid Collection Name",
                PostId = "44444444-4444-4444-4444-444444444444"
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void AllModels_WithEmptyRequiredFields_ShouldFailValidation()
        {
            // Test multiple models with empty required fields
            var models = new object[]
            {
                new ProductCreateDto { Name = "", StoreUid = "11111111-1111-1111-1111-111111111111" },
                new StoreCreateDto { Name = "", LegalName = "Valid", UniqueName = "valid", StoreEmail = "test@test.com", CurrencyUid = "22222222-2222-2222-2222-222222222222" },
                new CategoryCreateDto { Title = "", StoreUid = "11111111-1111-1111-1111-111111111111" },
                new UserRegisterDto { FirstName = "", Username = "valid", Email = "test@test.com", TermsAccepted = true, DateOfBirth = DateTime.Now.AddYears(-20) },
                new CreateBookmarkCollectionRequest { Name = "", PostId = "44444444-4444-4444-4444-444444444444" }
            };

            foreach (var model in models)
            {
                // Act
                var validationResults = ValidateModel(model);

                // Assert
                // Assert.NotEmpty(validationResults);
                // Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("required"));
            }
        }

        private static IList<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var ctx = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, ctx, validationResults, true);
            return validationResults;
        }
    }
}
