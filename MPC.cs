﻿/// 
/// 
/// MPD Ctrl
/// https://github.com/torumyax/MPD-Ctrl
/// 
/// TODO:
///  More error handling.
///  Settings.
///
/// Known issue:
///  
///
/// 


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Globalization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Text;

namespace WpfMPD
{
    public class MPC
    {
        /// <summary>
        /// Song Class for ObservableCollection. 
        /// </summary>
        /// 
        public class Song
        {
            public string ID { get; set; }
            public string Title { get; set; }
        }

        /// <summary>
        /// Status Class. It holds current MPD "status" information.
        /// </summary>
        /// 
        public class Status
        {
            public enum MpdPlayState
            {
                Play, Pause, Stop
            };

            private MpdPlayState _ps;
            private int _volume;
            private bool _repeat;
            private bool _random;
            private string _songID;
            private double _songTime;
            private double _songElapsed;

            public MpdPlayState MpdState
            {
                get { return _ps; }
                set { _ps = value; }
            }

            public int MpdVolume
            {
                get { return _volume; }
                set
                {
                    //todo check value. "0-100 or -1 if the volume cannot be determined"
                    _volume = value;
                }
            }

            public bool MpdRepeat
            {
                get { return _repeat; }
                set
                {
                    _repeat = value;
                }
            }

            public bool MpdRandom
            {
                get { return _random; }
                set
                {
                    _random = value;
                }
            }

            public string MpdSongID
            {
                get { return _songID; }
                set
                {
                    _songID = value;
                }
            }

            public double MpdSongTime
            {
                get { return _songTime; }
                set
                {
                    _songTime = value;
                }
            }

            public double MpdSongElapsed
            {
                get { return _songElapsed; }
                set
                {
                    _songElapsed = value;
                }
            }

            public Status()
            {
                //constructor
            }
        }

        /// <summary>
        /// Main MPC (MPD Client) Class. 
        /// </summary>

        #region MPC PRIVATE FIELD declaration

        private Status _st;
        private Song _currentSong;
        private ObservableCollection<Song> _songs = new ObservableCollection<Song>();
        private ObservableCollection<String> _playLists = new ObservableCollection<String>();
        private EventDrivenTCPClient _idleClient;

        #endregion END of MPC PRIVATE FIELD declaration

        #region MPC PUBLIC PROPERTY and EVENT FIELD

        public Status MpdStatus
        {
            get { return _st; }
        }

        public Song MpdCurrentSong
        {
            get
            {
                return _currentSong;
            }
            set
            {
                _currentSong = value;
            }
        }

        public ObservableCollection<Song> CurrentQueue
        {
            get { return this._songs; }
        }

        public ObservableCollection<String> Playlists
        {
            get { return this._playLists; }
        }

        public delegate void MpdStatusChanged(MPC sender, object data);

        public event MpdStatusChanged StatusChanged;

        #endregion END of MPC PUBLIC PROPERTY FIELD

        // MPC Constructor
        public MPC()
        {
            _st = new Status();

            //Enable multithreaded manupilations of ObservableCollections...
            BindingOperations.EnableCollectionSynchronization(this._songs, new object());
            BindingOperations.EnableCollectionSynchronization(this._playLists, new object());

            //Initialize idle tcp client
            _idleClient = new EventDrivenTCPClient(IPAddress.Parse("192.168.3.123"), int.Parse("6600"));
            _idleClient.ReceiveTimeout = 500000;
            _idleClient.AutoReconnect = false;
            _idleClient.DataReceived += new EventDrivenTCPClient.delDataReceived(IdleClient_DataReceived);
            _idleClient.ConnectionStatusChanged += new EventDrivenTCPClient.delConnectionStatusChanged(IdleClient_ConnectionStatusChanged);

        }

        #region MPC METHODS

        public async Task<bool> MpdIdleConnect()
        {
            //Idle client connect
            try
            {
                if (_idleClient.ConnectionState != EventDrivenTCPClient.ConnectionStatus.Connected)
                {
                    await Task.Run(() => { _idleClient.Connect(); });
                }
                return true;
            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDIdleConnect(): " + ex.Message);
                return false;
            }
            
        }

