# LongYinTalentTweaks

《龙胤立志传》MelonLoader Mod，用于修改建角阶段和角色天赋相关的限制。

## 当前功能

当前已实现：

1. 修改创建角色时的初始天赋点数。
2. 控制是否叠加成就提供的额外天赋点。
3. 修改创建角色时可选择的初始天赋数量上限。
4. 为正式角色天赋上限统一增加额外值。
5. 允许原本“初始不可领悟”的天赋在创建角色时直接领悟。
6. 配置文件修改后，建角界面打开状态下可自动刷新当前 `StartMenu`。

## 配置项

本 Mod 使用 `MelonPreferences`，配置文件位于：

```text
<游戏目录>\UserData\MelonPreferences.cfg
```

分类名为：

```text
[天赋点修改]
```

当前配置项：

- `StartingTagPoints`：创建角色时的基础天赋点数。
- `IncludeAchievementTagPoints`：是否叠加成就提供的 `AchTagPoint`。
- `StartingTagLimit`：创建角色时的基础天赋数量上限。
- `ExtraMaxTags`：所有难度统一额外增加的正式天赋上限。
- `AllowInitiallyUnchoosableStartTags`：是否允许“初始不可领悟”的天赋在建角时直接选择。

## 使用说明

### 前置条件

1. 已安装《龙胤立志传》。
2. 已安装 MelonLoader。
3. `Directory.Build.props` 中的游戏路径与本机实际路径一致。

当前工程默认路径：

```xml
<LongYinGameDir>E:\SteamLibrary\steamapps\common\LongYinLiZhiZhuan</LongYinGameDir>
```

### 构建

```powershell
dotnet build D:\codes\LongYin\LongYinTalentTweaks\LongYinTalentTweaks.sln -c Release
```

构建完成后会自动复制到：

```text
<游戏目录>\Mods\LongYinTalentTweaks.dll
```

### 运行时改配置

当创建角色界面 `StartMenu` 已打开时，直接修改并保存 `MelonPreferences.cfg`，本 Mod 会通过 MelonLoader 的配置变更回调触发刷新，当前界面会重新计算：

- 剩余天赋点
- 初始天赋数量上限
- 可点击的起始天赋

## 开发注意

1. 游戏运行时，`Mods\LongYinTalentTweaks.dll` 会被 `LongYinLiZhiZhuan.exe` 锁定。
2. 如果游戏未关闭，构建虽然可能成功，但自动复制到 `Mods` 目录会失败。
3. 调试代码改动时，建议先退出游戏再重新构建。

## 项目结构

```text
LongYinTalentTweaks
├─ Directory.Build.props
├─ LongYinTalentTweaks.sln
├─ README.md
└─ src
   └─ LongYinTalentTweaks
      ├─ LongYinTalentTweaks.csproj
      ├─ MainMod.cs
      ├─ ModConfig.cs
      └─ TalentPatches.cs
```

核心文件说明：

- `MainMod.cs`：MelonLoader 入口，初始化配置和 Harmony 补丁。
- `ModConfig.cs`：定义配置项，并接入 MelonLoader 配置变更回调。
- `TalentPatches.cs`：实现建角与天赋上限相关的 Harmony 补丁逻辑。
