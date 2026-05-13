using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using SharpDX.DirectInput;
using Triquetra.Input.CustomHandController;
using UnityEngine;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Converters;
using VTOLAPI;
using DeviceType = SharpDX.DirectInput.DeviceType;
using Object = UnityEngine.Object;

namespace Triquetra.Input
{
    public enum VRInteractAction
    {
        Interact,
        Move,
        TriggerAxis,
        ThumbstickAxis,
        ThumbstickButton,
        PrimaryButton,
        SecondaryButton
    }
    
    public class Binding
    {
        public const int AxisMin = 0;
        public const int AxisMiddle = 32768;
        public const int AxisMax = 65535;
        public const int ButtonMin = 0;
        public const int ButtonMax = 128;
        public const int Deadzone = 8192;
        public const int POVMin = 0;
        public const int POVMax = 36000;
        public int LastValue { get; set; } = -1; // Added this

        public static List<Binding> Bindings = new List<Binding>();
        public static DirectInput directInput = new DirectInput();

        public string Name = "New Binding";

        [XmlIgnore] public bool IsKeyboard { get; internal set; } = false;
        [XmlIgnore] public TriquetraJoystick Controller;
        
        
        public JoystickOffset Offset;
        
        [XmlIgnore] public int RawOffset => (int)Offset;
        public bool Invert;
        
        public AxisCentering AxisCentering = AxisCentering.Normal;
        
        public TwoAxis SelectedTwoAxis = TwoAxis.Positive;
        
        public POVFacing POVDirection = POVFacing.Up;
        
        public ControllerAction OutputAction = ControllerAction.None;
        
        public bool CombatCollective;
        
        
        public VRInteractAction InputAction = VRInteractAction.Interact;
        public float Speed = 0.1f;
        
        
        public ThumbstickDirection ThumbstickDirection = ThumbstickDirection.None;
        public string VRInteractName = "";
        public float TargetFoV = 0;
        public KeyboardKey KeyboardKey;
        
        [XmlIgnore] public DeviceInstance JoystickDevice;

        [XmlIgnore]
        private static VTModVariables _fs2ModVariables;
        
        [XmlIgnore]
        private Vector3 _lastPosition = Vector3.zero;

        [XmlIgnore]
        public static VTModVariables FS2ModVariables
        {
            get => _fs2ModVariables ?? (_fs2ModVariables = GetFS2ModVariables());
        }

        private static VTModVariables GetFS2ModVariables()
        {
            VTAPI.TryGetModVariables("Danku-FS2", out var modVars);
            return modVars;
        }

        // For the Xml Serialize/Deserialize
        public string ProductGuid
        {
            get => IsKeyboard ? "keyboard" : Controller?.Information.ProductGuid.ToString() ?? "";
            set
            {
                if (value == "keyboard")
                {
                    IsKeyboard = true;
                    return;
                }

                IsKeyboard = false;
                DeviceInstance device = directInput.GetDevices()
                    .Where(x => IsJoystick(x))
                    .FirstOrDefault(x => x.ProductGuid.ToString() == value);

                if (device != null)
                {
                    JoystickDevice = device;
                    Controller = new TriquetraJoystick(directInput, JoystickDevice.InstanceGuid);
                }
            }
        }

        public static bool IsButton(int offset) => offset >= (int)JoystickOffset.Buttons0 && offset <= (int)JoystickOffset.Buttons127;
        public static bool IsPOV(int offset) => offset >= (int)JoystickOffset.PointOfViewControllers0 && offset <= (int)JoystickOffset.PointOfViewControllers3;
        public static bool IsAxis(int offset) => !IsButton(offset) && !IsPOV(offset);

        [XmlIgnore] public bool IsOffsetButton => (IsKeyboard && !KeyboardKey.IsAxis) || RawOffset >= (int)JoystickOffset.Buttons0 && RawOffset <= (int)JoystickOffset.Buttons127;
        [XmlIgnore] public bool IsOffsetPOV => !IsKeyboard && RawOffset >= (int)JoystickOffset.PointOfViewControllers0 && RawOffset <= (int)JoystickOffset.PointOfViewControllers3;
        [XmlIgnore] public bool IsOffsetAxis => !IsOffsetButton && !IsOffsetPOV;

