using System;
using System.Collections.Generic;
using SharpDX.DirectInput;
using UnityEngine;
using UnityEngine.SceneManagement;

// TODO: Jettison button requires push in (menu button)
// TODO: Fix DDArmory bug where swept wings are used as throttle instead of real throttle
// TODO?: duplicate binding
// TODO?: XInput
// TODO?: Lock hand position to joystick
// TODO?: Non 1:1 bindings, being able to set limits and saturation

namespace Triquetra.Input
{
    public class TriquetraInputBinders : MonoBehaviour
    {
        public static TriquetraInputBinders Instance;
        
        public List<POVFacing> POVDirections = new List<POVFacing>() { POVFacing.Up, POVFacing.Right, POVFacing.Down, POVFacing.Left }; // TODO fix/unify

        public bool showWindow = true;
        private Rect windowRect = new Rect(Screen.width - 525, 25, 500, Screen.height - 75);
        private Vector2 scrollPosition;
        public bool Enabled = true;
        private string textFilter = "";
        private Dictionary<Binding, bool> collapsedBindings = new Dictionary<Binding, bool>();

        private void Awake()
        {
            SceneManager.sceneLoaded += (arg0, mode) =>
            {
                useMouseFly = false;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            };
            
            Instance = this;
        }

        public void OnGUI()
        {
            if (showWindow)
                windowRect = GUI.Window(500, windowRect, DoWindow, "Binders Display");
        }

        void DoWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUILayout.Label("Press F10 to toggle this screen");

            Enabled = GUILayout.Toggle(Enabled, "Enabled");

            GUI.enabled = Enabled;

            GUILayout.BeginHorizontal();
            {
                try
                {
                    if (GUILayout.Button("Load"))
                    {
                        Plugin.LoadBindings();
                    }
                    if (GUILayout.Button("Save"))
                    {
                        Plugin.SaveBindings();
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Collapse All"))
                {
                    foreach (Binding binding in Binding.Bindings)
                    {
                        collapsedBindings[binding] = true;
                    }
                }
                if (GUILayout.Button("Expand All"))
                {
                    foreach (Binding binding in Binding.Bindings)
                    {
                        collapsedBindings[binding] = false;
                    }
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginVertical();
            {
                useMouseFly = GUILayout.Toggle(useMouseFly, "Use Mouse Fly");
                GUILayout.Label($"Mouse Sensitivity: {_mouseSensitivity}");
                _mouseSensitivity = GUILayout.HorizontalSlider(_mouseSensitivity, 0.5f, 5f);
            }
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Name Filter: ");
                textFilter = GUILayout.TextField(textFilter);
            }
            GUILayout.EndHorizontal();

            if (Plugin.asyncLoadingBindings)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"Loading Bindings...");
                }
                GUILayout.EndHorizontal();
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            {
                foreach (Binding binding in Binding.Bindings)
                {
                    if (binding == null) continue;

                    if (!binding.Name.ToLower().Contains(textFilter?.ToLower() ?? ""))
                        continue;

                    // New Binding [x]
                    GUILayout.BeginHorizontal();
                    {
                        binding.Name = GUILayout.TextField(binding.Name);

                        if (GUILayout.Button("x", GUILayout.Width(24)))
                        {
                            Binding.Bindings.Remove(binding);
                        }
                    }
                    GUILayout.EndHorizontal();

                    bool isCollapsed = false;
                    if (!collapsedBindings.TryGetValue(binding, out isCollapsed))
                    {
                        collapsedBindings[binding] = true;
                    }

                    if (isCollapsed)
                    {
                        if (GUILayout.Button("----- Expand -----"))
                        {
                            collapsedBindings[binding] = false;
                        }
                        continue;
                    }
                    else
                    {
                        if (GUILayout.Button("----- Collapse -----"))
                        {
                            collapsedBindings[binding] = true;
                        }
                    }

                    if (binding.IsKeyboard)
                    {
                        #region Keyboard Binding

                        // [Button] / [Axis]
                        GUILayout.BeginHorizontal();
                        {
                            // GUILayout.Label("Axis or Button: ");

                            if (GUILayout.Button(binding.KeyboardKey.IsAxis ? "Axis" : "Button"))
                            {
                                binding.KeyboardKey.IsAxis = !binding.KeyboardKey.IsAxis;
                            }
                        }
                        GUILayout.EndHorizontal();

                        if (!binding.KeyboardKey.IsAxis)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                // GUILayout.Label("Repeat or press button: ");

                                if (GUILayout.Button(!binding.KeyboardKey.IsRepeatButton ? "Repeat Button" : "Single Press Button"))
                                {
                                    binding.KeyboardKey.IsRepeatButton = !binding.KeyboardKey.IsRepeatButton;
                                }
                            }
                            GUILayout.EndHorizontal();
                        }

                        // (Button: / Axis + Button:) + ([Detect] / [Cancel])
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label((binding.KeyboardKey.IsAxis ? "Axis + Button: " : "Button: ") + binding.KeyboardKey.PrimaryKey.ToString());

                            if (detectingKeyKK == binding.KeyboardKey && detectingKeyIsSecondary == false)
                            {
                                if (GUILayout.Button("Cancel"))
                                {
                                    detectingKeyKK = null;
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("Detect"))
                                {
                                    detectingKeyKK = binding.KeyboardKey;
                                    detectingKeyIsSecondary = false;
                                }
                            }
                            if (GUILayout.Button("Clear"))
                            {
                                binding.KeyboardKey.PrimaryKey = KeyCode.None;
                            }
                        }
                        GUILayout.EndHorizontal();

                        if (binding.KeyboardKey.IsAxis)
                        {
                            // (Button: / Axis - Button:) + [button] + ([Detect] / [Cancel])
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("Axis - Button: " + binding.KeyboardKey.SecondaryKey.ToString());
                                if (detectingKeyKK == binding.KeyboardKey && detectingKeyIsSecondary == true)
                                {
                                    if (GUILayout.Button("Cancel"))
                                    {
                                        detectingKeyKK = null;
                                    }
                                }
                                else
                                {
                                    if (GUILayout.Button("Detect"))
                                    {
                                        detectingKeyKK = binding.KeyboardKey;
                                        detectingKeyIsSecondary = true;
                                    }
                                }
                                if (GUILayout.Button("Clear"))
                                {
                                    binding.KeyboardKey.SecondaryKey = KeyCode.None;
                                }
                            }
                            GUILayout.EndHorizontal();

                            // Smoothing ---[]-----
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("Smoothing: " + binding.KeyboardKey.Smoothing);
                                binding.KeyboardKey.Smoothing = (float)Math.Round(GUILayout.HorizontalSlider(binding.KeyboardKey.Smoothing, 0f, 1f), 2);
                            }
                            GUILayout.EndHorizontal();
                        }

