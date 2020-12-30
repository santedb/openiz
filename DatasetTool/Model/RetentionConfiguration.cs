using ExpressionEvaluator;
using Newtonsoft.Json;
using OpenIZ.Core.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OizDevTool.Model
{
    /// <summary>
    /// Configuration Section for configuring the retention policies
    /// </summary>
    [XmlType(nameof(DataRetentionConfiguration), Namespace = "http://openiz.org/configuration")]
    [XmlRoot(nameof(DataRetentionConfiguration), Namespace = "http://openiz.org/configuration")]
    public class DataRetentionConfiguration 
    {

        /// <summary>
        /// Variables
        /// </summary>
        [XmlArray("vars"), XmlArrayItem("add"), JsonProperty("vars")]
        public List<RetentionVariableConfiguration> Variables { get; set; }

        /// <summary>
        /// Data retention rules
        /// </summary>
        [XmlArray("rules"), XmlArrayItem("add"), JsonProperty("rules")]
        public List<DataRetentionRuleConfiguration> RetentionRules { get; set; }

    }

    /// <summary>
    /// Retention variable
    /// </summary>
    [XmlType(nameof(RetentionVariableConfiguration), Namespace = "http://openiz.org/configuration")]
    public class RetentionVariableConfiguration
    {
        /// <summary>
        /// The name of the object
        /// </summary>
        [XmlAttribute("name"), JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Expression to set
        /// </summary>
        [XmlText, JsonProperty("expr")]
        public string Expression { get; set; }

        /// <summary>
        /// Get the specified delegate
        /// </summary>
        public Delegate CompileFunc(Dictionary<String, Delegate> variableFunc = null)
        {
            CompiledExpression<dynamic> exp = new CompiledExpression<dynamic>(this.Expression);
            exp.TypeRegistry = new TypeRegistry();
            exp.TypeRegistry.RegisterDefaultTypes();
            exp.TypeRegistry.RegisterType<Guid>();
            exp.TypeRegistry.RegisterType<TimeSpan>();
            exp.TypeRegistry.RegisterParameter("now", () => DateTime.Now); // because MONO is scumbag

            if(variableFunc != null)
                foreach (var fn in variableFunc)
                    exp.TypeRegistry.RegisterParameter(fn.Key, fn.Value);
            //exp.TypeRegistry.RegisterSymbol("data", expressionParm);
            //exp.ScopeCompile<TData>();
            //Func<TData, bool> d = exp.ScopeCompile<TData>();
            return exp.Compile();
        }
    }

    /// <summary>
    /// Identifies the action to take when the retained object is set
    /// </summary>
    [Flags]
    public enum DataRetentionActionType
    {
        /// <summary>
        /// The object should be purged (deleted from the database)
        /// </summary>
        [XmlEnum("purge")]
        Purge = 0x1,
        /// <summary>
        /// The object should be obsoleted in the persistence layer
        /// </summary>
        [XmlEnum("obsolete")]
        Obsolete = 0x2,
        /// <summary>
        /// The object should be archived using the IDataArchiveService
        /// </summary>
        [XmlEnum("archive")]
        Archive = 0x4
    }

    /// <summary>
    /// Retention rule configuration
    /// </summary>
    [XmlType(nameof(DataRetentionRuleConfiguration), Namespace = "http://openiz.org/configuration")]
    public class DataRetentionRuleConfiguration
    {

        /// <summary>
        /// Gets the name of the rule
        /// </summary>
        [XmlAttribute("name"), JsonProperty("name")]
        public String Name { get; set; }
        /// <summary>
        /// Gets or sets the resource type
        /// </summary>
        [XmlAttribute("type"), JsonProperty("type")]
        public String ResourceTypeXml { get; set; }

        /// <summary>
        /// Gets the resource
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public Type ResourceType => new ModelSerializationBinder().BindToType(null, this.ResourceTypeXml);

        /// <summary>
        /// Gets or sets the filter expressions the rule applies (i.e. objects matching this rule will be included)
        /// </summary>
        [XmlArray("includes"), XmlArrayItem("filter"), JsonProperty("includes")]
        public List<String> IncludeExpressions { get; set; }

        /// <summary>
        /// Gets or sets the objects which are excluded.
        /// </summary>
        [XmlArray("excludes"), XmlArrayItem("filter"), JsonProperty("excludes")]
        public List<String> ExcludeExpressions { get; set; }

        /// <summary>
        /// Dictates the action
        /// </summary>
        [XmlAttribute("action"), JsonProperty("action")]
        public DataRetentionActionType Action { get; set; }

    }
}
