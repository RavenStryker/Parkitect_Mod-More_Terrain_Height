using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ImprovedLandscapeHeight
{
    public class MTH : AbstractMod, IModSettings
    {
        public const string VERSION_NUMBER = "v1.0";

        public override string getIdentifier() => "ImprovedLandscapeHeight";

        public override string getName() => "Improved Landscape Height";

        public override string getDescription() => @"Mod Inventor/Upkeep: RavenStryker" + Environment.NewLine + "Project Author: ChrisBradel" + Environment.NewLine + "Description: Improve your scenarios with a more dynamic landscape! Increases the default landscape height from 16 units, to 30 units!";

        public override string getVersionNumber() => VERSION_NUMBER;

        public override bool isMultiplayerModeCompatible() => true;

        public override bool isRequiredByAllPlayersInMultiplayerMode() => false;

        public GameObject go { get; private set; }
        public static MTH Instance;
        private Harmony _harmony;

        private static bool debug_mode = false;
        public static bool MOD_ENABLED = false;

        public static string _local_mods_directory = "";

        public static void MTHDebug(string debug_string, bool always_show = false)
        {
            if (debug_mode || always_show)
            {
                Debug.LogWarning("ImprovedLandscapeHeight Units: " + debug_string);

                if (_local_mods_directory != "")
                    File.AppendAllText(_local_mods_directory + "/ImprovedLandscapeHeight.txt", "ImprovedLandscapeHeight: " + debug_string + "\n");
            }

            if (File.Exists(_local_mods_directory + "/mth_debug"))
            {
                debug_mode = true;
                MTHDebug("Found debug flag file.");
            }
        }

        public MTH()
        {
            _local_mods_directory = GameController.modsPath;

            if (!MOD_ENABLED)
            {
                _harmony = new Harmony(getIdentifier());
                _harmony.PatchAll();
                MOD_ENABLED = true;
                MTHDebug(debug_string: "ENABLING MTH", always_show: true);
            }
        }

        public override void onEnabled()
        {
            Instance = this;

            if (!MOD_ENABLED)
            {
                _harmony = new Harmony(getIdentifier());
                _harmony.PatchAll();
                MOD_ENABLED = true;
                MTHDebug(debug_string: "ENABLING MTH", always_show: true);
            }

            MTHDebug("Modspath: " + GameController.modsPath, always_show: true);
            MTHDebug("ModspathRel: " + GameController.modsPathRelative, always_show: true);

            go = new GameObject();
            go.name = "MTH GameObject";
        }

        public override void onDisabled()
        {
            UnityEngine.Object.DestroyImmediate(go);

            if (MOD_ENABLED)
            {
                _harmony.UnpatchAll(getIdentifier());
                MOD_ENABLED = false;
                MTHDebug(debug_string: "DISABLING MTH", always_show: true);
            }
        }

        public void onDrawSettingsUI()
        {
        }

        public void onSettingsClosed()
        {
        }

        public void onSettingsOpened()
        {
        }
    }

    [HarmonyPatch]
    public class ImprovedLandscapeHeightPatch
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Trust me, it's used. It's found by Harmony and not referred to by this mod directly.")]
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(LandPatch), "changeHeight", parameters: new Type[]
        {
            typeof(int),typeof(float),typeof(bool)
        });

        [HarmonyPrefix]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "This is how Parkitect capitalizes it, so it has to match.")]
        public static bool changeHeight(ref int __result, LandPatch __instance, int cornerIndex, float delta, bool checkCollisions = true)
        {
            float new_terrain_min = 0f; // Originally 0f
            float new_terrain_max = 30f; // Originally 16f

            FieldInfo newHeightsField = typeof(LandPatch).GetField("newHeights", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            float[] newHeights;
            if (newHeightsField != null)
            {
                newHeights = (float[])newHeightsField.GetValue(__instance);
            }
            else
            {
                return true;
            }

            if (delta == 0f || (!__instance.BelongsToPark && !GameController.Instance.isInScenarioEditor))
            {
                __result = 0;
                return false;
            }
            newHeights[0] = __instance.h[0];
            newHeights[1] = __instance.h[1];
            newHeights[2] = __instance.h[2];
            newHeights[3] = __instance.h[3];
            newHeights[cornerIndex] = Mathf.Clamp(newHeights[cornerIndex] + delta, new_terrain_min, new_terrain_max);

            MethodInfo makeValidIndexMethod = typeof(LandPatch).GetMethod("makeValidIndex", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            if (makeValidIndexMethod == null)
            {
                return true;
            }

            int num = (int)makeValidIndexMethod.Invoke(__instance, new object[] { cornerIndex + 1 });
            int num2 = (int)makeValidIndexMethod.Invoke(__instance, new object[] { cornerIndex - 1 });
            int num3 = (int)makeValidIndexMethod.Invoke(__instance, new object[] { cornerIndex + 2 });
            float num4 = Mathf.Abs(delta);
            if (Mathf.Abs(newHeights[cornerIndex] - newHeights[num]) > num4)
            {
                newHeights[num] = Mathf.Clamp(newHeights[num] + delta, new_terrain_min, new_terrain_max);
            }
            if (Mathf.Abs(newHeights[cornerIndex] - newHeights[num2]) > num4)
            {
                newHeights[num2] = Mathf.Clamp(newHeights[num2] + delta, new_terrain_min, new_terrain_max);
            }
            if ((Mathf.Abs(newHeights[num2] - newHeights[num3]) > num4 || Mathf.Abs(newHeights[num] - newHeights[num3]) > num4) && Mathf.Abs(newHeights[num2] - newHeights[num3] - delta) <= num4 && Mathf.Abs(newHeights[num] - newHeights[num3] - delta) <= num4)
            {
                newHeights[num3] = Mathf.Clamp(newHeights[num3] + delta, new_terrain_min, new_terrain_max);
            }
            if (checkCollisions)
            {
                FieldInfo collisionTestPointsField = typeof(LandPatch).GetField("collisionTestPoints", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (collisionTestPointsField == null)
                {
                    return true;
                }

                Vector3[] collisionTestPoints = (Vector3[])collisionTestPointsField.GetValue(__instance);

                bool flag = false;
                collisionTestPoints[0] = __instance.BaseCornerPoints[cornerIndex];
                collisionTestPoints[0].y = newHeights[cornerIndex];
                collisionTestPoints[1] = __instance.BaseCornerPoints[num];
                collisionTestPoints[1].y = newHeights[num];
                collisionTestPoints[2] = __instance.BaseCornerPoints[num2];
                collisionTestPoints[2].y = newHeights[num2];
                collisionTestPoints[3] = __instance.BaseCornerPoints[num3];
                collisionTestPoints[3].y = newHeights[num3];
                collisionTestPoints[4] = __instance.BaseCornerPoints[cornerIndex] + (__instance.BaseCornerPoints[num] - __instance.BaseCornerPoints[cornerIndex]) / 4f + (__instance.BaseCornerPoints[num2] - __instance.BaseCornerPoints[cornerIndex]) / 4f;
                collisionTestPoints[4].y = newHeights[cornerIndex] + (newHeights[num] - newHeights[cornerIndex]) / 4f + (newHeights[num2] - newHeights[cornerIndex]) / 4f;
                collisionTestPoints[5] = __instance.BaseCornerPoints[num3] + (__instance.BaseCornerPoints[num] - __instance.BaseCornerPoints[num3]) / 4f + (__instance.BaseCornerPoints[num2] - __instance.BaseCornerPoints[num3]) / 4f;
                collisionTestPoints[5].y = newHeights[num3] + (newHeights[num] - newHeights[num3]) / 4f + (newHeights[num2] - newHeights[num3]) / 4f;

                FieldInfo collisionListField = typeof(LandPatch).GetField("collisionList", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (collisionListField == null)
                    return true;

                List<GameObject> collisionList = (List<GameObject>)collisionListField.GetValue(__instance);

                Collisions.Instance.getColliding(collisionList, collisionTestPoints, BoundingVolume.Layers.Buildvolume);
                foreach (GameObject collision in collisionList)
                {
                    BuildableObject component = collision.GetComponent<BuildableObject>();
                    if (component != null && (component.collidesWith & BoundingVolume.Layers.Terrain) > (BoundingVolume.Layers)0)
                    {
                        flag = true;
                        break;
                    }
                }
                if (flag)
                {
                    __result = 0;
                    return false;
                }
            }
            int num5 = 0;
            if (__instance.h[0] != newHeights[0])
            {
                num5++;
            }
            if (__instance.h[1] != newHeights[1])
            {
                num5++;
            }
            if (__instance.h[2] != newHeights[2])
            {
                num5++;
            }
            if (__instance.h[3] != newHeights[3])
            {
                num5++;
            }
            __instance.h[0] = newHeights[0];
            __instance.h[1] = newHeights[1];
            __instance.h[2] = newHeights[2];
            __instance.h[3] = newHeights[3];
            __instance.HeightDirty = true;

            LandPatchNeighbour[] adjacent = __instance.getAdjacent();
            for (int i = 0; i < adjacent.Length; i++)
            {
                adjacent[i].landPatch.HeightDirty = true;
            }

            __result = num5;
            return false;
        }
    }
}