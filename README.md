# CSharp Code Comment Sort Utility

This program sorts documentation comment blocks in C# source code files.
Updated files will have comments in the order Summary, Remarks, Parameters, and Returns.

For example:

```
        /// <summary>
        /// Method Description
        /// </summary>
        /// <remarks>
        /// Additional omments
        /// </remarks>
        /// <param name="inputFilePath">Input file</param>
        /// <param name="outputFilePath">Output file</param>
        /// <returns>True if successful, false if an error</returns>
```

Output files will have UTF-8 encoding, with a byte order mark.

## Console Switches

The C# Code Comment Sort Utility is a console application, and must be run from the Windows or Linux command prompt.

```
CSharpDocCommentSortUtility.exe 
  InputFilePath [/S] 
  [/RemoveEmpty] [/REM] [/RET] 
  [/Write] 
  [/Verbose] [/Quiet]
```

InputFilePath is a path to the C# source code file to process
* Wildcards are also supported, for example *.cs

Use `/S` or `/Recurse` to find matching files in the current directory and subdirectories
* Useful when using a wildcard to find cs files

Use `/Empty` or `/RemoveEmpty` empty remarks blocks and empty returns blocks
* i.e. `<remarks></remarks>` and `<returns></returns>`
* If this is enabled, `/REM` and `/RET` are implicitly enabled

Use `/REM` to remove empty remarks blocks
* i.e. `<remarks></remarks>`

Use `/RET` to remove empty returns blocks
* i.e. `<returns></returns>`

By default, this program previews changes that would be made
* Use `/Save` or `/Write` or `/Update` to replace files with updated versions

By default, this program shows the comment blocks that would be updated
* Use `/Verbose:False` to disable this

Use `/Q` or `/Quiet` to show a minimal amount of messages at the console
* This is useful if processing a large number of files
* When `/Q` is enabled, `/Verbose` is ignored

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
Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/

## License

This program is licensed under the Apache License, Version 2.0; you may not use this 
file except in compliance with the License.  You may obtain a copy of the 
License at https://opensource.org/licenses/Apache-2.0
