using System.Collections.Generic;
using System.IO;

namespace CSharpDocCommentSortUtility
{
    internal class RuntimeData
    {
        public string CommentBlockFirstLine { get; set; }

        public int CommentBlockStartLineNumber { get; set; }

        public int CurrentLineNumber { get; set; }

        public List<string> FileContents { get; }

        public string InputFilePath { get; }

        public bool InvalidElementWarned { get; set; }

        public string NextLine { get; set; }

        public StreamReader Reader { get; }

        public bool UnrecognizedElementWarned { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="reader"></param>
        /// <param name="fileContents"></param>
        public RuntimeData( string inputFilePath, StreamReader reader, List<string> fileContents)
        {
            Reader = reader;
            InputFilePath = inputFilePath;

            CommentBlockFirstLine = string.Empty;
            CommentBlockStartLineNumber = 0;
            CurrentLineNumber = 0;
            FileContents = fileContents;
            InvalidElementWarned = false;
            NextLine = null;
            UnrecognizedElementWarned = false;
        }
    }
}
