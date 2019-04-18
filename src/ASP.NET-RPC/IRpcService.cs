using System;
using System.Threading.Tasks;

namespace RpcMid {

    public interface IRpcService {

    }

    public class FirstService : IRpcService {

        public async Task<string> Test(int first, int second) {
            //await Task.Delay(100);
            return (first + second).ToString();
        }

    }

    public class SecondService : IRpcService {

        public string Test() {
            return "SecondService Test invoked";
        }

    }

}