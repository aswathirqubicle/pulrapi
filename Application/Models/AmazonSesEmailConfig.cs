
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

namespace Core.Application.Models
{
    public class AmazonSesEmailConfig : AWSCredentials
    {
        private readonly IConfiguration _config;

        public AmazonSesEmailConfig(IConfiguration config)
        {
            _config = config;
        }

        public override ImmutableCredentials GetCredentials()
        {
            var creds = new ImmutableCredentials(_config["Aws:SES_Access_Key"], _config["Aws:SES_Secret"], null);
            return creds;
        }
    }
}
