using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGC.Library
{
    public interface IGoogleCalendar
    {
        bool CreateEvent(CalendarEvent ev);

        bool UpdateEvent(CalendarEvent ev, GoogleCalendar.ActionType type);

        bool AddQuickEvent(String eventText);

        bool DeleteEvent(CalendarEvent ev, GoogleCalendar.ActionType type);

        CalendarEventList GetEvents(DateTime timeMin, DateTime timeMax);
        CalendarEventList GetOthersEvents(DateTime timeMin, DateTime timeMax , String CalendarIDs);
        CalendarEventList GetComparingEvents(DateTime timeMin, DateTime timeMax, List<String> CalendarIDs, String ViewFormat,bool primary);
        
        void DeleteEvents(CalendarEventList evs);

        RecurrenceSettings GetRecurrenceSettings(CalendarEvent ev);

        void LogOut();
    }
}
