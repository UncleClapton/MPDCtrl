﻿/// 
/// 
/// MPD Ctrl
/// https://github.com/torumyax/MPD-Ctrl
/// 
/// TODO:
///  About tab.
///  Password encryption.
///  Test against Mopidy.
///  TrayIcon?
///  Debug tab?
///
/// Known issue:
///  When maximized, there are some extra spaces in the scrollber.
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Configuration;
using System.Net;

namespace WpfMPD
{
    /// <summary>
    /// Profile Class for ObservableCollection<Profile>. 
    /// </summary>
    [Serializable]
    public class Profile
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }
        public string ID { get; set; }
    }

    /// <summary>
    /// ProfileSettings. Wrapper Class for storing ObservableCollection<Profile> in the settings. 
    /// </summary>
    public class ProfileSettings
    {
        public ObservableCollection<Profile> Profiles;
        public  ProfileSettings()
        {
            Profiles = new ObservableCollection<Profile>();
        }
    }

    /// <summary>
    /// MainViewModel Class. 
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        #region PRIVATE FIELD DECLARATION

        private MPC _MPC;
        private string _defaultHost;
        private int _defaultPort;
        private MPC.Song _selectedSong;
        private string _selecctedPlaylist;
        private bool _isChanged;
        private bool _isBusy;
        private string _playButton;
        private double _volume;
        private bool _repeat;
        private bool _random;
        private double _time;
        private double _elapsed;
        private DispatcherTimer _elapsedTimer;
        private bool _showSettings;
        private string _errorMessage;
        private Profile _profile;
        private static string _pathPlayButton = "M15,16H13V8H15M11,16H9V8H11M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private static string _pathPauseButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private static string _pathStopButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private ICommand _playCommand;
        private ICommand _playNextCommand;
        private ICommand _playPrevCommand;
        private ICommand _setRepeatCommand;
        private ICommand _setRandomCommand;
        private ICommand _setVolumeCommand;
        private ICommand _setSeekCommand;
        private ICommand _changeSongCommand;
        private ICommand _changePlaylistCommand;
        private ICommand _windowClosingCommand;
        private ICommand _playPauseCommand;
        private ICommand _playStopCommand;
        private ICommand _volumeMuteCommand;
        private ICommand _volumeDownCommand;
        private ICommand _volumeUpCommand;
        private ICommand _showSettingsCommand;
        private ICommand _newConnectinSettingCommand;
        private ICommand _addConnectinSettingCommand;
        private ICommand _deleteConnectinSettingCommand;

        #endregion END of PRIVATE FIELD declaration

        #region PUBLIC PROPERTY FIELD

        public ObservableCollection<MPC.Song> Songs
        {
            get { if (_MPC != null)
                {
                    return _MPC.CurrentQueue;
                }
                else
                {
                    return null;
                }
            }
        }

        public MPC.Song SelectedSong
        {
            get
            {
                return _selectedSong;
            }
            set
            {
                _selectedSong = value;
                this.NotifyPropertyChanged("SelectedSong");
                if (_MPC != null)
                {
                    if ((value != null) && (_MPC.MpdCurrentSong != null))
                    {
                        //System.Diagnostics.Debug.WriteLine("\n\nListView_SelectionChanged: " + value.Title);
                        if (_MPC.MpdCurrentSong.ID != value.ID)
                        {
                            if (ChangeSongCommand.CanExecute(null))
                            {
                                ChangeSongCommand.Execute(null);
                            }
                        }
                    }
                }
            }
        }

        public ObservableCollection<string> Playlists
        {
            get {
                if (_MPC != null) { 
                    return _MPC.Playlists;
                }
                else
                {
                    return null;
                }
            }
        }

        public string SelectedPlaylist
        {
            get
            {
                return _selecctedPlaylist;
            }
            set
            {
                if (_selecctedPlaylist != value)
                {
                    _selecctedPlaylist = value;
                    this.NotifyPropertyChanged("SelectedPlaylist");

                    if (_selecctedPlaylist != "")
                    {
                        //System.Diagnostics.Debug.WriteLine("\n\nPlaylist_SelectionChanged: " + _selecctedPlaylist);

                        if (ChangePlaylistCommand.CanExecute(null))
                        {
                            ChangePlaylistCommand.Execute(null);
                        }
                    }
                }
            }
        }

        public string PlayButton {
            get
            {
                return this._playButton;
            }
            set
            {
                this._playButton = value;
                this.NotifyPropertyChanged("PlayButton");
            }
        }

        public double Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                this._volume = value;
                this.NotifyPropertyChanged("Volume");

                if (_MPC != null)
                {
                    if (Convert.ToDouble(_MPC.MpdStatus.MpdVolume) != value)
                    {

                        //TODO try using ValueChanged Event using <i:Interaction.Triggers>  ?
                        if (SetVolumeCommand.CanExecute(null))
                        {
                            SetVolumeCommand.Execute(null);
                        }
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("Volume value is the same. Skipping.");
                    }
                }
            }
        }

        public bool Repeat
        {
            get { return _repeat; }
            set
            {
                this._repeat = value;
                this.NotifyPropertyChanged("Repeat");

                if (_MPC != null)
                {
                    if (_MPC.MpdStatus.MpdRepeat != value)
                    {
                        if (SetRpeatCommand.CanExecute(null))
                        {
                            SetRpeatCommand.Execute(null);
                        }
                    }
                }
            }
        }

        public bool Random
        {
            get { return _random; }
            set
            {
                this._random = value;
                this.NotifyPropertyChanged("Random");

                if (_MPC != null)
                {

                    if (_MPC.MpdStatus.MpdRandom != value)
                    {
                        if (SetRandomCommand.CanExecute(null))
                        {
                            SetRandomCommand.Execute(null);
                        }
                    }
                }
            }
        }

        public double Time
        {
            get
            {
                return this._time;
            }
            set
            {
                this._time = value;
                this.NotifyPropertyChanged("Time");
            }
        }

        public double Elapsed
        {
            get
            {
                return this._elapsed;
            }
            set
            {
                this._elapsed = value;
                this.NotifyPropertyChanged("Elapsed");

                if (SetSeekCommand.CanExecute(null))
                {
                    SetSeekCommand.Execute(null);
                }
            }
        }

        public bool IsChanged
        {
            get
            {
                return this._isChanged;
            }
            set
            {
                this._isChanged = value;
                this.NotifyPropertyChanged("IsChanged");
            }
        }

        public bool IsBusy
        {
            get
            {
                return this._isBusy;
            }
            set
            {
                this._isBusy = value;
                this.NotifyPropertyChanged("IsBusy");
            }
        }

        public bool ShowSettings
        {
            get { return this._showSettings; }
            set {
                this._showSettings = value;
                this.NotifyPropertyChanged("IsVisible");
                this.NotifyPropertyChanged("ShowSettings");
            }
        }

        public bool IsVisible
        {
            get {
                if (this._showSettings)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public ObservableCollection<Profile> ProfileList
        {
            get
            {
                return MPDCtrl.Properties.Settings.Default.Profiles.Profiles;
            }
        }

        public Profile SelectedProfile
        {
            get
            {
                return this._profile;
            }
            set
            {
                this._profile = value;
                if (this._profile != null) {
                    // work around validation.
                    this._defaultHost = this._profile.Host;
                    this.NotifyPropertyChanged("Host");
                    this._defaultPort = this._profile.Port;
                    this.NotifyPropertyChanged("Port");
                }
                else
                {
                    this._defaultHost = "";
                    this.NotifyPropertyChanged("Host");
                    this._defaultPort = 6600;
                    this.NotifyPropertyChanged("Port");
                }
                this.NotifyPropertyChanged("SelectedProfile");
            }
        }

        public string Host
        {
            get { return this._defaultHost; }
            set
            {
                ClearErrror("Host");
                this._defaultHost = value;

                // Validate input!
                if (value == "")
                {
                    SetError("Host", "Error: Host must be epecified.");
                }
                else {
                    this._defaultHost = value;
                    IPAddress ipAddress = null;
                    try {
                        ipAddress = IPAddress.Parse(value);
                        if (ipAddress != null) {
                            //
                            //ClearErrror("Host");
                            IsChanged = true;
                        }
                    }
                    catch
                    {
                        //System.FormatException
                        SetError("Host", "Error: Invalid address format.");
                    }
                }
                this.NotifyPropertyChanged("Host");
            }
        }

        public string Port
        {
            get { return this._defaultPort.ToString(); }
            set
            {
                ClearErrror("Port");
                // Validate input. Test with i;
                if (Int32.TryParse(value, out int i))
                {
                    //Int32.TryParse(value, out this._defaultPort)
                    // Change the value only when test was successfull.
                    this._defaultPort = i;
                    ClearErrror("Port");
                    IsChanged = true;
                }
                else
                {
                    SetError("Port", "Error: Part number must be consist of numbers.");
                }
                this.NotifyPropertyChanged("Port");

            }
        }

        public string ErrorMessage
        {
            get
            {
                return this._errorMessage;
            }
            set
            {
                this._errorMessage = value;
                this.NotifyPropertyChanged("ErrorMessage");
            }
        }
        
        #endregion END of PUBLIC PROPERTY FIELD

        // Constructor
        public MainViewModel()
        {
            this._isChanged = false;

            // Initialize play button with "play" state.
            this.PlayButton = _pathPlayButton;

            this._selecctedPlaylist = "";
            this._defaultPort = 6600;

            // Assign commands
            this._playCommand = new WpfMPD.Common.RelayCommand(this.PlayCommand_ExecuteAsync, this.PlayCommand_CanExecute);
            this._playNextCommand = new WpfMPD.Common.RelayCommand(this.PlayNextCommand_ExecuteAsync, this.PlayNextCommand_CanExecute);
            this._playPrevCommand = new WpfMPD.Common.RelayCommand(this.PlayPrevCommand_ExecuteAsync, this.PlayPrevCommand_CanExecute);
            this._setRepeatCommand = new WpfMPD.Common.RelayCommand(this.SetRpeatCommand_ExecuteAsync, this.SetRpeatCommand_CanExecute);
            this._setRandomCommand = new WpfMPD.Common.RelayCommand(this.SetRandomCommand_ExecuteAsync, this.SetRandomCommand_CanExecute);
            this._setVolumeCommand = new WpfMPD.Common.RelayCommand(this.SetVolumeCommand_ExecuteAsync, this.SetVolumeCommand_CanExecute);
            this._setSeekCommand = new WpfMPD.Common.RelayCommand(this.SetSeekCommand_ExecuteAsync, this.SetSeekCommand_CanExecute);
            this._changeSongCommand = new WpfMPD.Common.RelayCommand(this.ChangeSongCommand_ExecuteAsync, this.ChangeSongCommand_CanExecute);
            this._changePlaylistCommand = new WpfMPD.Common.RelayCommand(this.ChangePlaylistCommand_ExecuteAsync, this.ChangePlaylistCommand_CanExecute);
            this._windowClosingCommand = new WpfMPD.Common.RelayCommand(this.WindowClosingCommand_Execute, this.WindowClosingCommand_CanExecute);
            this._playPauseCommand = new WpfMPD.Common.RelayCommand(this.PlayPauseCommand_Execute, this.PlayPauseCommand_CanExecute);
            this._playStopCommand = new WpfMPD.Common.RelayCommand(this.PlayStopCommand_Execute, this.PlayStopCommand_CanExecute);
            this._volumeMuteCommand = new WpfMPD.Common.RelayCommand(this.VolumeMuteCommand_Execute, this.VolumeMuteCommand_CanExecute);
            this._volumeDownCommand = new WpfMPD.Common.RelayCommand(this.VolumeDownCommand_Execute, this.VolumeDownCommand_CanExecute);
            this._volumeUpCommand = new WpfMPD.Common.RelayCommand(this.VolumeUpCommand_Execute, this.VolumeUpCommand_CanExecute);
            this._showSettingsCommand = new WpfMPD.Common.RelayCommand(this.ShowSettingsCommand_Execute, this.ShowSettingsCommand_CanExecute);

            this._newConnectinSettingCommand = new WpfMPD.Common.RelayCommand(this.NewConnectinSettingCommand_Execute, this.NewConnectinSettingCommand_CanExecute);
            this._addConnectinSettingCommand = new WpfMPD.Common.RelayCommand(this.AddConnectinSettingCommand_Execute, this.AddConnectinSettingCommand_CanExecute);
            this._deleteConnectinSettingCommand = new WpfMPD.Common.RelayCommand(this.DeleteConnectinSettingCommand_Execute, this.DeleteConnectinSettingCommand_CanExecute);



            // Upgrade settings. (just in case.)
            MPDCtrl.Properties.Settings.Default.Upgrade();

            // Load settings.

            // Must be the first time.
            if (MPDCtrl.Properties.Settings.Default.Profiles == null)
            {
                MPDCtrl.Properties.Settings.Default.Profiles = new ProfileSettings();
            }

            // Profile setting is empty.
            if (MPDCtrl.Properties.Settings.Default.Profiles.Profiles.Count < 1)
            {
                ShowSettings = true;
            }
            else
            {
                var item = ProfileList.FirstOrDefault(i => i.ID == MPDCtrl.Properties.Settings.Default.DefaultProfileID);
                if (item != null)
                {
                    // This should propagate values for _defaultHost and _defaultPort.
                    SelectedProfile = (item as Profile);
                    // just in case.
                    ShowSettings = false;
                    // start client.
                    StartConnection();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Profile where DefaultProfileID is NULL");
                    ShowSettings = true;
                }
            }
        }

        #region PRIVATE METHODS

        private async Task<bool> StartConnection()
        {
            // Create MPC instance.
            this._MPC = new MPC(this._defaultHost, this._defaultPort);

            // Assign idle event.
            this._MPC.StatusChanged += new MPC.MpdStatusChanged(OnStatusChanged);
            this._MPC.ErrorReturned += new MPC.MpdError(OnError);

            // Init Song's time elapsed timer.
            _elapsedTimer = new DispatcherTimer();
            _elapsedTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _elapsedTimer.Tick += new EventHandler(ElapsedTimer);

            // Connect to MPD server and query status and info.
            if (await QueryStatus()) {
                // Start idle connection.
                if (await ConnectIdle())
                {
                    // Start idle.
                    //return await _MPC.MpdIdleStart();
                    // Or not.

                    ErrorMessage = "";

                    return true;
                }
            }
            return false;
        }

        private async Task<bool> ConnectIdle()
        {
            return await _MPC.MpdIdleConnect();
        }

        private async void DisConnectIdle()
        {
            bool isDone = await _MPC.MpdIdleDisConnect();
        }

        private async void OnStatusChanged(MPC sender, object data)
        {
            if (data == null) { return; }

            // list of SubSystems we are subscribing.
            // player mixer options playlist stored_playlist

            bool isPlayer = false;
            bool isPlaylist = false;
            bool isStoredPlaylist = false;
            foreach (var subsystem in (data as List<string>))
            {
                //System.Diagnostics.Debug.WriteLine("OnStatusChanged: " + subsystem);

                if (subsystem == "player")
                {
                    isPlayer = true;
                }
                else if (subsystem == "mixer")
                {
                    isPlayer = true;
                }
                else if (subsystem == "options")
                {
                    isPlayer = true;
                }
                else if (subsystem == "playlist")
                {
                    isPlaylist = true;
                }
                else if (subsystem == "stored_playlist")
                {
                    isStoredPlaylist = true;
                }
            }

            // Little dirty, but ObservableCollection isn't thread safe, so...
            if (IsBusy) {
                await Task.Delay(1000);
                if (IsBusy)
                {
                    await Task.Delay(1000);
                    if (IsBusy)
                    {
                        System.Diagnostics.Debug.WriteLine("OnStatusChanged: TIME OUT");
                        return;
                    }
                }
            }

            if ((isPlayer && isPlaylist))
            {
                System.Diagnostics.Debug.WriteLine("OnStatusChanged: isPlayer & isPlaylist");

                IsBusy = true;

                // Reset view.
                sender.CurrentQueue.Clear();
                this._selectedSong = null;
                //this.NotifyPropertyChanged("SelectedSong");
                //this._selecctedPlaylist = "";
                //this.NotifyPropertyChanged("SelectedPlaylist");
                //UpdateButtonStatus();

                // Get updated information.
                bool isDone = await sender.MpdQueryCurrentPlaylist();
                if (isDone)
                {
                    System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryCurrentPlaylist> is done.");

                    _selecctedPlaylist = "";
                    this.NotifyPropertyChanged("SelectedPlaylist");

                    // Update status.
                    isDone = await sender.MpdQueryStatus();
                    if (isDone)
                    {
                        System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryStatus> is done.");

                        try
                        {
                            var item = _MPC.CurrentQueue.FirstOrDefault(i => i.ID == _MPC.MpdStatus.MpdSongID);
                            if (item != null)
                            {
                                //sender.MpdCurrentSong = (item as MPC.Song);
                                this._selectedSong = (item as MPC.Song);
                                this.NotifyPropertyChanged("SelectedSong");
                                System.Diagnostics.Debug.WriteLine("OnStatusChanged isPlayer & isPlaylist SelectedSong is : " + this._selectedSong.Title);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("OnStatusChanged isPlayer & isPlaylist SelectedSong is NULL");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("_MPC.CurrentQueue.FirstOrDefault@(isPlaylist) failed: " + ex.Message);
                        }

                        // Listview selection changed event in the code behind takes care ScrollIntoView. 
                        // This is a VIEW matter.

                        IsBusy = false;
                        UpdateButtonStatus();

                        //Testing
                        //await _MPC.MpdIdleStart();
                    }
                    else
                    {
                        IsBusy = false;
                        // Let user know.
                        System.Diagnostics.Debug.WriteLine("MpdQueryStatus returned with false." + "\n");
                    }

                    if (isStoredPlaylist)
                    {
                        // Retrieve playlists
                        sender.Playlists.Clear();
                        isDone = await sender.MpdQueryPlaylists();
                        if (isDone)
                        {
                            //System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                            // Selected item should now read "Current Play Queue"
                            //https://stackoverflow.com/questions/2343446/default-text-for-templated-combo-box?rq=1

                            IsBusy = false;
                        }
                        else
                        {
                            IsBusy = false;
                            //TODO: Let user know.
                            System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                        }
                    }
                    else
                    {
                        IsBusy = false;
                    }
                }
                else
                {
                    IsBusy = false;
                    //TODO: Let user know.
                    System.Diagnostics.Debug.WriteLine("QueryCurrentPlayQueue returned false." + "\n");
                }
            }
            else if (isPlaylist)
            {
                System.Diagnostics.Debug.WriteLine("OnStatusChanged: isPlaylist");
                IsBusy = true;

                // Reset view.
                sender.CurrentQueue.Clear();
                this._selectedSong = null;
                //this.NotifyPropertyChanged("SelectedSong");
                //this._selecctedPlaylist = "";
                //this.NotifyPropertyChanged("SelectedPlaylist");
                //UpdateButtonStatus();

                // Get updated information.
                bool isDone = await sender.MpdQueryCurrentPlaylist();
                if (isDone)
                {
                    System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryCurrentPlaylist> is done.");

                    _selecctedPlaylist = "";
                    this.NotifyPropertyChanged("SelectedPlaylist");

                    // Update status.
                    isDone = await sender.MpdQueryStatus();
                    if (isDone)
                    {
                        System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryStatus> is done.");

                        try
                        {
                            var item = _MPC.CurrentQueue.FirstOrDefault(i => i.ID == _MPC.MpdStatus.MpdSongID);
                            if (item != null)
                            {
                                //sender.MpdCurrentSong = (item as MPC.Song);
                                this._selectedSong = (item as MPC.Song);
                                this.NotifyPropertyChanged("SelectedSong");
                                System.Diagnostics.Debug.WriteLine("OnStatusChanged isPlaylist SelectedSong is : " + this._selectedSong.Title);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("OnStatusChanged isPlaylist SelectedSong is NULL");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("_MPC.CurrentQueue.FirstOrDefault@(isPlaylist) failed: " + ex.Message);
                        }

                        IsBusy = false;
                        UpdateButtonStatus();

                        //Testing
                        //await _MPC.MpdIdleStart();

                        if (isStoredPlaylist)
                        {
                            // Retrieve playlists
                            sender.Playlists.Clear();
                            isDone = await sender.MpdQueryPlaylists();
                            if (isDone)
                            {
                                //System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                                // Selected item should now read "Current Play Queue"
                                //https://stackoverflow.com/questions/2343446/default-text-for-templated-combo-box?rq=1

                                //IsBusy = false;
                            }
                            else
                            {
                                //IsBusy = false;
                                //TODO: Let user know.
                                System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                            }
                        }
                        else
                        {
                            IsBusy = false;
                        }
                    }
                    else
                    {
                        IsBusy = false;
                        // Let user know.
                        System.Diagnostics.Debug.WriteLine("MpdQueryStatus returned with false." + "\n");
                    }
                }
                else
                {
                    IsBusy = false;
                    //TODO: Let user know.
                    System.Diagnostics.Debug.WriteLine("QueryCurrentPlayQueue returned false." + "\n");
                }
            }
            else if (isPlayer)
            {
                System.Diagnostics.Debug.WriteLine("OnStatusChanged: isPlayer");

                IsBusy = true;

                // Reset view.
                //this._selectedSong = null;
                //this.NotifyPropertyChanged("SelectedSong");
                //UpdateButtonStatus();

                // Update status.
                bool isDone = await sender.MpdQueryStatus();
                if (isDone)
                {
                    System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryStatus> is done.");
                    
                    try
                    {
                        var item = _MPC.CurrentQueue.FirstOrDefault(i => i.ID == _MPC.MpdStatus.MpdSongID);
                        if (item != null)
                        {
                            //sender.MpdCurrentSong = (item as MPC.Song);
                            this._selectedSong = (item as MPC.Song);
                            this.NotifyPropertyChanged("SelectedSong");
                            System.Diagnostics.Debug.WriteLine("OnStatusChanged isPlayer SelectedSong is : " + this._selectedSong.Title);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("OnStatusChanged isPlayer SelectedSong is NULL");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("_MPC.CurrentQueue.FirstOrDefault@(isPlayer) failed: " + ex.Message);
                    }

                    // Listview selection changed event in the code behind takes care ScrollIntoView. 
                    // This is a VIEW matter.

                    IsBusy = false;
                    UpdateButtonStatus();

                    //Testing
                    //await _MPC.MpdIdleStart();

                    if (isStoredPlaylist)
                    {
                        // Retrieve playlists
                        sender.Playlists.Clear();
                        isDone = await sender.MpdQueryPlaylists();
                        if (isDone)
                        {
                            //System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                            // Selected item should now read "Current Play Queue"
                            //https://stackoverflow.com/questions/2343446/default-text-for-templated-combo-box?rq=1

                            //IsBusy = false;
                        }
                        else
                        {
                            IsBusy = false;
                            //TODO: Let user know.
                            System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                        }
                    }
                    else
                    {
                        //IsBusy = false;
                    }
                }
                else
                {
                    IsBusy = false;
                    // Let user know.
                    System.Diagnostics.Debug.WriteLine("MpdQueryStatus returned with false." + "\n");
                }
            }
            else if (isStoredPlaylist)
            {
                this._selecctedPlaylist = "";
                this.NotifyPropertyChanged("SelectedPlaylist");

                if (isStoredPlaylist)
                {
                    // Retrieve playlists
                    sender.Playlists.Clear();
                    bool isDone = await sender.MpdQueryPlaylists();
                    if (isDone)
                    {
                        //System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                        // Selected item should now read "Current Play Queue"
                        //https://stackoverflow.com/questions/2343446/default-text-for-templated-combo-box?rq=1

                        //IsBusy = false;
                    }
                    else
                    {
                        //IsBusy = false;
                        //TODO: Let user know.
                        System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                    }

                    //Testing
                    //await _MPC.MpdIdleStart();
                }
                else
                {
                    IsBusy = false;
                }
            }

            // Don't.It's already done.
            //bool isDone = await _MPC.MpdIdleStart();
        }

        private async Task<bool> QueryStatus()
        {
            //IsBusy = true;
            bool isDone = await _MPC.MpdQueryStatus();
            if (isDone)
            {
                System.Diagnostics.Debug.WriteLine("QueryStatus is done.");

                UpdateButtonStatus();
                //IsBusy = false;

                // Retrieve play queue
                return await QueryCurrentPlayQueue();
            }
            else
            {
                //IsBusy = false;
                //TODO: connection fail to establish. 
                // Let user know.
                System.Diagnostics.Debug.WriteLine("QueryStatus returned with false." + "\n");
                return false;
            }
        }

        private async Task<bool> QueryCurrentPlayQueue()
        {
            IsBusy = true;
            bool isDone = await _MPC.MpdQueryCurrentPlaylist();
            if (isDone)
            {
                System.Diagnostics.Debug.WriteLine("QueryCurrentPlaylist is done.");

                if (_MPC.CurrentQueue.Count > 0) {
                    
                    var listItem = _MPC.CurrentQueue.Where(i => i.ID == _MPC.MpdStatus.MpdSongID);
                    if (listItem != null)
                    {
                        foreach (var item in listItem)
                        {
                            this._MPC.MpdCurrentSong = (item as MPC.Song);
                            break;
                            
                        }
                    }
                    // Change it "quietly".
                    this._selectedSong = _MPC.MpdCurrentSong;
                    // Let listview know it is changed.
                    this.NotifyPropertyChanged("SelectedSong");

                    // Listview selection changed event in the code behind takes care ScrollIntoView. 
                    // This is a VIEW matter.
                }

                IsBusy = false;

                // Retrieve playlists
                return await QueryPlaylists();
            }
            else
            {
                IsBusy = false;
                // Let user know.
                System.Diagnostics.Debug.WriteLine("QueryCurrentPlayQueue returned false." + "\n");
                return false;
            }

        }

        private async Task<bool> QueryPlaylists()
        {
            IsBusy = true;
            bool isDone = await _MPC.MpdQueryPlaylists();
            if (isDone)
            {
                System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                //selected item should now read "Current Play Queue"
                //https://stackoverflow.com/questions/2343446/default-text-for-templated-combo-box?rq=1

                IsBusy = false;

                return true;
            }
            else
            {
                IsBusy = false;
                // Let user know.
                System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                return false;
            }
        }

        private void OnError(MPC sender, object data)
        {
            if (data == null) { return; }

            //TODO: How and when do you clear the error message?
            ErrorMessage = (data as string);
        }

        private void UpdateButtonStatus()
        {
            //Play button
            switch (_MPC.MpdStatus.MpdState)
            {
                case MPC.Status.MpdPlayState.Play:
                    {
                        this.PlayButton = _pathPlayButton;
                        break;
                    }
                case MPC.Status.MpdPlayState.Pause:
                    {
                        this.PlayButton = _pathPauseButton;
                        break;
                    }
                case MPC.Status.MpdPlayState.Stop:
                    {
                        this.PlayButton = _pathStopButton;
                        break;
                    }
            }

            // "quietly" update view.
            this._volume = Convert.ToDouble(_MPC.MpdStatus.MpdVolume);
            this.NotifyPropertyChanged("Volume");

            this._random = _MPC.MpdStatus.MpdRandom;
            this.NotifyPropertyChanged("Random");

            this._repeat = _MPC.MpdStatus.MpdRepeat;
            this.NotifyPropertyChanged("Repeat");

            // no need to care about "double" updates for time.
            this.Time = _MPC.MpdStatus.MpdSongTime;

            this._elapsed = _MPC.MpdStatus.MpdSongElapsed;
            this.NotifyPropertyChanged("Elapsed");

            //start elapsed timer.
            if (_MPC.MpdStatus.MpdState == MPC.Status.MpdPlayState.Play)
            {
                _elapsedTimer.Start();
            }
            else
            {
                _elapsedTimer.Stop();
            }
        }

        private void ElapsedTimer(object sender, EventArgs e)
        {
            if ((_elapsed < _time) && (_MPC.MpdStatus.MpdState == MPC.Status.MpdPlayState.Play))
            {
                this._elapsed += 1;
                this.NotifyPropertyChanged("Elapsed");
            }
            else
            {
                _elapsedTimer.Stop();
            }
        }

        #endregion END of PRIVATE METHODS

        #region COMMANDS

        public ICommand PlayCommand { get { return this._playCommand; } }

        public bool PlayCommand_CanExecute()
        {
            if (this.IsBusy) { return false; } 
            if (_MPC == null) { return false; }
            if (_MPC.CurrentQueue.Count < 1) { return false; }
            return true;
        }

        public async void PlayCommand_ExecuteAsync()
        {
            bool isDone = false;
            switch (_MPC.MpdStatus.MpdState)
            {
                case MPC.Status.MpdPlayState.Play:
                    {
                        //State>>Play: So, send Pause command
                        isDone = await _MPC.MpdPlaybackPause();
                        break;
                    }
                case MPC.Status.MpdPlayState.Pause:
                    {
                        //State>>Pause: So, send Resume command
                        isDone = await _MPC.MpdPlaybackResume();
                        break;
                    }
                case MPC.Status.MpdPlayState.Stop:
                    {
                        //State>>Stop: So, send Play command
                        isDone = await _MPC.MpdPlaybackPlay();
                        break;
                    }
            }

            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();
                /*
                var listItem = _MPC.CurrentQueue.Where(i => i.ID == _MPC.MpdStatus.MpdSongID);
                if (listItem != null)
                {
                    foreach (var item in listItem)
                    {
                        this._MPC.MpdCurrentSong = (item as MPC.Song);
                        break;
                    }
                }
                // Change it "quietly".
                this._selectedSong = _MPC.MpdCurrentSong;
                // Let listview know it is changed.
                this.NotifyPropertyChanged("SelectedSong");
                */
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PlayCommand returned false." + "\n");
            }

        }

        public ICommand PlayNextCommand { get { return this._playNextCommand; } }

        public bool PlayNextCommand_CanExecute()
        {
            if (this.IsBusy) { return false; }
            if (_MPC == null) { return false; }
            if (_MPC.CurrentQueue.Count < 1) { return false; }
            return true;
        }

        public async void PlayNextCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MpdPlaybackNext();

            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();
                /*
                var listItem = _MPC.CurrentQueue.Where(i => i.ID == _MPC.MpdStatus.MpdSongID);
                if (listItem != null)
                {
                    foreach (var item in listItem)
                    {
                        this._MPC.MpdCurrentSong = (item as MPC.Song);
                        break;
                    }
                }
                // Change it "quietly".
                this._selectedSong = _MPC.MpdCurrentSong;
                // Let listview know it is changed.
                this.NotifyPropertyChanged("SelectedSong");
                */
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PlayNextCommand returned false." + "\n");
            }
        }

        public ICommand PlayPrevCommand { get { return this._playPrevCommand; } }

        public bool PlayPrevCommand_CanExecute()
        {
            if (this.IsBusy) { return false; }
            if (_MPC == null) { return false; }
            if (_MPC.CurrentQueue.Count < 1) { return false; }
            return true;
        }

        public async void PlayPrevCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MpdPlaybackPrev();

            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();
                /*
                var listItem = _MPC.CurrentQueue.Where(i => i.ID == _MPC.MpdStatus.MpdSongID);
                if (listItem != null)
                {
                    foreach (var item in listItem)
                    {
                        this._MPC.MpdCurrentSong = (item as MPC.Song);
                        break;
                    }
                }
                // Change it "quietly".
                this._selectedSong = _MPC.MpdCurrentSong;
                // Let listview know it is changed.
                this.NotifyPropertyChanged("SelectedSong");
                */
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PlayPrevCommand returned false. " + "\n");
            }
        }

        public ICommand SetRpeatCommand { get { return this._setRepeatCommand; } }

        public bool SetRpeatCommand_CanExecute()
        {
            //if (this.IsBusy) { return false; }
            if (_MPC == null) { return false; }
            return true;
        }

        public async void SetRpeatCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MpdSetRepeat(this._repeat);

            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdSetRepeat returned false");
            }
        }

        public ICommand SetRandomCommand { get { return this._setRandomCommand; } }

        public bool SetRandomCommand_CanExecute()
        {
            //if (this.IsBusy) { return false; }
            if (_MPC == null) { return false; }
            return true;
        }

        public async void SetRandomCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MpdSetRandom(this._random);

            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdSetRandom returned false");
            }
        }

        public ICommand SetVolumeCommand { get { return this._setVolumeCommand; } }

        public bool SetVolumeCommand_CanExecute()
        {
            //if (this.IsBusy) { return false; }
            if (_MPC == null) { return false; }
            return true;
        }

        public async void SetVolumeCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MpdSetVolume(Convert.ToInt32(this._volume));

            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdSetVolume returned false");
            }
        }

        public ICommand SetSeekCommand { get { return this._setSeekCommand; } }

        public bool SetSeekCommand_CanExecute()
        {
            if (this.IsBusy) { return false; }
            if (_MPC == null) { return false; }
            return true;
        }

        public async void SetSeekCommand_ExecuteAsync()
        {
            
            bool isDone = await _MPC.MpdPlaybackSeek(_MPC.MpdStatus.MpdSongID, Convert.ToInt32(this._elapsed));

            if (isDone)
            {
                //Don't need to. Timer takes care of updating slider elapsed value.
                //UpdateButtonStatus();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdPlaybackSeek returned false");
            }
            
        }

        public ICommand ChangeSongCommand { get { return this._changeSongCommand; } }

        public bool ChangeSongCommand_CanExecute()
        {
            if (this.IsBusy) { return false; }
            if (_MPC == null) { return false; }
            if (_MPC.CurrentQueue.Count < 1) { return false; }
            if (_selectedSong == null) { return false; }
            return true;
        }

        public async void ChangeSongCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MpdPlaybackPlay(_selectedSong.ID);
            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();

                /*
                var listItem = _MPC.CurrentQueue.Where(i => i.ID == _MPC.MpdStatus.MpdSongID);
                if (listItem != null)
                {
                    foreach (var item in listItem)
                    {
                        this._MPC.MpdCurrentSong = (item as MPC.Song);
                        break;
                    }
                }
                // Change it "quietly".
                this._selectedSong = _MPC.MpdCurrentSong;
                // Let listview know it is changed.
                this.NotifyPropertyChanged("SelectedSong");
                */
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdPlaybackPlay returned false");
            }
        }

        public ICommand ChangePlaylistCommand { get { return this._changePlaylistCommand; } }

        public bool ChangePlaylistCommand_CanExecute()
        {
            //if (this.IsBusy) { return false; }
            if (_MPC == null) { return false; }
            if (this._selecctedPlaylist == "") { return false; }
            return true;
        }

        public async void ChangePlaylistCommand_ExecuteAsync()
        {
            // Little dirty, but since our ObservableCollection isn't thread safe...
            if (IsBusy)
            {
                await Task.Delay(1000);
                if (IsBusy)
                {
                    await Task.Delay(1500);
                    if (IsBusy)
                    {
                        System.Diagnostics.Debug.WriteLine("ChangePlaylistCommand_ExecuteAsync: TIME OUT");
                        return;
                    }
                }
            }

            if (this._selecctedPlaylist == "") { return; }
           
            IsBusy = true;

            _MPC.CurrentQueue.Clear();
            this._selectedSong = null;

            //MPD >> clear load playlistinfo > returns and updates playlist.
            bool isDone = await _MPC.MpdChangePlaylist(this._selecctedPlaylist);
            if (isDone)
            {
                _selecctedPlaylist = "";
                this.NotifyPropertyChanged("SelectedPlaylist");

                if (_MPC.CurrentQueue.Count > 0) {
                    /*
                    //Start play. MPD >> play status > returns and update status.
                    isDone = await _MPC.MpdPlaybackPlay();
                    if (isDone)
                    {
                        try
                        {
                            var item = _MPC.CurrentQueue.FirstOrDefault(i => i.ID == _MPC.MpdStatus.MpdSongID);
                            if (item != null)
                            {
                                //sender.MpdCurrentSong = (item as MPC.Song);
                                this._selectedSong = (item as MPC.Song);
                                this.NotifyPropertyChanged("SelectedSong");
                                System.Diagnostics.Debug.WriteLine("ChangePlaylistCommand_ExecuteAsync SelectedSong is : " + this._selectedSong.Title);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("_MPC.CurrentQueue.FirstOrDefault@(isPlayer) failed@ChangePlaylistCommand_ExecuteAsync: " + ex.Message);
                        }

                        // Don't. Let idle connection do the job.
                        //UpdateButtonStatus();

                        //if (_MPC.MpdCurrentSong != null)
                        //{
                        //    //change it quietly.
                        //    this._selectedSong = _MPC.MpdCurrentSong;
                        //    //let listview know it is changed.
                        //    this.NotifyPropertyChanged("SelectedSong");
                        //}

                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("\n\nMpdPlaybackPlay returned false");
                    }
                    */
                }
                
                IsBusy = false;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdChangePlaylist returned false");
            }

            //TODO make other controls disabled
            //https://stackoverflow.com/questions/7346663/how-to-show-a-waitcursor-when-the-wpf-application-is-busy-databinding

        }

        public ICommand WindowClosingCommand { get { return this._windowClosingCommand; } }

        public bool WindowClosingCommand_CanExecute()
        {
            return true;
        }

        public void WindowClosingCommand_Execute()
        {
            //System.Diagnostics.Debug.WriteLine("WindowClosingCommand");

            // Disconnect idle connection.
            if (_MPC != null) {
                DisConnectIdle();
            }
        }

        public ICommand PlayPauseCommand { get { return this._playPauseCommand; } }

        public bool PlayPauseCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }

        public async void PlayPauseCommand_Execute()
        {
            bool isDone = await _MPC.MpdPlaybackPause();
            if (isDone)
            {
                // Idle connection takes care of the rest.
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PlayPauseCommand returned false.");
            }
        }

        public ICommand PlayStopCommand { get { return this._playStopCommand; } }

        public bool PlayStopCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }

        public async void PlayStopCommand_Execute()
        {
            bool isDone = await _MPC.MpdPlaybackStop();
            if (isDone)
            {
                // Idle connection takes care of the rest.
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PlayStopCommand returned false.");
            }
        }

        public ICommand VolumeMuteCommand { get { return this._volumeMuteCommand; } }

        public bool VolumeMuteCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }

        public async void VolumeMuteCommand_Execute()
        {
            bool isDone = await _MPC.MpdSetVolume(0);
            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\nVolumeMuteCommand returned false.");
            }
        }

        public ICommand VolumeDownCommand { get { return this._volumeDownCommand; } }

        public bool VolumeDownCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }

        public async void VolumeDownCommand_Execute()
        {
            if (this._volume >= 10) {
                bool isDone = await _MPC.MpdSetVolume(Convert.ToInt32(this._volume - 10));
                if (isDone)
                {
                    // Don't. Let idle connection do the job.
                    //UpdateButtonStatus();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("\nVolumeDownCommand returned false.");
                }
            }
        }

        public ICommand VolumeUpCommand { get { return this._volumeUpCommand; } }

        public bool VolumeUpCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }

        public async void VolumeUpCommand_Execute()
        {
            if (this._volume <= 90)
            {
                bool isDone = await _MPC.MpdSetVolume(Convert.ToInt32(this._volume + 10));
                if (isDone)
                {
                    // Don't. Let idle connection do the job.
                    //UpdateButtonStatus();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("\nVolumeUpCommand returned false.");
                }
            }
        }

        public ICommand ShowSettingsCommand { get { return this._showSettingsCommand; } }

        public bool ShowSettingsCommand_CanExecute()
        {
            return true;
        }

        public void ShowSettingsCommand_Execute()
        {
            if (ShowSettings)
            {
                ShowSettings = false;
            }
            else {
                IsChanged = false;

                ShowSettings = true;
            }
        }

        public ICommand NewConnectinSettingCommand { get { return this._newConnectinSettingCommand; } }

        public bool NewConnectinSettingCommand_CanExecute()
        {
            //if (_ErrorMessages.Count > 0) { return false; }
            //if (!IsChanged) { return false; }
            //if (this._defaultHost == "") { return false; }
            //if (IsBusy) { return false; }
            return true;
        }

        public async void NewConnectinSettingCommand_Execute()
        {
            // New connection from Setting.

            // Validate Host input!
            if (this._defaultHost == "")
            {
                SetError("Host", "Error: Host must be epecified.");
                this.NotifyPropertyChanged("Host");
                return;
            }
            else
            {
                IPAddress ipAddress = null;
                try
                {
                    ipAddress = IPAddress.Parse(this._defaultHost);
                    if (ipAddress != null)
                    {
                        // Good.
                    }
                }
                catch
                {
                    //System.FormatException
                    SetError("Host", "Error: Invalid address format.");
                    this.NotifyPropertyChanged("Host");
                    return;
                }
            }

            if (_MPC != null) {
                await this._MPC.MpdIdleStop();
                await this._MPC.MpdIdleDisConnect();

                _MPC.CurrentQueue.Clear();
                _MPC.Playlists.Clear();
                _MPC.MpdCurrentSong = null;
                _MPC = null;
            }

            IsBusy = true;
            if (await StartConnection())
            {
                IsBusy = false;
                ShowSettings = false;

                if (this._profile == null)
                {
                    Profile profile = new Profile
                    {
                        Host = this._defaultHost,
                        Port = this._defaultPort,
                        Name = this._defaultHost + ":" + this._defaultPort.ToString(),
                        ID = Guid.NewGuid().ToString(),
                    };

                    MPDCtrl.Properties.Settings.Default.Profiles.Profiles.Add(profile);

                    this._profile = profile;
                }
                else
                {
                    this._profile.Host = this._defaultHost;
                    this._profile.Port = this._defaultPort;

                }

                // Make it default;
                MPDCtrl.Properties.Settings.Default.DefaultProfileID = this._profile.ID;

                this.NotifyPropertyChanged("SelectedProfile");
                this.NotifyPropertyChanged("Playlists");

                // Save settings.
                MPDCtrl.Properties.Settings.Default.Save();
            }
            else
            {
                IsBusy = false;
                //TODO: show error.
                System.Diagnostics.Debug.WriteLine("Failed@NewConnectinSettingCommand_Execute");
            }

            this.NotifyPropertyChanged("Songs");
        }

        public ICommand AddConnectinSettingCommand { get { return this._addConnectinSettingCommand; } }

        public bool AddConnectinSettingCommand_CanExecute()
        {
            return true;
        }

        public void AddConnectinSettingCommand_Execute()
        {
            // Add a new profile.
            MPDCtrl.Properties.Settings.Default.DefaultProfileID = "";
            SelectedProfile = null;

        }

        public ICommand DeleteConnectinSettingCommand { get { return this._deleteConnectinSettingCommand; } }

        public bool DeleteConnectinSettingCommand_CanExecute()
        {
            if (SelectedProfile == null) { return false; }
            return true;
        }

        public void DeleteConnectinSettingCommand_Execute()
        {
            // Delete the selected profile entry. 
            if (this._profile != null) {
                try
                {
                    MPDCtrl.Properties.Settings.Default.Profiles.Profiles.Remove(this._profile);
                }
                catch { }
            }
            SelectedProfile = null;
        }

        #endregion END of COMMANDS


        #region == INotifyPropertyChanged ==

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        #endregion

        #region == IDataErrorInfo ==

        private Dictionary<string, string> _ErrorMessages = new Dictionary<string, string>();

        string IDataErrorInfo.Error
        {
            get { return (_ErrorMessages.Count > 0) ? "Has Error" : null; }
        }

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (_ErrorMessages.ContainsKey(columnName))
                    return _ErrorMessages[columnName];
                else
                    return "";
            }
        }

        protected void SetError(string propertyName, string errorMessage)
        {
            _ErrorMessages[propertyName] = errorMessage;
        }

        protected void ClearErrror(string propertyName)
        {
            if (_ErrorMessages.ContainsKey(propertyName))
                //_ErrorMessages.Remove(propertyName);
                _ErrorMessages[propertyName] = "";
        }


        #endregion

    }

}
