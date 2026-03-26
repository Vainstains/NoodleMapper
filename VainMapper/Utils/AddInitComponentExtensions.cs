using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace VainMapper.Utils;

public static class AddInitComponentExtensions
{
    private static Dictionary<Type, MethodInfo> m_initMethods = new();

    private static bool CheckTypes(MethodInfo? info, Type[] argTypes, out string error)
    {
        if (info == null)
        {
            error = "Init method does not exist";
            return false;
        }
        
        bool compatible = true;
        var paramTypes = info.GetParameters();
        
        if (argTypes.Length != paramTypes.Length)
            compatible = false;
        else
            for (int i = 0; i < argTypes.Length; i++)
                if (!paramTypes[i].ParameterType.IsAssignableFrom(argTypes[i]))
                {
                    compatible = false;
                    break;
                }
        
        if (!compatible)
            error = CreateArgumentParameterMismatchMessage(info, argTypes);
        else
            error = string.Empty;

        return compatible;
    }

    private static string CreateArgumentParameterMismatchMessage(MethodInfo info, Type[] argTypes)
    {
        StringBuilder sb = new();

        sb.Append("Arguments `");
        for (int i = 0; i < argTypes.Length; i++)
        {
            var argType = argTypes[i];
            sb.Append(argType.Name);
            if (i != argTypes.Length - 1)
                sb.Append(", ");
        }
        sb.Append("` are incompatible with ");
        
        sb.Append(info.DeclaringType.Name).Append('.').Append(info.Name).Append('(');
        var paramInfos = info.GetParameters();
        for (var i = 0; i < paramInfos.Length; i++)
        {
            var param = paramInfos[i];
            sb.Append(param.ParameterType.Name).Append(' ').Append(param.Name);
            if (i < paramInfos.Length - 1)
                sb.Append(", ");
        }
        sb.Append(')');
        
        return sb.ToString();
    }

    private static MethodInfo? GetInitMethodForType(Type type, Type[] argumentTypes, out string error)
    {
        MethodInfo? method;
        if (m_initMethods.TryGetValue(type, out var result))
        {
            method = result;
        }
        else
        {
            method = type.GetMethod(
                "Init",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (method != null)
                m_initMethods.Add(type, method);
        }

        if (CheckTypes(method, argumentTypes, out error))
            return method;
        return null;
    }
    
    // ==================
    // Generics
    // ==================
    
    public static TComponent AddInitChild<TComponent>(this RectTransform self, params object[] args) 
        where TComponent : Component => 
        (self.AddInitComponent(typeof(TComponent), args) as TComponent)!;

    public static TComponent AddInitComponent<TComponent>(this RectTransform self, params object[] args) 
        where TComponent : Component => 
        (self.AddInitComponent(typeof(TComponent), args) as TComponent)!;
    
    public static TComponent AddInitChild<TComponent>(this GameObject self, params object[] args) 
        where TComponent : Component => 
        (self.AddInitComponent(typeof(TComponent), args) as TComponent)!;
    
    public static TComponent AddInitComponent<TComponent>(this GameObject self, params object[] args)
        where TComponent : Component => 
        (self.AddInitComponent(typeof(TComponent), args) as TComponent)!;
    
    // ==================
    // Non-generics
    // ==================
    
    public static Component AddInitChild(this RectTransform self, Type componentType, params object[] args) =>
        self.gameObject.AddInitChild(componentType, args);
    
    public static Component AddInitComponent(this RectTransform self, Type componentType, params object[] args) =>
        self.gameObject.AddInitComponent(componentType, args);
    
    public static Component AddInitChild(this GameObject self, Type componentType, params object[] args) =>
        self.AddChildObject(componentType.Name).AddInitComponent(componentType, args);

    public static Component AddInitComponent(this GameObject self, Type componentType, params object[] args)
    {
        if (!componentType.IsSubclassOf(typeof(Component)))
        {
            throw new ArgumentException($"Type '{componentType}' is not a subclass of Component.");
        }
        
        var comp = self.AddComponent(componentType)!;
        
        var method = GetInitMethodForType(
            componentType,
            args.Select(a => a.GetType()).ToArray(),
            out var err);

        if (method == null)
            Debug.LogError($"Error in AddInitComponent<{componentType.Name}>: {err}");
        
        try
        {
            method!.Invoke(comp, args);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Init(...) invocation on {componentType.Name} failed: {ex}");
        }

        return comp;
    }
}