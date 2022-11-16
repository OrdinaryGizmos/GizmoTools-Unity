using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using UnityEngine;
using WebSoft;
using Action = System.Action;


namespace GizmoTools
{

    //<summary>
    //JSON seriable interface
    //</summary>
    public interface IJson
    {
        //If you want a field to be serialized, use the GizmoSerialize Attribute
        void OnDeserialize() { }
        void OnSerialize() { }
    }

    public static class GizmoSerializer
    {
        static ConditionalWeakTable<IJson, Fields> table;
        private static MemoryStream stream = new MemoryStream();
        private static BinaryWriter writer = new BinaryWriter(stream);
        private static BinaryReader reader = new BinaryReader(stream);

        private static MemoryStream _tempstream = new MemoryStream();
        private static BinaryWriter _tempwriter = new BinaryWriter(_tempstream);
        private static BinaryReader _tempreader = new BinaryReader(_tempstream);

        static GizmoSerializer()
        {
            table = new ConditionalWeakTable<IJson, Fields>();
        }
        private sealed class Fields // mixin's fields held in private nested class
        {
            internal string JSONString;
            internal string TypeName;
            internal bool _defined;
            private PropertyBag properties;
            public PropertyBag JSONFields
            {
                get
                {
                    return properties;
                }
            }
            public Fields(object parent)
            {
                //Debug.Log(pvar.HP);
                properties = new PropertyBag(parent);
            }
        }

        public static void CleanStream()
        {
            stream.SetLength(0);
        }
        public static void Write(byte[] data)
        {
            writer.Write(data);
        }
        public static void Write(int data)
        {
            writer.Write(data);
        }
        public static void Write(float data)
        {
            writer.Write(data);
        }
        public static void Write(uint data)
        {
            writer.Write(data);
        }
        public static void Write(UInt64 data)
        {
            writer.Write(data);
        }
        public static void Write(string data)
        {
            writer.Write(data);
        }
        public static void Write(bool data)
        {
            writer.Write(data);
        }
        public static float ReadSingle()
        {
            return reader.ReadSingle();
        }
        public static float ReadInt32()
        {
            return reader.ReadInt32();
        }
        public static float ReadInt()
        {
            return reader.ReadInt32();
        }
        public static byte[] Read(int length = 1)
        {
            return reader.ReadBytes(length);
        }

        public static byte[] GetStream()
        {
            return stream.ToArray();
        }
        public static string ReadString()
        {
            return reader.ReadString();
        }
        public static bool ReadBool()
        {
            return reader.ReadBoolean();
        }



        private static byte[] GetStashStream()
        {
            return _tempstream.ToArray();
        }
        private static string ReadStashString()
        {
            return _tempreader.ReadString();
        }
        private static void CleanStashStream()
        {
            _tempstream.SetLength(0);
        }
        private static void WriteStash(byte[] data)
        {
            _tempwriter.Write(data);
        }
        private static void WriteStash(string data)
        {
            _tempwriter.Write(data);
        }
        private static byte[] ReadStash(int length = 1)
        {
            return _tempreader.ReadBytes(length);
        }
        // public static byte[] BinarySerialize(this IJson map)
        // {
        //     map.OnSerialize();
        //     return map.BinaryData;
        // }
        // public static void BinaryDeserialize(this IJson map)
        // {
        //     map.OnDeserialize();
        // }
        // public static void BinaryDeserialize(this IJson map, byte[] data)
        // {
        //     CleanStream();
        //     Write(data);
        //     stream.Position = 0;
        //     map.BinaryData = data;
        //     map.OnDeserialize();
        // }

