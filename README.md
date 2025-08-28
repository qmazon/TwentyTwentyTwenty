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
IntervalTime                = 00:20:00      # 两次休息的间隔时间
EscapeNextTime              = 00:02:00      # 强制退出休息后，下次休息的时间
RestTime                    = 00:00:20      # 休息的时间
FadeInTime                  = 00:00:01      # 淡入动画的时间
FadeOutTime                 = 00:00:00.8    # 淡出动画的时间
RestFinishedColorChangeTime = 00:00:00.8    # 即将退出休息时，颜色改变动画的时间
Invisibility                = 96            # 蒙板的遮盖程度
CountdownColor              = "Aqua"        # 倒计时的颜色
FailedColor                 = "OrangeRed"   # 强制退出休息时倒计时的颜色
SuccessColor                = "Gold"        # 倒计时正常结束的颜色

# 颜色表示方式：
# 1. Color = "Red"
# 2. Color = "#FFFF0000"
# 3. Color = 0xFFFF0000
# 方法1.的可用颜色列表：参见https://learn.microsoft.com/en-us/dotnet/media/art-color-table.png
#
# Invisibility 应该是一个介于0到255（包含）的整数。您也可以写作0x00到0xFF。
# 
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