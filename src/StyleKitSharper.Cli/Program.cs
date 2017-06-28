using Microsoft.Extensions.CommandLineUtils;
using System;

namespace StyleKitSharper.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Hello World!");

            var app = new CommandLineApplication();
            app.Name = "sks";
            app.Description = "PaintCode StyleKit transpiler from Java to C# for Xamarin.Android";

            var sourceArg = app.Argument("[source]", "java file");
            var targetArg = app.Argument("[target]", "cs file");
            var namespaceOpt = app.Option("-n|--namespace", "override target namespace", CommandOptionType.SingleValue);

            app.HelpOption("-?|-h|--help");

            app.OnExecute(() =>
            {
                Console.WriteLine($"Source: {sourceArg.Value}");
                Console.WriteLine($"Target: {targetArg.Value}");
                Console.WriteLine($"NS: {namespaceOpt.Value()}");

                return 0;
            });

            app.Execute(args);
        }
    }
}