        public async Task<bool> MpdIdleDisConnect()
        {
            //Idle client close connection
            try
            {
                if (_idleClient.ConnectionState == EventDrivenTCPClient.ConnectionStatus.Connected) {
                    await Task.Run(() => { _idleClient.Disconnect(); });
                }
                return true;
            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDIdleDisConnect(): " + ex.Message);
                return false;
            }
        }
        
        private void IdleClient_ConnectionStatusChanged(EventDrivenTCPClient sender, EventDrivenTCPClient.ConnectionStatus status)
        {
            //fired when the connection status changes in the TCP client

            //System.Diagnostics.Debug.WriteLine("IdleConnection: " + status.ToString());

            if (status == EventDrivenTCPClient.ConnectionStatus.Connected)
            {
                //
            }
        }

        private void IdleClient_DataReceived(EventDrivenTCPClient sender, object data)
        {
            //fired when new data is received in the TCP client

            //System.Diagnostics.Debug.WriteLine("IdleConnection DataReceived: " + (data as string) );

            if ((data as string).StartsWith("OK MPD"))
            {
                //Connected and received OK. So go idle.
                //sender.Send("idle player mixer options playlist stored_playlist\n");
            }
            else
            {
                
                //TODO do something.

                /*
                 changed: playlist
                 changed: player
                 changed: options
                 OK
                */

                //Fire up changed event.
                StatusChanged?.Invoke(this, data);

                //go idle and wait
                //sender.Send("idle player mixer options playlist stored_playlist\n");
            }
            
        }

        public async Task<bool> MpdIdleStart()
        {
            //Idle client send "idle" command
            try
            {
                if (_idleClient.ConnectionState == EventDrivenTCPClient.ConnectionStatus.Connected)
                {
                    await Task.Run(() => { _idleClient.Send("idle player mixer options playlist stored_playlist\n"); });
                    return true;
                }
                else { return false; }
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDIdleStart(): " + ex.Message);
                return false;
            }
        }

        public async Task<bool> MpdIdleStop()
        {
            //Idle client send "noidle" command
            try
            {
                if (_idleClient.ConnectionState == EventDrivenTCPClient.ConnectionStatus.Connected)
                {
                    await Task.Run(() => { _idleClient.Send("noidle"); });
                    return true;
                }
                else { return false; }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDIdleStop(): " + ex.Message);
                return false;
            }
        }

        private static async Task<List<string>> SendRequest(string server, int port, string mpdCommand)
        {
            IPAddress ipAddress = null;
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(server), port);
            ipAddress = ep.Address;

            List<string> responseMultiLines = new List<string>();

