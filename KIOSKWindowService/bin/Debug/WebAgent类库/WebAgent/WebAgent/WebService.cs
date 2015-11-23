using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace WebAgent
{
    public class WebService
    {
        #region 获取文件ini路径
        public static string WASConfigPath = Application.StartupPath + "\\WebAgentService.ini";//获取INI文件路径
        #endregion


        #region 读取ini文件
        /// <summary>
        /// 读取INI文件
        /// </summary>
        /// <param name="section">节点名称</param>
        /// <param name="key">键</param>
        /// <param name="def">值</param>
        /// <param name="retval">stringbulider对象</param>
        /// <param name="size">字节大小</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        [DllImport("kernel32")]
        public static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retval, int size, string filePath);
        #endregion


        #region 读取ini文件中指定节点的指定key的value值
        /// <summary>
        /// 自定义读取INI文件中的内容方法
        /// </summary>
        /// <param name="Section">键</param>
        /// <param name="key">值</param>
        /// <param name="Path">文件路径</param>
        /// <returns></returns>
        public static string ContentValue(string section, string key, string Path)
        {
            if (section.Trim().Length <= 0 || key.Trim().Length <= 0)
            {
                return string.Empty;
            }
            else
            {
                StringBuilder temp = new StringBuilder(1024);
                GetPrivateProfileString(section, key, string.Empty, temp, 1024, Path);
                return temp.ToString().Trim();
            }
        }
        #endregion
    }
}
