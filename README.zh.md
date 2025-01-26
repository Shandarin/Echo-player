[English](README.md) | [中文](README.zh.md) 

# Echo Player

Echo Player 是一个基于 WPF 的视频播放器，旨在通过观看视频简化语言学习。

## 测试语言矩阵
| 母语\学习语言  | 英语       | 中文       |
|:---------|------------|------------|
| 英语       |            |            |
| 中文       | 已测试     |            |

## 功能
### 字幕功能
- 点击单词即可查询翻译
- 基于 AI 的句子分析
- 鼠标悬停显示字幕

### 生词本
- 收集并保存单词和句子以供复习

### 系统要求
- Windows 11

### 快捷键
- 空格键：播放/暂停
- 左/右方向键：后退/前进
- 上/下方向键：音量控制
- Esc 键：退出全屏

### API
- 牛津词典：单词查询
- OpenAI：句子分析
- 使用您自己的 API 密钥
- 或使用我们的免费 API 服务（目前无需费用）

### 开发
使用以下技术构建：
- WPF  
- LibVLCSharp  
- CommunityToolkit.Mvvm  
- System.Data.SQLite  
- SubtitlesParser  
- Newtonsoft.Json  
- Mkvtoolnix  

### 许可证
MIT 许可证