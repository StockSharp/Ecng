using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ecng.Xaml.Charting.Common.Helpers;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    interface ITickLabelsPool
    {
        /// <summary>
        /// Gets the summary amount of created instances
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the amount of pooled instances
        /// </summary>
        int AvailableCount { get; }

        /// <summary>
        /// Gets the value indicating whether current <see cref="ITickLabelsPool"/> instance is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Retrieves an item from the pool. 
        /// </summary>
        /// <returns>The item retrieved from the pool.</returns>
        DefaultTickLabel Get();

        /// <summary>
        /// Retrieves an item from the pool. 
        /// </summary>
        /// <returns>The item retrieved from the pool.</returns>
        DefaultTickLabel Get(Func<DefaultTickLabel, DefaultTickLabel> actionOnCreation);

        /// <summary>
        /// Places an item in the pool.
        /// </summary>
        /// <param name="item">The item to place to the pool.</param>
        void Put(DefaultTickLabel item);

        /// <summary>
        /// Disposes of items in the pool that implement IDisposable.
        /// </summary>
        void Dispose();
    }

    class TickLabelsPool<T> : ObjectPool<T>, ITickLabelsPool where T:DefaultTickLabel, new()
    {
        public new DefaultTickLabel Get()
        {
            return base.Get();
        }

        public DefaultTickLabel Get(Func<DefaultTickLabel, DefaultTickLabel> actionOnCreation)
        {
            return base.Get(T => (T)actionOnCreation(T));
        }

        public void Put(DefaultTickLabel item)
        {
            base.Put((T)item);
        }
    }
}
