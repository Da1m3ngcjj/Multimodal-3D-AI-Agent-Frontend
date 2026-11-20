# Multimodal-3D-AI-Agent-Frontend
多模态3D AI交互项目，Unity前端 + Python后端Agent，支持LLM驱动3D场景交互。

## 🎬 项目演示视频
[点击这里观看 1 分钟 Demo 演示](https://b23.tv/gsq9z14)

## ✨ 项目简介
Unity 3D交互客户端，可和后端FastAPI大模型服务通信，实现自然语言驱动场景内物体、角色交互。
> 本仓库为 Unity 前端工程，后端代码单独仓库。

## 📦 环境与依赖
- Unity 版本：2022.3.62
- 包管理：UPM（Package Manager）
- Git LFS：本项目模型、贴图、prefab、场景资源使用 Git LFS 托管
- 唤起Blender需要以下配置:Assets/Script/ChatController.cs
```csharp
    //替换Blender安装路径
    string blenderPath = @"Z:\Program Files\Blender Foundation\Blender 5.3\blender-launcher.exe"; 
    

### 第三方依赖包
- https://www.reallusion.com/auto-setup/unity/download.html?autoOpenForm=cc-unity-auto-setup-in-unity
- ccic_unity_tools-2_2_6
- 其他开源UPM包（打开Unity会自动从manifest.json拉取）

## 🚀 本地部署步骤
1. 克隆仓库
```bash
git clone https://github.com/Da1m3ngcjj/Multimodal-3D-AI-Agent-Frontend
2. 依赖后端仓库
git clone https://github.com/Da1m3ngcjj/Multimodal-3D-AI-Agent-Backend

⚠️ 克隆前请确认本地已安装 Git LFS，否则模型资源会下载失败
