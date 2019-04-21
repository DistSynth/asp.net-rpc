using Newtonsoft.Json;

namespace AspNet.RPC.Core {

    [JsonObject(MemberSerialization.OptIn)]
    public class RpcRequest {

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public object Params { get; set; }

        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";

    }

}