        [XmlIgnore] public bool OffsetSelectOpen = false;
        [XmlIgnore] public bool VRInteractActionSelectOpen = false;
        [XmlIgnore] public bool OutputActionSelectOpen = false;
        [XmlIgnore] public bool POVDirectionSelectOpen = false;
        [XmlIgnore] public bool DetectingOffset = false;
        [XmlIgnore] public bool ThumbstickDirectionSelectOpen = false;
        [XmlIgnore] public bool AxisCenteringSelectOpen = false;

        [XmlIgnore] public TriquetraJoystick.JoystickUpdated bindingDelegate;
        
        
        [XmlIgnore] public TriquetraInput_VRHandController handController = null;

        public Binding()
        {
            NextJoystick();
        }
        public Binding(bool isKeyboard)
        {
            if (isKeyboard)
            {
                IsKeyboard = true;
                AxisCentering = AxisCentering.Middle;
                KeyboardKey = new KeyboardKey();
            }
            else
                NextJoystick();
        }

        private int currentJoystickIndex = -1;
        public void NextJoystick()
        {
            if (IsKeyboard)
                return;

            List<DeviceInstance> devices = directInput.GetDevices().Where(x => IsJoystick(x)).ToList();
            if (devices.Count == 0)
            {
                return;
            }
            currentJoystickIndex = (currentJoystickIndex + 1) % devices.Count;

            this.JoystickDevice = devices[currentJoystickIndex];
            Controller = new TriquetraJoystick(directInput, JoystickDevice.InstanceGuid);
        }
        public void PrevJoystick()
        {
            if (IsKeyboard)
                return;

            List<DeviceInstance> devices = directInput.GetDevices().Where(x => IsJoystick(x)).ToList();
            if (devices.Count == 0)
            {
                return;
            }
            currentJoystickIndex = (currentJoystickIndex - 1) % devices.Count;

            this.JoystickDevice = devices[currentJoystickIndex];
            Controller = new TriquetraJoystick(directInput, JoystickDevice.InstanceGuid);
        }

        public bool IsJoystick(DeviceInstance deviceInstance)
        {
            return deviceInstance.Type == DeviceType.Joystick
                   || deviceInstance.Type == DeviceType.Gamepad
                   || deviceInstance.Type == DeviceType.FirstPerson
                   || deviceInstance.Type == DeviceType.Flight
                   || deviceInstance.Type == DeviceType.Driving
                   || deviceInstance.Type == DeviceType.Supplemental;
        }

        public void RunAction(int joystickValue)
        {
            RunNonFlyingSceneActions(joystickValue);

            if (Plugin.IsFlyingScene())
                RunFlyingSceneActions(joystickValue);
        }

        private void RunNonFlyingSceneActions(int joystickValue)
        {
            Action action = OutputAction switch
            {
                ControllerAction.FlatscreenCenterInteract => () => RunFlatscreenCenterInteract(joystickValue),
                ControllerAction.FlatscreenFoV => () => RunFlatscreenFoV(joystickValue),
                ControllerAction.FlatscreenMoveCamera => () => ControllerActions.FS2Camera.Thumbstick(this, joystickValue),
                ControllerAction.NewVRInteract => () => RunNewVRInteract(joystickValue),
                _ => () => {/* Default case */}
            };

            action();
        }

        private object RunFlatscreenCenterInteract(int joystickValue)
        {
            if (FS2ModVariables != null)
                FS2ModVariables.TrySetValue("InteractCenter", GetButtonPressed(joystickValue));
            else
                Debug.Log("FS2 Mod Variables null!");
            return null;
        }

        private void RunFlatscreenFoV(int joystickValue)
        {
            if (GetButtonPressed(joystickValue))
            {
                if (FS2ModVariables  != null)
                    FS2ModVariables.TrySetValue("SetZoom", TargetFoV);
                else
                    Debug.Log("FS2 Mod Variables null!");
            }
        }

        private object RunNewVRInteract(int joystickValue)
        {
            if (!Plugin.IsFlyingScene())
                return null;

            var vehicleObject = VTAPI.GetPlayersVehicleGameObject();
            if (vehicleObject == null)
                return null;

            var interactables = vehicleObject.GetComponentsInChildren<VRInteractable>();
            var targetInteractable = interactables.FirstOrDefault(x => x.interactableName == VRInteractName);
            
            if (targetInteractable)
                RunVRInteractAction(targetInteractable, joystickValue);
            
            return null;
        }

