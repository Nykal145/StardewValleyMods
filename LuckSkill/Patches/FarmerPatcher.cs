using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Spacechase.Shared.Patching;
using SpaceShared;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;

namespace LuckSkill.Patches
{
    /// <summary>Applies Harmony patches to <see cref="Farmer"/>.</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = DiagnosticMessages.NamedForHarmony)]
    internal class FarmerPatcher : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<Farmer>(nameof(Farmer.gainExperience)),
                prefix: this.GetHarmonyMethod(nameof(Before_GainExperience))
            );

            harmony.Patch(
                original: this.RequireMethod<Farmer>(nameof(Farmer.getProfessionForSkill)),
                postfix: this.GetHarmonyMethod(nameof(After_GetProfessionForSkill))
            );
        }

        /// <summary>The method to call before <see cref="Farmer.gainExperience"/>.</summary>
        private static bool Before_GainExperience(Farmer __instance, int which, ref int howMuch)
        {
            // This fixes overpowered geodes.

            if (which == Farmer.luckSkill && Game1.currentLocation is MineShaft ms)
            {
                bool foundGeode = false;
                var st = new StackTrace();
                foreach (var frame in st.GetFrames())
                {
                    if (frame.GetMethod().Name.Contains(nameof(MineShaft.checkStoneForItems)))
                    {
                        foundGeode = true;
                        break;
                    }
                }

                if (foundGeode)
                {
                    int msa = ms.getMineArea();
                    if (msa != 0)
                    {
                        howMuch /= msa;
                    }
                }
            }

            if (howMuch <= 0)
            {
                return false;
            }

            if (!__instance.IsLocalPlayer && Game1.IsServer)
            {
                __instance.queueMessage(17, Game1.player, which, howMuch);
                return false;
            }
            if (((int)__instance.farmingLevel + (int)__instance.fishingLevel + (int)__instance.foragingLevel + (int)__instance.combatLevel + (int)__instance.miningLevel) / 2 >= 25) 
            {
                int currentMasteryLevel = MasteryTrackerMenu.getCurrentMasteryLevel();
                Game1.stats.Increment("MasteryExp", howMuch);
                if (MasteryTrackerMenu.getCurrentMasteryLevel() > currentMasteryLevel)
                {
                    Game1.showGlobalMessage(Game1.content.LoadString("Strings\\1_6_Strings:Mastery_newlevel"));
                    Game1.playSound("newArtifact");
                }
            }
            else
            {
                Game1.stats.Set("MasteryExp", 0);
            }

            Log.DebugOnlyLog("Skill: " + which.ToString());
            Log.DebugOnlyLog("How Much: " + howMuch.ToString());
            Log.DebugOnlyLog("current xp: " + __instance.experiencePoints[which].ToString());
            Log.DebugOnlyLog("new xp: " + (__instance.experiencePoints[which] + howMuch).ToString());
            Log.DebugOnlyLog("Level Gain: " + Farmer.checkForLevelGain(__instance.experiencePoints[which], __instance.experiencePoints[which] + howMuch).ToString());
            int num = Farmer.checkForLevelGain(__instance.experiencePoints[which], __instance.experiencePoints[which] + howMuch);
            __instance.experiencePoints[which] += howMuch;
            int num2 = -1;
            if (num != -1)
            {
                switch (which)
                {
                    case 0:
                        num2 = __instance.farmingLevel;
                        __instance.farmingLevel.Value = num;
                        break;
                    case 3:
                        num2 = __instance.miningLevel;
                        __instance.miningLevel.Value = num;
                        break;
                    case 1:
                        num2 = __instance.fishingLevel;
                        __instance.fishingLevel.Value = num;
                        break;
                    case 2:
                        num2 = __instance.foragingLevel;
                        __instance.foragingLevel.Value = num;
                        break;
                    case 5:
                        num2 = __instance.luckLevel;
                        __instance.luckLevel.Value = num;
                        break;
                    case 4:
                        num2 = __instance.combatLevel;
                        __instance.combatLevel.Value = num;
                        break;
                }
            }

            if (num <= num2)
            {
                return false;
            }

            for (int i = num2 + 1; i <= num; i++)
            {
                __instance.newLevels.Add(new Point(which, i));
                if (__instance.newLevels.Count == 1)
                {
                    Game1.showGlobalMessage(Game1.content.LoadString("Strings\\1_6_Strings:NewIdeas"));
                }
            }
            return false;
        }

        /// <summary>The method to call after <see cref="Farmer.getProfessionForSkill"/>.</summary>
        public static void After_GetProfessionForSkill(Farmer __instance, int skillType, int skillLevel, ref int __result)
        {
            // Get profession hook

            if (skillType != Farmer.luckSkill)
                return;

            if (skillLevel == 5)
            {
                if (__instance.professions.Contains(Mod.FortunateProfessionId))
                    __result = Mod.FortunateProfessionId;
                else if (__instance.professions.Contains(Mod.PopularHelperProfessionId))
                    __result = Mod.PopularHelperProfessionId;
            }
            else if (skillLevel == 10)
            {
                if (__instance.professions.Contains(Mod.FortunateProfessionId))
                {
                    if (__instance.professions.Contains(Mod.LuckyProfessionId))
                        __result = Mod.LuckyProfessionId;
                    else if (__instance.professions.Contains(Mod.UnUnluckyProfessionId))
                        __result = Mod.UnUnluckyProfessionId;
                }
                else if (__instance.professions.Contains(Mod.PopularHelperProfessionId))
                {
                    if (__instance.professions.Contains(Mod.ShootingStarProfessionId))
                        __result = Mod.ShootingStarProfessionId;
                    else if (__instance.professions.Contains(Mod.SpiritChildProfessionId))
                        __result = Mod.SpiritChildProfessionId;
                }
            }
        }
    }
}