                        // Action [Select] / Action [Cancel]
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Action: " + binding.OutputAction.ToString());
                            if (GUILayout.Button(binding.OutputActionSelectOpen ? "Cancel" : "Select"))
                            {
                                binding.OutputActionSelectOpen = !binding.OutputActionSelectOpen;
                            }
                        }
                        GUILayout.EndHorizontal();

                        // Thumbstick Direction
                        if (binding.OutputAction == ControllerAction.ThrottleThumbStick || binding.OutputAction == ControllerAction.JoystickThumbStick || binding.OutputAction == ControllerAction.FlatscreenMoveCamera)
                        {
                            if (binding.OutputAction == ControllerAction.ThrottleThumbStick)
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label("(Helo Only) Use Combat Collective: ");
                                    if (GUILayout.Button(binding.CombatCollective ? "True" : "False"))
                                    {
                                        binding.CombatCollective = !binding.CombatCollective;
                                    }
                                }
                                GUILayout.EndHorizontal();
                            }
                            // Thumbstick Direction [Select] / Thumbstick Direction [Cancel]
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("└ Thumbstick Direction: " + binding.ThumbstickDirection.ToString());
                                if (GUILayout.Button(binding.ThumbstickDirectionSelectOpen ? "Cancel" : "Select"))
                                {
                                    binding.ThumbstickDirectionSelectOpen = !binding.ThumbstickDirectionSelectOpen;
                                }
                            }
                            GUILayout.EndHorizontal();

                            if (binding.ThumbstickDirectionSelectOpen)
                            {
                                foreach (ThumbstickDirection direction in Enum.GetValues(typeof(ThumbstickDirection)))
                                {
                                    if (GUILayout.Button(direction.ToString()))
                                    {
                                        binding.ThumbstickDirection = direction;
                                        binding.ThumbstickDirectionSelectOpen = false;
                                    }
                                }
                            }
                        }

                        if (binding.OutputAction == ControllerAction.Countermeasures)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("(Helo Only) Use Combat Collective: ");
                                if (GUILayout.Button(binding.CombatCollective ? "True" : "False"))
                                {
                                    binding.CombatCollective = !binding.CombatCollective;
                                }
                            }
                            GUILayout.EndHorizontal();
                        }

                        if (binding.OutputAction == ControllerAction.Brakes)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("(Helo Only) Use Combat Collective: ");
                                if (GUILayout.Button(binding.CombatCollective ? "True" : "False"))
                                {
                                    binding.CombatCollective = !binding.CombatCollective;
                                }
                            }
                            GUILayout.EndHorizontal();
                        }

                        if (binding.OutputAction == ControllerAction.VRInteract)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("VRInteractable Name: ");
                                binding.VRInteractName = GUILayout.TextField(binding.VRInteractName);
                            }
                            GUILayout.EndHorizontal();
                        }

                        if (binding.OutputAction == ControllerAction.NewVRInteract)
                        {
                            GUILayout.Label("This only works in flying scenes.");
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("VRInteractable Name: ");
                                binding.VRInteractName = GUILayout.TextField(binding.VRInteractName);
                            }
                            GUILayout.EndHorizontal();
                            
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("VRInteractable Action: ");
                                if (GUILayout.Button(binding.InputAction.ToString()))
                                {
                                    binding.VRInteractActionSelectOpen = !binding.VRInteractActionSelectOpen;
                                }
                            }
                            GUILayout.EndHorizontal();
                            
                            if (binding.InputAction == VRInteractAction.Move || binding.InputAction == VRInteractAction.ThumbstickAxis)
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label("Speed: ");
                                    if (float.TryParse(GUILayout.TextField(binding.Speed.ToString()),
                                            out float speed))
                                    {
                                        binding.Speed = speed;
                                    }
                                    
                                }
                                GUILayout.EndHorizontal();
                                
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label($"Direction: {binding.ThumbstickDirection}");
                                    if (!binding.ThumbstickDirectionSelectOpen)
                                    {
                                        if (GUILayout.Button(binding.ThumbstickDirection.ToString()))
                                            binding.ThumbstickDirectionSelectOpen = true;
                                    }
                                    else
                                    {
                                        GUILayout.BeginVertical();
                                        {
                                            foreach (ThumbstickDirection action in Enum.GetValues(typeof(ThumbstickDirection)))
                                            {
                                                if (GUILayout.Button(action.ToString()))
                                                {
                                                    binding.ThumbstickDirection = action;
                                                    binding.ThumbstickDirectionSelectOpen = false;
                                                }
                                            }
                                        }
                                        GUILayout.EndVertical();
                                    }
                                }
                                GUILayout.EndHorizontal();
                            }
                            
                            if (binding.VRInteractActionSelectOpen)
                            {
                                foreach (VRInteractAction action in Enum.GetValues(typeof(VRInteractAction)))
                                {
                                    if (GUILayout.Button(action.ToString()))
                                    {
                                        binding.InputAction = action;
                                        binding.VRInteractActionSelectOpen = false;
                                    }
                                }
                            }
                        }

                        if (binding.OutputAction == ControllerAction.FlatscreenFoV)
                        {
                            GUILayout.Label("Target FoV: " + binding.TargetFoV);
                            binding.TargetFoV = GUILayout.HorizontalSlider(binding.TargetFoV, 30f, 120f);
                        }

                        if (binding.OutputActionSelectOpen)
                        {
                            foreach (ControllerAction action in Enum.GetValues(typeof(ControllerAction)))
                            {
                                if (GUILayout.Button(action.ToString()))
                                {
                                    binding.OutputAction = action;
                                    binding.OutputActionSelectOpen = false;
                                }
                            }
                        }

                        // Axis / Button sliders
                        {
                            GUI.enabled = false;
                            if (binding.KeyboardKey.IsAxis)
                            {
                                bool upPressed = UnityEngine.Input.GetKey(binding.KeyboardKey.PrimaryKey);
                                bool downPressed = UnityEngine.Input.GetKey(binding.KeyboardKey.SecondaryKey);

                                int translatedValue = binding.KeyboardKey.GetAxisTranslatedValue();
                                GUILayout.HorizontalSlider(translatedValue, Binding.AxisMin, Binding.AxisMax);

                                GUI.enabled = Enabled;

                                if (!upPressed && !downPressed)
                                    GUILayout.Label("--- Unpressed ---");
                                else
                                    GUILayout.Label("=== Pressed! ===");
                            }
                            else
                            {
                                bool pressed = UnityEngine.Input.GetKey(binding.KeyboardKey.PrimaryKey);
                                if (pressed)
                                    GUILayout.HorizontalSlider(1, 0, 1);
                                else
                                    GUILayout.HorizontalSlider(0, 0, 1);

                                GUI.enabled = Enabled;
                                GUILayout.Label(pressed ? "=== Pressed! ===" : "--- Unpressed ---");
                            }
                            GUI.enabled = Enabled;
                        }

                        #endregion
                    }
                    else
                    {
                        #region Joystick Binding
                        // [<] Joystick Name [>]
                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("<"))
                            {
                                binding.PrevJoystick();
                            }

                            if (binding.Controller != null)
                                GUILayout.Label($"{binding.Controller.Properties.ProductName} ({binding.JoystickDevice.InstanceGuid}");
                            else
                                GUILayout.Label("No Controller");

                            if (GUILayout.Button(">"))
                            {
                                binding.NextJoystick();
                            }
                        }
                        GUILayout.EndHorizontal();

                        // Offset [Select] / Offset [Cancel]
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Offset: " + binding.Offset.ToString());
                            if (GUILayout.Button(binding.DetectingOffset ? "Cancel" : "Detect"))
                            {
                                if (!binding.DetectingOffset)
                                {
                                    binding.DetectingOffset = true;
                                    binding.bindingDelegate = CreateControllerUpdatedDelegate(binding);
                                    binding.Controller.Updated += binding.bindingDelegate;
                                }
                                else
                                {
                                    try
                                    {
                                        binding.Controller.Updated -= binding.bindingDelegate;
                                    }
                                    catch { }
                                    binding.DetectingOffset = false;
                                }
                            }
                            if (GUILayout.Button(binding.OffsetSelectOpen ? "Cancel" : "Select"))
                            {
                                binding.OffsetSelectOpen = !binding.OffsetSelectOpen;
                            }
                        }
                        GUILayout.EndHorizontal();

                        // [X]
                        // [Y]
                        // [Z]
                        // ...
                        if (binding.OffsetSelectOpen)
                        {
                            foreach (JoystickOffset offset in Enum.GetValues(typeof(JoystickOffset)))
                            {
                                if (GUILayout.Button(offset.ToString()))
                                {
                                    binding.Offset = offset;
                                    binding.OffsetSelectOpen = false;
                                }
                            }
                        }

                        if (!binding.IsOffsetPOV) // If not a POV hat
                        {
                            // [Invert] / [Uninvert]
                            if (GUILayout.Button(binding.Invert ? "Uninvert" : "Invert"))
                            {
                                binding.Invert = !binding.Invert;
                            }
                        }
                        else
                        {
                            // POV Direction
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("└ POV Direction: " + binding.POVDirection.ToString());
                                if (GUILayout.Button(binding.POVDirectionSelectOpen ? "Cancel" : "Select"))
                                {
                                    binding.POVDirectionSelectOpen = !binding.POVDirectionSelectOpen;
                                }
                            }
                            GUILayout.EndHorizontal();

                            if (binding.POVDirectionSelectOpen)
                            {
                                foreach (POVFacing pov in POVDirections)
                                {
                                    if (GUILayout.Button(pov.ToString()))
                                    {
                                        binding.POVDirection = pov;
                                        binding.POVDirectionSelectOpen = false;
                                    }
                                }
                            }
                        }

                        // Axis Centering: [Normal] / Axis Centering: [Middle] / Axis Centering: [TwoAxis]
                        if (binding.IsOffsetAxis)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label($"Axis Centering: {binding.AxisCentering}");
                                if (GUILayout.Button(binding.AxisCenteringSelectOpen ? "Cancel" : "Select"))
                                {
                                    binding.AxisCenteringSelectOpen = !binding.AxisCenteringSelectOpen;
                                }
                            }
                            GUILayout.EndHorizontal();

                            if (binding.AxisCenteringSelectOpen)
                            {
                                foreach (AxisCentering axis in Enum.GetValues(typeof(AxisCentering)))
                                {
                                    if (GUILayout.Button(axis.ToString()))
                                    {
                                        binding.AxisCentering = axis;
                                        binding.AxisCenteringSelectOpen = false;
                                    }
                                }
                            }

                            if (binding.AxisCentering == AxisCentering.TwoAxis)
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label("└ Selected Axis: ");
                                    if (GUILayout.Button(binding.SelectedTwoAxis.ToString()))
                                    {
                                        if (binding.SelectedTwoAxis == TwoAxis.Positive)
                                            binding.SelectedTwoAxis = TwoAxis.Negative;
                                        else
                                            binding.SelectedTwoAxis = TwoAxis.Positive;
                                    }
                                }
                                GUILayout.EndHorizontal();
                            }
                        }

                        // Action [Select] / Action [Cancel]
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Action: " + binding.OutputAction.ToString());
                            if (GUILayout.Button(binding.OutputActionSelectOpen ? "Cancel" : "Select"))
                            {
                                binding.OutputActionSelectOpen = !binding.OutputActionSelectOpen;
                            }
                        }
                        GUILayout.EndHorizontal();

                        if (binding.OutputActionSelectOpen)
                        {
                            foreach (ControllerAction action in Enum.GetValues(typeof(ControllerAction)))
                            {
                                if (GUILayout.Button(action.ToString()))
                                {
                                    binding.OutputAction = action;
                                    binding.OutputActionSelectOpen = false;
                                }
                            }
                        }

                        // Thumbstick Direction
                        if (binding.OutputAction == ControllerAction.ThrottleThumbStick || binding.OutputAction == ControllerAction.JoystickThumbStick || binding.OutputAction == ControllerAction.FlatscreenMoveCamera)
                        {
                            if (binding.OutputAction == ControllerAction.ThrottleThumbStick)
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label("(Helo Only) Use Combat Collective: ");
                                    if (GUILayout.Button(binding.CombatCollective ? "Combat" : "Flight"))
                                    {
                                        binding.CombatCollective = !binding.CombatCollective;
                                    }
                                }
                            }
                            // Thumbstick Direction [Select] / Thumbstick Direction [Cancel]
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("└ Thumbstick Direction: " + binding.ThumbstickDirection.ToString());
                                if (GUILayout.Button(binding.ThumbstickDirectionSelectOpen ? "Cancel" : "Select"))
                                {
                                    binding.ThumbstickDirectionSelectOpen = !binding.ThumbstickDirectionSelectOpen;
                                }
                            }
                            GUILayout.EndHorizontal();

                            if (binding.ThumbstickDirectionSelectOpen)
                            {
                                foreach (ThumbstickDirection direction in Enum.GetValues(typeof(ThumbstickDirection)))
                                {
                                    if (GUILayout.Button(direction.ToString()))
                                    {
                                        binding.ThumbstickDirection = direction;
                                        binding.ThumbstickDirectionSelectOpen = false;
                                    }
                                }
                            }
                        }

                        if (binding.OutputAction == ControllerAction.VRInteract)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("VRInteractable Name: ");
                                binding.VRInteractName = GUILayout.TextField(binding.VRInteractName);
                            }
                            GUILayout.EndHorizontal();
                        }
                        
                        
                        if (binding.OutputAction == ControllerAction.FlatscreenFoV)
                        {
                            GUILayout.Label("Target FoV: " + binding.TargetFoV);
                            binding.TargetFoV = GUILayout.HorizontalSlider(binding.TargetFoV, 30f, 120f);
                        }
 
                        if (binding.Controller != null)
                        {
                            JoystickUpdate value = binding.Controller.RawState[binding.RawOffset];

                            GUI.enabled = false;
                            // Current Axis Value Slider
                            if (binding.IsOffsetButton)
                            {
                                GUILayout.HorizontalSlider(value.Value, Binding.ButtonMin, Binding.ButtonMax);
                            }
                            else if (binding.IsOffsetPOV)
                            {
                                GUILayout.HorizontalSlider(value.Value, Binding.POVMin, Binding.POVMax);
                            }
                            else if (binding.IsOffsetAxis)
                            {
                                GUILayout.HorizontalSlider(value.Value, Binding.AxisMin, Binding.AxisMax);
                            }
                            GUI.enabled = Enabled;

                            GUILayout.Label(binding.GetButtonPressed(value.Value) ? "=== Pressed! ===" : "--- Unpressed ---");
                        }
                        #endregion
                    }

                    GUILayout.Space(30);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Create Joystick Binding"))
                {
                    Binding.Bindings.Add(new Binding());
                }

                if (GUILayout.Button("Create Keyboard Binding"))
                {
                    Binding.Bindings.Add(new Binding(true));
                }
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        private TriquetraJoystick.JoystickUpdated CreateControllerUpdatedDelegate(Binding binding)
        {
            return (TriquetraJoystick joystick, JoystickUpdate update) =>
            {
                if (joystick.Information.InstanceGuid == binding.Controller?.Information.InstanceGuid)
                {
                    // Logger.WriteLine($"Detecting update from {update.Offset} - {update.Value}");

                    if (Binding.IsButton(update.RawOffset) && update.Value >= 128)
                    {
                        binding.Offset = update.Offset;
                        binding.Controller.Updated -= binding.bindingDelegate;
                        binding.DetectingOffset = false;
                        return;
                    }
                    else if (Binding.IsPOV(update.RawOffset) && update.Value > -1)
                    {
                        binding.Offset = update.Offset;
                        binding.Controller.Updated -= binding.bindingDelegate;
                        binding.DetectingOffset = false;
                        return;
                    }
                    else if (Binding.IsAxis(update.RawOffset))
                    {
                        binding.Offset = update.Offset;
                        binding.Controller.Updated -= binding.bindingDelegate;
                        binding.DetectingOffset = false;
                        return;
                    }
                }
            };
        }

        KeyboardKey detectingKeyKK;
        bool detectingKeyIsSecondary;
        // Binding detectingKeyBinding;

        // Not the best way to detect the pressed key, but good enough
        public void DetectDownKey()
        {
            if (detectingKeyKK == null)
                return;
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.None || key == KeyCode.F9 || key == KeyCode.F10 || key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Mouse2) // Ignore these keycodes
                    continue;
                if (key == KeyCode.Escape && UnityEngine.Input.GetKeyDown(key)) // Cancel on ESC
                {
                    detectingKeyKK = null;
                    continue;
                }
                if (key == KeyCode.JoystickButton0) // Ignore all the Joystick buttons
                    return;

                if (UnityEngine.Input.GetKeyDown(key))
                {
                    if (!detectingKeyIsSecondary)
                        detectingKeyKK.PrimaryKey = key;
                    else
                        detectingKeyKK.SecondaryKey = key;

                    detectingKeyKK = null;
                    return;
                }
            }
        }

        public static bool useMouseFly = false;

        private float _mouseSensitivity = 5f;

        private Vector3 _mouseXY = Vector3.zero;

        public void LateUpdate()
        {
            if (detectingKeyKK != null)
                DetectDownKey();

            if (UnityEngine.Input.GetKeyDown(KeyCode.F10))
            {
                showWindow = !showWindow;
            }

            TriquetraInputJoysticks.PollActiveJoysticks();
            TriquetraInputJoysticks.HandleKeyboardBindings();

            ControllerActions.FS2Camera.UpdateThumbstick();
            
            if (Plugin.IsFlyingScene())
            {
                /*if (UnityEngine.Input.GetKeyDown(KeyCode.LeftAlt))
                    _useMouseFly = !_useMouseFly;*/
                
                if (useMouseFly)
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    UpdateMouse();
                }
                else
                {
                    if (Binding.FS2ModVariables != null && Binding.FS2ModVariables.TryGetValue("CursorHideTimer", out var timer))
                    {
                        Cursor.visible = (float)timer > 0;
                    }
                    else
                    {
                        Cursor.visible = true;
                    }
                    Cursor.lockState = CursorLockMode.None;
                    ControllerActions.Joystick.UpdateStick();
                }
                ControllerActions.Joystick.UpdateThumbstick();
                //ControllerActions.Throttle.UpdateThumbstick();
            }
        }

        private void UpdateMouse()
        {
            var currJoy = ControllerActions.Joystick.GetStick();
            
            var x = UnityEngine.Input.GetAxis("Mouse X") * _mouseSensitivity;
            var y = UnityEngine.Input.GetAxis("Mouse Y") * _mouseSensitivity;

            
            Vector3 mouseFly = new Vector3(y, 0, -x) + currJoy;

            Vector3 finalStick = Vector3.Lerp(_mouseXY, mouseFly, 3.5f * Time.deltaTime);

            finalStick.x = Mathf.Clamp(finalStick.x, -1f, 1f);
            finalStick.z = Mathf.Clamp(finalStick.z, -1f, 1f);

            _mouseXY = finalStick;

            ControllerActions.Joystick.joystick?.OnSetStick?.Invoke(_mouseXY);
        }

        int frameCount = 0;
        // Main update loop for all logic
        public void FixedUpdate()
        {
            frameCount++;
            if (!Enabled)
                return;

            if (frameCount >= 5 * 60) // every 5 seconds
            {
                frameCount = 0;
                TriquetraInputJoysticks.PopulateActiveJoysticks();
                ControllerActions.TryGetSticks();
            }
        }
    }
}