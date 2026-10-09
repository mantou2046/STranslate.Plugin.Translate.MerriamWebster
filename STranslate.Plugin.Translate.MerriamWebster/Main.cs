using Microsoft.Extensions.Logging;
using STranslate.Plugin.Translate.MerriamWebster.View;
using STranslate.Plugin.Translate.MerriamWebster.ViewModel;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Controls;

namespace STranslate.Plugin.Translate.MerriamWebster;

/// <summary>
/// 基于 Merriam-Webster dictionaryapi.com 的英英词典插件。
/// </summary>
public class Main : DictionaryPluginBase
{
    private const string ApiRoot = "https://dictionaryapi.com/api/v3/references";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;

    public override Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel(Context, Settings);
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    public override void Init(IPluginContext context)
    {
        Context = context;
        Settings = context.LoadSettingStorage<Settings>();
    }

    public override void Dispose() => _viewModel?.Dispose();

    public override async Task TranslateAsync(string content, DictionaryResult result, CancellationToken cancellationToken = default)
    {
        if (!MerriamParser.IsQueryable(content))
        {
            // 该接口只支持单词查询，整句交给其他翻译服务。
            result.ResultType = DictionaryResultType.NoResult;
            return;
        }

        // dictionaryapi.com 只有英文（及西英库的西班牙语）词条，没有中文。
        // 非拉丁文字在发请求前就拦掉：既省一次配额，也避免把接口返回的
        // "Invalid API key" 误当成 key 配置问题报给用户。
        if (!MerriamParser.IsLatinScript(content))
        {
            result.ResultType = DictionaryResultType.Error;
            result.Text = Context.GetTranslation("STranslate_Plugin_Translate_MerriamWebster_NonLatinInput");
            return;
        }

        if (string.IsNullOrWhiteSpace(Settings.ApiKey))
        {
            result.ResultType = DictionaryResultType.Error;
            result.Text = Context.GetTranslation("STranslate_Plugin_Translate_MerriamWebster_MissingApiKey");
            return;
        }

        var query = MerriamParser.NormalizeWord(content);

        // 无词性可用的兜底标题，以及派生词分组标题，跟随宿主语言。
        var fallbackLabel = Context.GetTranslation("STranslate_Plugin_Translate_MerriamWebster_Definition");
        var derivedLabel = Context.GetTranslation("STranslate_Plugin_Translate_MerriamWebster_Derived");

        foreach (var reference in BuildReferenceOrder())
        {
            try
            {
                var entries = await QueryAsync(reference, query, cancellationToken);

                // 查不到词时字典会返回拼写建议（纯字符串数组），此时换下一个参考库尝试。
                if (entries is null)
                    continue;

                if (FillResult(entries, result, query, Settings.ShowExamples, fallbackLabel, derivedLabel))
                    return;
            }
            catch (OperationCanceledException)
            {
                result.ResultType = DictionaryResultType.NoResult;
                return;
            }
            catch (MerriamApiException ex)
            {
                // key 无效 / 未订阅该参考库：这是配置问题，给出可操作的提示，
                // 并且不要再往下一个参考库重试（换库也一样会被拒）。
                Context.Logger.LogWarning("Merriam-Webster 鉴权失败 ({Reference}): {Message}", reference, ex.RawBody);
                result.ResultType = DictionaryResultType.Error;
                result.Text = ex.Error == ApiError.MissingKey
                    ? Context.GetTranslation("STranslate_Plugin_Translate_MerriamWebster_MissingApiKey")
                    : Context.GetTranslation("STranslate_Plugin_Translate_MerriamWebster_InvalidKey");
                return;
            }
            catch (Exception ex)
            {
                Context.Logger.LogError(ex, "Merriam-Webster 查询失败: {Word} ({Reference})", query, reference);
                result.ResultType = DictionaryResultType.Error;
                result.Text = string.Format(
                    Context.GetTranslation("STranslate_Plugin_Translate_MerriamWebster_RequestFailed"),
                    ex.Message);
                return;
            }
        }

        // 所有参考库都没有该词条：不是错误，只是查不到，避免污染历史记录。
        result.ResultType = DictionaryResultType.NoResult;
    }

    /// <summary>
    /// 依次拼接实际要尝试的参考库。开启兜底时，若首选不是 Collegiate 会再追加一次 Collegiate。
    /// </summary>
    private List<MerriamReference> BuildReferenceOrder()
    {
        var order = new List<MerriamReference> { Settings.Reference };

        if (Settings.FallbackToCollegiate && Settings.Reference != MerriamReference.Collegiate)
            order.Add(MerriamReference.Collegiate);

        return order;
    }