        private void RunFlyingSceneActions(int joystickValue)
        {
            Action action = OutputAction switch
            {
                ControllerAction.Throttle => () =>
                {
                    if (IsKeyboard) 
                        ControllerActions.Throttle.MoveThrottle(this, joystickValue, 0.025f);
                    else
                        ControllerActions.Throttle.SetThrottle(this, joystickValue);
                },
                ControllerAction.HeloPower => () =>
                {
                    if (IsKeyboard) 
                        ControllerActions.Throttle.MoveThrottle(this, joystickValue, 0.025f);
                    else
                        ControllerActions.Helicopter.SetPower(this, joystickValue);
                },
                ControllerAction.Pitch =>  () => ControllerActions.Joystick.SetPitch(this, joystickValue),
                ControllerAction.Yaw => () => ControllerActions.Joystick.SetYaw(this, joystickValue),
                ControllerAction.Roll => () => ControllerActions.Joystick.SetRoll(this, joystickValue),
                ControllerAction.JoystickTrigger => () => ControllerActions.Joystick.Trigger(this, joystickValue),
                ControllerAction.SwitchWeapon => () => ControllerActions.Joystick.SwitchWeapon(this, joystickValue),
                ControllerAction.JoystickThumbStick => () => ControllerActions.Joystick.Thumbstick(this, joystickValue),
                ControllerAction.ThrottleThumbStick => () => ControllerActions.Throttle.Thumbstick(this, joystickValue),
                ControllerAction.Countermeasures => () => ControllerActions.Throttle.Countermeasures(this, joystickValue),
                ControllerAction.Brakes => () => ControllerActions.Throttle.TriggerBrakes(this, joystickValue),
                ControllerAction.FlapsIncrease => () => ControllerActions.Flaps.IncreaseFlaps(this, joystickValue),
                ControllerAction.FlapsDecrease => () => ControllerActions.Flaps.DecreaseFlaps(this, joystickValue),
                ControllerAction.FlapsCycle => () => ControllerActions.Flaps.CycleFlaps(this, joystickValue),
                ControllerAction.Visor => () => ControllerActions.Helmet.ToggleVisor(this, joystickValue),
                ControllerAction.NightVisionGoggles => () => ControllerActions.Helmet.ToggleNVG(this, joystickValue),
                ControllerAction.PTT => () => ControllerActions.Radio.PTT(this, joystickValue),
                ControllerAction.ToggleMouseFly => () => RunToggleMouseFly(joystickValue),
                ControllerAction.VRInteract => () => RunVRInteractAction(joystickValue),
                ControllerAction.HeloModThrottle => () => ControllerActions.Helicopter.collectiveModded = GetButtonPressed(joystickValue),
                _ => () => {/* Default case */ }
            };

            action();
        }

        private object RunToggleMouseFly(int joystickValue)
        {
            if (GetButtonPressed(joystickValue))
                TriquetraInputBinders.useMouseFly = !TriquetraInputBinders.useMouseFly;
            return null;
        }

        private object RunVRInteractAction(int joystickValue)
        {
            var interactable = GameObject.FindObjectsOfType<VRInteractable>(false)
                .FirstOrDefault(i => i.interactableName.ToLower() == VRInteractName.ToLower());

            if (interactable != null)
            {
                if (GetButtonPressed(joystickValue))
                    Interactions.Interact(interactable);
                else
                    Interactions.AntiInteract(interactable);
            }
            return null;
        }

        public void RunVRInteractAction(VRInteractable interactable, int joystickValue)
        {
            if (handController)
            {
                if (!GetButtonPressed(joystickValue))
                {
                    HandleHandControllerRelease();
                    return;
                }
            }
            else
            {
                if (!GetButtonPressed(joystickValue))
                    return;

                InitializeHandController(interactable);
            }
            
            float joystickAxis = GetAxisAsFloat(joystickValue) - 0.5f;
            
            Action action = InputAction switch
            {
                VRInteractAction.Move => () => HandleMoveAction(interactable, joystickAxis),
                VRInteractAction.PrimaryButton => () => handController?.ThumbButtonPressed(),
                VRInteractAction.SecondaryButton => () => handController?.SecondaryThumbButtonPressed(),
                VRInteractAction.ThumbstickAxis => () => HandleThumbstickAxisAction(joystickAxis),
                VRInteractAction.ThumbstickButton => () => handController?.ThumbstickButtonPressed(),
                VRInteractAction.TriggerAxis => () => handController?.TriggerAxis(GetAxisAsFloat(joystickValue)),
                _ => () => {/* Default case */ }
            };

            action();
        }

