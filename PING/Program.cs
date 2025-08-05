using System;
using System.Threading;
using System.Net.NetworkInformation;

namespace PingExample
{
    class Program
    {
        private static string _hostname;
        private static int _timeout = 2000; // 2 seconds in milliseconds
        private static object _consolelock = new object();

        // Locks console output and prints info about a PingReply (with color)
        public static void PrintPingReply(PingReply reply, ConsoleColor textColor)
        {
            lock (_consolelock)
            {
                Console.ForegroundColor = textColor;

                Console.WriteLine("Got ping response from {0}", _hostname);
                Console.WriteLine("  Remote address: {0}", reply.Address);
                Console.WriteLine("  Roundtrip time: {0} ms", reply.RoundtripTime);
                Console.WriteLine("  Size: {0} bytes", reply.Buffer.Length);

                if (reply.Options != null)
                    Console.WriteLine("  TTL: {0}", reply.Options.Ttl);
                else
                    Console.WriteLine("  TTL: (unknown)");

                Console.ResetColor();
            }
        }

        // A callback for doing an Asynchronous ping
        public static void PingCompletedHandler(object sender, PingCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("Ping was canceled.");
            }
            else if (e.Error != null)
            {
                Console.WriteLine("There was an error with the Ping, reason={0}", e.Error.Message);
            }
            else if (e.Reply.Status == IPStatus.Success)
            {
                PrintPingReply(e.Reply, ConsoleColor.Cyan);
            }
            else
            {
                Console.WriteLine("Async Ping to {0} failed: {1}", _hostname, e.Reply.Status);
            }

            // Notify the calling thread
            AutoResetEvent waiter = (AutoResetEvent)e.UserState;
            waiter.Set();
        }

        // Performs a Synchronous Ping
        public static void SendSynchronousPing(Ping pinger)
        {
            try
            {
                PingReply reply = pinger.Send(_hostname, _timeout);
                if (reply.Status == IPStatus.Success)
                {
                    PrintPingReply(reply, ConsoleColor.Magenta);
                }
                else
                {
                    Console.WriteLine("Synchronous Ping to {0} failed:", _hostname);
                    Console.WriteLine("  Status: {0}", reply.Status);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Synchronous Ping failed with exception: {0}", ex.Message);
            }
        }

        public static void Main(string[] args)
        {
            Ping pinger = new Ping();
            pinger.PingCompleted += PingCompletedHandler;

            Console.WriteLine("Send a Ping to whom: ");
            _hostname = Console.ReadLine();

            AutoResetEvent waiter = new AutoResetEvent(false);
            try
            {
                pinger.SendAsync(_hostname, waiter);

                if (!waiter.WaitOne(_timeout))
                {
                    pinger.SendAsyncCancel();
                    Console.WriteLine("Async Ping to {0} timed out.", _hostname);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Async Ping failed with exception: {0}", ex.Message);
            }

            // Now the synchronous one
            SendSynchronousPing(pinger);
        }
    }
}
