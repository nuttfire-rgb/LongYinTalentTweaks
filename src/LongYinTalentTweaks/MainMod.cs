using HarmonyLib;
using MelonLoader;

[assembly: MelonInfo(typeof(LongYinTalentTweaks.MainMod), "LongYinTalentTweaks", "0.1.0", "skysw")]
[assembly: MelonGame("TppStudio", "LongYinLiZhiZhuan")]

namespace LongYinTalentTweaks;

public sealed class MainMod : MelonMod
{
    private HarmonyLib.Harmony? _harmony;

    public override void OnInitializeMelon()
    {
        ModConfig.Initialize();
        StartMenuConfigRefreshState.Initialize();

        _harmony = new HarmonyLib.Harmony("LongYinTalentTweaks");
        _harmony.PatchAll(typeof(MainMod).Assembly);

        MelonLogger.Msg(
            $"LongYinTalentTweaks loaded. StartingTagPoints={ModConfig.StartingTagPoints:0.##}, StartingTagLimit={ModConfig.StartingTagLimit}, ExtraMaxTags={ModConfig.ExtraMaxTags}");
    }
}
