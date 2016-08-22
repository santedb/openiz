﻿/*
 * Copyright 2016-2016 Mohawk College of Applied Arts and Technology
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
 * Date: 2016-1-27
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Globalization;
using OpenIZ.Core.Model.Attributes;
using System.Collections;
using System.IO;
using OpenIZ.Core.Model.DataTypes;
using MARC.HI.EHRS.SVC.Messaging.FHIR;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Query;
using MARC.HI.EHRS.SVC.Messaging.FHIR.Resources;

namespace OpenIZ.Messaging.FHIR.Util
{

    /// <summary>
    /// OpenIZ FHIR query
    /// </summary>
    public class OpenIzFhirQuery<TModel> : FhirQuery
    {

        /// <summary>
        /// Query expression
        /// </summary>
        public LambdaExpression QueryExpression { get; set; }

        /// <summary>
        /// Gets or sets the composed query
        /// </summary>
        public Expression<Func<TModel, bool>> ToPredicate()
        {
            return Expression.Lambda<Func<TModel, bool>>(this.QueryExpression.Body, this.QueryExpression.Parameters);
        }
    }

    /// <summary>
    /// A class which is responsible for translating a series of Query Parmaeters to a LINQ expression
    /// to be passed to the persistence layer
    /// </summary>
    public class QueryRewriter
    {
        private static TraceSource s_tracer = new TraceSource("OpenIZ.Messaging.FHIR");

        // The query parameter map
        private static QueryParameterMap s_map;
        // Default
        private static QueryParameterType s_default;

        /// <summary>
        /// Static CTOR
        /// </summary>
        static QueryRewriter()
        {
            using (Stream s = typeof(QueryRewriter).Assembly.GetManifestResourceStream("OpenIZ.Messaging.FHIR.ParameterMap.xml"))
            {
                XmlSerializer xsz = new XmlSerializer(typeof(QueryParameterMap));
                s_map = xsz.Deserialize(s) as QueryParameterMap;
                s_default = s_map.Map.FirstOrDefault(o => o.SourceType == typeof(ResourceBase));
            }
        }

        
        /// <summary>
        /// Re-writes the FHIR query parameter to IMSI query parameter format
        /// </summary>
        /// <returns></returns>
        public static FhirQuery RewriteFhirQuery<TFhirResource, TModelType>(System.Collections.Specialized.NameValueCollection fhirQuery, out NameValueCollection imsiQuery)
        {
            // Try parse
            if (fhirQuery == null) throw new ArgumentNullException(nameof(fhirQuery));

            // Count and offset parameters
            int count = 0, offset = 0;
            if (!Int32.TryParse(fhirQuery["_count"] ?? "100", out count))
                throw new ArgumentException("_count");
            if (!Int32.TryParse(fhirQuery["_offset"] ?? "0", out offset))
                throw new ArgumentException("_offset");

            // Return new query
            FhirQuery retVal = new FhirQuery()
            {
                ActualParameters = new System.Collections.Specialized.NameValueCollection(),
                Quantity = count,
                Start = offset,
                MinimumDegreeMatch = 100,
                QueryId = Guid.NewGuid(),
                IncludeHistory = false,
                IncludeContained = false
            };

            imsiQuery = new NameValueCollection();

            var map = s_map.Map.FirstOrDefault(o => o.SourceType == typeof(TFhirResource));

            foreach (var kv in fhirQuery.AllKeys)
            {

                List<String> value = new List<string>(fhirQuery.GetValues(kv).Length);
                var parmMap = map?.Map.FirstOrDefault(o => o.FhirName == kv);
                if (parmMap == null)
                    parmMap = s_default.Map.FirstOrDefault(o => o.FhirName == kv);
                if(parmMap == null) continue;

                foreach (var v in fhirQuery.GetValues(kv))
                {
                    if (String.IsNullOrEmpty(v)) continue;

                    // Operands
                    bool chop = false;
                    string opValue = String.Empty;
                    if (v.Length > 2)
                        switch (v.Substring(0, 2))
                        {
                            case "gt":
                                chop = true;
                                opValue = ">";
                                break;
                            case "ge":
                                chop = true;
                                opValue = ">=";
                                break;
                            case "lt":
                                chop = true;
                                opValue = "<";
                                break;
                            case "le":
                                chop = true;
                                opValue = "<=";
                                break;
                            case "ne":
                                chop = true;
                                opValue = "!";
                                break;
                            case "eq":
                                chop = true;
                                break;
                            default:
                                break;
                        }
                    retVal.ActualParameters.Add(kv, v);
                    value.Add(opValue + v.Substring(chop ? 2 : 0));
                }

                if (value.Count(o => !String.IsNullOrEmpty(o)) == 0)
                    continue;

                // Query 
                switch (parmMap.FhirType)
                {
                    case "identifier":
                        foreach (var itm in value)
                        {
                            if (itm.Contains("|"))
                            {
                                var segs = itm.Split('|');
                                imsiQuery.Add(String.Format("{0}[{1}].value", parmMap.ModelName, segs[0]), segs[1]);
                            }
                            else
                                imsiQuery.Add(parmMap.ModelName + ".value", itm);
                        }
                        break;
                    case "concept":
                        foreach (var itm in value)
                        {
                            if (itm.Contains("|"))
                            {
                                var segs = itm.Split('|');
                                imsiQuery.Add(String.Format("{0}.referenceTerm[{1}].term.mnemonic", parmMap.ModelName, segs[0]), segs[1]);
                            }
                            else
                                imsiQuery.Add(parmMap.ModelName + ".referenceTerm.term.mnemonic", itm);
                        }
                        break;
                    case "tag":
                        foreach (var itm in value)
                        {
                            if (itm.Contains("|"))
                            {
                                var segs = itm.Split('|');
                                imsiQuery.Add(String.Format("{0}[{1}].value", parmMap.ModelName, segs[0]), segs[1]);
                            }
                            else
                                imsiQuery.Add(parmMap.ModelName, itm);
                        }
                        break;

                    default:
                        imsiQuery.Add(parmMap.ModelName, value);
                        break;
                }
            }

            return retVal;
        }
    }
}