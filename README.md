# TwentyTwentyTwenty

> 每 20 分钟提醒休息 20 秒的护眼小工具，使用 GPLv3 协议。

## 功能一览
- 每 20 分钟弹出一次灰度蒙板，倒计时 20 秒后自动淡出
- 通过系统托盘图标可随时触发一次休息或退出程序
- 单实例运行，防止重复启动

## 退出
- 在灰度蒙板上按 `Ctrl+Alt` 立即开始淡出并关闭蒙板，程序继续运行
- 右键托盘图标 → 退出程序

## 自定义配置
首次启动后，程序会在`%APPDATA%\TwentyTwentyTwenty\settings.toml`自动生成配置文件，内容如下：

```toml
interval_minutes = 20   # 两次休息间隔（分钟）
rest_seconds     = 20   # 每次休息倒计时（秒）
fade_in_seconds  = 1.5  # 淡入动画时长（秒）
fade_out_seconds = 1.5  # 淡出动画时长（秒）
```

修改并保存后，重启生效。

## 运行
前往[Releases](./../../releases)界面下载最新版本，启动即可。**需要`.NET9`运行时。**

### 编译
```bash
git clone https://github.com/qmazon/TwentyTwentyTwenty.git
```
用 Visual Studio 打开` TwentyTwentyTwenty.sln`，按 F5 直接运行（x64）。


## License
GPL-3.0