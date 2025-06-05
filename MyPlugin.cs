using CNC5Core;
using HMIAPI;
using HMIControls;

namespace PluginIGEMS
{
    public class MyPlugin : CNCPlugin
    {
        // Static reference to machine - accessible throughout the plugin
        internal static Machine? Machine;

        // Plugin component instances
        internal IndicatorTower indicatorTower;
        internal Drill drill;

        public override void SystemStarted(Machine machine)
        {
            // Store machine reference for use throughout plugin
            Machine = machine;

            // Display startup message on CNC screen
            Machine.SetMessage("My IGEMS CNC");

            // Load all configuration settings from INI file
            MySettings.Load();

            // Initialize optional components based on settings
            if (MySettings.IndicatorTowerEnable)
                indicatorTower = new IndicatorTower(Machine);

            if (MySettings.DrillEnable)
                drill = new Drill(Machine);

            // Custom action button in Integrator Setting menu
            HMIEvents.IntegratorSettingsCustomAction_Click += IntegratorSettingsCustomAction;

            // GUI building event - allows adding custom buttons to main interface
            HMIEvents.BuildingGUI += HMIEvents_BuildingGUI;
        }

        private void IntegratorSettingsCustomAction(object? sender, EventArgs e)
        {
            // Show plugin's main configuration interface
            using (MyHomePage form = new MyHomePage())
            {
                form.ShowDialog();
            }
        }

        private void HMIEvents_BuildingGUI(object? sender, EventArgs e)
        {
            // Register a custom toggle button in the main CNC interface
            HMIToggleButton myButton = Funcs.RegisterToggleButton("lighton", "Turn On Light",MyButtonClicked);

        }

        private void MyButtonClicked(Machine machine, bool on)
        {
            // Control work area lighting based on button state
            if (on)
            {
                machine.SetMessage("Lamp On");
                machine.DOWrite(MySettings.LightningPin, true);
            }
            else
            {
                machine.SetMessage("Lamp Off");
                machine.DOWrite(MySettings.LightningPin, false);
            }
        }

        //State includes 2 bit integer. (0 0)
        //First bit determinate Front Shield status (1 > Up / 0 > Down)
        //Second bit determinate Back Shield status (1 > Up / 0 > Down)
        //We can read states with "ShieldStateChange" and after action we have to set state with "ShieldStateGet" for shields buttons indicator on MainForm.
        public override void ShieldStateChange(int state)
        {
            Shields.SetPrefferedShieldStateWithBits(state);
        }

        public override int ShieldStateGet()
        {
            return Shields.prefferedShieldStateBits;
        }
    }
}
