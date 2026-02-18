using System;
using System.Collections;
using System.Text;
using System.Net;
#if NANOFRAMEWORK
using nanoFramework.WebServer;
#endif

namespace UniversalFeeder.Firmware
{
    public class WebServerService : IDisposable
    {
        private readonly IFeedingSequenceService _feedingSequence;
        private readonly IBuzzerService _buzzerService;
#if NANOFRAMEWORK
        private WebServer _server;
#endif

        public WebServerService(IFeedingSequenceService feedingSequence, IBuzzerService buzzerService)
        {
            _feedingSequence = feedingSequence;
            _buzzerService = buzzerService;
            
#if NANOFRAMEWORK
            _server = new WebServer(80, HttpProtocol.Http);
            _server.CommandReceived += OnCommandReceived;
#endif
        }

        public void Start()
        {
#if NANOFRAMEWORK
            _server.Start();
#endif
            Console.WriteLine("Web Server Started on port 80");
        }

#if NANOFRAMEWORK
        private void OnCommandReceived(object source, WebServerEventArgs e)
        {
            var url = e.Context.Request.RawUrl;
            var query = RequestProcessor.ParseQueryString(url);

            if (url.StartsWith("/feed"))
            {
                if (query.Contains("ms"))
                {
                    int ms = int.Parse((string)query["ms"]);
                    _feedingSequence.Execute(ms);
                    SendResponse(e.Context, "Feeding sequence started");
                }
                else
                {
                    SendResponse(e.Context, "Error: Missing 'ms' parameter", HttpStatusCode.BadRequest);
                }
            }
            else if (url.StartsWith("/chime"))
            {
                if (query.Contains("vol"))
                {
                    float vol = float.Parse((string)query["vol"]);
                    _buzzerService.Play(vol, 1000);
                    SendResponse(e.Context, "Chime played");
                }
                else
                {
                    SendResponse(e.Context, "Error: Missing 'vol' parameter", HttpStatusCode.BadRequest);
                }
            }
            else
            {
                SendResponse(e.Context, "Universal Auto-Feeder Online", HttpStatusCode.OK);
            }
        }

        private void SendResponse(HttpListenerContext context, string message, HttpStatusCode status = HttpStatusCode.OK)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            context.Response.StatusCode = (int)status;
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        }
#endif

        public void Dispose()
        {
#if NANOFRAMEWORK
            _server?.Dispose();
#endif
        }
    }
}
