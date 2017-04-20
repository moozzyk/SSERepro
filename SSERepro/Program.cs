
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SSERepro
{
    class Program
    {
        private static string response;

        public static object Timespan { get; private set; }

        static void Main(string[] args)
        {
            response = File.ReadAllText("response.html");

            var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 6500);
            try
            {
                server.Start();
                while (true)
                {
                    var client = server.AcceptTcpClient();
                    _ = HandleRequest(client);
                }
            }
            finally
            {
                server.Stop();
            }
        }

        private static async Task HandleRequest(TcpClient client)
        {
            var buffer = new byte[4096];
            var stream = client.GetStream();
            if (await stream.ReadAsync(buffer, 0, buffer.Length) == 0)
            {
                return;
            }

            var sb = new StringBuilder();
            var request = Encoding.ASCII.GetString(buffer);
            if (request.Contains("Accept: text/html"))
            {
                sb.AppendLine("HTTP/1.1 200 OK");
                sb.AppendLine("Content-Type: text/html");
                sb.AppendLine($"Content-Length: {response.Length}");
                sb.AppendLine();
                sb.Append(response);

                var buff = Encoding.ASCII.GetBytes(sb.ToString());
                await stream.WriteAsync(buff, 0, buff.Length);
                await stream.FlushAsync();
            }
            else if (request.Contains("Accept: text/event-stream"))
            {
                sb.AppendLine("HTTP/1.1 200 OK");
                sb.AppendLine("Content-Type: text/event-stream");
                // sb.AppendLine("Transfer-Encoding: chunked");
                sb.AppendLine();

                var buff = Encoding.ASCII.GetBytes(sb.ToString());
                await stream.WriteAsync(buff, 0, buff.Length);
                await stream.FlushAsync();

                await Task.Delay(TimeSpan.FromSeconds(5));
                for (var i = 1; i < 10; i++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    var data = $"data: {i}\n\n";
                    sb.Length = 0;
                    // sb.AppendLine(data.Length.ToString("x4"));
                    sb.Append(data);

                    buff = Encoding.ASCII.GetBytes(sb.ToString());
                    await stream.WriteAsync(buff, 0, buff.Length);
                    await stream.FlushAsync();
                }

                stream.WriteByte(13);
                stream.WriteByte(10);
            }
            client.Close();
        }
    }
}
