// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using Dotnet_new3;

namespace Microsoft.DotNet.Cli
{
    internal class CompleteCommand
    {
        public static int Run(string[] args)
        {
            try
            {
                var suggestions = Suggestions(CompleteCommandParser.GetCommand().Parse(args), args);

                foreach (var suggestion in suggestions)
                {
                    Console.WriteLine(suggestion);
                }
            }
            catch (Exception)
            {
                return 1;
            }

            return 0;
        }

        private static string[] Suggestions(ParseResult complete, string[] args)
        {
            var input = complete.ValueForArgument(CompleteCommandParser.PathArgument) ?? string.Empty;

            var position = complete.ValueForOption(CompleteCommandParser.PositionOption);

            if (position > input.Length)
            {
                input += " ";
            }

            var result = Program.CreateNewCommand(args[1].Split(" ")).Parse(input);

            return result.GetSuggestions(position)
                .Distinct()
                .ToArray();
        }
    }
}
