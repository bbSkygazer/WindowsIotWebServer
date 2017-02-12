using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;


// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace WindowsIotWebServer
{
    public sealed class StartupTask : IBackgroundTask
    {
        internal static BackgroundTaskDeferral _Deferral = null;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            // 

             _Deferral = taskInstance.GetDeferral();

            var webserver = new MyWebserver();

            await ThreadPool.RunAsync(workItem => { webserver.Start(); });

        }
    }
}

internal class MyWebserver
{
    private const uint BufferSize = 8192;

    public async void Start()
    {
        var listener = new StreamSocketListener();

        await listener.BindServiceNameAsync("8000");

        listener.ConnectionReceived += async (sender, args) =>
        {
            var request = new StringBuilder();

            using (var input = args.Socket.InputStream)
            {
                var data = new byte[BufferSize];
                IBuffer buffer = data.AsBuffer(); 
                var dataRead = BufferSize;

                while (dataRead == BufferSize)
                {
                    await input.ReadAsync(
                    buffer, BufferSize, InputStreamOptions.Partial);
                    request.Append(Encoding.UTF8.GetString(
                    data, 0, data.Length));
                    dataRead = buffer.Length;
                }
            }

            string query = GetQuery(request);

            using (var output = args.Socket.OutputStream)
            {
                using (var response = output.AsStreamForWrite())
                {
                    var html = Encoding.UTF8.GetBytes(
                    $"<html><head><title>Background Message</title></head><body>Hello from the background process!<br/>{query}</body></html>");
                    using (var bodyStream = new MemoryStream(html))
                    {
                        var header = $"HTTP/1.1 200 OK\r\nContent-Length: {bodyStream.Length}\r\nConnection: close\r\n\r\n";
                        var headerArray = Encoding.UTF8.GetBytes(header);
                        await response.WriteAsync(headerArray,
                        0, headerArray.Length);
                        await bodyStream.CopyToAsync(response);
                        await response.FlushAsync();
                    }
                }
            }
        };
    }

    private static string GetQuery(StringBuilder request)
    {
        var requestLines = request.ToString().Split(' ');

        var url = requestLines.Length > 1
        ? requestLines[1] : string.Empty;

        var uri = new Uri("http://localhost" + url);
        var query = uri.Query;
        return query;
    }
}