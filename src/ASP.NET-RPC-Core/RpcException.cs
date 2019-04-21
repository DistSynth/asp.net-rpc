using System;
using Newtonsoft.Json;

namespace AspNet.RPC.Core {

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class RpcException : ApplicationException {

        private readonly string _stacktrace;

        public RpcException(string message) {
            this.message = message;
        }


        public RpcException() {
        }


        [JsonProperty]
        public string message { get; set; }

        [JsonProperty]
        public int code { get; set; }

        public override string ToString() {
            return $"{base.ToString()}, RpcExceptionMessage = {message}";
        }

    }

}