using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VTOLAPI;
using VTOLVR.DLC.Rotorcraft;
using VTOLVR.Multiplayer;

namespace Triquetra.Input
{
    public enum ControllerAction
    {
        None,
        Throttle,
        HeloPower,
        Pitch,
        Yaw,
        Roll,
        JoystickTrigger,
        JoystickThumbStick,
        ThrottleThumbStick,
        Countermeasures,
        Brakes, // ThrottleTrigger
        FlapsIncrease,
        FlapsDecrease,
        FlapsCycle,
        SwitchWeapon,
        // LandingGear,
        Visor,
        NightVisionGoggles,
        PTT,
        VRInteract,
        ToggleMouseFly,
        FlatscreenCenterInteract,
        FlatscreenFoV,
        FlatscreenMoveCamera,
        NewVRInteract,
        HeloModThrottle
    }

    
    public static class ControllerActions
    {
        public static class Radio
        {
            internal static CockpitTeamRadioManager radioManager;

            static bool wasTalking = false;
            public static void PTT(Binding binding, int joystickValue)
            {
                if (radioManager == null)
                    radioManager = FindRadioManager();

                if (binding.GetButtonPressed(joystickValue))
                {
                    wasTalking = true;
                    radioManager.ptt?.StartVoice();
                }
                else if (wasTalking)
                {
                    radioManager.ptt?.StopVoice();
                    wasTalking = false;
                }
            }

            internal static CockpitTeamRadioManager FindRadioManager()
            {
                return GameObject.FindObjectOfType<CockpitTeamRadioManager>();
            }
        }
        public static class Helmet
        {
            private static HelmetController helmetController;

            public static void ToggleVisor(Binding binding, int joystickValue, int delta = 1)
            {
                if (helmetController == null)
                    helmetController = FindHelmetController();

                if (binding.GetButtonPressed(joystickValue))
                    helmetController.ToggleVisor();
            }

            public static void ToggleNVG(Binding binding, int joystickValue, int delta = 1)
            {
                if (helmetController == null)
                    helmetController = FindHelmetController();

                if (binding.GetButtonPressed(joystickValue))
                    helmetController.ToggleNVG();
            }

            private static HelmetController FindHelmetController()
            {
                return GameObject.FindObjectsOfType<HelmetController>(false).First();
            }
        }

        public static class Flaps
        {
            internal static VRLever flaps;
            public static void IncreaseFlaps(Binding binding, int joystickValue, int delta = 1)
            {
                if (flaps == null)
                    flaps = FindFlaps();

                if (binding.GetButtonPressed(joystickValue))
                    Interactions.MoveLever(flaps, delta, true);
            }

            public static void DecreaseFlaps(Binding binding, int joystickValue)
            {
                IncreaseFlaps(binding, joystickValue, -1);
            }

            public static void CycleFlaps(Binding binding, int joystickValue)
            {
                if (flaps == null)
                    flaps = FindFlaps();

                if (binding.GetButtonPressed(joystickValue))
                    Interactions.MoveLever(flaps, 1, false);
            }

            internal static VRLever FindFlaps()
            {
                return GameObject.FindObjectsOfType<VRLever>(false).Where(l => l?.GetComponent<VRInteractable>()?.interactableName == "Flaps").FirstOrDefault();
            }
        }

        // TODO: countermeasures, alt. controls (from holding trigger on throttle)
        public static class Helicopter
        {
            internal static VRThrottle power;
            
            internal static bool collectiveModded;
            
            public static void SetPower(Binding binding, int joystickValue)
            {
                if (power == null)
                    power = FindThrottle();

                Interactions.SetThrottle(power, binding.GetAxisAsFloat(joystickValue));
            }

            public static void MoveThrottle(Binding binding, int joystickValue, float delta)
            {
                if (power == null)
                    return;

                Interactions.MoveThrottle(power, (float)Math.Round(binding.GetAxisAsFloat(joystickValue) - 0.5f, 1) * delta);
            }

            internal static VRThrottle FindThrottle()
            {
                return GameObject.FindObjectsOfType<VRThrottle>(false).Where(t => t.interactable?.interactableName == "Power").FirstOrDefault();
            }
        }

        public static class Throttle
        {
            internal static VRThrottle throttle;
            internal static AH94CollectiveFunctions collectiveFunctions;
            
            public static void SetThrottle(Binding binding, int joystickValue)
            {
                if (throttle == null)
                    return;

                Interactions.SetThrottle(throttle, binding.GetAxisAsFloat(joystickValue));
            }

