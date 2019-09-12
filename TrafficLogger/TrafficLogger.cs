using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TrafficLogger
{
    public partial class TrafficLogger : ServiceBase
    {
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
        }

        protected override void OnStart(string[] args)
        {
            tmrIntervall.Start();
            trafficLog.WriteEntry("TrafficLogger started", EventLogEntryType.Information);
        }

        protected override void OnContinue()
        {
            tmrIntervall.Start();
            trafficLog.WriteEntry("TrafficLogger continued", EventLogEntryType.Information);
            base.OnContinue();
        }

        protected override void OnStop()
        {
            tmrIntervall.Stop();
            trafficLog.WriteEntry("TrafficLogger stopped", EventLogEntryType.Information);
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
        }
    }
}
