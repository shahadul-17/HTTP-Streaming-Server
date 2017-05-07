using System;

namespace HTTP_Streaming_Server
{
    public class Program
    {
        public static readonly int BUFFER_SIZE = 8192;

        public static void Main(string[] args)
        {
            HTTPStreamingServer httpStreamingServer = new HTTPStreamingServer(8080);    // port = 8080...
            httpStreamingServer.StartServer();

            Console.ReadKey();
        }
    }
}