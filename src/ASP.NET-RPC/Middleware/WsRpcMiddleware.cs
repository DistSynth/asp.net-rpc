using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AspNet.RPC.Middleware {

    public class WsRpcMiddleware {

        private readonly RequestDelegate _next;
        private readonly RpcExecutor _rpcExecutor;

        public WsRpcMiddleware(RequestDelegate next, RpcExecutor rpcExecutor) {
            _next = next;
            _rpcExecutor = rpcExecutor;
        }

        public async Task Invoke(HttpContext ctx) {
            if (ctx.WebSockets.IsWebSocketRequest) {
                var webSocket = await ctx.WebSockets.AcceptWebSocketAsync();
                await ProcessRpcRequest(ctx, webSocket);
                return;
            }

            await _next(ctx);

        }

        private async Task ProcessRpcRequest(HttpContext context, WebSocket webSocket) {
            var res = await ReceiveFullMessage(webSocket, CancellationToken.None);
            while (!res.Item1.CloseStatus.HasValue) {
                using (var jsonReader = new JsonTextReader(new StreamReader(new MemoryStream(res.Item2.ToArray())))) {
                    var json = await JObject.LoadAsync(jsonReader);
                    var path = context.Request.Path.Value.Trim('/');

                    json = JObject.FromObject(await _rpcExecutor.ProcessRpcRequest(path, json));

                    var resMS = new MemoryStream();
                    using (var jsw = new JsonTextWriter(new StreamWriter(resMS))) {
                        await json.WriteToAsync(jsw);
                    }

                    var arr = resMS.ToArray();

                    await webSocket.SendAsync(new ArraySegment<byte>(arr, 0, arr.Length), WebSocketMessageType.Text,
                        true, CancellationToken.None);
                }


                res = await ReceiveFullMessage(webSocket, CancellationToken.None);
            }

            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }

        private async Task<(WebSocketReceiveResult, IEnumerable<byte>)> ReceiveFullMessage(
            WebSocket socket, CancellationToken cancelToken) {
            WebSocketReceiveResult response;
            var message = new List<byte>();

            var buffer = new byte[4096];
            do {
                response = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken);
                message.AddRange(new ArraySegment<byte>(buffer, 0, response.Count));
            } while (!response.EndOfMessage);

            return (response, message);
        }

    }

}