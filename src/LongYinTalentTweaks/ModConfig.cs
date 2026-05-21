using System;
using MelonLoader;

namespace LongYinTalentTweaks;

internal static class ModConfig
{
    private static MelonPreferences_Category? _category;
    private static MelonPreferences_Entry<float>? _startingTagPoints;
    private static MelonPreferences_Entry<bool>? _includeAchievementTagPoints;
    private static MelonPreferences_Entry<int>? _startingTagLimit;
    private static MelonPreferences_Entry<int>? _extraMaxTags;
    private static MelonPreferences_Entry<bool>? _allowInitiallyUnchoosableStartTags;

    internal static event Action? ConfigChanged;

    internal static float StartingTagPoints => Math.Max(0f, _startingTagPoints?.Value ?? 20f);

    internal static bool IncludeAchievementTagPoints => _includeAchievementTagPoints?.Value ?? true;

    internal static int StartingTagLimit => Math.Max(0, _startingTagLimit?.Value ?? 5);

    internal static int ExtraMaxTags => Math.Max(0, _extraMaxTags?.Value ?? 0);

    internal static bool AllowInitiallyUnchoosableStartTags => _allowInitiallyUnchoosableStartTags?.Value ?? false;

    internal static void Initialize()
    {
        _category = MelonPreferences.CreateCategory("LongYinTalentTweaks", "天赋点修改");
        _startingTagPoints = _category.CreateEntry("StartingTagPoints", 20f, "基础天赋点数。");
        _includeAchievementTagPoints = _category.CreateEntry("IncludeAchievementTagPoints", true, "是否添加成就额外天赋点。");
        _startingTagLimit = _category.CreateEntry("StartingTagLimit", 5, "基础天赋数量上限。");
        _extraMaxTags = _category.CreateEntry("ExtraMaxTags", 0, "难度额外天赋上限。");
        _allowInitiallyUnchoosableStartTags = _category.CreateEntry("AllowInitiallyUnchoosableStartTags", false, "允许初始不可领悟的天赋在创建角色时被领悟。");

        SubscribeToEntryChanges(_startingTagPoints);
        SubscribeToEntryChanges(_includeAchievementTagPoints);
        SubscribeToEntryChanges(_startingTagLimit);
        SubscribeToEntryChanges(_extraMaxTags);
        SubscribeToEntryChanges(_allowInitiallyUnchoosableStartTags);

        _category.LoadFromFile(false);
        MelonPreferences.Save();
    }

    private static void SubscribeToEntryChanges<T>(MelonPreferences_Entry<T>? entry)
    {
        if (entry == null)
        {
            return;
        }

        entry.OnEntryValueChanged.Subscribe(
            new LemonAction<T, T>(static (_, _) => NotifyConfigChanged()),
            0,
            false);
    }

    private static void NotifyConfigChanged()
    {
        Action? callback = ConfigChanged;
        if (callback == null)
        {
            return;
        }

        try
        {
            callback.Invoke();
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"处理配置变更回调失败: {ex.Message}");
        }
    }
}
