﻿using ExpressionEvaluator;
using OpenIZ.Core.Applets.ViewModel;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Acts;
using OpenIZ.Core.Model.Constants;
using OpenIZ.Core.Model.Map;
using OpenIZ.Core.Model.Query;
using OpenIZ.Core.Model.Reflection;
using OpenIZ.Core.Model.Roles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Serialization;

namespace OpenIZ.Protocol.Xml.Model
{
    /// <summary>
    /// Reperesents a then condition clause
    /// </summary>
    [XmlType(nameof(ProtocolThenClauseCollection), Namespace = "http://openiz.org/protocol")]
    public class ProtocolThenClauseCollection : BaseProtocolElement
    {

        /// <summary>
        /// Actions to be performed
        /// </summary>
        [XmlElement("action", Type = typeof(ProtocolDataAction))]
        public List<ProtocolDataAction> Action { get; set; }

        /// <summary>
        /// Evaluate the actions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Act> Evaluate(Patient p)
        {
            List<Act> retVal = new List<Act>();

            foreach (var itm in this.Action)
            {
                Act act = null;
                if (itm.Element is String) // JSON
                    act = JsonViewModelSerializer.DeSerialize<Act>(itm.Element as String);
                else
                    act = itm.Element as Act;

                // Now do the actions to the properties as stated
                foreach (var instr in itm.Do)
                {
                    instr.Evaluate(act, p);
                }

                // Assign this patient as the record target
                act.Key = act.Key ?? Guid.NewGuid();
                act.Participations.Add(new ActParticipation(ActParticipationKey.RecordTarget, p.Key) { ParticipationRole = new Core.Model.DataTypes.Concept() { Key = ActParticipationKey.RecordTarget, Mnemonic = "RecordTarget" } });
                // Add record target to the source for forward rules
                p.Participations.Add(new ActParticipation(ActParticipationKey.RecordTarget, p) { SourceEntity = act, ParticipationRole = new Core.Model.DataTypes.Concept() { Key = ActParticipationKey.RecordTarget, Mnemonic = "RecordTarget" } });
                act.CreationTime = DateTime.Now;
                // The act to the return value
                retVal.Add(act);
            }

            return retVal;
        }
    }

    /// <summary>
    /// Asset data action base
    /// </summary>
    [XmlType(nameof(ProtocolDataAction), Namespace = "http://openiz.org/protocol")]
    public class ProtocolDataAction
    {

        /// <summary>
        /// Gets the elements to be performed
        /// </summary>
        [XmlElement("Act", typeof(Act), Namespace = "http://openiz.org/model")]
        [XmlElement("TextObservation", typeof(TextObservation), Namespace = "http://openiz.org/model")]
        [XmlElement("SubstanceAdministration", typeof(SubstanceAdministration), Namespace = "http://openiz.org/model")]
        [XmlElement("QuantityObservation", typeof(QuantityObservation), Namespace = "http://openiz.org/model")]
        [XmlElement("CodedObseration", typeof(CodedObservation), Namespace = "http://openiz.org/model")]
        [XmlElement("PatientEncounter", typeof(PatientEncounter), Namespace = "http://openiz.org/model")]
        [XmlElement("jsonModel", typeof(String))]
        public Object Element { get; set; }

        /// <summary>
        /// Associate the specified data for stuff that cannot be serialized
        /// </summary>
        [XmlElement("assign", typeof(PropertyAssignAction))]
        [XmlElement("add", typeof(PropertyAddAction))]
        public List<PropertyAction> Do { get; set; }
    }

    /// <summary>
    /// Associate data
    /// </summary>
    [XmlType(nameof(PropertyAction), Namespace = "http://openiz.org/protocol")]
    public abstract class PropertyAction : ProtocolDataAction
    {
        /// <summary>
        /// Action name
        /// </summary>
        public abstract String ActionName { get; }

        /// <summary>
        /// The name of the property
        /// </summary>
        [XmlAttribute("propertyName")]
        public String PropertyName { get; set; }

        /// <summary>
        /// Evaluate the expression
        /// </summary>
        /// <returns></returns>
        public abstract object Evaluate(Act act, Patient recordTarget);
    }

    /// <summary>
    /// Property assign value
    /// </summary>
    [XmlType(nameof(PropertyAssignAction), Namespace = "http://openiz.org/protocol")]
    public class PropertyAssignAction : PropertyAction
    {

