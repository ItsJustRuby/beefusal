using System.IO;
using System.Reflection;
using FluentValidation;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text;
using System.Collections.Generic;

namespace Beefusal
{
    internal class Credentials
    {
        public string User { get; set; }
        public string Password { get; set; }
    }

    internal class QueryHolder
    {
        public string Name { get; set; }
        public string Query { get; set; }
    }

    internal class Config
    {
        public string SentryDsn { get; set; }
        public Credentials Credentials { get; set; }

        public string TargetFolder { get; set; }
        public List<QueryHolder> Queries { get; set; }
    }

    internal class ConfigValidator : AbstractValidator<Config>
    {
        public ConfigValidator()
        {
            RuleFor(c => c.TargetFolder)
                .NotEmpty().NotNull();

            RuleFor(c => c.Credentials)
                .NotEmpty().NotNull();
            RuleFor(c => c.Credentials.User)
                .NotEmpty().NotNull();
            RuleFor(c => c.Credentials.Password)
                .NotEmpty().NotNull();

            RuleFor(c => c.Queries)
                .NotEmpty().NotNull();
        }
    }

    internal static class ConfigLoader
    {
        public static Config Load()
        {
            var configStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Beefusal.config.yml");

            var configText = new StreamReader(configStream).ReadToEnd();

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize<Config>(configText);

            var validationResult = new ConfigValidator().Validate(config);

            if (!validationResult.IsValid)
            {
                var message = new StringBuilder();
                foreach (var error in validationResult.Errors)
                    message.AppendLine(error.ToString());
                throw new System.Exception(message.ToString());
            }

            return config;
        }
    }
}
