﻿using OpenIZ.AdminConsole.Attributes;
using OpenIZ.Messaging.AMI.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Represents the server information commandlet
    /// </summary>
    [AdminCommandlet]
    public static class ServerInfoCmdlet
    {

        // Ami client
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(Core.Interop.ServiceEndpointType.AdministrationIntegrationService));

        /// <summary>
        /// Get server information
        /// </summary>
        public static void Init()
        {
            try
            {
                var diagReport = m_client.GetServerDiagnoticReport().ApplicationInfo;
                Console.WriteLine("* {0} -> v.{1} ({2})", m_client.Client.Description.Endpoint[0].Address, diagReport.Version, diagReport.InformationalVersion);
            }
            catch { }
        }


        /// <summary>
        /// Get diagnostic info from server
        /// </summary>
        [AdminCommand("sinfo", "Gets diagnostic information from the server")]
        public static void ServerVersionQuery()
        {
            var diagReport = m_client.GetServerDiagnoticReport().ApplicationInfo;

            Console.WriteLine("Diagnostic Report for {0}", m_client.Client.Description.Endpoint[0].Address);
            Console.WriteLine("Server Reports As:\r\n {0} v. {2} ({3}) \r\n {4}", diagReport.Name, diagReport.Product, diagReport.Version, diagReport.InformationalVersion, diagReport.Copyright);
        }

        /// <summary>
        /// Get assembly info from server
        /// </summary>
        [AdminCommand("sasm", "Shows the server assembly information")]
        public static void ServerAssemblyQuery()
        {
            var diagReport = m_client.GetServerDiagnoticReport().ApplicationInfo;

            // Loaded assemblies
            Console.WriteLine("Assemblies:\r\nAssembly{0}Version    Information", new String(' ', 22));
            foreach(var itm in diagReport.Assemblies)
            {
                if (itm.Name == "Microsoft.GeneratedCode") continue;
                Console.WriteLine("{0}{1}{2}{3}{4}",
                    itm.Name.Length > 28 ? itm.Name.Substring(0, 28) : itm.Name,
                    itm.Name.Length > 28 ? "  " : new string(' ', 30 - itm.Name.Length),
                    itm.Version.Length > 10 ? itm.Version.Substring(0, 10) : itm.Version,
                    itm.Version.Length > 10 ? " " : new string(' ', 11 - itm.Version.Length),
                    itm.Info?.Length > 50 ? itm.Info?.Substring(0, 50) : itm.Info);
            }
        }

        /// <summary>
        /// Get assembly info from server
        /// </summary>
        [AdminCommand("svci", "Shows the server service information")]
        public static void ServiceInformation()
        {
            var diagReport = m_client.GetServerDiagnoticReport().ApplicationInfo;

            // Loaded assemblies
            Console.WriteLine("Services:\r\nService{0}Status", new String(' ', 35));
            foreach (var itm in diagReport.ServiceInfo)
            {
                string name = itm.Description ?? itm.Type;
                Console.WriteLine("{0}{1}{2}",
                    name.Length > 37 ? name.Substring(0, 37) + "..." : name,
                    name.Length > 37 ? "  " : new string(' ', 37 - name.Length),
                    itm.IsRunning ? "Running" : "Stopped");
            }
        }

    }
}
