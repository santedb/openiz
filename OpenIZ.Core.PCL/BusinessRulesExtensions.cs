using OpenIZ.Core.Interfaces;
using OpenIZ.Core.Model;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core
{
    /// <summary>
    /// Business rule extensions
    /// </summary>
    public static class BusinessRulesExtensions
    {

        /// <summary>
        /// Add a business rule service to this instance of me or the next instance
        /// </summary>
        public static object AddBusinessRule(this IServiceProvider me, Type instance)
        {
            var ibre = instance.GetTypeInfo().ImplementedInterfaces.Where((t) => t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(IBusinessRulesService<>)).FirstOrDefault();
            if (ibre == null)
                throw new InvalidOperationException($"{nameof(instance)} must implement IBusinessRulesService<T>");
            var meth = typeof(BusinessRulesExtensions).GetGenericMethod(nameof(AddBusinessRule), ibre.GenericTypeArguments, new Type[] { typeof(IServiceProvider), typeof(Type) });
            return meth.Invoke(null, new object[] { me, instance });
        }

        /// <summary>
        /// Add a business rule service to this instance of me or the next instance
        /// </summary>
        public static IBusinessRulesService GetBusinessRuleService(this IServiceProvider me, Type instanceType)
        {
            var ibt = typeof(IBusinessRulesService<>).MakeGenericType(instanceType);
            return ApplicationServiceContext.Current.GetService(ibt) as IBusinessRulesService;
        }

        /// <summary>
        /// Adds a new business rule service for the specified model to the application service otherwise adds it to the chain
        /// </summary>
        /// <typeparam name="TModel">The type of model to bind to</typeparam>
        /// <param name="me">The application service to be added to</param>
        /// <param name="breType">The instance of the BRE</param>
        public static object AddBusinessRule<TModel>(this IServiceProvider me, Type breType) where TModel : IdentifiedData
        {
            var cbre = me.GetService(typeof(IBusinessRulesService<TModel>)) as IBusinessRulesService;
            if (cbre == null)
            {
                (me.GetService(typeof(IServiceManager)) as IServiceManager).AddServiceProvider(breType);
                return me.GetService(breType);
            }
            else if (cbre.GetType() != breType) // Only add if different
            {
                while (cbre.Next != null)
                {
                    if (cbre.GetType() == breType) return breType; // duplicate
                    cbre = cbre.Next;
                }
                cbre.Next = Activator.CreateInstance(breType) as IBusinessRulesService;
                return cbre.Next;
            }
            else
                return cbre;

        }

    }

   
}
