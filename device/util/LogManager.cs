using System;

using System.IO;

using log4net;
using log4net.Config;

namespace GodOfBeer.util
{
    public class LogManager
    {
        private static LogManager mThis;
        private ILog mLogger = null;

        public static LogManager GetInstance()
        {
            if(mThis == null)
            {
                mThis = new LogManager();
                mThis.Init();
            }

            return mThis;
        }

        public void Init()
        {
            string strDirPath = @"C:\GodOfBeerLog";
            DirectoryInfo dirInfo = new DirectoryInfo(strDirPath);
            if(dirInfo.Exists == false)
            {
                dirInfo.Create();
            }
            string appPath = AppDomain.CurrentDomain.BaseDirectory + @"LogConfig.xml";
            XmlConfigurator.Configure(new System.IO.FileInfo(appPath));
            mLogger = log4net.LogManager.GetLogger("RollingFile");
        }

        public ILog GetLogger()
        {
            return mLogger;
        }
    }
}
