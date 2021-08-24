using System;
using System.Reflection;
using PRISM;

namespace CSharpDocCommentSortUtility
{
    internal class SortUtilityOptions
    {
        // Ignore Spelling: wildcards

        /// <summary>
        /// Program date
        /// </summary>
        public const string PROGRAM_DATE = "August 24, 2021";

        /// <summary>
        /// Input file path
        /// </summary>
        /// <remarks>.xml file</remarks>
        [Option("InputFilePath", "I",
            ArgPosition = 1, Required = true, HelpShowsDefault = false,
            HelpText = "The name of the input file to process; may contain wildcards. " +
                       "When using /I at the command line, surround the filename with double quotes if it contains spaces")]
        public string InputFilePath { get; set; }

        [Option("Recurse", "S",
            HelpShowsDefault = false,
            HelpText = "If true, search for files in subdirectories")]
        public bool RecurseDirectories { get; set; }

        [Option("RemoveEmpty", "Empty",
            HelpShowsDefault = true,
            HelpText = "If true, remove empty remarks blocks and empty returns blocks, i.e. <remarks></remarks> and <returns></returns>\n" +
                       "If this is enabled, RemoveEmptyRemarks and RemoveEmptyReturns are implicitly enabled")]
        public bool RemoveEmptyBlocks { get; set; } = true;

        [Option("RemoveEmptyRemarks", "REM",
            HelpShowsDefault = false,
            HelpText = "If true, remove empty remarks blocks, i.e. <remarks></remarks>")]
        public bool RemoveEmptyRemarks { get; set; }

        [Option("RemoveEmptyReturns", "RET",
            HelpShowsDefault = false,
            HelpText = "If true, remove empty returns blocks, i.e. <returns></returns>")]
        public bool RemoveEmptyReturns { get; set; }

        [Option("RenameInvalidElements", "FixInvalid", "RenameInvalid",
            HelpShowsDefault = true,
            HelpText = "If true, rename invalid elements, changing from <return></return> to <returns></returns>, " +
                       "and from <remark></remark> to <remarks></remarks>")]
        public bool RenameInvalidElements { get; set; } = true;

        [Option("Save", "Update", "Write",
            HelpShowsDefault = false,
            HelpText = "By default, this program previews changes that would be made. Set this to true (or use /Save or /Update) to replace files with updated versions")]
        public bool UpdateFiles { get; set; }

        [Option("Verbose",
            HelpShowsDefault = true,
            HelpText = "When true, show updated documentation comment blocks")]
        public bool VerboseMode { get; set; } = true;

        [Option("Quiet", "Q",
            HelpShowsDefault = false,
            HelpText = "If true, only show messages if a file is updated (or would be updated)\n" +
                       "If this is enabled, VerboseMode is ignored ")]
        public bool QuietMode { get; set; }

        /// <summary>
        /// Return Enabled if value is true
        /// Return Disabled if value is false
        /// </summary>
        /// <param name="value"></param>
        private static string BoolToEnabledDisabled(bool value)
        {
            return value ? "Enabled" : "Disabled";
        }

        /// <summary>
        /// Get the program version
        /// </summary>
        public static string GetAppVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";

            return version;
        }

        /// <summary>
        /// True if the path has a * or ?
        /// </summary>
        /// <param name="filePath"></param>
        public bool PathHasWildcard(string filePath)
        {
            return filePath.Contains("*") || filePath.Contains("?");
        }

        /// <summary>
        /// Show the options at the console
        /// </summary>
        public void OutputSetOptions()
        {
            Console.WriteLine("Options:");

            if (PathHasWildcard(InputFilePath))
            {
                Console.WriteLine(" {0,-29} {1}", "Finding files that match:", InputFilePath);
                Console.WriteLine(" {0,-29} {1}", "Find files in subdirectories:", BoolToEnabledDisabled(RecurseDirectories));
            }
            else
            {
                Console.WriteLine(" {0,-29} {1}", "Processing file:", InputFilePath);

                if (RecurseDirectories)
                {
                    Console.WriteLine(" {0,-29} {1}", "Also searching subdirectories:", BoolToEnabledDisabled(RecurseDirectories));
                }
            }

            Console.WriteLine(" {0,-29} {1}", "Remove empty remarks:", BoolToEnabledDisabled(RemoveEmptyRemarks || RemoveEmptyBlocks));
            Console.WriteLine(" {0,-29} {1}", "Remove empty returns:", BoolToEnabledDisabled(RemoveEmptyReturns || RemoveEmptyBlocks));

            Console.WriteLine(" {0,-29} {1}", "Rename invalid elements:", BoolToEnabledDisabled(RenameInvalidElements));

            Console.WriteLine(" {0,-29} {1}", "Quiet mode:", BoolToEnabledDisabled(QuietMode));
            Console.WriteLine(" {0,-29} {1}", "Verbose mode:", BoolToEnabledDisabled(VerboseMode && !QuietMode));

            Console.WriteLine();

            if (UpdateFiles)
                Console.WriteLine(" Replacing files with updated versions");
            else
                Console.WriteLine(" Previewing files that would be processed");
        }
        /// <summary>
        /// Validate the options
        /// </summary>
        /// <returns>True if all options are valid</returns>
        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(InputFilePath))
            {
                ConsoleMsgUtils.ShowError($"ERROR: Input path must be provided and non-empty; \"{InputFilePath}\" was provided");
                return false;
            }

            return true;
        }
    }
}
