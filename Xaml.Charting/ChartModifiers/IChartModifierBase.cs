using System.ComponentModel;
using Ecng.Xaml.Charting.Utility.Mouse;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// A Base interface for common shared properties between ChartModifiers in the 2D and 3D Ultrachart libraries 
    /// </summary>
    public interface IChartModifierBase : IReceiveMouseEvents, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the <see cref="ServiceContainer"/> which provides access to services throughout Ultrachart. 
        /// ServiceContainers are created one per <see cref="UltrachartSurface"/> instance, 
        /// and shared between peripheral components such as <see cref="AxisBase"/>, <see cref="BaseRenderableSeries"/>, <see cref="ChartModifierBase"/> instances.
        /// For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
        /// </summary>
        IServiceContainer Services { get; set; }

        /// <summary>
        /// Gets the <see cref="IChartModifierSurface"/> instance on the parent <see cref="UltrachartSurface"/>, which acts as a canvas to place UIElements
        /// </summary>
        IChartModifierSurface ModifierSurface { get; }

        /// <summary>
        /// Gets modifier name
        /// </summary>
        string ModifierName { get; }

        /// <summary>
        /// Gets or sets whether this Chart Modifier is attached to a parent <see cref="UltrachartSurface"/>
        /// </summary>
        bool IsAttached { get; set; }

        /// <summary>
        /// Gets or sets the DataContext for this Chart Modifier 
        /// </summary>
        object DataContext { get; set; }

        /// <summary>
        /// If true, this <see cref="IChartModifier"/> can receive handled events. Chart modifiers work similarly to mouse event handlers in WPF and Silverlight. If a modifier
        /// further up the stack receives an event and handles it, then subsequent modifiers do not receive the event. This property overrides this behaviour. 
        /// </summary>
        bool ReceiveHandledEvents { get; }

        /// <summary>
        /// Called when the Chart Modifier is attached to the Chart Surface
        /// </summary>
        void OnAttached();

        /// <summary>
        /// Called immediately before the Chart Modifier is detached from the Chart Surface
        /// </summary>
        void OnDetached();
    }
}