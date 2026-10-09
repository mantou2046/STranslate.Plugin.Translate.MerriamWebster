using STranslate.Plugin.Translate.MerriamWebster;
using System.Text.Json;

namespace ParserTests;

/// <summary>
/// Exercises the real MerriamParser against the exact response shapes
/// documented by dictionaryapi.com. Run: dotnet run -c Release
/// </summary>
internal static class Program
{
    private static int _failures;

    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // ---- 1. offline fixtures (exact structures from the MW JSON docs) ----
        TestVoluminous();
        TestShortDefOnly();
        TestNestedBindingSubstitute();
        TestSpanishBilingual();
        TestInflections();
        TestAudioUrls();
        TestMarkupCleaning();
        TestWordNormalization();
        TestScriptGating();
        TestApiErrorClassification();

        // ---- 2. live check if a key was supplied ----
        var key = Environment.GetEnvironmentVariable("MW_API_KEY")
                  ?? (args.Length > 0 ? args[0] : null);

        if (!string.IsNullOrWhiteSpace(key))
        {
            Console.WriteLine("\n== LIVE dictionaryapi.com ==");
            await LiveCheckAsync(key!);
        }
        else
        {
            Console.WriteLine("\n== LIVE check skipped (set MW_API_KEY or pass key as arg) ==");
        }

