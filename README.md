# STranslate.Plugin.Translate.MerriamWebster

<div align="center">

**适用于 STranslate 的 Merriam-Webster 英英词典插件**

基于 [dictionaryapi.com](https://dictionaryapi.com/) 官方 API 开发，在 STranslate 里查单词时直接给出英英释义、音标、真人发音、派生词与词形变化 —— 排版上参考 STranslate 内置必应词典：例句单独成栏、复数与过去式等词形变化填到对应卡片，不再与释义混在一起。

</div>

---

---

## ⚠️ 关于「英汉」：dictionaryapi.com 不提供中文

**这个 API 全系没有中文词典。** 官方提供的 9 个参考库只有「英英」和「英西」两类：

| 参考库 | 类型 | 本插件支持 |
| --- | --- | :---: |
| Collegiate | 英英（单语） | ✅ |
| Learner's | 英英（单语，释义更简单） | ✅ |
| Medical | 英英（医学） | ✅ |
| Elementary / Intermediate / School | 英英（分级读物） | — |
| Collegiate / Intermediate Thesaurus | 英文同义词 | — |
| **Spanish-English** | **英 ⇄ 西 双语（双向）** | ✅ |

所以：

- 想要**英汉**，这个 API 做不到，得换数据源（如有道、彩云小译、腾讯 TMT 等翻译 API，或 ECDICT 之类的词库）。
- 想要**英西**，选 `Spanish-English (英西双语)` —— 这是本插件里唯一的双语引擎，查英文词返回西班牙语对译词，并带 **gender 标注**（`masculine` / `feminine`）与**例句译文**。

### 英西库长这样

以 `language` 为例，插件会渲染成：

```text
language  /ˈlæŋɡwɪdʒ/  🔊
noun
  idioma (masculine), lengua (feminine)
  lenguaje (masculine)

例句
  • the English language → el idioma inglés
  • body language → lenguaje corporal
```

其中 `(masculine)` 来自响应里的 `gl` 字段、`→ el idioma inglés` 来自例句里的 `tr` 字段 —— 这两项是英西库区别于英英库的核心信息，插件已专门解析。例句单独成栏，定义行不再被 `例: ...` 内联打乱。

### 1.0.3 之后：例句与词形变化拆开

从 **1.0.3** 起，释义区、**例句区**、**词形变化卡片**互不混在一起 —— 学习必应词典对 `DictionaryResult` 各字段的处理方式：

| 数据 | 落点 | 在宿主里的呈现 |
| --- | --- | --- |
| 音标 / 发音 | `result.Symbols[].Phonetic` / `AudioUrl` | 词头旁边的 `/ˈθrɛʃhoʊld/` + 🔊 |
| 释义正文 | `result.DictMeans.Means` | 按词性分组，去掉了行内 `例:` |
| 例句 | `result.Sentences` | 单独一栏「例句」，每条一行 |
| 复数 | `result.Plurals` | 顶部小卡片「复数：thresholds」 |
| 过去式 / 过去分词 / 现在分词 / 第三人称单数 | `PastTense` / `PastParticiple` / `PresentParticiple` / `ThirdPersonSingular` | 动词卡片对应字段 |
| 比较级 / 最高级 | `result.Comparative` / `Superlative` | 形容词 / 副词卡片对应字段 |

以 `threshold` 为例，**修复前**是「定义+例句糊成一段」，**修复后**拆成「noun 两行 + 例句三行 + 复数 thresholds」，看起来会清爽很多。

### 查中文会看到什么

**别被这条报错误导**：dictionaryapi.com 对「key 无效」和「查了它不认识的词」返回的是**同一条纯文本**：

```text
Invalid API key. Not subscribed for this reference.
```

而且 HTTP 状态码都是 `200` —— 接口层面**无法区分**这两种情况。所以旧版本在输入中文时会把这条消息原样抛给用户，看起来像是 key 配错了，实际是「这个 API 根本没有中文词条」。

从 **1.0.2** 起，插件在**发请求之前**就按文字脚本判断：

| 输入 | 处理 |
| --- | --- |
| `猫咪` / `こんにちは` / `안녕` / `привет` | 本地拦截，提示「Merriam-Webster 只有英英 / 英西词典，不支持中文等非英文查询」 |
| `café` / `don't` / `self-esteem` / `3-D` | 正常放行（含变音符号、连字符、撇号、词内数字） |
| 英文单词但查不到 | 返回「无结果」，交给其他翻译服务，不算错误 |

同时鉴权类错误也分开提示了：**未填 Key** → 去设置页填；**Key 无效 / 未订阅** → 检查 Key 并确认订阅。

## ✨ 功能

- 📖 **英英释义**：按词性（noun / verb / adjective …）分组的完整释义，自动展开 `sseq` 嵌套义项与 `sdsense`（"also / especially"）补充义项。
- 🗣️ **英西双语**：选用 `Spanish-English` 库时，返回西班牙语对译词，并解析 `gl` 性别标注与例句 `tr` 译文。
- 🔊 **音标与真人发音**：Merriam-Webster 音标写法 + 官方 mp3 发音（插件会按官方规则自动推导音频地址），落进 `Symbols`，宿主渲染成 `/ˈθrɛʃhoʊld/` + 🔊。
- 📝 **例句单独成栏**：`vis` 中的例句不再用 `例:` 内联糊在定义里，而是放进 SDK 的 `Sentences` 集合，由宿主单独渲染（与必应词典一致）。
- 🌱 **词形变化卡片**：从 `infl` 字段解析复数 / 过去式 / 过去分词 / 现在分词 / 第三人称单数 / 比较级 / 最高级，分别填进 `Plurals` / `PastTense` / `PastParticiple` / `PresentParticiple` / `ThirdPersonSingular` / `Comparative` / `Superlative`，由宿主对应卡片渲染。
- 🌿 **派生词**：`uros` 派生词单独成组，`stems` 屈折形式放入标签。
- 🔁 **词典兜底**：当选用的词典查不到词时，可自动回退到 Collegiate 再查一次。
- 🛡️ **非拉丁文字客户端拦截**：中文 / 日文 / 韩文 / 西里尔文等不发请求，避免消耗配额、避免把接口返回的「key 无效」误报给用户（v1.0.2 起）。
- 🧹 **标记清理**：把 `{bc}`、`{sx|…||}`、`{wi}…{/wi}` 等排版 token 还原成可读纯文本。

## 📦 安装

1. 前往 [Releases](https://github.com/mantou2046/STranslate.Plugin.Translate.MerriamWebster/releases) 页面，下载最新的 `STranslate.Plugin.Translate.MerriamWebster.spkg`。
2. 打开 STranslate → **设置** → **插件** → **添加插件**，选择下载的 `.spkg` 文件。
3. 在 **文本翻译 → 词典** 中点击 **添加**，选择 **Merriam-Webster**。

## ⚙️ 配置

| 配置项 | 说明 |
| --- | --- |
| **API Key** | 在 [dictionaryapi.com](https://dictionaryapi.com/) 注册后获取。**Collegiate** 与 **Learner's** 两个 Key 都能直接用。 |
| **词典** | `Collegiate (英英)` / `Learner's (英英)` / `Medical (英英)` / `Spanish-English (英西双语)`。后三者需要**单独订阅**，Key 无权限时会自动回退到 Collegiate。 |
| **附带例句** | 是否展示例句区。开启后，例句放进独立的「例句」栏，不再与定义混在一起。 |
| **回退到 Collegiate** | 当前词典无结果时是否再用 Collegiate 查一次。 |

### 获取 API Key

1. 打开 <https://dictionaryapi.com/> 注册账号。
2. 在 **Products** 页面分别订阅需要的词典（Collegiate 与 Learner's 免费）。
3. 订阅页即可看到对应的 API Key。

## 🧩 插件类型说明

本插件实现的是 **`DictionaryPluginBase`（词典插件）**，而不是 `TranslatePluginBase`：

- dictionaryapi.com 是**英英词典接口**，只做释义查询，不做跨语言翻译，因此语义上属于「词典」。
- 插件只接受**单个单词**（含连字符、不超过 64 字符）；整句输入会直接返回「无结果」，让其他翻译服务接管。

## 🛠️ 构建

```powershell
# 需要 .NET 10 SDK
dotnet build .\STranslate.Plugin.Translate.MerriamWebster\STranslate.Plugin.Translate.MerriamWebster.csproj -c Release
```

Release 构建会自动生成插件包：

```text
.artifacts/plugins/STranslate.Plugin.Translate.MerriamWebster.spkg
```

### 源码调试

把本仓库 clone 到 STranslate 主仓库的 `src/Plugins/ThirdPlugins/` 下，用 Visual Studio 打开 `STranslate.slnx`，将插件项目加入解决方案，把 `STranslate` 设为启动项目，`F5` 调试。Debug 输出会直接落到宿主的 `.artifacts/Debug/Plugins/` 目录。

### 解析逻辑测试

解析逻辑抽在纯静态类 `MerriamParser` 中，不依赖 WPF / 宿主，可独立测试。测试用例使用**官方 JSON 文档里给出的真实响应结构**（voluminous / feline / 3-D / 西英 language 等），并覆盖标记清理、音频目录规则、单词规范化、**文字脚本判定**与**接口错误分类**，共 96 条断言：

```powershell
# 离线用例
dotnet run --project .\tests\ParserTests\ParserTests.csproj -c Release

# 加真实 API 回归（联网，需要自己的 Key）
$env:MW_API_KEY="你的key"
dotnet run --project .\tests\ParserTests\ParserTests.csproj -c Release
```

### 重新生成图标

```powershell
python .\tools\make_icon.py
```

## 📁 目录结构

```text
STranslate.Plugin.Translate.MerriamWebster/
├── STranslate.Plugin.Translate.MerriamWebster/
│   ├── Main.cs                    # DictionaryPluginBase 实现
│   ├── MerriamParser.cs           # 纯解析逻辑（可单测）
│   ├── Models.cs                  # dictionaryapi.com JSON 模型
│   ├── Settings.cs                # 持久化配置
│   ├── plugin.json                # 插件清单
│   ├── icon.png
│   ├── View/SettingsView.xaml     # 设置面板
│   ├── ViewModel/                 # 设置视图模型
│   └── Languages/                 # zh-cn / en 本地化
├── tests/ParserTests/             # 解析逻辑测试
├── tools/                         # 图标生成等辅助脚本
└── .github/workflows/dotnet.yml   # 打 tag 自动发布
```

## 🚀 发布

推送以 `v` 开头的 tag 即触发 GitHub Actions 自动打包并发布到 Releases：

```powershell
git tag v1.0.0
git push origin v1.0.0
```

## 📖 相关文档

- [STranslate 主项目](https://github.com/STranslate/STranslate)
- [插件开发调试指南](https://github.com/STranslate/STranslate/blob/main/src/docs/community-plugin-development.md)
- [dictionaryapi.com JSON 字段文档](https://dictionaryapi.com/products/json)

## 📄 许可

MIT
