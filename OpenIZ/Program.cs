﻿using MARC.HI.EHRS.SVC.Core;
using MohawkCollege.Util.Console.Parameters;
using OpenIZ.Core.Model.EntityLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ
{
    /// <summary>
    /// Guid for the service
    /// </summary>
    [Guid("21F35B18-E417-4F8E-B9C7-73E98B7C71B8")]
    static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(String[] args)
        {

            // Parser
            ParameterParser<ConsoleParameters> parser = new ParameterParser<ConsoleParameters>();

            // Trace copyright information
            Assembly entryAsm = Assembly.GetEntryAssembly();

            bool hasConsole = true;

            // Dump some info
            Trace.TraceInformation("OpenIZ Startup : v{0}", entryAsm.GetName().Version);
            Trace.TraceInformation("OpenIZ Working Directory : {0}", entryAsm.Location);
            Trace.TraceInformation("Operating System: {0} {1}", Environment.OSVersion.Platform, Environment.OSVersion.VersionString);
            Trace.TraceInformation("CLI Version: {0}", Environment.Version);

            try
            {
                var parameters = parser.Parse(args);
                EntitySource.Current = new EntitySource(new PersistenceServiceEntitySource());

                // What to do?
                if (parameters.ShowHelp)
                    parser.WriteHelp(Console.Out);
                else if(parameters.ConsoleMode)
                {
                    Console.WriteLine("Open Immunize (OpenIZ) {0} ({1})", entryAsm.GetName().Version, entryAsm.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
                    Console.WriteLine("{0}", entryAsm.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);
                    Console.WriteLine("Complete Copyright information available at http://openiz.codeplex.com/wikipage?title=Contributions");
                    ServiceUtil.Start(typeof(Program).GUID);
                    Console.WriteLine("Press [ENTER] to stop...");
                    Console.ReadLine();
                    ServiceUtil.Stop();
                }
                else
                {
                    hasConsole = false;
                    ServiceBase[] servicesToRun = new ServiceBase[] { new OpenIZ() };
                    ServiceBase.Run(servicesToRun);
                }
            }
            catch(Exception e)
            {
#if DEBUG
                Trace.TraceError(e.ToString());
                if (hasConsole)
                    Console.WriteLine(e.ToString());
#else
                Trace.TraceError("Error encountered: {0}. Will terminate", e.Message);
#endif
            }

        }
    }
}
