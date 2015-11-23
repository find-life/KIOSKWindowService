namespace KIOSKWindowService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.KIOSKServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.KIOSKServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // KIOSKServiceProcessInstaller
            // 
            this.KIOSKServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.KIOSKServiceProcessInstaller.Password = null;
            this.KIOSKServiceProcessInstaller.Username = null;
            this.KIOSKServiceProcessInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.KIOSKServiceProcessInstaller_AfterInstall);
            // 
            // KIOSKServiceInstaller
            // 
            this.KIOSKServiceInstaller.Description = "KIOSK Movie System Service";
            this.KIOSKServiceInstaller.ServiceName = "KIOSKSysService";
            this.KIOSKServiceInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.KIOSKServiceInstaller_AfterInstall);
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.KIOSKServiceProcessInstaller,
            this.KIOSKServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller KIOSKServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller KIOSKServiceInstaller;
    }
}