            try
            {
                TcpClient client = new TcpClient();
                await client.ConnectAsync(ep.Address, port);

                System.Diagnostics.Debug.WriteLine("\n\n" + "Server " + server + " connected.");

                NetworkStream networkStream = client.GetStream();
                StreamWriter writer = new StreamWriter(networkStream);
                StreamReader reader = new StreamReader(networkStream);
                writer.AutoFlush = true;

                //first check MPD's initial response on connect.
                string responseLine = await reader.ReadLineAsync();

                System.Diagnostics.Debug.WriteLine("Connected response: " + responseLine);

                //Check if it starts with "OK MPD"
                if (responseLine.StartsWith("OK MPD"))
                {

                    //if it's ok, then request command.
                    await writer.WriteLineAsync(mpdCommand);

                    System.Diagnostics.Debug.WriteLine("Request: " + mpdCommand);

                    //read multiple lines untill "OK".
                    while (!reader.EndOfStream)
                    {
                        responseLine = await reader.ReadLineAsync();

                        //System.Diagnostics.Debug.WriteLine("Response loop: " + responseLine);

                        if ((responseLine != "OK") && (responseLine != ""))
                        {
                            responseMultiLines.Add(responseLine);

                            if (responseLine.StartsWith("ACK"))
                            {
                                System.Diagnostics.Debug.WriteLine("Response ACK: " + responseLine + "\n");
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    responseMultiLines.Add(responseLine);

                    System.Diagnostics.Debug.WriteLine("MPD returned an error on connect: " + responseLine + "\n");
                }

                client.Close();
                System.Diagnostics.Debug.WriteLine("Connection closed.");

                return responseMultiLines;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@SendRequest: " + ex.Message);
                //TODO: Show error message to user.
                // 'System.Net.Sockets.SocketException'
                // 接続済みの呼び出し先が一定の時間を過ぎても正しく応答しなかったため、接続できませんでした。または接続済みのホストが応答しなかったため、確立された接続は失敗しました。
                return null;
            }
        }

        public async Task<bool> MpdQueryStatus()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string mpdCommand = "status";

                Task<List<string>> tsResponse = SendRequest(server, port, mpdCommand);
                await tsResponse;
                return ParseStatusResponse(tsResponse.Result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryStatus(): " + ex.Message);
            }
            return false;
        }

        private bool ParseStatusResponse(List<string> sl)
        {
            if (sl == null) { return false; }
            if (sl.Count == 0) { return false; }

            Dictionary<string, string> MpdStatusValues = new Dictionary<string, string>();
            foreach (string value in sl)
            {
                if (value.StartsWith("ACK")) { return false; }

                string[] StatusValuePair = value.Split(':');
                if (StatusValuePair.Length > 1)
                {
                    if (MpdStatusValues.ContainsKey(StatusValuePair[0].Trim()))
                    {
                        if (StatusValuePair.Length == 2) {
                            MpdStatusValues[StatusValuePair[0].Trim()] = StatusValuePair[1].Trim();
                        }
                        else if (StatusValuePair.Length == 3)
                        {
                            MpdStatusValues[StatusValuePair[0].Trim()] = StatusValuePair[1].Trim() +':'+ StatusValuePair[2].Trim();
                        } 

                    }
                    else
                    {
                        if (StatusValuePair.Length == 2)
                        {
                            MpdStatusValues.Add(StatusValuePair[0].Trim(), StatusValuePair[1].Trim());
                        }else if (StatusValuePair.Length == 3)
                        {
                            MpdStatusValues.Add(StatusValuePair[0].Trim(), (StatusValuePair[1].Trim() +":"+ StatusValuePair[2].Trim()));
                        }
                    }
                }
            }

            //state
            if (MpdStatusValues.ContainsKey("state"))
            {
                switch (MpdStatusValues["state"])
                {
                    case "play":
                        {
                            _st.MpdState = Status.MpdPlayState.Play;
                            break;
                        }
                    case "pause":
                        {
                            _st.MpdState = Status.MpdPlayState.Pause;
                            break;
                        }
                    case "stop":
                        {
                            _st.MpdState = Status.MpdPlayState.Stop;
                            break;
                        }
                }
            }

            //volume
            if (MpdStatusValues.ContainsKey("volume"))
            {
                try
                {
                    _st.MpdVolume = Int32.Parse(MpdStatusValues["volume"]);
                }
                catch (FormatException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            //songID
            _st.MpdSongID = "";
            if (MpdStatusValues.ContainsKey("songid"))
            {
                _st.MpdSongID = MpdStatusValues["songid"];
            }

            //repeat opt bool
            if (MpdStatusValues.ContainsKey("repeat"))
            {
                try
                {
                    //if (Int32.Parse(MpdStatusValues["repeat"]) > 0)
                    if (MpdStatusValues["repeat"] == "1")
                    {
                        _st.MpdRepeat = true;
                    }
                    else
                    {
                        _st.MpdRepeat = false;
                    }

                }
                catch (FormatException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            //random opt bool
            if (MpdStatusValues.ContainsKey("random"))
            {
                try
                {
                    if (Int32.Parse(MpdStatusValues["random"]) > 0)
                    {
                        _st.MpdRandom = true;
                    }
                    else
                    {
                        _st.MpdRandom = false;
                    }

                }
                catch (FormatException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            if (MpdStatusValues.ContainsKey("time"))
            {
                //System.Diagnostics.Debug.WriteLine(MpdStatusValues["time"]);
                try
                {
                    if (MpdStatusValues["time"].Split(':').Length > 1)
                    {
                        _st.MpdSongTime = Double.Parse(MpdStatusValues["time"].Split(':')[1].Trim());
                        _st.MpdSongElapsed = Double.Parse(MpdStatusValues["time"].Split(':')[0].Trim());
                    }
                }
                catch (FormatException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            if (MpdStatusValues.ContainsKey("elapsed"))
            {
                try
                {
                    _st.MpdSongElapsed = Double.Parse(MpdStatusValues["elapsed"]);
                }
                catch { }
            }

            if (MpdStatusValues.ContainsKey("duration"))
            {
                try
                {
                    _st.MpdSongTime = Double.Parse(MpdStatusValues["duration"]);
                }
                catch { }
            }


            //TODO: more?


            var listItem = _songs.Where(i => i.ID == _st.MpdSongID);
            if (listItem != null)
            {
                foreach (var item in listItem)
                {
                    _currentSong = item as Song;

                    //System.Diagnostics.Debug.WriteLine("StatusResponse linq: _songs.Where?="+ _currentSong.Title);
                }
            }
            
            return true;
        }

        public async Task<bool> MpdQueryCurrentPlaylist()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string mpdCommand = "playlistinfo";

                Task<List<string>> tsResponse = SendRequest(server, port, mpdCommand);
                await tsResponse;

                return ParsePlaylistInfoResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryCurrentPlaylist(): " + ex.Message);
            }
            return false;
        }

        private bool ParsePlaylistInfoResponse(List<string> sl)
        {
            _songs.Clear();

            if (sl == null) { return false; }
            if (sl.Count == 0) { return false; }

            Dictionary<string, string> SongValues = new Dictionary<string, string>();
            foreach (string value in sl)
            {
                //System.Diagnostics.Debug.WriteLine("@ParsePlaylistInfoResponse(): " + value);

                if (value.StartsWith("ACK")) { return false; }

                string[] StatusValuePair = value.Split(':');
                if (StatusValuePair.Length > 1)
                {
                    if (SongValues.ContainsKey(StatusValuePair[0].Trim()))
                    {
                        SongValues[StatusValuePair[0].Trim()] = StatusValuePair[1].Trim();
                    }
                    else
                    {
                        SongValues.Add(StatusValuePair[0].Trim(), StatusValuePair[1].Trim());
                    }

                }

                if (SongValues.ContainsKey("Id"))
                {

                    Song sng = new Song();
                    sng.ID = SongValues["Id"];

                    if (SongValues.ContainsKey("Title"))
                    {
                        sng.Title = SongValues["Title"];
                    }
                    else
                    {
                        sng.Title = "- no title";
                        if (SongValues.ContainsKey("file"))
                        {
                            sng.Title = Path.GetFileName(SongValues["file"]);
                        }
                    }

                    if (sng.ID == _st.MpdSongID)
                    {
                        _currentSong = sng;

                        //System.Diagnostics.Debug.WriteLine(sng.ID + ":" + sng.Title + " - is current.");
                    }

                    _songs.Add(sng);

                    SongValues.Clear();

                }


            }

            return true;
        }

        public async Task<bool> MpdQueryPlaylists()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string mpdCommand = "listplaylists";

                Task<List<string>> tsResponse = SendRequest(server, port, mpdCommand);
                await tsResponse;

                /*
                playlist: Blues
                Last-Modified: 2018-01-26T12:12:10Z
                playlist: Jazz
                Last-Modified: 2018-01-26T12:12:37Z
                OK
                 */

                return ParsePlaylistsResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryPlaylists(): " + ex.Message);
            }
            return false;
        }

        private bool ParsePlaylistsResponse(List<string> sl)
        {
            _playLists.Clear();

            if (sl == null) { return false; }
            if (sl.Count == 0) { return false; }

            foreach (string value in sl)
            {
                //System.Diagnostics.Debug.WriteLine("@ParsePlaylistsResponse(): " + value + "");

                if (value.StartsWith("ACK")) { return false; }

                if (value.StartsWith("playlist:")) {
                    if (value.Split(':').Length > 1) { 
                    _playLists.Add(value.Split(':')[1].Trim());
                    }
                }
                else if (value.StartsWith("Last-Modified: ") || (value.StartsWith("OK"))) 
                {
                    //ignore
                }
                else
                {
                    //ignore
                }
            }
            return true;
        }

        public async Task<bool> MpdPlaybackPlay(string songID = "")
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                if (songID != "")
                {
                    data = data + "playid " + songID + "\n";
                }
                else
                {
                    data = data + "play" + "\n";
                }

                data = data + "status" + "\n" + "command_list_end";

                //send task
                Task<List<string>> tsResponse = SendRequest(server, port, data);

                await tsResponse;

                //Alternatively just
                //string sResponse = await SendRequest(server, port, data);
                //"Received response: " + tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPlay: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdPlaybackSeek(string songID, int seekTime)
        {
            if ((songID == "") || (seekTime == 0)) { return false; }
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                data = data + "seekid " + songID + " " + seekTime.ToString() + "\n";

                data = data + "status" + "\n" + "command_list_end";

                //send task
                Task<List<string>> tsResponse = SendRequest(server, port, data);

                await tsResponse;
                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdPlaybackSeek: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdPlaybackPause()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                data = data + "pause 1" + "\n";

                data = data + "status" + "\n" + "command_list_end";

                Task<List<string>> tsResponse = SendRequest(server, port, data);

                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPause: " + ex.Message);
            }

            return false;
        }

        public async Task<bool> MpdPlaybackResume()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                data = data + "pause 0" + "\n";

                data = data + "status" + "\n" + "command_list_end";

                Task<List<string>> tsResponse = SendRequest(server, port, data);

                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackResume: " + ex.Message);
            }

            return false;
        }

        public async Task<bool> MpdPlaybackStop()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                data = data + "stop" + "\n";

                data = data + "status" + "\n" + "command_list_end";

                Task<List<string>> tsResponse = SendRequest(server, port, data);

                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackStop: " + ex.Message);
            }

            return false;
        }

