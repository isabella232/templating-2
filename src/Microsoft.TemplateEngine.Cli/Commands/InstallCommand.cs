// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.Extensions;
using Microsoft.TemplateEngine.Cli.HelpAndUsage;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli.Commands
{
    internal class CommandType
    {
        public static bool IsNew(Type type) => type == typeof(New);

        public static bool IsLegacy(Type type) => type == typeof(Legacy);
    }

    internal class Legacy : CommandType
    {
    }

    internal class New : CommandType
    {
    }

    internal class InstallCommand<T> : BaseCommand<InstallCommandArgs<T>> where T : CommandType
    {
        internal InstallCommand(ITemplateEngineHost host, ITelemetryLogger logger, NewCommandCallbacks callbacks)
            : base(host, logger, callbacks, CommandType.IsNew(typeof(T)) ? "install" : "--install")
        {
            if (CommandType.IsLegacy(typeof(T)))
            {
                IsHidden = true;
                AddAlias("-i");
            }
        }

        protected override Task<NewCommandStatus> ExecuteAsync(InstallCommandArgs<T> args, IEngineEnvironmentSettings environmentSettings, InvocationContext context)
        {
            if (CommandType.IsNew(typeof(T)))
            {
                if (InstallCommandArgs<Legacy>.IsAnyOptionUsed(args.ParseResult))
                {
                    return Task.FromResult(NewCommandStatus.InvalidParamValues);
                }
            }

            using TemplatePackageManager templatePackageManager = new TemplatePackageManager(environmentSettings);
            TemplateInformationCoordinator templateInformationCoordinator = new TemplateInformationCoordinator(
                environmentSettings,
                templatePackageManager,
                new TemplateCreator(environmentSettings),
                new HostSpecificDataLoader(environmentSettings),
                TelemetryLogger,
                environmentSettings.GetDefaultLanguage());

            TemplatePackageCoordinator templatePackageCoordinator = new TemplatePackageCoordinator(
                TelemetryLogger,
                environmentSettings,
                templatePackageManager,
                templateInformationCoordinator,
                environmentSettings.GetDefaultLanguage());

            return templatePackageCoordinator.EnterInstallFlowAsync(args, context.GetCancellationToken());
        }

        protected override InstallCommandArgs<T> ParseContext(ParseResult parseResult)
        {
            return new InstallCommandArgs<T>(parseResult);
        }
    }

    internal class InstallCommandArgs<T> : GlobalArgs
    {
        public InstallCommandArgs(ParseResult parseResult) : base(parseResult)
        {
            TemplatePackages = parseResult.ValueForArgument(NameArgument) ?? throw new Exception("This shouldn't happen, we set ArgumentArity(1)...");
            Interactive = parseResult.ValueForOption(InteractiveOption);
            AdditionalSources = parseResult.ValueForOption(AddSourceOption);
        }

        public IReadOnlyList<string> TemplatePackages { get; }

        public bool Interactive { get; }

        public IReadOnlyList<string>? AdditionalSources { get; }

        private static Argument<IReadOnlyList<string>> NameArgument { get; } = new("name")
        {
            Description = "Name of NuGet package or folder.",
            Arity = new ArgumentArity(1, 99)
        };

        private static Option<bool> InteractiveOption { get; } = new("--interactive")
        {
            Description = "When downloading enable NuGet interactive."
        };

        private static Option<IReadOnlyList<string>> AddSourceOption { get; } = new(new[] { "--add-source" })
        {
            Description = "Add NuGet source when looking for package.",
            AllowMultipleArgumentsPerToken = true,
        };

        internal static void AddToCommand(Command command, bool legacy = false)
        {
            command.AddArgument(NameArgument);
            command.AddOption(InteractiveOption);
            command.AddOption(AddSourceOption);
        }

        internal static bool IsAnyOptionUsed(ParseResult parseResult)
        {
            if (parseResult.HasOption(InteractiveOption))
            {
                Reporter.Error.WriteLine($"{AddSourceOption.Name} used in wrong position.");
                return true;
            }
            if (parseResult.HasOption(AddSourceOption))
            {
                Reporter.Error.WriteLine($"{AddSourceOption.Name} used in wrong position.");
                return true;
            }

            return false;
        }
    }
}