            public static void MoveThrottle(Binding binding, int joystickValue, float delta)
            {
                if (throttle == null)
                    return;

                Interactions.MoveThrottle(throttle, (float)Math.Round(binding.GetAxisAsFloat(joystickValue) - 0.5f, 1) * delta);
            }

            private static bool triggerPressed = false;
            public static void TriggerBrakes(Binding binding, int joystickValue)
            {
                if (collectiveFunctions)
                {
                    if (binding?.CombatCollective == true)
                        return; // Combat collective handles trigger internally
                    
                    HandleTriggerForCollective(binding, joystickValue);
                    return;
                }
                
                if (throttle == null)
                    return;

                HandleTriggerForThrottle(binding, joystickValue);
            }

            private static void HandleTriggerForCollective(Binding binding, int joystickValue)
            {
                if (!triggerPressed && binding.GetButtonPressed(joystickValue))
                {
                    triggerPressed = true;
                    collectiveFunctions.flightCollective.OnTriggerDown?.Invoke();
                }
                else if (triggerPressed && !binding.GetButtonPressed(joystickValue))
                {
                    triggerPressed = false;
                    collectiveFunctions.flightCollective.OnTriggerUp?.Invoke();
                }
            }

            private static void HandleTriggerForThrottle(Binding binding, int joystickValue)
            {
                if (!triggerPressed && binding.GetButtonPressed(joystickValue))
                {
                    triggerPressed = true;
                    throttle.OnTriggerDown?.Invoke();
                }
                else if (triggerPressed && !binding.GetButtonPressed(joystickValue))
                {
                    triggerPressed = false;
                    throttle.OnTriggerUp?.Invoke();
                }

                throttle.OnTriggerAxis?.Invoke(binding.GetAxisAsFloat(joystickValue));
            }

            #region Thumbstick
            private static bool thumbstickButtonPressed = false;
            public static void ThumbstickButton(Binding binding, int joystickValue)
            {
                if (collectiveFunctions)
                {
                    if (binding?.CombatCollective == true)
                        HandleCombatThumbstickButton(binding, joystickValue);
                    else
                        collectiveFunctions.FlightMenuButtonDown();
                    return;
                }

                if (throttle == null)
                    return;

                HandleStandardThumbstickButton(binding, joystickValue);
            }

            private static void HandleCombatThumbstickButton(Binding binding, int joystickValue)
            {
                if (Helicopter.collectiveModded)
                {
                    collectiveFunctions.CombatOnTriggerDown();
                    collectiveFunctions.combatCollective.triggerIsDown = true;
                }

                if (binding.GetButtonPressed(joystickValue))
                {
                    collectiveFunctions.CombatOnStickPressed();
                }

                if (!thumbstickButtonPressed && binding.GetButtonPressed(joystickValue))
                {
                    thumbstickButtonPressed = true;
                    collectiveFunctions.CombatOnStickPressDown();
                }
                else if (thumbstickButtonPressed && !binding.GetButtonPressed(joystickValue))
                {
                    thumbstickButtonPressed = false;
                    collectiveFunctions.CombatOnStickPressUp();
                }

                if (Helicopter.collectiveModded)
                {
                    collectiveFunctions.CombatOnTriggerUp();
                    collectiveFunctions.combatCollective.triggerIsDown = false;
                }
            }

            private static void HandleStandardThumbstickButton(Binding binding, int joystickValue)
            {
                if (binding.GetButtonPressed(joystickValue))
                {
                    throttle.OnStickPressed?.Invoke();
                }

                if (!thumbstickButtonPressed && binding.GetButtonPressed(joystickValue))
                {
                    thumbstickButtonPressed = true;
                    throttle.OnStickPressDown?.Invoke();
                }
                else if (thumbstickButtonPressed && !binding.GetButtonPressed(joystickValue))
                {
                    thumbstickButtonPressed = false;
                    throttle.OnStickPressUp?.Invoke();
                }
            }

            public static bool ThumbstickUp = false;
            public static bool ThumbstickRight = false;
            public static bool ThumbstickDown = false;
            public static bool ThumbstickLeft = false;
            private static bool thumbstickWasZero = false;
            private static bool thumbstickWasMoving = false;
            public static void UpdateThumbstick(Binding binding = null)
            {
                if (throttle == null)
                    return;

                // Convert the boolean button states into 1.0f or 0.0f numerical values
                float x = (ThumbstickRight ? 1.0f : 0.0f) - (ThumbstickLeft ? 1.0f : 0.0f);
                float y = (ThumbstickUp ? 1.0f : 0.0f) - (ThumbstickDown ? 1.0f : 0.0f);

                Vector3 vector = new Vector3(x, y, 0);

                if (collectiveFunctions && binding?.CombatCollective == true)
                    UpdateCombatThumbstick(vector);
                else if (collectiveFunctions && binding != null)
                    collectiveFunctions.OnFlightCollectiveThumbstick(vector);
                else
                    UpdateStandardThumbstick(vector);
                }

