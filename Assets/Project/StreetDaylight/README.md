# 白天街道配置

场景：`Assets/Project/Scenes/Test Street Daylight.unity`。

- `Sun - daylight`：控制太阳方向、强度和阴影；第一版为50度高度的日间光照。
- Lighting/Environment：使用 `Street Daylight Sky`，复用Start场景的蓝天贴图；天空、地平线和地面环境光提供阴影内的基础可见度。
- `Street Daylight Studio` / Volume：使用独立的 `Street Daylight Look`，固定曝光、Neutral色调映射，无夜间Bloom。
- 同一物体上的Scene Render Pipeline Override使用独立的 `Street Daylight URP` 和Renderer，避免更改夜景共享资产。
- Street Lighting Conditions使用 `E_DaylightWithShops`：24盏路灯关闭，住宅窗户自发光关闭。2处商店通过橱窗商品贴图及招牌的可见自发光呈现明亮营业状态，原有纹理细节保留。
- `Occupied windows and shops - fixed seed 2350001` 下原有4个门口补光均关闭，并从白天控制器的灯光列表中移除。商店模型没有实际室内空间，当前效果由橱窗/室内展示贴图发光表达，不使用门口光斑代替营业照明。
- 白天专用材质副本仅由白天Scene引用，原夜景材质不修改。

这是可继续调节的日光第一版，采用实时太阳、天空色环境补光和实时环境反射；未生成烘焙的间接光或完整物理照度校准。桌面画面和移动通过独立工程验证，Quest实际显示和性能仍需头显检查。

移动初始化已支持Start、Start Night、旧Test Street及三个新场景名，在运行中加载新场景也会初始化。默认电脑模式保持不变，WASD/手柄移动，鼠标右键/右摇杆观察；F2/F3为现有VR控制模式。
