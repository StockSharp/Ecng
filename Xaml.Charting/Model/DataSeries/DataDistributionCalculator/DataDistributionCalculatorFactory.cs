using System;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    internal static class DataDistributionCalculatorFactory
    {
        internal static IDataDistributionCalculator<TX> Create<TX>(bool isFifo)
            where TX : IComparable
        {
            if (typeof(TX) == typeof(float))
                return isFifo ? (IDataDistributionCalculator<TX>)new SingleDataDistributionCalculator() : new ListSingleDataDistributionCalculator() as IDataDistributionCalculator<TX>;                            
            if (typeof (TX) == typeof (double))
                return isFifo ? (IDataDistributionCalculator<TX>)new DoubleDataDistributionCalculator() : new ListDoubleDataDistributionCalculator() as IDataDistributionCalculator<TX>;                            
            if (typeof(TX) == typeof(Decimal))
                return isFifo ? (IDataDistributionCalculator<TX>)new DecimalDataDistributionCalculator() : new ListDecimalDataDistributionCalculator() as IDataDistributionCalculator<TX>;                            
            if (typeof(TX) == typeof(Int16))
                return isFifo ? (IDataDistributionCalculator<TX>)new Int16DataDistributionCalculator() : new ListInt16DataDistributionCalculator() as IDataDistributionCalculator<TX>;                            
            if (typeof(TX) == typeof(Int32))
                return isFifo ? (IDataDistributionCalculator<TX>)new Int32DataDistributionCalculator() : new ListInt32DataDistributionCalculator() as IDataDistributionCalculator<TX>;                            
            if (typeof(TX) == typeof(Int64))
                return isFifo ? (IDataDistributionCalculator<TX>)new Int64DataDistributionCalculator() : new ListInt64DataDistributionCalculator() as IDataDistributionCalculator<TX>;                            
            if (typeof(TX) == typeof(UInt16))
                return isFifo ? (IDataDistributionCalculator<TX>)new UInt16DataDistributionCalculator() : new ListUInt16DataDistributionCalculator() as IDataDistributionCalculator<TX>;                            
            if (typeof(TX) == typeof(UInt32))
                return isFifo ? (IDataDistributionCalculator<TX>)new UInt32DataDistributionCalculator() : new ListUInt32DataDistributionCalculator() as IDataDistributionCalculator<TX>;                            
            if (typeof(TX) == typeof(UInt64))
                return isFifo ? (IDataDistributionCalculator<TX>)new UInt64DataDistributionCalculator() : new ListUInt64DataDistributionCalculator() as IDataDistributionCalculator<TX>;                            
            if (typeof(TX) == typeof(Byte))
                return isFifo ? (IDataDistributionCalculator<TX>)new ByteDataDistributionCalculator() : new ListByteDataDistributionCalculator() as IDataDistributionCalculator<TX>;                            
            if (typeof(TX) == typeof(SByte))
                return isFifo ? (IDataDistributionCalculator<TX>)new SByteDataDistributionCalculator() : new ListSByteDataDistributionCalculator() as IDataDistributionCalculator<TX>;                            
            if (typeof(TX) == typeof(DateTime))
                return isFifo ? (IDataDistributionCalculator<TX>)new DateTimeDataDistributionCalculator() : new ListDateTimeDataDistributionCalculator() as IDataDistributionCalculator<TX>;                            
            if (typeof(TX) == typeof(TimeSpan))
                return isFifo ? (IDataDistributionCalculator<TX>)new TimeSpanDataDistributionCalculator() : new ListTimeSpanDataDistributionCalculator() as IDataDistributionCalculator<TX>;                            

            throw new NotImplementedException(string.Format("Cannot create a DataDistributionCalculator for the type TX={0}", typeof(TX)));
        }
    }
}