        public async Task<bool> MpdPlaybackNext()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                if (_st.MpdState != Status.MpdPlayState.Play)
                {
                    data = data + "play" + "\n";
                }

                data = data + "next" + "\n";

                data = data + "status" + "\n" + "command_list_end";

                Task<List<string>> tsResponse = SendRequest(server, port, data);

                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackNext: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdPlaybackPrev()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                data = data + "previous" + "\n";

                if (_st.MpdState != Status.MpdPlayState.Play)
                {
                    data = data + "play" + "\n";
                }

                data = data + "status" + "\n" + "command_list_end";

                Task<List<string>> tsResponse = SendRequest(server, port, data);

                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPrev: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdSetVolume(int v)
        {
            if (v == _st.MpdVolume){return true;}

            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                //string data = "setvol " + v.ToString();
                string data = "command_list_begin" + "\n";
                
                data = data + "setvol " + v.ToString() + "\n";

                data = data + "status" + "\n" + "command_list_end";

                Task<List<string>> tsResponse = SendRequest(server, port, data);

                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetVol: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdSetRepeat(bool on)
        {
            if (_st.MpdRepeat == on){ return true;}

            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                if (on) { 
                    data = data + "repeat 1" + "\n";
                }
                else
                {
                    data = data + "repeat 0" + "\n";
                }
                data = data + "status" + "\n" + "command_list_end";

                Task<List<string>> tsResponse = SendRequest(server, port, data);

                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetRepeat: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdSetRandom(bool on)
        {
            if (_st.MpdRandom == on){return true;}

            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                if (on)
                {
                    data = data + "random 1" + "\n";
                }
                else
                {
                    data = data + "random 0" + "\n";
                }
                data = data + "status" + "\n" + "command_list_end";

                Task<List<string>> tsResponse = SendRequest(server, port, data);

                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetRandom: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdChangePlaylist(string playlistName)
        {
            if (playlistName.Trim() != "")
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                data = data + "clear" +  "\n";

                data = data + "load " + playlistName + "\n";

                data = data + "playlistinfo" + "\n" + "command_list_end";

                Task<List<string>> tsResponse = SendRequest(server, port, data);

                await tsResponse;

                return ParsePlaylistInfoResponse(tsResponse.Result);
            }
            else
            {
                return false;
            }
        }

        #endregion END of MPD METHODS

        /// END OF MPC Client Class 
    }




    /// <summary>
    /// Event driven TCP client wrapper
    /// https://www.daniweb.com/programming/software-development/code/422291/user-friendly-asynchronous-event-driven-tcp-client
    /// </summary>
    public class EventDrivenTCPClient : IDisposable
    {
        #region Consts/Default values
        const int DEFAULTTIMEOUT = 5000; //Default to 5 seconds on all timeouts
        const int RECONNECTINTERVAL = 2000; //Default to 2 seconds reconnect attempt rate
        #endregion

        #region Components, Events, Delegates, and CTOR
        //Timer used to detect receive timeouts
        private System.Timers.Timer tmrReceiveTimeout = new System.Timers.Timer();
        private System.Timers.Timer tmrSendTimeout = new System.Timers.Timer();
        private System.Timers.Timer tmrConnectTimeout = new System.Timers.Timer();
        public delegate void delDataReceived(EventDrivenTCPClient sender, object data);
        public event delDataReceived DataReceived;
        public delegate void delConnectionStatusChanged(EventDrivenTCPClient sender, ConnectionStatus status);
        public event delConnectionStatusChanged ConnectionStatusChanged;
        public enum ConnectionStatus
        {
            NeverConnected,
            Connecting,
            Connected,
            AutoReconnecting,
            DisconnectedByUser,
            DisconnectedByHost,
            ConnectFail_Timeout,
            ReceiveFail_Timeout,
            SendFail_Timeout,
            SendFail_NotConnected,
            Error
        }
        public EventDrivenTCPClient(IPAddress ip, int port, bool autoreconnect = true)
        {
            this._IP = ip;
            this._Port = port;
            this._AutoReconnect = autoreconnect;
            this._client = new TcpClient(AddressFamily.InterNetwork);
            this._client.NoDelay = true; //Disable the nagel algorithm for simplicity
            ReceiveTimeout = DEFAULTTIMEOUT;
            SendTimeout = DEFAULTTIMEOUT;
            ConnectTimeout = DEFAULTTIMEOUT;
            ReconnectInterval = RECONNECTINTERVAL;
            tmrReceiveTimeout.AutoReset = false;
            tmrReceiveTimeout.Elapsed += new System.Timers.ElapsedEventHandler(tmrReceiveTimeout_Elapsed);
            tmrConnectTimeout.AutoReset = false;
            tmrConnectTimeout.Elapsed += new System.Timers.ElapsedEventHandler(tmrConnectTimeout_Elapsed);
            tmrSendTimeout.AutoReset = false;
            tmrSendTimeout.Elapsed += new System.Timers.ElapsedEventHandler(tmrSendTimeout_Elapsed);

            ConnectionState = ConnectionStatus.NeverConnected;
        }
        #endregion

        #region Private methods/Event Handlers
        void tmrSendTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.ConnectionState = ConnectionStatus.SendFail_Timeout;
            DisconnectByHost();
        }
        void tmrReceiveTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.ConnectionState = ConnectionStatus.ReceiveFail_Timeout;
            DisconnectByHost();
        }
        void tmrConnectTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ConnectionState = ConnectionStatus.ConnectFail_Timeout;
            DisconnectByHost();
        }
        private void DisconnectByHost()
        {
            this.ConnectionState = ConnectionStatus.DisconnectedByHost;
            tmrReceiveTimeout.Stop();
            if (AutoReconnect)
                Reconnect();
        }
        private void Reconnect()
        {
            if (this.ConnectionState == ConnectionStatus.Connected)
                return;
            this.ConnectionState = ConnectionStatus.AutoReconnecting;
            try
            {
                this._client.Client.BeginDisconnect(true, new AsyncCallback(cbDisconnectByHostComplete), this._client.Client);
            }
            catch { }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Try connecting to the remote host
        /// </summary>
        public void Connect()
        {
            if (this.ConnectionState == ConnectionStatus.Connected)
                return;
            this.ConnectionState = ConnectionStatus.Connecting;
            tmrConnectTimeout.Start();
            this._client.BeginConnect(this._IP, this._Port, new AsyncCallback(cbConnect), this._client.Client);
        }
        /// <summary>
        /// Try disconnecting from the remote host
        /// </summary>
        public void Disconnect()
        {
            if (this.ConnectionState != ConnectionStatus.Connected)
                return;
            this._client.Client.BeginDisconnect(true, new AsyncCallback(cbDisconnectComplete), this._client.Client);
        }
        /// <summary>
        /// Try sending a string to the remote host
        /// </summary>
        /// <param name="data">The data to send</param>
        public void Send(string data)
        {
            if (this.ConnectionState != ConnectionStatus.Connected)
            {
                this.ConnectionState = ConnectionStatus.SendFail_NotConnected;
                return;
            }
            var bytes = _encode.GetBytes(data);
            SocketError err = new SocketError();
            tmrSendTimeout.Start();
            this._client.Client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, out err, new AsyncCallback(cbSendComplete), this._client.Client);
            if (err != SocketError.Success)
            {
                Action doDCHost = new Action(DisconnectByHost);
                doDCHost.Invoke();
            }
        }
        /// <summary>
        /// Try sending byte data to the remote host
        /// </summary>
        /// <param name="data">The data to send</param>
        public void Send(byte[] data)
        {
            if (this.ConnectionState != ConnectionStatus.Connected)
                throw new InvalidOperationException("Cannot send data, socket is not connected");
            SocketError err = new SocketError();
            this._client.Client.BeginSend(data, 0, data.Length, SocketFlags.None, out err, new AsyncCallback(cbSendComplete), this._client.Client);
            if (err != SocketError.Success)
            {
                Action doDCHost = new Action(DisconnectByHost);
                doDCHost.Invoke();
            }
        }
        public void Dispose()
        {
            this._client.Close();
            this._client.Client.Dispose();
        }
        #endregion

