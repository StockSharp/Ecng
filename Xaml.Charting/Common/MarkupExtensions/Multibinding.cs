/* 
 * Copyright Henrik Jonsson 2011 ( https://workspaces.codeproject.com/nazarrudnyk/multibinding-in-silverlight-5/article )
 * This code is licenced under the The Code Project Open Licence 1.02 (see Licence.htm and http://www.codeproject.com/info/cpol10.aspx). 
 */

using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xaml;
using Ecng.Xaml.Charting.Common.Databinding;
using Ecng.Xaml.Charting.Common.Helpers;

namespace Ecng.Xaml.Charting.Common.MarkupExtensions
{


#if SILVERLIGHT
    /// <summary>
    /// Implementation for SL and WPF compatible MultiBinding
    /// </summary>
    [ContentProperty("Bindings")]
#endif
    public class MultiBindingCompatible :
#if !SILVERLIGHT
                                          MultiBinding
    {
    }
#else 
                                          DependencyObject, IMarkupExtension<Object>
    {       
        protected bool IsSealed { get; private set; }

        public void Seal()
        {
            IsSealed = true;
            if( Bindings != null ) Bindings.Seal();
        }

        public void CheckSealed()
        {
            if (IsSealed) throw new InvalidOperationException("Properties on MultiBinding cannot be changed after it has been applied.");
        }

        private readonly BindingCollection  _mBindings = new BindingCollection();

        /// <summary>
        /// Gets collection of bindings
        /// </summary>
        public BindingCollection Bindings
        {
            get { return _mBindings; }
        }

        private BindingMode _mBindingMode = BindingMode.OneWay;

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        /// <value>
        /// The mode.
        /// </value>
        public BindingMode Mode
        {
            get { return _mBindingMode; }
            set { CheckSealed(); _mBindingMode = value; }
        }


        private object _mTargetNullValue;

        /// <summary>
        /// Gets or sets the value to use when the converted value is null.
        /// </summary>
        /// <value>
        /// The target null value.
        /// </value>
        public object TargetNullValue
        {
            get { return _mTargetNullValue; }
            set { CheckSealed(); _mTargetNullValue = value; }
        }

        private CultureInfo _mConverterCulture;

        /// <summary>
        /// Gets or sets the converter culture.
        /// </summary>
        /// <value>
        /// The converter culture.
        /// </value>
        public CultureInfo ConverterCulture
        {
            get { return _mConverterCulture; }
            set { CheckSealed(); _mConverterCulture = value; }
        }

        private bool _mValidatesOnExceptions;

        /// <summary>
        /// Gets or sets a value indicating whether exceptions thrown during conversion is to cause validation errors or not.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [validates on exceptions]; otherwise, <c>false</c>.
        /// </value>
        public bool ValidatesOnExceptions
        {
            get { return _mValidatesOnExceptions; }
            set { CheckSealed(); _mValidatesOnExceptions = value; }
        }

        private bool _mNotifyOnValidationError;

        /// <summary>
        /// Gets or sets a value indicating whether an event should be raised on validation errors.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [notify on validation error]; otherwise, <c>false</c>.
        /// </value>
        public bool NotifyOnValidationError
        {
            get { return _mNotifyOnValidationError; }
            set { CheckSealed();  _mNotifyOnValidationError = value; }
        }

        private object _mFallbackValue;

        /// <summary>
        /// Gets or sets the fallback value,i .e. the value to use when no value is available.
        /// </summary>
        /// <value>
        /// The fallback value to use when the Converter returns a DependencyProperty.UnsetValue.
        /// </value>
        public object FallbackValue
        {
            get { return _mFallbackValue; }
            set { CheckSealed(); _mFallbackValue = value; }
        }

        private UpdateSourceTrigger _mUpdateSourceTrigger = UpdateSourceTrigger.Default;

        /// <summary>
        /// Gets or sets the update source trigger determing when the sources are updated in a two-way binding. 
        /// </summary>
        /// <value>
        /// The update source trigger.
        /// </value>
        public UpdateSourceTrigger UpdateSourceTrigger
        {
            get { return _mUpdateSourceTrigger; }
            set { CheckSealed(); _mUpdateSourceTrigger = value; }
        }
        
        /// <summary>
        /// Maximum number of multibindings that can be used in a single Style. As the styleMultiBindingProperties will be
        /// used in a round-robbin fashion using more multibindings in a a single Style will result in the first bindings
        /// will be overwritten by later bindings.
        /// </summary>
        private const int MAX_STYLE_MULTI_BINDINGS_COUNT = 8;

        static MultiBindingCompatible()
        {
            // Create Style Multi Binding attached properties
            StyleMultBindingProperties = new DependencyProperty[MAX_STYLE_MULTI_BINDINGS_COUNT];
            for (int i = 0; i < MAX_STYLE_MULTI_BINDINGS_COUNT; i++)
            {
                StyleMultBindingProperties[i] = DependencyProperty.RegisterAttached("StyleMultiBinding"+i, 
                                                                                    typeof(MultiBindingCompatible), 
                                                                                    typeof(MultiBindingCompatible), 
                                                                                    new PropertyMetadata(null, OnStyleMultiBindingChanged));
            }
        }

        public static MultiBindingCompatible GetStyleBinding(DependencyObject obj)
        {
            return (MultiBindingCompatible)obj.GetValue(StyleBindingProperty);
        }

        public static void SetStyleBinding(DependencyObject obj, MultiBindingCompatible value)
        {
            obj.SetValue(StyleBindingProperty, value);
        }

        // Using a DependencyProperty as the backing store for Multibinding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StyleBindingProperty = DependencyProperty.RegisterAttached("StyleBinding",
                                                                                                             typeof(MultiBindingCompatible),
                                                                                                             typeof(MultiBindingCompatible), 
                                                                                                             new PropertyMetadata(null, OnMultiBindingChanged));

