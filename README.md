# MSLX.Plugin.DDNS

MSLX 动态域名解析 (DDNS) 插件。

## 功能特性

- 支持 IPv4 和 IPv6 双栈动态解析，支持独立配置与开关。
- 内置多种公网 IP 获取方式：
  - API 接口获取（默认提供 IPv4 及 IPv6 API，支持自定义接口）。
  - 本地物理/虚拟网卡直接获取（智能过滤回环及无效地址）。
  - 支持强制指定自定义 IP。
- 广泛的 DNS 服务商支持：
  - 腾讯云 (Tencent Cloud)
  - 阿里云 (Alibaba Cloud)
  - DNSPod (基于 Token)
- 智能同步机制：
  - 支持自定义 1-30 分钟轮询同步间隔。
  - 自动检测 IP 变动，仅在 IP 发生变化时才请求服务商 API，减少接口调用。
- 深度集成 MSLX 控制台，提供实时状态监控和可视化的操作界面。

## 编译与配置

1. 本项目采用 ILRepack 合并依赖包（如 Aliyun / Tencent SDK），以保证插件在 MSLX 容器中独立加载无冲突。
2. 将编译后生成的 `MSLX.Plugin.DDNS.dll` 放置到 MSLX 插件目录。
3. 在前端设置界面中，选择对应的服务商并填写对应的 Access Key 与 Secret Key（或 Token）即可开始使用。