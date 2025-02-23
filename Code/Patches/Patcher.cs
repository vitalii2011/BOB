﻿using System;
using System.Reflection;
using HarmonyLib;
using CitiesHarmony.API;


namespace BOB
{
    /// <summary>
    /// Class to manage the mod's Harmony patches.
    /// </summary>
    public static class Patcher
    {
        // Unique harmony identifier.
        private const string harmonyID = "com.github.algernon-A.csl.bob";

        // Flag.
        internal static bool Patched => patched;
        private static bool patched = false;
        private static bool buildingOverlaysPatched = false, netOverlaysPatched = false, treeOverlaysPatched = false;


        /// <summary>
        /// Apply all Harmony patches.
        /// </summary>
        public static void PatchAll()
        {
            // Don't do anything if already patched.
            if (!patched)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage("deploying Harmony patches");

                    // Apply all annotated patches and update flag.
                    Harmony harmonyInstance = new Harmony(harmonyID);
                    harmonyInstance.PatchAll();
                    patched = true;
                }
                else
                {
                    Logging.Error("Harmony not ready");
                }
            }
        }


        /// <summary>
        /// Remove all Harmony patches.
        /// </summary>
        public static void UnpatchAll()
        {
            // Only unapply if patches appplied.
            if (patched)
            {
                Logging.KeyMessage("reverting Harmony patches");

                // Unapply patches, but only with our HarmonyID.
                Harmony harmonyInstance = new Harmony(harmonyID);
                harmonyInstance.UnpatchAll(harmonyID);
                patched = false;
            }
        }


        /// <summary>
        /// Applies or unapplies overlayer patches for buildings.
        /// </summary>
        /// <param name="active">True to enable patches; false to disable</param>
        internal static void PatchBuildingOverlays(bool active)
        {
            Type[] buildingRenderPropsTypes = { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(Building).MakeByRefType(), typeof(int), typeof(RenderManager.Instance).MakeByRefType(), typeof(bool), typeof(bool), typeof(bool) };

            // Don't do anything if we're already at the current state.
            if (buildingOverlaysPatched != active)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage(active ? "deploying" : "reverting", " building overlay Harmony patches");

                    // Manually patch building overlay renderer.
                    Harmony harmonyInstance = new Harmony(harmonyID);
                    MethodInfo overlayTargetMethod = typeof(BuildingManager).GetMethod("EndOverlay");
                    MethodInfo overlayPatchMethod = typeof(RenderOverlays).GetMethod(nameof(RenderOverlays.RenderOverlay));
                    MethodInfo renderTargetMethod = typeof(BuildingAI).GetMethod("RenderProps", BindingFlags.NonPublic | BindingFlags.Instance, null, buildingRenderPropsTypes, null);
                    MethodInfo renderPatchMethod = typeof(OverlayTranspilers).GetMethod(nameof(OverlayTranspilers.BuildingTranspiler));

                    // Safety check.
                    if (overlayTargetMethod == null || overlayPatchMethod == null || renderTargetMethod == null || renderPatchMethod == null)
                    {
                        Logging.Error("couldn't find required render overlay method");
                        return;
                    }

                    // Apply or remove patches according to flag.
                    if (active)
                    {
                        harmonyInstance.Patch(overlayTargetMethod, postfix: new HarmonyMethod(overlayPatchMethod));
                        harmonyInstance.Patch(renderTargetMethod, transpiler: new HarmonyMethod(renderPatchMethod));
                    }
                    else
                    {
                        harmonyInstance.Unpatch(overlayTargetMethod, overlayPatchMethod);
                        harmonyInstance.Unpatch(renderTargetMethod, renderPatchMethod);
                    }

                    // Update status flag.
                    buildingOverlaysPatched = active;
                }
                else
                {
                    Logging.Error("Harmony not ready");
                }
            }
        }


        /// <summary>
        /// Applies or unapplies overlayer patches for networks.
        /// </summary>
        /// <param name="active">True to enable patches; false to disable</param>
        internal static void PatchNetworkOverlays(bool active)
        {
            // Don't do anything if we're already at the current state.
            if (netOverlaysPatched != active)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage(active ? "deploying" : "reverting", " network overlay Harmony patches");

                    // Manually patch building overlay renderer.
                    Harmony harmonyInstance = new Harmony(harmonyID);
                    MethodInfo overlayTargetMethod = typeof(NetManager).GetMethod("EndOverlay");
                    MethodInfo overlayPatchMethod = typeof(RenderOverlays).GetMethod(nameof(RenderOverlays.RenderOverlay));
                    MethodInfo renderTargetMethod = typeof(NetLane).GetMethod(nameof(NetLane.RenderInstance));
                    MethodInfo renderPatchMethod = typeof(OverlayTranspilers).GetMethod(nameof(OverlayTranspilers.NetTranspiler));

                    // Safety check.
                    if (overlayTargetMethod == null || overlayPatchMethod == null || renderTargetMethod == null || renderPatchMethod == null)
                    {
                        Logging.Error("couldn't find required render overlay method");
                        return;
                    }

                    // Apply or remove patches according to flag.
                    if (active)
                    {
                        harmonyInstance.Patch(overlayTargetMethod, postfix: new HarmonyMethod(overlayPatchMethod));
                        harmonyInstance.Patch(renderTargetMethod, transpiler: new HarmonyMethod(renderPatchMethod));
                    }
                    else
                    {
                        harmonyInstance.Unpatch(overlayTargetMethod, overlayPatchMethod);
                        harmonyInstance.Unpatch(renderTargetMethod, renderPatchMethod);
                    }

                    // Update status flag.
                    netOverlaysPatched = active;
                }
                else
                {
                    Logging.Error("Harmony not ready");
                }
            }
        }


        /// <summary>
        /// Applies or unapplies overlayer patches for map trees and props.
        /// </summary>
        /// <param name="active">True to enable patches; false to disable</param>
        internal static void PatchMapOverlays(bool active)
        {
            Type[] treeRenderTypes = { typeof(RenderManager.CameraInfo), typeof(uint), typeof(int) };

            // Don't do anything if we're already at the current state.
            if (treeOverlaysPatched != active)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage(active ? "deploying" : "reverting", " tree overlay Harmony patches");

                    // Manually patch building overlay renderer.
                    Harmony harmonyInstance = new Harmony(harmonyID);
                    MethodInfo treeOverlayTargetMethod = typeof(TreeManager).GetMethod("EndOverlay");
                    MethodInfo propOverlayTargetMethod = typeof(PropManager).GetMethod("EndOverlay");
                    MethodInfo overlayPatchMethod = typeof(RenderOverlays).GetMethod(nameof(RenderOverlays.RenderOverlay));
                    MethodInfo treeRenderTargetMethod = typeof(TreeInstance).GetMethod(nameof(TreeInstance.RenderInstance), BindingFlags.Public | BindingFlags.Instance);
                    MethodInfo treeRenderPatchMethod = typeof(OverlayTranspilers).GetMethod(nameof(OverlayTranspilers.TreeTranspiler));
                    MethodInfo propRenderTargetMethod = typeof(PropInstance).GetMethod(nameof(PropInstance.RenderInstance), new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) });
                    MethodInfo propRenderPatchMethod = typeof(OverlayTranspilers).GetMethod(nameof(OverlayTranspilers.PropTranspiler));

                    // Safety check.
                    if (treeOverlayTargetMethod == null || propOverlayTargetMethod == null || overlayPatchMethod == null || treeRenderTargetMethod == null || treeRenderPatchMethod == null || propRenderTargetMethod == null || propRenderPatchMethod == null)
                    {
                        Logging.Error("couldn't find required render overlay method");
                        return;
                    }

                    // Apply or remove patches according to flag.
                    if (active)
                    {
                        harmonyInstance.Patch(treeOverlayTargetMethod, postfix: new HarmonyMethod(overlayPatchMethod));
                        harmonyInstance.Patch(treeRenderTargetMethod, transpiler: new HarmonyMethod(treeRenderPatchMethod));
                        harmonyInstance.Patch(propOverlayTargetMethod, postfix: new HarmonyMethod(overlayPatchMethod));
                        harmonyInstance.Patch(propRenderTargetMethod, transpiler: new HarmonyMethod(propRenderPatchMethod));
                    }
                    else
                    {
                        harmonyInstance.Unpatch(treeOverlayTargetMethod, overlayPatchMethod);
                        harmonyInstance.Unpatch(treeRenderTargetMethod, treeRenderPatchMethod);
                        harmonyInstance.Unpatch(propOverlayTargetMethod, overlayPatchMethod);
                        harmonyInstance.Unpatch(propRenderTargetMethod, propRenderPatchMethod);
                    }

                    // Update status flag.
                    treeOverlaysPatched = active;
                }
                else
                {
                    Logging.Error("Harmony not ready");
                }
            }
        }
    }
}