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
        // Ignore Spelling: inheritdoc, paramref, typeparam, typeparamref

        private readonly Regex mElementMatcher = new("^[ \t]*///[ \t]*<(?<ElementName>[^ />]+)", RegexOptions.Compiled);

        private readonly Regex mCommentMatcher = new("^[ \t]*//[^/]", RegexOptions.Compiled);

        public SortUtilityOptions Options { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        public DocCommentSortUtility(SortUtilityOptions options)
        {
            Options = options;
        }

        private void HandleElement(
            RuntimeData runtimeData,
            string elementName,
            string expectedElementName,
            string elementNameSynonym,
            List<string> targetSection,
            ref string dataLine,
            ref List<string> currentSection)
        {
            var emptyComment = string.Format("/// <{0}></{0}>", elementName);

            if (elementName.Equals(elementNameSynonym))
            {
                if (!runtimeData.InvalidElementWarned)
                {
                    OnWarningEvent("Invalid element in file " + PathUtils.CompactPathString(runtimeData.InputFilePath, 120));
                    runtimeData.InvalidElementWarned = true;
                }

                OnWarningEvent(string.Format(
                    "Line {0} has <{1}>; it should instead have <{2}>; auto-updating",
                    runtimeData.CurrentLineNumber, elementNameSynonym, expectedElementName));

                dataLine = dataLine.Replace(
                    string.Format("<{0}>", elementNameSynonym),
                    string.Format("<{0}>", expectedElementName));

                dataLine = dataLine.Replace(
                    string.Format("</{0}>", elementNameSynonym),
                    string.Format("</{0}>", expectedElementName));
            }

            if ((Options.RemoveEmptyReturns || Options.RemoveEmptyBlocks) && dataLine.Trim().Equals(emptyComment))
            {
                // Skip this line
            }
            else
            {
                targetSection.Add(dataLine);
                currentSection = targetSection;
            }
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
        /// <param name="runtimeData"></param>
        /// <returns>True if the documentation comment block was updated, otherwise false</returns>
        private bool ProcessDocumentationBlock(RuntimeData runtimeData)
        {
            var summaryLines = new List<string>();
            var remarksLines = new List<string>();
            var argumentLines = new List<string>();
            var returnLines = new List<string>();

            var currentSection = summaryLines;

            var dataLine = runtimeData.CommentBlockFirstLine;

            var originalComments = new List<string>
            {
                dataLine ?? string.Empty
            };

            runtimeData.NextLine = null;
            var finalLine = string.Empty;

            while (true)
            {
                var elementMatch = mElementMatcher.Match(dataLine ?? string.Empty);

                if (string.IsNullOrEmpty(dataLine))
                {
                    currentSection.Add(string.Empty);
                }
                else if (mCommentMatcher.IsMatch(dataLine))
                {
                    currentSection.Add(dataLine);
                }
                else if (elementMatch.Success)
                {
                    var elementName = elementMatch.Groups["ElementName"].Value;

                    switch (elementName)
                    {
                        case "summary":
                            summaryLines.Add(dataLine);
                            currentSection = summaryLines;
                            break;

                        case "param":
                        case "paramref":
                            argumentLines.Add(dataLine);
                            currentSection = argumentLines;
                            break;

                        case "example":
                        case "inheritdoc":
                        case "para":
                        case "see":
                            currentSection.Add(dataLine);
                            break;

                        case "remarks":
                        case "remark":
                            HandleElement(
                                runtimeData, elementName,
                                "remarks", "remark",
                                remarksLines, ref dataLine, ref currentSection);

                            break;

                        case "returns":
                        case "return":
                            HandleElement(
                                runtimeData, elementName,
                                "returns", "return",
                                returnLines, ref dataLine, ref currentSection);

                            break;

                        default:
                            if (!runtimeData.UnrecognizedElementWarned)
                            {
                                OnWarningEvent("Unrecognized element in file " + PathUtils.CompactPathString(runtimeData.InputFilePath, 120));
                                runtimeData.UnrecognizedElementWarned = true;
                            }

                            OnWarningEvent(string.Format("Unrecognized element name on line {0}: {1}", runtimeData.CurrentLineNumber, elementName));

                            currentSection.Add(dataLine);
                            break;
                    }
                }
                else if (dataLine.TrimStart().StartsWith("///"))
                {
                    currentSection.Add(dataLine);
                }
                else
                {
                    // No longer in the documentation comment block
                    runtimeData.NextLine = dataLine;

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

                if (runtimeData.Reader.EndOfStream)
                    break;

                dataLine = runtimeData.Reader.ReadLine();
                runtimeData.CurrentLineNumber++;

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

            runtimeData.FileContents.AddRange(updatedComments);

            if (!commentBlockUpdated)
                return false;

            if (Options.QuietMode || !Options.VerboseMode)
                return true;

            Console.WriteLine();
            OnStatusEvent(string.Format("Line {0}:", runtimeData.CommentBlockStartLineNumber));

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

                var fileContents = new List<string>();

                using (var reader = new StreamReader(new FileStream(inputFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    var runtimeData = new RuntimeData(inputFile.FullName, reader, fileContents);

                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        runtimeData.CurrentLineNumber++;

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

                        runtimeData.CommentBlockFirstLine = dataLine;
                        runtimeData.CommentBlockStartLineNumber = runtimeData.CurrentLineNumber;

                        var sectionUpdated = ProcessDocumentationBlock(runtimeData);

                        if (sectionUpdated)
                            sectionsUpdated++;

                        if (runtimeData.NextLine != null)
                            fileContents.Add(runtimeData.NextLine);
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
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (Options.PathHasWildcard(Options.InputFilePath))
                return ProcessFilesWildcard(Options);

            return ProcessFile(Options.InputFilePath);
        }
    }
}
