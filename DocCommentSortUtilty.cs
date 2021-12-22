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

        private readonly Regex mCommentMatcher = new("^[ \t]*//[^/]", RegexOptions.Compiled);

        private readonly Regex mCommentedScopeMatcher = new("^[ \t]*//[ \t]*(public|protected|private|internal)[ \t]", RegexOptions.Compiled);

        private readonly Regex mElementMatcher = new("^[ \t]*///[ \t]*<(?<ElementName>[^ />]+)", RegexOptions.Compiled);

        private readonly Regex mInvalidClosingElementMatcher = new("^[ \t]*///.+</(?<ElementName>remark|return)>", RegexOptions.Compiled);

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
            if (!string.IsNullOrWhiteSpace(elementNameSynonym) && elementName.Equals(elementNameSynonym))
            {
                WarnInvalidElementName(runtimeData, elementNameSynonym, expectedElementName);

                if (Options.RenameInvalidElements)
                {
                    dataLine = dataLine.Replace(
                        string.Format("<{0}>", elementNameSynonym),
                        string.Format("<{0}>", expectedElementName));

                    dataLine = dataLine.Replace(
                        string.Format("</{0}>", elementNameSynonym),
                        string.Format("</{0}>", expectedElementName));
                }
            }

            if (IsEmptyBlockToSkip(elementName, dataLine))
            {
                // Skip this line
                return;
            }

            targetSection.Add(dataLine);
            currentSection = targetSection;
        }

        private bool IsEmptyBlockToSkip(string elementName, string dataLine)
        {
            var emptyComment = string.Format("/// <{0}></{0}>", elementName);

            if (!dataLine.Trim().Equals(emptyComment))
                return false;

            switch (elementName)
            {
                case "remarks" or "remark" when (Options.RemoveEmptyBlocks || Options.RemoveEmptyRemarks):
                case "returns" or "return" when (Options.RemoveEmptyBlocks || Options.RemoveEmptyReturns):
                case "value" when (Options.RemoveEmptyBlocks || Options.RemoveEmptyValueTags):
                    return true;

                default:
                    return false;
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
        /// <param name="sectionsUpdated">Output: number of sections that were updated (or would be updated)</param>
        /// <param name="processingMultipleFiles"></param>
        /// <returns>True if success, false if an error</returns>
        public bool ProcessFile(string inputFilePath, out int sectionsUpdated, bool processingMultipleFiles = false)
        {
            sectionsUpdated = 0;

            try
            {
                var inputFile = new FileInfo(inputFilePath);
                if (!inputFile.Exists)
                {
                    OnWarningEvent("File not found: " + inputFile.FullName);
                    return false;
                }

                return SortDocumentationComments(inputFile, out sectionsUpdated, processingMultipleFiles);
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
            var updatedFiles = 0;

            foreach (var fileToProcess in filesToProcess)
            {
                if (options.UpdateFiles && !Options.QuietMode)
                {
                    OnDebugEvent("Processing " + PathUtils.CompactPathString(fileToProcess.FullName, 70));
                    OnStatusEvent(string.Empty);
                }

                var successOneFile = ProcessFile(fileToProcess.FullName, out var sectionsUpdated, processingMultipleFiles);

                if (sectionsUpdated > 0)
                    updatedFiles++;

                if (successOneFile)
                {
                    successCount++;
                    continue;
                }

                failureCount++;
                OnWarningEvent("Error processing " + fileToProcess.Name);
            }

            if (successCount == 0 && failureCount == 0)
            {
                OnWarningEvent("No files were found with file spec " + options.InputFilePath);
                return false;
            }

            var fileCount = successCount + failureCount;

            if (fileCount <= 1)
            {
                return failureCount == 0;
            }

            OnStatusEvent(string.Empty);

            if (updatedFiles == 0)
            {
                OnStatusEvent(string.Format("All {0} files are already up-to-date", fileCount));
            }
            else
            {
                OnStatusEvent(string.Format("{0} {1} / {2} files",
                    options.UpdateFiles ? "Updated" : "Would update",
                    updatedFiles,
                    fileCount));
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
            var typeParamLines = new List<string>();
            var argumentLines = new List<string>();
            var returnLines = new List<string>();
            var exceptionLines = new List<string>();

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
                if (dataLine.TrimStart().StartsWith("/// ///"))
                {
                    WarnInvalidFormat(runtimeData, dataLine);

                    if (Options.RenameInvalidElements)
                    {
                        dataLine = ReplaceFirst(dataLine, "/// ///", "///");
                    }
                }

                var elementMatch = mElementMatcher.Match(dataLine ?? string.Empty);

                if (string.IsNullOrEmpty(dataLine))
                {
                    currentSection.Add(string.Empty);
                }
                else if (mCommentMatcher.IsMatch(dataLine))
                {
                    currentSection.Add(dataLine);

                    // Check for a commented out scope keyword
                    // If found, that implies the current comment is on a commented out method, property, etc.
                    if (mCommentedScopeMatcher.IsMatch(dataLine))
                        break;
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

                        case "typeparam":
                        case "typeparamref":
                            typeParamLines.Add(dataLine);
                            currentSection = typeParamLines;
                            break;

                        case "param":
                        case "paramref":
                            argumentLines.Add(dataLine);
                            currentSection = argumentLines;
                            break;

                        case "value":
                            HandleElement(
                                runtimeData, elementName,
                                "value", string.Empty,
                                argumentLines, ref dataLine, ref currentSection);

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

                        case "exception":
                            exceptionLines.Add(dataLine);
                            currentSection = exceptionLines;
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
                    var invalidClosingElementMatch = mInvalidClosingElementMatcher.Match(dataLine);
                    if (invalidClosingElementMatch.Success)
                    {
                        var currentName = invalidClosingElementMatch.Groups["ElementName"].Value;

                        WarnInvalidElementName(runtimeData, currentName, currentName + "s", true);

                        if (Options.RenameInvalidElements)
                        {
                            dataLine = dataLine.Replace(
                                string.Format("</{0}>", currentName),
                                string.Format("</{0}s>", currentName));
                        }
                    }

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
            updatedComments.AddRange(typeParamLines);
            updatedComments.AddRange(argumentLines);
            updatedComments.AddRange(returnLines);
            updatedComments.AddRange(exceptionLines);

            if (resharperDisableLine.Length > 0)
            {
                updatedComments.Add(resharperDisableLine);
            }

            var commentBlockUpdated = !ListsMatch(originalComments, updatedComments);

            runtimeData.FileContents.AddRange(updatedComments);

            if (!commentBlockUpdated)
                return false;

            if (!Options.VerboseMode)
                return true;

            OnStatusEvent(string.Empty);
            OnStatusEvent(string.Format("Line {0}:", runtimeData.CommentBlockStartLineNumber));

            foreach (var item in updatedComments)
            {
                OnStatusEvent(item);
            }

            return true;
        }

        private string ReplaceFirst(string dataLine, string textToFind, string replacementText)
        {
            var matcher = new Regex(textToFind);
            return matcher.Replace(dataLine, replacementText, 1);
        }

        private bool SortDocumentationComments(FileInfo inputFile, out int sectionsUpdated, bool processingMultipleFiles = false)
        {
            sectionsUpdated = 0;

            try
            {
                if (processingMultipleFiles && !Options.QuietMode)
                {
                    OnStatusEvent(string.Empty);
                    OnStatusEvent("Processing " + inputFile.FullName);
                }

                var outputFilePath = inputFile.FullName + "_sorted.tmp";

                var outputFile = new FileInfo(outputFilePath);

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

                if (sectionsUpdated > 0)
                {
                    OnStatusEvent(string.Empty);
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

            return ProcessFile(Options.InputFilePath, out _);
        }

        private void WarnInvalidElementName(RuntimeData runtimeData, string currentName, string expectedName, bool isClosingTag = false)
        {
            if (!runtimeData.InvalidFormatWarned)
            {
                OnWarningEvent("Invalid element in file " + PathUtils.CompactPathString(runtimeData.InputFilePath, 120));
                runtimeData.InvalidFormatWarned = true;
            }

            OnWarningEvent(string.Format(
                "Line {0} has <{4}{1}>; it should instead have <{4}{2}>{3}",
                runtimeData.CurrentLineNumber, currentName, expectedName,
                Options.RenameInvalidElements ? " -- auto-updating" : " -- leaving as-is",
                isClosingTag ? "/" : string.Empty));
        }

        private void WarnInvalidFormat(RuntimeData runtimeData, string currentLine)
        {
            if (!runtimeData.InvalidFormatWarned)
            {
                OnWarningEvent("Invalid doc comment format in file " + PathUtils.CompactPathString(runtimeData.InputFilePath, 120));
                runtimeData.InvalidFormatWarned = true;
            }

            OnWarningEvent(string.Format(
                "Line {0} has '/// ///'; it should instead be: {1}{2}",
                runtimeData.CurrentLineNumber, ReplaceFirst(currentLine, "/// ///", "///").Trim(),
                Options.RenameInvalidElements ? " -- auto-updating" : " -- leaving as-is"));
        }
    }
}
