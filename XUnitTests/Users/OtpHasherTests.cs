using System.Collections.Generic;
using Core.Infrastructure.Services.Users;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace XUnitTests.Users
{
    public class OtpHasherTests
    {
        private static OtpHasher CreateHasher()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Otp:HashKey"] = "unit-test-otp-secret-key-1234567890"
                })
                .Build();
            return new OtpHasher(config);
        }

        [Fact]
        public void Hash_DoesNotReturnPlaintext()
        {
            var hasher = CreateHasher();

            var hash = hasher.Hash("123456");

            Assert.NotEqual("123456", hash);
            Assert.False(string.IsNullOrWhiteSpace(hash));
        }

        [Fact]
        public void Hash_IsDeterministic()
        {
            var hasher = CreateHasher();

            Assert.Equal(hasher.Hash("123456"), hasher.Hash("123456"));
        }

        [Fact]
        public void Verify_CorrectCode_ReturnsTrue()
        {
            var hasher = CreateHasher();
            var hash = hasher.Hash("654321");

            Assert.True(hasher.Verify("654321", hash));
        }

        [Fact]
        public void Verify_WrongCode_ReturnsFalse()
        {
            var hasher = CreateHasher();
            var hash = hasher.Hash("654321");

            Assert.False(hasher.Verify("000000", hash));
        }

        [Fact]
        public void Verify_NullOrEmptyInputs_ReturnFalse()
        {
            var hasher = CreateHasher();
            var hash = hasher.Hash("111111");

            Assert.False(hasher.Verify(null, hash));
            Assert.False(hasher.Verify("111111", null));
            Assert.False(hasher.Verify("", ""));
        }

        [Fact]
        public void Hash_DifferentKeys_ProduceDifferentHashes()
        {
            var hasherA = CreateHasher();

            var configB = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Otp:HashKey"] = "a-completely-different-secret-key"
                })
                .Build();
            var hasherB = new OtpHasher(configB);

            Assert.NotEqual(hasherA.Hash("123456"), hasherB.Hash("123456"));
        }
    }
}
