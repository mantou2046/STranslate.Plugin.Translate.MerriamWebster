# Changelog

## 1.0.4

- **修复：音标与发音音频在部分词典下完全不显示。**
  根因是解析层只读了 `hwi.prs[].mw`（韦氏自创拼读）。但 dictionaryapi.com 不同参考库用的音标字段不同：
  **Collegiate / Medical 用 `mw`，Learner's / Spanish-English 用 `ipa`**。选了 Learner's 库时 `mw` 为空，
  插件就不会往 `result.Symbols` 里加任何 `Symbol`，宿主于是整块音标都不渲染 —— 而同样基于
  `DictionaryPluginBase` 的 FreeDict 因为同时读了 `mw` 与 `ipa`，所以显示正常。
  现在新增 `MerriamParser.ExtractPronunciation()`：优先取 `mw`，缺失时回退 `ipa`，两条音标路径都能命中。
- **修复：音标被包了两层斜杠（`//ˈθrɛʃhoʊld//`）。**
  宿主 `OutputControl` 的音标文本本身带 `StringFormat='/\{0\}/'`，会再包一层 `/…/`；
  插件侧不应自加斜杠，现已去掉，显示为标准 `/ˈθrɛʃhoʊld/`。
- 音频按钮的显示条件（宿主侧 `AudioUrl` 非空才出现小喇叭）不受影响，音频地址仍按官方目录规则推导。
- 测试断言由 115 条增加到 **122 条**：新增 Learner's 风格 fixture（`prs` 只有 `ipa`）与 `TestPronunciation`，
  覆盖「mw 存在取 mw / mw 缺失取 ipa / 无发音返回空」三种情况。

## 1.0.3

- **重做释义排版：例句与词形变化独立成区，不再混在定义里。**
  学习 STranslate 内置必应词典的处理方式：
  - **音标 / 发音音频**：每条发音独立成 `Symbol`（Label 标 `us` / `uk` / `zh`），由宿主渲染成 `/ˈθrɛʃhoʊld/` + 小喇叭按钮。
  - **例句**：从 `DictMeans.Means` 抽离出来，填入 `DictionaryResult.Sentences`，宿主会单独渲染成一栏，
    不再与定义行用 `例: ...` 内联糊在一起。
  - **词形变化**：`infl` 字段（复数 / 过去式 / 过去分词 / 现在分词 / 第三人称单数 / 比较级 / 最高级）
    解析后填入 SDK 专门的集合 `Plurals` / `PastTense` / `PastParticiple` / `PresentParticiple` /
    `ThirdPersonSingular` / `Comparative` / `Superlative`，由宿主对应卡片渲染。
- 解析层拆分：`ExtractSenses` → `ExtractDefinitions` + `ExtractSentences` + `ExtractInflections`，
  三种信息不再共用同一份缓存，调用方各取所需。
- `MerriamApiException` 解析路径保持不变，鉴权错误分类依然准确。
- 测试断言由 96 条增加到 **115 条**：新增 `ThresholdJson` / `RunJson` / `GoodJson` 三个 fixture，
  覆盖名词复数、不规则动词屈折、不规则形容词比较级，新增 `TestInflections` 集成断言。

## 1.0.2

- **修复：查询中文（及其他非拉丁文字）时误报「Invalid API key. Not subscribed for this reference.」。**
  dictionaryapi.com 对「key 无效」和「查了不存在的词」返回的是**完全相同的纯文本消息**，
  且 HTTP 状态码同为 200，接口本身无法区分，因此改为在发请求前本地拦截：
  非拉丁文字（中文 / 日文 / 韩文 / 西里尔文 / 希腊文 / 阿拉伯文等）直接返回明确提示，不再消耗配额。
- 鉴权错误按类型区分提示：未填 Key → 提示去设置页填写；Key 无效或未订阅所选词典 →
  提示检查 Key 并确认已订阅对应词典，且不再对后续参考库做无意义的重试。
- 普通网络 / 解析异常与鉴权失败分开提示，不再一律套用同一句文案。
- 测试断言由 62 条增加到 96 条，新增文字脚本判定与错误分类的覆盖。

## 1.0.1

- 支持英西双语库：解析 `gl`（词性 / 性别标注）与例句译文 `tr` 字段。
- 设置页词典下拉标注「英英 / 英西双语」，明确区分单语与双语库。
- 补充说明：dictionaryapi.com **不提供中文词典**，全系仅英英与英西。

## 1.0.0

- 初始版本：基于 Merriam-Webster dictionaryapi.com 的英英词典插件。
- 支持 Collegiate / Learner's / Medical / Spanish-English 四种参考词典。
- 支持音标、发音音频、按词性分组的释义、派生词与屈折形式标签。