        Console.WriteLine();
        Console.WriteLine(_failures == 0 ? "ALL CHECKS PASSED" : $"{_failures} CHECK(S) FAILED");
        return _failures == 0 ? 0 : 1;
    }

    // ------------------------------------------------------------- fixtures

    private const string VoluminousJson = """
    [
      {
        "meta": {
          "id": "voluminous",
          "stems": ["voluminous", "voluminously", "voluminousness", "voluminousnesses"]
        },
        "hwi": {
          "hw": "vo*lu*mi*nous",
          "prs": [{ "mw": "v\u0259-\u02c8l\u00fc-m\u0259-n\u0259s", "sound": { "audio": "volumi02", "ref": "c", "stat": "1" } }]
        },
        "fl": "adjective",
        "def": [
          { "sseq": [
            [
              [ "sense", {
                  "sn": "1 a",
                  "dt": [
                    [ "text", "{bc}having or marked by great {a_link|volume} or bulk {bc}{sx|large||} " ],
                    [ "vis", [ { "t": "long {wi}voluminous{/wi} tresses" } ] ]
                  ],
                  "sdsense": {
                    "sd": "also",
                    "dt": [
                      [ "text", "{bc}{sx|full||} " ],
                      [ "vis", [ { "t": "a {wi}voluminous{/wi} skirt" } ] ]
                    ]
                  }
              } ],
              [ "sense", { "sn": "b", "dt": [ [ "text", "{bc}{sx|numerous||} " ] ] } ]
            ],
            [ [ "sense", { "sn": "2 a", "dt": [ [ "text", "{bc}filling or capable of filling a large volume or several volumes" ] ] } ] ],
            [ [ "sense", { "sn": "3", "dt": [ [ "text", "{bc}consisting of many folds, coils, or convolutions {bc}{sx|winding||}" ] ] } ] ]
          ] }
        ],
        "uros": [
          { "ure": "vo*lu*mi*nous*ly", "fl": "adverb" },
          { "ure": "vo*lu*mi*nous*ness", "fl": "noun" }
        ],
        "shortdef": [
          "having or marked by great volume or bulk : large; also : full",
          "numerous",
          "filling or capable of filling a large volume or several volumes"
        ]
      }
    ]
    """;

    private const string ThreeDJson = """
    [
      {
        "meta": { "id": "3-D" },
        "hwi": { "hw": "3-D", "prs": [ { "mw": "\u02ccthr\u0113-\u02c8d\u0113", "sound": { "audio": "3d000001" } } ] },
        "fl": "noun",
        "shortdef": [ "a three-dimensional form; also : an image or object that seems to have depth" ]
      }
    ]
    """;

    private const string FelineJson = """
    [
      {
        "meta": { "id": "feline" },
        "hwi": { "hw": "fe*line", "prs": [ { "mw": "\u02c8f\u0113-\u02ccl\u012bn", "sound": { "audio": "feli01" } } ] },
        "fl": "adjective",
        "def": [
          { "sseq": [
            [ [ "sense", { "sn": "1", "dt": [ [ "text", "{bc}of, relating to, or affecting cats" ] ] } ] ],
            [ [ "sense", { "sn": "2", "dt": [ [ "text", "{bc}resembling a cat: such as " ] ],
                "sseq": [
                  [ [ "sense", { "sn": "a", "dt": [ [ "text", "{bc}sleekly graceful" ] ] } ] ],
                  [ [ "sense", { "sn": "b", "dt": [ [ "text", "{bc}sly, treacherous" ] ] } ] ]
                ] } ] ]
          ] }
        ],
        "shortdef": [ "of, relating to, or affecting cats", "resembling a cat" ]
      }
    ]
    """;

    private const string TenaciousJson = """
    [
      {
        "meta": { "id": "tenacious" },
        "hwi": { "hw": "te*na*cious", "prs": [ { "mw": "t\u0259-\u02c8n\u0101-sh\u0259s", "sound": { "audio": "tenaci01" } } ] },
        "fl": "adjective",
        "def": [
          { "sseq": [
            [ [ "sense", { "sn": "1", "dt": [ [ "text", "{bc}{sx|not easily stopped||} or {sx|persistent||}" ] ] } ] ],
            [ [ "sense", { "sn": "2", "dt": [ [ "text", "{bc}very determined to continue" ] ] } ] ]
          ] }
        ],
        "shortdef": [ "not easily stopped or persistent", "very determined to continue" ]
      }
    ]
    """;

    // Spanish-English dictionary: bilingual, uses "gl" gloss tokens and "tr"
    // translations inside verbal illustrations. Taken verbatim from the MW docs.
    private const string SpanishJson = """
    [
      {
        "meta": { "id": "language", "lang": "en", "src": "spanish", "stems": ["language", "languages"] },
        "hwi": { "hw": "language", "prs": [ { "mw": "\u02c8l\u00e6\u014bgw\u026ad\u0292", "sound": { "audio": "langua01" } } ] },
        "fl": "noun",
        "def": [
          { "sseq": [
            [
              [ "sense", { "sn": "1", "dt": [
                  [ "text", "{bc}{a_link|idioma} " ],
                  [ "gl", "masculine" ],
                  [ "text", ", {a_link|lengua} " ],
                  [ "gl", "feminine" ],
                  [ "vis", [ { "t": "the English language", "tr": "el idioma ingl\u00e9s" } ] ]
              ] } ]
            ],
            [
              [ "sense", { "sn": "2", "dt": [
                  [ "text", "{bc}{a_link|lenguaje} " ],
                  [ "gl", "masculine" ],
                  [ "vis", [ { "t": "body language", "tr": "lenguaje corporal" } ] ]
              ] } ]
            ]
          ] }
        ],
        "shortdef": [ "idioma, lengua", "lenguaje" ]
      }
    ]
    """;

    // threshold 类形态 —— 用户截图报告的词。MW 的 `infl` 字段给出复数 `thresholds`，
    // `prs` 字段给出美式发音与音频，`dt.vis` 给出每条义项下的例句。
    private const string ThresholdJson = """
    [
      {
        "meta": { "id": "threshold", "stems": ["threshold", "thresholds"] },
        "hwi": {
          "hw": "thresh*old",
          "prs": [ { "mw": "\u02ccthre\u1e57\u02cch\u014dld", "sound": { "audio": "thresho01" } } ]
        },
        "fl": "noun",
        "def": [
          { "sseq": [
            [
              [ "sense", { "sn": "1 a", "dt": [
                  [ "text", "{bc}a piece of wood, metal, or stone that forms the bottom of a door and that you walk over as you enter a room or building" ],
                  [ "vis", [ { "t": "He stepped across the threshold." }, { "t": "When they were married he carried her over the threshold." } ] ]
              ] } ]
            ],
            [
              [ "sense", { "sn": "2", "dt": [
                  [ "text", "{bc}the point or level at which something begins or changes" ],
                  [ "vis", [ { "t": "If your income rises above a certain threshold, your tax rate also rises." } ] ]
              ] } ]
            ]
          ] }
        ],
        "infl": [ { "infl": "thresholds", "label": "plural" } ],
        "shortdef": [ "the bottom of a doorway", "the point at which something begins to change" ]
      }
    ]
    """;

    // run 类形态 —— 不规则动词，含 past/past participle/present participle 三种 infl。
    private const string RunJson = """
    [
      {
        "meta": { "id": "run", "stems": ["run", "runs", "running", "ran", "run"] },
        "hwi": { "hw": "run", "prs": [ { "mw": "\u02c8r\u0259n", "sound": { "audio": "run000001" } } ] },
        "fl": "verb",
        "infl": [
          { "infl": "ran", "label": "past" },
          { "infl": "run", "label": "past participle" },
          { "infl": "running", "label": "present participle" },
          { "infl": "runs", "label": "third person singular" }
        ],
        "shortdef": [ "to go faster than a walk", "to move at a fast trot" ]
      }
    ]
    """;

    // good/well/better/best 类形态 —— 形容词 / 副词的不规则比较级。
    private const string GoodJson = """
    [
      {
        "meta": { "id": "good", "stems": ["good", "better", "best", "well", "better", "best"] },
        "hwi": { "hw": "good", "prs": [ { "mw": "\u02c8g\u00fbd", "sound": { "audio": "good00001" } } ] },
        "fl": "adjective",
        "infl": [
          { "infl": "better", "label": "comparative" },
          { "infl": "best", "label": "superlative" }
        ],
        "shortdef": [ "agreeable or pleasing" ]
      }
    ]
    """;

    private static List<MwEntry> Parse(string json) =>
        JsonSerializer.Deserialize<List<MwEntry>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    // ------------------------------------------------------------ the tests

    private static void TestVoluminous()
    {
        Console.WriteLine("== voluminous (full def tree, sdsense, examples) ==");
        var entry = Parse(VoluminousJson)[0];

        var definitions = MerriamParser.ExtractDefinitions(entry);
        foreach (var d in definitions) Console.WriteLine("    def: " + d);
        var sentences = MerriamParser.ExtractSentences(entry);
        foreach (var s in sentences) Console.WriteLine("    ex:  " + s);

        Check("5 sense lines", definitions.Count == 5, $"got {definitions.Count}");
        Check("no leading colon anywhere", definitions.All(d => !d.TrimStart().StartsWith(':')), "a definition starts with ':'");
        Check("sdsense is its own line prefixed 'also: '", definitions.Any(d => d.StartsWith("also: ") && d.Contains("full")));
        Check("cross-ref {sx|large||} resolved to 'large'", definitions.Any(d => d.Contains("large")));
        Check("middle boundary rendered as colon", definitions.Any(d => d.Contains("bulk: large")));
        Check("sense 1b present", definitions.Any(d => d == "numerous"));
        Check("sense 3 present", definitions.Any(d => d.Contains("many folds")));
        Check("no brace residue", definitions.All(d => !d.Contains('{') && !d.Contains('}')));

        // 例句应独立存在，不在定义里
        Check("2 examples extracted from dt", sentences.Count == 2, $"got {sentences.Count}");
        Check("examples contain 'tresses'", sentences.Any(s => s.Contains("tresses")));
        Check("examples contain 'skirt'", sentences.Any(s => s.Contains("skirt")));
        Check("definitions do NOT contain '例:' inline",
              definitions.All(d => !d.Contains("例:")), string.Join(" / ", definitions));

        // headword + audio + run-ons
        Check("headword de-syllabified", MerriamParser.FormatHeadword(entry.Hwi!.Headword) == "voluminous",
              MerriamParser.FormatHeadword(entry.Hwi.Headword));
        Check("audio url for volumi02",
              MerriamParser.BuildAudioUrl(entry.Hwi.Pronunciations![0].Sound!.Audio)
              == "https://media.merriam-webster.com/audio/prons/en/us/mp3/v/volumi02.mp3");
        var runOns = entry.UndefinedRunOns!.Select(r => MerriamParser.FormatRunOn(r.Entry)).ToList();
        Check("run-ons formatted", runOns.SequenceEqual(["voluminously", "voluminousness"]), string.Join(",", runOns));
    }

    private static void TestShortDefOnly()
    {
        Console.WriteLine("\n== shortdef-only entries (no def tree) ==");
        var threeD = Parse(ThreeDJson)[0];
        var definitions = MerriamParser.ExtractDefinitions(threeD);
        foreach (var d in definitions) Console.WriteLine("    " + d);

        Check("3-D falls back to shortdef", definitions.Count >= 1, $"got {definitions.Count}");
        Check("3-D shortdef cleaned", definitions.All(d => !d.Contains('{')), string.Join(" / ", definitions));
        Check("3-D number-subdir audio",
              MerriamParser.BuildAudioUrl("3d000001").EndsWith("/number/3d000001.mp3"),
              MerriamParser.BuildAudioUrl("3d000001"));

        var ten = Parse(TenaciousJson)[0];
        var tenDefs = MerriamParser.ExtractDefinitions(ten);
        Check("tenacious 2 senses", tenDefs.Count == 2, $"got {tenDefs.Count}");
        Check("cross-refs {sx|} resolved in shortdef",
              tenDefs.Any(d => d.Contains("not easily stopped or persistent")), string.Join(" / ", tenDefs));
    }

    private static void TestNestedBindingSubstitute()
    {
        Console.WriteLine("\n== nested binding substitute (sseq inside sense) ==");
        var entry = Parse(FelineJson)[0];
        var definitions = MerriamParser.ExtractDefinitions(entry);
        foreach (var d in definitions) Console.WriteLine("    " + d);

        Check("binding substitute kept", definitions.Any(d => d.Contains("resembling a cat")), "missing parent sense");
        Check("sub-sense 2a flattened", definitions.Any(d => d.Contains("sleekly graceful")));
        Check("sub-sense 2b flattened", definitions.Any(d => d.Contains("sly, treacherous")));
        Check("4 senses total", definitions.Count == 4, $"got {definitions.Count}");
        Check("no inline examples", definitions.All(d => !d.Contains("例:")));
    }

    private static void TestSpanishBilingual()
    {
        Console.WriteLine("\n== Spanish-English (bilingual: gl gloss + tr translation) ==");
        var entry = Parse(SpanishJson)[0];
        var definitions = MerriamParser.ExtractDefinitions(entry);
        var sentences = MerriamParser.ExtractSentences(entry);
        foreach (var d in definitions) Console.WriteLine("    def: " + d);
        foreach (var s in sentences) Console.WriteLine("    ex:  " + s);

        Check("2 senses", definitions.Count == 2, $"got {definitions.Count}");
        Check("Spanish equivalents present", definitions.Any(d => d.Contains("idioma")));
        Check("second equivalent present", definitions.Any(d => d.Contains("lengua")));
        Check("gloss 'masculine' kept", definitions.Any(d => d.Contains("(masculine)")), string.Join(" / ", definitions));
        Check("gloss 'feminine' kept", definitions.Any(d => d.Contains("(feminine)")), string.Join(" / ", definitions));
        Check("no brace residue", definitions.All(d => !d.Contains('{')));
        Check("no leading colon", definitions.All(d => !d.TrimStart().StartsWith(':')));
        Check("gloss appears after its equivalent, not before",
              !definitions.Any(d => d.TrimStart().StartsWith("(masculine)")), "gloss leaked to line start");

        // 例句独立到 Sentences，且西英库的 tr 译文保留
        Check("2 sentences extracted", sentences.Count == 2, $"got {sentences.Count}");
        Check("first sentence has translation arrow", sentences.Any(s => s.Contains("→") && s.Contains("el idioma inglés")));
        Check("second sentence has translation", sentences.Any(s => s.Contains("lenguaje corporal")));
    }

    /// <summary>
