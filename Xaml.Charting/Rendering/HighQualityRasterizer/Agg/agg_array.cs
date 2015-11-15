//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# Port port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
//
// Permission to copy, use, modify, sell and distribute this software 
// is granted provided this copyright notice appears in all copies. 
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------
using System;

namespace MatterHackers.Agg
{
    internal interface IDataContainer<T>
    {
        T[] Array { get; }
        void RemoveLast();
    }

    internal class ArrayPOD<T> : IDataContainer<T>
    {
        public ArrayPOD()
            : this(64)
        {
        }

        public ArrayPOD(int size)
        {
            m_array = new T[size];
            m_size = size;
        }

        public void RemoveLast()
        {
            throw new NotImplementedException();
        }

        public ArrayPOD(ArrayPOD<T> v)
        {
            m_array = (T[])v.m_array.Clone();
            m_size = v.m_size;
        }

        public void Resize(int size)
        {
            if(size != m_size)
            {
                m_array = new T[size];
            }
        }

        public int Size() { return m_size; }

        public T this[int index]
        {
            get
            {
                return m_array[index];
            }

            set
            {
                m_array[index] = value;
            }
        }

        public T[] Array
        {
            get
            {
                return m_array;
            }
        }
        private T[] m_array;
        private int m_size;
    };


    //--------------------------------------------------------------pod_vector
    // A simple class template to store Plain Old Data, a vector
    // of a fixed size. The data is contiguous in memory
    //------------------------------------------------------------------------
    internal class VectorPOD<dataType> : IDataContainer<dataType>
    {
        protected int currentSize;
        private dataType[] internalArray = new dataType[0];

        public int AllocatedSize
        {
            get
            {
                return internalArray.Length;
            }
        }

        public VectorPOD()
        {
        }

        public VectorPOD(int cap)
            : this(cap, 0)
        {
        }

        public VectorPOD(int capacity, int extraTail)
        {
            Allocate(capacity, extraTail);
        }

        public virtual void Remove(int indexToRemove)
        {
            if (indexToRemove >= Length)
            {
                throw new Exception("requested remove past end of array");
            }

            for (int i = indexToRemove; i < Length-1; i++ )
            {
                internalArray[i] = internalArray[i + 1];
            }
            
            currentSize--;
        }

        public virtual void RemoveLast()
        {
            if (currentSize != 0)
            {
                currentSize--;
            }
        }

        // Copying
        public VectorPOD(VectorPOD<dataType> vectorToCopy)
        {
            currentSize = vectorToCopy.currentSize;
            internalArray = (dataType[])vectorToCopy.internalArray.Clone();
        }

        public void CopyFrom(VectorPOD<dataType> vetorToCopy)
        {
            Allocate(vetorToCopy.currentSize);
            if (vetorToCopy.currentSize != 0)
            {
                vetorToCopy.internalArray.CopyTo(internalArray, 0);
            }
        }

        // Set new capacity. All data is lost, size is set to zero.
        public void Capacity(int newCapacity)
        {
            Capacity(newCapacity, 0);
        }

        public void Capacity(int newCapacity, int extraTail)
        {
            currentSize = 0;
            if (newCapacity > AllocatedSize)
            {
                internalArray = null;
                int sizeToAllocate = newCapacity + extraTail;
                if (sizeToAllocate != 0)
                {
                    internalArray = new dataType[sizeToAllocate];
                }
            }
        }

        public int Capacity() { return AllocatedSize; }

        // Allocate n elements. All data is lost, 
        // but elements can be accessed in range 0...size-1. 
        public void Allocate(int size)
        {
            Allocate(size, 0);
        }

        public void Allocate(int size, int extraTail)
        {
            Capacity(size, extraTail);
            currentSize = size;
        }

