# 路线记录功能

在 Test Street 的 Play 模式中，先点击 Game 画面，让游戏接收键盘输入。

- G 或 Xbox X：第一次开始，第二次停止并保存。下一次开始新记录，不覆盖旧文件。
- Quest Link：输入适配为左手 Touch 手柄 X；实际头显连接、XR 追踪与按键仍需实机联调。记录器不会主动切换原有移动模式，原来的 F2/F3 仍负责选择 VR 摇杆/位置偏移移动。
- 停止后点击 Open saved folder，可以打开本次保存目录。

默认 Windows 保存位置为当前用户的文档文件夹下 DM2350_Results。Unity 编辑器和 Windows 程序都直接保存到运行它们的电脑。Quest 独立运行时保存到应用数据目录，文件不会自动传到电脑。

在 Hierarchy 选择 Street Night Studio，可调整 Route Recorder 组件中的匿名参与者编号、组别、采样间隔、保存目录、地图范围和图片分辨率。Route Recorder Input 中可以调整键盘和 Xbox 的输入绑定。开发测试产生的数据不要当成正式参与者数据。

每次记录保存 trajectory.csv、session.json 和 route.png。图片中黄色为路线，绿色为起点，红色为终点；左上箭头指向世界坐标 +Z。底图由独立的正交摄像机从正上方拍摄，保留真实场景材质及停止记录时的灯光状态。路线在拍摄后叠加，不会被建筑遮住。修改街道范围后应同步检查 Map Bounds，确保覆盖全部可探索区域。

CSV 使用 UTC 时间和点号小数，记录玩家世界坐标、视点位置和前向方向、当前灯光、移动模式、暂停状态及视点来源。head_tracking_valid 在桌面模式为 not_applicable；VR 模式为 1/0，表示是否报告有效头显追踪，不等同于眼动追踪。

运行时定期刷新 trajectory.csv.partial 和 session.json。正常停止后生成正式 CSV，再导出图片。地图导出失败时仍保留 CSV 和 JSON，并显示原因。中途退出 Play 或关闭程序时尽力结束数据文件，不在退出过程中强行生成图片。崩溃或断电后可能保留 .partial，最多存在尚未刷新的数据，不能保证完全恢复最后一刻。

复位或大幅跳变会另起路线段，不在地图上连接穿越场景的直线。灯光和移动模式变化会写入 session.json 的 events。

该功能由独立组件组成；关闭 Route Recorder Input 和 Route Recorder 即可停用，不修改现有移动输入或灯光设置。
