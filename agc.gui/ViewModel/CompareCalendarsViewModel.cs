using AGC.GUI.Common;
using AGC.Library;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;

namespace AGC.GUI.ViewModel
{
    public class CompareCalendarsViewModel : ViewModelBase
    {
        #region Constants

        private const string SINGLE_MONTH = "Single month";
        private const string ALL_MONTHS = "All months";
        private const string INTERVENING_MONTHS = "Intervening months";
        private const string DEFAULT_VIEW = "All People";
        private const string FREE_BUSY_VIEW = "Some People";
        
        private static List<string> VIEW_FORMAT_LIST = new List<string>(new string[] { DEFAULT_VIEW, FREE_BUSY_VIEW  });


        #endregion

        #region Private Properties

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IGoogleCalendar calendar;
        private readonly ICalendarEventService service;
        private readonly ITimeIntervals period;
        private readonly IRepository repository;
        private readonly IMessanger messanger;

        private enum EventsListType
        {
            Today,
            Tomorrow,
            ThisWeek,
            NextWeek,
            ThisMonth,
            NextMonth,
            Period
        }

        private enum UpdateType
        {
            full,
            confirm,
            makeTentative
        }

        private static List<string> PERIOD_TYPE = new List<string>(new string[] { SINGLE_MONTH, ALL_MONTHS, INTERVENING_MONTHS });

        private EventsListType eventListType = EventsListType.Today;
        private SortFilterPreferences sortFilterPreferences;

        #endregion

        #region Commands
        
        public RelayCommand GetComapringCalendarsCommand { get; private set; }
        public RelayCommand GetTodayEventsCommand { get; private set; }
        public RelayCommand GetTomorrowEventsCommand { get; private set; }
        public RelayCommand GetThisWeekEventsCommand { get; private set; }
        public RelayCommand GetNextWeekEventsCommand { get; private set; }
        public RelayCommand GetThisMonthEventsCommand { get; private set; }
        public RelayCommand GetNextMonthEventsCommand { get; private set; }
        public RelayCommand GetPeriodEventsCommand { get; private set; }
        public RelayCommand RefreshCommand { get; private set; }
        public RelayCommand DeleteEventCommand { get; private set; }
        public RelayCommand UpdateEventCommand { get; private set; }
        public RelayCommand ConfirmEventCommand { get; private set; }
        public RelayCommand MakeTentativeEventCommand { get; private set; }
        public RelayCommand ShowChooseDateEventsControlsCommand { get; private set; }
        public RelayCommand HideChooseDateEventsControlsCommand { get; private set; }
        public RelayCommand GetChooseDateEventsCommand { get; private set; }
        public RelayCommand SetSortingAndFilteringPreferencesCommand { get; private set; }
        public RelayCommand LogOutCommand { get; private set; }

        #endregion

        #region Constructor

        public CompareCalendarsViewModel(IGoogleCalendar googleCalendar, ICalendarEventService eventService, ITimeIntervals timeInterval, IRepository commonRepository, IMessanger commonMessanger)
        {
            try
            {
                log.Debug("Loading EventsList view model...");

                calendar = googleCalendar;
                service = eventService;
                period = timeInterval;
                repository = commonRepository;
                sortFilterPreferences = repository.GetSortFilterPreferences();
                messanger = commonMessanger;

               //ComparingEvents = service.GetComparingEvents(calendar, period.Today());
               // ComparingEvents = service.FormatEventsDatesStringRepresentation(ComparingEvents, repository.GetDateTimePreferences());

                GetComapringCalendarsCommand = new RelayCommand(GetComparingCalendars);
                GetTodayEventsCommand = new RelayCommand(GetTodayEvents);
                GetTomorrowEventsCommand = new RelayCommand(GetTomorrowEvents);
                GetThisWeekEventsCommand = new RelayCommand(GetThisWeekEvents);
                GetNextWeekEventsCommand = new RelayCommand(GetNextWeekEvents);
                GetThisMonthEventsCommand = new RelayCommand(GetThisMonthEvents);
                GetNextMonthEventsCommand = new RelayCommand(GetNextMonthEvents);
                GetPeriodEventsCommand = new RelayCommand(GetPeriodEvents);
                RefreshCommand = new RelayCommand(RefreshEventsList);
                DeleteEventCommand = new RelayCommand(DeleteEvent);
                UpdateEventCommand = new RelayCommand(FullUpdateEvent);
                ConfirmEventCommand = new RelayCommand(ConfirmEvent);
                MakeTentativeEventCommand = new RelayCommand(MakeTentativeEvent);
                ShowChooseDateEventsControlsCommand = new RelayCommand(ShowChooseDateEventsControls);
                HideChooseDateEventsControlsCommand = new RelayCommand(HideChooseDateEventsControls);
                GetChooseDateEventsCommand = new RelayCommand(GetChooseDateEvents);
                SetSortingAndFilteringPreferencesCommand = new RelayCommand(SetSortingAndFilteringPreferences);
                LogOutCommand = new RelayCommand(LogOut);

                log.Debug("EventsList view model was succssfully loaded");
            }
            catch(Exception ex)
            {
                log.Error("Failed to load EventsList view model:", ex);
            }
        }

