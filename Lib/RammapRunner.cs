using System.Diagnostics;
using System.ComponentModel;
using Extenders;
using System.IO;

namespace Rammap
{
    /// <summary>Rammap运行模式</summary>
    public enum RammapMode
    {
        /// <summary>清空工作集</summary>
        [Description("-Ew")]
        EmptyWorkingSets,
        /// <summary>清空系统工作集</summary>
        [Description("-Es")]
        EmptySystemWorkingSets,
        /// <summary>清空修改后的页面列表</summary>
        [Description("-Em")]
        EmptyModifiedPageList,
        /// <summary>清空备用列表</summary>
        [Description("-Et")]
        EmptyStandbyList,
        /// <summary>清空优先级为0的备用列表</summary>
        [Description("-E0")]
        EmptyPrioity0StandbyList,
    }
    /// <summary>调用Rammap</summary>
    static public class RammapRunner
    {
        /// <summary>运行Rammap</summary>
        /// <param name="mode">运行模式</param>
        /// <returns>成功运行返回True否则返回False</returns>
        static public bool Run(RammapMode mode)
        {
            if (!File.Exists(@"RAMMap.exe"))
                return false;
            ProcessStartInfo info = new();
            info.FileName = @"powershell.exe";
            info.Arguments = @$".\RAMMap.exe {mode.GetDescription()}";
            info.CreateNoWindow = true;
            if (Process.Start(info) is Process p)
            {
                p.WaitForExit();
                p.Close();
                return true;
            }
            return false;
        }
    }
}