            private static void UpdateCombatThumbstick(Vector3 vector)
            {
                if (Helicopter.collectiveModded)
                {
                    collectiveFunctions.CombatOnTriggerDown();
                    collectiveFunctions.combatCollective.triggerIsDown = true;
                }

                if (vector != Vector3.zero)
                {
                    thumbstickWasZero = false;
                    collectiveFunctions.CombatOnSetThumbstick(vector);
                    thumbstickWasMoving = true;
                }
                else if (!thumbstickWasZero)
                {
                    collectiveFunctions.CombatOnSetThumbstick(vector);
                    thumbstickWasZero = true;
                    thumbstickWasMoving = true;
                }
                else if (thumbstickWasMoving)
                {
                    collectiveFunctions.CombatOnResetThumbstick();
                    thumbstickWasMoving = false;
                }

                if (Helicopter.collectiveModded)
                {
                    collectiveFunctions.CombatOnTriggerUp();
                    collectiveFunctions.combatCollective.triggerIsDown = false;
                }
            }

            private static void UpdateStandardThumbstick(Vector3 vector)
            {
                if (vector != Vector3.zero)
                {
                    thumbstickWasZero = false;
                    throttle.OnSetThumbstick?.Invoke(vector);
                    thumbstickWasMoving = true;
                }
                else if (!thumbstickWasZero)
                {
                    throttle.OnSetThumbstick?.Invoke(vector);
                    thumbstickWasZero = true;
                    thumbstickWasMoving = true;
                }
                else if (thumbstickWasMoving)
                {
                    throttle.OnResetThumbstick?.Invoke();
                    thumbstickWasMoving = false;
                }
            }

            public static void Thumbstick(Binding binding, int joystickValue)
            {
                Action action = binding.ThumbstickDirection switch
                {
                    ThumbstickDirection.Up => () => ThumbstickUp = binding.GetButtonPressed(joystickValue),
                    ThumbstickDirection.Down => () => ThumbstickDown = binding.GetButtonPressed(joystickValue),
                    ThumbstickDirection.Right => () => ThumbstickRight = binding.GetButtonPressed(joystickValue),
                    ThumbstickDirection.Left => () => ThumbstickLeft = binding.GetButtonPressed(joystickValue),
                    ThumbstickDirection.Press => () => ThumbstickButton(binding, joystickValue),
                    _ => () => {/* Default case */ }
                };
                action();
                UpdateThumbstick(binding);
            }
            #endregion

            private static bool cmPressed = false;
            public static void Countermeasures(Binding binding, int joystickValue)
            {
                if (collectiveFunctions)
                {
                    if (binding?.CombatCollective == true)
                        HandleCombatCountermeasures(binding, joystickValue);
                    else
                        collectiveFunctions.FlightMenuButtonDown();
                    return;
                }

                if (throttle == null)
                    return;

                HandleStandardCountermeasures(binding, joystickValue);
            }

            private static void HandleCombatCountermeasures(Binding binding, int joystickValue)
            {
                if (Helicopter.collectiveModded)
                {
                    collectiveFunctions.CombatOnTriggerDown();
                    collectiveFunctions.combatCollective.triggerIsDown = true;
                }

                if (!cmPressed && binding.GetButtonPressed(joystickValue))
                {
                    cmPressed = true;
                    collectiveFunctions.CombatMenuButtonDown();
                }
                else if (cmPressed && !binding.GetButtonPressed(joystickValue))
                {
                    cmPressed = false;
                    collectiveFunctions.CombatMenuButtonUp();
                }

                if (Helicopter.collectiveModded)
                {
                    collectiveFunctions.CombatOnTriggerUp();
                    collectiveFunctions.combatCollective.triggerIsDown = false;
                }
            }

            private static void HandleStandardCountermeasures(Binding binding, int joystickValue)
            {
                if (!cmPressed && binding.GetButtonPressed(joystickValue))
                {
                    cmPressed = true;
                    throttle.OnMenuButtonDown?.Invoke();
                }
                else if (cmPressed && !binding.GetButtonPressed(joystickValue))
                {
                    cmPressed = false;
                    throttle.OnMenuButtonUp?.Invoke();
                }
            }

