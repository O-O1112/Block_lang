using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BlockEngine
{
    class Program
    {
        static string ReadScriptFile(string path)
        {
            FileInfo info = new FileInfo(path);
            if (info.Length > SecurityLimits.MaxScriptBytes)
                throw new InvalidOperationException(string.Format("Script file exceeds the {0} MiB limit.", SecurityLimits.MaxScriptBytes / (1024 * 1024)));
            return File.ReadAllText(path);
        }

        static void Main(string[] args)
        {
            try
            {
                MainCore(args);
            }
            catch (Exception ex)
            {
                // Keep fatal errors actionable even when the host cannot render Exception.ToString().
                Console.Error.WriteLine("[Block fatal] " + ex.GetType().FullName);
                Console.Error.WriteLine(ex.Message ?? "(no message)");
                if (!string.IsNullOrEmpty(ex.StackTrace)) Console.Error.WriteLine(ex.StackTrace);
                Environment.ExitCode = 1;
            }
        }

        static void MainCore(string[] args)
        {
            if (args.Length == 0)
            {
                ShowAnimationAndUsage();
                return;
            }

            string arg0 = args[0].ToLowerInvariant().Trim();
            if (arg0 == "--version" || arg0 == "-v" || arg0 == "version")
            {
#if BLOCK_LITE
                Console.WriteLine("Block Lite Engine v" + BlockVersion.Value + " (Lite Edition)");
#elif BLOCK_PLUS
                Console.WriteLine("Block+ Engine v" + BlockVersion.Value + " (Flagship Edition)");
#else
                Console.WriteLine("Block Language Engine v" + BlockVersion.Value + " (Standard Edition)");
#endif
                return;
            }

            if (arg0 == "--help" || arg0 == "-h" || arg0 == "help")
            {
                ShowAnimationAndUsage(false);
                return;
            }

            if (arg0 == "runtimes" || arg0 == "doctor")
            {
                CliCommands.RunRuntimes(arg0 == "doctor");
                return;
            }

            if (arg0 == "workspace")
            {
                CliCommands.RunWorkspace(args);
                return;
            }

            if (arg0 == "find")
            {
                CliCommands.RunFind(args.Length > 1 ? JoinCommandLinePath(args, 1) : null);
                return;
            }

            if (arg0 == "info" || arg0 == "capabilities")
            {
                string infoPath = args.Length > 1 ? JoinCommandLinePath(args, 1) : null;
                CliCommands.RunInfo(infoPath);
                return;
            }

            if (arg0 == "check" && args.Length > 1)
            {
                CliCommands.RunCheck(JoinCommandLinePath(args, 1));
                return;
            }

            if (arg0 == "check")
            {
                Console.Error.WriteLine("Usage: block check <file>");
                Environment.ExitCode = 1;
                return;
            }

            if (arg0 == "config")
            {
                if (args.Length > 1 && string.Equals(args[1], "path", StringComparison.OrdinalIgnoreCase))
                {
                    CliCommands.RunConfigPath();
                    return;
                }
                if (args.Length > 1 && string.Equals(args[1], "show", StringComparison.OrdinalIgnoreCase))
                {
                    CliCommands.RunConfigShow();
                    return;
                }
                Config.RunSettingsCLI();
                return;
            }

            if (arg0 == "project" && args.Length > 1 &&
                (string.Equals(args[1], "root", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(args[1], "run", StringComparison.OrdinalIgnoreCase)))
            {
                CliCommands.RunProjectCommand(args);
                return;
            }

#if !BLOCK_LITE
            if (arg0 == "ecosystem" || arg0 == "eco" || arg0 == "pkg" || arg0 == "project")
            {
                Ecosystem.RunCli(args);
                return;
            }
#else
            if (arg0 == "project")
            {
                CliCommands.RunProjectCommand(args);
                return;
            }
#endif

#if !BLOCK_LITE
            if (args[0].ToLower() == "serve")
            {
                int port = 8080;
                if (args.Length > 1 && (!int.TryParse(args[1], out port) || port < 1 || port > 65535))
                {
                    Console.Error.WriteLine("Invalid port. Use an integer between 1 and 65535.");
                    Environment.ExitCode = 1;
                    return;
                }
                EngineConfig apiCfg = Config.LoadConfig();
                ApiServer.StartApiServer(apiCfg, port);
                return;
            }
#endif

#if BLOCK_PLUS
            if (args[0].ToLower() == "fmt" && args.Length > 1)
            {
                RunFmtCLI(JoinCommandLinePath(args, 1));
                return;
            }
            if (args[0].ToLower() == "doc" && args.Length > 1)
            {
                RunDocCLI(JoinCommandLinePath(args, 1));
                return;
            }
#endif


            // cmd.exe splits unquoted paths at spaces. For the file-execution
            // form, reassemble the remaining arguments so filenames such as
            // "新文件 1.blkp" still resolve instead of truncating at the space.
            EngineConfig cfg = Config.LoadConfig();
            string scriptArgument = arg0 == "run"
                ? (args.Length > 1 ? JoinCommandLinePath(args, 1) : null)
                : (args.Length > 1 ? string.Join(" ", args) : args[0]);
            string scriptPath;
            try
            {
                scriptPath = BlockPathResolver.ResolveScript(scriptArgument, cfg, arg0 == "run" ? "run" : "script");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[Block] " + ex.Message);
                Environment.ExitCode = 1;
                return;
            }

            ExecuteScript(scriptPath, cfg);
        }

        private static string JoinCommandLinePath(string[] args, int startIndex)
        {
            if (args == null || args.Length <= startIndex) return "";
            return string.Join(" ", args, startIndex, args.Length - startIndex);
        }

        internal static void ExecuteScript(string scriptPath, EngineConfig cfg)
        {
            try
            {
                string code = ReadScriptFile(scriptPath);
#if !BLOCK_LITE
                if (Server.ParseAndRunServer(code, cfg, scriptPath))
                {
                    return; // Started server
                }
#endif
                Task.Run(async () =>
                {
                    Dictionary<string, object> initialState = new Dictionary<string, object>();
                    await Executor.ExecuteBlocksAsync(Parser.ParseBlocks(code, scriptPath, cfg), cfg, scriptPath, initialState, null);
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(string.Format("Execution Error: {0}", ex.Message));
                Environment.ExitCode = 1;
            }
        }

        static void ShowAnimationAndUsage(bool infinite = true)
        {
            string[] logo = new string[] {
                "                                                               ",
                "   =========================================================   ",
                "          ____  _      ____   ____ _  __                     ",
                "         |  _ \\| |    / __ \\ / ___| |/ /                     ",
                "         | |_) | |   | |  | | |   | ' /                      ",
                "         |  _ <| |   | |  | | |   |  <                       ",
                "         | |_) | |___| |__| | |___| . \\                      ",
                "         |____/|______\\____/ \\____|_|\\_\\                     ",
                "                                                               ",
#if BLOCK_LITE
                "          BLOCK LITE ENGINE v" + BlockVersion.Value + "                         ",
#elif BLOCK_PLUS
                "          BLOCK+ ENGINE v" + BlockVersion.Value + "                             ",
#else
                "          BLOCK LANGUAGE ENGINE v" + BlockVersion.Value + "                     ",
#endif
                "          Official Website: block.blockengine.workers.dev      ",
                "   =========================================================   ",
                "                                                               "
            };

            try 
            {
                Console.CursorVisible = false;
                int startTop = Console.CursorTop;
                // Ensure there is enough space to print without scrolling
                if (startTop + logo.Length >= Console.WindowHeight)
                {
                     Console.WriteLine(new string('\n', logo.Length));
                     startTop = Console.CursorTop - logo.Length;
                }

                int loops = infinite ? 100000 : 2;
                for (int loop = 0; loop < loops; loop++)
                {
                    if (infinite && Console.KeyAvailable) 
                    {
                        Console.ReadKey(true);
                        break;
                    }

                    for (int pos = -10; pos < 70; pos++)
                    {
                        if (infinite && Console.KeyAvailable) break;

                        Console.SetCursorPosition(0, startTop);
                        for (int i = 0; i < logo.Length; i++)
                        {
                            for (int j = 0; j < logo[i].Length; j++)
                            {
                                if (j >= pos && j < pos + 8)
                                {
                                    Console.ForegroundColor = ConsoleColor.DarkGray;
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.White;
                                }
                                Console.Write(logo[i][j]);
                            }
                            Console.WriteLine();
                        }
                        System.Threading.Thread.Sleep(infinite ? 40 : 15);
                    }
                }
                Console.ResetColor();
                Console.CursorVisible = true;
            } 
            catch 
            {
                // Fallback for redirected stdout or unsupported console handles
                for (int i = 0; i < logo.Length; i++)
                {
                    Console.WriteLine(logo[i]);
                }
            }

#if BLOCK_LITE
            Console.WriteLine("Usage: block-lite <file.blkl>");
            Console.WriteLine("       block-lite run <file.blkl>");
            Console.WriteLine("       block-lite check <file.blkl>");
            Console.WriteLine("       block-lite info [file.blkl]");
            Console.WriteLine("       block-lite capabilities");
            Console.WriteLine("       block-lite runtimes");
            Console.WriteLine("       block-lite doctor");
            Console.WriteLine("       block-lite workspace show|set|clear");
            Console.WriteLine("       block-lite find [name]");
            Console.WriteLine("       block-lite project root|run [path]");
            Console.WriteLine("       block-lite config");
            Console.WriteLine("       block-lite config show|path");
#elif BLOCK_PLUS
            Console.WriteLine("Usage: block-plus <file.blkp>");
            Console.WriteLine("       block-plus run <file.blkp>");
            Console.WriteLine("       block-plus check <file.blkp>");
            Console.WriteLine("       block-plus info [file.blkp]");
            Console.WriteLine("       block-plus capabilities");
            Console.WriteLine("       block-plus runtimes");
            Console.WriteLine("       block-plus doctor");
            Console.WriteLine("       block-plus workspace show|set|clear");
            Console.WriteLine("       block-plus find [name]");
            Console.WriteLine("       block-plus project root|run [path]");
            Console.WriteLine("       block-plus config");
            Console.WriteLine("       block-plus config show|path");
            Console.WriteLine("       block-plus serve [port]");
            Console.WriteLine("       block-plus ecosystem|project init|list|add ...");
            Console.WriteLine("       block-plus fmt <file.blkp>");
            Console.WriteLine("       block-plus check <file.blkp>");
            Console.WriteLine("       block-plus doc <file.blkp>");
#else
            Console.WriteLine("Usage: block <file.blk>");
            Console.WriteLine("       block run <file.blk>");
            Console.WriteLine("       block check <file.blk>");
            Console.WriteLine("       block info [file.blk]");
            Console.WriteLine("       block capabilities");
            Console.WriteLine("       block runtimes");
            Console.WriteLine("       block doctor");
            Console.WriteLine("       block workspace show|set|clear");
            Console.WriteLine("       block find [name]");
            Console.WriteLine("       block project root|run [path]");
            Console.WriteLine("       block config");
            Console.WriteLine("       block config show|path");
            Console.WriteLine("       block serve 8080");
            Console.WriteLine("       block ecosystem|project init|list|add ...");
#endif
        }

#if BLOCK_PLUS
        static void RunFmtCLI(string filePath)
        {
            string path = Path.GetFullPath(filePath);
            if (!File.Exists(path)) { Console.Error.WriteLine("File not found: " + path); Environment.ExitCode = 1; return; }
            
            // H9: Fix: Write backup before overwriting original
            string backupPath = path + ".bak";
            File.Copy(path, backupPath, overwrite: true);
            Console.WriteLine("[Block+ Fmt] Backup created: " + backupPath);
            
            try
            {
                string code = ReadScriptFile(path);
                var blocks = Parser.ParseBlocks(code, path, Config.LoadConfig());
                System.Collections.Generic.List<string> formatted = new System.Collections.Generic.List<string>();
                foreach (var b in blocks)
                {
                    if (b.Language == "block")
                    {
                        if (!string.IsNullOrWhiteSpace(b.Code)) formatted.Add(b.Code.Trim());
                    }
                    else
                    {
                        formatted.Add(string.Format("<{0}>\n{1}\n</{0}>", b.Language, b.Code.Trim()));
                    }
                }
                string result = string.Join("\n\n", formatted) + "\n";
                File.WriteAllText(path, result, System.Text.Encoding.UTF8);
                Console.WriteLine("[Block+ Fmt] Formatted successfully: " + path);
                Console.WriteLine("[Block+ Fmt] Backup kept at: " + backupPath + " (delete when satisfied)");
            }
            catch (Exception ex)
            {
                // H9: Fix: Restore backup on failure
                File.Copy(backupPath, path, overwrite: true);
                Console.Error.WriteLine("[Block+ Fmt] FAILED and restored original: " + ex.Message);
                Environment.ExitCode = 1;
            }
        }

        static void RunDocCLI(string filePath)
        {
            string path = Path.GetFullPath(filePath);
            if (!File.Exists(path)) { Console.Error.WriteLine("File not found: " + path); Environment.ExitCode = 1; return; }
            string code = ReadScriptFile(path);
            var blocks = Parser.ParseBlocks(code, path, Config.LoadConfig());
            string docPath = Path.ChangeExtension(path, ".doc.md");

            List<string> docLines = new List<string>();
            docLines.Add("# Block+ Script Documentation");
            docLines.Add("\n**Source File**: `" + Path.GetFileName(path) + "`");
            docLines.Add("**Generated**: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            docLines.Add("\n## Executable Blocks Summary\n");
            docLines.Add("| Block # | Language | Size (Bytes) |");
            docLines.Add("|:---:|:---:|:---:|");

            int idx = 1;
            foreach (var b in blocks)
            {
                docLines.Add(string.Format("| {0} | `<{1}>` | {2} |", idx++, b.Language, System.Text.Encoding.UTF8.GetByteCount(b.Code)));
            }

            File.WriteAllText(docPath, string.Join("\n", docLines), System.Text.Encoding.UTF8);
            Console.WriteLine("[Block+ Doc] Documentation generated: " + docPath);
        }
#endif
    }
}
