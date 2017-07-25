﻿using MohawkCollege.Util.Console.Parameters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.AdminConsole.Parameters
{
    /// <summary>
    /// Console parameters
    /// </summary>
    public class ConsoleParameters
    {

        /// <summary>
        /// Console parameters
        /// </summary>
        public ConsoleParameters()
        {
            this.RealmId = "localhost";
            this.UseTls = false;
            this.AppId = "org.openiz.oizac";
            this.AppSecret = "oizac-secret";
            this.Port = "8080";
            this.User = "administrator";
        }

        /// <summary>
        /// Realm identifier
        /// </summary>
        [Parameter("realm")]
        [Parameter("r")]
        [Description("Sets the realm to administer (default: localhost)")]
        public String RealmId { get; set; }

        /// <summary>
        /// Application identifier
        /// </summary>
        [Parameter("appId")]
        [Parameter("a")]
        [Description("Sets the application identifier (default: org.openiz.oizac)")]
        public String AppId { get; set; }

        /// <summary>
        /// Application secret
        /// </summary>
        [Parameter("secret")]
        [Parameter("s")]
        [Description("Sets the application secret")]
        public String AppSecret { get; set; }

        /// <summary>
        /// Sets the port for the ims
        /// </summary>
        [Parameter("port")]
        [Description("Sets the IMS port number (default: 8080 non-tls, 8443 tls)")]
        public String Port { get; set; }

        /// <summary>
        /// Use TLS setting
        /// </summary>
        [Parameter("tls")]
        [Parameter("t")]
        [Description("When true execute with TLS (default: false)")]
        public bool UseTls { get; set; }

        /// <summary>
        /// User setting
        /// </summary>
        [Parameter("user")]
        [Parameter("u")]
        [Description("Log into the IMS with the specified user (default: administrator)")]
        public string User { get; set; }

        /// <summary>
        /// Set password
        /// </summary>
        [Parameter("password")]
        [Parameter("p")]
        [Description("Set the password")]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the verbosity level
        /// </summary>
        [Description("Set the verbosity of output (Verbose, Error, Warning) (default: Error)")]
        [Parameter("v")]
        [Parameter("verbose")]
        public string Verbosity { get; set; }

        /// <summary>
        /// Gets or sets the proxy
        /// </summary>
        [Parameter("x")]
        [Parameter("proxy")]
        [Description("Sets the HTTP proxy address")]
        public string Proxy { get; set; }

        /// <summary>
        /// Show help and exit
        /// </summary>
        [Parameter("help")]
        [Parameter("?")]
        [Description("Show help and exit")]
        public bool Help { get; internal set; }
    }
}
