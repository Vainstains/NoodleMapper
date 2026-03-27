using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SimpleJSON;
using UnityEngine;

namespace VainLib.Data;

public static class JSONReflector
{
    #region Reflection
    private class ImplementationRecord
    {
        public Type Type { get; }
        public string Name { get; }
        public bool IsFallback { get; }

        public ImplementationRecord(Type type)
        {
            Type = type;
            Name = type.Name;

            var nameAttribute = type.GetCustomAttribute<JsonIDAttribute>();
            if (nameAttribute != null)
                Name = nameAttribute.ID;

            IsFallback = type.IsSubclassOf(typeof(JsonFallback));
        }
    }

    private class InterfaceImplementationDict : Dictionary<string, ImplementationRecord>
    {
        public ImplementationRecord? DefaultFallback { get; set; }
    }

    private abstract class ReflectedMemberRecord
    {
        private readonly Dictionary<string, object> _attributes = new();

        public abstract bool IsReadOnly { get; }
        public bool IsInterface { get; }
        public Type? InterfaceType { get; }
        public Type DeclaredType { get; }
        public string Name { get; }

        protected ReflectedMemberRecord(MemberInfo member)
        {
            Name = member.Name;

            var nameAttribute = member.GetCustomAttribute<JsonIDAttribute>();
            if (nameAttribute != null)
                Name = nameAttribute.ID;

            foreach (var attribute in member.GetCustomAttributes(true))
            {
                _attributes.Add(attribute.GetType().Name, attribute);
            }

            DeclaredType = member.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)member).FieldType,
                MemberTypes.Property => ((PropertyInfo)member).PropertyType,
                _ => throw new ArgumentException("Unsupported member type", nameof(member))
            };

            InterfaceType = DeclaredType.GetInterfaceType();
            IsInterface = InterfaceType != null;
        }

        public bool TryGetAttribute<T>(out T attribute) where T : Attribute
        {
            if (_attributes.TryGetValue(typeof(T).Name, out var obj))
            {
                attribute = (T)obj;
                return true;
            }

            attribute = default!;
            return false;
        }

        public abstract object GetValue(object instance);
        public abstract void SetValue(object instance, object value);
    }

    private class ReflectedFieldRecord : ReflectedMemberRecord
    {
        public FieldInfo Field { get; }
        public override bool IsReadOnly => Field.IsInitOnly;

        public ReflectedFieldRecord(FieldInfo field) : base(field)
        {
            Field = field;
        }

        public override object GetValue(object instance) => Field.GetValue(instance);

        public override void SetValue(object instance, object value)
        {
            if (IsReadOnly)
            {
                Debug.LogError($"Cannot set readonly field {Field.Name}");
                return;
            }

            Field.SetValue(instance, value);
        }
    }

    private class ReflectedPropertyRecord : ReflectedMemberRecord
    {
        public PropertyInfo Property { get; }
        public override bool IsReadOnly => Property.CanWrite && Property.GetSetMethod() == null;

        public ReflectedPropertyRecord(PropertyInfo property) : base(property)
        {
            Property = property;
        }

        public override object GetValue(object instance) => Property.GetValue(instance);

        public override void SetValue(object instance, object value)
        {
            if (IsReadOnly)
            {
                Debug.LogError($"Cannot set readonly property {Property.Name}");
                return;
            }

            Property.SetValue(instance, value);
        }
    }

    private class ReflectedTypeRecord
    {
        public ReflectedMemberRecord[] Members { get; }
        public MethodInfo? OnJsonDeserialized { get; }

        public ReflectedTypeRecord(Type type)
        {
            var members = new List<ReflectedMemberRecord>();
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                members.Add(new ReflectedFieldRecord(field));
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                members.Add(new ReflectedPropertyRecord(property));
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (method.GetCustomAttribute<OnJsonDeserializedAttribute>() != null)
                {
                    OnJsonDeserialized = method;
                    break;
                }
            }

            Members = members.ToArray();
        }
    }

    private static readonly Dictionary<Type, InterfaceImplementationDict> AvailableInterfaces = new();
    private static readonly Dictionary<Type, Type?> InterfaceTypes = new();
    private static readonly Dictionary<Type, ReflectedTypeRecord> ReflectedTypes = new();

    private static bool IsGenericList(this Type type) => type.IsGenericType && (
        type.GetGenericTypeDefinition() == typeof(List<>) ||
        type.GetGenericTypeDefinition() == typeof(IList<>)
    );

    private static bool IsGenericDictionary(this Type type) => type.IsGenericType && (
        type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
        type.GetGenericTypeDefinition() == typeof(IDictionary<,>)
    );

    private static Type? GetInterfaceType(this Type type)
    {
        if (type.IsInterface)
            return type;

        if (InterfaceTypes.TryGetValue(type, out var interfaceType))
            return interfaceType;

        if (type.IsArray)
        {
            interfaceType = type.GetElementType();
        }
        else if (type.IsGenericList())
        {
            interfaceType = type.GetGenericArguments()[0];
        }
        else if (type.IsGenericDictionary())
        {
            interfaceType = type.GetGenericArguments()[1];
        }

        if (interfaceType != null && !interfaceType.IsInterface)
            interfaceType = null;

        InterfaceTypes[type] = interfaceType;

        if (interfaceType != null)
        {
            CollectInterfaceImplementations(interfaceType);
        }

        return interfaceType;
    }

    private static bool IsClassOrStruct(this Type type)
    {
        return type.IsClass || (type.IsValueType && !type.IsEnum && !type.IsPrimitive);
    }

    private static void CollectInterfaceImplementations(Type interfaceType)
    {
        if (AvailableInterfaces.ContainsKey(interfaceType))
            return;

        Debug.Log($"Collecting implementations for {interfaceType.Name}...");
        var implementations = new InterfaceImplementationDict();

        var implementingTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClassOrStruct())
            .Where(t => t.GetInterfaces().Contains(interfaceType));

        foreach (var implementingType in implementingTypes)
        {
            var impl = new ImplementationRecord(implementingType);
            implementations.Add(implementingType.Name, impl);
            if (impl.IsFallback)
            {
                implementations.DefaultFallback = impl;
                Debug.Log($"    -Found implementation {impl.Name} (set as fallback)");
            }
            else
            {
                Debug.Log($"    -Found implementation {impl.Name}");
            }
        }

        AvailableInterfaces[interfaceType] = implementations;
    }

    private static ReflectedTypeRecord GetReflectedTypeRecord(Type type)
    {
        if (!ReflectedTypes.TryGetValue(type, out var record))
        {
            record = new ReflectedTypeRecord(type);
            ReflectedTypes.Add(type, record);
        }

        return record;
    }

    #endregion

    #region Serialization

    private static bool TrySerializePrimitive(object? obj, out JSONNode node)
    {
        if (obj == null)
        {
            node = JSONNull.CreateOrGet();
            return true;
        }

        if (obj is string stringValue)
        {
            node = new JSONString(stringValue);
            return true;
        }

        if (obj is bool boolValue)
        {
            node = new JSONBool(boolValue);
            return true;
        }

        if (obj is int intValue)
        {
            node = new JSONNumber(intValue);
            return true;
        }

        if (obj is float floatValue)
        {
            node = new JSONNumber(floatValue);
            return true;
        }

        if (obj is double doubleValue)
        {
            node = new JSONNumber(doubleValue);
            return true;
        }

        if (obj is JSONNode jsonNode)
        {
            node = jsonNode;
            return true;
        }

        if (obj is Vector2 vector2)
        {
            node = new JSONArray();
            node.Add(vector2.x);
            node.Add(vector2.y);
            return true;
        }

        if (obj is Vector2Int vector2Int)
        {
            node = new JSONArray();
            node.Add(vector2Int.x);
            node.Add(vector2Int.y);
            return true;
        }

        if (obj is Vector3 vector3)
        {
            node = new JSONArray();
            node.Add(vector3.x);
            node.Add(vector3.y);
            node.Add(vector3.z);
            return true;
        }

        if (obj is Vector3Int vector3Int)
        {
            node = new JSONArray();
            node.Add(vector3Int.x);
            node.Add(vector3Int.y);
            node.Add(vector3Int.z);
            return true;
        }

        if (obj is Vector4 vector4)
        {
            node = new JSONArray();
            node.Add(vector4.x);
            node.Add(vector4.y);
            node.Add(vector4.z);
            node.Add(vector4.w);
            return true;
        }

        if (obj is Quaternion quaternion)
        {
            node = new JSONArray();
            node.Add(quaternion.x);
            node.Add(quaternion.y);
            node.Add(quaternion.z);
            node.Add(quaternion.w);
            return true;
        }

        if (obj is Rect rect)
        {
            node = new JSONArray();
            node.Add(rect.x);
            node.Add(rect.y);
            node.Add(rect.width);
            node.Add(rect.height);
            return true;
        }

        if (obj is RectOffset rectOffset)
        {
            node = new JSONArray();
            node.Add(rectOffset.left);
            node.Add(rectOffset.right);
            node.Add(rectOffset.top);
            node.Add(rectOffset.bottom);
            return true;
        }

        if (obj is Color color)
        {
            node = new JSONArray();
            node.Add(color.r);
            node.Add(color.g);
            node.Add(color.b);
            node.Add(color.a);
            return true;
        }

        if (obj is Matrix4x4 matrix4x4)
        {
            node = new JSONArray();
            for (var i = 0; i < 16; i++)
            {
                node.Add(matrix4x4[i]);
            }
            return true;
        }

        node = JSONNull.CreateOrGet();
        return false;
    }

    private static JSONNode SerializeObject(object? obj, Type? interfaceType = null)
    {
        if (TrySerializePrimitive(obj, out var primitiveNode))
            return primitiveNode;
        
        var type = obj.GetType();

        if (type.IsEnum)
        {
            return new JSONString(obj.ToString());
        }

        if (type.IsArray)
        {
            if (type.GetArrayRank() != 1)
            {
                Debug.LogError("Cannot serialize multi-dimensional arrays");
                return new JSONArray();
            }

            var array = (Array)obj;
            var node = new JSONArray();
            foreach (var element in array)
            {
                var elementNode = SerializeObject(element, interfaceType);
                node.Add(elementNode);
            }

            return node;
        }

        if (type.IsGenericList())
        {
            var list = (IList)obj;
            var node = new JSONArray();
            foreach (var element in list)
            {
                var elementNode = SerializeObject(element, interfaceType);
                node.Add(elementNode);
            }

            return node;
        }

        if (type.IsGenericDictionary())
        {
            var dictionary = (IDictionary)obj;
            var node = new JSONObject();
            foreach (var key in dictionary.Keys)
            {
                var keyString = key.ToString();
                var value = dictionary[key];
                var valueNode = SerializeObject(value, interfaceType);
                node.Add(keyString, valueNode);
            }

            return node;
        }

        if (type.IsSubclassOf(typeof(JsonFallback)))
        {
            var fallback = (JsonFallback)obj;
            return fallback.Node;
        }

        if (type.IsClassOrStruct())
        {
            var record = GetReflectedTypeRecord(type);
            var objectNode = new JSONObject();
            foreach (var member in record.Members)
            {
                if (member.IsReadOnly)
                    continue;

                var value = member.GetValue(obj);
                var memberNode = SerializeObject(value, member.InterfaceType);
                // objectNode.Add($"{member.Name}_dbg", member.DeclaredType.Name);
                // objectNode.Add($"{member.Name}_dbg_interface", member.InterfaceType?.Name ?? "(null)");
                objectNode.Add(member.Name, memberNode);
            }

            if (interfaceType != null)
            {
                var typeName = "<unk>";

                if (AvailableInterfaces.TryGetValue(interfaceType, out var impls))
                {
                    foreach (var impl in impls)
                    {
                        if (impl.Value.Type == type)
                        {
                            typeName = impl.Value.Name;
                            break;
                        }
                    }
                }

                objectNode["@type"] = typeName;
            }

            return objectNode;
        }

        Debug.LogError($"Cannot serialize type {type.Name}");
        return JSONNull.CreateOrGet();
    }

    public static JSONNode ToJSON<T>(T t)
    {
        var type = typeof(T);
        if (type.IsInterface)
            return SerializeObject(t, type);

        return SerializeObject(t);
    }

    #endregion

    #region Deserialization

    private static object? ConvertDictionaryKey(string key, Type keyType)
    {
        if (keyType == typeof(string))
            return key;

        if (keyType.IsEnum)
            return Enum.Parse(keyType, key);

        if (keyType == typeof(bool))
            return bool.Parse(key);

        return Convert.ChangeType(key, keyType);
    }

    private static Type ResolveDeserializationType(JSONNode node, Type declaredType, Type? interfaceType)
    {
        if (interfaceType == null)
            return declaredType;

        CollectInterfaceImplementations(interfaceType);
        if (!AvailableInterfaces.TryGetValue(interfaceType, out var implementations))
            return declaredType;

        if (node is not JSONObject objectNode)
            return declaredType;

        var typeName = objectNode["@type"].Value;
        if (!string.IsNullOrEmpty(typeName))
        {
            foreach (var implementation in implementations.Values)
            {
                if (implementation.Name == typeName)
                    return implementation.Type;
            }
        }

        return implementations.DefaultFallback?.Type ?? declaredType;
    }

    // the biggest array we'll ever need to deserialize is for a 4x4 matrix; 16 floats.
    private static readonly float[] s_floatArray = new float[16];
    private static float[] DeserializeKeyedOrArray(JSONNode node, params string[] keys)
    {
        if (node.IsArray)
        {
            var array = node.AsArray;
            if (array.Count != keys.Length)
                return s_floatArray;

            for (var i = 0; i < keys.Length; i++)
            {
                s_floatArray[i] = array[i].AsFloat;
            }

            return s_floatArray;
        }
        if (node.IsObject)
        {
            var obj = node.AsObject;
            if (obj.Count != keys.Length)
                return s_floatArray;

            for (var i = 0; i < keys.Length; i++)
            {
                if (!obj.HasKey(keys[i]))
                    s_floatArray[i] = 0;
                s_floatArray[i] = obj[keys[i]].AsFloat;
            }

            return s_floatArray;
        }

        for (var i = 0; i < keys.Length; i++)
            s_floatArray[i] = 0;
        return s_floatArray;
    }

    private static bool TryDeserializePrimitive(JSONNode node, Type targetType, out object? obj)
    {
        if (targetType == typeof(string))
        {
            obj = node.Value;
            return true;
        }

        if (targetType == typeof(bool))
        {
            obj = node.AsBool;
            return true;
        }

        if (targetType == typeof(int))
        {
            obj = node.AsInt;
            return true;
        }

        if (targetType == typeof(float))
        {
            obj = node.AsFloat;
            return true;
        }

        if (targetType == typeof(double))
        {
            obj = node.AsDouble;
            return true;
        }

        // We serialize these as arrays, but for leniency's sake we'll also
        // support their keyed versions.

        if (!(node.IsArray || node.IsObject))
        {
            obj = null;
            return false;
        }

        if (targetType == typeof(Vector2))
        {
            var floats = DeserializeKeyedOrArray(node, "x", "y");
            obj = new Vector2(floats[0], floats[1]);
            return true;
        }

        if (targetType == typeof(Vector2Int))
        {
            var floats = DeserializeKeyedOrArray(node, "x", "y");
            obj = new Vector2Int((int)floats[0], (int)floats[1]);
            return true;
        }

        if (targetType == typeof(Vector3))
        {
            var floats = DeserializeKeyedOrArray(node, "x", "y", "z");
            obj = new Vector3(floats[0], floats[1], floats[2]);
            return true;
        }

        if (targetType == typeof(Vector3Int))
        {
            var floats = DeserializeKeyedOrArray(node, "x", "y", "z");
            obj = new Vector3Int((int)floats[0], (int)floats[1], (int)floats[2]);
            return true;
        }

        if (targetType == typeof(Vector4))
        {
            var floats = DeserializeKeyedOrArray(node, "x", "y", "z", "w");
            obj = new Vector4(floats[0], floats[1], floats[2], floats[3]);
            return true;
        }

        if (targetType == typeof(Quaternion))
        {
            var floats = DeserializeKeyedOrArray(node, "x", "y", "z", "w");
            obj = new Quaternion(floats[0], floats[1], floats[2], floats[3]);
            return true;
        }

        if (targetType == typeof(Rect))
        {
            var floats = DeserializeKeyedOrArray(node, "x", "y", "w", "h");
            obj = new Rect(floats[0], floats[1], floats[2], floats[3]);
            return true;
        }

        if (targetType == typeof(RectOffset))
        {
            var floats = DeserializeKeyedOrArray(node, "l", "r", "t", "b");
            obj = new RectOffset((int)floats[0], (int)floats[1], (int)floats[2], (int)floats[3]);
            return true;
        }

        if (targetType == typeof(Color))
        {
            var floats = DeserializeKeyedOrArray(node, "r", "g", "b", "a");
            var col = new Color(floats[0], floats[1], floats[2], floats[3]);
            if (node.IsObject && !node.HasKey("a"))
                col.a = 1;
            obj = col;
            return true;
        }

        if (targetType == typeof(Matrix4x4))
        {
            var floats = DeserializeKeyedOrArray(node, 
                "m00", "m01", "m02", "m03",
                "m10", "m11", "m12", "m13",
                "m20", "m21", "m22", "m23",
                "m30", "m31", "m32", "m33");
            var mat = new Matrix4x4();
            for (var i = 0; i < 16; i++)
                mat[i] = floats[i];
            obj = mat;
            return true;
        }

        obj = null;
        return false;
    }

    private static object? DeserializeObject(JSONNode node, Type declaredType, Type? interfaceType = null)
    {
        if (node == null || node.IsNull)
            return null;

        if (declaredType == typeof(JSONNode) || declaredType.IsSubclassOf(typeof(JSONNode)))
            return node;

        var targetType = ResolveDeserializationType(node, declaredType, interfaceType);

        if (TryDeserializePrimitive(node, targetType, out var obj))
            return obj;

        if (targetType.IsEnum)
            return Enum.Parse(targetType, node.Value);

        if (targetType.IsArray)
        {
            if (targetType.GetArrayRank() != 1)
            {
                Debug.LogError("Cannot deserialize multi-dimensional arrays");
                return null;
            }

            if (node is not JSONArray arrayNode)
                return null;

            var elementType = targetType.GetElementType()!;
            var elementInterfaceType = elementType.GetInterfaceType();
            var array = Array.CreateInstance(elementType, arrayNode.Count);
            for (var i = 0; i < arrayNode.Count; i++)
            {
                array.SetValue(DeserializeObject(arrayNode[i], elementType, elementInterfaceType), i);
            }

            return array;
        }

        if (targetType.IsGenericList())
        {
            if (node is not JSONArray arrayNode)
                return null;

            var elementType = targetType.GetGenericArguments()[0];
            var elementInterfaceType = elementType.GetInterfaceType();
            var listType = targetType.IsInterface
                ? typeof(List<>).MakeGenericType(elementType)
                : targetType;
            var list = (IList)Activator.CreateInstance(listType)!;
            for (var i = 0; i < arrayNode.Count; i++)
            {
                list.Add(DeserializeObject(arrayNode[i], elementType, elementInterfaceType));
            }

            return list;
        }

        if (targetType.IsGenericDictionary())
        {
            if (node is not JSONObject objectNode)
                return null;

            var genericArguments = targetType.GetGenericArguments();
            var keyType = genericArguments[0];
            var valueType = genericArguments[1];
            var valueInterfaceType = valueType.GetInterfaceType();
            var dictionaryType = targetType.IsInterface
                ? typeof(Dictionary<,>).MakeGenericType(genericArguments)
                : targetType;
            var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType)!;
            foreach (var kvp in objectNode)
            {
                dictionary.Add(
                    ConvertDictionaryKey(kvp.Key, keyType),
                    DeserializeObject(kvp.Value, valueType, valueInterfaceType)
                );
            }

            return dictionary;
        }

        if (targetType.IsSubclassOf(typeof(JsonFallback)))
        {
            var fallback = (JsonFallback)Activator.CreateInstance(targetType)!;
            fallback.Node = node;
            return fallback;
        }

        if (targetType.IsClassOrStruct())
        {
            if (node is not JSONObject objectNode)
                return null;

            var instance = Activator.CreateInstance(targetType)!;
            var record = GetReflectedTypeRecord(targetType);
            var memberNodes = new Dictionary<string, JSONNode>(objectNode.Count);
            foreach (var kvp in objectNode)
            {
                memberNodes[kvp.Key] = kvp.Value;
            }

            foreach (var member in record.Members)
            {
                if (member.IsReadOnly)
                    continue;

                if (!memberNodes.TryGetValue(member.Name, out var memberNode))
                    continue;

                var value = DeserializeObject(memberNode, member.DeclaredType, member.InterfaceType);
                try
                {
                    member.SetValue(instance, value!);
                }
                catch (Exception e)
                {
                    // haha fuck you
                }
            }
            
            if (record.OnJsonDeserialized != null)
            {
                record.OnJsonDeserialized.Invoke(instance, null);
            }
            return instance;
        }

        Debug.LogError($"Cannot deserialize type {targetType.Name}");
        return null;
    }

    public static T? ToObject<T>(JSONNode node)
    {
        var type = typeof(T);
        var interfaceType = type.IsInterface ? type : null;
        return (T?)DeserializeObject(node, type, interfaceType);
    }

    #endregion
}
