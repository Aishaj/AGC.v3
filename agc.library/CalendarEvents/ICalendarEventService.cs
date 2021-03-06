﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGC.Library
{
    public interface ICalendarEventService
    {
        CalendarEventList GetEvents(IGoogleCalendar calendar, TimeIntervals period);
        CalendarEventList GetOthersEvents(IGoogleCalendar calendar, TimeIntervals period, String CalendarIDs);
        CalendarEventList GetComparingEvents(IGoogleCalendar calendar, TimeIntervals period, List<String> CalendarIDs,String viewFormat,bool primary);
        CalendarEventList GetEvents(IGoogleCalendar calendar, DateTime start, DateTime end);

        CalendarEventList SearchEvents(CalendarEventList events, String keyword);

        CalendarEventList FormatEventsDatesStringRepresentation(CalendarEventList allEvents, DateTimePreferences preferences);

        CalendarEventList Sort(CalendarEventList allEvents, SortFilterPreferences preferences);

        CalendarEventList FilterByStartTime(CalendarEventList allEvents, SortFilterPreferences preferences);

        CalendarEventList FilterByDayOfWeek(CalendarEventList allEvents, SortFilterPreferences preferences);

        CalendarEventList FilterByStatus(CalendarEventList allEvents, SortFilterPreferences preferences);
    }
}
