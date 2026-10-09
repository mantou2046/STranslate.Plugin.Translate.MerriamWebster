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

    private static List<MwEntry> Parse(string json) =>
        JsonSerializer.Deserialize<List<MwEntry>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    // ------------------------------------------------------------ the tests

    private static void TestVoluminous()
    {
        Console.WriteLine("== voluminous (full def tree, sdsense, examples) ==");
        var entry = Parse(VoluminousJson)[0];

        var means = MerriamParser.ExtractSenses(entry, showExamples: true);
        foreach (var m in means) Console.WriteLine("    " + m);

        Check("5 sense lines", means.Count == 5, $"got {means.Count}");
        Check("no leading colon anywhere", means.All(m => !m.TrimStart().StartsWith(':')), "a definition starts with ':'");
        Check("sdsense is its own line prefixed 'also: '", means.Any(m => m.StartsWith("also: ") && m.Contains("full")));
        Check("cross-ref {sx|large||} resolved to 'large'", means.Any(m => m.Contains("large")));
        Check("example from dt kept on main sense", means.Any(m => m.StartsWith("having") && m.Contains("例:") && m.Contains("tresses")));
        Check("example from sdsense kept", means.Any(m => m.StartsWith("also:") && m.Contains("skirt")));
        Check("middle boundary rendered as colon", means.Any(m => m.Contains("bulk: large")));
        Check("sense 1b present", means.Any(m => m == "numerous"));
        Check("sense 3 present", means.Any(m => m.Contains("many folds")));
        Check("no brace residue", means.All(m => !m.Contains('{') && !m.Contains('}')));

        // without examples
        var bare = MerriamParser.ExtractSenses(entry, showExamples: false);
        Check("examples omitted when disabled", bare.All(m => !m.Contains("例:")), string.Join(" / ", bare));

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
        var means = MerriamParser.ExtractSenses(threeD, showExamples: true);
        foreach (var m in means) Console.WriteLine("    " + m);

        Check("3-D falls back to shortdef", means.Count >= 1, $"got {means.Count}");
        Check("3-D shortdef cleaned", means.All(m => !m.Contains('{')), string.Join(" / ", means));
        Check("3-D number-subdir audio",
              MerriamParser.BuildAudioUrl("3d000001").EndsWith("/number/3d000001.mp3"),
              MerriamParser.BuildAudioUrl("3d000001"));

        var ten = Parse(TenaciousJson)[0];
        var tenMeans = MerriamParser.ExtractSenses(ten, showExamples: false);
        Check("tenacious 2 senses", tenMeans.Count == 2, $"got {tenMeans.Count}");
        Check("cross-refs {sx|} resolved in shortdef",
              tenMeans.Any(m => m.Contains("not easily stopped or persistent")), string.Join(" / ", tenMeans));
    }

    private static void TestNestedBindingSubstitute()
    {
        Console.WriteLine("\n== nested binding substitute (sseq inside sense) ==");
        var entry = Parse(FelineJson)[0];
        var means = MerriamParser.ExtractSenses(entry, showExamples: true);
        foreach (var m in means) Console.WriteLine("    " + m);

        Check("binding substitute kept", means.Any(m => m.Contains("resembling a cat")), "missing parent sense");
        Check("sub-sense 2a flattened", means.Any(m => m.Contains("sleekly graceful")));
        Check("sub-sense 2b flattened", means.Any(m => m.Contains("sly, treacherous")));
        Check("4 senses total", means.Count == 4, $"got {means.Count}");
    }

    private static void TestSpanishBilingual()
    {
        Console.WriteLine("\n== Spanish-English (bilingual: gl gloss + tr translation) ==");
        var entry = Parse(SpanishJson)[0];
        var means = MerriamParser.ExtractSenses(entry, showExamples: true);
        foreach (var m in means) Console.WriteLine("    " + m);

        Check("2 senses", means.Count == 2, $"got {means.Count}");
        Check("Spanish equivalents present", means.Any(m => m.Contains("idioma")));
        Check("second equivalent present", means.Any(m => m.Contains("lengua")));
        Check("gloss 'masculine' kept", means.Any(m => m.Contains("(masculine)")), string.Join(" / ", means));
        Check("gloss 'feminine' kept", means.Any(m => m.Contains("(feminine)")), string.Join(" / ", means));
        Check("example translation via tr kept", means.Any(m => m.Contains("→") && m.Contains("el idioma inglés")),
              string.Join(" / ", means));
        Check("second sense translation kept", means.Any(m => m.Contains("lenguaje corporal")));
        Check("no brace residue", means.All(m => !m.Contains('{')));
        Check("no leading colon", means.All(m => !m.TrimStart().StartsWith(':')));
        Check("gloss appears after its equivalent, not before",
              !means.Any(m => m.TrimStart().StartsWith("(masculine)")), "gloss leaked to line start");

        var bare = MerriamParser.ExtractSenses(entry, showExamples: false);
        Check("examples omitted but glosses kept when disabled",
              bare.All(m => !m.Contains("→")) && bare.Any(m => m.Contains("(masculine)")),
              string.Join(" / ", bare));
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
            foreach (var word in new[] { "hello", "voluminous", "tenacious" })
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

                var means = entries.SelectMany(e => MerriamParser.ExtractSenses(e, true)).ToList();
                var headword = entries.Select(e => e.Hwi?.Headword).FirstOrDefault(h => !string.IsNullOrWhiteSpace(h));
                var audio = entries.SelectMany(e => e.Hwi?.Pronunciations ?? [])
                                   .Select(p => MerriamParser.BuildAudioUrl(p.Sound?.Audio))
                                   .FirstOrDefault(a => !string.IsNullOrWhiteSpace(a));

                Console.WriteLine($"    [{reference}] {word} -> headword={MerriamParser.FormatHeadword(headword)}, " +
                                  $"senses={means.Count}, audio={(string.IsNullOrEmpty(audio) ? "none" : "yes")}");
                foreach (var m in means.Take(3)) Console.WriteLine("        " + m);

                Check($"[{reference}] {word} parsed headword", !string.IsNullOrWhiteSpace(MerriamParser.FormatHeadword(headword)));
                Check($"[{reference}] {word} has senses", means.Count > 0, "no senses extracted");
                Check($"[{reference}] {word} no brace residue", means.All(m => !m.Contains('{')));
                Check($"[{reference}] {word} no leading colon", means.All(m => !m.TrimStart().StartsWith(':')));
                Check($"[{reference}] {word} no empty defs", means.All(m => !string.IsNullOrWhiteSpace(m)));
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
