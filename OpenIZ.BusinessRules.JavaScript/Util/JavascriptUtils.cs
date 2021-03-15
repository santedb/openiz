/*
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 2021-2-9
 */
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenIZ.Core.Applets.ViewModel.Json;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Json.Formatter;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenIZ.BusinessRules.JavaScript.Util
{

    /// <summary>
    /// Utility functions for javascript bridge
    /// </summary>
    public static class JavascriptUtils
    {

        // Date Regular Expression
        private static Regex date_regex = new Regex(@"(\d{4})-(\d{2})-(\d{2})");

        /// <summary>
        /// Produces a literal representation of the provided data
        /// </summary>
        public static String ProduceLiteral(object data)
        {
            StringBuilder sb = new StringBuilder();
            var dict = data as IDictionary<String, Object>;
            if (dict != null)
                foreach (var kv in dict)
                    sb.AppendFormat("{0}={{{1}}}", kv.Key, ProduceLiteral(kv.Value));
            else
                sb.Append(data?.ToString() ?? "null");
            return sb.ToString();
        }

        /// <summary>
        /// Expand the view model object to an identified object 
        /// </summary>
        public static IdentifiedData ToModel(Object data)
        {
            try
            {
                var dictData = data as IDictionary<String, object>;
                //if (dictData?.ContainsKey("$item") == true) // HACK: JInt does not like Item property on ExpandoObject
                //{
                //    dictData.Add("item", dictData["$item"]);
                //    dictData.Remove("$item");
                //}

                // Serialize to a view model serializer
                using (MemoryStream ms = new MemoryStream())
                {
                    JsonSerializer jsz = new JsonSerializer();
                    using (JsonWriter reader = new JsonTextWriter(new StreamWriter(ms, Encoding.UTF8, 2048, true)))
                        jsz.Serialize(reader, data);

                    // De-serialize
                    ms.Seek(0, SeekOrigin.Begin);

                    using (var szr = new JsonViewModelSerializer())
                    {
                        szr.LoadSerializerAssembly(typeof(SecurityApplicationViewModelSerializer).GetTypeInfo().Assembly);
                        return szr.DeSerialize<IdentifiedData>(ms);
                    }
                }
            }
            catch (Exception e)
            {
                throw new JsonSerializationException("Error serializing object", e);
            }
        }

        /// <summary>
        /// Simplifies an HDSI object
        /// </summary>
        public static ExpandoObject ToViewModel(IdentifiedData data)
        {
            try
            {
                // Serialize to a view model serializer
                using (MemoryStream ms = new MemoryStream())
                {
                    using (TextWriter tw = new StreamWriter(ms, Encoding.UTF8, 2048, true))
                    using (var szr = new JsonViewModelSerializer())
                    {
                        szr.LoadSerializerAssembly(typeof(SecurityApplicationViewModelSerializer).GetTypeInfo().Assembly);
                        szr.Serialize(tw, data);
                    }

                    ms.Seek(0, SeekOrigin.Begin);

                    // Parse
                    JsonSerializer jsz = new JsonSerializer() { DateFormatHandling = DateFormatHandling.IsoDateFormat, TypeNameHandling = TypeNameHandling.None };
                    using (JsonReader reader = new JsonTextReader(new StreamReader(ms)))
                    {
                        var retVal = jsz.Deserialize<Newtonsoft.Json.Linq.JObject>(reader);
                        return ConvertToJint(retVal);
                    }
                }
            }
            catch (Exception e)
            {
                throw new JsonSerializationException($"Error converting {data} to view model", e);
            }
        }

        /// <summary>
        /// Convert to Jint object
        /// </summary>
        public static ExpandoObject ConvertToJint(JObject source)
        {
            try
            {
                var retVal = new ExpandoObject();

                if (source == null)
                    return retVal;

                var expandoDic = (IDictionary<String, Object>)retVal;
                foreach (var kv in source)
                {
                    if (kv.Value is JObject)
                        expandoDic.Add(kv.Key, ConvertToJint(kv.Value as JObject));
                    else if (kv.Value is JArray)
                        expandoDic.Add(kv.Key, (kv.Value as JArray).Select(o => o is JValue ? (o as JValue).Value : ConvertToJint(o as JObject)).ToArray());
                    else
                    {
                        object jValue = (kv.Value as JValue).Value;
                        if (jValue is String && date_regex.IsMatch(jValue.ToString())) // Correct dates
                        {
                            var dValue = date_regex.Match(jValue.ToString());
                            expandoDic.Add(kv.Key, new DateTime(Int32.Parse(dValue.Groups[1].Value), Int32.Parse(dValue.Groups[2].Value), Int32.Parse(dValue.Groups[3].Value)));
                        }
                        else
                            expandoDic.Add(kv.Key, (kv.Value as JValue).Value);
                    }
                }
                return retVal;
            }
            catch (Exception e)
            {
                throw new JsonSerializationException("Error converting object to/from JINT", e);
            }
        }


    }
}