        #region Callbacks
        private void cbConnectComplete()
        {
            if (_client.Connected == true)
            {
                tmrConnectTimeout.Stop();
                ConnectionState = ConnectionStatus.Connected;
                this._client.Client.BeginReceive(this.dataBuffer, 0, this.dataBuffer.Length, SocketFlags.None, new AsyncCallback(cbDataReceived), this._client.Client);
            }
            else
            {
                ConnectionState = ConnectionStatus.Error;
            }
        }
        private void cbDisconnectByHostComplete(IAsyncResult result)
        {
            var r = result.AsyncState as Socket;
            if (r == null)
                throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as a socket object");
            r.EndDisconnect(result);
            if (this.AutoReconnect)
            {
                Action doConnect = new Action(Connect);
                doConnect.Invoke();
                return;
            }
        }
        private void cbDisconnectComplete(IAsyncResult result)
        {
            var r = result.AsyncState as Socket;
            if (r == null)
                throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as a socket object");
            r.EndDisconnect(result);
            this.ConnectionState = ConnectionStatus.DisconnectedByUser;

        }
        private void cbConnect(IAsyncResult result)
        {
            var sock = result.AsyncState as Socket;
            if (result == null)
                throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as a socket object");
            if (!sock.Connected)
            {
                if (AutoReconnect)
                {
                    System.Threading.Thread.Sleep(ReconnectInterval);
                    Action reconnect = new Action(Connect);
                    reconnect.Invoke();
                    return;
                }
                else
                    return;
            }
            sock.EndConnect(result);
            var callBack = new Action(cbConnectComplete);
            callBack.Invoke();
        }
        private void cbSendComplete(IAsyncResult result)
        {
            var r = result.AsyncState as Socket;
            if (r == null)
                throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as a socket object");
            SocketError err = new SocketError();
            r.EndSend(result, out err);
            if (err != SocketError.Success)
            {
                Action doDCHost = new Action(DisconnectByHost);
                doDCHost.Invoke();
            }
            else
            {
                lock (SyncLock)
                {
                    tmrSendTimeout.Stop();
                }
            }
        }
        private void cbChangeConnectionStateComplete(IAsyncResult result)
        {
            var r = result.AsyncState as EventDrivenTCPClient;
            if (r == null)
                throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as a EDTC object");
            r.ConnectionStatusChanged.EndInvoke(result);
        }
        private void cbDataReceived(IAsyncResult result)
        {
            var sock = result.AsyncState as Socket;
            if (sock == null)
                throw new InvalidOperationException("Invalid IASyncResult - Could not interpret as a socket");
            SocketError err = new SocketError();
            int bytes = sock.EndReceive(result, out err);  
            if (bytes == 0 || err != SocketError.Success)
            {
                lock (SyncLock)
                {
                    tmrReceiveTimeout.Start();
                    return;
                }
            }
            else
            {
                lock (SyncLock)
                {
                    tmrReceiveTimeout.Stop();
                }
            }
            if (DataReceived != null)
            {
                DataReceived.BeginInvoke(this, _encode.GetString(dataBuffer, 0, bytes), new AsyncCallback(cbDataRecievedCallbackComplete), this);
            }
        }
        private void cbDataRecievedCallbackComplete(IAsyncResult result)
        {
            var r = result.AsyncState as EventDrivenTCPClient;
            if (r == null)
                throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as EDTC object");
            r.DataReceived.EndInvoke(result);
            SocketError err = new SocketError();
            this._client.Client.BeginReceive(this.dataBuffer, 0, this.dataBuffer.Length, SocketFlags.None, out err, new AsyncCallback(cbDataReceived), this._client.Client);
            if (err != SocketError.Success)
            {
                Action doDCHost = new Action(DisconnectByHost);
                doDCHost.Invoke();
            }
        }
        #endregion

