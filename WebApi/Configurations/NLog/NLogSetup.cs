using Microsoft.Extensions.Configuration;
using NLog;
using NLog.AWS.Logger;
using NLog.Config;
using NLog.Layouts;

namespace WebApi.Configurations.NLog
{
    public static class NLogSetup
    {
        private static LoggingConfiguration AddAwsTarget(IConfiguration configuration)
        {
            var awsTarget = new AWSTarget()
            {
                LogGroup = configuration["Aws:CloudwatchLogGroup"],
                Region = configuration["Aws:CloudwatchRegion"],
                Credentials = new Amazon.Runtime.BasicAWSCredentials(
                    configuration["Aws:CloudwatchAccessKey"],
                    configuration["Aws:CloudwatchSecret"]),
                Layout = new JsonLayout()
                {
                    Attributes = {
                        new JsonAttribute("level", "${level}"),
                        new JsonAttribute("message", "${message}"),
                        new JsonAttribute("logger", "${logger}"),
                        new JsonAttribute("callsite", "${callsite:filename=true}"),
                        new JsonAttribute("exception", "${exception:tostring}"),
                        new JsonAttribute("logged", "${longdate}"),
                    }
                }
            };

            var config = new LoggingConfiguration();
            config.AddTarget("aws", awsTarget);

            var rule = new LoggingRule("*", LogLevel.Info, awsTarget);
            config.LoggingRules.Add(rule);

            return config;
        }

        internal static void Configure(IConfiguration configuration)
        {
            LogManager.Configuration = AddAwsTarget(configuration);
        }
    }
}
