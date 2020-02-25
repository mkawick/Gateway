using System;
using System.Collections.Generic;
using NDesk.Options;
//using System.CommandLine;
//using Mono.Options;

namespace CommonLibrary
{
    public static class Parser
    {
        public static int ApplicationId = 0;
        public static int FPS = 0;
        public static string ipAddr = "";

        public static void ParseCommandLine(string[] args)
        {
            OptionSet options = new OptionSet()
                .Add("id=|appid=|AppId=", a => ApplicationId = Convert.ToInt32(a))
                .Add("f=|fps=", f => FPS = Convert.ToInt32(f))
                .Add("ip=|ipaddr=|IpAddr=", ip => ipAddr = ip)
                .Add("?|h|help", h => DisplayHelp()); 
        }

        static void DisplayHelp()
        {
            Console.WriteLine("appid= sets the matching server and client ids to control packets");
            Console.WriteLine("fps= sets the max frame rate for updates from the server");
            Console.WriteLine("ipaddr= sets the target ipaddr");
            Console.WriteLine("help or h displays help");
        }
    }
}