using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace TrafficLogger
{
    public partial class TrafficLogger : ServiceBase
    {
        private TrafficWriter _trafficWriter;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        public TrafficLogger()
        {
            InitializeComponent();

            trafficLog = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("TrafficLogger"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "TrafficLogger", "DayLog");
            }
            trafficLog.Source = "TrafficLogger";
            trafficLog.Log = "DayLog";

            _trafficWriter = new TrafficWriter();
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            //Start timer and write entry
            tmrIntervall.Start();
            trafficLog.WriteEntry("TrafficLogger started", EventLogEntryType.Information);

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnContinue()
        {
            tmrIntervall.Start();
            trafficLog.WriteEntry("TrafficLogger continued", EventLogEntryType.Information);
            base.OnContinue();
        }

        protected override void OnStop()
        {
            // Update the service state to Stop Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            tmrIntervall.Stop();
            trafficLog.WriteEntry("TrafficLogger stopped", EventLogEntryType.Information);

            // Update the service state to Stopped.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnPause()
        {
            tmrIntervall.Stop();
            trafficLog.WriteEntry("TrafficLogger paused", EventLogEntryType.Information);
            base.OnPause();
        }

        protected override void OnShutdown()
        {
            tmrIntervall.Stop();
            trafficLog.WriteEntry("TrafficLogger stopped due to shutdown", EventLogEntryType.Information);
            base.OnShutdown();
        }

        private void tmrIntervall_Tick(object sender, EventArgs e)
        {
            trafficLog.WriteEntry("Action", EventLogEntryType.Information, 1);
            _trafficWriter.WriteLogEntry();
        }
    }
}
