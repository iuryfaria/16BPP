﻿using System;
using System.Threading;
using System.Net.NetworkInformation;

namespace PingExample
{
    //this is an example of how to use Ping both sychrnously
    //ad asynchronously
    class PingExample
    {
        private static string _hostname;
        private static int _timeout = 2000; // 2 seconds in milliseconds
        private static object _consolelock = new object();

        //locks console output and prints info about a PingReply (with color)
        public static void PrintPingReply(PingReply reply, ConsoleColor textColor)
        {
            lock (_consolelock)
            {
                Console.ForegroundColor = textColor;

                Console.WriteLine("Got ping response from {0}", _hostname);
                Console.WriteLine("  Remote address: {0}", reply.Address);
                Console.WriteLine("  Roundtrip time: {0}", reply.RoundtripTime);
                Console.WriteLine("  Size: {0} bytes", reply.Buffer.Length);
                Console.WriteLine("  TTL: {0}", reply.Options.Ttl);

                Console.ResetColor();
            }
        }

        //A callback for doing an Asynchronous ping
        public static void PingCompletedHandler(object sender, PingCompletedEventArgs e)
        {
            //Canceled, error, or fine?
            if (e.Cancelled)
                Console.WriteLine("Ping was canceled.");
            else if (e.Error != null)
                Console.WriteLine("There was an error with the Ping, reason={0}", e.Error.Message);
            else
                PrintPingReply(e.Reply, ConsoleColor.Cyan);

            // Notify the calling thread
            AutoResetEvent waiter = (AutoResetEvent)e.UserState;
            waiter.Set();

        }

        //Perfoms a Synchronous Ping
        public static void SendSynchronousPing(Ping pinger, ConsoleColor textColor)
        {
            PingReply reply = pinger.Send(_hostname, _timeout);  //will block for at most
            if(reply.Status == IPStatus.Success)
                PrintPingReply(reply, ConsoleColor.Magenta);
            else
            {
                Console.WriteLine("Synchronous Ping to {0} failed:", _hostname);
                Console.WriteLine("  Status: {0}", reply.Status);
            }
        }

        public static void Main(string[] args)
        {
            //Setup the pinger
            Ping pinger = new Ping();
            pinger.PingCompleted += PingCompletedHandler;

            //poll the user where to send the Ping to
            Console.WriteLine("Send a Ping to whom: ");
            _hostname = Console.ReadLine();

            //Send async (w/ callback)
            AutoResetEvent waiter = new AutoResetEvent(false); //st to not-signaled
            pinger.SendAsync(_hostname, waiter);

            // Check immediately for the async ping
            if (waiter.WaitOne(_timeout) == false)
            {
                pinger.SendAsyncCancel();
                Console.WriteLine("Async Ping to {0} timed out.", _hostname);
            }

            // Send it synchronously
            SendSynchronousPing(pinger, ConsoleColor.Magenta);
        }
    }
}