        #endregion

        #region Public Properties


        public const string ViewFormatListPropertyName = "ViewList";
        private List<string> _viewFormatList = VIEW_FORMAT_LIST;
        public List<string> ViewFormatList
        {
            get
            {
                return _viewFormatList;
            }

            set
            {
                if (_viewFormatList == value)
                {
                    return;
                }

                RaisePropertyChanging(ViewFormatListPropertyName);
                _viewFormatList = value;
                RaisePropertyChanged(ViewFormatListPropertyName);
            }
        }



        public const string PrimaryEmailPropertyName = "primaryEmail";
        private bool _primaryEmail=false ;
        public bool PrimaryEmail
        {
            get
            {
                return _primaryEmail;
            }

            set
            {
                if (_primaryEmail == value)
                {
                    return;
                }

                RaisePropertyChanging(PrimaryEmailPropertyName);
                _primaryEmail = value;
                RaisePropertyChanged(PrimaryEmailPropertyName);
            }
        }


        public const string SelectedViewFormatPropertyName = "ViewFormat";
        private string _SelectedviewFormat = DEFAULT_VIEW;
        public string SelectedViewFormat
        {
            get
            {
                return _SelectedviewFormat;
            }

            set
            {
                if (_SelectedviewFormat == value)
                {
                    return;
                }

                RaisePropertyChanging(SelectedViewFormatPropertyName);
                _SelectedviewFormat = value;
                GetComparingCalendars();
                RaisePropertyChanged(SelectedViewFormatPropertyName);
            }
        }


        public const string ComparingEventsPropertyName = "ComparingEvents";
        private CalendarEventList _Comparingevents;
        public CalendarEventList ComparingEvents
        {
            get
            {
                return _Comparingevents;
            }

            set
            {
                if (_Comparingevents == value)
                {
                    return;
                }

                RaisePropertyChanging(ComparingEventsPropertyName);
                _Comparingevents = value;
                RaisePropertyChanged(ComparingEventsPropertyName);
            }
        }


        public const string ComparingCalendarListPropertyName = "List";
        private String _ComparingCalendarsList;
        public String ComparingCalendarsList
        {
            get
            {
                return _ComparingCalendarsList;
            }

            set
            {
                if (_ComparingCalendarsList == value)
                {
                  return;
                }

                RaisePropertyChanging(ComparingCalendarListPropertyName);
                _ComparingCalendarsList = value;
                RaisePropertyChanged(ComparingCalendarListPropertyName);
            }
        }

        public const string NumberOfMonthToAddPropertyName = "NumberOfMonthToAdd";
        private int _numberOfMonhToAdd = 0;
        public int NumberOfMonthToAdd
        {
            get
            {
                return _numberOfMonhToAdd;
            }

            set
            {
                if (_numberOfMonhToAdd == value)
                {
                    return;
                }

                RaisePropertyChanging(NumberOfMonthToAddPropertyName);
                _numberOfMonhToAdd = value;
                RaisePropertyChanged(NumberOfMonthToAddPropertyName);
            }
        }