            internal static VRThrottle FindThrottle()
            {
                var vehicleObject = VTAPI.GetPlayersVehicleGameObject();
                if (vehicleObject)
                {
                    var throttles = vehicleObject.GetComponentsInChildren<VRThrottle>(true);
                    var nonPower = throttles.FirstOrDefault(t => t.interactable && !t.interactable.interactableName.Contains("Power"));
                    
                    if (nonPower != null)
                        return nonPower;
                    return throttles[0];
                }
                return null;
            }

            internal static AH94CollectiveFunctions FindCollective()
            {
                var vehicleObject = VTAPI.GetPlayersVehicleGameObject();
                if (vehicleObject)
                {
                    var throttles = vehicleObject.GetComponentsInChildren<VRThrottle>(true);
                    var nonPower = throttles.FirstOrDefault(t => t.interactable && t.interactable.interactableName.ToLower().Contains("flight"));
                    if (nonPower != null)
                    {
                        collectiveFunctions = nonPower.GetComponentInParent<AH94CollectiveFunctions>();
                        if (nonPower != null)
                            return collectiveFunctions;
                    }
                }
                return null;
            }
        }

        public static class Joystick
        {
            internal static VRJoystick joystick;
            private static Vector3 stickVector = new Vector3(0, 0, 0);
            public static void SetPitch(Binding binding, int joystickValue)
            {
                stickVector.x = binding.GetAxisAsFloat(joystickValue) - 0.5f;
            }
            public static void SetYaw(Binding binding, int joystickValue)
            {
                stickVector.y = binding.GetAxisAsFloat(joystickValue) - 0.5f;
            }
            public static void SetRoll(Binding binding, int joystickValue)
            {
                stickVector.z = binding.GetAxisAsFloat(joystickValue) - 0.5f;
            }

            public static Vector3 GetStick()
            {
                return stickVector * 2;
            }

            public static void UpdateStick()
            {
                if (joystick == null)
                    return;

                joystick.OnSetStick.Invoke(stickVector * 2); // stickVector is usually -0.5 to 0.5
            }

            private static bool triggerPressed = false;
            public static void Trigger(Binding binding, int joystickValue)
            {
                if (joystick == null)
                    return;

                if (!triggerPressed && binding.GetButtonPressed(joystickValue))
                {
                    triggerPressed = true;
                    joystick.OnTriggerDown?.Invoke();
                }
                else if (triggerPressed && !binding.GetButtonPressed(joystickValue))
                {
                    triggerPressed = false;
                    joystick.OnTriggerUp?.Invoke();
                }

                joystick.OnTriggerAxis?.Invoke(binding.GetAxisAsFloat(joystickValue));
            }

            #region Thumbstick
            private static bool thumbstickButtonPressed = false;
            public static void ThumbstickButton(Binding binding, int joystickValue)
            {
                if (joystick == null)
                    return;

                if (binding.GetButtonPressed(joystickValue))
                    joystick.OnThumbstickButton?.Invoke();

                if (!thumbstickButtonPressed && binding.GetButtonPressed(joystickValue))
                {
                    thumbstickButtonPressed = true;
                    joystick.OnThumbstickButtonDown?.Invoke();
                }
                else if (thumbstickButtonPressed && !binding.GetButtonPressed(joystickValue))
                {
                    thumbstickButtonPressed = false;
                    joystick.OnThumbstickButtonUp?.Invoke();
                }
            }

            public static bool ThumbstickUp = false;
            public static bool ThumbstickRight = false;
            public static bool ThumbstickDown = false;
            public static bool ThumbstickLeft = false;
            private static bool thumbstickWasZero = false;
            private static bool thumbstickWasMoving = false;
            public static void UpdateThumbstick()
            {
                if (joystick == null)
                    return;

                // Convert the boolean button states into 1.0f or 0.0f numerical values
                float x = (ThumbstickRight ? 1.0f : 0.0f) - (ThumbstickLeft ? 1.0f : 0.0f);
                float y = (ThumbstickUp ? 1.0f : 0.0f) - (ThumbstickDown ? 1.0f : 0.0f);

                Vector3 vector = new Vector3(x, y, 0);

                if (vector != Vector3.zero)
                {
                    thumbstickWasZero = false;
                    joystick.OnSetThumbstick?.Invoke(vector);
                    thumbstickWasMoving = true;
                }
                else if (!thumbstickWasZero)
                {
                    joystick.OnSetThumbstick?.Invoke(vector);
                    thumbstickWasZero = true;
                    thumbstickWasMoving = true;
                }
                else if (thumbstickWasMoving)
                {
                    joystick.OnResetThumbstick?.Invoke();
                    thumbstickWasMoving = false;
                }
            }

