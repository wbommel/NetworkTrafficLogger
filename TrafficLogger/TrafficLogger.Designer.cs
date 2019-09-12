namespace TrafficLogger
{
    partial class TrafficLogger
    {
        /// <summary> 
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Komponenten-Designer generierter Code

        /// <summary> 
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tmrIntervall = new System.Windows.Forms.Timer(this.components);
            this.trafficLog = new System.Diagnostics.EventLog();
            ((System.ComponentModel.ISupportInitialize)(this.trafficLog)).BeginInit();
            // 
            // tmrIntervall
            // 
            this.tmrIntervall.Interval = 60000;
            this.tmrIntervall.Tick += new System.EventHandler(this.tmrIntervall_Tick);
            // 
            // TrafficLogger
            // 
            this.ServiceName = "TrafficLogger";
            ((System.ComponentModel.ISupportInitialize)(this.trafficLog)).EndInit();

        }

        #endregion

        private System.Windows.Forms.Timer tmrIntervall;
        private System.Diagnostics.EventLog trafficLog;
    }
}