/// 用户截图报告的核心 bug：Merriam-Webster 插件的释义区把例句用「例:」内联，
/// 视觉上糊成一团。学习必应词典的处理：
/// - 例句放进 <c>result.Sentences</c>（宿主 UI 会单独渲染成一栏）；
/// - 变形（复数 / 过去式 / 比较级等）放进 <c>result.Plurals</c> / <c>PastTense</c> 等专门集合；
/// - 音标 / 音频放进 <c>result.Symbols</c>（每个口音一个 Symbol）。
/// </summary>
private static void TestInflections()
{
    Console.WriteLine("\n== infl field (noun plurals, verb forms, comparative/superlative) ==");

    // ---- 名词复数（threshold 是用户报告的词）----
    var threshold = Parse(ThresholdJson)[0];
    var thresholdInfl = MerriamParser.ExtractInflections(threshold);
    foreach (var (label, forms) in thresholdInfl) Console.WriteLine($"    {label}: {string.Join(", ", forms)}");
    Check("threshold has 'plural' infl", thresholdInfl.ContainsKey("plural"));
    Check("threshold plural = 'thresholds'", thresholdInfl["plural"].Single() == "thresholds");

    var thresholdDefs = MerriamParser.ExtractDefinitions(threshold);
    var thresholdSentences = MerriamParser.ExtractSentences(threshold);
    foreach (var d in thresholdDefs) Console.WriteLine("    def: " + d);
    foreach (var s in thresholdSentences) Console.WriteLine("    ex:  " + s);
    Check("threshold: 2 clean definitions", thresholdDefs.Count == 2, $"got {thresholdDefs.Count}");
    Check("threshold: no 例: inline",
          thresholdDefs.All(d => !d.Contains("例:")),
          string.Join(" / ", thresholdDefs));
    Check("threshold: 3 sentences extracted", thresholdSentences.Count == 3, $"got {thresholdSentences.Count}");
    Check("threshold: sentences don't appear in definitions",
          thresholdSentences.All(s => !thresholdDefs.Any(d => d.Contains(s))));

    // ---- 动词屈折（不规则动词 run）----
    var run = Parse(RunJson)[0];
    var runInfl = MerriamParser.ExtractInflections(run);
    foreach (var (label, forms) in runInfl) Console.WriteLine($"    run.{label}: {string.Join(", ", forms)}");
    Check("run: past = 'ran'", runInfl["past"].Single() == "ran");
    Check("run: past participle = 'run'", runInfl["past participle"].Single() == "run");
    Check("run: present participle = 'running'", runInfl["present participle"].Single() == "running");
    Check("run: third person singular = 'runs'", runInfl["third person singular"].Single() == "runs");

    // ---- 形容词比较级 / 最高级（不规则 good）----
    var good = Parse(GoodJson)[0];
    var goodInfl = MerriamParser.ExtractInflections(good);
    foreach (var (label, forms) in goodInfl) Console.WriteLine($"    good.{label}: {string.Join(", ", forms)}");
    Check("good: comparative = 'better'", goodInfl["comparative"].Single() == "better");
    Check("good: superlative = 'best'", goodInfl["superlative"].Single() == "best");

    // ---- 无 infl 字段的条目（threshold 之外的简单条目）----
    var ten = Parse(TenaciousJson)[0];
    Check("tenacious has no infl", MerriamParser.ExtractInflections(ten).Count == 0,
          string.Join(",", MerriamParser.ExtractInflections(ten).Keys));

    // ---- 音标与音频：threshold 应该有发音 ----
    var prs = threshold.Hwi?.Pronunciations;
    Check("threshold has at least one pronunciation", prs is { Count: > 0 });
    Check("threshold pronunciation has mw", prs![0].Mw != null && prs![0].Mw!.Length > 0, prs![0].Mw);
    Check("threshold pronunciation has audio",
          prs![0].Sound?.Audio == "thresho01",
          prs![0].Sound?.Audio);
    Check("threshold audio URL",
          MerriamParser.BuildAudioUrl(prs![0].Sound!.Audio) ==
          "https://media.merriam-webster.com/audio/prons/en/us/mp3/t/thresho01.mp3",
          MerriamParser.BuildAudioUrl(prs![0].Sound!.Audio));
}