        public const string NumberOfMonthToAddRangePropertyName = "NumberOfMonthToAddRange";
        private int[] _numberOfMonthToAddRange = new int[] {12,11,10,9,8,7,6,5,4,3,2,1,0};
        public int[] NumberOfMonthToAddRange
        {
            get
            {
                return _numberOfMonthToAddRange;
            }

            set
            {
                if (_numberOfMonthToAddRange == value)
                {
                    return;
                }

                RaisePropertyChanging(NumberOfMonthToAddRangePropertyName);
                _numberOfMonthToAddRange = value;
                RaisePropertyChanged(NumberOfMonthToAddRangePropertyName);
            }
        }

        public const string SingleMonthPeriodPropertyName = "SingleMonthPeriod";
        private bool _singleMonthPeriod = false;
        public bool SingleMonthPeriod
        {
            get
            {
                return _singleMonthPeriod;
            }

            set
            {
                if (_singleMonthPeriod == value)
                {
                    return;
                }

                RaisePropertyChanging(SingleMonthPeriodPropertyName);
                _singleMonthPeriod = value;
                RaisePropertyChanged(SingleMonthPeriodPropertyName);
            }
        }

        public const string EnableSearchPropertyName = "EnableSearch";
        private bool _enableSearch = false;
        public bool EnableSearch
        {
            get
            {
                return _enableSearch;
            }

            set
            {
                if (_enableSearch == value)
                {
                    return;
                }

                RaisePropertyChanging(EnableSearchPropertyName);
                _enableSearch = value;
                RaisePropertyChanged(EnableSearchPropertyName);
            }
        }

        public const string TextToSearchPropertyName = "TextToSearch";
        private string _textToSearch = String.Empty;
        public String TextToSearch
        {
            get
            {
                return _textToSearch;
            }

            set
            {
                if (_textToSearch == value)
                {
                    return;
                }

                RaisePropertyChanging(TextToSearchPropertyName);
                _textToSearch = value;
                RaisePropertyChanged(TextToSearchPropertyName);
            }
        }

        public const string IsEventsListFocusedPropertyName = "IsEventsListFocused";
        private bool _isEventsListFocused = false;
        public bool IsEventsListFocused
        {
            get
            {
                return _isEventsListFocused;
            }

            set
            {
                if (_isEventsListFocused == value)
                {
                    RaisePropertyChanging(IsEventsListFocusedPropertyName);
                    _isEventsListFocused = false;
                    RaisePropertyChanged(IsEventsListFocusedPropertyName);
                }

                RaisePropertyChanging(IsEventsListFocusedPropertyName);
                _isEventsListFocused = value;
                RaisePropertyChanged(IsEventsListFocusedPropertyName);
            }
        }

        public const string IsDefaultControlsFocusedPropertyName = "IsDefaultControlsFocused";
        private bool _isDefaultControlsFocused = false;
        public bool IsDefaultControlsFocused
        {
            get
            {
                return _isDefaultControlsFocused;
            }

            set
            {
                if (_isDefaultControlsFocused == value)
                {
                    return;
                }

                RaisePropertyChanging(IsDefaultControlsFocusedPropertyName);
                _isDefaultControlsFocused = value;
                RaisePropertyChanged(IsDefaultControlsFocusedPropertyName);
            }
        }

        public const string IsChooseDateControlsFocusedPropertyName = "IsChooseDateControlsFocused";
        private bool _isChooseDateControlsFocused = false;
        public bool IsChooseDateControlsFocused
        {
            get
            {
                return _isChooseDateControlsFocused;
            }

            set
            {
                if (_isChooseDateControlsFocused == value)
                {
                    return;
                }

                RaisePropertyChanging(IsChooseDateControlsFocusedPropertyName);
                _isChooseDateControlsFocused = value;
                RaisePropertyChanged(IsChooseDateControlsFocusedPropertyName);
            }
        }

