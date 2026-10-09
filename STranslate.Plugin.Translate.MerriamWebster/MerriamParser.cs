using System.Text.Json;
using System.Text.RegularExpressions;

namespace STranslate.Plugin.Translate.MerriamWebster;

/// <summary>
/// dictionaryapi.com 响应的纯解析逻辑。不依赖宿主，便于单元测试。
/// </summary>
public static class MerriamParser
{
    /// <summary>标记 token 与替换文本（顺序敏感，长的 token 必须排在前面）。</summary>
    private static readonly (string Token, string Replacement)[] TokenReplacements =
    [
        ("{bc}", ": "),
        ("{b}", ""), ("{/b}", ""),
        ("{it}", ""), ("{/it}", ""),
        ("{inf}", ""), ("{/inf}", ""),
        ("{sc}", ""), ("{/sc}", ""),
        ("{sup}", ""), ("{/sup}", ""),
        ("{ldquo}", "\""), ("{rdquo}", "\""),
        ("{p_br}", " "),
        ("{ds||", ""), ("{ds}", ""), ("{ds|", ""),
        ("{dx}", ""), ("{dxt|", "|"), ("{|}", ""),
        ("{ma}", ""),
        ("{wi}", ""), ("{/wi}", ""),
        ("{phrase}", ""), ("{/phrase}", ""),
        ("{parahw}", ""), ("{/parahw}", ""),
        ("{qword}", ""), ("{/qword}", ""),
        ("{a_link|", ""), ("{d_link|", ""), ("{et_link|", ""), ("{i_link|", ""),
        ("{sx|", ""),
        ("{gloss}", "("), ("{/gloss}", ")"),
        ("}", ""), ("|", " "),
    ];

    /// <summary>
    /// 把定义文本中的标记 token 清理成可读纯文本。
    /// {bc} 是「边界/冒号」token，只在两段文本之间才有意义；位于首尾时必须丢弃，
    /// 否则释义会渲染成 ": having or marked by ..."。
    /// </summary>
    public static string CleanMarkup(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var text = raw;
        foreach (var (token, replacement) in TokenReplacements)
            text = text.Replace(token, replacement, StringComparison.Ordinal);

        text = Regex.Replace(text, @"\s{2,}", " ").Trim();
        text = text.TrimStart(':', ';').TrimStart();
        text = text.TrimEnd(':', ';').TrimEnd();
        text = Regex.Replace(text, @"\s+([,;:])", "$1");

        return text.Trim();
    }

    /// <summary>音频基础名转完整 URL，目录规则见官方 JSON 文档。</summary>
    public static string BuildAudioUrl(string? audio)
    {
        if (string.IsNullOrWhiteSpace(audio))
            return string.Empty;

        string subdirectory;
        if (audio.StartsWith("bix", StringComparison.OrdinalIgnoreCase))
            subdirectory = "bix";
        else if (audio.StartsWith("gg", StringComparison.OrdinalIgnoreCase))
            subdirectory = "gg";
        else if (!char.IsLetter(audio[0]))
            subdirectory = "number";
        else
            subdirectory = char.ToLowerInvariant(audio[0]).ToString();

        return $"https://media.merriam-webster.com/audio/prons/en/us/mp3/{subdirectory}/{audio}.mp3";
    }

    /// <summary>run-on 条目去掉音节分隔符与标记。</summary>
    public static string FormatRunOn(string? entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
            return string.Empty;

        return CleanMarkup(entry.Replace("*", string.Empty)).Trim();
    }

    /// <summary>词头去掉音节分隔符。</summary>
    public static string FormatHeadword(string? headword) =>
        string.IsNullOrWhiteSpace(headword) ? string.Empty : CleanMarkup(headword.Replace("*", string.Empty));

