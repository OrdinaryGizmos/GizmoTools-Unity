using System;
using System.Collections;
using System.IO;
using Unity.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Assertions.Must;
using WebSoft;
using Object = UnityEngine.Object;

namespace GizmoTools
{
    public static class GizmoTools
{

    public static string ByteToHexBitFiddle(byte[] bytes)
    {
        char[] c = new char[bytes.Length * 2];
        int b;
        for (int i = 0; i < bytes.Length; i++)
        {
            b = bytes[i] >> 4;
            c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
            b = bytes[i] & 0xF;
            c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
        }
        return new string(c);
    }

    public static byte[] HexBitStringToBytes(string hexString)
    {
        if ((hexString.Length & 1) != 0)
        {
            throw new ArgumentException("Input must have even number of characters");
        }
        int length = hexString.Length / 2;
        byte[] ret = new byte[length];
        for (int i = 0, j = 0; i < length; i++)
        {
            int high = ParseNybble(hexString[j++]);
            int low = ParseNybble(hexString[j++]);
            ret[i] = (byte)((high << 4) | low);
        }

        return ret;
    }

    private static int ParseNybble(char c)
    {
        // TODO: Benchmark using if statements instead
        switch (c)
        {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                return c - '0';
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f':
                return c - ('a' - 10);
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
                return c - ('A' - 10);
            default:
                throw new ArgumentException("Invalid nybble: " + c);
        }
    }

    public static IEnumerable<string> Chunk(string str, int maxChunkSize)
    {
        for (int i = 0; i < str.Length; i += maxChunkSize)
            yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
    }

    public static Vector2 getVector2(string rString)
    {
        string[] temp = rString.Split(',');
        string digitsOnly = @"[-+]?([0-9]*\.[0-9]+|[0-9]+)";
        temp[0] = Regex.Match(temp[0], digitsOnly).ToString();
        temp[1] = Regex.Match(temp[1], digitsOnly).ToString();
        var floatx = System.Convert.ToSingle(temp[0]);
        var floaty = System.Convert.ToSingle(temp[1]);
        var Vector2rValue = new Vector2(floatx, floaty);
        return Vector2rValue;

    }

    public static Vector3 getVector3(string rString)
    {
        var digitsOnly = @"[-+]?([0-9]*\.[0-9]+|[0-9]+),?";
        var matches = Regex.Matches(rString, digitsOnly);
        var stripped = "";
        foreach (var match in matches)
        {
            stripped += match.ToString();
        }
        var temp = stripped.Split(',');
        //temp[0] = Regex.Match(temp[0], digitsOnly).ToString();
        //temp[1] = Regex.Match(temp[1], digitsOnly).ToString();
        //temp[2] = Regex.Match(temp[2], digitsOnly).ToString();
        var floatx = System.Convert.ToSingle(temp[0]);
        var floaty = System.Convert.ToSingle(temp[1]);
        var floatz = System.Convert.ToSingle(temp[2]);
        var Vector3rValue = new Vector3(floatx, floaty, floatz);
        return Vector3rValue;
    }

    public static string Encrypt(string toEncrypt)
    {
        byte[] keyArray = UTF8Encoding.UTF8.GetBytes("85458928622725788545892862272578");
        // 256-AES key
        byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);
        RijndaelManaged rDel = new RijndaelManaged();
        rDel.Key = keyArray;
        rDel.Mode = CipherMode.ECB;
        // http://msdn.microsoft.com/en-us/library/system.security.cryptography.ciphermode.aspx
        rDel.Padding = PaddingMode.PKCS7;
        // better lang support
        ICryptoTransform cTransform = rDel.CreateEncryptor();
        byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        return Convert.ToBase64String(resultArray, 0, resultArray.Length);
    }