        public const string SelectedEventPropertyName = "SelectedEvent";
        private CalendarEvent _selectedEvent;
        public CalendarEvent SelectedEvent
        {
            get
            {
                return  _selectedEvent;
            }

            set
            {
                if ( _selectedEvent == value)
                {
                    return;
                }

                RaisePropertyChanging(SelectedEventPropertyName);
                 _selectedEvent = value;
                RaisePropertyChanged(SelectedEventPropertyName);
            }
        }

        public const string ShowChooseDateControlsPropertyName = "ShowChooseDateControls";
        private bool _showChooseDateControls = true;
        public bool ShowChooseDateControls
        {
            get
            {
                return _showChooseDateControls;
            }

            set
            {
                if (_showChooseDateControls == value)
                {
                    return;
                }

                RaisePropertyChanging(ShowChooseDateControlsPropertyName);
                _showChooseDateControls = value;
                RaisePropertyChanged(ShowChooseDateControlsPropertyName);
            }
        }

        public const string ShowDefaultControlsPropertyName = "ShowDefaultControls";
        private bool _showDefaultControls = true;
        public bool ShowDefaultControls
        {
            get
            {
                return _showDefaultControls;
            }

            set
            {
                if (_showDefaultControls == value)
                {
                    return;
                }

                RaisePropertyChanging(ShowDefaultControlsPropertyName);
                _showDefaultControls = value;
                RaisePropertyChanged(ShowDefaultControlsPropertyName);
            }
        }

        public const string EndDateSpecifiedPropertyName = "EndDateSpecified";
        private bool _endDateSpecified = false;
        public bool EndDateSpecified
        {
            get
            {
                return _endDateSpecified;
            }

            set
            {
                if (_endDateSpecified == value)
                {
                    return;
                }

                RaisePropertyChanging(EndDateSpecifiedPropertyName);
                _endDateSpecified = value;
                RaisePropertyChanged(EndDateSpecifiedPropertyName);
            }
        }

        public const string StartDatePropertyName = "StartDate";
        private DateTime _startDate = DateTime.Today;
        public DateTime StartDate
        {
            get
            {
                return _startDate;
            }

            set
            {
                if (_startDate == value)
                {
                    return;
                }

                RaisePropertyChanging(StartDatePropertyName);
                _startDate = value;
                RaisePropertyChanged(StartDatePropertyName);
                EndDate = StartDate.AddDays(30);
            }
        }

        public const string EndDatePropertyName = "EndDate";
        private DateTime _endDate = DateTime.Today;
        public DateTime EndDate
        {
            get
            {
                return _endDate;
            }

            set
            {
                if (_endDate == value)
                {
                    return;
                }

                RaisePropertyChanging(EndDatePropertyName);
                _endDate = value;
                RaisePropertyChanged(EndDatePropertyName);
            }
        }

        public const string PeriodEndMonthPropertyName = "PeriodEndMonth";
        private int _periodEndMonth = DateTime.Today.Month;
        public int PeriodEndMonth
        {
            get
            {
                return _periodEndMonth;
            }

            set
            {
                if (_periodEndMonth == value)
                {
                    return;
                }

                RaisePropertyChanging(PeriodEndMonthPropertyName);
                _periodEndMonth = value;
                RaisePropertyChanged(PeriodEndMonthPropertyName);
            }
        }

        public const string PeriodTypeListPropertyName = "PeriodTypeList";
        private List<string> _periodTypeList = PERIOD_TYPE;
        public List<string> PeriodTypeList
        {
            get
            {
                return _periodTypeList;
            }

            set
            {
                if (_periodTypeList == value)
                {
                    return;
                }

                RaisePropertyChanging(PeriodTypeListPropertyName);
                _periodTypeList = value;
                RaisePropertyChanged(PeriodTypeListPropertyName);
            }
        }

        public const string SelectedPeriodTypePropertyName = "SelectedPeriodType";
        private string _selectedPeriodType = SINGLE_MONTH;
        public string SelectedPeriodType
        {
            get
            {
                return _selectedPeriodType;
            }

            set
            {
                if (_selectedPeriodType == value)
                {
                    return;
                }

                RaisePropertyChanging(SelectedPeriodTypePropertyName);
                _selectedPeriodType = value;
                RaisePropertyChanged(SelectedPeriodTypePropertyName);
            }
        }