    /// <summary>提取条目的全部义项文本。</summary>
    public static List<string> ExtractSenses(MwEntry entry, bool showExamples)
    {
        var means = new List<string>();

        foreach (var group in entry.Definitions ?? [])
        {
            if (group.SenseSequence is not { ValueKind: JsonValueKind.Array } sseq)
                continue;

            // sseq -> 义项组 -> [ "sense", {...} ]
            foreach (var senseGroup in sseq.EnumerateArray())
            {
                if (senseGroup.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var wrapper in senseGroup.EnumerateArray())
                {
                    if (wrapper.ValueKind == JsonValueKind.Array && wrapper.GetArrayLength() >= 2)
                        CollectSenseText(wrapper[1], means, showExamples);
                }
            }
        }

        if (means.Count == 0)
        {
            foreach (var shortDef in entry.ShortDefinitions ?? [])
            {
                var text = CleanMarkup(shortDef);
                if (!string.IsNullOrWhiteSpace(text) && !means.Contains(text))
                    means.Add(text);
            }
        }

        return means;
    }

    /// <summary>
    /// 展开一个 sense：主释义一行；sdsense 补充释义单独一行；复合义项追加子义项。
    /// </summary>
    private static void CollectSenseText(JsonElement sense, List<string> means, bool showExamples)
    {
        if (sense.ValueKind != JsonValueKind.Object)
            return;

        var texts = new List<string>();
        var examples = new List<string>();
        if (sense.TryGetProperty("dt", out var dt))
            ExtractDt(dt, texts, examples);
        AddSenseLine(texts, examples, means, showExamples);

        if (sense.TryGetProperty("sdsense", out var sdSense) && sdSense.ValueKind == JsonValueKind.Object)
        {
            var sdTexts = new List<string>();
            var sdExamples = new List<string>();
            if (sdSense.TryGetProperty("dt", out var sdDt))
                ExtractDt(sdDt, sdTexts, sdExamples);

            var label = sdSense.TryGetProperty("sd", out var sdValue) && sdValue.ValueKind == JsonValueKind.String
                ? sdValue.GetString()?.Trim()
                : null;

            if (!string.IsNullOrWhiteSpace(label))
            {
                if (sdTexts.Count == 0)
                {
                    sdTexts.Add(label);
                }
                else
                {
                    for (var i = 0; i < sdTexts.Count; i++)
                        sdTexts[i] = $"{label}: {sdTexts[i]}";
                }
            }

            AddSenseLine(sdTexts, sdExamples, means, showExamples);
        }

        if (sense.TryGetProperty("sseq", out var childSeq) && childSeq.ValueKind == JsonValueKind.Array)
        {
            foreach (var childGroup in childSeq.EnumerateArray())
            {
                if (childGroup.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var childWrapper in childGroup.EnumerateArray())
                {
                    if (childWrapper.ValueKind != JsonValueKind.Array || childWrapper.GetArrayLength() < 2)
                        continue;

                    CollectSenseText(childWrapper[1], means, showExamples);
                }
            }
        }
    }

    private static void AddSenseLine(List<string> texts, List<string> examples, List<string> means, bool showExamples)
    {
        var text = string.Join("; ", texts.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (showExamples && examples.Count > 0)
            text += $"  例: {string.Join(" | ", examples.Take(3))}";

        if (!means.Contains(text))
            means.Add(text);
    }

    /// <summary>
    /// 从 dt 数组中提取定义文本与例句。
    /// 除 text / vis 外，还处理：
    /// - gl（gloss）：词性 / 性别标注，常见于西英词典，如 masculine / feminine，用括号附加。
    /// - vis 项里的 tr：例句的译文，西英词典专有，与英文例句一并给出。
    /// </summary>
    private static void ExtractDt(JsonElement dt, List<string> texts, List<string> examples)
    {
        if (dt.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in dt.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() < 2)
                continue;

            var type = item[0].GetString();
            var payload = item[1];

            switch (type)
            {
                case "text":
                    var text = payload.ValueKind == JsonValueKind.String ? CleanMarkup(payload.GetString()) : null;
                    if (!string.IsNullOrWhiteSpace(text))
                        texts.Add(text);
                    break;

                case "gl":
                    // gloss 紧跟在前一段释义文本之后，附加成 "(masculine)"
                    var gloss = payload.ValueKind == JsonValueKind.String ? payload.GetString()?.Trim() : null;
                    if (!string.IsNullOrWhiteSpace(gloss))
                        texts.Add($"({gloss})");
                    break;

                case "vis" when payload.ValueKind == JsonValueKind.Array:
                    foreach (var illustration in payload.EnumerateArray())
                    {
                        if (illustration.ValueKind != JsonValueKind.Object ||
                            !illustration.TryGetProperty("t", out var t))
                            continue;

                        var example = CleanMarkup(t.GetString());
                        if (string.IsNullOrWhiteSpace(example))
                            continue;

                        // 西英词典的例句带 tr 译文
                        if (illustration.TryGetProperty("tr", out var tr))
                        {
                            var translation = CleanMarkup(tr.GetString());
                            if (!string.IsNullOrWhiteSpace(translation))
                                example = $"{example} → {translation}";
                        }

                        examples.Add(example);
                    }
                    break;
            }
        }
    }

