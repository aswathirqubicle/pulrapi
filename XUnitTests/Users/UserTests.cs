using FakeItEasy;
using MediatR;
using Core.Application.Mediatr.Users.Commands.Login;
using Core.Application.Interfaces;
using Core.Application.Models.External.Apple;
using Core.Application.Models.Users;
using Core.Application.Exceptions;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Xunit;

namespace XUnitTests.Users
{
    public class UserTests
    {
        private readonly IMediator _mediator;

        public UserTests()
        {
            _mediator = A.Fake<IMediator>();
        }

        [Fact]
        public void UserTests_LoginCommand_LoginResponse()
        {
            // Arrange
            var command = new LoginCommand()
            {
                IsEmail = true,
                Username = "user",
                Password = "pwd",
            };
            var handler = new LoginCommandHandler(null,null,null,null);

            // Act

            //Unit x = await handler.Handle(command, new System.Threading.CancellationToken());

            // Assert

            //Assert
            //_mediator.Verify(x => x.Publish(It.IsAny<CustomersChanged>()));
            
        }

        [Fact]
        public async Task AppleLogin_WithExpiredToken_ShouldThrowNotAuthenticatedException()
        {
            // Arrange
            var userService = A.Fake<IUserService>();
            var expiredToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLmV4YW1wbGUuYXBwIiwiZXhwIjoxNjAwMDAwMDAwLCJpYXQiOjE2MDAwMDAwMDAsInN1YiI6IjEyMzQ1Njc4OTAifQ.invalid_signature";
            
            // Mock the user service to throw NotAuthenticatedException for expired token
            A.CallTo(() => userService.LoginWithAppleAsync(expiredToken, null, null))
                .Throws(new NotAuthenticatedException("Invalid or expired Apple identity token."));

            // Act & Assert
            await Assert.ThrowsAsync<NotAuthenticatedException>(async () =>
            {
                var command = new AppleLoginCommand
                {
                    IdentityToken = expiredToken
                };
                var handler = new AppleLoginCommandHandler(userService,null,null);
                await handler.Handle(command, System.Threading.CancellationToken.None);
            });
        }

        [Theory]
        [InlineData("<script>alert(1)</script>")]
        [InlineData("'; DROP TABLE Users; --")]
        [InlineData("device@#$%^&*()")]
        [InlineData("device with spaces")]
        [InlineData("device\nwith\nnewlines")]
        [InlineData("device\twith\ttabs")]
        [InlineData("")]
        [InlineData(null)]
        public void SignOutDeviceRequest_WithMaliciousInput_ShouldFailValidation(string maliciousInput)
        {
            // Arrange
            var request = new SignOutDeviceRequest
            {
                DeviceIdentifier = maliciousInput
            };

            // Act
            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(validationResults);
        }

        [Theory]
        [InlineData("valid-device-123")]
        [InlineData("device_identifier_456")]
        [InlineData("Device123")]
        [InlineData("a")]
        [InlineData("1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890")] // 100 chars
        public void SignOutDeviceRequest_WithValidInput_ShouldPassValidation(string validInput)
        {
            // Arrange
            var request = new SignOutDeviceRequest
            {
                DeviceIdentifier = validInput
            };

            // Act
            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void SignOutDeviceRequest_WithTooLongInput_ShouldFailValidation()
        {
            // Arrange
            var request = new SignOutDeviceRequest
            {
                DeviceIdentifier = new string('a', 101) // 101 characters - exceeds limit
            };

            // Act
            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, vr => vr.ErrorMessage.Contains("cannot exceed 100 characters"));
        }
    }
}