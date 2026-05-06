using System;
using System.IO; // Required for error logger
using System.Collections.Generic;
using SharpDX.DirectInput;

namespace Triquetra.Input
{
    public class TriquetraJoystick : Joystick
    {
        private static Dictionary<int, JoystickState> joystickStates = new Dictionary<int, JoystickState>();
        private static Dictionary<int, JoystickUpdate[]> rawStates = new Dictionary<int, JoystickUpdate[]>();
        private bool hasAcquired;

        public TriquetraJoystick(IntPtr nativePtr) : base(nativePtr)
        {
        }

        public TriquetraJoystick(DirectInput directInput, Guid deviceGuid) : base(directInput, deviceGuid)
        {
        }

        public bool HasAcquired { get => hasAcquired; private set => hasAcquired = value; }
        public JoystickState State { get
            {
                if (!joystickStates.ContainsKey(Properties.JoystickId))
                {
                    joystickStates.Add(Properties.JoystickId, new JoystickState());
                }
                return joystickStates[Properties.JoystickId];
            }
        }
        public JoystickUpdate[] RawState
        {
            get
            {
                if (!rawStates.ContainsKey(Properties.JoystickId))
                {
                    rawStates.Add(Properties.JoystickId, new JoystickUpdate[268]);
                }
                return rawStates[Properties.JoystickId];
            }
        }

        public new void Acquire()
        {
            hasAcquired = true;
            Properties.BufferSize = 128;
            base.Acquire();
        }

        public delegate void JoystickUpdated(TriquetraJoystick joystick, JoystickUpdate update);

        public event JoystickUpdated Updated;

        public new void Poll()
        {
            if (!hasAcquired)
                Acquire();

            try 
            {
                base.Poll();
                JoystickUpdate[] updates = base.GetBufferedData();
                
                // If no movement, exit early and save CPU
                if (updates == null || updates.Length == 0) return;

                foreach (JoystickUpdate update in updates)
                {
                    // Update the internal states first
                    State.Update(update);
                    RawState[update.RawOffset] = update;

                    // Optimized Lookup: Only loop through bindings that match this specific ID
                    // Perhaps use a Dictionary to be even faster?
                    foreach (Binding binding in Binding.Bindings)
                    {
                        // Skip irrelevant bindings immediately
                        if (binding.IsKeyboard || binding.Offset != update.Offset) continue;
                        
                        if (binding.Controller.Properties.JoystickId == this.Properties.JoystickId)
                        {
                            // DEADZONE/THRESHOLD CHECK
                            // Only run heavy plane logic if the value changed significantly
                            // (Prevents "Sensor Jitter" from tanking FPS)
                            if (Math.Abs(update.Value - binding.LastValue) > 10) 
                            {
                                // Error logger
                                try
                                {
                                    binding.RunAction(update.Value);
                                }
                                catch (Exception actionEx)
                                {
                                    LogToFile($"Action Error (Offset {update.Offset}): {actionEx.Message}\n{actionEx.StackTrace}");
                                }
                                
                                binding.LastValue = update.Value; // Added LastValue to binding class
                            }

                            binding.Controller.Updated?.Invoke(this, update);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Hopefully this will catch big stuff (Like the loop crashing entirely)
                LogToFile($"CRITICAL POLL ERROR: {e.Message}\n{e.StackTrace}");
                // DirectInput can throw errors if a device is unplugged mid-game
                UnityEngine.Debug.LogError("TriquetraInput: Error during Poll: " + e.Message);
            }
        }

        // Helper method to write error log file
        private void LogToFile(string text)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Triquetra_Debug_Log.txt");
                string logEntry = $"[{DateTime.Now:dd-MM-yyyy HH:mm:ss.fff}] {text}\n" + new string('-', 30) + "\n";
                File.AppendAllText(path, logEntry);
            }
            catch {/* Ignore logging errors to prevent recursive crashing*/}
        }
    }
}