using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace AGC.Library
{
    public class GoogleCalendar : IGoogleCalendar
    {
        private const string CLIENT_ID = "643150686764-lr52us0outlsd46hn6vhn58nhj6q85tr.apps.googleusercontent.com";
        private const string CLIENT_SECRET = "Bgms-ReGArsk8xzjUJhqiuUJ";
        private const string DEFAULT_CALENDAR = "primary";
        private const string APPLICATION_NAME = "Accessible Google Calendar";

        private const string DATE_FORMAT = "yyyy-MM-dd";
        private const string TIME_FORMAT = "yyyy-MM-ddTHH:MM:ss";

        private const string CONFIRMED = "confirmed";
        private const string TENTATIVE = "tentative";

        private CalendarService service;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static string accessToken = string.Empty;



        private List<FreeTime> allFree = new List<FreeTime>();
        private List<FreeTime> finalFree = new List<FreeTime>();
        private static Dictionary<FreeTime, int> FinalallFree= new Dictionary<FreeTime, int>();
        private static Dictionary<FreeTime, String> FinalallFreeConnection = new Dictionary<FreeTime, String>();
        private Dictionary<FreeTime, string> FreeBusy = new Dictionary<FreeTime, string>();
        private static Dictionary<FreeTime, string> FinalFreeBusy = new Dictionary<FreeTime, string>();
        private static int numberOfEmails;
        private static bool primary = false;
        private static String firstEmail;
        #region Constructor

        public GoogleCalendar()
        {
            Authorization();
        }


        #endregion

        #region Public Methods

        public enum ActionType
        {
            none,
            single,
            all,
            following
        }

        public bool CreateEvent(CalendarEvent ev)
        {
            log.Debug("Try to create new event title=\"" + ev.Title + "\"");

            try
            {
                // New event
                Event newEvent = ConvertCalendarEventToGoogleEvent(ev, false);

                service.Events.Insert(newEvent, DEFAULT_CALENDAR).Execute();

                log.Debug("New event was successfully created");

                return true;
            }
            catch (Exception ex)
            {
                log.Error("Event creation failed with error:", ex);
                log.Info("Event Details: " + ev.ToString());
                return false;
            }
        }

        public bool TestCreateEvent(CalendarEvent ev)
        {
            log.Debug("Try to create new event title=\"" + ev.Title + "\"");

            try
            {
                // New event
                Event newEvent = ConvertCalendarEventToGoogleEvent(ev, false);
                newEvent.Status = "cancelled";

                service.Events.Insert(newEvent, DEFAULT_CALENDAR).Execute();

                log.Debug("New event was successfully created");

                return true;
            }
            catch (Exception ex)
            {
                log.Error("Event creation failed with error:", ex);
                log.Info("Event Details: " + ev.ToString());
                return false;
            }
        }

        public bool UpdateEvent(CalendarEvent ev, ActionType type)
        {
            log.Debug("Try to update event title=\"" + ev.Title + "\"");
            try
            {
                switch (type)
                {
                    case ActionType.single:
                        {
                            ev.RRule = String.Empty;
                            break;
                        }
                    case ActionType.all:
                        {
                            ev = GetMainEventData(ev); //.Id = GetMainEventId(ev.Id);
                            break;
                        }
                    case ActionType.following:
                        {
                            // Create recurrence event with new settings
                            CreateEvent(ev);
                            ev = GetAllPreviousEvents(ev);
                            break;
                        }
                }

                Event newEvent = ConvertCalendarEventToGoogleEvent(ev, true);

                // Increate sequence number... I hate you Google API for your crazy things >_<
                newEvent = UpdateSequenceNumber(newEvent);

                service.Events.Update(newEvent, DEFAULT_CALENDAR, newEvent.Id).Execute();

                log.Debug("New event was successfully updated");

                return true;
            }
            catch (Exception ex)
            {
                log.Error("Event update failed with error:", ex);
                log.Info("Event Details: " + ev.ToString());
                return false;
            }
        }

        public bool AddQuickEvent(String eventText)
        {
            log.Debug("Try to add quick event");

            try
            {
                service.Events.QuickAdd(DEFAULT_CALENDAR, eventText).Execute();
                log.Debug("Quick event was successfully added");
                return true;
            }
            catch (Exception ex)
            {
                log.Error("Failed to add quick event. Error:", ex);
                log.Info("Event Details: " + eventText);
                return false;
            }
        }

        public bool DeleteEvent(CalendarEvent ev, ActionType type)
        {
            try
            {
                switch (type)
                {
                    case ActionType.single:
                        {
                            service.Events.Delete(DEFAULT_CALENDAR, ev.Id).Execute();
                            break;
                        }
                    case ActionType.all:
                        {
                            ev.Id = GetMainEventId(ev.Id);
                            service.Events.Delete(DEFAULT_CALENDAR, ev.Id).Execute();
                            break;
                        }
                    case ActionType.following:
                        {
                            UpdateEvent(GetAllPreviousEvents(ev), ActionType.all);
                            break;
                        }
                }
                return true;
            }
            catch (Exception ex)
            {
                log.Error("Event deleting failed with error:", ex);
                log.Info("Event Details: " + ev.ToString());
                return false;
            }
        }

        public CalendarEventList GetEvents(DateTime timeMin, DateTime timeMax)
        {
            log.Debug("Try to get all events from Google Calendar");
            CalendarEventList calendarEvents = new CalendarEventList();

            try
            {
                EventsResource.ListRequest events = service.Events.List(DEFAULT_CALENDAR);
                events.SingleEvents = true;
                events.MaxResults = 2500;
                events.TimeMin = timeMin;
                events.TimeMax = timeMax;
                Events eventList = events.Execute();
                calendarEvents = ConvertToCalendarEvents(eventList.Items);
                calendarEvents.SortByDate();
                log.Debug("Successfully got all events from Google Calendar");
            }
            catch (Exception ex)
            {
                log.Error("Failed to get all events from Google Calendar with error:", ex);
            }

            return calendarEvents;
        }

        public CalendarEventList GetOthersEvents(DateTime timeMin, DateTime timeMax, String CalendarIDs)
        {
            log.Debug("Try to get  events from other Google Calendar");
            CalendarEventList calendarEvents = new CalendarEventList();

            try
            {
                EventsResource.ListRequest events = service.Events.List(CalendarIDs);
                events.SingleEvents = true;
                events.MaxResults = 2500;
                events.TimeMin = timeMin;
                events.TimeMax = timeMax;
                Events eventList = events.Execute();
                calendarEvents = ConvertToCalendarEvents(eventList.Items);
                calendarEvents.SortByDate();

                log.Debug("Successfully got  events from other Google Calendar");
            }
            catch (Exception ex)
            {
                log.Error("Failed to get  events from other  Google Calendar with error:", ex);
            }

            return calendarEvents;

        }

        public CalendarEventList GetComparingEvents(DateTime timeMin, DateTime timeMax, List<String> CalendarIDs, String ViewFormat,bool primaryEmail)
        {

             allFree = new List<FreeTime>();
         finalFree = new List<FreeTime>();
         FinalallFree= new Dictionary<FreeTime, int>();
         FinalallFreeConnection = new Dictionary<FreeTime, String>();
         FreeBusy = new Dictionary<FreeTime, string>();
      FinalFreeBusy = new Dictionary<FreeTime, string>();
      primary = primaryEmail;
            if(primary)
                firstEmail = CalendarIDs[0].Substring(0, CalendarIDs[0].IndexOf("."));

            //Console.WriteLine("fffffffffffff "+ firstEmail);


            log.Debug("Try to compare  events from other Google Calendars ");
            CalendarEventList calendarEvents = new CalendarEventList();

            Dictionary<string, List<FreeTime>> personAndFreeTimes = new Dictionary<string, List<FreeTime>>();
            List<String> noEvents = new List<string>();

            

            try
            {
                foreach (String calendarID in CalendarIDs)
                {

                    List<FreeTime> freeTimeList = new List<FreeTime>();
                    if (timeMax.DayOfWeek == 0)
                        timeMax = timeMax.AddDays(-2);

                    if (timeMax.DayOfWeek == DayOfWeek.Saturday)
                        timeMax = timeMax.AddDays(-1);

                    if (timeMin.DayOfWeek == DayOfWeek.Saturday)
                        timeMax = timeMax.AddDays(+2);

                    if (timeMin.DayOfWeek == DayOfWeek.Sunday)
                        timeMax = timeMax.AddDays(+1);





                    EventsResource.ListRequest events = service.Events.List(calendarID);

                    events.SingleEvents = true;
                    events.MaxResults = 2500;
                    events.TimeMin = timeMin;
                    events.TimeMax = timeMax;
                    Events eventList = events.Execute();






                    FreeBusyRequest request = new FreeBusyRequest();
                    List<FreeBusyRequestItem> requestItems = new List<FreeBusyRequestItem>();
                    request.TimeMin = timeMin;
                    request.TimeMax = timeMax;
                    FreeBusyRequestItem r = new FreeBusyRequestItem();
                                                          

                    r.Id = calendarID;

                    request.Items = new List<FreeBusyRequestItem>();
                    request.Items.Add(r);

                    FreebusyResource.QueryRequest TestRequest = service.Freebusy.Query(request);
                    var responseOBJ = TestRequest.Execute();
                    int count = responseOBJ.Calendars[calendarID].Busy.Count;
                    

                    if (count == 0)
                    {
                        FreeTime free = new FreeTime();

                        free.setStart(new DateTime(timeMax.Year, timeMax.Month, timeMax.Day, 9, 0, 0));
                        free.setEnd(new DateTime(timeMax.Year, timeMax.Month, timeMax.Day, 5, 0, 0).AddHours(12));
                        free.setEmail(calendarID);
                        freeTimeList.Add(free);
                        //Console.WriteLine(calendarID + " " + free.Start + " " + free.End);

                    }


                    if (count != 0)
                    {


                       // Console.WriteLine(timeMax.Date + "date max ");
                        //Console.WriteLine(timeMin.Date + "date min ");
                       // Console.WriteLine(DateTime.Today + "fff");
                        DateTime d = timeMin.Date;
                        Boolean found = false;
                        

                        if (d.DayOfWeek == DayOfWeek.Sunday)
                            d = d.AddDays(1);

                      //  Console.WriteLine("first     "+ d.Date);
                        if (timeMax.Date.CompareTo(timeMin.Date) != 0 && d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                        {

                            //Console.WriteLine("second    " + d.Date);
                        while (d <= timeMax)
                        {
                          found = false;
                            foreach (TimePeriod n in responseOBJ.Calendars[calendarID].Busy)
                            { 
                                //Console.WriteLine("third    " + d.Date);
                                //Console.WriteLine(d.CompareTo(n.Start.Value.Date) + " d: " + d.Date + "n :  " + n.Start.Value.Date); 
                                if (d.CompareTo(n.Start.Value.Date) == 0)
                                {
                                    found = true;
                                    break;
                                }
                            }

                           // Console.WriteLine("fourth    " + d.Date + " " + found);
                            if (!found)
                            {
                                //Console.WriteLine("not found    " + d.Date);
                                FreeTime free = new FreeTime();

                                free.setStart(new DateTime(d.Year, d.Month, d.Day, 9, 0, 0));
                                free.setEnd(new DateTime(d.Year, d.Month, d.Day, 5, 0, 0).AddHours(12));
                                free.setEmail(calendarID);
                                freeTimeList.Add(free);
                               // Console.WriteLine("adding to list    " + free.Start + " " + free.End);
                            }
                            d= d.AddDays(+1);
                            
                            if (d.DayOfWeek == DayOfWeek.Saturday)
                                d = d.AddDays(+2);
                            
                        }
                        }


                        int i = 0;
                        int j = 0;
                        Boolean singleevent = false;
                       // Console.WriteLine("the count " + count);
                        if (count == 1) singleevent = true;
                        while (i < count && count != 0)
                        {
                            FreeTime free = new FreeTime();
                            Boolean startatNine = false;
                            Boolean endFive = false;

                            DateTime starttime = (responseOBJ.Calendars[calendarID].Busy[i].Start).Value;
                            DateTime endtime = (responseOBJ.Calendars[calendarID].Busy[i].End).Value;


                           // Console.WriteLine(" lets see " + starttime + " " + endtime);

                            DateTime startworkhours = new DateTime(starttime.Year, starttime.Month, starttime.Day, 9, 0, 0);
                            DateTime endworkhours = new DateTime(starttime.Year, starttime.Month, starttime.Day, 5, 0, 0).AddHours(12);


                            if (i == 0 && singleevent == false)
                            {
                                if (j == 0 && (DateTime.Compare(startworkhours, starttime) != 0))
                                {
                                    free = subtractFromNine(starttime, endtime, i);
                                    free.setEmail(calendarID);

                                    if ((DateTime.Compare(starttime.Date, ((DateTime)responseOBJ.Calendars[calendarID].Busy[i + 1].Start).Date) < 0))
                                    {

                                        i--;
                                        j++;
                                    }

                                }



                                else if (startworkhours.Equals(starttime) || j > 0)
                                {
                                    //Console.WriteLine("urghhhhh" + DateTime.Compare(starttime.Date, ((DateTime)responseOBJ.Calendars[calendarID].Busy[i + 1].Start).Date));

                                    if (startworkhours.Equals(starttime) && j == 0)
                                    {

                                       // Console.WriteLine("inside second if ");
                                        startatNine = true;
                                        if (DateTime.Compare(starttime.Date, ((DateTime)responseOBJ.Calendars[calendarID].Busy[i + 1].Start).Date) < 0)
                                        {
                                          // Console.WriteLine("::::::::::::::::::::::::::::::::::::::::::::::::::::");
                                            j++;
                                            i--;

                                        }


                                    }

                                    else if ((DateTime.Compare(starttime.Date, ((DateTime)responseOBJ.Calendars[calendarID].Busy[i + 1].Start).Date) < 0) && j > 0)
                                    {
                                        //Console.WriteLine("inside first if ");
                                        free = subtractFromFive(starttime, endtime, i);
                                        free.setEmail(calendarID);
                                        j = 0;
                                    }

                                }

                            }




                            else if (i + 1 == count && singleevent == false)
                            {
                                if (j == 0)
                                {
                                   //Console.WriteLine(" else if (i + 1 == count && !(endworkhours.equals(endtime)) && singleevent == false) j==0 ");
                                    DateTime tempstarttime = (responseOBJ.Calendars[calendarID].Busy[i - 1].Start).Value;
                                    DateTime tempendtime = (responseOBJ.Calendars[calendarID].Busy[i - 1].End).Value;

                                    if ((DateTime.Compare(starttime.Date, tempendtime.Date) == 0))
                                    {
                                        //Console.WriteLine(" entered if ");
                                        TimeSpan interval = starttime - tempendtime;
                                        free.setStart(starttime - interval);
                                        //Console.WriteLine(free.Start);
                                        free.setEnd(starttime);
                                        free.setEmail(calendarID);

                                  }

                                    else
                                    {
                                        free = subtractFromNine(starttime, endtime, i);
                                        
                                    }

                                    if (!(endworkhours.Equals(endtime)))
                                    {
                                        j++;
                                        i--;
                                    }

                                }
                                else
                                {
                                    if (!(endworkhours.Equals(endtime)))
                                    {
                                        free = subtractFromFive(starttime, endtime, i);
                                        j = 0;
                                    }
                                    else endFive = true;


                                }


                            }
                            else if (singleevent)
                            {
                                //Console.WriteLine("inside single event " + j + " " + starttime + " " + startworkhours);
                                if (j == 0 && !(DateTime.Compare(starttime, startworkhours) == 0))
                                {
                                    free = subtractFromNine(starttime, endtime, i);


                                    if (!(DateTime.Compare(endtime, endworkhours) == 0))
                                    {
                                        j++;
                                        i--;
                                    }

                                }


                                else
                                {
                                    if (!(DateTime.Compare(endtime, endworkhours) == 0))
                                    {
                                        free = subtractFromFive(starttime, endtime, i);
                                        free.setEmail(calendarID);
                                        j = 0;
                                    }
                                    else endFive = true;

                                }


                            }

                            else
                            {

                               //Console.WriteLine("yaaaaaaaaaaaaaaarab");
                                DateTime tempstarttime = (responseOBJ.Calendars[calendarID].Busy[i - 1].Start).Value;
                                DateTime tempendtime = (responseOBJ.Calendars[calendarID].Busy[i - 1].End).Value;
                                if (j == 0)
                                {
                                    if ((DateTime.Compare(tempendtime.Date, starttime.Date) == 0))
                                    {
                                        //Console.WriteLine("entered the first");
                                        TimeSpan interval = starttime - tempendtime;
                                        free.setStart(starttime - interval);
                                        free.setEnd(starttime);
                                        free.setEmail(calendarID);


                                    }

                                    else
                                    {
                                        //Console.WriteLine("entered the second");
                                        if (startworkhours.Equals(starttime))
                                        {
                                           // Console.WriteLine("statrt at nine");
                                            startatNine = true;
                                        }
                                        else
                                        {
                                            free = subtractFromNine(starttime, endtime, i);
                                        }

                                    }

                                    if ((DateTime.Compare(starttime.Date, ((DateTime)(responseOBJ.Calendars[calendarID].Busy[i + 1].Start).Value).Date) < 0))
                                    {
                                       //Console.WriteLine("entered the comparason");
                                        i--;
                                        j++;



                                    }

                                }
                                else
                                {
                                    //Console.WriteLine("supposed " + endworkhours.Equals(endtime));
                                    if (!(endworkhours.Equals(endtime)))
                                    {
                                        free = subtractFromFive(starttime, endtime, i);
                                        free.setEmail(calendarID);
                                        j = 0;
                                    }
                                    else { endFive = true; j = 0; }

                                }



                            }
                            i++;


                            if (!startatNine && !endFive)
                            {
                                //Console.WriteLine("befor adding " + free.Start + "  " + free.End);
                                freeTimeList.Add(free);
                               //  Console.WriteLine(calendarID + " " + free.Start + " " + free.End);

                            }

                            //Console.WriteLine("after adding to list");

                        }//end while
                    }//end if (count !=0 )

                   
                    personAndFreeTimes.Add(calendarID.Substring(0, calendarID.IndexOf(".")), freeTimeList);

                    //foreach (String k in personAndFreeTimes.Keys)
                    //{
                    //    foreach (FreeTime f in personAndFreeTimes[k])
                    //        //Console.WriteLine(k + " " + f.Start + " " + f.End);
                    //}

                    freeTimeList.Sort();

                   

                    }//end foreach


                numberOfEmails = personAndFreeTimes.Count;
                Stack myStack = new Stack();
                foreach (String key in personAndFreeTimes.Keys)
                {


                    myStack.Push(personAndFreeTimes[key]);
                }

                //  -----------------------------------  All People View   --------------------------------------------------   //

                if (ViewFormat == "All People")
                {

                 
                    Stack s1 = new Stack();
                    Stack s2 = new Stack();
                    foreach (String key in personAndFreeTimes.Keys)
                    {
                        // string result = noEvents.Find(delegate(String  i) { return i == key; });
                        //  if(result!=null)
                        KeyValuePair<List<FreeTime>, String> pair = new KeyValuePair<List<FreeTime>, String>(personAndFreeTimes[key], key);

                        s1.Push(pair);
                        s2.Push(pair);
                        //Console.WriteLine("pair " + pair.Key.ToString() + " " + pair.Value);
                    }



                    while (s1.Count != 0)
                    {
                        KeyValuePair<List<FreeTime>, String> sp1 = (KeyValuePair<List<FreeTime>, String>)s1.Pop();
                        while (s2.Count != 0)
                        {
                            KeyValuePair<List<FreeTime>, String> sp2 = (KeyValuePair<List<FreeTime>, String>)s2.Pop();
                            if (!(sp1.Value.Equals(sp2.Value)))
                                FindMutual2(sp1, sp2);
                        }
                        s2 = s1;
                    }



                    FinalMutual2();

                    if (personAndFreeTimes.Count > 2)
                    {

                        FinalMutual3();
                        FinalMutual3();
                    }
                    //Console.WriteLine(" back to main");
                    // FinalMutual4();
                    calendarEvents = ConvertToMutualCalendarEvents(FinalFreeBusy);

                }//end if All People view

                //  -----------------------------------     --------------------------------------------------   //


                //  -----------------------------------  "Some People" View   --------------------------------------------------   //

                if (ViewFormat == "Some People")
                {

                    Stack s1 = new Stack();
                    Stack s2 = new Stack();
                    foreach (String key in personAndFreeTimes.Keys)
                    {
                        
                        KeyValuePair<List<FreeTime>, String> pair = new KeyValuePair<List<FreeTime>, String>(personAndFreeTimes[key], key);

                        s1.Push(pair);
                        s2.Push(pair);
                        //Console.WriteLine("pair " + pair.Key.ToString() + " " + pair.Value);
                    }



                    while (s1.Count != 0)
                    {
                        KeyValuePair<List<FreeTime>, String> sp1 = (KeyValuePair<List<FreeTime>, String>)s1.Pop();
                        while (s2.Count != 0)
                        {
                            KeyValuePair<List<FreeTime>, String> sp2 = (KeyValuePair<List<FreeTime>, String>)s2.Pop();
                            if (!(sp1.Value.Equals(sp2.Value)))
                                FindMutual2(sp1, sp2);
                        }
                        s2 = s1;
                    }



                    FinalMutual2();

                    // if (personAndFreeTimes.Count > 2)
                    int num = 1;
                    while( num<personAndFreeTimes.Count)
                    {
                        FinalMutual3();
                        num++;
                    }
                    FinalMutual3();
                    
                    calendarEvents = ConvertToFreeBusyCalendarEvents(FinalFreeBusy);
                } //end free busy view 

                //Console.WriteLine("before sort");
                calendarEvents.SortByDate();

                log.Debug("Successfully compared  events from other Google Calendars");
            }// end try


            catch (Exception ex)
            {
                log.Error("Failed to compare  events from other  Google Calendars with error:", ex);
            }

            
            return calendarEvents;

        }

       

        public void DeleteEvents(CalendarEventList evs)
        {
            foreach (CalendarEvent ev in evs)
            {
                DeleteEvent(ev, ActionType.single);
            }
        }

        public RecurrenceSettings GetRecurrenceSettings(CalendarEvent ev)
        {
            if (!ev.IsRecurrenceEvent)
            {
                return new RecurrenceSettings();
            }

            return new RecurrenceSettings(ev.Start, GetGoogleEventById(GetMainEventId(ev.Id)).Recurrence[0]);
        }

        #endregion

        #region Private Methods
        
        private FreeTime subtractFromNine(DateTime startTime, DateTime endTime, int i)
        {
            FreeTime free = new FreeTime();


            DateTime startWorkHours = new DateTime(startTime.Year, startTime.Month, startTime.Day, 9, 0, 0);
            DateTime endWorkHours = new DateTime(startTime.Year, startTime.Month, startTime.Day, 5, 0, 0).AddHours(12);

            TimeSpan interval = startTime - startWorkHours;
            free.setStart(startTime - interval);
            free.setEnd(startTime);
           // Console.WriteLine("inside the method : " + free.Start + " " + free.End);
            return free;


        }

        private FreeTime subtractFromFive(DateTime startTime, DateTime endTime, int i)
        {
            FreeTime free = new FreeTime();


            DateTime startWorkHours = new DateTime(startTime.Year, startTime.Month, startTime.Day, 9, 0, 0);
            DateTime endWorkHours = new DateTime(startTime.Year, startTime.Month, startTime.Day, 5, 0, 0).AddHours(12);

            TimeSpan interval = endWorkHours - endTime;
            free.setStart(endWorkHours - interval);
            free.setEnd(endWorkHours);
           // Console.WriteLine("inside the five method : " + free.Start + " " + free.End);

            return free;


        }

        private void Authorization()
        {
            log.Debug("Start Authorization");

            try
            {
                UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = CLIENT_ID,
                    ClientSecret = CLIENT_SECRET,
                },
                new[] { CalendarService.Scope.Calendar },
                "user",
                CancellationToken.None).Result;

                // Save Token for log out
                accessToken = credential.Token.AccessToken;

                // Create the service.
                service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = APPLICATION_NAME,
                });
            }
            catch (Exception ex)
            {
                log.Error("Authorization failed with error:", ex);
            }
            log.Debug("Finish Authorization");

        }

        public void LogOut()
        {
            log.Debug("Start Log out");

            try
            {
                UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = CLIENT_ID,
                    ClientSecret = CLIENT_SECRET,
                },
                new[] { CalendarService.Scope.Calendar },
                "user",
                CancellationToken.None).Result;

                // Save Token for log out
                credential.Token.AccessToken = String.Empty;
                credential.Token.ExpiresInSeconds = 1;
                credential.Token.RefreshToken = String.Empty;

                // Create the service.
                service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = APPLICATION_NAME,
                });
            }
            catch (Exception ex)
            {
                log.Error("Log out failed with error:", ex);
            }
            log.Debug("Finish Log out");

            //System.Diagnostics.Process.Start("https://accounts.google.com/o/oauth2/revoke?token=" + accessToken);
        }

        private static EventDateTime ConvertToEventDateTime(DateTime? dateTime, bool isFullDateEvent)
        {
            EventDateTime eventDateTime = new EventDateTime();
            if (isFullDateEvent)
            {
                DateTime date = dateTime ?? DateTime.Now;
                eventDateTime.Date = date.ToString(DATE_FORMAT);
            }
            else
            {
                eventDateTime.DateTime = dateTime;
            }
            eventDateTime.TimeZone = "Europe/London";
            return eventDateTime;
        }

        private static CalendarEvent ConvertGoogleEventToCalendarEvent(Event ev)
        {
            CalendarEvent calendarEvent;
            try
            {
                calendarEvent = new CalendarEvent(ev.Id, ev.Summary, ev.Description, ev.Location, GetEventStartDate(ev), GetEventEndDate(ev), IsFullDayEvent(ev), IsRecurringEvent(ev), IsConfirmedEvent(ev));
            }
            catch (Exception ex)
            {
                log.Error("Failed to convert Google Calendar event to Calendar event with error:", ex);
                log.Info("Event Details");
                log.Info("Event Id: " + ev.Id);
                log.Info("Event Summary: " + ev.Summary);
                log.Info("Event Description: " + ev.Description);
                log.Info("Event Location: " + ev.Location);
                log.Info("Event Start: " + ev.Start.DateTime);
                log.Info("Event End: " + ev.End.DateTime);
                calendarEvent = new CalendarEvent();
            }

            return calendarEvent;
        }

        private static CalendarEvent ConvertMutualGoogleEventToCalendarEvent(KeyValuePair<FreeTime, String> f)
        {
            CalendarEvent calendarEvent;
            try

            {
                
                calendarEvent = new CalendarEvent("", "", "", f.Key.Start, f.Key.End);
               
            }
            catch (Exception ex)
            {
                log.Error("Failed to convert Google Calendar event to Calendar event with error:", ex);
                log.Info("Event Details");
                // log.Info("Event Id: " + ev.Id);
                //log.Info("Event Summary: " + ev.Summary);
                //log.Info("Event Description: " + ev.Description);
                //log.Info("Event Location: " + ev.Location);
                //log.Info("Event Start: " + ev.Start.DateTime);
                // log.Info("Event End: " + ev.End.DateTime);
                calendarEvent = new CalendarEvent();
            }

            return calendarEvent;
        }

        private static CalendarEvent ConvertFreeBusyGoogleEventToCalendarEvent(KeyValuePair<FreeTime, String> f)
        {
            CalendarEvent calendarEvent;
            try
            {
                if (f.Value.Count(Char.IsWhiteSpace) == numberOfEmails - 1)
                calendarEvent = new CalendarEvent("", "all free ", "", f.Key.Start, f.Key.End);
                else
                calendarEvent = new CalendarEvent("", f.Value, "", f.Key.Start, f.Key.End);


            }
            catch (Exception ex)
            {
                log.Error("Failed to convert Google Calendar event to Calendar event with error:", ex);
                log.Info("Event Details");
                // log.Info("Event Id: " + ev.Id);
                //log.Info("Event Summary: " + ev.Summary);
                //log.Info("Event Description: " + ev.Description);
                //log.Info("Event Location: " + ev.Location);
                //log.Info("Event Start: " + ev.Start.DateTime);
                // log.Info("Event End: " + ev.End.DateTime);
                calendarEvent = new CalendarEvent();
            }

            return calendarEvent;
        }

        private static Event ConvertCalendarEventToGoogleEvent(CalendarEvent ev, bool rememberId)
        {
            try
            {
                Event googleEvent = new Event();

                if (!string.IsNullOrEmpty(ev.Id) && rememberId)
                {
                    googleEvent.Id = ev.Id;
                }

                googleEvent.Summary = ev.Title;
                googleEvent.Location = ev.Location;
                googleEvent.Description = ev.Content;

                googleEvent.Start = ConvertToEventDateTime(ev.Start, ev.IsFullDateEvent);
                googleEvent.End = ev.IsFullDateEvent ? ConvertToEventDateTime(ev.Start, true) : ConvertToEventDateTime(ev.End, false);

                // Recurrence
                if (!String.IsNullOrEmpty(ev.RRule))
                {
                    googleEvent.Recurrence = new String[] { ev.RRule };
                }

                // Reminder
                googleEvent.Reminders = ConvertMinutesToGoogleEventReminder(ev.Reminder);

                // Status
                googleEvent.Status = ev.Confirmed ? CONFIRMED : TENTATIVE;

                return googleEvent;
            }
            catch (Exception ex)
            {
                log.Error("CalendarEvent convertation to GoogleEvent failed with error:", ex);
                log.Info("Event Details: " + ev.ToString());
                return null;
            }
        }

        private static CalendarEventList ConvertToCalendarEvents(IList<Event> googleEvents)
        {
            CalendarEventList calendarEvents = new CalendarEventList();

            foreach (Event ev in googleEvents)
            {
                calendarEvents.Add(ConvertGoogleEventToCalendarEvent(ev));
            }

            return calendarEvents;
        }

        private static CalendarEventList ConvertToMutualCalendarEvents(Dictionary<FreeTime, string> FreeBusy)
        {
            CalendarEventList calendarEvents = new CalendarEventList();

           
            
            foreach (KeyValuePair<FreeTime, String> kv in FreeBusy)
            {
               if (kv.Value.Count(Char.IsWhiteSpace) == numberOfEmails - 1)
                calendarEvents.Add(ConvertMutualGoogleEventToCalendarEvent(kv));

            }

            return calendarEvents;
        }


        private static CalendarEventList ConvertToFreeBusyCalendarEvents(Dictionary<FreeTime, string> FreeBusy)
        {
            CalendarEventList calendarEvents = new CalendarEventList();

            foreach (KeyValuePair<FreeTime, String> kv in FreeBusy)
            {
                 if (primary)
                 {
                     if (kv.Value.IndexOf(firstEmail) != -1)
                         calendarEvents.Add(ConvertFreeBusyGoogleEventToCalendarEvent(kv));
                     else
                         continue;

                 }
                 else
                calendarEvents.Add(ConvertFreeBusyGoogleEventToCalendarEvent(kv));
            }

            return calendarEvents;
        }

        private static Event.RemindersData ConvertMinutesToGoogleEventReminder(int minutes)
        {
            Event.RemindersData reminder = new Event.RemindersData()
            {
                Overrides = new List<EventReminder> 
                    { 
                        new EventReminder()
                        {
                            Method = "email",
                            Minutes = minutes
                        }
                    },
                UseDefault = false
            };
            return reminder;
        }

        private Event GetGoogleEventById(string id)
        {
            return service.Events.Get(DEFAULT_CALENDAR, id).Execute();
        }

        private CalendarEvent GetMainEventData(CalendarEvent ev)
        {
            ev.Id = GetMainEventId(ev.Id);

            // Find start and end dates of the first event in the series using main part of event ID
            CalendarEventList events = GetEvents(DateTime.Today.AddYears(-4), DateTime.Today.AddYears(4));

            int i = 0;
            while (!events[i].Id.Contains(ev.Id))
            {
                i++;
            }

            ev.Start = events[i].Start;
            ev.End = events[i].End;

            return ev;
        }

        private CalendarEvent GetAllPreviousEvents(CalendarEvent ev)
        {
            // Get recurrence event using it's single instance event id
            CalendarEvent old = ConvertGoogleEventToCalendarEvent(GetGoogleEventById(GetMainEventId(ev.Id)));

            // Get old event recurrence settings
            RecurrenceSettings previous = GetRecurrenceSettings(old);

            // Change it to end one day before new event
            previous.EndsOn(ev.Start.AddDays(-1));
            old.RRule = previous.ToString();

            return old;
        }

        private static DateTime GetEventStartDate(Event ev)
        {
            return ev.Start.Date == null ? (DateTime)ev.Start.DateTime : Convert.ToDateTime(ev.Start.Date);
        }

        private static DateTime? GetEventEndDate(Event ev)
        {
            return ev.End.Date == null ? ev.End.DateTime : Convert.ToDateTime(ev.End.Date).AddSeconds(-1);
        }

        private static bool IsFullDayEvent(Event ev)
        {
            if (!String.IsNullOrEmpty(ev.End.Date))
                return true;
            else
                return false;
        }

        private static bool IsRecurringEvent(Event ev)
        {
            if (ev.Id.Contains('_'))
                return true;
            else
                return false;
        }

        private static bool IsConfirmedEvent(Event ev)
        {
            return ev.Status == CONFIRMED ? true : false;
        }

        private static string GetMainEventId(string id)
        {
            return id.Split('_')[0];
        }

        private Event UpdateSequenceNumber(Event ev)
        {
            int sequence = service.Events.Get(DEFAULT_CALENDAR, ev.Id).Execute().Sequence ?? 0;
            sequence++;
            ev.Sequence = sequence;
            return ev;
        }

        private static string GetRRule(Event ev)
        {
            if (!IsFullDayEvent(ev))
            {
                return String.Empty;
            }

            return ev.Recurrence != null ? ev.Recurrence[0] : String.Empty;
        }

        private static int GetReminder(Event ev)
        {
            if (ev.Reminders.UseDefault == true)
            {
                return 10;
            }
            else
            {
                return ev.Reminders.Overrides[0].Minutes ?? 10;
            }
        }

        private void FindMutual(KeyValuePair<String,List<FreeTime>>  list, KeyValuePair<String,List<FreeTime>> list2)
        {
            Boolean intersecFound = false;
            FreeTime intersection = null;
            foreach (var t in list.Value)
            {
                intersecFound = false;
               // if (list != null)///&& list.Count > 1)
                //{
                    Console.WriteLine(t.Start + " " + t.End + " ccccc");
                    foreach (var s in list2.Value)
                    {
                        if ((DateTime.Compare(t.Start.Date, s.Start.Date) != 0) || ((DateTime.Compare(t.Start.Date, s.Start.Date) == 0) && t.End.CompareTo(s.Start) <= 0))
                            continue;
                        intersection = null;
                        
                        Console.WriteLine(s.Start + " " + s.End + " ssssss");
                        if (!((DateTime.Compare(t.Start.Date, s.Start.Date) == 0) && (DateTime.Compare(t.Start, s.Start) == 0) && (DateTime.Compare(t.End, s.End) == 0)) && (t.Start.Date == s.Start.Date))
                        {
                            Console.WriteLine("callling inteersection");
                            intersection = Intersection(t, s);
                        }
                        if (((DateTime.Compare(t.Start.Date, s.Start.Date) == 0) && (DateTime.Compare(t.Start, s.Start) == 0) && (DateTime.Compare(t.End, s.End) == 0)) && (t.Start.Date == s.Start.Date))
                        {
                            Console.WriteLine("callling inteersection");
                            intersection = Intersection(t, s);
                        }


                        if (intersection != null)
                        {
                            
                           allFree.Add(intersection);
                           if (FinalallFree.ContainsKey(intersection) && !(list.Key.Equals(list2.Key)))
                           {
                               if (!(FinalallFreeConnection[intersection].Contains(list.Key)) && !(FinalallFreeConnection[intersection].Contains(list2.Key)))
                               {
                                   FinalallFree[intersection]++;
                                   Console.WriteLine("does not the string exists ");
                                   Console.WriteLine("######## 2" + list.Key + " " + list2.Key);
                               }
                                  
                           }
                              
                           else
                           {
                               FinalallFree.Add(intersection,1);
                               if (!(list.Key.Equals(list2.Key)))
                               {
                                   FinalallFreeConnection.Add(intersection, list.Key + " " + list2.Key);
                                   Console.WriteLine("######## 2" + list.Key + " " + list2.Key);
                               }
                               
                           }
                              
                            Console.WriteLine(intersection.Start + " " + intersection.End + " intersection result");
                            intersecFound = true;
                        }



                    }//end foreach
                    if (!intersecFound)
                    {
                        // allFree.Add(t);
                        intersecFound = false;
                    }



             //   }// end if

            }//end foreach

            IEnumerable<FreeTime> noduplicates = allFree.Distinct();
            allFree = noduplicates.ToList();

        }//end method

        private void FinalMutual()
        {
            Boolean intersecFound = false;
            FreeTime intersection = null;
            foreach (var t in allFree)
            {
                intersecFound = false;
                if (allFree != null)///&& list.Count > 1)
                {
                    Console.WriteLine(t.Start + " " + t.End + " in all freeeee");
                    foreach (var s in allFree)
                    {
                        if ((DateTime.Compare(t.Start.Date, s.Start.Date) != 0)|| ((DateTime.Compare(t.Start.Date, s.Start.Date) == 0) && t.End.CompareTo(s.Start) <= 0))
                            continue;


                        intersection = null;
                        Console.WriteLine(s.Start + " " + s.End + " ssssss  in all freeeee");
                        if (!((DateTime.Compare(t.Start.Date, s.Start.Date) == 0) && (DateTime.Compare(t.Start, s.Start) == 0) && (DateTime.Compare(t.End, s.End) == 0)) && (t.Start.Date == s.Start.Date))
                        {
                            Console.WriteLine("callling inteersection 2 ");
                            intersection = Intersection(t, s);
                        }
                        // else if ((list.Count == list2.Count) && ((DateTime.Compare(t.Start.Date, s.Start.Date) == 0) && (DateTime.Compare(t.Start, s.Start) == 0) && (DateTime.Compare(t.End, s.End) == 0)) && (t.Start.Date == s.Start.Date))
                        //  intersection = new FreeTime(t.Start, t.End);
                        // else if ((DateTime.Compare(t.Start.Date, s.Start.Date) == 0) && (DateTime.Compare(t.Start, s.Start) == 0) && (DateTime.Compare(t.End, s.End) == 0))
                        //{
                        //  finalFree.Add(t);
                        //   intersecFound = true;
                        // }
                        if (intersection != null)
                        {
                            finalFree.Add(intersection);
                            Console.WriteLine(intersection.Start + " " + intersection.End + " intersection result");
                            intersecFound = true;
                        }

                    }//end foreach




                }// end if
                Console.WriteLine("check ------------ intersection found  " + !intersecFound);
                if (!intersecFound)
                {
                    Console.WriteLine("no intersection found");
                    intersecFound = false;
                }

            }//end foreach

            IEnumerable<FreeTime> noduplicates = finalFree.Distinct();
            finalFree = noduplicates.ToList();
        }
        

        private void FindMutual2(KeyValuePair<List<FreeTime>, String> pair1, KeyValuePair<List<FreeTime>, String> pair2)
        {

           // Console.WriteLine("now we entered mutual2 " + pair1.Value + " " + pair2.Value);
            //foreach (FreeTime f in pair1.Key)
            //{

            //    Console.WriteLine("Pair 1 " + f.Start + " " + f.End );
            //}

            //foreach (FreeTime f in pair2.Key)
            //{

            //    Console.WriteLine("Pair 2 " + f.Start + " " + f.End);
            //}

            Boolean intersecFound = false;
            FreeTime intersection = null;
            foreach (var t in pair1.Key)
            {
                intersecFound = false;
                if (pair2.Value != null)///&& list.Count > 1)
                {
                    //Console.WriteLine(t.Start + " " + t.End + " tttt");
                    foreach (var s in pair2.Key)
                    {
                        if ((DateTime.Compare(t.Start.Date, s.Start.Date) != 0) ||((DateTime.Compare(t.Start.Date, s.Start.Date) == 0) && t.End.CompareTo(s.Start) <= 0) )
                            continue;
                      //  Console.WriteLine(!((DateTime.Compare(t.Start.Date, s.Start.Date) == 0) && (DateTime.Compare(t.Start, s.Start) == 0) && (DateTime.Compare(t.End, s.End) == 0)) && (t.Start.Date == s.Start.Date));
                        Console.WriteLine(t.Start + " " + t.End + " sssss ");
                        intersection = null;
                        if (!((DateTime.Compare(t.Start.Date, s.Start.Date) == 0) && (DateTime.Compare(t.Start, s.Start) == 0) && (DateTime.Compare(t.End, s.End) == 0)) && (t.Start.Date == s.Start.Date))
                        {
                            //Console.WriteLine("callling inteersection for " + pair1.Value + " " + pair2.Value);
                            intersection = Intersection(t, s);
                        }
                        if (intersection == null)
                        {
                            if (((DateTime.Compare(t.Start.Date, s.Start.Date) == 0) && ((DateTime.Compare(t.Start, s.Start) == 0) && (DateTime.Compare(t.End, s.End) == 0))) && !(pair1.Value.Equals(pair2.Value)))
                                intersection = Intersection(t, s);

                        }
                        //{
                        //    Console.WriteLine("callling inteersection for " + pair1.Value + " " + pair2.Value);
                        //    intersection = Intersection(t, s);
                        //}

                        if (intersection != null)
                        {

                            allFree.Add(intersection);

                            if (FreeBusy.ContainsKey(intersection))
                            {

                              
                               // Console.WriteLine((pair1.Value) + " " + " " + (pair2.Value) + " " + (pair1.Key.ToString()));
                                if (FreeBusy[intersection].IndexOf(pair1.Value) == -1)
                                    FreeBusy[intersection] = FreeBusy[intersection] + " " + pair1.Value;

                                if (FreeBusy[intersection].IndexOf(pair2.Value) == -1)
                                    FreeBusy[intersection] = FreeBusy[intersection] + " " + pair2.Value;

                            }

                            else
                            {
                                FreeBusy.Add(intersection, (pair1.Value + " " + pair2.Value));
                                //Console.WriteLine(intersection.Start + " " + intersection.End + " intersection result 1 between " + (String)pair1.Value + " " + (String)pair2.Value + " ");

                            }

                            intersecFound = true;
                        }

                    }//end foreach
                    if (!intersecFound)
                    {

                        //if (!FreeBusy.ContainsKey(t))
                        //    FreeBusy.Add(t, pair1.Value);
                        //Console.WriteLine(t.Start + " " + t.End + " no intersection between " + (String)pair1.Value + " " + (String)pair2.Value + " ");

                        intersecFound = false;
                    }

                }// end if

                //foreach (FreeTime f in allFree)
                //    Console.WriteLine(f.Start+ " ........ find mutual 2 .............. " +f.End);


            }//end foreach

            IEnumerable<FreeTime> noduplicates = allFree.Distinct();
            allFree = noduplicates.ToList();

        }//end method

        private void FinalMutual2()
        {
            //foreach (KeyValuePair<FreeTime, String> t in FreeBusy)
            //{
            //    Console.WriteLine(t.Key.Start + " $$$$ " + t.Key.End + "  $$$$$ " + t.Value);
            //}

            Boolean intersection = false;
            Boolean intersecFound2 = false;
            foreach (KeyValuePair<FreeTime, String> t in FreeBusy)
            {
                intersecFound2 = false;  // DO NOT REMOVE
               
                //Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

                if (FreeBusy != null)///&& list.Count > 1)
                {
                    //Console.WriteLine(t.Key.Start + " " + t.Key.End + " AAAAAAAAA");
                    foreach (KeyValuePair<FreeTime, String> s in FreeBusy)
                    {
                       if ((DateTime.Compare(t.Key.Start.Date, s.Key.Start.Date) != 0) ||((DateTime.Compare(t.Key.Start.Date, s.Key.Start.Date) == 0) && t.Key.End.CompareTo(s.Key.Start) <= 0))
                            continue;
                     
                        //Console.WriteLine(s.Key.Start + " " + s.Key.End + " sssssssssss");
                        //Console.WriteLine("*****************************************************************");
                        if (!((DateTime.Compare(t.Key.Start.Date, s.Key.Start.Date) == 0) && (DateTime.Compare(t.Key.Start, s.Key.Start) == 0) && (DateTime.Compare(t.Key.End, s.Key.End) == 0)) && (t.Key.Start.Date == s.Key.Start.Date))
                        {

                            intersection = Intersection2(t, s);
                             if (intersection)
                            {
                                intersecFound2 = true;
                            }
                         
                        }
            

                    }//end foreach

                }

                if (!intersecFound2)
                {
                    //Console.WriteLine("intersection found");
                    if (!FinalFreeBusy.ContainsKey(t.Key))
                        FinalFreeBusy.Add(new FreeTime(t.Key.Start, t.Key.End), t.Value);
                   // Console.WriteLine("Added  " + new FreeTime(t.Key.Start, t.Key.End), t.Value.ToString());

                    intersecFound2 = false;

                }
            }

        }//end method


        private void FinalMutual3()
        {
            Dictionary<FreeTime, string> FinalFreeBusy2 = new Dictionary<FreeTime, string>(FinalFreeBusy);
            FinalFreeBusy = new Dictionary<FreeTime, string>();
            //foreach (KeyValuePair<FreeTime, String> t in FinalFreeBusy2)
            //{
            //    Console.WriteLine(t.Key.Start + " $$$$ " + t.Key.End + "  $$$$$ " + t.Value);
            //}

            Boolean intersecFound = false;
            Boolean intersection = false;
            foreach (KeyValuePair<FreeTime, String> t in FinalFreeBusy2)
            {
                //Console.WriteLine(" intersection 3  ");
                intersecFound = false;
                if (FinalFreeBusy2 != null)///&& list.Count > 1)
                {
                    //Console.WriteLine(t.Key.Start + " " + t.Key.End + FinalFreeBusy2);
                    foreach (KeyValuePair<FreeTime, String> s in FinalFreeBusy2)
                    {
                       
                        if ((DateTime.Compare(t.Key.Start.Date, s.Key.Start.Date) != 0)||((DateTime.Compare(t.Key.Start.Date, s.Key.Start.Date) == 0) && t.Key.End.CompareTo(s.Key.Start) <= 0))
                            continue;
                       // Console.WriteLine(s.Key.Start + " " + s.Key.End +" " +s.Value+ " sssssssssss   f3");
                        //Console.WriteLine("*********************final 3 **************");
                        if (!((DateTime.Compare(t.Key.Start, s.Key.Start) == 0) && (DateTime.Compare(t.Key.End, s.Key.End) == 0)) && (t.Key.Start.Date == s.Key.Start.Date) /*&& t.Value.Equals(s.Value)*/ )
                        {
                            //Console.WriteLine("intered interesion in intesection3");
                            intersection = Intersection3(t, s);
                            if (intersection) intersecFound = true;
                        }



                    }//end foreach
                    if (!intersecFound)
                    {
                        if (!FinalFreeBusy.ContainsKey(t.Key))
                            FinalFreeBusy.Add(new FreeTime(t.Key.Start, t.Key.End), t.Value);
                        intersecFound = false;
                    }

                }
            }
            //Console.WriteLine("end of final 3");

            Dictionary<FreeTime, string> FinalFreeBusy3 = new Dictionary<FreeTime, string>(FinalFreeBusy);
            foreach (KeyValuePair<FreeTime, String> first in FinalFreeBusy3)
            {
                foreach (KeyValuePair<FreeTime, String> second in FinalFreeBusy3)
                    if (first.Key.Start.CompareTo(second.Key.Start) == 0 && first.Key.End.CompareTo(second.Key.End) != 0)
                    {
                                        
                    FreeTime r = new FreeTime(new DateTime(Math.Min(first.Key.Start.Ticks, second.Key.Start.Ticks)), new DateTime(Math.Max(first.Key.End.Ticks, second.Key.End.Ticks)));
                    FinalFreeBusy.Remove(r);
                   
                    }
                       
            }
        }//end method


        private static FreeTime Intersection(FreeTime first, FreeTime second)
        {
           // Console.WriteLine("inside intersection " + first.Start + " " + first.End + " sescond " + second.Start + " " + second.End);
            int result1 = DateTime.Compare(first.Start, second.End);
            int result2 = DateTime.Compare(first.End, second.Start);
            int result3 = DateTime.Compare(first.Start, second.Start);
            int result4 = DateTime.Compare(first.End, second.End);
            Boolean equal = (result3 == 0 && result4 == 0);

            if (!(result1 >= 0) && !(result2 <= 0) && !equal)
            {
                //Console.WriteLine(new DateTime(Math.Max(first.Start.Ticks, second.Start.Ticks)).TimeOfDay + "min max" + new DateTime(Math.Min(first.End.Ticks, second.End.Ticks)).TimeOfDay);

                return new FreeTime(new DateTime(Math.Max(first.Start.Ticks, second.Start.Ticks)), new DateTime(Math.Min(first.End.Ticks, second.End.Ticks)));

            }

            else if (result3 == 0 && result4 == 0)
                return (new FreeTime(first.Start, first.End));

            else return null;


        }

        private static Boolean Intersection2(KeyValuePair<FreeTime, String> first, KeyValuePair<FreeTime, String> second)
        {
          
            List<FreeTime> resultList = new List<FreeTime>();
            int result1 = DateTime.Compare(first.Key.Start, second.Key.End);
            int result2 = DateTime.Compare(first.Key.End, second.Key.Start);
            int result3 = DateTime.Compare(first.Key.Start, second.Key.Start);
            int result4 = DateTime.Compare(first.Key.End, second.Key.End);
            String calName;
            Boolean equal = (result3 == 0 && result4 == 0);

            if (!(result1 >= 0) && !(result2 <= 0) && !equal)
            {
               // Console.WriteLine("inside first if  ");
                if (result3 != 0)
                {
                    //Console.WriteLine("gggggggggggggg  1111111111111   ");
                    FreeTime f = new FreeTime(new DateTime(Math.Min(first.Key.Start.Ticks, second.Key.Start.Ticks)), new DateTime(Math.Max(first.Key.Start.Ticks, second.Key.Start.Ticks)));
                    if (result3 < 0) calName = first.Value;
                    else calName = second.Value;
                    if (FinalFreeBusy.Count != 0)
                    {
                        if (!FinalFreeBusy.ContainsKey(f))
                        {
                            FinalFreeBusy.Add(f, calName);
                            //Console.WriteLine("Added " + f.Start + " " + f.End + " " + calName);

                        }


                    }
                    else {
                        if (!FinalFreeBusy.ContainsKey(f))
                            FinalFreeBusy.Add(f, calName); 
                       // Console.WriteLine("Added " + f.Start + " " + f.End + " " + calName); 
                    }


                }


                if (result4 != 0)
                {
                    //Console.WriteLine("gggggggggggggg222222222222  ccccc ");
                    FreeTime f = new FreeTime(new DateTime(Math.Min(first.Key.End.Ticks, second.Key.End.Ticks)), new DateTime(Math.Max(first.Key.End.Ticks, second.Key.End.Ticks)));
                    //Console.WriteLine(f.Start + " " + f.End + "{{{{{{{{{{{{ ");
                    if (result4 < 0) { calName = second.Value; 
                       // Console.WriteLine("gggggggggggggg222222222222  iffff "); 
                    }
                    else calName = first.Value;


                    if (FinalFreeBusy.Count != 0)
                    {
                        //beforeAdding(new KeyValuePair<FreeTime, String>(f, calName));

                        if (!FinalFreeBusy.ContainsKey(f))
                        {
                            //Console.WriteLine("gggggggggggggg222222222222  contains ");
                            FinalFreeBusy.Add(f, calName);
                            //Console.WriteLine("added " + f.Start + " " + f.End + " " + calName);
                        }

                    }
                    else
                    {
                        if (!FinalFreeBusy.ContainsKey(f))
                            FinalFreeBusy.Add(f, calName);
                        //Console.WriteLine("added " + f.Start + " " + f.End + " " + calName);
                    }

                }
                //Console.WriteLine("gggggggggggggg333333333333333  ccccc whyyyyyyy");
                FreeTime r = new FreeTime(new DateTime(Math.Max(first.Key.Start.Ticks, second.Key.Start.Ticks)), new DateTime(Math.Min(first.Key.End.Ticks, second.Key.End.Ticks)));

               // Console.WriteLine("gggggggggggggg333333333333333  ccccc whyyyyyyy222222222");

                if (!FinalFreeBusy.ContainsKey(r))
                {
                    //Console.WriteLine("gggggggggggggg333333333333333  ccccc whyyyyyyy  3333 ");
                    FinalFreeBusy.Add(r, RemoveDuplicateWords(second.Value + " " + first.Value));
                    //Console.WriteLine("added " + r.Start + " " + r.End + " " + RemoveDuplicateWords(second.Value + " "+ first.Value));
                }


                return true;

            }

            if (result3 == 0 && result4 == 0 && !(first.Value.Equals(second.Value)))
            {
                //Console.WriteLine("inside second if  ");
                if (!FinalFreeBusy.ContainsKey((new FreeTime(first.Key.Start, first.Key.End))))
                    FinalFreeBusy.Add(new FreeTime(first.Key.Start, first.Key.End), RemoveDuplicateWords(first.Value + " " + second.Value));
                //Console.WriteLine("added " + new FreeTime(first.Key.Start, first.Key.End).Start + " " + new FreeTime(first.Key.Start, first.Key.End).End + " " + first.Value + second.Value);
                return true;
            }


            //Console.WriteLine("outside if  ");
            return false;

        }

        private static Boolean Intersection3(KeyValuePair<FreeTime, String> first, KeyValuePair<FreeTime, String> second)
        {
            //Console.WriteLine("inside intesection3");

            int result1 = DateTime.Compare(first.Key.Start, second.Key.End);
            int result2 = DateTime.Compare(first.Key.End, second.Key.Start);
            int result3 = DateTime.Compare(first.Key.Start, second.Key.Start);
            int result4 = DateTime.Compare(first.Key.End, second.Key.End);
            String calName;
            Boolean equal = (result3 == 0 && result4 == 0);

            if (!(result1 >= 0) && !(result2 <= 0) && !equal)
            {
                // --------------
                if (result3 != 0 && result4 == 0 && !first.Value.Equals(second.Value))
                {
                   // Console.WriteLine("intersction3 1111111111111   ");
                    FreeTime f = new FreeTime(new DateTime(Math.Min(first.Key.Start.Ticks, second.Key.Start.Ticks)), new DateTime(Math.Max(first.Key.Start.Ticks, second.Key.Start.Ticks)));
                    if (result3 < 0) calName = first.Value;
                    else calName = second.Value;
                    if (FinalFreeBusy.Count != 0)
                    {
                        if (!FinalFreeBusy.ContainsKey(f))
                        {
                            FinalFreeBusy.Add(f, calName);
                            //Console.WriteLine("Added " + f.Start + " " + f.End + " " + calName);
                        }
                        else
                        {
                            //Console.WriteLine("already contained " + f.Start + " " + f.End + " " + FinalFreeBusy[f]);
                            //Console.WriteLine("entered the new code ");
                            //if (!FinalFreeBusy[f].Equals(calName))
                            //{
                            //    Console.WriteLine("entered the new if statement ");
                            //    FinalFreeBusy.Remove(f);
                            //    FinalFreeBusy.Add(f, calName);
                            //    Console.WriteLine("Added " + f.Start + " " + f.End + " " + calName);
                            //}

                            if (FinalFreeBusy[f].IndexOf(calName) == -1)
                            {
                                FinalFreeBusy[f] = FinalFreeBusy[f] + " " + calName;
                                FinalFreeBusy[f]=RemoveDuplicateWords(FinalFreeBusy[f] );
                            }
                               


                        }
                            

                    }
                    else
                    {
                        if (!FinalFreeBusy.ContainsKey(f)) FinalFreeBusy.Add(f, calName);
                        //Console.WriteLine("Added " + f.Start + " " + f.End + " " + calName);
                    }





                   // Console.WriteLine(" new intersction3 1111111111111   ");
                    FreeTime f2 = new FreeTime(new DateTime(Math.Max(first.Key.Start.Ticks, second.Key.Start.Ticks)), new DateTime(Math.Max(first.Key.End.Ticks, second.Key.End.Ticks)));
                    if (result3 < 0) calName = second.Value;
                    else calName = first.Value;
                    if (FinalFreeBusy.Count != 0)
                    {
                        if (!FinalFreeBusy.ContainsKey(f2))
                        {
                            FinalFreeBusy.Add(f2, RemoveDuplicateWords(second.Value + " " + first.Value));
                            //Console.WriteLine("Added " + f2.Start + " " + f2.End + " " + RemoveDuplicateWords(second.Value + " " + first.Value));
                        }
                        else
                        {
                           // Console.WriteLine("already contained " + f2.Start + " " + f2.End + " " + FinalFreeBusy[f2]);
                           // Console.WriteLine("entered the new code ");
                            //if (!FinalFreeBusy[f2].Equals(RemoveDuplicateWords(second.Value + " " + first.Value)))
                            //{
                            //    Console.WriteLine("entered the new if statement ");
                            //    FinalFreeBusy.Remove(f2);
                            //    FinalFreeBusy.Add(f2, RemoveDuplicateWords(second.Value + " " + first.Value));
                            //    Console.WriteLine("Added " + f2.Start + " " + f2.End + " " + RemoveDuplicateWords(second.Value + " " + first.Value));
                            //}
                            if (FinalFreeBusy[f2].IndexOf(calName) == -1)
                            {
                                FinalFreeBusy[f2] = FinalFreeBusy[f2] + " " + calName;
                                FinalFreeBusy[f2]=RemoveDuplicateWords(FinalFreeBusy[f2]);
                            }
                                


                        }
                    }
                    else
                    {
                        if (!FinalFreeBusy.ContainsKey(f2)) FinalFreeBusy.Add(f2, RemoveDuplicateWords(second.Value + " " + first.Value));
                        //Console.WriteLine("Added " + f2.Start + " " + f2.End + " " + RemoveDuplicateWords(second.Value + " " + first.Value));
                    }


                    FreeTime r = new FreeTime(new DateTime(Math.Min(first.Key.Start.Ticks, second.Key.Start.Ticks)), new DateTime(Math.Max(first.Key.End.Ticks, second.Key.End.Ticks)));
                    FinalFreeBusy.Remove(r);
                    //Console.WriteLine("Removed " + r.Start + " " + r.End + " ");


                }

                // ---------------------

                if (result4 != 0 && result3 == 0 && !first.Value.Equals(second.Value))
                {
                   // Console.WriteLine("intersction3  222222222222  ccccc ");
                    FreeTime f = new FreeTime(new DateTime(Math.Min(first.Key.End.Ticks, second.Key.End.Ticks)), new DateTime(Math.Max(first.Key.End.Ticks, second.Key.End.Ticks)));
                    if (result4 < 0) { calName = second.Value; Console.WriteLine("intersction3  222222222222  iffff "); }
                    else calName = first.Value;


                    if (FinalFreeBusy.Count != 0)
                    {
                        if (!FinalFreeBusy.ContainsKey(f))
                        {
                           // Console.WriteLine("intersction3   222222222222  contains ");
                            FinalFreeBusy.Add(f, calName);
                            //Console.WriteLine("added " + f.Start + " " + f.End + " " + calName);
                        }
                        else
                        {
                           // Console.WriteLine("already contained " + f.Start + " " + f.End + " " + FinalFreeBusy[f]);
                          //  Console.WriteLine("entered the new code ");
                            //if (!FinalFreeBusy[f].Equals(calName))
                            //{
                            //    Console.WriteLine("entered the new if statement ");
                            //    FinalFreeBusy.Remove(f);
                            //    FinalFreeBusy.Add(f, calName);
                            //    Console.WriteLine("Added " + f.Start + " " + f.End + " " + calName);
                            //}
                            if (FinalFreeBusy[f].IndexOf(calName) == -1)
                            {
                                FinalFreeBusy[f] = FinalFreeBusy[f] + " " + calName;
                               FinalFreeBusy[f]= RemoveDuplicateWords(FinalFreeBusy[f]);
                            }
                              


                        }
                    }
                    else
                    {
                        FinalFreeBusy.Add(f, calName);
                        //Console.WriteLine("added " + f.Start + " " + f.End + " " + calName);
                    }

                   // Console.WriteLine("new  intersction3  222222222222  ccccc ");
                    FreeTime f2 = new FreeTime(new DateTime(Math.Min(first.Key.Start.Ticks, second.Key.Start.Ticks)), new DateTime(Math.Min(first.Key.End.Ticks, second.Key.End.Ticks)));
                    if (result4 < 0) { calName = first.Value; Console.WriteLine(" new intersction3  222222222222  iffff "); }
                    else calName = second.Value;

                    if (FinalFreeBusy.Count != 0)
                    {

                        if (!FinalFreeBusy.ContainsKey(f2))
                        {
                           // Console.WriteLine(" new intersction3   222222222222  contains ");
                            FinalFreeBusy.Add(f2, RemoveDuplicateWords(second.Value + " " + first.Value));
                            //Console.WriteLine("added new  " + f2.Start + " " + f2.End + " " + RemoveDuplicateWords(second.Value + " " + first.Value));
                        }
                        else
                        {
                           // Console.WriteLine("already contained " + f2.Start + " " + f2.End + " " + FinalFreeBusy[f2]);
                           // Console.WriteLine("entered the new code ");
                            //if (!FinalFreeBusy[f2].Equals(RemoveDuplicateWords(second.Value + " " + first.Value)))
                            //{
                            //    Console.WriteLine("entered the new if statement ");
                            //    FinalFreeBusy.Remove(f2);
                            //    FinalFreeBusy.Add(f2, RemoveDuplicateWords(second.Value + " " + first.Value));
                            //    Console.WriteLine("Added " + f2.Start + " " + f2.End + " " + RemoveDuplicateWords(second.Value + " " + first.Value));
                            //}
                            if (FinalFreeBusy[f2].IndexOf(calName) == -1)
                            {
                                FinalFreeBusy[f2] = FinalFreeBusy[f2] + " " + calName;
                                FinalFreeBusy[f2]= RemoveDuplicateWords(FinalFreeBusy[f2]);

                            }
                                


                        }


                    }
                    else
                    {

                        FinalFreeBusy.Add(f2, RemoveDuplicateWords(second.Value + " " + first.Value));
                      //  Console.WriteLine("added new" + f2.Start + " " + f2.End + " " + RemoveDuplicateWords(second.Value + " " + first.Value));
                    }

                    FreeTime r = new FreeTime(new DateTime(Math.Min(first.Key.Start.Ticks, second.Key.Start.Ticks)), new DateTime(Math.Max(first.Key.End.Ticks, second.Key.End.Ticks)));
                    FinalFreeBusy.Remove(r);
                  //  Console.WriteLine("Removed " + r.Start + " " + r.End + " ");


                }

                //--------------------------------------

                if (first.Value.Equals(second.Value))
                {
                  //  Console.WriteLine("inside values are equal ");
                    FreeTime j = new FreeTime(new DateTime(Math.Min(first.Key.Start.Ticks, second.Key.Start.Ticks)), new DateTime(Math.Max(first.Key.End.Ticks, second.Key.End.Ticks)));
                    if (!FinalFreeBusy.ContainsKey(j))
                    {
                        FinalFreeBusy.Add(j, first.Value);
                      //  Console.WriteLine("added " + j.Start + " " + j.End + " " + first.Value);
                        FinalFreeBusy.Remove(first.Key);
                        FinalFreeBusy.Remove(second.Key);
                      //  Console.WriteLine("Removed " + first.Key.Start + " " + first.Key.End + " ");
                       // Console.WriteLine("Removed " + second.Key.Start + " " + second.Key.End + " ");
                    }
                    else
                    {
                        //if (!FinalFreeBusy[j].Equals(first.Value))
                        //{
                        //    FinalFreeBusy.Remove(j);
                        //    FinalFreeBusy.Add(j, first.Value);
                        //    Console.WriteLine("Added " + j.Start + " " + j.End + " " + first.Value);
                        //}

                        if (FinalFreeBusy[j].IndexOf(first.Value) == -1)
                        {
                            FinalFreeBusy[j] = FinalFreeBusy[j] + " " + first.Value;
                           FinalFreeBusy[j]= RemoveDuplicateWords(FinalFreeBusy[j] );
                        }
                           


                      //  Console.WriteLine("removed without adding");
                        FinalFreeBusy.Remove(first.Key);
                        FinalFreeBusy.Remove(second.Key);
                       // Console.WriteLine("Removed " + first.Key.Start + " " + first.Key.End + " ");
                      //  Console.WriteLine("Removed " + second.Key.Start + " " + second.Key.End + " ");
                    }


                }

                //-------------------

                if (result3 != 0 && !first.Value.Equals(second.Value) && result4 != 0)
                {
                   // Console.WriteLine("inside nor start nor end are equal ");
                    string calName1, calName2;
                    FreeTime f1 = new FreeTime(new DateTime(Math.Min(first.Key.Start.Ticks, second.Key.Start.Ticks)), new DateTime(Math.Max(first.Key.Start.Ticks, second.Key.Start.Ticks)));
                    if (result3 < 0) calName1 = first.Value;
                    else calName1 = second.Value;

                    FreeTime f2 = new FreeTime(new DateTime(Math.Min(first.Key.End.Ticks, second.Key.End.Ticks)), new DateTime(Math.Max(first.Key.End.Ticks, second.Key.End.Ticks)));
                    if (result4 < 0)
                        calName2 = second.Value;
                    else calName2 = first.Value;


                    if (!FinalFreeBusy.ContainsKey(f1))
                    {
                        FinalFreeBusy.Add(f1, calName1);
                       // Console.WriteLine("added " + f1.Start + " " + f1.End + " " + calName1);
                    }
                    else
                    {

                        //if (!FinalFreeBusy[f1].Equals(calName1))
                        //{
                        //    FinalFreeBusy.Remove(f1);
                        //    FinalFreeBusy.Add(f1, calName1);
                        //    Console.WriteLine("Added " + f1.Start + " " + f1.End + " " + calName1);
                        //}

                        if (FinalFreeBusy[f1].IndexOf(calName1) == -1)
                        {
                            FinalFreeBusy[f1] = FinalFreeBusy[f1] + " " + calName1;
                            FinalFreeBusy[f1]=RemoveDuplicateWords(FinalFreeBusy[f1] );
                        }
                            


                    }



                    if (!FinalFreeBusy.ContainsKey(f2))
                    {
                        FinalFreeBusy.Add(f2, calName2);
                      //  Console.WriteLine("added " + f2.Start + " " + f2.End + " " + calName2);
                    }
                    else
                    {
                        //if (!FinalFreeBusy[f2].Equals(calName2))
                        //{
                        //    FinalFreeBusy.Remove(f2);
                        //    FinalFreeBusy.Add(f2, calName2);
                        //    Console.WriteLine("Added " + f2.Start + " " + f2.End + " " + calName2);
                        //}

                        if (FinalFreeBusy[f2].IndexOf(calName2) == -1)
                        {
                            FinalFreeBusy[f2] = FinalFreeBusy[f2] + " " + calName2;

                         FinalFreeBusy[f2]=   RemoveDuplicateWords(FinalFreeBusy[f2]);
                        }
                            
                    }




                    FreeTime r = new FreeTime(new DateTime(Math.Max(first.Key.Start.Ticks, second.Key.Start.Ticks)), new DateTime(Math.Min(first.Key.End.Ticks, second.Key.End.Ticks)));
                    if (!FinalFreeBusy.ContainsKey(r))
                    {
                        FinalFreeBusy.Add(r, RemoveDuplicateWords(second.Value + " " + first.Value));
                        //Console.WriteLine("added " + r.Start + " " + r.End + " " + RemoveDuplicateWords(second.Value + " " + first.Value));
                    }
                    else
                    {

                        //if (!FinalFreeBusy[r].Equals(RemoveDuplicateWords(second.Value + " " + first.Value)))
                        //{
                        //    FinalFreeBusy.Remove(r);
                        //    FinalFreeBusy.Add(r, RemoveDuplicateWords(second.Value + " " + first.Value));
                        //    Console.WriteLine("Added " + r.Start + " " + r.End + " " + RemoveDuplicateWords(second.Value + " " + first.Value));
                        //}

                        if (FinalFreeBusy[r].IndexOf(RemoveDuplicateWords(second.Value + " " + first.Value)) == -1)
                        {
                            FinalFreeBusy[r] = FinalFreeBusy[r] + " " + RemoveDuplicateWords(second.Value + " " + first.Value);
                          FinalFreeBusy[r]=  RemoveDuplicateWords(FinalFreeBusy[r]);
                        }


                    }

                    

                    FreeTime re = new FreeTime(new DateTime(Math.Min(first.Key.Start.Ticks, second.Key.Start.Ticks)), new DateTime(Math.Max(first.Key.End.Ticks, second.Key.End.Ticks)));

                   // Console.WriteLine("Removed " + re.Start + " " + re.End + " ");
                    FinalFreeBusy.Remove(re);


                }

                return true;

            }


            else
                return false;




        }

      

        static private string RemoveDuplicateWords(string v)
        {
            // 1
            // Keep track of words found in this Dictionary.
            var d = new Dictionary<string, bool>();

            // 2
            // Build up string into this StringBuilder.
            StringBuilder b = new StringBuilder();

            // 3
            // Split the input and handle spaces and punctuation.
            string[] a = v.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            // 4
            // Loop over each word
            foreach (string current in a)
            {
                // 5
                // Lowercase each word
                string lower = current.ToLower();

                // 6
                // If we haven't already encountered the word,
                // append it to the result.
                if (!d.ContainsKey(lower))
                {
                    b.Append(current).Append(' ');
                    d.Add(lower, true);
                }
            }
            // 7
            // Return the duplicate words removed
            return b.ToString().Trim();
        }

        #endregion
    }



}

