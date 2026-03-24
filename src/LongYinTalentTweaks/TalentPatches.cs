using System;
using System.Threading;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace LongYinTalentTweaks;

[HarmonyPatch(typeof(HeroData), nameof(HeroData.GetMaxTagNum))]
internal static class HeroDataGetMaxTagNumPatch
{
    private static void Postfix(HeroData __instance, ref int __result)
    {
        if (__instance == null || __instance.heroID != 0)
        {
            return;
        }

        __result = Math.Max(0, __result + ModConfig.ExtraMaxTags);
    }
}

[HarmonyPatch(typeof(HeroTagData), nameof(HeroTagData.StartChooseAble))]
internal static class HeroTagDataStartChooseAblePatch
{
    private static void Postfix(ref bool __result)
    {
        if (!StartMenuPatchHelpers.ShouldAllowInitiallyUnchoosableStartTags())
        {
            return;
        }

        __result = true;
    }
}

[HarmonyPatch(typeof(HeroTagData), nameof(HeroTagData.GetDescribe))]
internal static class HeroTagDataGetDescribePatch
{
    private const string StartDisabledMarkup = "<color=red>初始不可领悟</color>";

    private static void Postfix(ref string __result)
    {
        if (!StartMenuPatchHelpers.ShouldAllowInitiallyUnchoosableStartTags() || string.IsNullOrEmpty(__result))
        {
            return;
        }

        __result = __result.Replace(StartDisabledMarkup, string.Empty, StringComparison.Ordinal);
    }
}

[HarmonyPatch(typeof(StartMenuController), nameof(StartMenuController.ResetPlayerTag))]
internal static class StartMenuControllerResetPlayerTagPatch
{
    private static void Postfix(StartMenuController __instance)
    {
        HeroData? player = StartGameSettingController.Instance?.Player;
        if (player == null)
        {
            return;
        }

        StartMenuPatchHelpers.ApplyCurrentConfigToStartMenuPlayer(player);
        __instance.RefreshTagMenu();
    }
}

[HarmonyPatch(typeof(StartMenuController), nameof(StartMenuController.RefreshTagMenu))]
internal static class StartMenuControllerRefreshTagMenuPatch
{
    private static void Postfix(StartMenuController __instance)
    {
        StartMenuPatchHelpers.RefreshStartMenu(__instance);
    }
}

[HarmonyPatch(typeof(StartMenuController), nameof(StartMenuController.Update))]
internal static class StartMenuControllerUpdatePatch
{
    private static void Postfix(StartMenuController __instance)
    {
        if (!StartMenuConfigRefreshState.TryConsumeRefreshRequest())
        {
            return;
        }

        StartMenuPatchHelpers.RefreshStartMenu(__instance, applyConfigToPlayer: true);
    }
}

[HarmonyPatch(typeof(StartMenuController), nameof(StartMenuController.StartChooseTagClicked))]
internal static class StartMenuControllerStartChooseTagClickedPatch
{
    private static bool Prefix(StartMenuController __instance, int tagID)
    {
        HeroData? player = StartGameSettingController.Instance?.Player;
        HeroTagDataBase? targetTag = StartMenuPatchHelpers.GetHeroTagDataBase(tagID);
        if (player == null || targetTag == null)
        {
            return true;
        }

        StartMenuPatchHelpers.ApplyCurrentConfigToStartMenuPlayer(player);

        float cost = targetTag.GetCostValue(true);
        if (!StartMenuPatchHelpers.CanStartChooseTag(__instance, player, tagID, targetTag, requireStartChooseAble: true)
            || player.heroTagPoint < cost)
        {
            StartMenuPatchHelpers.PlayUiSound("Sound/SoundEffect/WrongClick");
            return false;
        }

        StartMenuPatchHelpers.PlayUiSound("Sound/SoundEffect/Success");
        player.UnderstandTag(tagID, false);
        player.heroTagPoint -= cost;
        __instance.RefreshTagMenu();
        return false;
    }
}

