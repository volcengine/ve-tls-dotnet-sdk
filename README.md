# VolcengineTLS

欢迎使用火山引擎日志服务（tls）SDK for .NET，本文档为您介绍如何获取及调用SDK。

## 前置准备

### 服务开通
请确保您已开通了您需要访问的服务。您可前往[火山引擎控制台](https://console.volcengine.com/ )，在左侧菜单中选择或在顶部搜索栏中搜索您需要使用的服务，进入服务控制台内完成开通流程。

### 获取安全凭证
Access Key（访问密钥）是访问火山引擎服务的安全凭证，包含Access Key ID（简称为AK）和Secret Access Key（简称为SK）两部分。您可登录[火山引擎控制台](https://console.volcengine.com/ )，前往“[访问控制](https://console.volcengine.com/iam )”的“[访问密钥](https://console.volcengine.com/iam/keymanage/ )”中创建及管理您的Access Key。更多信息可参考[访问密钥帮助文档](https://www.volcengine.com/docs/6291/65568 )。

### 环境检查

Windows

适用于 .NET Framework 4.6.2及以上

## 获取与安装

SDK 托管在 NUGET 包管理平台，可通过 NUGET 安装。

https://www.nuget.org/packages/VolcengineTls

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
