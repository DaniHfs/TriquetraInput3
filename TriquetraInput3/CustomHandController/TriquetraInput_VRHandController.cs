using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Triquetra.Input.CustomHandController
{
    [HarmonyPatch]
    public class TriquetraInput_VRHandController : VRHandController
    {
        private Vector2 _stickAxis;
        private float _triggerAxis;
        private bool _stickPressed;
        private bool _primaryPressed;
        private bool _secondaryPressed;
        private bool _triggerPressed;
        
        public bool primaryButtonDown;
        public bool primaryButtonUp;
        public bool wasPrimaryButtonPressed;
        
        public bool secondaryButtonDown;
        public bool secondaryButtonUp;
        public bool wasSecondaryButtonPressed;
        
        public bool triggerButtonDown;
        public bool triggerButtonUp;
        public bool wasTriggerButtonPressed;

        public bool markedForDestruction;

        private bool _destroyNextFrame;
        
        public override void Awake()
        {
        }

        public override void Start()
        {
        }
        
        

        public override void Update()
        {
            SyncInternalState();
            UpdateButtonStates();
            HandleDestruction();
        }

        private void SyncInternalState()
        {
            stickAxis = _stickAxis;
            stickPressed = _stickPressed;
            triggerAxis = _triggerAxis;
            thumbButtonPressed = _primaryPressed;
            secondaryThumbButtonPressed = _secondaryPressed;
            triggerClicked = _triggerPressed;
        }

        private void UpdateButtonStates()
        {
            UpdateStickButton();
            UpdatePrimaryButton();
            UpdateSecondaryButton();
            UpdateTriggerButton();
        }

        private void HandleDestruction()
        {
            if (_destroyNextFrame)
            {
                activeInteractable?.UnClick(this);
                Destroy(gameObject);
            }
            else
                _destroyNextFrame = markedForDestruction;
        }

        public void UpdateStickButton() => UpdateButtonState(stickPressed, ref wasStickPressed, 
            (value) => { stickPressDown = value; }, (value) => { stickPressUp = value; });

        public void UpdatePrimaryButton() => UpdateButtonState(thumbButtonPressed, ref wasPrimaryButtonPressed,
            (value) => { primaryButtonDown = value; }, (value) => { primaryButtonUp = value; });

        public void UpdateSecondaryButton() => UpdateButtonState(secondaryThumbButtonPressed, ref wasSecondaryButtonPressed,
            (value) => { secondaryButtonDown = value; }, (value) => { secondaryButtonUp = value; });

        public void UpdateTriggerButton() => UpdateButtonState(triggerClicked, ref wasTriggerButtonPressed,
            (value) => { triggerButtonDown = value; }, (value) => { triggerButtonUp = value; });

        private static void UpdateButtonState(bool isPressed, ref bool wasPressed, Action<bool> setDown, Action<bool> setUp)
        {
            if (isPressed)
            {
                setDown(!wasPressed);
                wasPressed = true;
            }
            else
            {
                setUp(wasPressed);
                wasPressed = false;
            }
        }

        public override void OnEnable()
        {
            controllers ??= new List<VRHandController>();
            
            if (interactionTransform)
                return;

            interactionTransform = new GameObject("interactionTf").transform;
            interactionTransform.parent = transform;
            interactionTransform.localPosition = Vector3.zero;
        }

        public override void OnDisable()
        {
        }

        public override void OnDestroy()
        {
        }

        [HarmonyPatch(typeof(VRHandController), nameof(VRHandController.HapticPulse))]
        [HarmonyPrefix]
        public static bool P_HapticPulse(VRHandController __instance)
        {
            if (__instance is not TriquetraInput_VRHandController _instance)
                return true;
            
            return false;
        }

        [HarmonyPatch(typeof(VRHandController), nameof(VRHandController.Vibrate))]
        [HarmonyPrefix]
        public static bool P_Vibrate(VRHandController __instance)
        {
            if (__instance is not TriquetraInput_VRHandController _instance)
                return true;
            
            return false;
        }

        [HarmonyPatch(typeof(VRHandController), nameof(VRHandController.GetStickPressDown))]
        [HarmonyPrefix]
        public static bool P_GetStickPressDown(VRHandController __instance, ref bool __result)
        {
            if (__instance is not TriquetraInput_VRHandController _instance)
                return true;
            //_instance.UpdateStickButton();
            __result = _instance.stickPressDown;
            return false;
        }

        [HarmonyPatch(typeof(VRHandController), nameof(VRHandController.GetStickPressUp))]
        [HarmonyPrefix]
        public static bool P_GetStickPressUp(VRHandController __instance, ref bool __result)
        {
            if (__instance is not TriquetraInput_VRHandController _instance)
                return true;
            //_instance.UpdateStickButton();
            __result = _instance.stickPressUp;
            return false;
        }

        [HarmonyPatch(typeof(VRHandController), nameof(VRHandController.GetThumbButtonDown))]
        [HarmonyPrefix]
        public static bool P_GetThumbButtonDown(VRHandController __instance, ref bool __result)
        {
            if (__instance is not TriquetraInput_VRHandController _instance)
                return true;
            //_instance.UpdatePrimaryButton();
            __result = _instance.primaryButtonDown;
            return false;
        }

        [HarmonyPatch(typeof(VRHandController), nameof(VRHandController.GetThumbButtonUp))]
        [HarmonyPrefix]
        public static bool P_GetThumbButtonUp(VRHandController __instance, ref bool __result)
        {
            if (__instance is not TriquetraInput_VRHandController _instance)
                return true;
            //_instance.UpdatePrimaryButton();
            __result = _instance.primaryButtonUp;
            return false;
        }

        [HarmonyPatch(typeof(VRHandController), nameof(VRHandController.GetSecondButtonDown))]
        [HarmonyPrefix]
        public static bool P_GetSecondButtonDown(VRHandController __instance, ref bool __result)
        {
            if (__instance is not TriquetraInput_VRHandController _instance)
                return true;
            //_instance.UpdateSecondaryButton();
            __result = _instance.secondaryButtonDown;
            return false;
        }

        [HarmonyPatch(typeof(VRHandController), nameof(VRHandController.GetSecondButtonUp))]
        [HarmonyPrefix]
        public static bool P_GetSecondButtonUp(VRHandController __instance, ref bool __result)
        {
            if (__instance is not TriquetraInput_VRHandController _instance)
                return true;
            //_instance.UpdateSecondaryButton();
            __result = _instance.secondaryButtonUp;
            return false;
        }

        [HarmonyPatch(typeof(VRHandController), nameof(VRHandController.GetTriggerClickDown))]
        [HarmonyPrefix]
        public static bool P_GetTriggerClickDown(VRHandController __instance, ref bool __result)
        {
            if (__instance is not TriquetraInput_VRHandController _instance)
                return true;
            //_instance.UpdateTriggerButton();
            __result = _instance.triggerButtonDown;
            return false;
        }

        [HarmonyPatch(typeof(VRHandController), nameof(VRHandController.GetTriggerClickUp))]
        [HarmonyPrefix]
        public static bool P_GetTriggerClickUp(VRHandController __instance, ref bool __result)
        {
            if (__instance is not TriquetraInput_VRHandController _instance)
                return true;
            //_instance.UpdateTriggerButton();
            __result = _instance.triggerButtonUp;
            return false;
        }
        
        // STICK AXIS
        public void StickAxis(Vector2 axis)
        {
            _stickAxis = axis;
            
            var eventDelegate = (MulticastDelegate)typeof(VRHandController).GetField(nameof(OnStickAxis), BindingFlags.Instance | BindingFlags.Public)?.GetValue(this);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { this, axis });
                }
            }
        }

        // THUMB BUTTON
        public void ThumbButtonPressed()
        {
            _primaryPressed = true;
            
            var eventDelegate = (MulticastDelegate)typeof(VRHandController).GetField(nameof(OnThumbButtonPressed), BindingFlags.Instance | BindingFlags.Public)?.GetValue(this);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { });
                }
            }
        }

        public void ThumbButtonReleased()
        {
            _primaryPressed = false;
            
            var eventDelegate = (MulticastDelegate)typeof(VRHandController).GetField(nameof(OnSecondaryThumbButtonReleased), BindingFlags.Instance | BindingFlags.Public)?.GetValue(this);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { });
                }
            }
        }

        // SECONDARY THUMB BUTTON
        public void SecondaryThumbButtonPressed()
        {
            _secondaryPressed = true;
            
            var eventDelegate = (MulticastDelegate)typeof(VRHandController).GetField(nameof(SecondaryThumbButtonPressed), BindingFlags.Instance | BindingFlags.Public)?.GetValue(this);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { });
                }
            }

        }

        public void SecondaryThumbButtonReleased()
        {
            _secondaryPressed = false;
            
            var eventDelegate = (MulticastDelegate)typeof(VRHandController).GetField(nameof(SecondaryThumbButtonReleased), BindingFlags.Instance | BindingFlags.Public)?.GetValue(this);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { });
                }
            }
            
        }

        // THUMBSTICK BUTTON
        public void ThumbstickButtonPressed()
        {
            _stickPressed = true;
            
            var eventDelegate = (MulticastDelegate)typeof(VRHandController).GetField(nameof(OnStickPressed), BindingFlags.Instance | BindingFlags.Public)?.GetValue(this);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { });
                }
            }

        }

        public void ThumbstickButtonReleased()
        {
            _stickPressed = false;
            
            var eventDelegate = (MulticastDelegate)typeof(VRHandController).GetField(nameof(OnStickUnpressed), BindingFlags.Instance | BindingFlags.Public)?.GetValue(this);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { });
                }
            }
        }

        // TRIGGER AXIS
        public void TriggerAxis(float axis)
        {
            if (axis < 0.0001f)
                _triggerPressed = false;
            if (axis > 0.9999f)
                _triggerPressed = true;
            
            var eventDelegate = (MulticastDelegate)typeof(VRHandController).GetField(nameof(OnTriggerAxis), BindingFlags.Instance | BindingFlags.Public)?.GetValue(this);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { this, axis });
                }
            }

        }
    }
}