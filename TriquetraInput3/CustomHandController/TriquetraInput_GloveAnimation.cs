using System;
using HarmonyLib;
using UnityEngine;

namespace Triquetra.Input.CustomHandController
{
    [HarmonyPatch]
    public class TriquetraInput_GloveAnimation : GloveAnimation
    {
        public override void Start()
        {
        }

        public override void OnEnable()
        {
        }

        public override void OnDestroy()
        {
        }

        public override void LateUpdate()
        {
        }

        public override void Update()
        {
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.PressButton))]
        [HarmonyPrefix]
        public static bool P_PressButton(GloveAnimation __instance, Transform buttonTransform, bool pressAndHold,
            bool planarZ)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.UnPressButton))]
        [HarmonyPrefix]
        public static bool P_UnPressButton(GloveAnimation __instance)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.SetThrottle))]
        [HarmonyPrefix]
        public static bool P_SetThrottle(GloveAnimation __instance, VRThrottle t)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.SetJoystick))]
        [HarmonyPrefix]
        public static bool P_SetJoystick(GloveAnimation __instance, VRJoystick joy)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.ClearBoundsPose))]
        [HarmonyPrefix]
        public static bool P_ClearBoundsPose(GloveAnimation __instance, PoseBounds poseBounds)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.ClearInteractPose))]
        [HarmonyPrefix]
        public static bool P_ClearInteractPose(GloveAnimation __instance)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.PlayTwistPushButton))]
        [HarmonyPrefix]
        public static bool P_PlayTwistPushButton(GloveAnimation __instance)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.SetBoundsPose))]
        [HarmonyPrefix]
        public static bool P_SetBoundsPose(GloveAnimation __instance, PoseBounds poseBounds)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.SetKnobTransform))]
        [HarmonyPrefix]
        public static bool P_SetKnobTransform(GloveAnimation __instance, Transform knobTransform, Transform lockTransform,
            bool small)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.SetLeverTransform))]
        [HarmonyPrefix]
        public static bool P_SetLeverTransform(GloveAnimation __instance, Transform tf)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.SetOriginTransform))]
        [HarmonyPrefix]
        public static bool P_SetOriginTransform(GloveAnimation __instance, Transform tf)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.SetLockTransform))]
        [HarmonyPrefix]
        public static bool P_SetLockTransform(GloveAnimation __instance, Transform tf)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.SetPoseHover))]
        [HarmonyPrefix]
        public static bool P_SetPoseHover(GloveAnimation __instance, Poses pose)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.SetRemoteGesture))]
        [HarmonyPrefix]
        public static bool P_SetRemoteGesture(GloveAnimation __instance, int gesture)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.SetPoseInteractable))]
        [HarmonyPrefix]
        public static bool P_SetPoseInteractable(GloveAnimation __instance, Poses pose)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.SetRemoteSkeletonFingers))]
        [HarmonyPrefix]
        public static bool P_SetRemoteSkeletonFingers(GloveAnimation __instance, float thumb, float index, float middle,
            float ring, float pinky)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(GloveAnimation), nameof(GloveAnimation.SetFreeLockTransform))]
        [HarmonyPrefix]
        public static bool P_SetFreeLockTransform(GloveAnimation __instance, Transform tf, Transform lockReference)
        {
            if (__instance is not TriquetraInput_GloveAnimation)
                return true;
            return false;
        }

    }
}