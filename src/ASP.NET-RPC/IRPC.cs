using Microsoft.AspNetCore.Builder;

namespace RpcMid {

    public static class IRPC {

        public static IApplicationBuilder UseHttpTransport(this IApplicationBuilder appBuilder) {
            appBuilder.UseMiddleware<HttpRpcMiddleware>();
            return appBuilder;
        }

        public static IApplicationBuilder UseWsTransport(this IApplicationBuilder appBuilder) {
            appBuilder.UseWebSockets();
            appBuilder.UseMiddleware<WsRpcMiddleware>();
            return appBuilder;
        }

    }

}