            public static void Thumbstick(Binding binding, int joystickValue)
            {
                Action action = binding.ThumbstickDirection switch
                {
                    ThumbstickDirection.Up => () => ThumbstickUp = binding.GetButtonPressed(joystickValue),
                    ThumbstickDirection.Down => () => ThumbstickDown = binding.GetButtonPressed(joystickValue),
                    ThumbstickDirection.Right => () => ThumbstickRight = binding.GetButtonPressed(joystickValue),
                    ThumbstickDirection.Left => () => ThumbstickLeft = binding.GetButtonPressed(joystickValue),
                    ThumbstickDirection.Press => () => ThumbstickButton(binding, joystickValue),
                    _ => () => {/* Default case */ }
                };
                action();
                UpdateThumbstick();
            }
            #endregion

            internal static VRJoystick FindJoystick()
            {
                var vehicleObject = VTAPI.GetPlayersVehicleGameObject();
                if (vehicleObject)
                {
                    var joysticks = vehicleObject.GetComponentsInChildren<VRJoystick>(true);
                    var stick = joysticks.FirstOrDefault(vrJoystick => vrJoystick.name == "joyInteractable_sideFront") ??
                                   joysticks.FirstOrDefault();
                    return stick;
                }
                return null;
            }

            private static bool menuButtonPressed = false;
            public static void SwitchWeapon(Binding binding, int joystickValue)
            {
                if (joystick == null)
                    return;

                if (!menuButtonPressed && binding.GetButtonPressed(joystickValue))
                {
                    menuButtonPressed = true;
                    joystick.OnMenuButtonDown?.Invoke();
                }
                else if (menuButtonPressed && !binding.GetButtonPressed(joystickValue))
                {
                    menuButtonPressed = false;
                    joystick.OnMenuButtonUp?.Invoke();
                }
            }
        }

        public static class FS2Camera
        {
            #region Thumbstick
            public static float ThumbstickUp = 0;
            public static float ThumbstickRight = 0;
            public static float ThumbstickDown = 0;
            public static float ThumbstickLeft = 0;
            private static bool thumbstickWasZero = false;
            private static bool thumbstickWasMoving = false;
            public static void UpdateThumbstick()
            {
                var modVariables = Binding.FS2ModVariables;
                if (modVariables == null)
                    return;

                Vector2 vector = new Vector2();
                vector.x += ThumbstickRight - ThumbstickLeft;
                vector.y += ThumbstickUp - ThumbstickDown;

                if (vector != Vector2.zero)
                {
                    thumbstickWasZero = false;
                    modVariables.TrySetValue("RotateCamera", vector);
                    thumbstickWasMoving = true;
                }
                else if (!thumbstickWasZero)
                {
                    modVariables.TrySetValue("RotateCamera", vector);
                    thumbstickWasZero = true;
                    thumbstickWasMoving = true;
                }
                else if (thumbstickWasMoving)
                {
                    modVariables.TrySetValue("RotateCamera", Vector2.zero);
                    thumbstickWasMoving = false;
                }
            }

            public static void Thumbstick(Binding binding, int joystickValue)
            {
                Action action = binding.ThumbstickDirection switch
                {
                    ThumbstickDirection.Up => () => ThumbstickUp = binding.GetAxisAsFloat(joystickValue),
                    ThumbstickDirection.Down => () => ThumbstickDown = binding.GetAxisAsFloat(joystickValue),
                    ThumbstickDirection.Right => () => ThumbstickRight = binding.GetAxisAsFloat(joystickValue),
                    ThumbstickDirection.Left => () => ThumbstickLeft = binding.GetAxisAsFloat(joystickValue),
                    _ => () => {/* Default case */ }
                };

                action();
            }
            #endregion
        }

        public static void Print(Binding binding, int joystickValue)
        {
            Plugin.Write($"Triquetra.Input: Axis is {binding.Offset}. Value is {joystickValue}");
        }

        internal static void TryGetSticks()
        {
            if (Plugin.IsFlyingScene())
            {
                if (Joystick.joystick == null)
                    Joystick.joystick = Joystick.FindJoystick();
                if (Throttle.throttle == null)
                    Throttle.throttle = Throttle.FindThrottle();
                if (Throttle.collectiveFunctions == null)
                    Throttle.collectiveFunctions = Throttle.FindCollective();
            }
        }
    }
}
