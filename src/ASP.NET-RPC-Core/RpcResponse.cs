using Newtonsoft.Json;

namespace AspNet.RPC.Core {

    [JsonObject(MemberSerialization.OptIn)]
    public class RpcResponse {

        [JsonProperty(PropertyName = "jsonrpc")]
        public string JsonRpc => "2.0";

        [JsonProperty(PropertyName = "result")]
        public object Result { get; set; }

        [JsonProperty(PropertyName = "error")]
        public RpcException Error { get; set; }

        [JsonProperty(PropertyName = "id")]
        public object Id { get; set; }

    }

    [JsonObject(MemberSerialization.OptIn)]
    public class RpcResponse<T> {

        [JsonProperty(PropertyName = "jsonrpc")]
        public string JsonRpc => "2.0";

        [JsonProperty(PropertyName = "result")]
        public T Result { get; set; }

        [JsonProperty(PropertyName = "error")]
        public RpcException Error { get; set; }

        [JsonProperty(PropertyName = "id")]
        public object Id { get; set; }

    }

}