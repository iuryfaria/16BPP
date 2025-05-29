using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading.Tasks;

namespace HttpListenerExample
{
    class HttpServer
    {
        public static HttpListener listener;
        public static string url = "http://localhost:8080/";
        public static int pageViews = 0;
        public static int requestCount = 0;
        public static string pageData = 
            "<!DocType html>" +
            "<html>" +
            "   <head>" +
            "       <title>HttpListener Example</title>" +
            "   </head>" +
            "   <body>" +
            "       <p>Page Views: {0}</p>" +
            "       <form method=\"post\" action=\"shutdown\">" +
            "           <input type=\"submit\" value=\"Shutdown\" {1}> " +
            "       </form>" + 
            "      </body>"+
            "</html>";


        public static async Task HandleInComingConnections()
        {
            bool runServer = true;

            while (runServer)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                Console.WriteLine("Request #: {0}", ++requestCount);
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();

                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown"))
                {
                    Console.WriteLine("Shutting requested");
                    runServer = false;
                }

                if(req.Url.AbsolutePath != "/favicon.ico")
                
                    pageViews += 1;

                string disableSubmit = !runServer ? "disabled" : "";
                byte[] data = Encoding.UTF8.GetBytes(
                    string.Format(pageData, pageViews, disableSubmit));
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();

            }
        }


        public static void Main(string[] args)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            Task listenTask = HandleInComingConnections();
            listenTask.GetAwaiter().GetResult();

            listener.Close();
        }

    }
}
