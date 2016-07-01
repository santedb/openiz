using OpenIZ.Core.Diagnostics;
using System;
using System.IO;
using System.Reflection;

namespace OpenIZ.Core.Http
{
	/// <summary>
	/// Form element attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class FormElementAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OpenIZ.Core.Http.FormElementAttribute"/> class.
		/// </summary>
		/// <param name="name">Name.</param>
		public FormElementAttribute(String name)
		{
			this.Name = name;
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		public String Name
		{
			get;
			set;
		}
	}

	/// <summary>
	/// Form body serializer.
	/// </summary>
	public class FormBodySerializer : IBodySerializer
	{
		private Tracer m_tracer = Tracer.GetTracer(typeof(FormBodySerializer));

		#region IBodySerializer implementation

		/// <summary>
		/// Serialize the specified object
		/// </summary>
		public void Serialize(System.IO.Stream s, object o)
		{
			// Get runtime properties
			bool first = true;
			using (StreamWriter sw = new StreamWriter(s))
			{
				foreach (var pi in o.GetType().GetRuntimeProperties())
				{
					// Use XML Attribute
					FormElementAttribute fatt = pi.GetCustomAttribute<FormElementAttribute>();
					if (fatt == null)
						continue;

					// Write
					String value = pi.GetValue(o)?.ToString();
					if (String.IsNullOrEmpty(value))
						continue;

					if (!first)
						sw.Write("&");
					sw.Write("{0}={1}", fatt.Name, value);
					first = false;
				}
			}
		}

		/// <summary>
		/// De-serialize
		/// </summary>
		/// <returns>The serialize.</returns>
		/// <param name="s">S.</param>
		public object DeSerialize(System.IO.Stream s)
		{
			throw new NotImplementedException();
		}

		#endregion IBodySerializer implementation
	}
}