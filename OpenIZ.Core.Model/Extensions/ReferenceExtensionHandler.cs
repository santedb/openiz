using OpenIZ.Core.Interfaces;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Acts;
using OpenIZ.Core.Model.DataTypes;
using OpenIZ.Core.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Extensions
{
    /// <summary>
    /// Reference types
    /// </summary>
    public enum ReferenceType : byte
    {
        /// <summary>
        /// The reference is to an entity
        /// </summary>
        Entity = 0x0,
        /// <summary>
        /// The reference is to an act
        /// </summary>
        Act = 0x1,
        /// <summary>
        /// The reference is to a concept
        /// </summary>
        Concept = 0x2

    }
    /// <summary>
    /// Represents an extension handler which stores references to another type
    /// </summary>
    public class ReferenceExtensionHandler : IExtensionHandler
    {
        /// <summary>
        /// Gets the name of the extension handler
        /// </summary>
        public string Name => "Reference";

        /// <summary>
        /// De-serialize the object
        /// </summary>
        public object DeSerialize(byte[] extensionData)
        {
            if (extensionData.Length != 0x11)
                throw new InvalidOperationException($"Extension data must be {0x11} bytes long");
            Guid uuid = new Guid(extensionData.Skip(1).ToArray());
            switch ((ReferenceType)extensionData[0])
            {
                case ReferenceType.Act:
                    return Model.EntityLoader.EntitySource.Current.Get<Act>(uuid);
                case ReferenceType.Entity:
                    return Model.EntityLoader.EntitySource.Current.Get<Entity>(uuid);
                case ReferenceType.Concept:
                    return Model.EntityLoader.EntitySource.Current.Get<Concept>(uuid);
                default:
                    throw new InvalidOperationException($"Control type {extensionData[0]} is unknown");
            }

        }

        /// <summary>
        /// Get the display data
        /// </summary>
        public string GetDisplay(object data)
        {
            return data?.ToString();
        }

        /// <summary>
        /// Serialize the object
        /// </summary>
        public byte[] Serialize(object data)
        {
            byte[] retVal = new byte[0x11];
            if (data is Entity)
                retVal[0] = (byte)ReferenceType.Entity;
            else if (data is Act)
                retVal[0] = (byte)ReferenceType.Act;
            else if (data is Concept)
                retVal[0] = (byte)ReferenceType.Concept;
            else
                throw new ArgumentOutOfRangeException("Only Entity, Act, or Concept can be stored in this extension type");

            Array.Copy((data as IdentifiedData).Key.Value.ToByteArray(), 0, retVal, 1, 0x10);
            return retVal;
        }
    }
}