        public const string EnableSortingAndFilteringPropertyName = "EnableSortingAndFiltering";
        private bool _enableSortingAndFiltering = false;
        public bool EnableSortingAndFiltering
        {
            get
            {
                return _enableSortingAndFiltering;
            }

            set
            {
                if (_enableSortingAndFiltering == value)
                {
                    return;
                }

                RaisePropertyChanging(EnableSortingAndFilteringPropertyName);
                _enableSortingAndFiltering = value;
                RaisePropertyChanged(EnableSortingAndFilteringPropertyName);
            }
        }

        #endregion

        #region Private Methods

        private void GetComparingCalendars()
        {
           
            //Regex emailFormat = new Regex(@"[\w-]+@([\w-]+\.)+[\w-]+");

            //if (!string.IsNullOrEmpty(mailAddress) && mailIDPattern.IsMatch(mailAddress))
                if(_ComparingCalendarsList != null)
                { 
                List<String>  emailList = convertEmailStringToList(_ComparingCalendarsList);
                ComparingEvents = service.GetComparingEvents(calendar, period.Today(), emailList, _SelectedviewFormat, _primaryEmail);
                ComparingEvents = service.FormatEventsDatesStringRepresentation(ComparingEvents, repository.GetDateTimePreferences());
                SortAndFilterEvents();
                eventListType = EventsListType.Today;
                ShowResults();

                }
            //}
            
        }

        private void GetTodayEvents()
        {
            List<String> emailList = convertEmailStringToList(_ComparingCalendarsList);
            ComparingEvents = service.GetComparingEvents(calendar, period.Today(), emailList, _SelectedviewFormat,_primaryEmail);
            ComparingEvents = service.FormatEventsDatesStringRepresentation(ComparingEvents, repository.GetDateTimePreferences());
            SortAndFilterEvents();
            eventListType = EventsListType.Today;
            ShowResults();   
        }

        private void GetTomorrowEvents()
        {
            List<String> emailList = convertEmailStringToList(_ComparingCalendarsList);
            ComparingEvents = service.GetComparingEvents(calendar, period.Tomorrow(), emailList, _SelectedviewFormat, _primaryEmail);
            ComparingEvents = service.FormatEventsDatesStringRepresentation(ComparingEvents, repository.GetDateTimePreferences());
            SortAndFilterEvents();
            eventListType = EventsListType.Tomorrow;
            ShowResults();  
        }

        private void GetThisWeekEvents()
        {
            List<String> emailList = convertEmailStringToList(_ComparingCalendarsList);
            ComparingEvents = service.GetComparingEvents(calendar, period.ThisWeek(), emailList, _SelectedviewFormat, _primaryEmail);
            ComparingEvents = service.FormatEventsDatesStringRepresentation(ComparingEvents, repository.GetDateTimePreferences());
            SortAndFilterEvents();
            eventListType = EventsListType.ThisWeek;
            ShowResults();
        }

        private void GetNextWeekEvents()
        {
            List<String> emailList = convertEmailStringToList(_ComparingCalendarsList);
            ComparingEvents = service.GetComparingEvents(calendar, period.NextWeek(), emailList, _SelectedviewFormat, _primaryEmail);
            ComparingEvents = service.FormatEventsDatesStringRepresentation(ComparingEvents, repository.GetDateTimePreferences());
            SortAndFilterEvents();
            eventListType = EventsListType.NextWeek;
            ShowResults();
        }

        private void GetThisMonthEvents()
        {
            List<String> emailList = convertEmailStringToList(_ComparingCalendarsList);
            ComparingEvents = service.GetComparingEvents(calendar, period.ThisMonth(), emailList, _SelectedviewFormat, _primaryEmail);
            ComparingEvents = service.FormatEventsDatesStringRepresentation(ComparingEvents, repository.GetDateTimePreferences());
            SortAndFilterEvents();
            eventListType = EventsListType.ThisMonth;
            ShowResults();
        }