        private void HandleHandControllerRelease()
        {
            Action action = InputAction switch
            {
                VRInteractAction.PrimaryButton => () => handController?.ThumbButtonReleased(),
                VRInteractAction.SecondaryButton => () => handController?.SecondaryThumbButtonReleased(),
                VRInteractAction.ThumbstickButton => () => handController?.ThumbstickButtonReleased(),
                VRInteractAction.ThumbstickAxis => () => HandleThumbstickAxisRelease(),
                _ => () => {/* Default case */ }
            };

            action();
            handController.markedForDestruction = true;
            handController = null;
            _lastPosition = Vector3.zero;
        }

        private object HandleThumbstickAxisRelease()
        {
            if (!GameSettings.IsThumbstickMode())
                handController?.ThumbstickButtonReleased();
            return null;
        }

        private void InitializeHandController(VRInteractable interactable)
        {
            var parentA = new GameObject("TriquetraHandController_ParentA");
            var parentB = new GameObject("TriquetraHandController_ParentB");
            parentB.transform.parent = parentA.transform;

            var handControllerObject = new GameObject($"TriquetraHandController_{VRInteractName}");
            handControllerObject.transform.parent = parentB.transform;
            
            handController = handControllerObject.AddComponent<TriquetraInput_VRHandController>();
            var gloveAnimation = handControllerObject.AddComponent<TriquetraInput_GloveAnimation>();
            handController.gloveAnimation = gloveAnimation;
            
            _lastPosition = Vector3.zero;
            
            if (interactable.activeController)
                interactable.StopInteraction();

            handController.activeInteractable = null;
            handController.hoverInteractable = interactable;
            interactable.Click(handController);
        }

        private object HandleMoveAction(VRInteractable interactable, float joystickAxis)
        {
            Vector3 moveDir = GetDirectionFromThumbstick(joystickAxis);
            _lastPosition += moveDir * (Speed * Time.deltaTime);
            handController?.transform.position = interactable.transform.TransformPoint(_lastPosition);
            return null;
        }

        private object HandleThumbstickAxisAction(float joystickAxis)
        {
            Vector2 axis = GetAxis2DFromThumbstick(joystickAxis);
            Vector2 finalAxis = axis;
            
            if (!GameSettings.IsThumbstickMode())
            {
                handController?.ThumbstickButtonPressed();
                finalAxis = ApplyThumbstickClamp(finalAxis, joystickAxis);
            }

            if (IsKeyboard)
                finalAxis *= 2;
            
            handController?.StickAxis(finalAxis);
            return null;
        }

        private Vector3 GetDirectionFromThumbstick(float joystickAxis)
        {
            return ThumbstickDirection switch
            {
                ThumbstickDirection.Up => new Vector3(0, joystickAxis, 0),
                ThumbstickDirection.Right => new Vector3(joystickAxis, 0, 0),
                ThumbstickDirection.Down => new Vector3(0, -joystickAxis, 0),
                ThumbstickDirection.Left => new Vector3(-joystickAxis, 0, 0),
                _ => Vector3.zero
            };
        }

        private Vector2 GetAxis2DFromThumbstick(float joystickAxis)
        {
            return ThumbstickDirection switch
            {
                ThumbstickDirection.Up => new Vector2(0, joystickAxis),
                ThumbstickDirection.Right => new Vector2(joystickAxis, 0),
                ThumbstickDirection.Down => new Vector2(0, -joystickAxis),
                ThumbstickDirection.Left => new Vector2(-joystickAxis, 0),
                _ => Vector2.zero
            };
        }

        private Vector2 ApplyThumbstickClamp(Vector2 finalAxis, float joystickAxis)
        {
            bool xNegative = finalAxis.x < 0;
            bool yNegative = finalAxis.y < 0;

            return ThumbstickDirection switch
            {
                ThumbstickDirection.Right => finalAxis, // No idea why Right is empty, not touching - Midnight
                ThumbstickDirection.Left => new Vector2(
                    Mathf.Clamp(finalAxis.x, xNegative ? -1f : 0.351f, xNegative ? -0.351f : 1),
                    finalAxis.y
                ),
                ThumbstickDirection.Up => finalAxis, // No idea why Up is empty, not touching - Midnight
                ThumbstickDirection.Down => new Vector2(
                    finalAxis.x,
                    Mathf.Clamp(finalAxis.y, yNegative ? -1f : 0.351f, yNegative ? -0.351f : 1)
                ),
                ThumbstickDirection.Press => Vector2.zero,
                _ => finalAxis
            };
        }

