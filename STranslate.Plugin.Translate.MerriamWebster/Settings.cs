namespace STranslate.Plugin.Translate.MerriamWebster;

/// <summary>
/// Merriam-Webster 字典插件的持久化配置。
/// </summary>
public class Settings
{
    /// <summary>
    /// dictionaryapi.com 的 API Key（Collegiate 与 Learner's 两个 Key 通用）。
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 使用的词典参考库。
    /// </summary>
    public MerriamReference Reference { get; set; } = MerriamReference.Collegiate;

    /// <summary>
    /// 查询失败（无结果）时，是否改用 Collegiate 作为兜底参考库重试一次。
    /// </summary>
    public bool FallbackToCollegiate { get; set; } = true;

    /// <summary>
    /// 是否在释义中附带例句。
    /// </summary>
    public bool ShowExamples { get; set; } = true;
}

/// <summary>
/// dictionaryapi.com 提供的参考库类型。
/// 注意：该服务**不提供中文**，全部为英英（单语）或英西（双语）词典。
/// </summary>
public enum MerriamReference
{
    /// <summary>
    /// Collegiate Dictionary（韦氏大学词典）：英英，词条最全，带音频。
    /// </summary>
    Collegiate,

    /// <summary>
    /// Advanced Learner's English Dictionary：英英，释义更简单，适合学习者。
    /// </summary>
    Learners,

    /// <summary>
    /// Medical Dictionary：英英，医学专业词汇。
    /// </summary>
    Medical,

    /// <summary>
    /// Spanish-English Dictionary：**英西双语（双向）**，非英英。
    /// 释义为西班牙语对译词，并带 gender 标注（masculine / feminine）。
    /// </summary>
    Spanish
}
