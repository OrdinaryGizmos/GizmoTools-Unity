using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WebSoft;

namespace GizmoTools
{
    /// <summary>
    /// Tracks Fields and Properties that Change
    /// BUG: Will not track items in lists or dictionaries. Only actual values.
    /// </summary>
    public interface ITrackChanges
    {
        PropertyBag Properties { get; set; }
        Dictionary<string, object> OldValues { get; set; }

        /// <summary>
        /// Called After Changes are sent, but before they are reset.
        /// </summary>
        void OnChanges();
    }

    public static class TrackChangesExtensions
    {
        /// <summary>
        /// Initialize "OldValues" and register object to change tracker
        /// </summary>
        /// <param name="trackObject"></param>
        public static void StartTracking(this ITrackChanges trackObject)
        {

            if (trackObject.Properties == null)
                trackObject.Properties = new PropertyBag(trackObject);
            if (trackObject.OldValues == null)
                trackObject.OldValues = new Dictionary<string, object>();

            // We need to loop through all the GizmoSerialize attributes and populate out OldValues
            var _fields = trackObject.GetType().GetFields()
                .Where(fieldInfo => fieldInfo.IsDefined(typeof(GizmoSerializeAttribute), false));
            foreach (System.Reflection.FieldInfo field in _fields)
            {
                var Fieldname = field.Name;
                if (trackObject.Properties[Fieldname].Value != null)
                    trackObject.OldValues[Fieldname] = trackObject.Properties[Fieldname].Value;
            }

            var _properties = trackObject.GetType().GetProperties()
                .Where(propertyInfo => propertyInfo.IsDefined(typeof(GizmoSerializeAttribute), false));
            foreach (System.Reflection.PropertyInfo property in _properties)
            {
                var Fieldname = property.Name;
                if (trackObject.Properties[Fieldname].Value != null)
                    trackObject.OldValues[Fieldname] = trackObject.Properties[Fieldname].Value;
            }
            //Debug.Log("Value Count: " + trackObject.OldValues.Count);
            foreach (KeyValuePair<string, object> value in trackObject.OldValues)
            {
                // Debug.Log(value.Key + ": " + value.Value.GetType() + ": " + value.Value);
            }
        }

        /// <summary>
        /// Set all "OldValues" equal to "Properties"
        /// </summary>
        /// <param name="trackObject"></param>
        public static void ResetValues(this ITrackChanges trackObject)
        {
            trackObject.StartTracking();

        }

        /// <summary>
        /// Get a list of Field and Property names that have been modified
        /// </summary>
        /// <param name="trackObject"></param>
        /// <returns></returns>
        public static List<string> GetDirty(this ITrackChanges trackObject)
        {
            var dirtylist = new List<string>();
            if (trackObject.Properties == null)
                trackObject.StartTracking();

            foreach (var property in trackObject.Properties)
            {
                if (!trackObject.OldValues[property.Name].Equals(property.Value))
                {
                    //Debug.Log(property.Name + ": " + property.Value.GetType() + ": " + property.Value);
                    //Debug.Log(trackObject.OldValues[property.Name].GetType() + ": " + trackObject.OldValues[property.Name]);
                    dirtylist.Add(property.Name);
                }
            }
            return dirtylist;
        }


    }
}