using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RpcMid {

    public class HttpRpcMiddleware {

        private readonly RequestDelegate _next;
        private readonly RpcExecutor _rpcExecutor;
        private readonly ILogger<RpcExecutor> _logger;

        public HttpRpcMiddleware(RequestDelegate next, RpcExecutor rpcExecutor, ILogger<RpcExecutor> logger) {
            _next = next;
            _rpcExecutor = rpcExecutor;
            _logger = logger;
        }

        public async Task Invoke(HttpContext ctx) {
            var httpRequest = ctx.Request;
            if (!httpRequest.Method.Equals("POST", StringComparison.InvariantCultureIgnoreCase)) {
                await _next.Invoke(ctx);
                return;
            }

            var path = httpRequest.Path.Value.Trim('/');

            try {
                JToken json;
                using (var jsonReader = new JsonTextReader(new StreamReader(httpRequest.Body))) {
                    json = await JToken.ReadFromAsync(jsonReader).ConfigureAwait(false);
                }

                var res = JToken.FromObject(await _rpcExecutor.ProcessRpcRequest(path, json).ConfigureAwait(false));
                ctx.Response.ContentType = "application/json";
                using (var jw = new JsonTextWriter(new StreamWriter(ctx.Response.Body))) {
                    await res.WriteToAsync(jw).ConfigureAwait(false);;
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

        }

    }

}