using CNC5Core;
using HMI5;
using HMIAPI;

namespace PluginIGEMS
{
    internal class IndicatorTower
    {
        bool closing;
        Machine? machine;

        internal IndicatorTower(Machine machine) 
        {
            // Check if indicator tower is properly configured in settings
            // If all pins are -1 (unconfigured), skip initialization
            if (MySettings.IndicatorRedBulb < 0 && MySettings.IndicatorGreenBulb < 0 && MySettings.IndicatorYellowBulb < 0)
                return;

            this.machine = machine;

            // Perform initial hardware test to verify all lights work
            LightTest();

            // Subscribe to slow timer for periodic status updates
            HMIEvents.SlowTimerTick += HMIEvents_SlowTimerTick;

            // Subscribe to system shutdown to turn off lights cleanly
            HMIEvents.SystemClosing += HMIEvents_SystemClosing;
        }

        private void HMIEvents_SystemClosing(object? sender, EventArgs e)
        {
            // Prevent SlowTimer from interfering during shutdown
            closing = true;

            // Turn off all indicator lights
            SetColor(LedColors.Off);
        }

        private void LightTest()
        {
            // Start with all lights off
            SetColor(LedColors.Off);

            // Sequential light test with 200ms delays
            // Red light test
            machine.DOWrite(MySettings.IndicatorRedBulb, true);
            Thread.Sleep(200);
            // Green light test
            machine.DOWrite(MySettings.IndicatorGreenBulb, true);
            Thread.Sleep(200);
            // Yellow light test
            machine.DOWrite(MySettings.IndicatorYellowBulb, true);
            Thread.Sleep(200);
            // Turn all lights off after test
            SetColor(LedColors.Off);
        }

        
        /// Sets the indicator tower to display specified color combination
        private void SetColor(LedColors color)
        {
            // Use HasFlag to check if each color bit is set in the combination
            machine.DOWrite(MySettings.IndicatorRedBulb, color.HasFlag(LedColors.Red));
            machine.DOWrite(MySettings.IndicatorGreenBulb, color.HasFlag(LedColors.Green));
            machine.DOWrite(MySettings.IndicatorYellowBulb, color.HasFlag(LedColors.Yellow));
        }

        private void HMIEvents_SlowTimerTick(object? sender, EventArgs e)
        {
            // Update indicator colors based on current machine state
            DecideColor();
        }

        private void DecideColor()
        {
            // Local flags for different error conditions
            bool error_progstop = false; // Errors that stop program execution
            bool error_warning = false;  // Warning-level errors that don't stop program

            // Skip processing during shutdown
            if (closing)
                return;

            // Default to no lights
            LedColors color = LedColors.Off;

            // Analyze current error state
            if (ErrorManager.ActiveErrorCount > 0) // Active errors exist
            {
                // Check severity of most severe error
                if (ErrorManager.MostSevereError.ErrorLevel != CNCExceptionLevel.Level5Warning) 
                {
                    error_progstop = true;
                }
                else
                {
                    error_warning = true;
                }
            }

            // HIGHEST PRIORITY: Emergency stop or program-stopping errors
            if (!Funcs.EmergencyOK || error_progstop) //Emergency Stop active or Program Stopped Couse of one Error
            {
                color = LedColors.Red;
            }
            // SECOND PRIORITY: Machine is running a program
            else if (machine.RunState == RunState.Running) 
            {
                if (machine.HaltState == HaltState.Paused) //Machine Paused
                {
                    if (error_warning) //Paused and Has Warning
                    {
                        color |= LedColors.Red_Yellow;
                    }
                    else //Paused and no Warning
                    {
                        color |= LedColors.Green_Yellow;
                    } 
                }
                else if (error_warning) //Program still running but there is a warning
                {
                    color |= LedColors.Red_Yellow;
                }
                else
                {
                    color |= LedColors.Yellow;
                }
            }
            // THIRD PRIORITY: Not running but has warnings
            else if (machine.RunState != RunState.Running && error_warning) 
            {
                color |= LedColors.Red_Green;
            }
            // LOWEST PRIORITY: Ready and no issues
            else 
            {
                color |= LedColors.Green;
            }

            // Apply the determined color to physical lights
            SetColor(color);  
        }

        enum LedColors
        {
            Off = 0,
            Red = 1,
            Green = 2,
            Red_Green = 3,
            Yellow = 4,
            Red_Yellow = 5,
            Green_Yellow = 6,
        }
    }
}
