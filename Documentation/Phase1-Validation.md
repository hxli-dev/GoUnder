# Phase 1 技术稳定化：验证与交接

基线：`51f9c8633d04a948bdc08de486ed1bbb8c01a2c4`。所有修复位于 `polish`；没有升级 Unity、URP，没有修改谜题答案、对白、关卡布局或美术风格。

## 已实现的行为

- 为 URPWater 和两处 Quixel 工具的 UnityEditor using 加入编辑器条件编译。
- 新游戏只重置游戏进度，保留设置；菜单淡出从点击开始计算，完成后 Single 加载。
- Continue 按明确楼层名加载，支持三个楼层入口检查点。检查点保存入口背包快照，继续时回放本层；不宣称恢复到任意中途位置或谜题状态。
- 物品以现有 `.meta` GUID 作为稳定 ID；读取兼容旧显示名与文件名，包括 Communication Code。Evelyn 使用非空名称。
- 背包只初始化一次、按物品数据引用清除已收集对象、去重并即时保存。容量不足时不销毁拾取物。一楼不再清除物品键。
- 设置独立默认值、初始化 inactive 设置页、滑条即时生效；镜头解锁恢复已保存的灵敏度。
- 二楼相机、Volume 和音频指向经过场景核对的现有对象；一楼亮度字段接回现有 Global Volume。保留已有非空背景音分组并纳入实际 BGM/环境音。
- 数据库显式区分旧连续楼层门与当前分场景流程。当前二楼没有 F1DoorOpen 控制器对象，因此不虚构或替换另一扇门。旧场景仍保留原有门功能；二楼使用已有的电话/视频转场。
- 视频预备有超时与错误处理，错误会记录并进入下一叙事节点；成功播放仍使用原有 50/28/26/55 秒时间。流程禁止重复启动。
- 修复电脑/通讯镜头移动中 Escape 导致的中间状态、电视操作期间暂停/背包互相覆盖、冻结期间重力累计、通讯加载条再次打开不重置等问题。
- 从历史恢复 toypig.fbx、INVENTORY.png、testtext.png 原始文件及其原 GUID。没有以新素材替代。
- 锁文件对齐原 manifest：Timeline 1.4.8，URP 所需 Core/ShaderGraph 10.5.1。

## 已验证与限制

执行环境已恢复基线 3,436 条 LFS 资源路径（3,206 个去重对象），逐路径 SHA-256 通过；另恢复三个历史资源。27 个 registry 包下载与校验通过。所有既有 6,062 个 `.meta` 内容不变。

65 项静态断言通过；15 项使用内存替身的独立 C# 逻辑断言通过。224 个 C# 文件以 C# 8 / Player 编译符号完成语法解析，未发现语法错误或运行时文件中的有效 UnityEditor using。

以上不是 Unity API 编译、场景导入或实机通关。原版 Unity 安装器退出码 2，明确要求管理员权限；本环境没有 2020.3.26f1 编辑器。没有使用 6000 系列打开本项目，没有产生 Windows 游戏构建。

仍有两个找不到历史原件的资源 GUID：

- 三楼 `e6a2779af045a49488e81065899823d4`：旧 prefab 来源，场景中 141 处引用；572 个 main 历史提交未找到对应 `.meta`。
- 二楼 `d3e751979bdfd4f44ac8ae7d2ed8c214`：left_profile_bar SpriteRenderer 的图片；同样没有找到历史 `.meta`。

不要为了通过验证删除来源记录或随意换图。应使用目标 Unity 检查实际影响，并找回原件。

## 原版编辑器可用后的命令

从项目根目录，使用 **2020.3.26f1** 编辑器：

```powershell
& $unity2020 -batchmode -quit -projectPath $PWD -executeMethod Phase1Validation.Validate -logFile Logs/phase1-import.log
& $unity2020 -batchmode -quit -projectPath $PWD -executeMethod Phase1Validation.BuildWindows -logFile Logs/phase1-build.log
```

`$unity2020` 必须是该版本真实存在的 Unity.exe 路径。验证工具拒绝其他版本；菜单 `Tools/Phase 1/Validate build scenes` 也可调用。工具恢复原场景设置、不保存场景。Windows 目标是 x86_64 Development，输出 `Builds/Windows/Deep Blue Requiem.exe`；不存在该文件就不能报告构建成功。

独立逻辑测试（需要 .NET 10 SDK，不是项目运行依赖）：

```powershell
dotnet run --project Tools/Phase1/LogicTests/LogicTests.csproj
```

## 手工测试清单

下列项目均为 **Not Tested**，完成实测后再改为 Passed / Failed，并附构建提交、日志与步骤。

| 项目 | 操作与预期 | 状态 |
|---|---|---|
| 原版导入 | 2020.3.26f1，包解析完成；无编译错误、Missing Script 或未解释的断引用 | Not Tested |
| Windows 构建 | x86_64 Development 产生 EXE 和 Data 文件夹 | Not Tested |
| 独立启动 | 关闭 Unity，直接启动 EXE，显示菜单；收集 Player.log | Not Tested |
| 全新用户 | 无进度启动；分别先打开音量、控制、亮度，均有各自默认值 | Not Tested |
| 设置保留 | 调整三项设置，返回菜单、新游戏、关闭重启，设置不被重置 | Not Tested |
| 三楼 | 移动、奔跑、E 交互、抽屉、物品拾取、Tab、文件/模型预览、门拖拽正常 | Not Tested |
| 三楼电话 | 接听完整电话，交互锁正常释放；退出电视不覆盖电话限制 | Not Tested |
| 三楼→二楼 | 视频只启动一次，正常结束后载入二楼并初始化正确 | Not Tested |
| 二楼设置 | 相机、音量、亮度均即时生效，无 NullReference | Not Tested |
| 数据库 | 按现有两阶段答案完成，邮件、加载页、通讯码出现；不调用不存在的旧门 | Not Tested |
| 通讯 | 错误/正确输入、第二次尝试、Escape、信号条/NoResponse 正常 | Not Tested |
| 镜头中退出 | 电脑/通讯进入第一秒按 Escape 不产生卡住，稳定后退出能恢复移动 | Not Tested |
| 二楼门/电话 | 证据物品拖拽可解锁，电话结束后按原流程触发海啸与转场 | Not Tested |
| 二楼→一楼 | 视频结束后初始化一楼；已有物品键不被删除 | Not Tested |
| 一楼/结局 | 录音带→投影→Evelyn 文件→最终门→结局→菜单，顺序不变 | Not Tested |
| 三层检查点 | 每层中途退出并重启 Continue，回本层入口和入口物品；本层谜题可重新完成 | Not Tested |
| 背包幂等 | 反复打开关闭不重复添加；同一物品不重复收集；存档恢复不依赖 GameObject 名称 | Not Tested |
| 错误视频 | 在隔离测试副本模拟缺片/解码失败，日志可定位且不会永久锁住 | Not Tested |
| 暂停 | 预览/电话/电视/电脑前后暂停行为合理；返回菜单 timeScale 恢复 | Not Tested |
| 分辨率 | 至少 1280×720、1920×1080 和非 16:9；关键按钮和字幕可见 | Not Tested |

旧存档只保留了部分物品和一个二楼标志，无法精确重建过去的门、电话和位置状态。迁移能读取旧物品名称和旧楼层入口，但仍需实测，不把历史丢失的状态宣称为已恢复。完成原版构建、缺失资源核验及主线通关后，才进入下一阶段视觉审查。
