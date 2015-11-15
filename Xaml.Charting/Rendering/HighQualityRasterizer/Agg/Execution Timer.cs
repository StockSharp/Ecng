//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2007 Lars Brubaker
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
// Contact: larsbrubaker@gmail.com
//----------------------------------------------------------------------------
// Description:	A simple performance timer.
//*********************************************************************************************************************
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MatterHackers.Agg
{
    internal class NamedExecutionTimer
    {
        internal long LastStartTicks;
        internal uint RecurseLevel;
        internal uint NumStarts;

        internal String m_Name;

        internal double TotalRunningTime;
        internal double TotalTimeExcludingSubroutines;

        static Stopwatch singleStopWatch;

        long GetCurrentTicks()
        {
            if (singleStopWatch == null)
            {
                singleStopWatch = new Stopwatch();
                singleStopWatch.Start();
            }

            return singleStopWatch.ElapsedTicks;
        }

        public NamedExecutionTimer(string Name)
        {
            Reset();
            m_Name = Name;

            ExecutionTimer.Instance.AddTimer(this);
        }

        public void Start()
        {
            if (RecurseLevel != 0)
            {
                throw new System.NotImplementedException();
            }
            else
            {
                LastStartTicks = GetCurrentTicks();
                ExecutionTimer.Instance.Starting(this);
                NumStarts++;
            }

            RecurseLevel++;
        }

        public bool IsRunning()
        {
            return LastStartTicks > 0;
        }

        public void Stop()
        {
            if (RecurseLevel == 0)
            {
                // You tried to exit without ever entering?
                throw new System.InvalidOperationException();
            }

            RecurseLevel--;
            if (RecurseLevel == 0)
            {
                long TotalTicks = GetCurrentTicks() - LastStartTicks;
                LastStartTicks = 0;

                double TimeToAdd = (double)((double)TotalTicks / (double)Stopwatch.Frequency);
                TotalRunningTime += TimeToAdd;
                TotalTimeExcludingSubroutines += TimeToAdd;
                ExecutionTimer.Instance.Stoping(this, TimeToAdd);
            }
        }

        internal void Reset()
        {
            LastStartTicks = 0;
            RecurseLevel = 0;
            NumStarts = 0;
            TotalRunningTime = 0.0f;
            TotalTimeExcludingSubroutines = 0.0f;
        }

        public double GetTotalSeconds()
        {
            return TotalRunningTime;
        }

        public double GetTotalSecondsExcludingSubroutines()
        {
            return TotalTimeExcludingSubroutines;
        }
    }

    internal sealed class ExecutionTimer
    {
        List<NamedExecutionTimer> CallStack;
        List<NamedExecutionTimer> NamedTimerList;
        static readonly ExecutionTimer instanceExecutionTimer = new ExecutionTimer();

        private ExecutionTimer()
        {
            NamedTimerList = new List<NamedExecutionTimer>();
            CallStack = new List<NamedExecutionTimer>();
        }

        public static ExecutionTimer Instance
        {
            get
            {
                return instanceExecutionTimer;
            }
        }

        internal void Starting(NamedExecutionTimer namedTimer)
        {
            CallStack.Add(namedTimer);
        }

        internal void Stoping(NamedExecutionTimer namedTimer, double timeThisRun)
        {
            int Count = CallStack.Count;
            if(Count > 1)
            {
                CallStack[Count - 2].TotalTimeExcludingSubroutines -= timeThisRun;
            }
            CallStack.RemoveAt(Count - 1);
        }

        internal void AddTimer(NamedExecutionTimer namedTimer)
        {
            NamedTimerList.Add(namedTimer);
        }

        public void Reset()
        {
            foreach (NamedExecutionTimer namedTimer in NamedTimerList)
            {
                namedTimer.Reset();
            }
        }

        public string GetResults(double totalTime)
        {
            StringBuilder OutString = new StringBuilder();

            OutString.Append("***************************************\n");

            OutString.Append("Total  | No Subs| %Total |%No Subs| Name\n");

            foreach (NamedExecutionTimer NamedTimer in NamedTimerList)
            {
                if (NamedTimer.GetTotalSeconds() > 0)
                {
                    OutString.Append(String.Format("{0:000.00}", NamedTimer.GetTotalSeconds() * 1000)
                        + " | " + String.Format("{0:000.00}", NamedTimer.GetTotalSecondsExcludingSubroutines() * 1000)
                        + " | " + String.Format("{0:00.00}", System.Math.Min(99.99, NamedTimer.GetTotalSeconds() / totalTime * 100)) + "%"
                        + " | " + String.Format("{0:00.00}", NamedTimer.GetTotalSecondsExcludingSubroutines() / totalTime * 100) + "%"
                        + " | "
                        + NamedTimer.m_Name);

                    OutString.Append(" (" + NamedTimer.NumStarts.ToString() + ")\n");
                }
            }

            OutString.Append("\n\n");

            return OutString.ToString();
        }

	    public void AppendResultsToFile(String fileName, double totalTime)
        {
            FileStream file;
            file = new FileStream(fileName, FileMode.Append, FileAccess.Write);
            StreamWriter sw = new StreamWriter(file);

            sw.Write(GetResults(totalTime));

            sw.Close();
            file.Close();
        }
    }
}