    /// <summary>查询前的规范化：去掉首尾标点，只保留单词本体。</summary>
    public static string NormalizeWord(string word)
    {
        var trimmed = word.Trim().Trim('\'', '"', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']');

        while (trimmed.Length > 0 && !char.IsLetterOrDigit(trimmed[^1]))
            trimmed = trimmed[..^1];

        return string.IsNullOrWhiteSpace(trimmed) ? word.Trim() : trimmed;
    }

    /// <summary>是否是可交给词典查询的单词（单行、不含空格、长度合理）。</summary>
    public static bool IsQueryable(string word)
    {
        var w = (word ?? string.Empty).Trim();
        return !string.IsNullOrWhiteSpace(w) && !w.Contains(' ') && w.Length <= 64;
    }

    /// <summary>
    /// 是否全部由拉丁字母构成（允许连字符、撇号、空格等词内分隔符）。
    /// dictionaryapi.com 只收录英文（以及西英库的西班牙语）词条，
    /// 中文 / 日文 / 韩文 / 西里尔文等非拉丁文字必然查不到，
    /// 应当在发请求之前就拦下来，避免白白消耗一次配额、并触发误导性的接口报错。
    /// </summary>
    public static bool IsLatinScript(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return false;

        foreach (var ch in word)
        {
            if (char.IsWhiteSpace(ch) || ch is '-' or '\'' or '\u2019' or '.')
                continue;

            if (!char.IsLetter(ch))
            {
                // 数字允许出现在词内（如 3-D），但不影响判断
                if (char.IsDigit(ch))
                    continue;

                return false;
            }

            // 非拉丁字母（汉字、假名、谚文、西里尔文……）一律视为不支持
            if (!IsLatinLetter(ch))
                return false;
        }

        return word.Any(char.IsLetter);
    }

    /// <summary>判断字符是否属于拉丁字母（基本区 + 常见扩展）。</summary>
    private static bool IsLatinLetter(char ch)
    {
        // ASCII 字母
        if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'))
            return true;

        // 带变音符号的拉丁字母：À-ÖØ-öø-ÿ 等
        if (ch >= '\u00C0' && ch <= '\u024F')
            return true;

        // 拉丁扩展附加区
        if (ch >= '\u1E00' && ch <= '\u1EFF')
            return true;

        return false;
    }

    /// <summary>
    /// 识别接口返回的纯文本错误信息（HTTP 200 但内容不是 JSON）。
    /// 返回分类结果，供调用方给出有针对性的提示。
    /// </summary>
    public static ApiError ClassifyTextResponse(string body)
    {
        var text = (body ?? string.Empty).Trim();

        if (text.Contains("Key is required", StringComparison.OrdinalIgnoreCase))
            return ApiError.MissingKey;

        if (text.Contains("Invalid API key", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Not subscribed", StringComparison.OrdinalIgnoreCase))
            return ApiError.InvalidKeyOrNotSubscribed;

        return ApiError.Unknown;
    }
}

/// <summary>接口以纯文本返回的错误类型。</summary>
public enum ApiError
{
    /// <summary>未分类的异常响应。</summary>
    Unknown,

    /// <summary>请求里没有带 key。</summary>
    MissingKey,

    /// <summary>key 无效，或该 key 未订阅当前参考库。</summary>
    InvalidKeyOrNotSubscribed
}
