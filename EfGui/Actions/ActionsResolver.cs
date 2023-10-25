﻿using CliWrap;
using CliWrap.Buffered;
using System;
using System.Threading.Tasks;

namespace EfGui.Actions;

public static class ActionsResolver
{
    public static string exampleProjectPath = "E:\\Projects\\ExampleProject\\ExampleProject";
    public static string appContextName = "AppContext";

    public static async Task RunDotnetEfTool(Logger logger, ActionOptions options)
    {
        try
        {
            Console.WriteLine($"Started operation: {options.ActionName}");

            Console.WriteLine("Running dotnet-ef command...");
            await Cli.Wrap("dotnet-ef")
                .WithArguments(args =>
                {
                    args.Add(options.DotnetEfArgs);
                    args.Add(new[] { "--context", appContextName });
                    args.Add(new[] { "--project", exampleProjectPath });
                })
                .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
                {
                    logger.LogInfo(line);
                }))
                .ExecuteBufferedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}

public class ActionOptions
{
    public string ActionName { get; set; }
    public string[] DotnetEfArgs { get; set; }
}