internal static class StartMenuConfigRefreshState
{
    private static int _refreshRequested;
    private static int _initialized;

    internal static void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0)
        {
            return;
        }

        ModConfig.ConfigChanged += RequestRefresh;
    }

    internal static void RequestRefresh()
    {
        Interlocked.Exchange(ref _refreshRequested, 1);
    }

    internal static bool TryConsumeRefreshRequest()
    {
        return Interlocked.Exchange(ref _refreshRequested, 0) == 1;
    }
}

internal static class StartMenuPatchHelpers
{
    internal static bool ShouldAllowInitiallyUnchoosableStartTags()
    {
        return ModConfig.AllowInitiallyUnchoosableStartTags && StartGameSettingController.Instance != null;
    }

    internal static void RefreshStartMenu(StartMenuController menuController, bool applyConfigToPlayer = false)
    {
        HeroData? player = StartGameSettingController.Instance?.Player;
        if (player == null)
        {
            return;
        }

        if (applyConfigToPlayer)
        {
            ApplyCurrentConfigToStartMenuPlayer(player);
        }

        UpdateTagPointText(menuController, player);
        UpdateTagLimitText(menuController, player);
        RefreshAllTagButtons(menuController, player);
    }

    internal static void ApplyCurrentConfigToStartMenuPlayer(HeroData player)
    {
        player.heroTagPoint = GetConfiguredStartingTagPoints() - GetCurrentSelectedStartTagCost(player);
    }

