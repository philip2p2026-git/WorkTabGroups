using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTabGroups.Patches
{
    [HarmonyPatch(typeof(Game))]
    public static class Patch_Game_Constructor
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (ConstructorInfo ctor in AccessTools.GetDeclaredConstructors(typeof(Game)))
            {
                yield return ctor;
            }
        }

        public static void Postfix(Game __instance)
        {
            __instance.components.RemoveAll(c => c is WorkTabGroupsManager);
            WorkTabGroupsManager.ClearInstance();
        }
    }

    [HarmonyPatch(typeof(GameComponentUtility), "StartedNewGame")]
    public static class Patch_GameComponentUtility_StartedNewGame
    {
        public static void Postfix()
        {
            if (WorkTabGroupsMod.Settings?.defaultLayoutPresetName.NullOrEmpty() == false)
            {
                WorkTabGroupsManager.EnsureRegistered()?.TryApplyDefaultLayoutOnNewGame();
            }
        }
    }
}
