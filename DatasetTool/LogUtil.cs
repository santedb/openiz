using LogViewer;
using MARC.Everest.Threading;
using MohawkCollege.Util.Console.Parameters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OizDevTool
{
    /// <summary>
    /// Log utility
    /// </summary>
    [Description("Tooling for interacting with logs files")]
    public static class LogUtil
    {

        /// <summary>
        /// Log to CSV parameters
        /// </summary>
        public class LogToCsvParameters
        {

            [Parameter("log")]
            public StringCollection Logs { get; set; }

            [Parameter("out")]
            public String Output { get; set; }

            [Parameter("geoip-block")]
            public String GeoIPBlocks { get; set; }

            [Parameter("geoip-locale")]
            public String GeoIPLocales { get; set; }

            [Parameter("geoip-skip")]
            public bool NoGeoIp { get; set; }

        }

        /// <summary>
        /// GEO IP structure
        /// </summary>
        public class GeoIpStruct
        {

            private UInt32 m_maskBits;

            public GeoIpStruct(String ipMask, String country)
            {
                var ipParts = ipMask.Split('/');
                this.Subnet = IPAddress.Parse(ipParts[0]);
                this.SubnetMask = Int32.Parse(ipParts[1]);
                this.m_maskBits = BitConverter.ToUInt32(this.Subnet.GetAddressBytes().Reverse().ToArray(), 0);
                var mask = uint.MaxValue << (32 - this.SubnetMask);
                this.m_maskBits = this.m_maskBits & mask;
                this.Country = country;
            }

            /// <summary>
            /// The subnet
            /// </summary>
            public IPAddress Subnet { get; set; }

            /// <summary>
            /// The netmask
            /// </summary>
            public int SubnetMask { get; set; }

            /// <summary>
            /// Country code
            /// </summary>
            public String Country { get; set; }

            public bool IsMasked(IPAddress other)
            {
                UInt32 ipaddr = BitConverter.ToUInt32(other.GetAddressBytes().Reverse().ToArray(), 0);
                var mask = uint.MaxValue << (32 - this.SubnetMask);
                return this.m_maskBits == (ipaddr & mask);
            }
        }

        public class RequestInfo
        {

            /// <summary>
            /// Create request info from match
            /// </summary>
            public RequestInfo(Match match)
            {
                this.RequestIp = IPAddress.Parse(match.Groups[5].Value);
                this.Method = match.Groups[6].Value;
                this.Url = new Uri(match.Groups[7].Value);
                this.UserAgent = match.Groups[8].Value;
                this.RequestId = Guid.Parse(match.Groups[9].Value);
                this.RequestDate = DateTime.Parse(match.Groups[1].Value);
                this.Thread = match.Groups[2].Value;
            }

            public Guid RequestId { get; set; }

            public IPAddress RequestIp { get; set; }

            public String Method { get; set; }

            public String Response { get; set; }

            public TimeSpan ProcessingTime { get; set; }

            public DateTime RequestDate { get; set; }

            public DateTime ResponseDate { get; set; }

            public Uri Url { get; set; }

            public String UserAgent { get; set; }

            public String Thread { get; set; }

            public GeoIpStruct GeoInfo { get; set; }
        }

        /// <summary>
        /// Imports the Core data
        /// </summary>
        [Description("Extracts a core log to CSV")]
        [ParameterClass(typeof(LogToCsvParameters))]
        [Example("Extract openiz_20190701.log to 20190701.csv", "--log=openiz_20190701.log --out=20190701.csv")]
        public static void CoreToCsv(string[] args)
        {

            LogToCsvParameters parms = new ParameterParser<LogToCsvParameters>().Parse(args);
           
            for (int i = 0; i < parms.Logs.Count; i++)
            {
                if (parms.Logs[i].Contains("*"))
                {
                    var filter = parms.Logs[i];
                    parms.Logs.AddRange(Directory.GetFiles(Path.GetDirectoryName(filter), Path.GetFileName(filter)));
                    parms.Logs.RemoveAt(i);
                }
            }

            List<String> sourceFiles = new List<string>(parms.Logs.Count);
            WaitThreadPool wtp = new WaitThreadPool();

            foreach (var filePath in parms.Logs)
            {

                wtp.QueueUserWorkItem((fileName) =>
                {
                    var fi = new FileInfo(fileName.ToString());
                    try
                    {
                        var ipPattern = new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\s-(.*)");
                        var tempFileName = Path.GetTempFileName();
                        Console.WriteLine("Processing {0} ({1:#,000} KB) to {2}", fi.Name, fi.Length / 1024, tempFileName);
                        using (var tw = File.CreateText(tempFileName))
                        using (var s = fi.OpenRead())
                        using (var sr = new StreamReader(s))
                            foreach (var itm in LogEvent.Load(sr).OfType<LogEvent>())
                            {
                                var message = itm.Message;
                                if (message.Contains("\r\n"))
                                    message = message.Substring(0, message.IndexOf("\r\n"));
                                if (message.Length > 200)
                                    message = message.Substring(0, 200);

                                var ipMatch = ipPattern.Match(message);
                                if (ipMatch.Success)
                                    message = ipMatch.Groups[1].Value;

                                tw.WriteLine($"{fi.Name},{itm.Sequence},{itm.Source},{itm.Thread},{itm.Date},{itm.Level},{message}");
                            }
                        Console.WriteLine("Completed processing of {0}", fi.Name);

                        sourceFiles.Add(tempFileName);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error processing {0} : {1}", fi.Name, e.Message);
                    }


                }, filePath);

            }

            Console.WriteLine("Waiting for processing to complete..");
            wtp.WaitOne();

            Console.WriteLine("Combining files...");
            try
            {
                using (var outs = File.Create(parms.Output ?? "openiz.csv"))
                {
                    var hdr = Encoding.UTF8.GetBytes("File,Sequence,Source,Thread,Date,Level,Message\r\n");
                    outs.Write(hdr, 0, hdr.Length);
                    foreach (var itm in sourceFiles)
                    {
                        Console.WriteLine("Including {0}", itm);
                        using (var ins = File.OpenRead(itm))
                            ins.CopyTo(outs);
                        File.Delete(itm);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Combining files: {0}", e.Message);
            }



        }
        /// <summary>
        /// Imports the Core data
        /// </summary>
        [Description("Extracts an HTTP log to CSV")]
        [ParameterClass(typeof(LogToCsvParameters))]
        [Example("Extract http_20190701.log to 20190701.csv", "--log=http_20190701.log --out=20190701.csv")]
        public static void HttpToCsv(string[] args)
        {

            LogToCsvParameters parms = new ParameterParser<LogToCsvParameters>().Parse(args);

            // Load geo refs
            List<GeoIpStruct> geoRefs = new List<GeoIpStruct>();
            if(!String.IsNullOrEmpty(parms.GeoIPBlocks))
            {
                using (var tr = File.OpenText(parms.GeoIPBlocks))
                {
                    tr.ReadLine();
                    while (!tr.EndOfStream)
                    {
                        var parts = tr.ReadLine().Split(',');
                        geoRefs.Add(new GeoIpStruct(parts[0], parts[1]));
                    }
                }
            }

            // Load geo names
            if(!String.IsNullOrEmpty(parms.GeoIPLocales))
            {
                Dictionary<String, String> trefs = new Dictionary<string, string>();
                using (var tr = File.OpenText(parms.GeoIPLocales))
                    while(!tr.EndOfStream)
                    {
                        var parts = tr.ReadLine().Split(',');
                        trefs.Add(parts[0], parts[4]);
                    }
                foreach (var itm in geoRefs)
                {
                    String cty = null;
                    if (trefs.TryGetValue(itm.Country, out cty))
                        itm.Country = cty;
                }
            }


            for(int i = 0; i < parms.Logs.Count; i++)
            {
                if(parms.Logs[i].Contains("*"))
                {
                    var filter = parms.Logs[i];
                    parms.Logs.AddRange(Directory.GetFiles(Path.GetDirectoryName(filter), Path.GetFileName(filter)));
                    parms.Logs.RemoveAt(i);
                }
            }

            WaitThreadPool wtp = new WaitThreadPool();
            object locker = new object();

            ConcurrentDictionary<Guid, RequestInfo> infoCache = new ConcurrentDictionary<Guid, RequestInfo>();
            ConcurrentDictionary<IPAddress, GeoIpStruct> geoCache = new ConcurrentDictionary<IPAddress, GeoIpStruct>();
            
            List<String> sourceFiles = new List<string>(parms.Logs.Count);

            Regex requestRe = new Regex(@"^([0-9\-\s\:APM\/]*?)\[@(\d*)\]?\s:\s(.*)\s(Information|Warning|Error|Fatal|Verbose):\s-?\d{1,10}?\s:\sHTTP RQO\s(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\s:\s([A-Z]+)\s(.*?)\s\((.*)\)\s-\s([a-z0-9-]{36}).*$"),
                responseRe = new Regex(@"^([0-9\-\s\:APM\/]*?)\[@(\d*)\]?\s:\s(.*)\s(Information|Warning|Error|Fatal|Verbose):\s-?\d{1,10}?\s:\sHTTP RSP\s([a-z0-9-]{36})\s:\s(\w+)\s\(([0-9\.]+)\sms");
            foreach (var filePath in parms.Logs)
            {

                wtp.QueueUserWorkItem((fileName) =>
                {
                    var fi = new FileInfo(fileName.ToString());
                    try
                    {
                        var tempFileName = Path.GetTempFileName();
                        Console.WriteLine("Loading {0} ({1:#,000} KB) to {2}", fi.Name, fi.Length / 1024, tempFileName);
                        int lineNo = 0;
                        using (var tw = File.CreateText(tempFileName))
                        using (var tr = fi.OpenText())
                            while (!tr.EndOfStream)
                            {

                                var line = tr.ReadLine();
                                lineNo++;
                                if (lineNo % 10000 == 0)
                                    Console.WriteLine("Processed {0} lines from {1}", lineNo, fi.Name);
                                var match = requestRe.Match(line);
                                if (match.Success)
                                {

                                    var requestInfo = new RequestInfo(match);
                                    if (!parms.NoGeoIp)
                                    {
                                        if (!geoCache.TryGetValue(requestInfo.RequestIp, out GeoIpStruct countryCode))
                                        {
                                            countryCode = geoRefs.FirstOrDefault(o => o.IsMasked(requestInfo.RequestIp));
                                            geoCache.TryAdd(requestInfo.RequestIp, countryCode);
                                        }
                                        requestInfo.GeoInfo = countryCode;
                                    }
                                    lock (locker)
                                        infoCache.TryAdd(requestInfo.RequestId, requestInfo);
                                }
                                else
                                {
                                    match = responseRe.Match(line);
                                    if (match.Success)
                                    {
                                        var uuid = Guid.Parse(match.Groups[5].Value);
                                        RequestInfo request = null;
                                        if (infoCache.TryGetValue(uuid, out request))
                                        {
                                            request.Response = match.Groups[6].Value;
                                            request.ResponseDate = DateTime.Parse(match.Groups[1].Value);
                                            request.ProcessingTime = new TimeSpan(0, 0, 0, 0, (int)Double.Parse(match.Groups[7].Value));

                                            // Append data
                                            tw.WriteLine($"{request.RequestId},{request.Thread},{request.RequestDate},{request.RequestIp},{request.UserAgent},{request.Method},{request.Url.Scheme}://{request.Url.Host}:{request.Url.Port},{request.Url.AbsolutePath}, {request.Url.Query},{request.GeoInfo?.Country},{request.Response},{request.ProcessingTime.TotalMilliseconds}");
                                            infoCache.TryRemove(uuid, out request);
                                        }
                                        else
                                            Console.WriteLine("WARNING: Cannot find request that goes with response {0}", uuid);
                                    }
                                }
                            }
                        Console.WriteLine("Completed processing of {0}", fi.Name);
                        sourceFiles.Add(tempFileName);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error Processing: {0}", e);
                    }


                }, filePath);

            }

            Console.WriteLine("Waiting for processing to complete..");
            wtp.WaitOne();

            Console.WriteLine("Combining files...");
            try
            {
                using (var outs = File.Create(parms.Output ?? "http.csv"))
                {
                    var hdr = Encoding.UTF8.GetBytes("ID,Thread,Date,IP,User Agent,Method,Host,Resource,Parms,Geo,Response,Time\r\n");
                    outs.Write(hdr, 0, hdr.Length);
                    foreach (var itm in sourceFiles)
                    {
                        Console.WriteLine("Including {0}", itm);
                        using (var ins = File.OpenRead(itm))
                            ins.CopyTo(outs);
                        File.Delete(itm);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Combining files: {0}", e.Message);
            }

        }
    }
}