    internal static int GetAchievementTagPoints()
    {
        try
        {
            RePlayerPrefData? prefData = GameDataController.playerPrefData;
            PlayerPrefDictionary? dictionary = prefData?.playerPrefData;
            return dictionary?.GetInt("AchTagPoint") ?? 0;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"读取 AchTagPoint 失败，将按 0 处理: {ex.Message}");
            return 0;
        }
    }

    internal static float GetConfiguredStartingTagPoints()
    {
        float startingPoints = ModConfig.StartingTagPoints;
        if (ModConfig.IncludeAchievementTagPoints)
        {
            startingPoints += GetAchievementTagPoints();
        }

        return startingPoints;
    }

    internal static float GetCurrentSelectedStartTagCost(HeroData player)
    {
        if (player.heroTagData == null)
        {
            return 0f;
        }

        float totalCost = 0f;
        for (int index = 0; index < player.heroTagData.Count; index += 1)
        {
            HeroTagData? selectedTag = player.heroTagData[index];
            HeroTagDataBase? selectedTagData = selectedTag?.DataBase();
            if (selectedTagData == null)
            {
                continue;
            }

            totalCost += selectedTagData.GetCostValue(true);
        }

        return totalCost;
    }

    internal static HeroTagDataBase? GetHeroTagDataBase(int tagID)
    {
        try
        {
            GameDataController? controller = GameDataController.Instance;
            if (controller?.heroTagDataBase == null || tagID < 0 || tagID >= controller.heroTagDataBase.Count)
            {
                return null;
            }

            return controller.heroTagDataBase[tagID];
        }
        catch
        {
            return null;
        }
    }

    internal static void UpdateTagPointText(StartMenuController menuController, HeroData player)
    {
        GameObject? tagRoot = menuController.tagRoot;
        if (tagRoot == null)
        {
            return;
        }

        Transform? tagPointTransform = tagRoot.transform.Find("TagPointNum");
        if (tagPointTransform == null)
        {
            return;
        }

        Text? tagPointText = tagPointTransform.GetComponent<Text>();
        if (tagPointText == null)
        {
            return;
        }

        LTLocalization.SetText(tagPointText, $"天赋点 {player.heroTagPoint:0.##}");
    }

    internal static void UpdateTagLimitText(StartMenuController menuController, HeroData player)
    {
        GameObject? tagRoot = menuController.tagRoot;
        if (tagRoot == null)
        {
            return;
        }

        Transform? tagNumTransform = tagRoot.transform.Find("TagNum");
        if (tagNumTransform == null)
        {
            return;
        }

        Text? tagNumText = tagNumTransform.GetComponent<Text>();
        if (tagNumText == null)
        {
            return;
        }

        int currentTagCount = player.heroTagData?.Count ?? 0;
        LTLocalization.SetText(tagNumText, $"初始上限 {currentTagCount}/{ModConfig.StartingTagLimit}");
    }

    internal static void RefreshAllTagButtons(StartMenuController menuController, HeroData player)
    {
        if (menuController.allTagGrid == null)
        {
            return;
        }

        for (int gridIndex = 0; gridIndex < menuController.allTagGrid.Count; gridIndex += 1)
        {
            GameObject? tagGrid = menuController.allTagGrid[gridIndex];
            if (tagGrid == null)
            {
                continue;
            }

            Transform gridTransform = tagGrid.transform;
            int childCount = gridTransform.childCount;
            for (int childIndex = 0; childIndex < childCount; childIndex += 1)
            {
                Transform? child = gridTransform.GetChild(childIndex);
                if (child == null)
                {
                    continue;
                }

                HeroTagIconController? icon = child.GetComponent<HeroTagIconController>();
                Button? button = child.GetComponent<Button>();
                if (icon == null || button == null)
                {
                    continue;
                }

                HeroTagData? targetTag = icon.targetTag;
                HeroTagDataBase? targetTagData = targetTag?.DataBase();
                bool canChoose = targetTag != null
                    && targetTagData != null
                    && targetTag.StartChooseAble()
                    && CanStartChooseTag(menuController, player, targetTag.tagID, targetTagData, requireStartChooseAble: false);

                button.interactable = canChoose;
                icon.RefreshInfo();
            }
        }
    }

    internal static bool CanStartChooseTag(
        StartMenuController menuController,
        HeroData player,
        int tagID,
        HeroTagDataBase targetTag,
        bool requireStartChooseAble)
    {
        if (requireStartChooseAble)
        {
            HeroTagData previewTag = new(tagID, -1f, string.Empty);
            if (!previewTag.StartChooseAble())
            {
                return false;
            }
        }

        int currentTagCount = player.GetHeroPermanentTagNum();
        if (currentTagCount >= ModConfig.StartingTagLimit)
        {
            if (targetTag.replaceTag == null || targetTag.replaceTag.Count <= 0)
            {
                return false;
            }
        }

        if (HasConflictingTag(player, tagID, targetTag))
        {
            return false;
        }

        return menuController.CheckMeetCondition(player, targetTag);
    }

    internal static bool HasConflictingTag(HeroData player, int tagID, HeroTagDataBase targetTag)
    {
        if (player.heroTagData == null)
        {
            return false;
        }

        string sameMeaning = targetTag.sameMeaning ?? string.Empty;
        int targetValue = Math.Abs(targetTag.value);

        for (int index = 0; index < player.heroTagData.Count; index += 1)
        {
            HeroTagData? currentTag = player.heroTagData[index];
            if (currentTag == null)
            {
                continue;
            }

            if (currentTag.tagID == tagID)
            {
                return true;
            }

            if (string.IsNullOrEmpty(sameMeaning))
            {
                continue;
            }

            HeroTagDataBase? currentTagData = currentTag.DataBase();
            if (currentTagData == null)
            {
                continue;
            }

            if (string.Equals(currentTagData.oppositeMeaning, sameMeaning, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(currentTagData.sameMeaning, sameMeaning, StringComparison.Ordinal)
                && Math.Abs(currentTagData.value) >= targetValue)
            {
                return true;
            }
        }

        return false;
    }

    internal static void PlayUiSound(string path, float volume = 1f)
    {
        try
        {
            AudioClip? clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                return;
            }

            if (Math.Abs(volume - 1f) < 0.001f)
            {
                NGUITools.PlaySound(clip);
            }
            else
            {
                NGUITools.PlaySound(clip, volume);
            }
        }
        catch
        {
        }
    }
}
