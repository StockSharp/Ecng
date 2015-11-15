using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    public abstract class LabelFormatterBase: ILabelFormatter
    {
        private IAxis _parentAxis;

        public IAxis ParentAxis
        {
            get { return _parentAxis; }
            protected set { _parentAxis = value; }
        }

        /// <summary>
        /// Called when the label formatted is initialized, with the parent axis instance
        /// </summary>
        /// <param name="parentAxis">The parent <see cref="IAxis" /> instance</param>
        public virtual void Init(IAxis parentAxis)
        {
            ParentAxis = parentAxis;
        }

        /// <summary>
        /// Called at the start of an axis render pass, before any labels are formatted for the current draw operation
        /// </summary>
        public virtual void OnBeginAxisDraw(){}

        /// <summary>
        /// Creates a <see cref="ITickLabelViewModel"/> instance, based on the data-value passed in.
        /// Invokes <see cref="FormatLabel"/> to format the specified data-value passed in.
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        public virtual ITickLabelViewModel CreateDataContext(IComparable dataValue)
        {
            return UpdateDataContext(new DefaultTickLabelViewModel(), dataValue);
        }

        /// <summary>
        /// Updates existing <see cref="ITickLabelViewModel"/>, based on the data-value passed in.
        /// Invokes <see cref="FormatLabel"/> to format the specified data-value passed in.
        /// </summary>
        /// <param name="labelDataContext">The instance to update</param>
        /// <param name="dataValue">The data-value to format</param>
        public virtual ITickLabelViewModel UpdateDataContext(ITickLabelViewModel labelDataContext, IComparable dataValue)
        {
            var formatted = FormatLabel(dataValue);
            labelDataContext.Text = formatted;

            return labelDataContext;
        }

        public abstract string FormatLabel(IComparable dataValue);

        public abstract string FormatCursorLabel(IComparable dataValue);
    }
}
