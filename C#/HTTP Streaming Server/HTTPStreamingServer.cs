using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HTTP_Streaming_Server
{
    public class HTTPStreamingServer
    {
        private int port;

        private static Socket socket;

        public HTTPStreamingServer(int port)
        {
            this.port = port;
        }

        public void StartServer()
        {
            IPHostEntry ipHostEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = null;

            for (int i = 0; i < ipHostEntry.AddressList.Length; i++)
            {
                if (ipHostEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddress = ipHostEntry.AddressList[i];

                    break;
                }
            }

            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

            Console.WriteLine("Server running at " + ipAddress + ":" + port + "\n");

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Bind(ipEndPoint);
                socket.Listen(5);

                while (true)
                {
                    Console.WriteLine("Waiting for clients...\n");
                    new Thread(new ParameterizedThreadStart(HandleRequest)).Start(socket.Accept());
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception: " + exception.Message + "\n");
            }
        }

        private static string ReadRequest(NetworkStream networkStream)
        {
            byte[] buffer = new byte[Program.BUFFER_SIZE];
            int bytesRead = 0;
            string request = "";

            while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) > 0)      // accepting client request...
            {
                request += Encoding.ASCII.GetString(buffer, 0, bytesRead);

                if (buffer[bytesRead - 1] == 10 && buffer[bytesRead - 2] == 13)     // checks if '\r\n' is found...
                {
                    break;
                }
            }

            return request.Trim();
        }

        private static void HandleRequest(object socket)       // newly created connection socket should be passed...
        {
            byte[] buffer = new byte[Program.BUFFER_SIZE];
            int offset = 0, bytesRead = 0;
            long rangeStart = 0, rangeEnd = 0;
            string fileName, request, responseHeader;

            FileInformation fileInformation = null;
            Dictionary<string, string> responseHeaders;

            using (NetworkStream networkStream = new NetworkStream((Socket)socket))
            {
                request = ReadRequest(networkStream);

                if (!string.IsNullOrEmpty(request))
                {
                    responseHeaders = Parse(request);
                    fileName = responseHeaders["File-Name"];

                    try
                    {
                        fileInformation = new FileInformation(fileName);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("Exception: " + exception.Message + "\n");
                    }

                    Console.WriteLine("Request\n=======\n\n" + request + "\n");     // just for debugging...

                    responseHeader = "HTTP/1.1 {0}\r\n" +
                        "Date: " + DateTime.Now.ToString("r") + "\r\n" +
                        "Accept-Ranges: bytes\r\n" +
                        "Host: " + responseHeaders["Host"] + "\r\n" +
                        "Content-Type: " + fileInformation.contentType + "\r\n" +
                        "Last-Modified: " + fileInformation.lastModified + "\r\n" +
                        "Pragma: no-cache\r\n" +
                        "Cache-Control: no-cache, no-store, must-revalidate\r\n";

                    if (responseHeaders.ContainsKey("Range-Start"))
                    {
                        responseHeader = string.Format(responseHeader, "206 Partial Content");

                        if (responseHeaders.ContainsKey("Range-End") && !string.IsNullOrEmpty(responseHeaders["Range-End"]))
                        {
                            rangeEnd = long.Parse(responseHeaders["Range-End"]);
                        }
                        else
                        {
                            rangeEnd = fileInformation.contentLength - 1;
                        }

                        responseHeader += "Content-Range: bytes " + responseHeaders["Range-Start"] + "-" + rangeEnd + "/" + fileInformation.contentLength + "\r\n";
                    }
                    else
                    {
                        responseHeader = string.Format(responseHeader, "200 OK");
                    }

                    responseHeader += "Connection: Keep-Alive\r\n\r\n";
                    offset = AddResponseHeader(ref buffer, responseHeader);

                    Console.WriteLine("Response\n========\n\n" + responseHeader.Trim() + "\n");        // just for debugging...

                    if (responseHeaders.ContainsKey("Range-Start"))
                    {
                        using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            rangeStart = long.Parse(responseHeaders["Range-Start"]);

                            if (rangeStart > 0)
                            {
                                fileStream.Seek(rangeStart, SeekOrigin.Begin);
                            }

                            while ((bytesRead = fileStream.Read(buffer, offset, buffer.Length - offset)) > 0)
                            {
                                try
                                {
                                    networkStream.Write(buffer, 0, offset + bytesRead);
                                    networkStream.Flush();

                                    offset = 0;
                                }
                                catch (Exception exception)
                                {
                                    Console.WriteLine("Exception: " + exception.Message + "\n");

                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        networkStream.Write(buffer, 0, offset);
                        networkStream.Flush();
                    }
                }
            }
        }

        private static int AddResponseHeader(ref byte[] buffer, string responseHeader)      // adds response header to the beginning of the buffer...
        {
            byte[] responseHeaderBytes = Encoding.ASCII.GetBytes(responseHeader);
            int i = 0;

            for (i = 0; i < responseHeaderBytes.Length; i++)
            {
                buffer[i] = responseHeaderBytes[i];
            }

            return i;       // returns the offset...
        }

        private static Dictionary<string, string> Parse(string request)
        {
            string[][] splits =
            {
                request.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries),
                null
            };

            Dictionary<string, string> headers = new Dictionary<string, string>();

            for (int i = 0; i < splits[0].Length; i++)
            {
                if (i == 0)
                {
                    splits[0][i] = splits[0][i].Substring(5);
                    splits[0][i] = splits[0][i].Substring(0, splits[0][i].Length - 9);

                    headers.Add("File-Name", splits[0][i]);
                }
                else
                {
                    splits[1] = splits[0][i].Split(new char[] { ':' });
                    splits[1][1] = splits[1][1].Trim();

                    if (splits[1][0] == "Range")
                    {
                        splits[1] = splits[1][1].Substring(6).Split(new char[] { '-' });
                        headers.Add("Range-Start", splits[1][0]);
                        headers.Add("Range-End", splits[1][1]);
                    }
                    else if (splits[1][0] == "Host")
                    {
                        headers.Add(splits[1][0], splits[1][1] + ":" + splits[1][2]);
                    }
                    else
                    {
                        headers.Add(splits[1][0], splits[1][1]);
                    }
                }
            }

            return headers;
        }
    }
}