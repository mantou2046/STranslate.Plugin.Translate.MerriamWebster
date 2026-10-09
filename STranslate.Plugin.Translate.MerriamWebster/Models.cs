using System.Text.Json;
using System.Text.Json.Serialization;

namespace STranslate.Plugin.Translate.MerriamWebster;

/// <summary>
/// dictionaryapi.com 词典条目的 JSON 映射。
/// 顶层响应是一个对象数组；查不到词时数组里是拼写建议字符串。
/// </summary>
public class MwEntry
{
    [JsonPropertyName("meta")]
    public MwMeta? Meta { get; set; }

    [JsonPropertyName("hwi")]
    public MwHeadwordInfo? Hwi { get; set; }

    /// <summary>functional label，即词性，例如 "adjective"、"noun"。</summary>
    [JsonPropertyName("fl")]
    public string? FunctionalLabel { get; set; }

    [JsonPropertyName("def")]
    public List<MwDefinitionGroup>? Definitions { get; set; }

    /// <summary>简短释义（前 3 个义项的纯文本摘要）。</summary>
    [JsonPropertyName("shortdef")]
    public List<string>? ShortDefinitions { get; set; }

    /// <summary>词形变化 / 派生词（如 voluminously、voluminousness）。</summary>
    [JsonPropertyName("uros")]
    public List<MwRunOn>? UndefinedRunOns { get; set; }

    /// <summary>
    /// 变形形式：复数、过去式、过去分词、现在分词、第三人称单数、比较级、最高级。
    /// 动词和名词都会带；动词若同时存在 <see cref="VerbalInflections"/>，通常 <c>infl</c> 是简表。
    /// 形态：<c>[{ "infl": "thresholds", "label": "plural" }]</c>。
    /// </summary>
    [JsonPropertyName("infl")]
    public List<MwInflection>? Inflections { get; set; }

    /// <summary>
    /// 动词屈折变化表（verb rections），结构深嵌套。
    /// System.Text.Json 无法直接映射成强类型，保留原始 <see cref="JsonElement"/> 逐项解析。
    /// 例如 "be" 这类强变化动词会返回完整 <c>vrs</c> 表；
    /// 同时存在 <c>infl</c> 时通常重复出现，二者取其一即可。
    /// </summary>
    [JsonPropertyName("vrs")]
    public JsonElement? VerbalInflections { get; set; }

    /// <summary>简明词典（如医学、法律）返回的补充段落。</summary>
    [JsonPropertyName("suppl")]
    public MwSupplemental? Supplemental { get; set; }
}

/// <summary>条目元信息。</summary>
public class MwMeta
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>该词条可命中的所有检索形式（词头、变体、屈折形式等）。</summary>
    [JsonPropertyName("stems")]
    public List<string>? Stems { get; set; }
}

/// <summary>词头与发音信息。</summary>
public class MwHeadwordInfo
{
    /// <summary>词头（带 * 音节分隔符，例如 "vo*lu*mi*nous"）。</summary>
    [JsonPropertyName("hw")]
    public string? Headword { get; set; }

    /// <summary>发音列表。</summary>
    [JsonPropertyName("prs")]
    public List<MwPronunciation>? Pronunciations { get; set; }
}

/// <summary>单条发音。</summary>
public class MwPronunciation
{
    /// <summary>Merriam-Webster 自有拼读写法（Collegiate / Medical 库用），例如 "və-ˈlü-mə-nəs"。</summary>
    [JsonPropertyName("mw")]
    public string? Mw { get; set; }

    /// <summary>国际音标（Learners / Spanish 库用），例如 "ˈthreshˌhōld"。
    /// 不同参考库用的音标字段不同，必须两个都读，否则选了 Learners 库时就读不到音标。</summary>
    [JsonPropertyName("ipa")]
    public string? Ipa { get; set; }

    [JsonPropertyName("sound")]
    public MwSound? Sound { get; set; }
}

/// <summary>发音音频信息。</summary>
public class MwSound
{
    /// <summary>音频基础文件名（不含目录与扩展名），例如 "volumi02"。</summary>
    [JsonPropertyName("audio")]
    public string? Audio { get; set; }
}

/// <summary>一个词性下的定义分组。</summary>
public class MwDefinitionGroup
{
    /// <summary>
    /// sense sequence：嵌套的义项结构。每层是 [type, payload] 形式的数组，
    /// System.Text.Json 无法直接映射成强类型元组，因此保留原始 JSON 逐层解析。
    /// </summary>
    [JsonPropertyName("sseq")]
    public JsonElement? SenseSequence { get; set; }
}

/// <summary>undefined run-on（词形变化 / 派生词）。</summary>
public class MwRunOn
{
    /// <summary>run-on 条目本身，例如 "vo*lu*mi*nous*ly"。</summary>
    [JsonPropertyName("ure")]
    public string? Entry { get; set; }

    /// <summary>run-on 的词性。</summary>
    [JsonPropertyName("fl")]
    public string? FunctionalLabel { get; set; }
}

/// <summary>
/// 一条屈折变化形式。
/// 形态：<c>{ "infl": "thresholds", "label": "plural" }</c>。
/// label 常见值：plural / past / past participle / present participle /
///              third person singular / comparative / superlative。
/// </summary>
public class MwInflection
{
    /// <summary>屈折形式本体，例如 "thresholds"、"ran"、"running"。</summary>
    [JsonPropertyName("infl")]
    public string? Form { get; set; }

    /// <summary>类型标签，参见类注释。</summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }
}

/// <summary>补充信息（部分词典以段落形式给出释义）。</summary>
public class MwSupplemental
{
    [JsonPropertyName("examples")]
    public List<MwText>? Examples { get; set; }
}

/// <summary>带文本的通用对象。</summary>
public class MwText
{
    [JsonPropertyName("t")]
    public string? Text { get; set; }
}
