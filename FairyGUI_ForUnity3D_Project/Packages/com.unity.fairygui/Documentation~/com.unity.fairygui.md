# Fairy GUI

## TextMeshPro 配置

具体见 [字体](https://fairygui.com/docs/editor/font)

- 安装 `TextMeshPro`（`package.json` 目前预置了对 `TextMeshPro 3.0.7` 的依赖）
- 在 Unity 中为 `项目设置/其他设置/脚本编译/脚本定义符号` 添加宏 `FAIRYGUI_TMPRO`
- 配置预设的字体（在 `com.unity.fairygui/Runtime/TextMeshProAssets/Resources/Fonts/` 文件夹下已经准备了黑体作为预设字体）。在主场景加入 `FairyGUI.UIConfig` 组件，设置缺省字体名称即可
- 新增字体时可用 [常用字库](./Resoures/常用字库.txt) 的内容。