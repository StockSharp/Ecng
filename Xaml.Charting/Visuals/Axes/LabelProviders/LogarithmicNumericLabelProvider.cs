using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Visuals.Axes.LabelProviders
{
    public class LogarithmicNumericLabelProvider: NumericLabelProvider
    {
        public override void Init(IAxis parentAxis)
        {
            if (!(parentAxis is ILogarithmicAxis))
            {
                throw new ArgumentException(
                    "LogarithmicNumericLabelProvider can be used with the LogarithmicNumericAxis only.");
            }

            base.Init(parentAxis);
        }

        public override ITickLabelViewModel UpdateDataContext(ITickLabelViewModel labelDataContext, IComparable dataValue)
        {
            var logAxis = ParentAxis as ILogarithmicAxis;

            var label = (NumericTickLabelViewModel)labelDataContext;
            label.HasExponent = logAxis != null && logAxis.ScientificNotation == ScientificNotation.LogarithmicBase;

            if (label.HasExponent)
            {
                var formatStr = ParentAxis.TextFormatting;
                var index = formatStr.IndexOfAny(new[] { 'e', 'E' });

                var sigFormatting = formatStr.Substring(0, index);
                var expFormatting = formatStr.Substring(index + 1);

                var negFormat = "###.##";
                var posFormat = negFormat;
                if (expFormatting.StartsWith("+"))
                {
                    posFormat = "+" + posFormat;
                }

                expFormatting = posFormat + ";-" + negFormat + ";0";

                var value = dataValue.ToDouble();

                var exponent = Math.Log(value, logAxis.LogarithmicBase);
                var sig = value / Math.Pow(logAxis.LogarithmicBase, exponent);

                label.HasExponent = true;

                label.Text = String.Format(CultureInfo.InvariantCulture, "{0:"+ sigFormatting+ "}x", sig);
                label.Exponent = String.Format(CultureInfo.InvariantCulture, "{0:" + expFormatting + "}", exponent);

                label.Separator = logAxis.LogarithmicBase.Equals(Math.E)
                    ? formatStr.Substring(index, 1)
                    : logAxis.LogarithmicBase.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                labelDataContext = base.UpdateDataContext(labelDataContext, dataValue);
            }

            return labelDataContext;
        }
    }
}
