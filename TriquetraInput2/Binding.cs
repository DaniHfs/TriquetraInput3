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
            get
            {
                if (_fs2ModVariables != null)
                    return _fs2ModVariables;
                VTAPI.TryGetModVariables("Danku-FS2", out _fs2ModVariables);
                return _fs2ModVariables;
            }
        }

        // For the Xml Serialize/Deserialize
        public string ProductGuid
        {
            get
            {
                if (IsKeyboard)
                    return "keyboard";
                else
                    return Controller?.Information.ProductGuid.ToString() ?? "";
            }
            set
            {
                if (value == "keyboard")
                {
                    IsKeyboard = true;
                    return;
                }
                IsKeyboard = false;

                DeviceInstance device = directInput.GetDevices().Where(x => IsJoystick(x)).FirstOrDefault(x => x.ProductGuid.ToString() == value);
                if (device == null)
                    return;

                this.JoystickDevice = device;
                Controller = new TriquetraJoystick(directInput, JoystickDevice.InstanceGuid);
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
            if (OutputAction == ControllerAction.FlatscreenCenterInteract)
            {
                if (FS2ModVariables != null)
                {
                    
                    FS2ModVariables.TrySetValue("InteractCenter", GetButtonPressed(joystickValue));
                }
                else
                {
                    Debug.Log($"FS2 Mod Variables null!");
                }
            }
            else if (OutputAction == ControllerAction.FlatscreenFoV)
            {
                if (GetButtonPressed(joystickValue))
                {
                    if (FS2ModVariables != null)
                    {
                        FS2ModVariables.TrySetValue("SetZoom", TargetFoV);
                    }
                    else
                    {
                        Debug.Log($"FS2 Mod Variables null!");
                    }
                }
            }
            else if (OutputAction == ControllerAction.FlatscreenMoveCamera)
            {
                ControllerActions.FS2Camera.Thumbstick(this, joystickValue);
            }
            else if (OutputAction == ControllerAction.NewVRInteract) // imitating controller for custom interactables maybe
            {
                if (Plugin.IsFlyingScene())
                {
                    var vehicleObject = VTAPI.GetPlayersVehicleGameObject();
                    if (vehicleObject != null)
                    {
                        var interactables = vehicleObject.GetComponentsInChildren<VRInteractable>();
                        var targetInteractable =
                            interactables.FirstOrDefault(x => x.interactableName == VRInteractName);
                        if (targetInteractable)
                        {
                            RunVRInteractAction(targetInteractable, joystickValue);
                        }
                    }
                }
            }
            if (Plugin.IsFlyingScene()) // Only try and get throttle in a flying scene
            {
                if (OutputAction == ControllerAction.Throttle)
                {
                    if (IsKeyboard)
                        ControllerActions.Throttle.MoveThrottle(this, joystickValue, 0.025f);
                    else
                        ControllerActions.Throttle.SetThrottle(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.HeloPower)
                {
                    if (IsKeyboard)
                        ControllerActions.Throttle.MoveThrottle(this, joystickValue, 0.025f);
                    else
                        ControllerActions.Helicopter.SetPower(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Pitch)
                {
                    ControllerActions.Joystick.SetPitch(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Yaw)
                {
                    ControllerActions.Joystick.SetYaw(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Roll)
                {
                    ControllerActions.Joystick.SetRoll(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.JoystickTrigger)
                {
                    ControllerActions.Joystick.Trigger(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.SwitchWeapon)
                {
                    ControllerActions.Joystick.SwitchWeapon(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.JoystickThumbStick)
                {
                    ControllerActions.Joystick.Thumbstick(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.ThrottleThumbStick)
                {
                    ControllerActions.Throttle.Thumbstick(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Countermeasures)
                {
                    ControllerActions.Throttle.Countermeasures(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Brakes)
                {
                    ControllerActions.Throttle.TriggerBrakes(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.FlapsIncrease)
                {
                    ControllerActions.Flaps.IncreaseFlaps(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.FlapsDecrease)
                {
                    ControllerActions.Flaps.DecreaseFlaps(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.FlapsCycle)
                {
                    ControllerActions.Flaps.CycleFlaps(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Visor)
                {
                    ControllerActions.Helmet.ToggleVisor(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.NightVisionGoggles)
                {
                    ControllerActions.Helmet.ToggleNVG(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.PTT)
                {
                    ControllerActions.Radio.PTT(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.ToggleMouseFly)
                {
                    if (GetButtonPressed(joystickValue))
                        TriquetraInputBinders.useMouseFly = !TriquetraInputBinders.useMouseFly;
                }
                else if (OutputAction == ControllerAction.VRInteract)
                {
                    VRInteractable interactable = GameObject.FindObjectsOfType<VRInteractable>(false)
                        .Where(i => i.interactableName.ToLower() == VRInteractName.ToLower())
                        .FirstOrDefault();
                    if (interactable != null)
                    {
                        if (GetButtonPressed(joystickValue))
                            Interactions.Interact(interactable);
                        else
                            Interactions.AntiInteract(interactable);
                    }
                }
                else if (OutputAction == ControllerAction.HeloModThrottle)
                {
                    ControllerActions.Helicopter.collectiveModded = GetButtonPressed(joystickValue);
                }
            }
        }

        /*private IEnumerator DestroyControllerRoutine(GameObject controllerGameObjectObj)
        {
            if (controllerGameObjectObj == null)
            {
                Debug.LogError($"[TriquetraInput2] No controller to destroy?????");
                yield break;
            }
            
            yield return new WaitForEndOfFrame();
            Debug.Log($"Destroying controller {controllerGameObjectObj}");
            Object.Destroy(controllerGameObjectObj);
        }*/

        public void RunVRInteractAction(VRInteractable interactable, int joystickValue)
        {
            if (handController)
            {
                if (!GetButtonPressed(joystickValue))
                {
                    switch (InputAction)
                    {
                        case VRInteractAction.PrimaryButton:
                            handController.ThumbButtonReleased();
                            break;
                        case VRInteractAction.SecondaryButton:
                            handController.SecondaryThumbButtonReleased();
                            break;
                        case VRInteractAction.ThumbstickButton:
                            handController.ThumbstickButtonReleased();
                            break;
                        case VRInteractAction.ThumbstickAxis:
                            if (!GameSettings.IsThumbstickMode())
                                handController.ThumbstickButtonReleased();
                            break;
                    }

                    // Destroy at end of next frame so joysticks and stuff can access its variables
                    handController.markedForDestruction = true;
                    handController = null;
                    _lastPosition = Vector3.zero;

                    
                    return;
                }
            }
            else
            {
                if (!GetButtonPressed(joystickValue))
                    return;
                
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
            
            float joystickAxis = GetAxisAsFloat(joystickValue) - 0.5f;
            
            switch (InputAction)
            {
                case VRInteractAction.Move:
                    Vector3 moveDir = Vector3.zero;
                    
                    switch (ThumbstickDirection)
                    {
                        case ThumbstickDirection.Up:
                            moveDir.y += joystickAxis;
                            break;
                        case ThumbstickDirection.Right:
                            moveDir.x += joystickAxis;
                            break;
                        case ThumbstickDirection.Down:
                            moveDir.y -= joystickAxis;
                            break;
                        case ThumbstickDirection.Left:
                            moveDir.x -= joystickAxis;
                            break;
                    }
                    _lastPosition += moveDir * (Speed * Time.deltaTime);
                    handController.transform.position = interactable.transform.TransformPoint(_lastPosition);
                    break;
                case VRInteractAction.PrimaryButton:
                    handController.ThumbButtonPressed();
                    break;
                case VRInteractAction.SecondaryButton:
                    handController.SecondaryThumbButtonPressed();
                    break;
                case VRInteractAction.ThumbstickAxis:
                    Vector2 axis = Vector3.zero;
                    
                    

                    switch (ThumbstickDirection)
                    {
                        case ThumbstickDirection.Up:
                            axis.y += joystickAxis;
                            break;
                        case ThumbstickDirection.Right:
                            axis.x += joystickAxis;
                            break;
                        case ThumbstickDirection.Down:
                            axis.y -= joystickAxis;
                            break;
                        case ThumbstickDirection.Left:
                            axis.x -= joystickAxis;
                            break;
                    }
                    
                    Vector2 finalAxis = axis;
                    if (!GameSettings.IsThumbstickMode())
                    {
                        handController.ThumbstickButtonPressed();
                        bool xNegative = finalAxis.x < 0;
                        bool yNegative = finalAxis.y < 0;
                        switch (ThumbstickDirection)
                        {
                            case ThumbstickDirection.Right:
                            case ThumbstickDirection.Left:
                                finalAxis.x = Mathf.Clamp(finalAxis.x, xNegative ? -1f : 0.351f, xNegative ? -0.351f : 1);
                                break;
                            case ThumbstickDirection.Up:
                            case ThumbstickDirection.Down:
                                finalAxis.y = Mathf.Clamp(finalAxis.y, yNegative ? -1f : 0.351f, yNegative ? -0.351f : 1);
                                break;
                            case ThumbstickDirection.Press:
                                finalAxis = Vector2.zero;
                                break;
                        }
                    }

                    // I have no clue if joysticks dont need this so pray
                    if (IsKeyboard)
                        finalAxis *= 2; 
                    
                    handController.StickAxis(finalAxis);
                    break;
                case VRInteractAction.ThumbstickButton:
                    handController.ThumbstickButtonPressed();
                    break;
                case VRInteractAction.TriggerAxis:
                    handController.TriggerAxis(GetAxisAsFloat(joystickValue));
                    break;
            }
        }

        public float GetAxisAsFloat(int value)
        {
            if (IsOffsetButton)
            {
                if (Invert)
                    return 1f - ((float)value / ButtonMax);
                return (float)value / ButtonMax;
            }
            if (IsOffsetPOV)
            {
                return (float)value / POVMax;
            }
            if (AxisCentering == AxisCentering.TwoAxis)
            {
                if (value > AxisMiddle && SelectedTwoAxis == TwoAxis.Positive)
                {
                    float val = 1f - Math.Abs((float)((float)(value - AxisMiddle) / AxisMiddle));
                    return Invert ? val : 1f - val;
                }
                else if (value < AxisMiddle && SelectedTwoAxis == TwoAxis.Negative)
                {
                    float val = Math.Abs((float)((float)value / AxisMiddle));
                    return Invert ? val : 1f - val;
                }
                else
                    return 0;

            }
            if (Invert)
                return 1f - ((float)value / AxisMax);
            return (float)value / AxisMax;
        }

        public bool GetButtonPressed(int value)
        {
            if (IsOffsetAxis)
            {
                if (AxisCentering == AxisCentering.Middle)
                    return value < AxisMiddle - Deadzone || value > AxisMiddle + Deadzone;
                else if (AxisCentering == AxisCentering.TwoAxis)
                {
                    return GetAxisAsFloat(value) >= 0.5f;
                }
                else // Normal Min-Max
                {
                    if (Invert) // Max-Min
                        return value < AxisMax - Deadzone;
                    else // Min-Max
                        return value > AxisMin + Deadzone;
                }
            }
            if (IsOffsetButton)
            {
                if (Invert)
                    return value <= ButtonMax;
                else
                    return value >= ButtonMax;
            }
            if (IsOffsetPOV)
            {
                if (POVDirection == POVFacing.Up)
                    return value == (int)POVFacing.Up || value == (int)POVFacing.UpRight || value == (int)POVFacing.UpLeft;
                else if (POVDirection == POVFacing.Right)
                    return value == (int)POVFacing.Right || value == (int)POVFacing.DownRight || value == (int)POVFacing.UpRight;
                else if (POVDirection == POVFacing.Down)
                    return value == (int)POVFacing.Down || value == (int)POVFacing.DownLeft || value == (int)POVFacing.DownRight;
                else if (POVDirection == POVFacing.Left)
                    return value == (int)POVFacing.Left || value == (int)POVFacing.UpLeft || value == (int)POVFacing.DownLeft;
                else
                    return false;
            }
            return false;
        }

        public void HandleKeyboardKeys()
        {
            if (KeyboardKey.IsAxis)
            {
                int translatedValue = KeyboardKey.GetAxisTranslatedValue();

                RunAction(translatedValue);
            }
            else // Is Button
            {
                if (KeyboardKey.IsRepeatButton)
                {
                    bool pressed = UnityEngine.Input.GetKeyDown(KeyboardKey.PrimaryKey);
                    int translatedValue = pressed ? ButtonMax : ButtonMin;
                    RunAction(translatedValue);
                }
                else
                {
                    bool pressed = UnityEngine.Input.GetKeyDown(KeyboardKey.PrimaryKey);
                    bool released = UnityEngine.Input.GetKeyUp(KeyboardKey.PrimaryKey);
                    int translatedValue = pressed ? ButtonMax : ButtonMin;

                    if (pressed && !KeyboardKey.PrimaryKeyDown)
                    {
                        KeyboardKey.PrimaryKeyDown = true;
                        RunAction(translatedValue);
                    }
                    else if (released)
                    {
                        KeyboardKey.PrimaryKeyDown = false;
                        RunAction(translatedValue);
                    }
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