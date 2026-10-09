using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace STranslate.Plugin.Translate.MerriamWebster.ViewModel;

/// <summary>
/// Merriam-Webster 字典插件设置视图模型，任意字段变更后立即持久化。
/// </summary>
public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IPluginContext _context;
    private readonly Settings _settings;

    [ObservableProperty]
    public partial string ApiKey { get; set; }

    [ObservableProperty]
    public partial MerriamReference Reference { get; set; }

    [ObservableProperty]
    public partial bool FallbackToCollegiate { get; set; }

    [ObservableProperty]
    public partial bool ShowExamples { get; set; }

    public SettingsViewModel(IPluginContext context, Settings settings)
    {
        _context = context;
        _settings = settings;

        ApiKey = settings.ApiKey;
        Reference = settings.Reference;
        FallbackToCollegiate = settings.FallbackToCollegiate;
        ShowExamples = settings.ShowExamples;

        PropertyChanged += OnSettingsPropertyChanged;
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ApiKey):
                _settings.ApiKey = ApiKey;
                break;
            case nameof(Reference):
                _settings.Reference = Reference;
                break;
            case nameof(FallbackToCollegiate):
                _settings.FallbackToCollegiate = FallbackToCollegiate;
                break;
            case nameof(ShowExamples):
                _settings.ShowExamples = ShowExamples;
                break;
            default:
                return;
        }

        _context.SaveSettingStorage<Settings>();
    }

    public void Dispose() => PropertyChanged -= OnSettingsPropertyChanged;
}
