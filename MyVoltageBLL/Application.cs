using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MyVoltageBLL
{
    public class Application
    {
        public static MyVoltageBLLConfig MyVoltageBLLConfig { get; set; }
        public static void Init()
        {
            XmlSerializer xConfig = new XmlSerializer(typeof(MyVoltageBLLConfig));

            string dllPath = System.IO.Path.GetDirectoryName(Assembly.GetAssembly(typeof(MyVoltageBLL.Application)).CodeBase).Remove(0, 6);
            if (dllPath[1] != ':') dllPath = "\\\\" + dllPath;
            dllPath = dllPath + @"\Configs";
            string configFileName = dllPath + @"\MyVoltageBLLConfig.xml";
            MyVoltageBLLConfig = (MyVoltageBLLConfig)xConfig.Deserialize(System.IO.File.OpenRead(configFileName));

        }
    }
}