        public float GetAxisAsFloat(int value)
        {
            float normalizedValue = NormalizeAxisValue(value);

            return AxisCentering switch
            {
                AxisCentering.TwoAxis => GetTwoAxisValue(value, normalizedValue),
                _ => Invert ? 1f - normalizedValue : normalizedValue
            };
        }

        private float NormalizeAxisValue(int value) => IsOffsetButton switch
        {
            true => (float)value / ButtonMax,
            false => IsOffsetPOV ? (float)value / POVMax : (float)value / AxisMax
        };

        private float GetTwoAxisValue(int value, float normalizedValue)
        {
            if (value > AxisMiddle && SelectedTwoAxis == TwoAxis.Positive)
            {
                float val = 1f - Math.Abs((float)(value - AxisMiddle) / AxisMiddle);
                return Invert ? val : 1f - val;
            }
            
            if (value < AxisMiddle && SelectedTwoAxis == TwoAxis.Negative)
            {
                float val = Math.Abs((float)value / AxisMiddle);
                return Invert ? val : 1f - val;
            }

            return 0;
        }

        public bool GetButtonPressed(int value)
        {
            if (IsOffsetAxis)
                return AxisCentering switch
                {
                    AxisCentering.Middle => value < AxisMiddle - Deadzone || value > AxisMiddle + Deadzone,
                    AxisCentering.TwoAxis => GetAxisAsFloat(value) >= 0.5f,
                    _ => Invert ? value < AxisMax - Deadzone : value > AxisMin + Deadzone
                };

            if (IsOffsetButton)
                return Invert ? value <= ButtonMax : value >= ButtonMax;
            
            if (IsOffsetPOV)
                return CheckPOVDirection(value);

            return false;
        }

        private bool CheckPOVDirection(int value) => POVDirection switch
        {
            POVFacing.Up => value == (int)POVFacing.Up || value == (int)POVFacing.UpRight || value == (int)POVFacing.UpLeft,
            POVFacing.Right => value == (int)POVFacing.Right || value == (int)POVFacing.DownRight || value == (int)POVFacing.UpRight,
            POVFacing.Down => value == (int)POVFacing.Down || value == (int)POVFacing.DownLeft || value == (int)POVFacing.DownRight,
            POVFacing.Left => value == (int)POVFacing.Left || value == (int)POVFacing.UpLeft || value == (int)POVFacing.DownLeft,
            _ => false
        };

        public void HandleKeyboardKeys()
        {
            if (KeyboardKey.IsAxis)
            {
                RunAction(KeyboardKey.GetAxisTranslatedValue());
            }
            else // Is Button
            {
                HandleKeyboardButton();
            }
        }

        private void HandleKeyboardButton()
        {
            if (KeyboardKey.IsRepeatButton)
            {
                bool pressed = UnityEngine.Input.GetKeyDown(KeyboardKey.PrimaryKey);
                RunAction(pressed ? ButtonMax : ButtonMin);
            }
            else
            {
                bool pressed = UnityEngine.Input.GetKeyDown(KeyboardKey.PrimaryKey);
                bool released = UnityEngine.Input.GetKeyUp(KeyboardKey.PrimaryKey);

                if (pressed && !KeyboardKey.PrimaryKeyDown)
                {
                    KeyboardKey.PrimaryKeyDown = true;
                    RunAction(ButtonMax);
                }
                else if (released)
                {
                    KeyboardKey.PrimaryKeyDown = false;
                    RunAction(ButtonMin);
                }
            }
        }
    }

    public enum AxisCentering
    {
        Normal, // Minimum
        Middle,
        TwoAxis
    }

    public enum POVFacing : int
    {
        None = -1,
        Up = 0,
        UpRight = 4500,
        Right = 9000,
        DownRight = 13500,
        Down = 18000,
        DownLeft = 22500,
        Left = 27000,
        UpLeft = 31500,
    }

    public enum ThumbstickDirection
    {
        None,
        Up,
        Down,
        Left,
        Right,
        Press
    }

    public enum TwoAxis
    {
        Positive,
        Negative
    }
}