using System;
using System.IO;
using PRISM;
using PRISM.Logging;

namespace CSharpDocCommentSortUtility
{
    /// <summary>
    /// This program sorts documentation comment blocks in C# source code files
    /// </summary>
    /// <remarks>
    /// <para>
    /// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)</para>
    /// <para>
    /// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
    /// Website: https://panomics.pnnl.gov/ or https://omics.pnl.gov
    /// </para>
    /// </remarks>
    internal static class Program
    {
        // Ignore Spelling: Conf

        internal static int Main(string[] args)
        {
            var programName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
            var exePath = PRISM.FileProcessor.ProcessFilesOrDirectoriesBase.GetAppPath();
            var exeName = Path.GetFileName(exePath);
            var cmdLineParser = new CommandLineParser<SortUtilityOptions>(programName, SortUtilityOptions.GetAppVersion())
            {
                ProgramInfo = ConsoleMsgUtils.WrapParagraph("This program sorts documentation comment blocks in C# source code files"),
                ContactInfo = "Program written by Matthew Monroe for PNNL (Richland, WA) in 2021" + Environment.NewLine +
                                             "E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov" + Environment.NewLine +
                                             "Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/"
            };

            cmdLineParser.UsageExamples.Add(exeName + " CodeFile.cs");
            cmdLineParser.UsageExamples.Add(exeName + " CodeFile.cs /write");
            cmdLineParser.UsageExamples.Add(exeName + " *.cs /s");
            cmdLineParser.UsageExamples.Add(exeName + " *.cs /s /quiet");
            cmdLineParser.UsageExamples.Add(exeName + " *.cs /s /quiet /verbose:false");
            cmdLineParser.UsageExamples.Add(exeName + @" /I:..\Program\*.cs /s /quiet /write");

            // The default argument name for parameter files is /ParamFile or -ParamFile
            // Also allow /Conf or /P
            cmdLineParser.AddParamFileKey("Conf");
            cmdLineParser.AddParamFileKey("P");

            var result = cmdLineParser.ParseArgs(args);
            var options = result.ParsedResults;
            if (!result.Success || !options.Validate())
            {
                // Delay for 750 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
                System.Threading.Thread.Sleep(750);
                return -1;
            }

            options.OutputSetOptions();

            try
            {
                var processor = new DocCommentSortUtility(options);
                RegisterEvents(processor);

                var success = processor.StartProcessing();

                if (success)
                    return 0;

                return -1;
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Error occurred in Program->Main", ex);
                return -1;
            }
        }

        /// <summary>
        /// Use this method to chain events between classes
        /// </summary>
        /// <param name="sourceClass"></param>
        private static void RegisterEvents(IEventNotifier sourceClass)
        {
            // Ignore: sourceClass.DebugEvent += OnDebugEvent;
            sourceClass.StatusEvent += OnStatusEvent;
            sourceClass.ErrorEvent += OnErrorEvent;
            sourceClass.WarningEvent += OnWarningEvent;
            // Ignore: sourceClass.ProgressUpdate += OnProgressUpdate;
        }

        private static void OnErrorEvent(string message, Exception ex)
        {
            ConsoleMsgUtils.ShowError(message, ex);
        }

        private static void OnStatusEvent(string message)
        {
            Console.WriteLine(message);
        }

        private static void OnWarningEvent(string message)
        {
            ConsoleMsgUtils.ShowWarning(message);
        }
    }
}
