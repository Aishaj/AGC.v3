using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AGC.Library
{
    class FreeTime : IEquatable<FreeTime> , IComparable
    {
        #region Constants

        private const bool FirstDayOfWeekIsMonday = true;

        #endregion

        #region Private Variables

        private DateTime start;
        private DateTime end;
        private String email;
        private static bool TodayIsSunday = (int)DateTime.Today.DayOfWeek == 0;
        

        #endregion

        #region Public Fields

        public DateTime Start { get { return start; } }

        public DateTime End { get { return end; } }
        
        public String Email { get { return email; } }

        public void setStart(DateTime startValue) { start = startValue; }

        public void setEnd(DateTime EndValue) { end = EndValue; }

        public void setEmail(String calendarValue) { email = calendarValue; }

        #endregion

        #region Constructor

        public FreeTime()
        {
            start = end = DateTime.Today;
        }

        public FreeTime(DateTime s,DateTime e)
        {
            start = s;
            end = e;
        }

        #endregion

        public enum PeriodType
        {
            SingleMonth,
            AllMonths,
            InterveningMonths
        }
        public bool Equals(FreeTime other)
        {

            ////Check whether the compared object is null.  
            //if (Object.ReferenceEquals(other, null)) return false;

            //Check wether the products' properties are equal.  
            return start.Equals(other.start) && end.Equals(other.end);
        }

        public override bool Equals(object obj)
        {
            if (obj is FreeTime)
                return false;
            return Equals((FreeTime)obj);
        }


        public override int GetHashCode()
        {

            //Get hash code for the start field if it is not null.  
            int hashStart = start.GetHashCode();

            //Get hash code for the end field.  
            int hashEnd = end.GetHashCode();

            //Calculate the hash code .  
            return hashStart ^ hashEnd;
        }


        public FreeTime  intersection(FreeTime first, FreeTime second)
        {

            int result1 = DateTime.Compare(first.start, second.end);
            int result2 = DateTime.Compare(first.end, second.start);

            if (!(result1 >= 0) && !(result2 <= 0) && (first != second))
                return new FreeTime(new DateTime(Math.Max(first.start.Ticks, second.start.Ticks)), new DateTime(Math.Min(first.end.Ticks, second.end.Ticks)));
            
            
            else return null;       

            
        }




        public FreeTime Today()
        {
            start = DateTime.Today;
            end = DateTime.Today.AddDays(1).AddSeconds(-1);
            return this;
        }

        public FreeTime Tomorrow()
        {
            start = DateTime.Today.AddDays(1);
            end = DateTime.Today.AddDays(2).AddSeconds(-1);
            return this;
        }

        public FreeTime ThisWeek()
        {
            if (FirstDayOfWeekIsMonday && TodayIsSunday)
            {
                start = DateTime.Today.AddDays(-6);
            }
            else
            {
                start = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            }
            
            end = start.AddDays(7).AddSeconds(-1);
            return this;
        }

        public FreeTime NextWeek()
        {
            if (FirstDayOfWeekIsMonday && TodayIsSunday)
            {
                start = DateTime.Today.AddDays(1);
            }
            else
            {
                start = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek).AddDays(7);
            }

            end = start.AddDays(7).AddSeconds(-1);
            return this;
        }

        public FreeTime ThisMonth()
        {
            MonthsPeriod(DateTime.Today.Month, PeriodType.SingleMonth);
            return this;
        }

        public FreeTime NextMonth()
        {
            MonthsPeriod(DateTime.Today.AddMonths(1).Month, PeriodType.SingleMonth);
            return this;
        }

        public FreeTime MonthsPeriod(int endMonth, PeriodType periodType)
        {
            // Add 1 Month, because month lists starts with 0, end month should be in range 1-12
            endMonth++;

            int currentMonth = DateTime.Today.Month;
            int numberOfMonthToAdd = endMonth > currentMonth ? endMonth - currentMonth : (12 - currentMonth) + endMonth;

            start = DateTime.Today.AddDays(1 - DateTime.Today.Day);
            end = start.AddMonths(numberOfMonthToAdd);

            switch (periodType)
            {
                case PeriodType.SingleMonth:
                    {
                        start = end.AddDays(1 - end.Day).AddMonths(-1);
                        break;
                    }
                case PeriodType.AllMonths:
                    {
                        break;
                    }
                case PeriodType.InterveningMonths:
                    {
                        start = start.AddMonths(1);
                        end = end.AddMonths(-1);
                        break;
                    }
            }

            if (start.Month == DateTime.Today.Month)
            {
                start = DateTime.Today;
            }

            end = end.AddSeconds(-1);
            
            return this;
        }

        public FreeTime All()
        {
            start = DateTime.Today.AddYears(-4);
            end = DateTime.Today.AddYears(4);
            return this;
        }

        public void WriteConsoleLog()
        {
            Console.WriteLine("START: {0:dd.MM.yy HH:mm:ss zzz} END: {1:dd.MM.yy HH:mm:ss zzz}", start, end); 
        }




        public int CompareTo(object obj)
        {
            FreeTime f = obj as FreeTime;
            return this.start.CompareTo(f.start);
        }

    }
}


     