using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ZLR.VM;
using System.Threading;
using System.Reflection;
using ZLR.Debugging;

namespace ZLR.Interfaces.SystemConsole
{
    class Program
    {
        enum DisplayType { FullScreen, Dumb, DumbBottomWinOnly }

        static int Main(string[] args)
        {
            try
            {
                Console.Title = "ConsoleZLR";

                Stream gameStream = null, debugStream = null;
                string gameDir = null, debugDir = null;
                string fileName = null, commandFile = null;
                DisplayType displayType = DisplayType.FullScreen;
                bool debugger = false, predictable = false;
                bool wait = true;

                if (args.Length >= 1 && args[0].Length > 0)
                {
                    int n = 0;

                    bool parsing = true;
                    do
                    {
                        switch (args[n].ToLower())
                        {
                            case "-commands":
                                if (args.Length > n + 1)
                                {
                                    commandFile = args[n + 1];
                                    n += 2;
                                    if (args.Length <= n)
                                        return Usage();
                                }
                                else
                                    return Usage();
                                break;
                            case "-dumb":
                                n++;
                                displayType = DisplayType.Dumb;
                                break;
                            case "-dumb2":
                                n++;
                                displayType = DisplayType.DumbBottomWinOnly;
                                break;
                            case "-debug":
                                n++;
                                debugger = true;
                                break;
                            case "-predictable":
                                n++;
                                predictable = true;
                                break;
                            case "-nowait":
                                n++;
                                wait = false;
                                break;
                            default:
                                parsing = false;
                                break;
                        }
                    } while (parsing);

                    gameStream = new FileStream(args[n], FileMode.Open, FileAccess.Read);
                    gameDir = Path.GetDirectoryName(Path.GetFullPath(args[n]));
                    fileName = Path.GetFileName(args[n]);

                    if (args.Length > n + 1)
                    {
                        debugStream = new FileStream(args[n + 1], FileMode.Open, FileAccess.Read);
                        debugDir = Path.GetDirectoryName(Path.GetFullPath(args[n + 1]));
                    }
                }
                else
                {
                    return Usage();
                }

                IZMachineIO io;

                switch (displayType)
                {
                    case DisplayType.Dumb:
                        io = new DumbIO(false, commandFile);
                        break;

                    case DisplayType.DumbBottomWinOnly:
                        io = new DumbIO(true, commandFile);
                        break;

                    case DisplayType.FullScreen:
                        ConsoleIO cio = new ConsoleIO(fileName);
                        if (commandFile != null)
                        {
                            cio.SuppliedCommandFile = commandFile;
                            cio.HideMorePrompts = true;
                        }
                        io = cio;
                        break;

                    default:
                        throw new NotImplementedException();
                }

                ZMachine zm = new ZMachine(gameStream, io);
                zm.PredictableRandom = predictable;
                if (commandFile != null)
                    zm.ReadingCommandsFromFile = true;
                if (debugStream != null)
                    zm.LoadDebugInfo(debugStream);

                if (debugger)
                {
                    List<string> sourcePath = new List<string>(3);
                    if (debugDir != null)
                        sourcePath.Add(debugDir);
                    sourcePath.Add(gameDir);
                    sourcePath.Add(Directory.GetCurrentDirectory());

                    DebuggerLoop(zm, sourcePath.ToArray());
                }
                else
                {
#if DEBUG
                    zm.Run();
#else
                try
                {
                    zm.Run();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
#endif
                    if (wait)
                    {
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey(true);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                return Error(ex.Message + " (" + ex.GetType().Name + ")");
            }
        }

        private static int Usage()
        {
            string exe = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            Console.WriteLine("Usage: {0} [-commands <commandfile.txt>] [-dumb | -dumb2] [-debug] [-predictable] [-nowait] <game_file.z5/z8> [<debug_file.dbg>]", exe);
            return 1;
        }

        private static int Error(string msg)
        {
            Console.Error.Write("Error: ");
            Console.Error.WriteLine(msg);
            return 2;
        }

        private static void DebuggerLoop(ZMachine zm, string[] sourcePath)
        {
            var console = new DebuggingConsole(zm, zm.IO, sourcePath);

            console.Activate();

            TimedInputCallback cb = () => false;
            byte[] terminatingKeys = { };

            while (console.Active)
            {
                byte terminator;
                string cmd = zm.IO.ReadLine(string.Empty, 0, cb, terminatingKeys, out terminator);

                console.HandleCommand(cmd);
            }
        }
    }
}
