using System.Runtime;

namespace GalacticLuxford
{
    public class DriveScanner
    {
        #region Public Methods
        public static int Main( string[] parameters )
        {
            ExitCode exitCode = ExitCode.NotYetInitialized;

            try
            {
                var     commandLineRecognized   = false;
                var     runMode                 = CommandMode.Help;
                string  scanConfigFilename      = DefaultConfigFileName;

                exitCode = ExitCode.Error_CommandLine;
                if (parameters?.Length > 0)
                {
                    commandLineRecognized = Enum.TryParse<CommandMode>(parameters[0], true, out runMode);
                    if (commandLineRecognized)
                    {
                        if (runMode == CommandMode.Scan)
                        {
                            if (parameters.Length == 1)
                                commandLineRecognized = true;
                            else if (parameters.Length == 2)
                                scanConfigFilename = parameters[1];
                            else
                                Console.WriteLine($"ERROR:  Too many parameters specified for {runMode}.");
                        }
                        else if (runMode == CommandMode.UnitTest)
                        {
                            if (parameters.Length == 1)
                                commandLineRecognized = true;
                            else
                                Console.WriteLine($"ERROR:  Too many parameters specified for {runMode}.");
                        }
                        else if (runMode == CommandMode.Help)
                        {
                            if (parameters.Length == 1)
                                commandLineRecognized = true;
                            else
                                Console.WriteLine($"ERROR:  Too many parameters specified for {runMode}.");
                        }
                        else
                        {
                            // This should never happen
                            throw new Exception($"ERROR:  Unknown run mode {parameters[0]}");
                        }
                    }
                    else if (parameters[0].Equals("?") || parameters[0].Equals("/?") || parameters[0].Equals("-?") || parameters[0].Equals("--?") || parameters[0].Equals("/help", StringComparison.InvariantCultureIgnoreCase) || parameters[0].Equals("-help", StringComparison.InvariantCultureIgnoreCase) || parameters[0].Equals("--help", StringComparison.InvariantCultureIgnoreCase))
                    {
                        runMode = CommandMode.Help;
                        commandLineRecognized = true;
                    }
                }
                else
                {
                    runMode = CommandMode.Scan;
                    commandLineRecognized = true;
                }

                if (commandLineRecognized)
                {
                    if (runMode == CommandMode.Scan)
                    {
                        if (File.Exists(scanConfigFilename))
                        {
                            // Read and validate configuration
                            var configFileText = File.ReadAllText(scanConfigFilename);
                            var configuration = System.Text.Json.JsonSerializer.Deserialize<ConfigurationOptions>(configFileText);
                            if (configuration != null)
                            {
                                // Was the config file format recognized?
                                if (configuration.directoriesToScan?.Length > 0)
                                {
                                    // Are engine names recognized?
                                    if (configuration.algorithmsToUse?.Length > 0)
                                    {
                                        bool useCreditCardEngine = false;
                                        bool useSocialSecurityEngine = false;

                                        foreach (var currEngineName in configuration.algorithmsToUse)
                                        {
                                            if (currEngineName.Equals(AlgorithmName_FindCreditCardNumbers, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                useCreditCardEngine = true;
                                            }
                                            else if (currEngineName.Equals(AlgorithmName_FindSocialSecurityNumbers, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                useSocialSecurityEngine = true;
                                            }
                                            else
                                            {
                                                Console.WriteLine($"ERROR:  Unrecognized algorithm {currEngineName} in config file {scanConfigFilename}.");
                                                exitCode = ExitCode.Error_Scan_UnrecognizedAlgorithmName;
                                            }
                                        }

                                        var scanner = new Scanner(configuration, useSocialSecurityEngine, useCreditCardEngine);
                                        foreach (var currDirectory in configuration.directoriesToScan)
                                        {
                                            scanner.ScanDirectoryRecursiveandReportResults(currDirectory);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"ERROR:  Configuration file {scanConfigFilename} does not specify any algorithm to use - nothing to do.");
                                        exitCode = ExitCode.Error_Scan_NoAlgorithms;
                                    }
                                }
                                else
                                {
                                    if (((configuration.algorithmsToUse?.Length).GetValueOrDefault() > 0) || (configuration.supportZip))
                                    {
                                        Console.WriteLine($"ERROR:  Configuration file {scanConfigFilename} does not specify any directories to scan - nothing to do.");
                                        exitCode = ExitCode.Error_Scan_NoDirectoriesToScan;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"ERROR:  Configuration file {scanConfigFilename} has no recognized configuration - nothing to do.");
                                        exitCode = ExitCode.Error_Scan_UnrecognizedConfig; ;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"ERROR:  Configuration file {scanConfigFilename} not found.  Please create it.");
                            exitCode = ExitCode.Error_Scan_ConfigFileNotFound;
                        }
                        exitCode = ExitCode.Success;
                    }
                    else if (runMode == CommandMode.UnitTest)
                    {
                        Console.WriteLine("Not yet implemented");
                        exitCode = ExitCode.Success;
                    }
                    else if (runMode == CommandMode.Help)
                    {
                        var currAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                        var fileDate = System.IO.File.GetLastWriteTime(currAssembly.Location);
                        var myVersion = currAssembly.GetName().Version.ToString();
                        Console.WriteLine($"DriveScanner v{myVersion} build date {fileDate.Date.ToShortDateString()}");
                        Console.WriteLine();
                        Console.WriteLine($"Support commands:");
                        Console.WriteLine($"    {CommandMode.Scan} [optionalConfigFilename]");
                        Console.WriteLine($"       A configuration file is required.  If a filename is not specified, the");
                        Console.WriteLine($"       default is {DefaultConfigFileName} in the current directory.");
                        Console.WriteLine($"    {CommandMode.UnitTest}");
                        Console.WriteLine($"    {CommandMode.Help}");
                        exitCode = ExitCode.Success;
                    }
                }
                else
                {
                    Console.WriteLine("Unrecognized command line.  Use /? for command line help.");
                    exitCode = ExitCode.Error_CommandLine;
                }
            }
            catch (Exception unexpectedException)
            {
                Console.WriteLine($"ERROR {unexpectedException.GetType().Name}:  {unexpectedException.Message}");
                exitCode = ExitCode.Error_Exception;
            }

            return (int) exitCode;
        }
        #endregion Public Methods

        #region Public Types and Constants
        /// <summary>
        /// Process exit codes used by this program.
        /// Never change, remove, or repurpose a value, only add new values.
        /// </summary>
        public enum ExitCode : int
        {
            Success                             = 0,
            Success_NothingToDo                 = 1,
            NotYetInitialized                   = -100,
            Error_CommandLine                   = -1,
            Error_General                       = -2,
            Error_Exception                     = -3,
            Error_Scan_ConfigFileNotFound       = -4,
            Error_Scan_NoDirectoriesToScan      = -5,
            Error_Scan_NoAlgorithms             = -6,
            Error_Scan_UnrecognizedAlgorithmName = -7,
            Error_Scan_UnrecognizedConfig       = -8
        }

        public enum CommandMode
        {
            UnitTest,
            Scan,
            Help
        }

        /// <summary>
        /// Default configuration file
        /// NOT using GalacticScanner.config so we don't step on the Framework's config file.
        /// </summary>
        public const string DefaultConfigFileName                       = "scanner.config";
        public const string AlgorithmName_FindCreditCardNumbers         = "CreditCard";
        public const string AlgorithmName_FindSocialSecurityNumbers     = "SocialSecurity";
        public class ConfigurationOptions
        {
            public string[]     directoriesToScan       { get; set; }
            public string[]     algorithmsToUse         { get; set; }
            public bool         supportZip              { get; set; }
            public bool         useAscii                { get; set; } = true;
            public bool         useUtf8                 { get; set; } = true;
            public bool         useUnicode              { get; set; } = true;
            public bool         continueScanOnError     { get; set; } = true;
            public int          readBufferSizeBytes     { get; set; } = 4*1048576;
        }

        public class Result
        {
            public string       filename                { get; set; }
            public string       detectingScannerId      { get; set; }
            public long         detectionLocation       { get; set; }
            public long         detectionLength         { get; set; }
            public string       matchValue              { get; set; }
            public long         matchFileBytePosition   { get; set; }
        }
        #endregion Public Types and Constants

        #region Private Classes
        private class Scanner
        {
            #region Constructor
            public Scanner( ConfigurationOptions newOptions, bool newFindSocialSecurity, bool newFindCreditCard )
            {
                if (newOptions == null)
                    throw new ArgumentNullException(nameof(newOptions));

                options = newOptions;
                readBuffer = new byte[options.readBufferSizeBytes];
                findSocialSecurity = newFindSocialSecurity;
                findCreditCard = newFindCreditCard;
            }
            #endregion Constructor

            #region Public Methods
            public Result[] ScanDirectoryRecursiveandReportResults( string directoryPathname )
            {
                var results = new List<Result>();

                var currDirectoryFiles = Directory.GetFiles(directoryPathname, "*", SearchOption.TopDirectoryOnly);
                int noBytesRead = 0;
                long readBufferStartingOffsetInFile = 0;
                long currMatchFileBytePosition = 0;
                foreach (var currDirectoryFile in currDirectoryFiles)
                {
                    try
                    {
                        // Read file in chunks
                        using (var currFileStream = new FileStream(currDirectoryFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            noBytesRead = currFileStream.Read(readBuffer);

                            if (options.useUnicode)
                            {
                                var realizedUnicodeString = System.Text.Encoding.Unicode.GetString(readBuffer, 0, noBytesRead);

                                if (findCreditCard)
                                {
                                    var creditCardMatches = creditCardFinder.Matches(realizedUnicodeString);
                                    if (creditCardMatches?.Count > 0)
                                    {
                                        foreach (System.Text.RegularExpressions.Match currMatch in creditCardMatches)
                                        {
                                            currMatchFileBytePosition = readBufferStartingOffsetInFile + currMatch.Index * 2;
                                            Console.WriteLine($"FOUND CreditCard UNICODE {currMatch.Value} at byte position {currMatchFileBytePosition} in {currDirectoryFile}");

                                            var newResult = new Result();
                                            newResult.detectingScannerId    = AlgorithmName_FindCreditCardNumbers;
                                            newResult.filename              = currDirectoryFile;
                                            newResult.detectionLength       = currMatch.Length;
                                            newResult.matchValue            = currMatch.Value;
                                            newResult.matchFileBytePosition = currMatchFileBytePosition;
                                            results.Add(newResult);
                                        }
                                    }
                                }

                                if (findSocialSecurity)
                                {
                                    var socialSecurityMatches = socialSecurityFinder.Matches(realizedUnicodeString);
                                    if (socialSecurityMatches?.Count > 0)
                                    {
                                        // Bug note:  byte position is inaccurate as UTF8 characters are variable size
                                        foreach (System.Text.RegularExpressions.Match currMatch in socialSecurityMatches)
                                            Console.WriteLine($"FOUND SocialSecurity UNICODE {currMatch.Value} at byte position {readBufferStartingOffsetInFile + currMatch.Index} in {currDirectoryFile}");
                                    }
                                }
                            }
                            if (options.useUtf8)
                            {
                                var realizedUtfString = System.Text.Encoding.UTF8.GetString(readBuffer, 0, noBytesRead);

                                if (findCreditCard)
                                {
                                    var creditCardMatches = creditCardFinder.Matches(realizedUtfString);
                                    if (creditCardMatches?.Count > 0)
                                    {
                                        foreach (System.Text.RegularExpressions.Match currMatch in creditCardMatches)
                                            Console.WriteLine($"FOUND CreditCard UTF8 {currMatch.Value} at byte position {readBufferStartingOffsetInFile + currMatch.Index} in {currDirectoryFile}");
                                    }
                                }

                                if (findSocialSecurity)
                                {
                                    var socialSecurityMatches = socialSecurityFinder.Matches(realizedUtfString);
                                    if (socialSecurityMatches?.Count > 0)
                                    {
                                        foreach (System.Text.RegularExpressions.Match currMatch in socialSecurityMatches)
                                            Console.WriteLine($"FOUND SocialSecurity UTF8 {currMatch.Value} at byte position {readBufferStartingOffsetInFile + currMatch.Index} in {currDirectoryFile}");
                                    }
                                }
                            }
                            if (options.useAscii)
                            {
                                var realizedAsciiString = System.Text.Encoding.ASCII.GetString(readBuffer, 0, noBytesRead);
                                if (findCreditCard)
                                {
                                    var creditCardMatches = creditCardFinder.Matches(realizedAsciiString);
                                    if (creditCardMatches?.Count > 0)
                                    {
                                        foreach (System.Text.RegularExpressions.Match currMatch in creditCardMatches)
                                            Console.WriteLine($"FOUND CreditCard ASCII {currMatch.Value} at byte position {readBufferStartingOffsetInFile + currMatch.Index} in {currDirectoryFile}");
                                    }
                                }

                                if (findSocialSecurity)
                                {
                                    var socialSecurityMatches = socialSecurityFinder.Matches(realizedAsciiString);
                                    if (socialSecurityMatches?.Count > 0)
                                    {
                                        foreach (System.Text.RegularExpressions.Match currMatch in socialSecurityMatches)
                                            Console.WriteLine($"FOUND SocialSecurity ASCII {currMatch.Value} at byte position {readBufferStartingOffsetInFile + currMatch.Index} in {currDirectoryFile}");
                                    }
                                }
                            }

                            readBufferStartingOffsetInFile += noBytesRead;
                        }
                    }
                    catch (Exception unexpectedFileException)
                    {
                        Console.WriteLine($"ERROR {unexpectedFileException.GetType().Name} scanning file {currDirectoryFile}:  {unexpectedFileException.Message}");
                        if (!options.continueScanOnError)
                            return results.ToArray();
                    }
                }

                // Recurse into subdirectories
                var currDirectorySubdirs = Directory.GetDirectories(directoryPathname, "*", SearchOption.TopDirectoryOnly);
                foreach (var currSubdir in currDirectorySubdirs)
                {
                    ScanDirectoryRecursiveandReportResults(currSubdir);
                }

                return results.ToArray(); ;
            }
            #endregion Public Methods

            #region Private Data Members
            private ConfigurationOptions                    options;
            private byte[]                                  readBuffer;
            private bool                                    findSocialSecurity;
            private bool                                    findCreditCard;
            private System.Text.RegularExpressions.Regex    creditCardFinder        = new System.Text.RegularExpressions.Regex(@"\d\d\d\d[\s\-]+\d\d\d\d[\s\-]+\d\d\d\d[\s\-]+\d\d\d\d", System.Text.RegularExpressions.RegexOptions.Compiled);
            private System.Text.RegularExpressions.Regex    socialSecurityFinder    = new System.Text.RegularExpressions.Regex(@"\d\d\d[\s\-]+\d\d[\s\-]+\d\d\d\d", System.Text.RegularExpressions.RegexOptions.Compiled);
            #endregion Private Data Members
        }
        #endregion Private Classes
    }
}