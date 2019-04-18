# ASP.NET JRPC

[![Build Status](https://travis-ci.org/DistSynth/asp.net-rpc.svg?branch=master)](https://travis-ci.org/DistSynth/asp.net-rpc)
[![NuGet Version](https://badge.fury.io/nu/asp.net-rpc.svg)](https://badge.fury.io/nu/asp.net-rpc)

Переработанная версия JRPC для ASP.NET Core.

```
+-----------------+   +---------------+   +--------------+
|                 |   |               |   |              |
| HTTP Middleware |   | WS Middleware |   | MQ Transport |
|                 |   |               |   |              |
+--------+--------+   +-------+-------+   +-------+------+
         |                    |                   |
         |                    |                   |
         |                    |                   |
         |            +-------v--------+          |
         |            |                |          |
         +------------>  RPC Executor  <----------+
                      |                |
                      +--------+-------+
                               |
                               |
                               |
                        +------v------+
                        |             |
                        | RPC Service |
                        |             |
                        +-------------+
```