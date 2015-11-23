using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Management;
using System.ServiceProcess;

namespace KIOSKWindowService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        #region 设置服务安装后自动启动事件
        /// <summary>
        /// 设置服务安装后自动启动的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KIOSKServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            ServiceController service = new ServiceController(this.KIOSKServiceInstaller.ServiceName);
            service.Start();//设置服务安装后立即启动
        }
        #endregion


        #region 设置服务与桌面交互  在serviceInstaller1的AfterInstall事件中使用
        /// <summary>
        /// 设置服务与桌面交互，在serviceInstaller1的AfterInstall事件中使用
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        private void SetServiceDesktopInsteract(string serviceName)
        {
            ManagementObject wmiService = new ManagementObject(string.Format("Win32_Service.Name='{0}'", serviceName));
            ManagementBaseObject changeMethod = wmiService.GetMethodParameters("Change");
            changeMethod["DesktopInteract"] = true;
            ManagementBaseObject utParam = wmiService.InvokeMethod("Change", changeMethod,null);
        }
        #endregion

        private void KIOSKServiceProcessInstaller_AfterInstall(object sender, InstallEventArgs e)
        {

        }

    }
}
