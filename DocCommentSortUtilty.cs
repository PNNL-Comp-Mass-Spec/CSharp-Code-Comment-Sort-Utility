using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PRISM;

namespace CSharpDocCommentSortUtility
{
    internal class DocCommentSortUtility : EventNotifier
    {
        private readonly Regex mArgumentsMatcher = new("^[ \t]*///[ \t]*<param ", RegexOptions.Compiled);

        private readonly Regex mCommentMatcher = new("^[ \t]*//[^/]", RegexOptions.Compiled);

        private readonly Regex mRemarksMatcher = new("^[ \t]*///[ \t]*<remarks>", RegexOptions.Compiled);

        private readonly Regex mReturnsMatcher = new("^[ \t]*///[ \t]*<returns>", RegexOptions.Compiled);

        private readonly Regex mSummaryMatcher = new("^[ \t]*///[ \t]*<summary>", RegexOptions.Compiled);

        private int mCurrentLineNumber;

        public SortUtilityOptions Options { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        public DocCommentSortUtility(SortUtilityOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Compare the contents of two lists of strings
        /// </summary>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <returns>True if the lists have the same strings, otherwise false</returns>
        private static bool ListsMatch(IReadOnlyList<string> list1, IReadOnlyList<string> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            // ReSharper disable once LoopCanBeConvertedToQuery
            for (var i = 0; i < list2.Count; i++)
            {
                if (!list1[i].Equals(list2[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Sort documentation comments in the given C# source code file
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="processingMultipleFiles"></param>
        /// <returns>True if success, false if an error</returns>
        public bool ProcessFile(string inputFilePath, bool processingMultipleFiles = false)
        {
            try
            {
                var inputFile = new FileInfo(inputFilePath);
                if (!inputFile.Exists)
                {
                    OnWarningEvent("File not found: " + inputFile.FullName);
                    return false;
                }

                return SortDocumentationComments(inputFile, processingMultipleFiles);
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error occurred in CodeCommentSortUtility->ProcessFile", ex);
                return false;
            }
        }

        /// <summary>
        /// Process files matching the given file path spec, specified by options.InputFilePath
        /// </summary>
        /// <param name="options"></param>
        /// <returns>true if success, false if an error</returns>
        public bool ProcessFilesWildcard(SortUtilityOptions options)
        {
            var filesToProcess = PathUtils.FindFilesWildcard(options.InputFilePath, options.RecurseDirectories);

            var processingMultipleFiles = filesToProcess.Count > 1;

            var successCount = 0;
            var failureCount = 0;

            foreach (var fileToProcess in filesToProcess)
            {
                if (options.UpdateFiles && !Options.QuietMode)
                {
                    ConsoleMsgUtils.ShowDebug("Processing " + PathUtils.CompactPathString(fileToProcess.FullName, 70));
                    Console.WriteLine();
                }

                var successOneFile = ProcessFile(fileToProcess.FullName, processingMultipleFiles);

                if (successOneFile)
                {
                    successCount++;
                    continue;
                }

                failureCount++;
                ConsoleMsgUtils.ShowWarning("Error processing " + fileToProcess.Name);
            }

            if (successCount == 0 && failureCount == 0)
            {
                ConsoleMsgUtils.ShowWarning("No files were found with file spec " + options.InputFilePath);
                return false;
            }

            return failureCount == 0;
        }

        /// <summary>
        /// Process a documentation comment block
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="firstLine"></param>
        /// <param name="inputFileStartLine"></param>
        /// <param name="fileContents"></param>
        /// <param name="nextLine"></param>
        /// <returns>True if the documentation comment block was updated, otherwise false</returns>
        private bool ProcessDocumentationBlock(
            StreamReader reader,
            string firstLine,
            int inputFileStartLine,
            List<string> fileContents,
            out string nextLine)
        {
            var summaryLines = new List<string>();
            var remarksLines = new List<string>();
            var argumentLines = new List<string>();
            var returnLines = new List<string>();

            var currentSection = summaryLines;

            var dataLine = firstLine;

            var originalComments = new List<string>
            {
                dataLine ?? string.Empty
            };

            nextLine = null;
            var finalLine = string.Empty;

            while (true)
            {
                if (string.IsNullOrEmpty(dataLine))
                {
                    currentSection.Add(string.Empty);
                }
                else if (mCommentMatcher.IsMatch(dataLine))
                {
                    currentSection.Add(dataLine);
                }
                else if (mSummaryMatcher.IsMatch(dataLine))
                {
                    summaryLines.Add(dataLine);
                    currentSection = summaryLines;
                }
                else if (mRemarksMatcher.IsMatch(dataLine))
                {
                    if ((Options.RemoveEmptyRemarks || Options.RemoveEmptyBlocks) && dataLine.Trim().Equals("/// <remarks></remarks>"))
                    {
                        // Skip this line
                    }
                    else
                    {
                        remarksLines.Add(dataLine);
                        currentSection = remarksLines;
                    }
                }
                else if (mArgumentsMatcher.IsMatch(dataLine))
                {
                    argumentLines.Add(dataLine);
                    currentSection = argumentLines;
                }
                else if (mReturnsMatcher.IsMatch(dataLine))
                {
                    if ((Options.RemoveEmptyReturns || Options.RemoveEmptyBlocks) && dataLine.Trim().Equals("/// <returns></returns>"))
                    {
                        // Skip this line
                    }
                    else
                    {
                        returnLines.Add(dataLine);
                        currentSection = returnLines;
                    }
                }
                else if (dataLine.TrimStart().StartsWith("///"))
                {
                    currentSection.Add(dataLine);
                }
                else
                {
                    // No longer in the documentation comment block
                    nextLine = dataLine;

                    if (originalComments.Count > 0)
                        originalComments.RemoveAt(originalComments.Count - 1);
                    else
                        OnDebugEvent("originalComments is empty; this is unexpected");

                    break;
                }

                if (!string.IsNullOrWhiteSpace(dataLine))
                {
                    finalLine = dataLine;
                }

                if (reader.EndOfStream)
                    break;

                dataLine = reader.ReadLine();
                mCurrentLineNumber++;

                originalComments.Add(dataLine ?? string.Empty);
            }

            string resharperDisableLine;

            if (finalLine.TrimStart().StartsWith("// ReSharper disable once Unused", StringComparison.OrdinalIgnoreCase) &&
                currentSection.Count > 0 &&
                currentSection[currentSection.Count - 1].Equals(finalLine))
            {
                currentSection.RemoveAt(currentSection.Count - 1);
                resharperDisableLine = finalLine;
            }
            else
            {
                resharperDisableLine = string.Empty;
            }

            var updatedComments = summaryLines.ToList();
            updatedComments.AddRange(remarksLines);
            updatedComments.AddRange(argumentLines);
            updatedComments.AddRange(returnLines);

            if (resharperDisableLine.Length > 0)
            {
                updatedComments.Add(resharperDisableLine);
            }

            var commentBlockUpdated = !ListsMatch(originalComments, updatedComments);

            fileContents.AddRange(updatedComments);

            if (!commentBlockUpdated)
                return false;

            if (Options.QuietMode || !Options.VerboseMode)
                return true;

            Console.WriteLine();
            OnStatusEvent(string.Format("Line {0}:", inputFileStartLine));

            foreach (var item in updatedComments)
            {
                OnStatusEvent(item);
            }

            return true;
        }

        private bool SortDocumentationComments(FileInfo inputFile, bool processingMultipleFiles = false)
        {
            try
            {
                if (processingMultipleFiles && !Options.QuietMode)
                {
                    Console.WriteLine();
                    OnStatusEvent("Processing  " + inputFile.FullName);
                }

                var outputFilePath = inputFile.FullName + "_sorted.tmp";

                var outputFile = new FileInfo(outputFilePath);

                var sectionsUpdated = 0;
                mCurrentLineNumber = 0;

                var fileContents = new List<string>();

                using (var reader = new StreamReader(new FileStream(inputFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        mCurrentLineNumber++;

                        if (string.IsNullOrEmpty(dataLine))
                        {
                            fileContents.Add(string.Empty);
                            continue;
                        }

                        if (!dataLine.TrimStart().StartsWith("///"))
                        {
                            fileContents.Add(dataLine);
                            continue;
                        }

                        var sectionUpdated = ProcessDocumentationBlock(reader, dataLine, mCurrentLineNumber, fileContents, out var nextLine);

                        if (sectionUpdated)
                            sectionsUpdated++;

                        if (nextLine != null)
                            fileContents.Add(nextLine);
                    }
                }

                if (!Options.QuietMode)
                {
                    Console.WriteLine();
                }

                if (!Options.UpdateFiles)
                {
                    if (sectionsUpdated > 0 || !Options.QuietMode && Options.VerboseMode)
                    {
                        OnStatusEvent(string.Format("  Would update {0} section{1} in file {2}",
                            sectionsUpdated,
                            sectionsUpdated == 1 ? string.Empty : "s",
                            PathUtils.CompactPathString(inputFile.FullName, 110)));
                    }

                    return true;
                }

                if (sectionsUpdated == 0)
                {
                    if (!Options.QuietMode)
                    {
                        OnStatusEvent("  Code comments are already properly sorted");
                    }

                    return true;
                }

                // Write as UTF-8, including adding a byte order mark
                using (var writer = new StreamWriter(new FileStream(outputFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(true)))
                {
                    foreach (var item in fileContents)
                    {
                        writer.WriteLine(item);
                    }
                }

                // Backup the input file

                string backupFilePath;
                var backupAddon = 0;

                while (true)
                {
                    backupFilePath = string.Format(inputFile.FullName + ".bak{0}", backupAddon == 0 ? string.Empty : backupAddon);

                    if (!File.Exists(backupFilePath))
                        break;

                    backupAddon++;
                }

                var originalFilePath = inputFile.FullName;

                inputFile.MoveTo(backupFilePath);

                outputFile.MoveTo(originalFilePath);

                OnStatusEvent(string.Format("  Updated {0} sections in file {1}", sectionsUpdated, PathUtils.CompactPathString(originalFilePath, 110)));

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error occurred in CodeCommentSortUtility->SortDocumentationComments", ex);
                return false;
            }
        }

        /// <summary>
        /// Process the file (or file spec) defined in Options.InputFilePath
        /// </summary>
        /// <returns>True if success, false if an error</returns>
        public bool StartProcessing()
        {
            if (Options.PathHasWildcard(Options.InputFilePath))
                return ProcessFilesWildcard(Options);

            return ProcessFile(Options.InputFilePath);
        }
    }
}