        // public static bool SanityCheck(this IJson map)
        // {
        //     if (map.BinaryData == null) return false;
        //     return map.BinaryData.SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0xFF, 0x00 });
        // }
        class WritablePropertiesOnlyResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
                return props.Where(p => p.Writable).ToList();
            }
        }
        public static string JSONSerialize(this IJson map)
        {
            //Sanity check to make sure we don't try to do a binary serialize. OnSerialize should check for this header
            //map.BinaryData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0xFF, 0x00 };
            map.OnSerialize();
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                StringEscapeHandling = StringEscapeHandling.Default,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new WritablePropertiesOnlyResolver(),
            };
            var table_map = table.GetValue(map, p => { return new Fields(map); });
            var Fields = table_map.JSONFields;
            if (!table_map._defined)
                map.JSONDefineFields();
            var obj = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
            table_map.TypeName = map.GetType().ToString();
            obj.Add("__objtype", table_map.TypeName);
            foreach (PropertyBag.Property Field in Fields)
            {
                IJson IJson = Field.Value as IJson;
                if (IJson != null)
                {                                                   //Recursively build the values
                    obj.Add(Field.Name, IJson);
                }
                else if (Field.Value is IList)                      //If it's a List of IJson interfaces, we need to process them independently.
                {
                    List<object> serialList = new List<object>();
                    bool _ismjson = false;
                    foreach (var item in (IList)Field.Value)
                    {
                        IJson ijson = item as IJson;
                        if (ijson != null)
                        {
                            serialList.Add(JObject.Parse(ijson.JSONSerialize()));
                            _ismjson = true;
                        }
                    }
                    if (!_ismjson)
                        obj.Add(Field.Name, Field.Value);
                    else
                        obj.Add(Field.Name, serialList);
                }
                else if (Field.Value is PropertyBag)
                {
                    Debug.Log("You cannot Serialize a PropertyBag due to Reflection");
                }
                else
                {
                    obj.Add(Field.Name, Field.Value);
                }

            }
            var _newobj = System.Text.RegularExpressions.Regex.Unescape(JsonConvert.SerializeObject(obj, settings));
            return _newobj;
        }

        public static void JSONDeserialize(this IJson map, string Json, GameObject root)
        {
            map.JSONDeserialize(Json, root.transform, () => { });
        }
        public static void JSONDeserialize(this IJson map, string Json)
        {
            var GO = map as MonoBehaviour;
            if (GO != null)
                map.JSONDeserialize(Json, GO.transform, () => { }); //pass current gameobject Transform as root
            else
                map.JSONDeserialize(Json, default(Transform), () => { }); //pass empty Transform as root
        }
        public static void JSONDeserialize(this IJson map, string Json, Action callback)
        {
            var GO = map as MonoBehaviour;
            if (GO != null)
                map.JSONDeserialize(Json, GO.transform, callback); //pass current gameobject Transform as root
            else
                map.JSONDeserialize(Json, default(Transform), callback); //pass empty Transform as root
        }

        public static void JSONDeserialize(this IJson map, string Json, Transform root, Action callback)
        {

            /*
            var table_map = table.GetValue(map, p => { return new Fields(map); });
            var fields = table_map.JSONFields;
            if (!table_map._defined)
                map.JSONDefineFields();
            */

            JsonConvert.PopulateObject(Json, map);

            /*
            IDictionary<string, object> json;
            json = JsonConvert.DeserializeObject<ExpandoObject>(Json);
            //table.GetOrCreateValue(map).JSONString = ""; //Clear the Stored value so it can only be used once.
            var gameObject = map as MonoBehaviour;
            var Job = new DeserializeJob()
            {
                map = map,
                json = json,
                fields = fields,
                root = root,
                callback = callback
            };

            Job.Execute();
            */
            map.OnDeserialize();
            callback?.Invoke();

        }

        public static IEnumerator JSONDeserializeAsync(this IJson map, string Json, bool async, Transform root, Action callback)
        {
            var GO = map as MonoBehaviour;
            if (GO == null) yield break;

            var table_map = table.GetValue(map, p => { return new Fields(map); });
            var fields = table_map.JSONFields;
            if (!table_map._defined)
                map.JSONDefineFields();
            IDictionary<string, object> json;
            json = JsonConvert.DeserializeObject<ExpandoObject>(Json);
            //table.GetOrCreateValue(map).JSONString = ""; //Clear the Stored value so it can only be used once.

            foreach (KeyValuePair<string, object> entry in json)
            {
                if (entry.Key == "__objtype") continue;
                //fields[entry.Key].Value
                //fields[entry.Key].Value = Conversions.Convert(entry.Value, fields[entry.Key].Value.GetType(), root, async);
                fields[entry.Key].Value = entry.Value;
                yield return new WaitForFixedUpdate();
            }
            map.OnDeserialize();
            callback?.Invoke();
        }
        public static void JSONDeserializeAsync(this IJson map, string Json, Transform root = default, Action callback = default(Action))
        {
            var GO = map as MonoBehaviour;
            if (GO == null) return;

            GO.StartCoroutine(map.JSONDeserializeAsync(Json, true, root, callback));
        }

        private struct DeserializeJob : IJob
        {
            public IJson map;
            public IDictionary<string, object> json;
            public PropertyBag fields;
            public Transform root;
            public Action callback;


            public void Execute()
            {
                foreach (KeyValuePair<string, object> entry in json)
                {
                    if (entry.Key != "__objtype")
                    {
                        //var _starttime = Time.realtimeSinceStartup;
                        //fields[entry.Key].Value = Conversions.Convert(entry.Value, fields[entry.Key].Value.GetType(), root);
                        var field_ijson = fields[entry.Key].Value as IJson;
                        if (field_ijson != null)
                        {
                            var table_map = table.GetValue(field_ijson, p => { return new Fields(field_ijson); });
                            var child_fields = table_map.JSONFields;
                            if (!table_map._defined)
                                field_ijson.JSONDefineFields();
                            var Job = new DeserializeJob()
                            {
                                map = field_ijson,
                                json = (ExpandoObject)entry.Value,
                                fields = child_fields,
                                root = root,
                                callback = callback
                            };

                            Job.Execute();
                        }
                        else if (entry.Value.GetType() == typeof(ExpandoObject))
                        {
                            Type propertyType = fields[entry.Key].propertyInfo?.PropertyType;
                            if (propertyType != null)
                            {
                                fields[entry.Key].Value = ((ExpandoObject)entry.Value).ToObject(propertyType);
                            }
                            else
                            {
                                Type fieldType = fields[entry.Key].fieldInfo?.FieldType;
                                if (fieldType != null)
                                {
                                    fields[entry.Key].Value = ((ExpandoObject)entry.Value).ToObject(fieldType);
                                }
                            }
                        }
                        else
                        {
                            fields[entry.Key].Value = entry.Value;
                        }
                        /*var _totaltime = Time.realtimeSinceStartup - _starttime; 
                        if(GameData._tracktimer.ContainsKey(fields[entry.Key].Value.GetType()))
                            GameData._tracktimer[fields[entry.Key].Value.GetType()] += _totaltime;
                        else
                            GameData._tracktimer[fields[entry.Key].Value.GetType()] = _totaltime; */
                    }
                }
            }
        }
        private static void JSONDefineFields(this IJson map)
        {
            /*
            This loops and finds all of the Properties and Fields that have the JsonProperty attribute
            */
            var table_map = table.GetValue(map, p => { return new Fields(map); });
            var fields = table_map.JSONFields;
            var properties = map.GetType().GetProperties()
              .Where(prop => prop.IsDefined(typeof(GizmoSerializeAttribute), false));
            foreach (System.Reflection.PropertyInfo prop in properties)
            {
                var Fieldname = prop.Name;
                if (fields[Fieldname].Value != null)
                    fields[Fieldname].Value = fields[Fieldname].Value;
                else
                {
                    //Debug.LogError("Found a null value when trying to define properties for object: " + fields[Fieldname].Owner + ", Property Name: " + Fieldname);
                    if (prop.PropertyType == typeof(String))
                        fields[Fieldname].Value = "";
                    else
                        fields[Fieldname].Value = Activator.CreateInstance(prop.PropertyType);
                }
            }

            var _fields = map.GetType().GetFields()
              .Where(prop => prop.IsDefined(typeof(GizmoSerializeAttribute), false));
            foreach (System.Reflection.FieldInfo field in _fields)
            {
                var Fieldname = field.Name;
                if (fields[Fieldname].Value != null)
                    fields[Fieldname].Value = fields[Fieldname].Value;
                else
                {
                    //Debug.LogError("Found a null value when trying to define fields for object: " + fields[Fieldname].Owner + ", Field Name: " + Fieldname);
                    if (field.FieldType == typeof(String))
                        fields[Fieldname].Value = "";
                    else
                        fields[Fieldname].Value = Activator.CreateInstance(field.FieldType);
                }
            }
            table_map._defined = true;
        }

        public static void Touch(this IJson map)
        {
            var table_map = table.GetValue(map, p => { return new Fields(map); });
            var Fields = table_map.JSONFields;
            if (!table_map._defined)
                map.JSONDefineFields();
        }

        public static void JSONAddToObject(this IJson map, GameObject gameObject, string newjson)
        {
            if (newjson == "") return;
            var json = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(newjson) as IDictionary<string, object>;
            if (json == null) return;
            foreach (KeyValuePair<string, object> entry in json)
            {
                if (entry.Key == "__objtype")
                {
                    var newtype = Type.GetType(entry.Value.ToString()); //This ensures that we use the most specific type (newtype : BASE) when deserializing
                    dynamic listitem = gameObject.AddComponent(newtype);
                    var behaviour = listitem as IJson;
                    behaviour.JSONDeserialize(newjson);
                    return;
                }
            }
        }

        /// <summary>
        /// Uses a list field names and gets the values represented there.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="_fields"></param>
        /// <returns></returns>
        public static string JSONSerializeFieldList(this IJson map, List<string> _fields)
        {
            map.OnSerialize();
            if (_fields.Count == 0)
                return "";
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                StringEscapeHandling = StringEscapeHandling.Default,
                NullValueHandling = NullValueHandling.Ignore,
            };
            var table_map = table.GetValue(map, p => { return new Fields(map); });
            var Fields = table_map.JSONFields;
            if (!table_map._defined)
                map.JSONDefineFields();
            var obj = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
            table_map.TypeName = map.GetType().ToString();
            obj.Add("__objtype", table_map.TypeName);
            foreach (string fieldname in _fields)
            {
                PropertyBag.Property field = Fields[fieldname];
                IJson IJson = field.Value as IJson;
                if (IJson != null)
                {                                                   //Recursively build the values
                                                                    //var _val = IJson.JSONSerialize();
                    obj.Add(field.Name, IJson);
                }
                else if (field.Value is IList)                      //If it's a List of MJSON interfaces, we need to process them independently.
                {
                    List<object> serialList = new List<object>();
                    bool _ismjson = false;
                    foreach (var item in (IList)field.Value)
                    {
                        IJson ijson = item as IJson;
                        if (ijson != null)
                        {
                            serialList.Add(JObject.Parse(ijson.JSONSerialize()));
                            _ismjson = true;
                        }
                    }
                    if (!_ismjson)
                        obj.Add(field.Name, field.Value);
                    else
                        obj.Add(field.Name, serialList);
                }
                else if (field.Value is PropertyBag)
                {
                    Debug.Log("You cannot Serialize a PropertyBag due to Reflection");
                }
                else
                {
                    obj.Add(field.Name, field.Value);
                }

            }
            var _newobj = System.Text.RegularExpressions.Regex.Unescape(JsonConvert.SerializeObject(obj, settings));
            return _newobj;
        }
        public static T Cast<T>(this IJson map, T type)
        {
            Debug.Log(type.ToString());
            return (T)map;
        }

        public static void ShelveStream()
        {
            CleanStashStream();
            WriteStash(GetStream());
            CleanStream();
        }

        public static void RestoreStream()
        {
            CleanStream();
            Write(GetStashStream());
            CleanStashStream();
        }
        public static byte[] ToBytes(this Vector2 vector2)
        {
            _tempstream.SetLength(0);
            _tempwriter.Write(vector2.x);
            _tempwriter.Write(vector2.y);
            return _tempstream.ToArray();
        }
        public static byte[] ToBytes(this Vector3 vector3)
        {
            _tempstream.SetLength(0);
            _tempwriter.Write(vector3.x);
            _tempwriter.Write(vector3.y);
            _tempwriter.Write(vector3.z);
            return _tempstream.ToArray();
        }

        public static void FromBytes(this Vector2 vector2, byte[] data)
        {
            _tempstream.SetLength(0);
            _tempwriter.Write(data);
            vector2.x = _tempreader.ReadSingle();
            vector2.y = _tempreader.ReadSingle();
        }
        public static void FromBytes(this Vector3 vector3, byte[] data)
        {
            _tempstream.SetLength(0);
            _tempwriter.Write(data);
            vector3.x = _tempreader.ReadSingle();
            vector3.y = _tempreader.ReadSingle();
            vector3.z = _tempreader.ReadSingle();
        }
    }
}