    /// <summary>
    /// 查询指定参考库。返回 null 表示该库没有这个词条（含拼写建议响应、空响应）。
    /// 鉴权类错误抛 <see cref="MerriamApiException"/>；其他异常（网络、JSON 结构）按原样抛出。
    /// </summary>
    private async Task<List<MwEntry>?> QueryAsync(MerriamReference reference, string word, CancellationToken cancellationToken)
    {
        var url = $"{ApiRoot}/{ReferencePath(reference)}/json/{Uri.EscapeDataString(word)}";

        var options = new Options
        {
            QueryParams = new Dictionary<string, string> { ["key"] = Settings.ApiKey.Trim() },
            Timeout = TimeSpan.FromSeconds(20)
        };

        var response = await Context.HttpService.GetAsync(url, options, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return null;

        var trimmed = response.TrimStart();

        // 鉴权失败时接口返回的是纯文本而非 JSON（HTTP 仍是 200）。
        // 注意：key 无效和「查了非英文词」返回的是同一条消息，所以调用方
        // 必须先在本地拦掉非拉丁文字，否则会误报成配置问题。
        if (trimmed[0] != '[')
            throw new MerriamApiException(MerriamParser.ClassifyTextResponse(trimmed), response);

        var nodes = JsonNode.Parse(response)?.AsArray();
        if (nodes is null || nodes.Count == 0)
            return null;

        var entries = new List<MwEntry>();
        foreach (var node in nodes)
        {
            // 字符串元素 = 拼写建议，说明该参考库没有这个词条。
            if (node is not JsonObject obj)
                return null;

            var entry = obj.Deserialize<MwEntry>(SerializerOptions);
            if (entry is not null)
                entries.Add(entry);
        }

        return entries.Count > 0 ? entries : null;
    }

    /// <summary>
    /// 把词条数组填充到 DictionaryResult。返回 false 表示没有可展示的释义。
    /// </summary>
    private static bool FillResult(List<MwEntry> entries, DictionaryResult result, string query, bool showExamples, string fallbackLabel, string derivedLabel)
    {
        var headword = entries
            .Select(e => e.Hwi?.Headword)
            .FirstOrDefault(h => !string.IsNullOrWhiteSpace(h));

        if (string.IsNullOrWhiteSpace(headword))
            return false;

        var filled = false;

        result.Text = MerriamParser.FormatHeadword(headword);
        result.ResultType = DictionaryResultType.Success;

        // 音标与发音音频。
        // 宿主 UI 会用 StringFormat 再包一层 "/…/"，这里不要自加斜杠，否则会变成 "//…//"。
        // 不同参考库音标字段不同（mw vs ipa），由 ExtractPronunciation 统一兜底。
        var (phonetic, audioUrl) = MerriamParser.ExtractPronunciation(entries);
        if (!string.IsNullOrWhiteSpace(phonetic))
        {
            result.Symbols.Add(new Symbol
            {
                Label = "us",
                Phonetic = phonetic!.Trim(),
                AudioUrl = audioUrl
            });
        }

        // 按词性分组填充释义（不混入例句）
        var grouped = new Dictionary<string, ObservableCollection<string>>();
        var order = new List<string>();

        foreach (var entry in entries)
        {
            var partOfSpeech = string.IsNullOrWhiteSpace(entry.FunctionalLabel)
                ? fallbackLabel
                : entry.FunctionalLabel!.Trim();

            var definitions = MerriamParser.ExtractDefinitions(entry);
            if (definitions.Count == 0)
                continue;

            if (!grouped.TryGetValue(partOfSpeech, out var bucket))
            {
                bucket = [];
                grouped[partOfSpeech] = bucket;
                order.Add(partOfSpeech);
            }

            foreach (var def in definitions)
            {
                if (!bucket.Contains(def))
                    bucket.Add(def);
            }
        }

        foreach (var partOfSpeech in order)
        {
            result.DictMeans.Add(new DictMean
            {
                PartOfSpeech = partOfSpeech,
                Means = grouped[partOfSpeech]
            });
            filled = true;
        }

        // 例句放进专门的 Sentences 集合，不和定义混在一起。
        // 学习必应词典的处理：定义走 DictMeans.Means，例句独立成区。
        if (showExamples)
        {
            var sentences = entries
                .SelectMany(MerriamParser.ExtractSentences)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .Take(20) // 接口有时会给出几十条例句，多数场景用不到这么多
                .ToList();

            foreach (var sentence in sentences)
                result.Sentences.Add(sentence);
        }

        // 词形变化（复数 / 过去式 / 比较级等）映射到 SDK 专门的集合。
        // 每个标签只取一次（一个词条里同一标签通常也只有一个值）。
        foreach (var entry in entries)
        {
            foreach (var (label, forms) in MerriamParser.ExtractInflections(entry))
            {
                var bucket = label.ToLowerInvariant() switch
                {
                    "plural" => result.Plurals,
                    "plural form" => result.Plurals,
                    "past" or "past tense" => result.PastTense,
                    "past participle" => result.PastParticiple,
                    "present participle" => result.PresentParticiple,
                    "present" => result.PresentParticiple,
                    "third person singular" or "third-person singular" => result.ThirdPersonSingular,
                    "comparative" => result.Comparative,
                    "superlative" => result.Superlative,
                    _ => null
                };

                if (bucket is null) continue;

                foreach (var form in forms)
                    if (!bucket.Contains(form))
                        bucket.Add(form);
            }
        }

        // 派生词 / undefined run-on
        var runOns = entries
            .SelectMany(e => e.UndefinedRunOns ?? [])
            .Select(r => MerriamParser.FormatRunOn(r.Entry))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();

        if (runOns.Count > 0)
        {
            result.DictMeans.Add(new DictMean
            {
                PartOfSpeech = derivedLabel,
                Means = new ObservableCollection<string>(runOns)
            });
            filled = true;
        }

        // 同词条的其他形式（stems）放入标签，便于确认命中的屈折形式
        entries
            .SelectMany(e => e.Meta?.Stems ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s) && !s.Equals(query, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .Take(10)
            .ToList()
            .ForEach(result.Tags.Add);

        return filled;
    }

    private static string ReferencePath(MerriamReference reference) => reference switch
    {
        MerriamReference.Learners => "learners",
        MerriamReference.Medical => "medical",
        MerriamReference.Spanish => "spanish",
        _ => "collegiate"
    };
}

/// <summary>
/// dictionaryapi.com 以纯文本返回的鉴权类错误（key 无效 / 未订阅 / 未提供 key）。
/// 单独成类型，便于与网络错误区分，给出可操作的提示。
/// </summary>
public sealed class MerriamApiException(ApiError error, string rawBody)
    : Exception(rawBody)
{
    /// <summary>错误分类。</summary>
    public ApiError Error { get; } = error;

    /// <summary>接口返回的原始文本。</summary>
    public string RawBody { get; } = rawBody;
}
