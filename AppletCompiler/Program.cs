﻿using MohawkCollege.Util.Console.Parameters;
using OpenIZ.Core.Applets.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace AppletCompiler
{
    class Program
    {

        private static readonly XNamespace xs_openiz = "http://openiz.org/applet";

        /// <summary>
        /// The main program
        /// </summary>
        static void Main(string[] args)
        {

            ParameterParser<ConsoleParameters> parser = new ParameterParser<ConsoleParameters>();
            var parameters = parser.Parse(args);

            if (parameters.Help)
            {
                parser.WriteHelp(Console.Out);
                return;
            }

            // First is there a Manifest.xml?
            if (!Path.IsPathRooted(parameters.Source))
                parameters.Source = Path.Combine(Environment.CurrentDirectory, parameters.Source);

            Console.WriteLine("Processing {0}...", parameters.Source);
            String manifestFile = Path.Combine(parameters.Source, "Manifest.xml");
            if (!File.Exists(manifestFile))
                Console.WriteLine("Directory must have Manifest.xml");
            else
            {
                XmlSerializer xsz = new XmlSerializer(typeof(AppletManifest));
                XmlSerializer packXsz = new XmlSerializer(typeof(AppletPackage));
                using (var fs = File.OpenRead(manifestFile))
                {
                    AppletManifest mfst = xsz.Deserialize(fs) as AppletManifest;
                    mfst.Assets.AddRange(ProcessDirectory(parameters.Source, parameters.Source));
                    using (var ofs = File.Create(parameters.Output ?? "out.xml"))
                        xsz.Serialize(ofs, mfst);
                    using (var ofs = File.Create(Path.ChangeExtension(parameters.Output ?? "out.xml", ".pak.raw")))
                        packXsz.Serialize(ofs, mfst.CreatePackage());
                    using (var ofs = File.Create(Path.ChangeExtension(parameters.Output ?? "out.xml", ".pak.gz")))
                    using (var gzs = new GZipStream(ofs, CompressionMode.Compress))
                        packXsz.Serialize(gzs, mfst.CreatePackage());
                }

            }
        }

        /// <summary>
        /// Process the specified directory
        /// </summary>
        private static IEnumerable<AppletAsset> ProcessDirectory(string source, String path)
        {
            List<AppletAsset> retVal = new List<AppletAsset>();
            foreach(var itm in Directory.GetFiles(source))
            {
                if (Path.GetFileName(itm).ToLower() == "manifest.xml")
                    continue;
                else 
                    switch(Path.GetExtension(itm))
                    {
                        case ".html":
                        case ".htm":
                        case ".xhtml":
                            XElement xe = XElement.Load(itm);

                            // Now we have to iterate throuh and add the asset\
                            AppletAssetHtml htmlAsset = new AppletAssetHtml();
                            htmlAsset.Layout = ResolveName(xe.Attribute(xs_openiz + "layout")?.Value);
                            htmlAsset.Titles = new List<LocaleString>(xe.Descendants().OfType<XElement>().Where(o => o.Name == xs_openiz + "title").Select(o=> new LocaleString() { Language = o.Attribute("lang")?.Value, Value = o.Value }));
                            htmlAsset.Bundle = new List<string>(xe.Descendants().OfType<XElement>().Where(o => o.Name == xs_openiz + "bundle").Select(o=> ResolveName(o.Value)));
                            htmlAsset.Script = new List<string>(xe.Descendants().OfType<XElement>().Where(o => o.Name == xs_openiz + "script").Select(o=> ResolveName(o.Value)));
                            htmlAsset.Style = new List<string>(xe.Descendants().OfType<XElement>().Where(o => o.Name == xs_openiz + "style").Select(o => ResolveName(o.Value)));

                            var includes = xe.DescendantNodes().OfType<XComment>().Where(o => o?.Value?.Trim().StartsWith("#include virtual=\"") == true).ToList();
                            foreach (var inc in includes)
                            {
                                String assetName = inc.Value.Trim().Substring(18); // HACK: Should be a REGEX
                                if (assetName.EndsWith("\""))
                                    assetName = assetName.Substring(0, assetName.Length - 1);
                                if (assetName == "content")
                                    continue;
                                var includeAsset = ResolveName(assetName);
                                inc.AddAfterSelf(new XComment(String.Format("#include virtual=\"{0}\"", includeAsset)));
                                inc.Remove();

                            }

                            var xel = xe.Descendants().OfType<XElement>().Where(o => o.Name.Namespace == xs_openiz).ToList();
                            if(xel != null)
                                foreach (var x in xel)
                                    x.Remove();
                            htmlAsset.Html = xe;
                            retVal.Add(new AppletAsset()
                            {
                                Name = ResolveName(itm.Replace(path, "")),
                                MimeType = "text/html",
                                Content = htmlAsset
                            });
                            break;
                        case ".jpg":
                        case ".jpeg":
                            retVal.Add(new AppletAsset()
                            {
                                Name = ResolveName(itm.Replace(path, "")),
                                MimeType = "image/jpeg",
                                Content = File.ReadAllBytes(itm)
                            });
                            break;
                        case ".bmp":
                            retVal.Add(new AppletAsset()
                            {
                                Name = ResolveName(itm.Replace(path, "")),
                                MimeType = "image/bmp",
                                Content = File.ReadAllBytes(itm)
                            });
                            break;
                        case ".gif":
                            retVal.Add(new AppletAsset()
                            {
                                Name = ResolveName(itm.Replace(path, "")),
                                MimeType = "image/gif",
                                Content = File.ReadAllBytes(itm)
                            });
                            break;
                        case ".png":
                            retVal.Add(new AppletAsset()
                            {
                                Name = ResolveName(itm.Replace(path, "")),
                                MimeType = "image/png",
                                Content = File.ReadAllBytes(itm)
                            });
                            break;
                        case ".css":
                            retVal.Add(new AppletAsset()
                            {
                                Name = ResolveName(itm.Replace(path, "")),
                                MimeType = "text/css",
                                Content = File.ReadAllText(itm)
                            });
                            break;
                        case ".js":
                            retVal.Add(new AppletAsset()
                            {
                                Name = ResolveName(itm.Replace(path, "")),
                                MimeType = "text/javascript",
                                Content = File.ReadAllText(itm)
                            });
                            break;
                    }
            }

            // Process sub directories
            foreach (var dir in Directory.GetDirectories(source))
                retVal.AddRange(ProcessDirectory(dir, path));

            return retVal;
        }

        /// <summary>
        /// Resolve the specified applet name
        /// </summary>
        private static String ResolveName(string value)
        {
            return Path.GetFileNameWithoutExtension(value?.ToLower().Replace("/", "-").Replace("\\","-"));
        }
    }
}
