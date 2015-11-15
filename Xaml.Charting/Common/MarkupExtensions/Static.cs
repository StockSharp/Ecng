// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// Static.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the license, see http://www.ultrachart.com/ultrachart-eula/
// 
// This source code is protected by international copyright law. Unauthorized
// reproduction, reverse-engineering, or distribution of all or any portion of
// this source code is strictly prohibited.
// 
// This source code contains confidential and proprietary trade secrets of
// ulc software Services Ltd., and should at no time be copied, transferred, sold,
// distributed or made available without express written permission.
// *************************************************************************************
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Xaml;

namespace Ecng.Xaml.Charting.Common.MarkupExtensions
{
    /// <summary>
    /// A MarkupExtension which introduces x:Static like syntax to both WPF and Silverlight (Cross-platform). This is used internally
    /// for the themes, but is also useful e.g. when creating custom Control Templates for Ultrachart
    /// </summary>
    /// <remarks>
    /// Licensed under the CodeProject Open License
    /// http://www.codeproject.com/Articles/305932/Static-and-Type-markup-extensions-for-Silverlight
    /// </remarks>
    public class Static : MarkupExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Static" /> class.
        /// </summary>
        public Static()
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Static" /> class.
        /// </summary>
        /// <param name="member">The member.</param>
        public Static(String member)
        {
            Member = member;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Static" /> class.
        /// </summary>
        /// <param name="memberType">Type of the member.</param>
        /// <param name="member">The member.</param>
        public Static(Type memberType, String member)
        {
            MemberType = memberType;
            Member = member;
        }

        private Type m_memberType;

        /// <summary>
        /// Gets or sets the type of the member.
        /// </summary>
        public Type MemberType
        {
            get { return m_memberType; }
            set { m_memberType = value; }
        }

        private String m_member;

        /// <summary>
        /// Gets or sets the member.
        /// </summary>
        public String Member
        {
            get { return m_member; }
            set { m_member = value; }
        }

        /// <summary>
        /// The unset argument
        /// </summary>
        public static readonly Object UnsetArgument = new Object();

        private Object m_arg1 = UnsetArgument;

        /// <summary>
        /// Gets or sets the arg1.
        /// </summary>
        public Object Arg1
        {
            get { return m_arg1; }
            set { m_arg1 = value; }
        }

        private Object m_arg2 = UnsetArgument;

        /// <summary>
        /// Gets or sets the arg2.
        /// </summary>
        public Object Arg2
        {
            get { return m_arg2; }
            set { m_arg2 = value; }
        }

        private Object m_arg3 = UnsetArgument;

        /// <summary>
        /// Gets or sets the arg3.
        /// </summary>
        public Object Arg3
        {
            get { return m_arg3; }
            set { m_arg3 = value; }
        }

        /// <summary>
        /// Gets the default type of the member.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns></returns>
        public static Type GetDefaultMemberType(DependencyObject obj)
        {
            return (Type)obj.GetValue(DefaultMemberTypeProperty);
        }

        /// <summary>
        /// Sets the default type of the member.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="value">The value.</param>
        public static void SetDefaultMemberType(DependencyObject obj, Type value)
        {
            obj.SetValue(DefaultMemberTypeProperty, value);
        }

        /// <summary>
        /// The default member type property
        /// </summary>
        public static readonly DependencyProperty DefaultMemberTypeProperty =
            DependencyProperty.RegisterAttached("DefaultMemberType", typeof(Type), typeof(Static), new PropertyMetadata(null));

        private static readonly Object NoCachedValue = new Object();

        private Object m_cachedValue = NoCachedValue;

        /// <summary>
        /// When implemented in a derived class, returns an object that is provided as the value of the target property for this markup extension.
        /// </summary>
        /// <param name="serviceProvider">A service provider helper that can provide services for the markup extension.</param>
        /// <returns>
        /// The object value to set on the property where the extension is applied.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Member property must be set to a non-empty value on Static markup extension.</exception>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (m_cachedValue != NoCachedValue) return m_cachedValue;

            if (String.IsNullOrWhiteSpace(m_member)) throw new InvalidOperationException("Member property must be set to a non-empty value on Static markup extension.");

            string member = m_member;
            Type memberType = m_memberType;

            // Look for and process any arguments
            List<Object> arguments = null;
            int argStartPos = member.IndexOf('(');
            if (argStartPos >= 0)
            {
                arguments = ParseArguments(member, argStartPos + 1);
                // Strip arguments
                member = member.Substring(0, argStartPos).Trim();
            }
            if (Arg1 != UnsetArgument)
            {
                if (arguments != null) throw new InvalidOperationException("Arg1, Arg2, etc. cannot be used when arguments are specified in member");
                arguments = new List<object>();
                arguments.Add(Arg1);
            }
            if (Arg2 != UnsetArgument)
            {
                if (arguments == null || arguments.Count != 1 || argStartPos >= 0) throw new InvalidOperationException("Arg1, Arg2, etc. cannot be used when arguments are specified in member and must be specified in consecutive order");
                arguments.Add(Arg2);
            }
            if (Arg3 != UnsetArgument)
            {
                if (arguments == null || arguments.Count != 2 || argStartPos >= 0) throw new InvalidOperationException("Arg1, Arg2, etc. cannot be used when arguments are specified in member and must be specified in consecutive order");
                arguments.Add(Arg3);
            }


            // Look for and process any member types in member           
            int lastDotIndex = member.LastIndexOf('.');
            if (lastDotIndex >= 0)
            {
                if (serviceProvider == null) return DependencyProperty.UnsetValue;

                IXamlTypeResolver resolver = serviceProvider.GetService(typeof(IXamlTypeResolver)) as IXamlTypeResolver;
                if (resolver == null) return DependencyProperty.UnsetValue;
                string unresolvedTypeName = m_member.Substring(0, lastDotIndex);
                memberType = resolver.Resolve(unresolvedTypeName);
                member = member.Substring(lastDotIndex + 1);
            }
            else
            {
                if (m_memberType != null)
                {
                    memberType = m_memberType;
                }
                else // no member type specified
                {
                    // Look for Static.DefaultMemberType set on root object.
                    if (serviceProvider != null)
                    {
                        IRootObjectProvider rop = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
                        if (rop != null)
                        {
                            DependencyObject rootObject = rop.RootObject as DependencyObject;
                            if (rootObject != null)
                            {
                                memberType = GetDefaultMemberType(rootObject);
                            }
                        }
                    }
                    if (memberType == null) return DependencyProperty.UnsetValue;
                }

            }
            object returnValue;
            if (arguments == null)
            {
                // First look for matching property
                PropertyInfo property = memberType.GetProperty(member, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (property != null)
                {
                    if (property.CanRead)
                    {
                        returnValue = property.GetValue(null, null);
                    }
                    else
                    {
                        throw new InvalidOperationException("Property " + member + " is not readable.");
                    }
                }
                else
                {
                    // Then look for public fields
                    FieldInfo field = memberType.GetField(member, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    if (field != null)
                    {
                        returnValue = field.GetValue(null);
                    }
                    else
                    {
                        throw new InvalidOperationException(String.Format("No public static property or field '{0}' found in type '{1}'", member, memberType.FullName));
                    }
                }
            }
            else  // Try to locate and invoke method
            {
                int argCount = arguments.Count;
                MethodInfo method = memberType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                                              .FirstOrDefault(m => String.Equals(m.Name, member, StringComparison.InvariantCultureIgnoreCase) && !m.IsGenericMethod && m.GetParameters().Length == argCount);
                if (method == null) throw new InvalidOperationException(String.Format("No public static non-generic method '{0}' with {1} parameter(s) found in type '{2}'", member, argCount, memberType));
                ParameterInfo[] pInfos = method.GetParameters();
                object[] paramValues = new object[argCount];
                for (int i = 0; i < argCount; i++)
                {
                    paramValues[i] = ConvertToType(pInfos[i].ParameterType, arguments[i], CultureInfo.InvariantCulture);
                }
                returnValue = method.Invoke(null, argCount == 0 ? null : paramValues);
            }
            // Try to convert return value to target property type.
            if (serviceProvider != null)
            {
                IProvideValueTarget pvt = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
                if (pvt != null)
                {
                    PropertyInfo targetProperty = pvt.TargetProperty as PropertyInfo;
                    if (targetProperty != null)
                    {
                        returnValue = ConvertToType(targetProperty.PropertyType, returnValue, Thread.CurrentThread.CurrentUICulture);
                    }
#if !SILVERLIGHT
                    else  // In WPF a DependencyProperty is used for dependency properties.
                    {
                        DependencyProperty propertyMetaData = pvt.TargetProperty as DependencyProperty;
                        if (propertyMetaData != null)
                        {
                            returnValue = ConvertToType(propertyMetaData.PropertyType, returnValue, Thread.CurrentThread.CurrentUICulture);
                        }
                    }
#endif
                }
            }
            m_cachedValue = returnValue;
            return returnValue;

        }

        private static object ConvertToType(Type targetType, object value, CultureInfo culture)
        {
            if (value == null) return null;
            if (targetType.IsAssignableFrom(value.GetType())) return value;
            // Return null if empty string unless target type is string
            if (targetType != typeof(String) && String.IsNullOrWhiteSpace(value.ToString())) return null;
            // Change Nullable types to base type 
            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            // Try IConvertible conversion as a last resort.
            return System.Convert.ChangeType(value, targetType, culture);
        }

        private static List<object> ParseArguments(string member, int pos)
        {
            List<object> arguments = new List<object>();
            while (true)
            {
                while (pos < member.Length && Char.IsWhiteSpace(member[pos])) pos++;
                if (pos >= member.Length) throw new InvalidOperationException("Ending ')' is missing from member specification");
                if (member[pos] == ')') return arguments;
                if (member[pos] == '\'') // If single quoted, read until next single quote
                {
                    pos++;
                    int startPos = pos;
                    while (pos < member.Length && (member[pos] != '\'' || member[pos - 1] == '\\')) pos++;
                    if (pos >= member.Length) throw new InvalidOperationException("Ending ' is missing from member specification");
                    arguments.Add(member.Substring(startPos, pos - startPos));
                }
                else
                {
                    int startPos = pos;
                    while (pos < member.Length && member[pos] != ')' && member[pos] != ',' && member[pos] != ')') pos++;
                    string argValue = member.Substring(startPos, pos - startPos).Trim();
                    arguments.Add(argValue);
                }
                while (pos < member.Length && member[pos] != ')')
                {
                    if (member[pos] == ',')
                    {
                        pos++;
                        break;
                    }
                    if (!Char.IsWhiteSpace(member, pos)) throw new InvalidOperationException("Separating comma (',') or ending paranthesis (')') is expected at position " + pos);
                    pos++;
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return null;
            // For VS Design-time support.
//            try
//            {
//                return ProvideValue(null) as String;
//            }
//            catch (Exception)
//            {
//                return null;
//            }

        }

    }
}