        private static void OnMultiBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var setter = d as Setter;
            if( setter == null || setter.Property == null || args.OldValue != null)
            {
                throw new InvalidOperationException("Can only apply Multibinding attached property on a Setter where the Property has been set before and no value has previosly been assigned to the Multibinding property.");
            }
            if( args.NewValue != null )
            {
                if (DesignerProperties.IsInDesignTool) return;

                var mb = (MultiBindingCompatible)args.NewValue;
                mb.ApplyToStyle(setter);
                setter.Value = mb;
            }
        }

        private static readonly DependencyProperty[] StyleMultBindingProperties;

        private static int _currentStyleMultiBindingIndex = 0;

        private static void OnStyleMultiBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var mb = (MultiBindingCompatible)args.NewValue;
            if (mb != null)
            {
                // Only apply multibinding from Style if no local value has been set.
                object existingValue = d.ReadLocalValue(mb._mStyleProperty);
                if (existingValue == DependencyProperty.UnsetValue)
                {
                    // Apply binding to target by creating MultiBindingExpression and attached data properties.
                    Binding resultBinding = mb.ApplyToTarget(d);
                    
                    BindingOperations.SetBinding(d, mb._mStyleProperty, resultBinding);
                    
                }
            }
        }

        /// <summary>
        /// Gets or sets the converter to use be used to convert between source values and target value (and vice versa).
        /// </summary>
        /// <value>
        /// The converter.
        /// </value>
        /// <remarks>
        /// The converter must either implement the <see cref="IMultiValueConverter"/> interface or the IValueConverter interface.
        /// 
        /// This property is bindable, i.e. you may specify it as a Binding relative to the MultiBinding target element.
        /// </remarks>
        public Object Converter
        {
            get { return GetValue(ConverterProperty); }
            set { SetValue(ConverterProperty, value); }
        }

        /// <summary>
        /// Represents Converter dependency property.
        /// </summary>
        public static readonly DependencyProperty ConverterProperty = DependencyProperty.Register("Converter", 
                                                                                                  typeof(Object), 
                                                                                                  typeof(MultiBindingCompatible), 
                                                                                                  new PropertyMetadata(null, OnDependencyPropertyChanged));
        /// <summary>
        /// Gets or sets the parameter to pass in to the Converter.
        /// </summary>
        /// <value>
        /// The converter parameter.
        /// </value>
        /// <remarks>
        /// This property is bindable, i.e. you may specify it as a Bindingin relative to the MultiBinding target element.
        /// </remarks>
        public object ConverterParameter
        {
            get { return (object)GetValue(ConverterParameterProperty); }
            set { SetValue(ConverterParameterProperty, value); }
        }

        /// <summary>
        /// Represents the ConverterParameter dependency property.
        /// </summary>
        public static readonly DependencyProperty ConverterParameterProperty = DependencyProperty.Register("ConverterParameter", 
                                                                                                           typeof(object),
                                                                                                           typeof(MultiBindingCompatible),
                                                                                                           new PropertyMetadata(null, OnDependencyPropertyChanged));
        /// <summary>
        /// Gets or sets a formatting string to be applied to result of the conversion. This property is bindable.
        /// </summary>
        /// <remarks>
        /// The String format follows the convention for String.Format formatting. Optionally, individual source values can be 
        /// referred to with %n-syntax where n is the zero-based index of the source value.
        /// 
        /// In case a Converter is specified the StringFormat is applied to result of the conversion.
        /// </remarks>
        public String StringFormat
        {
            get { return (String)GetValue(StringFormatProperty); }
            set { SetValue(StringFormatProperty, value); }
        }

        /// <summary>
        /// Represents the StringFormat dependency property.
        /// </summary>
        public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register("StringFormat", 
                                                                                                     typeof(String), 
                                                                                                     typeof(MultiBindingCompatible), 
                                                                                                     new PropertyMetadata(null, OnDependencyPropertyChanged));
     
        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var mb = (MultiBindingCompatible)d;
            mb.CheckSealed();
        }

        /// <summary>
        /// The dependency property this MultiBinding is to be applied to when using in a Style setter.
        /// </summary>
        private DependencyProperty _mStyleProperty;

        private static readonly PropertyInfo SetterValueProperty = typeof(Setter).GetProperty("Value");

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            var pvt = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            if (pvt == null)
            {
                throw new InvalidOperationException("MultiBinding cannot determine the binding target object since the IProviderValueTarget service is unavailable.");
            }

            if (pvt.TargetObject is Setter && pvt.TargetProperty == SetterValueProperty)
            {
                var setter = (Setter)pvt.TargetObject;

                if (DesignerProperties.IsInDesignTool)
                {
                    return new Binding().ProvideValue(serviceProvider);
                }
                ApplyToStyle(setter);
                return this;
            }
            if( pvt.TargetObject is Setter )
            {
                return this;
            }

            if (DesignerProperties.IsInDesignTool)
            {
                var propertyInfo = pvt.TargetProperty as PropertyInfo;
                object defaultValue;
                if (propertyInfo.PropertyType.IsValueType)
                {
                    defaultValue = propertyInfo.PropertyType.GetConstructor(Type.EmptyTypes).Invoke(null);
                }
                else
                {
                    defaultValue = null;
                }
                return defaultValue;
            }  

            var target = pvt.TargetObject as DependencyObject;
            if (target == null)
            {
                throw new InvalidOperationException("MultiBinding can only be applied to DependencyObjects.");
            }
            
            Binding resultBinding = ApplyToTarget(target);
            return resultBinding.ProvideValue(serviceProvider);
        }

        private void ApplyToStyle(Setter setter)
        {
            _mStyleProperty = setter.Property;
            if (_mStyleProperty == null)
            {
                throw new InvalidOperationException("When a MultiBinding is applied to a Style setter the Property must be set first.");
            }
            setter.Property = StyleMultBindingProperties[_currentStyleMultiBindingIndex];
            _currentStyleMultiBindingIndex = (_currentStyleMultiBindingIndex + 1) % MAX_STYLE_MULTI_BINDINGS_COUNT;
            Seal();
        }

        /// <summary>
        /// Applies this MultiBinding to a target object.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="targetType">The Type of the target dependency property.</param>
        /// <returns>A Binding specific for this target</returns>
        public Binding ApplyToTarget(DependencyObject target)
        {
            Seal();
            // Create new MultiBindingInfo to hold information about this multibinding
            var newInfo = new MultiBindingExpression(target, this);
            
            // Create new binding to expressions's SourceValues property
            var path = new PropertyPath("SourceValues");
            var resultBinding = new Binding
            {
                Converter = newInfo,
                Path = path,
                Source = newInfo,
                Mode = Mode,
                UpdateSourceTrigger = UpdateSourceTrigger,
                TargetNullValue = TargetNullValue,
                ValidatesOnExceptions = ValidatesOnExceptions,
                ValidatesOnNotifyDataErrors = true,
                NotifyOnValidationError = NotifyOnValidationError,
                ConverterCulture = ConverterCulture,
                FallbackValue = FallbackValue
            };

            newInfo.Update();
            return resultBinding;
        }
    }
#endif
}