        private void GetNextMonthEvents()
        {
            List<String> emailList = convertEmailStringToList(_ComparingCalendarsList);
            ComparingEvents = service.GetComparingEvents(calendar, period.NextMonth(), emailList, _SelectedviewFormat, _primaryEmail);
            ComparingEvents = service.FormatEventsDatesStringRepresentation(ComparingEvents, repository.GetDateTimePreferences());
            SortAndFilterEvents();
            eventListType = EventsListType.NextMonth;
            ShowResults();
        }

        private void GetPeriodEvents()
        {
            //Events = service.GetEvents(calendar.GetAllEvents(), period.NextNMonth(NumberOfMonthToAdd, SingleMonthPeriod));
            List<String> emailList = convertEmailStringToList(_ComparingCalendarsList);
            switch(SelectedPeriodType)
            {

                case SINGLE_MONTH:
                    {
                        ComparingEvents = service.GetComparingEvents(calendar, period.MonthsPeriod(PeriodEndMonth, TimeIntervals.PeriodType.SingleMonth), emailList, _SelectedviewFormat, _primaryEmail);
                        break;
                    }
                case ALL_MONTHS:
                    {
                        ComparingEvents = service.GetComparingEvents(calendar, period.MonthsPeriod(PeriodEndMonth, TimeIntervals.PeriodType.AllMonths), emailList, _SelectedviewFormat, _primaryEmail);
                        break;
                    }
                case INTERVENING_MONTHS:
                    {
                        ComparingEvents = service.GetComparingEvents(calendar, period.MonthsPeriod(PeriodEndMonth, TimeIntervals.PeriodType.InterveningMonths), emailList, _SelectedviewFormat, _primaryEmail);
                        break;
                    }
            }

            ComparingEvents = service.FormatEventsDatesStringRepresentation(ComparingEvents, repository.GetDateTimePreferences());
            SortAndFilterEvents();
            eventListType = EventsListType.Period;
            ShowResults();
        }

        private void DeleteEvent()
        {
            if (!SelectedEvent.IsFake)
            {
                if (SelectedEvent.IsRecurrenceEvent)
                {
                    repository.SetCurrentEvent(SelectedEvent);
                    var deleteEventWindow = new Views.DeleteEventOptionsView();
                    deleteEventWindow.ShowDialog();
                }
                else
                {
                    if (calendar.DeleteEvent(SelectedEvent, GoogleCalendar.ActionType.single))
                    {
                        messanger.Delete("Deleted", false);
                    }
                    else
                    {
                        messanger.Error("Failed to delete event. Please check log file for a detailed information about the error.", false);
                    }
                }
                RefreshEventsList();
            }
        }

        private void UpdateEvent(UpdateType updateType)
        {
            if (SelectedEvent.IsFake)
            {
                return;
            }

            repository.SetCurrentEvent(SelectedEvent);

            if (SelectedEvent.IsRecurrenceEvent)
            {
                var updateEventOptionsWindow = new Views.UpdateEventOptionsView();
                updateEventOptionsWindow.ShowDialog();
            }
            else
            {
                CalendarEventUpdater updateEvent = new CalendarEventUpdater(GoogleCalendar.ActionType.single, SelectedEvent);
                repository.SetEventUpdater(updateEvent);
            }

            if (repository.GetEventUpdater().Type == GoogleCalendar.ActionType.none)
            {
                return;
            }

            if (repository.GetEventUpdater().Type != GoogleCalendar.ActionType.none && updateType == UpdateType.full)
            {
                var updateEventWindow = new Views.UpdateEventView();
                updateEventWindow.ShowDialog();
                RefreshEventsList();
            }
            else
            {
                CalendarEventUpdater eventUpdater = repository.GetEventUpdater();
                eventUpdater.CalendarEvent.Confirmed = updateType == UpdateType.confirm ? true : false;
                eventUpdater.CalendarEvent.RRule = calendar.GetRecurrenceSettings(eventUpdater.CalendarEvent).ToString();
                if (calendar.UpdateEvent(eventUpdater.CalendarEvent, eventUpdater.Type))
                {
                    RefreshEventsList();
                    messanger.Success("Event status changed", false);
                }
                else
                {
                    messanger.Error("Failed to change event status. Please check log file for a detailed information about the error.", false);
                }
            }
        }