        #region Properties and members
        private IPAddress _IP = IPAddress.None;
        private ConnectionStatus _ConStat;
        private TcpClient _client;
        private byte[] dataBuffer = new byte[5000];
        private bool _AutoReconnect = false;
        private int _Port = 0;
        private Encoding _encode = Encoding.Default;
        object _SyncLock = new object();
        /// <summary>
        /// Syncronizing object for asyncronous operations
        /// </summary>
        public object SyncLock
        {
            get
            {
                return _SyncLock;
            }
        }
        /// <summary>
        /// Encoding to use for sending and receiving
        /// </summary>
        public Encoding DataEncoding
        {
            get
            {
                return _encode;
            }
            set
            {
                _encode = value;
            }
        }
        /// <summary>
        /// Current state that the connection is in
        /// </summary>
        public ConnectionStatus ConnectionState
        {
            get
            {
                return _ConStat;
            }
            private set
            {
                bool raiseEvent = value != _ConStat;
                _ConStat = value;
                if (ConnectionStatusChanged != null && raiseEvent)
                    ConnectionStatusChanged.BeginInvoke(this, _ConStat, new AsyncCallback(cbChangeConnectionStateComplete), this);
            }
        }
        /// <summary>
        /// True to autoreconnect at the given reconnection interval after a remote host closes the connection
        /// </summary>
        public bool AutoReconnect
        {
            get
            {
                return _AutoReconnect;
            }
            set
            {
                _AutoReconnect = value;
            }
        }
        public int ReconnectInterval { get; set; }
        /// <summary>
        /// IP of the remote host
        /// </summary>
        public IPAddress IP
        {
            get
            {
                return _IP;
            }
        }
        /// <summary>
        /// Port to connect to on the remote host
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
        }
        /// <summary>
        /// Time to wait after a receive operation is attempted before a timeout event occurs
        /// </summary>
        public int ReceiveTimeout
        {
            get
            {
                return (int)tmrReceiveTimeout.Interval;
            }
            set
            {
                tmrReceiveTimeout.Interval = (double)value;
            }
        }
        /// <summary>
        /// Time to wait after a send operation is attempted before a timeout event occurs
        /// </summary>
        public int SendTimeout
        {
            get
            {
                return (int)tmrSendTimeout.Interval;
            }
            set
            {
                tmrSendTimeout.Interval = (double)value;
            }
        }
        /// <summary>
        /// Time to wait after a connection is attempted before a timeout event occurs
        /// </summary>
        public int ConnectTimeout
        {
            get
            {
                return (int)tmrConnectTimeout.Interval;
            }
            set
            {
                tmrConnectTimeout.Interval = (double)value;
            }
        }
        #endregion       
    }

}