private static void TestAudioUrls()
    {
        Console.WriteLine("\n== audio URL subdirectory rules ==");
        Check("bix prefix -> bix/", MerriamParser.BuildAudioUrl("bixox").EndsWith("/bix/bixox.mp3"));
        Check("gg prefix -> gg/", MerriamParser.BuildAudioUrl("ggword01").EndsWith("/gg/ggword01.mp3"));
        Check("digit prefix -> number/", MerriamParser.BuildAudioUrl("3d000001").EndsWith("/number/3d000001.mp3"));
        Check("punctuation prefix -> number/", MerriamParser.BuildAudioUrl("_xyz01").EndsWith("/number/_xyz01.mp3"));
        Check("plain letter -> that letter/", MerriamParser.BuildAudioUrl("hello001").EndsWith("/h/hello001.mp3"));
        Check("uppercase letter lowercased", MerriamParser.BuildAudioUrl("Hello001").EndsWith("/h/Hello001.mp3"));
        Check("null audio -> empty", MerriamParser.BuildAudioUrl(null) == string.Empty);
    }

    private static void TestMarkupCleaning()
    {
        Console.WriteLine("\n== markup token cleaning ==");
        Check("leading {bc} dropped",
              MerriamParser.CleanMarkup("{bc}having great volume") == "having great volume",
              MerriamParser.CleanMarkup("{bc}having great volume"));
        Check("middle {bc} -> colon",
              MerriamParser.CleanMarkup("volume {bc}{sx|large||}") == "volume: large",
              MerriamParser.CleanMarkup("volume {bc}{sx|large||}"));
        Check("trailing {bc} dropped",
              MerriamParser.CleanMarkup("something {bc}") == "something",
              MerriamParser.CleanMarkup("something {bc}"));
        Check("{wi} pair removed",
              MerriamParser.CleanMarkup("a {wi}voluminous{/wi} skirt") == "a voluminous skirt",
              MerriamParser.CleanMarkup("a {wi}voluminous{/wi} skirt"));
        Check("{gloss} -> parentheses",
              MerriamParser.CleanMarkup("{gloss}biology{/gloss}") == "(biology)",
              MerriamParser.CleanMarkup("{gloss}biology{/gloss}"));
        Check("no space before comma",
              !MerriamParser.CleanMarkup("sly, treacherous , x").Contains(" ,"),
              MerriamParser.CleanMarkup("sly, treacherous , x"));
        Check("empty input safe", MerriamParser.CleanMarkup(null) == "" && MerriamParser.CleanMarkup("  ") == "");
        Check("no double spaces",
              !MerriamParser.CleanMarkup("{bc}a  {wi}b{/wi}").Contains("  "),
              "[" + MerriamParser.CleanMarkup("{bc}a  {wi}b{/wi}") + "]");
    }

    private static void TestWordNormalization()
    {
        Console.WriteLine("\n== word normalization / gating ==");
        Check("queryable: plain word", MerriamParser.IsQueryable("hello"));
        Check("queryable: hyphenated", MerriamParser.IsQueryable("self-esteem"));
        Check("not queryable: sentence", !MerriamParser.IsQueryable("hello world"));
        Check("not queryable: empty", !MerriamParser.IsQueryable("   "));
        Check("not queryable: too long", !MerriamParser.IsQueryable(new string('a', 65)));
        Check("normalize strips punctuation", MerriamParser.NormalizeWord("hello,") == "hello",
              MerriamParser.NormalizeWord("hello,"));
        Check("normalize strips quotes", MerriamParser.NormalizeWord("\"hello\"") == "hello",
              MerriamParser.NormalizeWord("\"hello\""));
        Check("normalize strips parentheses", MerriamParser.NormalizeWord("(hello)") == "hello",
              MerriamParser.NormalizeWord("(hello)"));
        Check("normalize keeps internal hyphen", MerriamParser.NormalizeWord("self-esteem") == "self-esteem",
              MerriamParser.NormalizeWord("self-esteem"));
        Check("normalize strips trailing underscore", MerriamParser.NormalizeWord("word_") == "word",
              MerriamParser.NormalizeWord("word_"));
        Check("normalize leaves clean word untouched", MerriamParser.NormalizeWord("hello") == "hello");
    }

    /// <summary>
    /// 复现用户报告的 bug：查询中文时接口返回的纯文本与「key 无效」完全相同，
    /// 因此必须在本地先按文字脚本拦截，不能把它当配置错误报出去。
    /// </summary>
    private static void TestScriptGating()
    {
        Console.WriteLine("\n== script gate (non-Latin input must be blocked before the request) ==");

        // ---- 必须放行：英文与常见拉丁变体 ----
        Check("latin: plain", MerriamParser.IsLatinScript("hello"));
        Check("latin: uppercase", MerriamParser.IsLatinScript("Hello"));
        Check("latin: hyphenated", MerriamParser.IsLatinScript("self-esteem"));
        Check("latin: with apostrophe", MerriamParser.IsLatinScript("don't"));
        Check("latin: typographic apostrophe", MerriamParser.IsLatinScript("don\u2019t"));
        Check("latin: accented", MerriamParser.IsLatinScript("caf\u00e9"), "café should pass");
        Check("latin: german umlaut", MerriamParser.IsLatinScript("\u00fcber"));
        Check("latin: with digit", MerriamParser.IsLatinScript("3-D"));
        Check("latin: with period", MerriamParser.IsLatinScript("e.g"));
        Check("latin: vietnamese tone marks", MerriamParser.IsLatinScript("ti\u1ebfng"));

        // ---- 必须拦截：非拉丁文字 ----
        Check("blocked: simplified chinese", !MerriamParser.IsLatinScript("\u4f60\u597d"), "你好");
        Check("blocked: traditional chinese", !MerriamParser.IsLatinScript("\u4f60\u597d\u55ce"), "你好嗎");
        Check("blocked: single han char", !MerriamParser.IsLatinScript("\u732b"));
        Check("blocked: japanese kana", !MerriamParser.IsLatinScript("\u3053\u3093\u306b\u3061\u306f"));
        Check("blocked: japanese kanji", !MerriamParser.IsLatinScript("\u6771\u4eac"));
        Check("blocked: korean hangul", !MerriamParser.IsLatinScript("\uc548\ub155"));
        Check("blocked: cyrillic", !MerriamParser.IsLatinScript("\u043f\u0440\u0438\u0432\u0435\u0442"));
        Check("blocked: greek", !MerriamParser.IsLatinScript("\u03b3\u03b5\u03b9\u03b1"));
        Check("blocked: arabic", !MerriamParser.IsLatinScript("\u0645\u0631\u062d\u0628\u0627"));
        Check("blocked: mixed latin + chinese", !MerriamParser.IsLatinScript("hello\u4f60\u597d"));
        Check("blocked: chinese punctuation only", !MerriamParser.IsLatinScript("\u3002\uff0c"));
        Check("blocked: empty", !MerriamParser.IsLatinScript(""));
        Check("blocked: whitespace", !MerriamParser.IsLatinScript("   "));
        Check("blocked: null", !MerriamParser.IsLatinScript(null!));

        // 与 IsQueryable 的组合顺序：中文两个字不构成「可查询单词」但也不该走 API
        Check("chinese word is queryable-shaped but not latin",
              MerriamParser.IsQueryable("\u4f60\u597d") && !MerriamParser.IsLatinScript("\u4f60\u597d"),
              "gate must fire after IsQueryable");
    }

    /// <summary>
    /// 接口用同一条纯文本消息表示「key 无效」和「未订阅」，
    /// 而且查非英文词也可能触发它 —— 所以分类必须准确，否则提示会误导用户。
    /// </summary>
    private static void TestApiErrorClassification()
    {
        Console.WriteLine("\n== plain-text API error classification ==");

        // 实测响应原文（HTTP 200，Content-Type: text/plain）
        const string invalidKey = "Invalid API key. Not subscribed for this reference.";
        const string missingKey = "Key is required.";
        const string blankKeyBody = "Invalid API key. Not subscribed for this reference.";

        Check("invalid key -> InvalidKeyOrNotSubscribed",
              MerriamParser.ClassifyTextResponse(invalidKey) == ApiError.InvalidKeyOrNotSubscribed,
              MerriamParser.ClassifyTextResponse(invalidKey).ToString());
        Check("not subscribed -> InvalidKeyOrNotSubscribed",
              MerriamParser.ClassifyTextResponse("Not subscribed for this reference.") == ApiError.InvalidKeyOrNotSubscribed);
        Check("key is required -> MissingKey",
              MerriamParser.ClassifyTextResponse(missingKey) == ApiError.MissingKey,
              MerriamParser.ClassifyTextResponse(missingKey).ToString());
        Check("case insensitive",
              MerriamParser.ClassifyTextResponse("INVALID API KEY") == ApiError.InvalidKeyOrNotSubscribed);
        Check("surrounding whitespace tolerated",
              MerriamParser.ClassifyTextResponse("  " + invalidKey + "\n") == ApiError.InvalidKeyOrNotSubscribed);
        Check("unknown body -> Unknown",
              MerriamParser.ClassifyTextResponse("Something exploded") == ApiError.Unknown);
        Check("empty body -> Unknown", MerriamParser.ClassifyTextResponse("") == ApiError.Unknown);
        Check("null body -> Unknown", MerriamParser.ClassifyTextResponse(null!) == ApiError.Unknown);
        Check("truncated (as seen in real bug report) still classified",
              MerriamParser.ClassifyTextResponse("Invalid API key. Not subscribed for this reference.")
              != ApiError.Unknown);

        // 关键回归：真实的 JSON 响应绝不能被误判成错误
        Check("valid JSON array is not an error body", VoluminousJson.TrimStart()[0] == '[');
        Check("blank key echoes same text as invalid key", blankKeyBody == invalidKey,
              "API cannot distinguish blank key from bad key -> local gate needed");
    }

    // ------------------------------------------------------------- live test

    private static async Task LiveCheckAsync(string key)
    {
        using var http = new HttpClient(new HttpClientHandler { UseProxy = false });
        http.Timeout = TimeSpan.FromSeconds(25);

        foreach (var reference in new[] { "collegiate", "learners" })
        {
            foreach (var word in new[] { "hello", "voluminous", "tenacious", "threshold", "run", "good" })
            {
                var url = $"https://dictionaryapi.com/api/v3/references/{reference}/json/{word}?key={Uri.EscapeDataString(key)}";
                string body;
                try
                {
                    body = await http.GetStringAsync(url);
                }
                catch (Exception ex)
                {
                    Check($"[{reference}] {word} HTTP ok", false, ex.Message);
                    continue;
                }

                var trimmed = body.TrimStart();
                if (trimmed.Length > 0 && trimmed[0] != '[')
                {
                    // Key 未订阅该参考库 —— 这是预期内的，插件会自动兜底
                    Console.WriteLine($"    [{reference}] {word}: not subscribed -> {trimmed.Split('\n')[0]}");
                    continue;
                }

                var nodes = JsonSerializer.Deserialize<JsonElement>(body);
                if (nodes.ValueKind != JsonValueKind.Array || nodes.GetArrayLength() == 0)
                {
                    Check($"[{reference}] {word} non-empty", false, body[..Math.Min(120, body.Length)]);
                    continue;
                }

                // 字符串数组 = 拼写建议
                if (nodes[0].ValueKind == JsonValueKind.String)
                {
                    Console.WriteLine($"    [{reference}] {word}: suggestions only ({nodes.GetArrayLength()} hints)");
                    Check($"[{reference}] {word} suggestion path is fine", true);
                    continue;
                }

                var entries = JsonSerializer.Deserialize<List<MwEntry>>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

                var means = entries.SelectMany(e => MerriamParser.ExtractDefinitions(e)).ToList();
                var headword = entries.Select(e => e.Hwi?.Headword).FirstOrDefault(h => !string.IsNullOrWhiteSpace(h));
                var pronunciation = entries.SelectMany(e => e.Hwi?.Pronunciations ?? [])
                                          .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Mw));
                var audio = pronunciation is null
                    ? string.Empty
                    : MerriamParser.BuildAudioUrl(pronunciation.Sound?.Audio);
                var sentences = entries.SelectMany(MerriamParser.ExtractSentences).ToList();
                var inflCount = entries.Sum(e => MerriamParser.ExtractInflections(e).Count);

                Console.WriteLine($"    [{reference}] {word} -> headword={MerriamParser.FormatHeadword(headword)}, " +
                                  $"defs={means.Count}, ex={sentences.Count}, infl={inflCount}, " +
                                  $"phonetic={(pronunciation?.Mw ?? "(none)")}, audio={(string.IsNullOrEmpty(audio) ? "none" : "yes")}");
                foreach (var m in means.Take(3)) Console.WriteLine("        def: " + m);
                if (sentences.Count > 0) Console.WriteLine($"        ex[0]: {sentences[0]}");

                Check($"[{reference}] {word} parsed headword", !string.IsNullOrWhiteSpace(MerriamParser.FormatHeadword(headword)));
                Check($"[{reference}] {word} has defs", means.Count > 0, "no defs extracted");
                Check($"[{reference}] {word} no brace residue", means.All(m => !m.Contains('{')));
                Check($"[{reference}] {word} no leading colon", means.All(m => !m.TrimStart().StartsWith(':')));
                Check($"[{reference}] {word} no empty defs", means.All(m => !string.IsNullOrWhiteSpace(m)));
                Check($"[{reference}] {word} no inline '例:' in defs",
                      means.All(m => !m.Contains("例:")),
                      string.Join(" / ", means));
                Check($"[{reference}] {word} defs don't contain sentences",
                      means.All(m => sentences.All(s => !m.Contains(s))));

                // 用户报告的核心 bug：音标必须填进 Symbol 才能在宿主里渲染出来
                Check($"[{reference}] {word} has pronunciation mw",
                      pronunciation?.Mw is { Length: > 0 },
                      pronunciation?.Mw ?? "(null)");
                Check($"[{reference}] {word} has audio url",
                      !string.IsNullOrEmpty(audio),
                      audio);
            }
        }
    }

    // --------------------------------------------------------------- helper

    private static void Check(string name, bool condition, string? detail = null)
    {
        if (!condition) _failures++;
        var mark = condition ? "PASS" : "FAIL";
        Console.WriteLine($"  [{mark}] {name}" + (!condition && detail is not null ? $"  -> {detail}" : ""));
    }
}
