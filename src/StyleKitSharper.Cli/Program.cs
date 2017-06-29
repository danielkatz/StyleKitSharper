using Microsoft.Extensions.CommandLineUtils;
using StyleKitSharper.Core;
using System;
using System.IO;

namespace StyleKitSharper.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new CommandLineApplication(false);
            app.Name = "sks";
            app.FullName = "StyleKitSharper";

            var sourceArg = app.Argument("source", "The path to the source Java StyleKit file.");
            var targetArg = app.Argument("target", "The path to the output C# file.");
            var namespaceOpt = app.Option("-n|--namespace", "Set the namespace for the C# file.", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                if (sourceArg.Value != null && targetArg.Value != null)
                {
                    Console.WriteLine(app.FullName);
                    Console.WriteLine();
                    Console.WriteLine($"Processing: '{sourceArg.Value}' => '{targetArg.Value}'...");

                    string javaCode = null;
                    string csharpCode = null;
                    var transpiler = new StyleKitTranspiler
                    {
                        Namespace = namespaceOpt.Value()
                    };

                    try
                    {
                        javaCode = File.ReadAllText(sourceArg.Value);
                        csharpCode = transpiler.Transpile(javaCode);
                        File.WriteAllText(targetArg.Value, csharpCode);
                    }
                    catch (IOException ex) when (javaCode == null)
                    {
                        app.Error.WriteLine(ex.Message);
                        return 1;
                    }
                    catch (IOException ex) when (javaCode != null)
                    {
                        app.Error.WriteLine(ex.Message);
                        return 2;
                    }
                    catch (Exception ex)
                    {
                        app.Error.WriteLine($"Unexpected error while transpiling '{sourceArg.Value}'.");
                        app.Error.WriteLine($"Message: {ex.Message}");
                        return 3;
                    }

                    Console.WriteLine("Done.");
                }
                else
                {
                    app.ShowHelp();
                }

                return 0;
            });

            app.HelpOption("-?|-h|--help");
            app.ExtendedHelpText = "\nhttps://github.com/danielkatz/StyleKitSharper";

            app.Execute(args);

#if DEBUG
            Console.ReadKey();
#endif
        }
    }
}