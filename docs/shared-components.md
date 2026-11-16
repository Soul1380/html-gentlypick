# 通用组件与扩展方式（v1.1.0）

基础组件位于 `app/ui-components.js`，统一样式位于 `app/ui-components.css`。产品组合位于 `app/ui-workflows.js`。桌面路径窗口与顶部按钮位于 `desktop/SharedControls.cs`。

## 基础、配置与组合

```js
// 同一基础路径框，文件夹和 Python 文件通过配置区分。
GPUI.path(input, {kind: 'folder', title: '选择导出目录'});
GPUI.path(pythonInput, {
  kind: 'file', title: '选择 Python 解释器',
  filter: 'Python (python.exe)|python.exe'
});

// 特殊选项加在子配置中；基础搜索、键盘和返回行为不复制。
// 在将该 select 插入页面之前注册。
GPUI.variant('customModelSelect', {
  title: '模型',
  onSelect: value => applyModel(value),
  actions: [{label: '检测运行环境', run: () => checkEnvironment()}]
});

// 独立业务组合成二级窗口，公共标题、返回和 Esc 由基础组件承担。
const window = GPUI.dialog({
  title: '导出设置', body: exportSettings,
  actions: [{label: '开始导出', primary: true, run: d => exportAndClose(d)}]
});
window.showModal();
```

无需建立多层继承树。样式、路径桥接、返回与键盘行为在基础组件维护；文件类型、动作、标题通过配置扩展；项目分组选择、模型管理和视频分析等独立业务通过组合实现。

普通单选框默认保留原生下拉，仅模型等显式配置 `GPUI.variant` 或标注 `data-ui-picker` 的字段使用可搜索的大窗口。统一规范不代表所有控件都弹窗。所有文件输入框共用右侧文件夹浏览入口（支持 Enter），并保留浏览器的原生文件类型约束。路径字段在 `folder-picker.js` 中登记字段及其类型。

## 状态与草稿

绿色：可用、已完成；黄色：待导入、等待；蓝色：处理中；红色：失败；灰色：尚未连接、待检测。同时保留状态文字，不依赖颜色辨认。

页面草稿只保存在当前页面运行内存中。侧栏“本地模型”在右侧主区域显示；页面内“管理模型”覆盖显示而不替换调用页面，二者使用独立容器。窗口返回、侧栏切换、后台数据刷新后保留输入；关闭应用会清除未保存草稿和密钥。

## 数据与安装

项目分组选择支持最多 1000 篇文字摘录，明确选择的项目不会因原 20 篇限制被截断。沿用每篇输入最多 8000 字的文本上限，AI 调用按最多 3 篇、约 12000 字分批处理。单次自动任务仍保留原输入保护限制。

文件夹浏览使用 Windows Common Item Dialog（带地址栏、可调整大小）；文件浏览使用升级版 OpenFileDialog。安装程序、导入旧资料和工作台使用同一实现。打开模型位置使用固定的资源管理器操作，不接收任意命令。

`build-desktop.ps1 -ReuseDependenciesFrom <现有完整软件目录>` 可以复用相同版本的运行库，避免反复下载。构建仍检查 Node 版本及视频工具 SHA-256。

## 视觉与滚动

工作台、模型与视频分析共用 `ui-theme.css` 的字体色板，内嵌页面由 `embedded-ui.css` 统一卡片、表单和间距，不再复制颜色。`ui-scrollbars.js/css` 提供细圆角悬浮滚动条，滚动后显示，停止约 1.1 秒后淡出；保留拖动、横向滚动、键盘和减少动态效果设置，弹窗内滚动条归属对应弹窗。普通少量选项仍保留原生下拉。