        /// <summary>
        /// Action name
        /// </summary>
        public override string ActionName
        {
            get
            {
                return "SetValue";
            }
        }

        /// <summary>
        /// Selection of scope
        /// </summary>
        [XmlAttribute("scope")]
        public String ScopeSelector { get; set; }

        /// <summary>
        /// Where filter for scope
        /// </summary>
        [XmlAttribute("where")]
        public String WhereFilter { get; set; }

        /// <summary>
        /// Value expression
        /// </summary>
        [XmlText()]
        public String ValueExpression { get; set; }

        /// <summary>
        /// Evaluate the specified action on the object
        /// </summary>
        public override object Evaluate(Act act, Patient recordTarget)
        {
            var propertyInfo = act.GetType().GetRuntimeProperty(this.PropertyName);

            if (this.Element != null)
                propertyInfo.SetValue(act, this.Element);
            else
            {
                CompiledExpression exp = new CompiledExpression(this.ValueExpression);
                exp.TypeRegistry = new TypeRegistry();
                exp.TypeRegistry.RegisterDefaultTypes();
                exp.TypeRegistry.RegisterType<Guid>();
                exp.TypeRegistry.RegisterType<TimeSpan>();
                exp.TypeRegistry.RegisterSymbol("now", DateTime.Now); // because MONO is scumbag

                Object setValue = null;

                // Scope
                IEnumerable<Act> tScope = null;
                if (!String.IsNullOrEmpty(this.ScopeSelector))
                {
                    var scopeProperty = recordTarget.GetType().GetRuntimeProperty(this.ScopeSelector);
                    var scopeValue = scopeProperty.GetValue(recordTarget);

                    if (scopeProperty == null) return null; // no scope

                    // Where clause?
                    object scope = scopeValue;
                    if(!String.IsNullOrEmpty(this.WhereFilter))
                    {
                        var itemType = scopeProperty.PropertyType.GetTypeInfo().GenericTypeArguments[0];
                        var predicateType = typeof(Func<,>).MakeGenericType(new Type[] { itemType, typeof(bool) });
                        var builderMethod = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { itemType }, new Type[] { typeof(NameValueCollection) });
                        var linq = builderMethod.Invoke(null, new Object[] { NameValueCollection.ParseQueryString(this.WhereFilter) });
                        // Call where clause
                        builderMethod = typeof(Expression).GetGenericMethod(nameof(Expression.Lambda), new Type[] { predicateType }, new Type[] { typeof(Expression), typeof(ParameterExpression[]) });
                        var firstMethod = typeof(Enumerable).GetGenericMethod("FirstOrDefault",
                               new Type[] { itemType },
                               new Type[] { scopeProperty.PropertyType, predicateType });

                        scope = firstMethod.Invoke(null, new Object[] { scopeValue, (linq as LambdaExpression).Compile() });

                    }
                    exp.TypeRegistry.RegisterType(scope.GetType().Name, scope.GetType());

                    var compileMethod = typeof(CompiledExpression).GetGenericMethod(nameof(CompiledExpression.ScopeCompile), new Type[] { scope.GetType() }, new Type[] { });
                    setValue = (compileMethod.Invoke(exp, null) as Delegate).DynamicInvoke(scope);
                }
                else
                    setValue = exp.ScopeCompile<Patient>().Invoke(recordTarget);

                //exp.TypeRegistry.RegisterSymbol("data", expressionParm);
                if(Core.Model.Map.MapUtil.TryConvert(setValue, propertyInfo.PropertyType, out setValue))
                    propertyInfo.SetValue(act, setValue);
            }

            return propertyInfo.GetValue(act);
        }

    }

    /// <summary>
    /// Add something to a property collection
    /// </summary>
    [XmlType(nameof(PropertyAddAction), Namespace = "http://openiz.org/protocol")]
    public class PropertyAddAction : PropertyAction
    {

        /// <summary>
        /// Evaluate
        /// </summary>
        public override object Evaluate(Act act, Patient recordTarget)
        {
            var value = act.GetType().GetRuntimeProperty(this.PropertyName) as IList;
            value?.Add(this.Element);
            return value;
        }

        /// <summary>
        /// Add action
        /// </summary>
        public override string ActionName
        {
            get
            {
                return "Add";
            }
        }
    }

}