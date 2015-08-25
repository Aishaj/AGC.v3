using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AGC.Library;
using AGC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace AGC.Library.Tests
{
    [TestClass()]
    public class GoogleCalendarTests
    {

        #region Private Variables

//private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static GoogleCalendar calendar = new GoogleCalendar();
        private static TimeIntervals period = new TimeIntervals();
        private static CalendarEventList allEvents;
        //private static ICalendarEventService service = new CalendarEventService();
        private static CalendarEventService service = new CalendarEventService();

        #endregion


        [TestMethod()]
        public void GoogleCalendarTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void CreateEventTest()
        {
            calendar.CreateEvent(new CalendarEvent("Today", "Content", "Location", period.Today().Start, period.Today().End));
        }

        [TestMethod()]
        public void TestCreateEventTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void UpdateEventTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddQuickEventTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void DeleteEventTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetEventsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetOthersEventsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetComparingEventsTest()
        {CalendarEventList events ;
        CalendarEventList l;
         
          List<string> s = new List<string>();
          s.Add("robert.stevens@manchester.ac.uk");
            s.Add("aisha.jaddoh@gmail");
             events = calendar.GetComparingEvents(DateTime.Today, DateTime.Today, s, "Free Busy View",false);
           // CalendarEventList events = service.GetComparingEvents(calendar, period.Today(), s, "Free Busy View");
            CalendarEvent ev = new  CalendarEvent("", "all free until ", "",new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 9, 0, 0), new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 10, 0, 0));
            CalendarEvent ev2 = new CalendarEvent("", "all free until ", "", new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 12, 30, 0), new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 2, 0, 0).AddHours(12));

             l = new CalendarEventList();
             l.Add(ev);
             l.Add(ev2);
           
            Assert.AreEqual(l, events,"Events are not equal ");

                 
        }
                

        [TestMethod()]
        public void DeleteEventsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetRecurrenceSettingsTest()
        {
            Assert.Fail();
        }
    }
}