        // Resize keeping the content.
        public void Resize(int newSize)
        {
            if(newSize > currentSize)
            {
                if(newSize > AllocatedSize)
                {
                    var newArray = new dataType[newSize];
                    if (internalArray != null)
                    {
                        for (int i = 0; i < internalArray.Length; i++)
                        {
                            newArray[i] = internalArray[i];
                        }
                    }
                    internalArray = newArray;
                }
            }
        }

#pragma warning disable 649
        static dataType zeroed_object;
#pragma warning restore 649

        public void zero()
        {
            int NumItems = internalArray.Length;
            for(int i=0; i<NumItems; i++)
            {
                internalArray[i] = zeroed_object;
            }
        }

        public virtual void add(dataType v) 
        {
            if (internalArray == null || internalArray.Length < (currentSize + 1))
            {
                Resize(currentSize + (currentSize / 2) + 16);
            }
            internalArray[currentSize++] = v;
        }

        public void push_back(dataType v) { internalArray[currentSize++] = v; }
        public void insert_at(int pos, dataType val)
        {
            if (pos >= currentSize)
            {
                internalArray[currentSize] = val;
            }
            else
            {
                for (int i = 0; i < currentSize - pos; i++)
                {
                    internalArray[i + pos + 1] = internalArray[i + pos];
                }
                internalArray[pos] = val;
            }
            ++currentSize;
        }

        public void inc_size(int size) { currentSize += size; }
        public int size() { return currentSize; }

        public dataType this[int i] 
        { 
            get 
            {
                return internalArray[i];
            }
        }

        public dataType[] Array
        {
            get
            {
                return internalArray;
            }
        }

        public dataType at(int i) { return internalArray[i]; }
        public dataType value_at(int i) { return internalArray[i]; }

        public dataType[] data() { return internalArray; }

        public void remove_all() { currentSize = 0; }
        public void clear() { currentSize = 0; }
        public void cut_at(int num) { if (num < currentSize) currentSize = num; }

        public int Length 
        {
            get
            {
                return currentSize;
            }
        }
    };

    //----------------------------------------------------------range_adaptor
    internal class VectorPOD_RangeAdaptor
    {
        VectorPOD<int> m_array;
        int m_start;
        int m_size;

        public VectorPOD_RangeAdaptor(VectorPOD<int> array, int start, int size)
        {
            m_array=(array);
            m_start=(start);
            m_size=(size);
        }

        public int size() { return m_size; }
        public int this[int i] 
        {
            get
            {
                return m_array.Array[m_start + i];
            }

            set
            {
                m_array.Array[m_start + i] = value;
            }
        }
        public int at(int i) { return m_array.Array[m_start + i]; }
        public int value_at(int i) { return m_array.Array[m_start + i]; }
    };

    internal class FirstInFirstOutQueue<T>
    {
        T[] itemArray;
        int size;
        int head;
        int shiftFactor;
        int mask;

        public int Count
        {
            get { return size; }
        }

        public FirstInFirstOutQueue(int shiftFactor)
        {
            this.shiftFactor = shiftFactor;
            mask = (1 << shiftFactor) - 1;
            itemArray = new T[1 << shiftFactor];
            head = 0;
            size = 0;
        }

        public T First
        {
            get { return itemArray[head & mask]; }
        }

        public void Enqueue(T itemToQueue)
        {
            if (size == itemArray.Length)
            {
                int headIndex = head & mask;
                shiftFactor += 1;
                mask = (1 << shiftFactor) - 1;
                T[] newArray = new T[1 << shiftFactor];
                // copy the from head to the end
                Array.Copy(itemArray, headIndex, newArray, 0, size - headIndex);
                // copy form 0 to the size
                Array.Copy(itemArray, 0, newArray, size - headIndex, headIndex);
                itemArray = newArray;
                head = 0;
            }
            itemArray[(head + (size++)) & mask] = itemToQueue;
        }

        public T Dequeue()
        {
            int headIndex = head & mask;
            T firstItem = itemArray[headIndex];
            if (size > 0)
            {
                head++;
                size--;
            }
            return firstItem;
        }
    }
}