    public static string Decrypt(string toDecrypt)
    {
        byte[] keyArray = UTF8Encoding.UTF8.GetBytes("85458928622725788545892862272578");
        // AES-256 key
        byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);
        RijndaelManaged rDel = new RijndaelManaged();
        rDel.Key = keyArray;
        rDel.Mode = CipherMode.ECB;
        // http://msdn.microsoft.com/en-us/library/system.security.cryptography.ciphermode.aspx
        rDel.Padding = PaddingMode.PKCS7;
        // better lang support
        ICryptoTransform cTransform = rDel.CreateDecryptor();
        byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        return UTF8Encoding.UTF8.GetString(resultArray);
    }
    public static string ZipStr(String str)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (DeflateStream gzip =
              new DeflateStream(output, CompressionMode.Compress))
            {
                using (StreamWriter writer =
                  new StreamWriter(gzip, System.Text.Encoding.UTF8))
                {
                    writer.Write(str);
                }
            }
            return ByteToHexBitFiddle(output.ToArray());
            //return output.ToArray();
        }
    }

    public static string UnZipStr(string zippedString)
    {
        byte[] input = HexBitStringToBytes(zippedString);
        using (MemoryStream inputStream = new MemoryStream(input))
        {
            using (DeflateStream gzip =
              new DeflateStream(inputStream, CompressionMode.Decompress))
            {
                using (StreamReader reader =
                  new StreamReader(gzip, System.Text.Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}

    public static class Conversions
    {
        private static string regex = "^({0})";
        private static Regex r;
        public static List<string> ProvidedConversions()
        {
            return new List<string>()
            {
                "System.String-UnityEngine.Vector3",
                "System.String-UnityEngine.Vector2",
                "Newtonsoft.Json.Linq.JObject-Sovereign.SerializeTransform",
                "Newtonsoft.Json.Linq.JObject-UnityEngine.Vector2",
                "Newtonsoft.Json.Linq.JObject-UnityEngine.Vector3",
                @"Newtonsoft.Json.Linq.JArray-System.Collections.Generic.Dictionary`2",
                @"Newtonsoft.Json.Linq.JArray-System.Collections.Generic.List`1"
            };
        }

        public static object Convert(object fromObj, Type toType, Transform root, bool async = false)
        {
            string toTypeString = toType.ToString();
            string fromTypeString = fromObj.GetType().ToString();
            if (fromTypeString == toTypeString)
                return fromObj;
            try
            {
                //Debug.Log(String.Format("From: {0}, To: {1}", fromTypeString, toTypeString));
                var obj = System.Convert.ChangeType(fromObj, toType);
                return obj;
            }
            catch (InvalidCastException e)
            {
                var MatchString = fromTypeString + "-" + toTypeString;
                foreach (var item in ProvidedConversions())
                {
                    r = new Regex(String.Format(regex, Regex.Escape(item)), RegexOptions.IgnoreCase);
                    //Debug.Log(item + ", " + MatchString + ": " + r.Match(MatchString).Success);
                    if (r.Match(MatchString).Success)
                    {
                        return CastToType(fromObj, toType, root, async);
                    }
                }
                dynamic _enum = Activator.CreateInstance(toType);
                //Debug.Log(fromObj.ToString() + ", " + _enum + ": " + _enumcheck);
                if (_enum is Enum)
                {
                    return Enum.Parse(toType, fromObj.ToString());
                }
                Debug.Log(e);
            }
            return null;
        }

        public static object CastToType(object fromObj, Type toType, Transform root, bool async = false)
        {
            string toTypeString = toType.ToString();
            string fromTypeString = fromObj.GetType().ToString();
            switch (fromTypeString)
            {
                case "System.String":
                    switch (toTypeString)
                    {
                        case "UnityEngine.Vector3":
                            return GizmoTools.getVector3(fromObj.ToString());
                        case "UnityEngine.Vector2":
                            return GizmoTools.getVector2(fromObj.ToString());
                    }
                    break;
                case "Newtonsoft.Json.Linq.JObject":
                    switch (toTypeString)
                    {
                        case "UnityEngine.Vector3":
                            return GizmoTools.getVector3(fromObj.ToString());
                        case "UnityEngine.Vector2":
                            return GizmoTools.getVector2(fromObj.ToString());
                    }
                    break;
                case "Newtonsoft.Json.Linq.JArray":
                    r = new Regex(@"^.*(?=\[)", RegexOptions.IgnoreCase);
                    System.Text.RegularExpressions.Match m = r.Match(toTypeString);
                    string typestring = toTypeString.Substring(m.Value.Length + 1, (toTypeString.Length - m.Length - 2));
                    Type type = Type.GetType(typestring);
                    //Debug.Log(type);
                    switch (m.Value)
                    {
                        case "System.Collections.Generic.Dictionary`2":
                            if (typestring == "[System.String,System.String]")
                            {
                                List<KeyValuePair<string, string>> jar = ((JArray)fromObj).ToObject<List<KeyValuePair<string, string>>>();
                                var rdict = new Dictionary<string, string>();
                                foreach (KeyValuePair<string, string> token in jar)
                                {
                                    rdict.Add(token.Key, token.Value);
                                }
                                return rdict;
                            }
                            else if (typestring == "[System.String,System.Int32]")
                            {
                                List<KeyValuePair<string, int>> jar = ((JArray)fromObj).ToObject<List<KeyValuePair<string, int>>>();
                                var rdict = new Dictionary<string, int>();
                                foreach (KeyValuePair<string, int> token in jar)
                                {
                                    rdict.Add(token.Key, token.Value);
                                }
                                return rdict;
                            }
                            else
                            {
                                return null;
                            }
                        case "System.Collections.Generic.List`1":
                            Type genericlist = typeof(List<>);
                            Type generictypelist = genericlist.MakeGenericType(type);
                            //dynamic typeinstance = Activator.CreateInstance(type); //This Creates an instance of type List<Object>. It is unneccessary because we only need the type declaration, not an instance.
                            //Debug.Log(fromObj.ToString());
                            //Debug.Log(generictypelist);

                            dynamic alist = Activator.CreateInstance(generictypelist);
                            if (typeof(MonoBehaviour).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                            {
                                if (!typeof(IJson).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                                    throw new NotImplementedException(type.ToString());

                                foreach (var item in ((JArray)fromObj).Children())
                                {
                                    GameObject baseobj;
                                    var newtype = Type.GetType(item.Value<string>("__objtype")); //This ensures that we use the most specific type (newtype : BASE) when deserializing
                                    /* THIS IS COMMENTED OUT BECAUSE WE NO LONGER STORE ISO_OBJECTS IN THE CELL DATA
                                    if (typeof(INetworkObjectInfo).GetTypeInfo().IsAssignableFrom(newtype.GetTypeInfo())){
                                        if (GameData.isServer)
                                        {
                                            Debug.Log(newtype);
                                            baseobj = Object.Instantiate(
                                                Resources.Load<GameObject>("Prefabs/Iso_Object"), root, true);
                                            dynamic listitem = baseobj.AddComponent(newtype);
                                            var behaviour = listitem as MJSON;
                                            var obj = listitem as MonoBehaviour;
                                            var netobj = (listitem as INetworkObjectInfo).NetObject;
                                            behaviour.JSONDeserialize(item.ToString());
                                            obj.StartCoroutine(netobj.WaitForCell());
                                            baseobj.transform.SetParent(root);
                                            //netobj.BeginTracking();
                                            alist.Add(listitem);
                                        }
                                    } else { **THE TILE SPAWNING FUNCTION** }
                                    */
                                    baseobj = new GameObject();//SimplePool.Spawn(CreateObjectPools.TilePrefab);
                                    dynamic listitem = baseobj.GetComponentInChildren(newtype);
                                    var behaviour = listitem as IJson;
                                    if (behaviour == null)
                                    {
                                        listitem = baseobj.AddComponent(newtype);
                                        behaviour = listitem as IJson;
                                        Debug.Log(newtype.ToString());
                                        //continue;
                                    }

                                    //behaviour.Constructor = item.ToString();
                                    baseobj.transform.SetParent(root, true);
                                    //alist.Add(listitem);
                                }
                            }
                            else
                            {
                                alist = ((JArray)fromObj).ToObject(generictypelist);
                            }
                            return alist;
                    }
                    break;
                default:
                    break;
            }
            return null;
        }
    }

    public class GizmoSerializeAttribute : Attribute
    {

    }
    public static class ExpandoObjectExtensions
    {
        public static TObject ToObject<TObject>(this IDictionary<string, object> someSource, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
               where TObject : class, new()
        {
            System.Diagnostics.Contracts.Contract.Requires(someSource != null);
            TObject targetObject = new TObject();
            Type targetObjectType = typeof(TObject);

            // Go through all bound target object type properties...
            foreach (PropertyInfo property in
                        targetObjectType.GetProperties(bindingFlags))
            {
                // ...and check that both the target type property name and its type matches
                // its counterpart in the ExpandoObject
                if (someSource.ContainsKey(property.Name)
                    && property.PropertyType == someSource[property.Name].GetType())
                {
                    property.SetValue(targetObject, someSource[property.Name]);
                }
            }

            return targetObject;
        }
        public static object ToObject(this IDictionary<string, object> someSource, Type targetObjectType,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        {
            System.Diagnostics.Contracts.Contract.Requires(someSource != null);
            var targetObject = Activator.CreateInstance(targetObjectType);
            foreach(KeyValuePair<string, object> pair in someSource)
            {
                Debug.Log(pair.Key);
            }
            //Type targetObjectType = typeof(TObject);
            // Go through all bound target object type properties...
            foreach (PropertyInfo property in
                        targetObjectType.GetProperties(bindingFlags))
            {
                // ...and check that both the target type property name and its type matches
                // its counterpart in the ExpandoObject
                if (someSource.ContainsKey(property.Name))
                //Removing Type Check since we want to work with text
                //&& property.PropertyType == someSource[property.Name].GetType())
                {
                    property.SetValue(targetObject, Convert.ChangeType(someSource[property.Name], property.PropertyType));
                    //property.SetValue(targetObject, someSource[property.Name]);
                }
            }
            // Go through all bound target object type Fields...
            foreach (FieldInfo field in
                        targetObjectType.GetFields(bindingFlags))
            {
                if (someSource.ContainsKey(field.Name))
                    // ...and check that both the target type Field name and its type matches
                    // its counterpart in the ExpandoObject
                    if (someSource.ContainsKey(field.Name))
                    //Removing Type Check since we want to work with text
                    //&& field.FieldType == someSource[field.Name].GetType())
                    {
                        field.SetValue(targetObject, Convert.ChangeType(someSource[field.Name], field.FieldType));
                        //field.SetValue(targetObject, someSource[field.Name]);

                    }
            }

            return targetObject;
        }
    }
    public static class GameObjectExtension
    {

        public static void SetLayer(this GameObject parent, int layer, bool includeChildren = true)
        {
            parent.layer = layer;
            if (includeChildren)
            {
                foreach (Transform trans in parent.transform.GetComponentsInChildren<Transform>(true))
                {
                    trans.gameObject.layer = layer;
                }
            }
        }

        public static bool HasComponent<T>(this GameObject _obj)
        {
            var component = _obj.GetComponentInParent<T>();
            return component != null;
        }

        public static void SetActive(this GameObject _obj, bool bActive, bool bUpdateChildren = false)
        {
            _obj.SetActive(bActive);
            if (!bUpdateChildren)
                return;

            UpdateChildren(_obj, bActive);
        }

        private static void UpdateChildren(GameObject _child, bool bActive)
        {

            for (var i = 0; i < _child.transform.childCount; i++)
            {
                var child = _child.transform.GetChild(i);
                child.gameObject.SetActive(bActive);
                if (child.childCount > 0) UpdateChildren(child.gameObject, bActive);
            }
        }
        public static Rect GUIRectWithObject(this GameObject go, Camera camera)
        {
            var renderer = go.GetComponentInChildren<Renderer>();
            Vector3 cen = renderer.bounds.center;
            Vector3 ext = renderer.bounds.extents;
            Vector2[] extentPoints = new Vector2[8]
            {
            camera.WorldToScreenPoint(new Vector3(cen.x-ext.x, cen.y-ext.y, cen.z-ext.z)),
            camera.WorldToScreenPoint(new Vector3(cen.x+ext.x, cen.y-ext.y, cen.z-ext.z)),
            camera.WorldToScreenPoint(new Vector3(cen.x-ext.x, cen.y-ext.y, cen.z+ext.z)),
            camera.WorldToScreenPoint(new Vector3(cen.x+ext.x, cen.y-ext.y, cen.z+ext.z)),
            camera.WorldToScreenPoint(new Vector3(cen.x-ext.x, cen.y+ext.y, cen.z-ext.z)),
            camera.WorldToScreenPoint(new Vector3(cen.x+ext.x, cen.y+ext.y, cen.z-ext.z)),
            camera.WorldToScreenPoint(new Vector3(cen.x-ext.x, cen.y+ext.y, cen.z+ext.z)),
            camera.WorldToScreenPoint(new Vector3(cen.x+ext.x, cen.y+ext.y, cen.z+ext.z))
            };
            Vector2 min = extentPoints[0];
            Vector2 max = extentPoints[0];
            foreach (Vector2 v in extentPoints)
            {
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        /// <summary>
        /// Checks if a GameObject has been destroyed.
        /// </summary>
        /// <param name="gameObject">GameObject reference to check for destructedness</param>
        /// <returns>If the game object has been marked as destroyed by UnityEngine</returns>
        public static bool IsDestroyed(this GameObject gameObject)
        {
            // UnityEngine overloads the == opeator for the GameObject type
            // and returns null when the object has been destroyed, but 
            // actually the object is still there but has not been cleaned up yet
            // if we test both we can determine if the object has been destroyed.
            return gameObject == null && !ReferenceEquals(gameObject, null);
        }

    }

    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }
    public static class BoundsExtensions
    {
        public static Rect ToScreenSpace(this Bounds bounds, Camera camera)
        {
            var origin = camera.WorldToScreenPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z));
            var extents = camera.WorldToScreenPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.max.z));

            return new Rect(origin.x, Screen.height - origin.y, extents.x - origin.x, origin.y - extents.y);
        }
    }

    public static class IDictionaryExtenstions
    {
        public static void CleanGroup(this IDictionary<uint, GameObject> toClean)
        {
            var _toDelete = new List<uint>();
            foreach (var keyValuePair in toClean)
            {
                if (keyValuePair.Value == null || keyValuePair.Value.IsDestroyed()) _toDelete.Add(keyValuePair.Key);
            }
            foreach (var key in _toDelete)
            {
                toClean.Remove(key);
            }
        }
    }
}
