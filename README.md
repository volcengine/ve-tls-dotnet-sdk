# VolcengineTLS

欢迎使用火山引擎日志服务（tls）SDK for .NET，本文档为您介绍如何获取及调用SDK。

## 前置准备

### 服务开通
请确保您已开通了您需要访问的服务。您可前往[火山引擎控制台](https://console.volcengine.com/ )，在左侧菜单中选择或在顶部搜索栏中搜索您需要使用的服务，进入服务控制台内完成开通流程。

### 获取安全凭证
Access Key（访问密钥）是访问火山引擎服务的安全凭证，包含Access Key ID（简称为AK）和Secret Access Key（简称为SK）两部分。您可登录[火山引擎控制台](https://console.volcengine.com/ )，前往“[访问控制](https://console.volcengine.com/iam )”的“[访问密钥](https://console.volcengine.com/iam/keymanage/ )”中创建及管理您的Access Key。更多信息可参考[访问密钥帮助文档](https://www.volcengine.com/docs/6291/65568 )。

### 环境检查

Windows
- 适用于 .NET Framework 4.6.2及以上
- 适用于 .NET Core 5.0-8.0版本

Linux/Mac
- 适用于 .NET Core 5.0-8.0版本

## 获取与安装

.NET Framework 4.6.2 SDK 1.0.1 托管在 NUGET 包管理平台，可通过 NUGET 安装。
https://www.nuget.org/packages/VolcengineTls

.NET Framework 4.6.2 SDK 1.0.2及后续版本托管在 NUGET 包管理平台，可通过 NUGET 安装。
https://www.nuget.org/packages/Volcengine.TLS.SDK.NetFramework

.NET Core SDK 托管在 NUGET 包管理平台，可通过 NUGET 安装。
https://www.nuget.org/packages/Volcengine.TLS.SDK.NetCore

## 相关配置
### 安全凭证配置

*注意：代码中Your AK及Your SK需要分别替换为您的AK及SK。*

**从环境变量加载AK/SK**：
  ```bash
  VOLCENGINE_ACCESS_KEY_ID="Your AK"
  VOLCENGINE_ACCESS_KEY_SECRET="Your SK"
  ```

## 使用示例
请看项目VolcengineTls.Example

---
## 快速入门

### 1. 前置准备

**服务开通**  
请确保您已开通要使用的火山引擎 TLS 服务，可前往火山引擎控制台开通。

**获取安全凭证**  
`Access Key`（访问密钥）包含 `Access Key ID (AK)` 和 `Secret Access Key (SK)`。可在火山引擎控制台的「访问控制」-「访问密钥」中创建与管理。

**环境检查**  
.Net Framework 确保运行环境已安装对应版本的 **.NET Framework (>= 4.6.2)**。

.Net Core 确保运行环境已安装对应版本的 **.NET SDK (>= net5.0)**。

### 配置说明

**VolcengineConfig 核心参数**  
可通过 **环境变量** 获取（例如：`VOLCENGINE_REGION`、`VOLCENGINE_ENDPOINT`、`VOLCENGINE_ACCESS_KEY_ID`、`VOLCENGINE_ACCESS_KEY_SECRET`、`VOLCENGINE_SECURITY_TOKEN`、`TOPIC_ID`）。

---

### 2. 初始化上报日志示例（`PutLogs`）

```csharp
using VolcengineTls.Request;
using VolcengineTls.Response;

namespace VolcengineTls.Examples
{
    public class PutLogsExample
    {
        public async Task RunAsync()
        {
            // 设置火山云权限配置
            var vcConfig = new VolcengineConfig(
                region: Environment.GetEnvironmentVariable("VOLCENGINE_REGION"),
                endpoint: Environment.GetEnvironmentVariable("VOLCENGINE_ENDPOINT"),
                accessKeyId: Environment.GetEnvironmentVariable("VOLCENGINE_ACCESS_KEY_ID"),
                accessKeySecret: Environment.GetEnvironmentVariable("VOLCENGINE_ACCESS_KEY_SECRET"),
                securityToken: Environment.GetEnvironmentVariable("VOLCENGINE_SECURITY_TOKEN")
            );

            // 实例化tls sdk client
            var client = new Client(vcConfig);

            // 构建日志数据
            var logGroupList = new Pb.LogGroupList();
            var logGroup = new Pb.LogGroup();

            // 单个logGroup不超过10000条
            var num = 100;
            while (num > 0)
            {
                var log = new Pb.Log
                {
                    Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                };

                var logContents = new List<Pb.LogContent>
                {
                    new Pb.LogContent { Key = "k1", Value = $"value{num}" },
                    new Pb.LogContent { Key = "k2", Value = $"value{num}" }
                };

                log.Contents.Add(logContents);

                logGroup.Logs.Add(log);

                num--;
            }

            logGroupList.LogGroups.Add(logGroup);

            // 构建请求
            var putLogsRequest = new PutLogsRequest(
                logGroupList: logGroupList,
                topicId: Environment.GetEnvironmentVariable("TOPIC_ID")
            );

            PutLogsResponse putLogsResponse = await client.PutLogs(putLogsRequest);
        }
    }
}
```

---

### 3. 初始化上报日志示例（`PutLogsV2`）

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VolcengineTls.Request;
using VolcengineTls.Response;

namespace VolcengineTls.Examples
{
    public class PutLogsV2Example
    {
        public async Task RunAsync()
        {
            // 设置火山云权限配置
            var vcConfig = new VolcengineConfig(
                region: Environment.GetEnvironmentVariable("VOLCENGINE_REGION"),
                endpoint: Environment.GetEnvironmentVariable("VOLCENGINE_ENDPOINT"),
                accessKeyId: Environment.GetEnvironmentVariable("VOLCENGINE_ACCESS_KEY_ID"),
                accessKeySecret: Environment.GetEnvironmentVariable("VOLCENGINE_ACCESS_KEY_SECRET"),
                securityToken: Environment.GetEnvironmentVariable("VOLCENGINE_SECURITY_TOKEN")
            );

            // 实例化tls sdk client
            var client = new Client(vcConfig);

            /// 构建日志数据（封装的Pb数据）
            var logs = new List<Log>();
            // 单个logGroup不超过10000条
            var num = 100;
            while (num > 0)
            {
                var log = new Log(
                    time: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    contents: new List<LogContent>{
                        new LogContent("k1", $"value{num}"),
                        new LogContent("k2", $"value{num}"),
                    }
                );

                logs.Add(log);

                num--;
            }

            // 构建请求
            var putLogsV2Request = new PutLogsV2Request(
                logs: logs,
                topicId: Environment.GetEnvironmentVariable("TOPIC_ID"),
                compressType: Consts.CompressLz4,
                hashKey: "",
                source: "your log source",
                fileName: "your log filename"
            );

            PutLogsResponse putLogsV2Response = await client.PutLogsV2(putLogsV2Request);
        }
    }
}
```

---

### 4. 初始化 Producer 上报日志示例

```csharp
using System;
using System.Threading;
using VolcengineTls.Producer;

namespace VolcengineTls.Examples
{
    public class DefaultCallBackImp : ICallBack
    {
        public void Fail(ProducerResult result)
        {
            Console.WriteLine("put log to tls failed");
        }

        public void Success(ProducerResult result)
        {
            Console.WriteLine("put log to tls success");
        }
    }

    public class ProducerExample
    {
        public void Run()
        {
            // 设置火山云权限配置
            var vc = new VolcengineConfig(
                region: Environment.GetEnvironmentVariable("VOLCENGINE_REGION"),
                endpoint: Environment.GetEnvironmentVariable("VOLCENGINE_ENDPOINT"),
                accessKeyId: Environment.GetEnvironmentVariable("VOLCENGINE_ACCESS_KEY_ID"),
                accessKeySecret: Environment.GetEnvironmentVariable("VOLCENGINE_ACCESS_KEY_SECRET"),
                securityToken: Environment.GetEnvironmentVariable("VOLCENGINE_SECURITY_TOKEN")
            );

            // 设置火山云配置，当前获取默认配置
            var config = new ProducerConfig(vc);
            // 实例化
            var producer = new ProducerImp(config);
            // 启动生产者
            producer.Start();

            /// 发送单条
            // 构造日志
            var keyNum = 2;
            var log = new Pb.Log
            {
                Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };

            for (var i = 0; i < keyNum; i++)
            {
                var logContent = new Pb.LogContent
                {
                    Key = $"key{i}",
                    Value = $"c#-value-test-{i}",
                };

                log.Contents.Add(logContent);
            }

            // 发送单条 或者使用SendLogV2
            producer.SendLog(
                topicId: Environment.GetEnvironmentVariable("TOPIC_ID"),
                log: log,
                source: "your log source",
                filename: "your log filename",
                shardHash: null,
                callback: new DefaultCallBackImp() // 不需要的话可以传null
            );

            /// 发送多条
            var logNum = 1000;
            var logGroup = new Pb.LogGroup();

            for (var j = 0; j < logNum; j++)
            {
                log = new Pb.Log
                {
                    Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
                };

                for (var i = 0; i < keyNum; i++)
                {
                    var logContent = new Pb.LogContent
                    {
                        Key = $"no-{j}-key{i}",
                        Value = $"no-{j}-c#-value-test-{i}",
                    };

                    log.Contents.add(logContent);
                }

                logGroup.Logs.Add(log);
            }

            producer.SendLogs(
                topicId: Environment.GetEnvironmentVariable("TOPIC_ID"),
                logs: logGroup,
                source: "your log source",
                filename: "your log filename",
                shardHash: null,
                callback: new DefaultCallBackImp() // 不需要的话可以传null
            );

            // 模拟其他任务处理
            Thread.Sleep(TimeSpan.FromSeconds(20));

            // 生产者关闭
            producer.Close();
        }
    }
}
```

---

### 5. 运行 example 示例

```csharp
using System;

namespace VolcengineTls.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting example...");

                var example = new ProducerExample();

                example.Run(); // 同步方法
                // example.RunAsync().Wait(); // 异步方法

                Console.WriteLine("Example completed successfully.");
            }
            catch (AggregateException ex)
            {
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Console.WriteLine($"Error: {innerEx.Message}");
                    if (innerEx.InnerException != null)
                    {
                        Console.WriteLine($"Inner Error: {innerEx.InnerException.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.log($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
```

---

## 异常处理

SDK 自定义异常 **`TlsError`** 包含以下核心属性，便于问题排查：

- `HttpCode`：HTTP 状态码
- `Code`：错误码（如 `"InvalidParameter"`）
- `ErrorMessage`：错误描述
- `RequestId`：请求 ID（用于火山引擎技术支持排查）

```csharp
try
{
    // 上传日志
}
catch (TlsError e)
{
    // 打印错误详情
    Console.WriteLine($"错误码：{ex.HttpCode}");
    Console.WriteLine($"错误码：{ex.Code}");
    Console.WriteLine($"错误描述：{ex.ErrorMessage}");
    Console.WriteLine($"请求ID：{ex.RequestId}");

    // 根据错误码处理重试逻辑
    if (ex.Code == "RequestTimeout"){
         // 自定义超时重试逻辑
    }
}
```

---

## 注意事项

- **敏感信息保护**：`AccessKeyId` 与 `AccessKeySecret` 请勿硬编码，建议通过环境变量或安全配置加载
- **日志格式**：日志内容的 `Key-Value` 需符合 TLS Topic 配置的字段类型（字符串 / 整数 / 浮点数等）
- **资源释放**：应用退出时，调用 `tlsClient.Dispose()` 释放连接池等资源
- **框架兼容性**：若项目使用 **.NET 5.0**，需确保运行环境已安装相应 **.NET SDK/Runtime**

---

## 许可证

本项目基于 **Apache License 2.0** 开源协议，详情见 `LICENSE`。
