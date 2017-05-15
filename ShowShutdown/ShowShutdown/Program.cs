using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

class Program
{
    [DllImport("Kernel32")]
    internal static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool Add);

    [DllImport("msvcrt.dll", PreserveSig = true)]
    static extern SignalHandler signal(int sig, SignalHandler handler);

    internal delegate bool HandlerRoutine(CtrlTypes ctrlType);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void SignalHandler(int sig);

    const int SIGINT = 2; // Ctrl-C
    const int SIGFPE = 8;
    const int SIGTERM = 15; // process termination
    const int WM_CLOSE = 0x0010;

    internal enum CtrlTypes
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT,
        CTRL_CLOSE_EVENT,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT
    }

    static void Main(string[] args)
    {
        var writer = new StreamWriter(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "log.txt"), true) {AutoFlush = true};

        writer.WriteLine("###### Starting up! ########");

        Console.CancelKeyPress += (s, e) => writer.WriteLine($"Console.CancelKeyPress {e.SpecialKey}, thread {Thread.CurrentThread.ManagedThreadId}");
        AppDomain.CurrentDomain.ProcessExit += (s, e) => writer.WriteLine($"CurrentDomain.ProcessExit, thread {Thread.CurrentThread.ManagedThreadId}");
        AppDomain.CurrentDomain.DomainUnload += (s, e) => writer.WriteLine($"CurrentDomain.DomainUnload, thread {Thread.CurrentThread.ManagedThreadId}");
        SystemEvents.SessionEnding += (s, e) => writer.WriteLine($"SystemEvents.SessionEnding {e.Reason}, thread {Thread.CurrentThread.ManagedThreadId}");
        SystemEvents.SessionEnded += (s, e) => writer.WriteLine($"SystemEvents.SessionEnded {e.Reason}, thread {Thread.CurrentThread.ManagedThreadId}");
        SystemEvents.SessionSwitch += (s, e) => writer.WriteLine($"SystemEvents.SessionSwitch {e.Reason}, thread {Thread.CurrentThread.ManagedThreadId}");
        SystemEvents.EventsThreadShutdown += (s, e) => writer.WriteLine($"SystemEvents.EventsThreadShutdown, thread {Thread.CurrentThread.ManagedThreadId}");
        SystemEvents.PowerModeChanged += (s, e) => writer.WriteLine($"SystemEvents.PowerModeChanged {e.Mode}, thread {Thread.CurrentThread.ManagedThreadId}");

        var hr = new HandlerRoutine(type =>
        {
            writer.WriteLine($"ConsoleCtrlHandler {type}, thread {Thread.CurrentThread.ManagedThreadId}");
            return false;
        });
        var sh = new SignalHandler(sig => writer.WriteLine($"Got signal {sig} on thread {Thread.CurrentThread.ManagedThreadId}"));
        writer.WriteLine($"SetConsoleCtrlHandler returned {SetConsoleCtrlHandler(hr, true)}");
        writer.WriteLine($"signal returned null: {signal(SIGTERM, sh) == null}");

        new Thread(() =>
        {
            using (var sf = new MyForm(writer))
                Application.Run(sf);
        }).Start();

        writer.WriteLine($"Okay, now I'm just sitting around waiting, on thread {Thread.CurrentThread.ManagedThreadId}");
        while (true)
            Thread.Sleep(1000);
        writer.WriteLine("uh, we exited the loop, what??");

        GC.KeepAlive(hr);
        GC.KeepAlive(sh);
    }

    class MyForm : Form
    {
        private TextWriter m_writer;

        public MyForm(TextWriter writer)
        {
            SuspendLayout();
            ClientSize = new Size(0, 0);
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Minimized;
            ResumeLayout();

            m_writer = writer;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CLOSE)
                m_writer.WriteLine($"got WM_CLOSE on thread {Thread.CurrentThread.ManagedThreadId}");
            //else
            //	Console.WriteLine($"got some random message {m.Msg} on thread {Thread.CurrentThread.ManagedThreadId}");

            base.WndProc(ref m);
        }
    }
}
