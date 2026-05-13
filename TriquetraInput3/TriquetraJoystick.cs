using System;
using System.IO; // Required for error logger
using System.Collections.Generic;
using SharpDX.DirectInput;
using Debug = UnityEngine.Debug;

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
        
        public JoystickState State
        {
            get => GetOrCreateState();
        }

        public JoystickUpdate[] RawState
        {
            get => GetOrCreateRawState();
        }

        private JoystickState GetOrCreateState()
        {
            int id = Properties.JoystickId;
            if (!joystickStates.TryGetValue(id, out var state))
            {
                state = new JoystickState();
                joystickStates[id] = state;
            }
            return state;
        }

        private JoystickUpdate[] GetOrCreateRawState()
        {
            int id = Properties.JoystickId;
            if (!rawStates.TryGetValue(id, out var state))
            {
                state = new JoystickUpdate[268];
                rawStates[id] = state;
            }
            return state;
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
                if (updates is null || updates.Length == 0)
                    return;

                ProcessJoystickUpdates(updates);
            }
            catch (Exception e)
            {
                // Print full error including line number
                LogToFile($"[CRITICAL POLL ERROR]\nMsg: {e.Message}\nStack Trace: {e.StackTrace}");
                Debug.Log("[Triquetra] Critical Failure in Poll. Check logs.");
            }
        }

        private void ProcessJoystickUpdates(JoystickUpdate[] updates)
        {
            // Grouping updates to prevent FPS drops from dual-throttle input
            Dictionary<int, int> frameSummary = new Dictionary<int, int>();
            foreach (JoystickUpdate update in updates)
            {
                // Update the internal states first
                State.Update(update);
                RawState[update.RawOffset] = update;
                frameSummary[(int)update.Offset] = update.Value;
            }

            // Process unique axis movements only once per frame
            foreach (var entry in frameSummary)
            {
                ProcessFrameSummaryEntry(entry.Key, entry.Value);
            }
        }

        private void ProcessFrameSummaryEntry(int currentOffset, int currentValue)
        {
            // Optimized Lookup: Only loop through bindings that match this specific ID
            foreach (Binding binding in Binding.Bindings)
            {
                // Null check to ensure binding and its controller exist
                if (binding?.Controller == null)
                    continue;

                // Skip irrelevant bindings immediately
                if (binding.IsKeyboard || (int)binding.Offset != currentOffset)
                    continue;
                
                if (binding.Controller.Properties.JoystickId != this.Properties.JoystickId)
                    continue;

                // DEADZONE/THRESHOLD CHECK
                // Only run heavy plane logic if the value changed significantly
                // (Prevents "Sensor Jitter" from tanking FPS)
                if (Math.Abs(currentValue - binding.LastValue) > 10) 
                {
                    try
                    {
                        binding.RunAction(currentValue);
                        binding.LastValue = currentValue;
                    }
                    catch (Exception actionEx)
                    {
                        LogToFile($"[Action Error] Offset: {currentOffset} | Msg: {actionEx.Message}");
                    }
                }

                try
                {
                    binding.Controller.Updated?.Invoke(this, RawState[currentOffset]);
                }
                catch (Exception invokeEx)
                {
                    LogToFile($"[Invoke Error] | Msg: {invokeEx.Message}");
                }
            }
        }

        // Custom logger
        private void LogToFile(string text)
        {
            Debug.Log($"[TRIQUETRA] {text}");
        }

    }
}