        private void FullUpdateEvent()
        {
            UpdateEvent(UpdateType.full);
        }

        private void ConfirmEvent()
        {
            UpdateEvent(UpdateType.confirm);
        }

        private void MakeTentativeEvent()
        {
            UpdateEvent(UpdateType.makeTentative);
        }

        private void ShowChooseDateEventsControls()
        {
            ShowChooseDateControls = true;
            ShowDefaultControls = false;
            IsChooseDateControlsFocused = true;
            IsDefaultControlsFocused = false;
        }

        private void HideChooseDateEventsControls()
        {
            ShowChooseDateControls = false;
            ShowDefaultControls = true;
            IsDefaultControlsFocused = true;
            IsChooseDateControlsFocused = false;
        }

        private void GetChooseDateEvents()
        {
            ComparingEvents = EndDateSpecified ? service.GetEvents(calendar, StartDate, EndDate.AddHours(23).AddMinutes(59).AddSeconds(59)) :
                                        service.GetEvents(calendar, StartDate, DateTime.Today.AddYears(2));

            ComparingEvents = EnableSearch ? service.SearchEvents(ComparingEvents, TextToSearch) : ComparingEvents;
            ComparingEvents = service.FormatEventsDatesStringRepresentation(ComparingEvents, repository.GetDateTimePreferences());
            eventListType = EventsListType.ThisMonth;
            ShowResults();
            HideChooseDateEventsControls();
        }

        private void SetSortingAndFilteringPreferences()
        {
            var sortingAndFilteringWindow = new Views.SortingAndFilteringView();
            sortingAndFilteringWindow.ShowDialog();
            sortFilterPreferences = repository.GetSortFilterPreferences();
            EnableSortingAndFiltering = sortFilterPreferences.Enable;
            RefreshEventsList();
        }

        private void SortAndFilterEvents()
        {
            if (!EnableSortingAndFiltering)
            {
                return;
            }

            if (sortFilterPreferences.EnableSorting)
            {
                ComparingEvents = service.Sort(ComparingEvents, sortFilterPreferences);
            }

            if (sortFilterPreferences.EnableTimeFilter)
            {
                ComparingEvents = service.FilterByStartTime(ComparingEvents, sortFilterPreferences);
            }

            if (sortFilterPreferences.EnableDayOfWeekFilter)
            {
                ComparingEvents = service.FilterByDayOfWeek(ComparingEvents, sortFilterPreferences);
            }

            if (sortFilterPreferences.EnableStatusFilter)
            {
                ComparingEvents = service.FilterByStatus(ComparingEvents, sortFilterPreferences);
            }           
        }

        private void ShowResults()
        {
            if (ComparingEvents.Count == 0)
            {
                messanger.Neutral("No events", false);
            }                
            else
            {
                IsEventsListFocused = true;
            }
        }

        private void RefreshEventsList()
        {
            switch (eventListType)
            {
                case EventsListType.Today:
                    {
                        GetTodayEvents();
                        break;
                    }
                case EventsListType.Tomorrow:
                    {
                        GetTomorrowEvents();
                        break;
                    }
                case EventsListType.ThisWeek:
                    {
                        GetThisWeekEvents();
                        break;
                    }
                case EventsListType.NextWeek:
                    {
                        GetNextWeekEvents();
                        break;
                    }
                case EventsListType.ThisMonth:
                    {
                        GetThisMonthEvents();
                        break;
                    }
                case EventsListType.NextMonth:
                    {
                        GetNextMonthEvents();
                        break;
                    }
                case EventsListType.Period:
                    {
                        GetPeriodEvents();
                        break;
                    }
            }
        }

        private  List<String> convertEmailStringToList(String _ComparingCalendarsList )
        {
            String listOfEmails = _ComparingCalendarsList;
            List<String> emailList = _ComparingCalendarsList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            return emailList;
        }
        private void LogOut()
        {
            calendar.LogOut();          
            RefreshEventsList();
            System.Diagnostics.Process.Start("https://accounts.google.com/Logout");
            Application.Current.Shutdown();
        }

        #endregion
    }
}