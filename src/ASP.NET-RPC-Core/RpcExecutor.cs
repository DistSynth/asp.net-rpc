using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AspNet.RPC.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AspNet.RPC.Core {

}

namespace AspNet.RPC {

    public class RpcExecutor {

        private IEnumerable<IRpcService> _services;
        private readonly ILogger<RpcExecutor> _logger;

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MethodInvoker>> _methods =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, MethodInvoker>>();

        private readonly ConcurrentDictionary<string, object> _instances = new ConcurrentDictionary<string, object>();

        public RpcExecutor(IEnumerable<IRpcService> services, ILogger<RpcExecutor> logger) {
            _services = services;
            _logger = logger;
        }

        public async Task<object> ProcessRpcRequest(string service, JToken json) {
            _logger.LogInformation("Processing request...");
            var request = json.ToObject<RpcRequest>();
            var methodName = request.Method.ToLower();

            var inst = _instances[service];
            var method = _methods[service][methodName];
            var resp = new RpcResponse {
                Id = request.Id,
                Result = await method.Invoke(inst, request.Params as JToken)
            };

            return resp;
        }

        public void Init() {
            _logger.LogInformation("Initialize RPC executor...");

            foreach (var rpcService in _services) {
                var svcType = rpcService.GetType();
                var typeName = svcType.Name;
                _logger.LogInformation(typeName);
                _methods[typeName] = BuildService(svcType);
                _instances[typeName] = rpcService;
            }
        }

        private ConcurrentDictionary<string, MethodInvoker> BuildService(Type type) {
            var handlers = new ConcurrentDictionary<string, MethodInvoker>();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                              .OrderByDescending(t => t.DeclaringType == type);
            var interfaces = type.GetInterfaces();

            var serializer = JsonSerializer.Create();

            var duplicateMethod = methods.GroupBy(t => Tuple.Create(t.Name, t.DeclaringType))
                                         .FirstOrDefault(t => t.Count() > 1);
            if (duplicateMethod != null) {
                var methodInfo = duplicateMethod.ToList().First();
                throw new Exception($"Method with name {methodInfo.Name} already exist in type {type}");
            }

            List<IGrouping<string, MethodInfo>> methodInfos = interfaces.SelectMany(
                                                                                    i => i.GetMethods(BindingFlags.Public | BindingFlags.Instance)).ToList()
                                                                        .GroupBy(t => t.Name.ToLower()).ToList();

            var duplicateInterfaceMethod = methodInfos.FirstOrDefault(t => t.Count() > 1);
            if (duplicateInterfaceMethod != null) {
                var methodInfo = duplicateInterfaceMethod.ToList().First();
                throw new Exception($"Method with name {methodInfo.Name} already exist in interfaces {type}");
            }

            var interfaceMethodsMap = methodInfos.ToDictionary(t => t.Key,
                                                               t => t.OrderByDescending(s => s.DeclaringType == type).FirstOrDefault());

            foreach (var method in methods.OrderByDescending(t => t.DeclaringType == type)) {
                //var attribute = method.GetCustomAttributes(typeof(JRpcMethodAttribute), false).SingleOrDefault() as JRpcMethodAttribute;
                var methodName = method.Name.ToLower();

                if (handlers.ContainsKey(methodName) && method.DeclaringType != type) {
                    continue;
                }

                interfaceMethodsMap.TryGetValue(methodName, out var interfaceMethodInfo);
                var methodInfo = interfaceMethodInfo ?? method;
                handlers[methodName] = new MethodInvoker(methodInfo, serializer);
            }

            return handlers;
        }

        private class MethodInvoker {

            private readonly JsonSerializer _jsonSerializer;
            private readonly Func<object, JToken, object> _delegate;
            private readonly ParameterInfo[] _parameters;

            public MethodInvoker(MethodInfo methodInfo, JsonSerializer jsonSerializer) {
                _jsonSerializer = jsonSerializer;
                _parameters = methodInfo.GetParameters();

                var instance = Expression.Parameter(typeof(object), "instance");
                var jToken = Expression.Parameter(typeof(JToken), "jToken");

                _delegate = Expression
                            .Lambda<Func<object, JToken, object>>(CreateCall(instance, jToken, methodInfo), instance, jToken)
                            .Compile();
            }

            public async Task<object> Invoke(object instance, JToken parameters) {
                var res = _delegate(instance, parameters);
                if (res is Task task) {
                    return task;
                }

                return res;
            }

            private static T GetArg<T>(
                JToken j, int i, IReadOnlyList<ParameterInfo> parameters,
                JsonSerializer jsonSerializer) {
                var jObj = j as JObject;
                JToken value;
                ParameterInfo parameterInfo;
                if (jObj != null) {
                    parameterInfo = parameters[i];
                    value = jObj[parameterInfo.Name];
                } else {
                    var jArr = (JArray) j;
                    parameterInfo = parameters[i];
                    value = jArr[i];
                }

                if (value != null) {
                    return value.ToObject<T>(jsonSerializer);
                }

                if (!parameterInfo.IsOptional) {
                    throw new Exception($"Not found expectedparams with name {parameterInfo.Name}");
                }

                if (parameterInfo.HasDefaultValue) {
                    return (T) parameterInfo.DefaultValue;
                }

                return default(T);
            }

            private Expression CreateCall(Expression instance, Expression jToken, MethodInfo methodInfo) {
                var getArg = typeof(MethodInvoker).GetMethod("GetArg", BindingFlags.NonPublic | BindingFlags.Static);
                var paramsExpressions = _parameters.Select((p, i) => {
                    var getArgTyped = getArg.MakeGenericMethod(p.ParameterType);
                    return Expression.Call(getArgTyped, jToken, Expression.Constant(i),
                                           Expression.Constant(_parameters), Expression.Constant(_jsonSerializer));
                });
                var callExpression = Expression.Call(Expression.Convert(instance, methodInfo.ReflectedType), methodInfo,
                                                     paramsExpressions);
                if (methodInfo.ReturnType == typeof(void)) {
                    return Expression.Block(callExpression, Expression.Constant(null));
                }

                return Expression.Convert(callExpression, typeof(object));
            }

        }

    }

}