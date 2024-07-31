/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019-2023
/// 
/// Based on SharpQuake (Quake Rewritten in C# by Yury Kiselev, 2010.)
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using SharpQuake.Desktop;
using SharpQuake.Framework;
using SharpQuake.Framework.Factories.IO;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.Logging;
using SharpQuake.Game.Client;
using SharpQuake.Networking.Client;
using SharpQuake.Networking.Server;
using SharpQuake.Sys.Programs;
using System;
using System.Diagnostics;

namespace SharpQuake.Sys
{
    public class EngineThink
    {
        private Stopwatch Stopwatch
        {
            get;
            set;
        }

        private Int32 _TimeCount; // static int timecount from Host_Frame
        private Double _TimeTotal; // static double timetotal from Host_Frame
        private Double _Time1 = 0; // static double time1 from _Host_Frame
        private Double _Time2 = 0; // static double time2 from _Host_Frame
        private Double _Time3 = 0; // static double time3 from _Host_Frame

        private readonly IEngine _engine;
        private readonly IConsoleLogger _logger;
        private readonly IMouseInput _mouse;
        private readonly IKeyboardInput _keyboard;
        private readonly MainWindow _window;
        private readonly CommandFactory _commands;
        private readonly ClientState _clientState;
        private readonly ServerState _serverState;
        private readonly client _client;
        private readonly Server _server;
        private readonly Network _network;
        private readonly ProgramsState _programsState;
        private readonly render _renderer;
        private readonly Scr _screen;
        private readonly View _view;
        private readonly snd _sound;
        private readonly cd_audio _cdAudio;

        public EngineThink( IEngine engine, IConsoleLogger logger, IMouseInput mouse, IKeyboardInput keyboard, 
            MainWindow window, CommandFactory commands, ClientState clientState, 
            ServerState serverState, client client, Server server, Network network,
            ProgramsState programsState, render renderer,  Scr screen, View view, 
            snd sound, cd_audio cdAudio )
        {
            _engine = engine;
            _logger = logger;
            _mouse = mouse;
            _keyboard = keyboard;
            _window = window;
            _commands = commands;
            _clientState = clientState;
            _serverState = serverState;
            _client = client;
            _server = server;
            _network = network;
            _programsState = programsState;
            _renderer = renderer;
            _screen = screen;
            _view = view;
            _sound = sound;
            _cdAudio = cdAudio;
            Stopwatch = new Stopwatch( );
        }

        /// <summary>
        /// Host_FilterTime
        /// Returns false if the time is too short to run a frame
        /// </summary>
        private Boolean FilterTime( Double time )
        {
            return Time.Filter( time, _clientState.StaticData.timedemo ); // Now stored in a specific time class
        }

        // _Host_Frame
        //
        //Runs all active servers
        private void InternalFrame( Double time )
        {
            // keep the random time dependent
            MathLib.Random( );

            // decide the simulation time
            if ( !FilterTime( time ) )
                return;         // don't run too fast, or packets will flood out

            // get new key events
            SendKeyEvents( );

            // allow mice or other external controllers to add commands
            _mouse.Commands( );

            // process console commands
            _commands.Buffer.Execute( );

            _network.Poll( );

            // if running the server locally, make intentions now
            if ( _serverState.Data.active )
                _client.SendCmd( );

            //-------------------
            //
            // server operations
            //
            //-------------------

            // check for commands typed to the host
            GetConsoleCommands( );

            if ( _serverState.Data.active )
                ServerFrame( );

            //-------------------
            //
            // client operations
            //
            //-------------------

            // if running the server remotely, send intentions now after
            // the incoming messages have been read
            if ( !_serverState.Data.active )
                _client.SendCmd( );

            Time.Increment( );

            // fetch results from server
            if ( _clientState.StaticData.state == cactive_t.ca_connected )
                _client.ReadFromServer( );

            // update video
            if ( Cvars.HostSpeeds.Get<Boolean>( ) )
                _Time1 = Timer.GetFloatTime( );

            _screen.UpdateScreen( );

            if ( Cvars.HostSpeeds.Get<Boolean>( ) )
                _Time2 = Timer.GetFloatTime( );

            // update audio
            if ( _clientState.StaticData.signon == ClientDef.SIGNONS )
            {
                _sound.Update( ref _renderer.Origin, ref _renderer.ViewPn, ref _renderer.ViewRight, ref _renderer.ViewUp );
                _clientState.DecayLights( );
            }
            else
                _sound.Update( ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref Utilities.ZeroVector );

            _cdAudio.Update( );

            if ( Cvars.HostSpeeds.Get<Boolean>( ) )
            {
                var pass1 = ( Int32 ) ( ( _Time1 - _Time3 ) * 1000 );
                _Time3 = Timer.GetFloatTime( );
                var pass2 = ( Int32 ) ( ( _Time2 - _Time1 ) * 1000 );
                var pass3 = ( Int32 ) ( ( _Time3 - _Time2 ) * 1000 );
                _logger.Print( "{0,3} tot {1,3} server {2,3} gfx {3,3} snd\n", pass1 + pass2 + pass3, pass1, pass2, pass3 );
            }

            FileSystem.Tick( );

            _view.FrameCount++;
        }

        // Host_Frame
        public void Frame( Double time )
        {
            if ( !Cvars.ServerProfile.Get<Boolean>( ) )
            {
                InternalFrame( time );
                return;
            }

            var time1 = Timer.GetFloatTime( );
            InternalFrame( time );
            var time2 = Timer.GetFloatTime( );

            _TimeTotal += time2 - time1;
            _TimeCount++;

            if ( _TimeCount < 1000 )
                return;

            var m = ( Int32 ) ( _TimeTotal * 1000 / _TimeCount );
            _TimeCount = 0;
            _TimeTotal = 0;
            var c = 0;
            foreach ( var cl in _serverState.StaticData.clients )
            {
                if ( cl.active )
                    c++;
            }

            _logger.Print( "serverprofile: {0,2:d} clients {1,2:d} msec\n", c, m );
        }

        /// <summary>
        /// The main render loop
        /// </summary>
        /// <param name="time"></param>
        public void FrameMain( Double time )
        {
            if ( _window.IsMinimised || _screen.BlockDrawing || _engine.IsDisposing )
                _screen.SkipUpdate = true;  // no point in bothering to draw

            Stopwatch.Stop( );
            var ts = Stopwatch.Elapsed.TotalSeconds;
            Stopwatch.Restart( );

            Frame( ts );
        }


        /// <summary>
        /// Host_ServerFrame
        /// </summary>
        public void ServerFrame( )
        {
            // run the world state
            _programsState.GlobalStruct.frametime = ( Single ) Sys.Time.Delta;

            // Execute server frame
            _server.Frame( );
        }

        /// <summary>
        /// Sys_SendKeyEventsa
        /// </summary>
        public void SendKeyEvents( )
        {
            _screen.SkipUpdate = false;
            _window.ProcessEvents( );
        }


        // Host_GetConsoleCommands
        //
        // Add them exactly as if they had been typed at the console
        [Obsolete( "This never really did anything, REFACTOR" )]
        private void GetConsoleCommands( )
        {
            //while ( true )
            //{
            //    var cmd = DedicatedServer.ConsoleInput( );

            //    if ( String.IsNullOrEmpty( cmd ) )
            //        break;

            //    _commands.Buffer.Append( cmd );
            //}
        }
    }
}
