# CSharp Code Comment Sort Utility

This program sorts documentation comment blocks in C# source code files.
Updated files will have documentation comments in the order 
Summary, Remarks, Parameters, Returns, and Exceptions. The program
also looks for non-standard element names and formatting in documentation comments.

For example:

```
        /// <summary>
        /// Method Description
        /// </summary>
        /// <remarks>
        /// Additional omments
        /// </remarks>
        /// <typeparam name="T">File format type</typeparam>
        /// <param name="inputFilePath">Input file</param>
        /// <param name="outputFilePath">Output file</param>
        /// <returns>True if successful, false if an error</returns>
        /// <exception cref="NotImplementedException"></exception>
```

Output files will have UTF-8 encoding, with a byte order mark.

## Console Switches

The C# Code Comment Sort Utility is a console application, and must be run from the Windows or Linux command prompt.

```
CSharpDocCommentSortUtility.exe 
  /I:InputFilePath [/S]
  [/Empty:False] [/REM] [/RET] [/REV]
  [/FixInvalid:False]
  [/Write] 
  [/Verbose] [/Quiet]
```

InputFilePath is a path to the C# source code file to process
* Wildcards are also supported, for example *.cs
* On Windows, `/I` is optional, for example:
  * `CSharpDocCommentSortUtility.exe *.cs`
  * `CSharpDocCommentSortUtility.exe Program\*.cs /S`
* On Linux, use of `/I` or `-I` is required; examples: 
  * `mono CSharpDocCommentSortUtility.exe /I:*.cs` 
  * `mono CSharpDocCommentSortUtility.exe /I:"*.cs"` 
  * `mono CSharpDocCommentSortUtility.exe -I:"*.cs" -S`

Use `/S` or `-S` or `--S` or `/Recurse` to find matching files in the current directory and subdirectories
* Useful when using a wildcard to find cs files

By default, `/Empty:True` is defined, meaning empty remarks blocks and empty returns blocks are auto-removed
* Removes: `<remarks></remarks>` and `<returns></returns>`
* When enabled, `/REM` and `/RET` and `/REV` are implicitly enabled
  * To disable this, use `/Empty:False` or `/RemoveEmpty:False`

When `/Empty:false` has been used, optionally use `/REM` to remove empty remarks blocks
* Remove: `<remarks></remarks>`

When `/Empty:false` has been used, optionally use `/RET` to remove empty returns blocks
* Remove: `<returns></returns>`

When `/Empty:false` has been used, optionally use `/REV` to remove empty value blocks
* Remove: `<value></value>`

By default, invalid remark and return elements are renamed
* Changes `<return>True if successful, false if an error</return>` to `<returns>True if successful, false if an error</returns>`
* Changes `<remark>Used by the shape class</remark>` to `<remarks>Used by the shape class</remarks>`
* Additionally, comments that have `/// ///` will be changed to have `///`
* To disable this, use `/FixInvalid:False` or `/RenameInvalid:False`

By default, this program previews changes that would be made
* Use `/Save` or `/Update` or `/Write` to replace files with updated versions
* The original file will be rename to end with `.bak`
  * If an existing `.bak` file already exists, the original file will be renamed to `.bak1` or `.bak2`, etc., thus avoiding overwriting existing files

By default, this program shows the comment blocks that would be updated
* Use `/Verbose:False` to disable this

Use `/Q` or `/Quiet` to reduce the number of messages shown at the console
* This is useful if processing a large number of files
* When both `/Q` and `/Verbose:False` are used, status messages will show the files that would be updated (or were updated)
* When Both `/Q` and `/Verbose:True` are used, updated documentation comment blocks will also be shown

The processing options can be specified in a parameter file using `/ParamFile:Options.conf` or `/Conf:Options.conf`
* Define options using the format `ArgumentName=Value`
* Lines starting with `#` or `;` will be treated as comments
* Additional arguments on the command line can supplement or override the arguments in the parameter file

Use `/CreateParamFile` to create an example parameter file
* By default, the example parameter file content is shown at the console
* To create a file named Options.conf, use `/CreateParamFile:Options.conf`

## Contacts

Written by Matthew Monroe for PNNL (Richland, WA) \
E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov \
Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics

## License

This program is licensed under the Apache License, Version 2.0; you may not use this 
file except in compliance with the License. You may obtain a copy of the 
License at https://opensource.org/licenses/Apache-2.0
