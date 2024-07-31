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

using System;
using System.IO;
using System.Text;
using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.IO.FileHandlers;
using SharpQuake.Game.Client;
using SharpQuake.Sys;

namespace SharpQuake
{
    partial class client
    {
        private IArchiveEntryReader DemoFile
        {
            get;
            set;
        }

        /// <summary>
        /// CL_StopPlayback
        ///
        /// Called when a demo file runs out, or the user starts a game
        /// </summary>
        public void StopPlayback( )
        {
            if ( !_state.StaticData.demoplayback )
                return;

            if ( _state.StaticData.demofile != null )
            {
                _state.StaticData.demofile.Dispose( );
                _state.StaticData.demofile = null;
            }
            _keyboard.IsWatchingDemo = false;
            _state.StaticData.demoplayback = false;
            _state.StaticData.state = cactive_t.ca_disconnected;

            if ( _state.StaticData.timedemo )
                FinishTimeDemo( );

            DemoFile?.Dispose( );
        }

        /// <summary>
        /// CL_Record_f
        /// record <demoname> <map> [cd track]
        /// </summary>
        private void Record_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            var c = msg.Parameters != null ? msg.Parameters.Length : 0;

            if ( c != 1 && c != 2 && c != 3 )
            {
                _logger.Print( "record <demoname> [<map> [cd track]]\n" );
                return;
            }

            if ( msg.Parameters[0].Contains( ".." ) )
            {
                _logger.Print( "Relative pathnames are not allowed.\n" );
                return;
            }

            if ( c == 2 && _state.StaticData.state == cactive_t.ca_connected )
            {
                _logger.Print( "Can not record - already connected to server\nClient demo recording must be started before connecting\n" );
                return;
            }

            // write the forced cd track number, or -1
            Int32 track;
            if ( c == 3 )
            {
                track = MathLib.atoi( msg.Parameters[2] );
                _logger.Print( "Forcing CD track to {0}\n", track );
            }
            else
                track = -1;

            var name = Path.Combine( FileSystem.GameDir, msg.Parameters[0] );

            //
            // start the map up
            //
            if ( c > 1 )
                _commands.ExecuteString( String.Format( "map {0}", msg.Parameters[1] ), CommandSource.Command );

            //
            // open the demo file
            //
            name = Path.ChangeExtension( name, ".dem" );

            _logger.Print( "recording to {0}.\n", name );
            var fs = FileSystem.OpenWrite( name, true );
            if ( fs == null )
            {
                _logger.Print( "ERROR: couldn't open.\n" );
                return;
            }
            var writer = new BinaryWriter( fs, Encoding.ASCII );
            _state.StaticData.demofile = new DisposableWrapper<BinaryWriter>( writer, true );
            _state.StaticData.forcetrack = track;
            var tmp = Encoding.ASCII.GetBytes( _state.StaticData.forcetrack.ToString( ) );
            writer.Write( tmp );
            writer.Write( '\n' );
            _state.StaticData.demorecording = true;
        }

        /// <summary>
        /// CL_Stop_f
        /// stop recording a demo
        /// </summary>
        private void Stop_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            if ( !_state.StaticData.demorecording )
            {
                _logger.Print( "Not recording a demo.\n" );
                return;
            }

            // write a disconnect message to the demo file
            _network.Message.Clear( );
            _network.Message.WriteByte( ProtocolDef.svc_disconnect );
            WriteDemoMessage( );

            // finish up
            if ( _state.StaticData.demofile != null )
            {
                _state.StaticData.demofile.Dispose( );
                _state.StaticData.demofile = null;
            }
            _state.StaticData.demorecording = false;
            _logger.Print( "Completed demo\n" );
        }

        // CL_PlayDemo_f
        //
        // play [demoname]
        private void PlayDemo_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            var c = msg.Parameters != null ? msg.Parameters.Length : 0;

            if ( c != 1 )
            {
                _logger.Print( "play <demoname> : plays a demo\n" );
                return;
            }

            //
            // disconnect from server
            //
            Disconnect( );

            //
            // open the demo file
            //
            var name = Path.ChangeExtension( msg.Parameters[0], ".dem" );

            _logger.Print( "Playing demo from {0}.\n", name );
            if ( _state.StaticData.demofile != null )
            {
                _state.StaticData.demofile.Dispose( );
            }
            DisposableWrapper<BinaryReader> reader;
            DemoFile = FileSystem.FOpenFile( name, out reader );
            _state.StaticData.demofile = reader;
            if ( _state.StaticData.demofile == null )
            {
                _logger.Print( "ERROR: couldn't open.\n" );
                _state.StaticData.demonum = -1;		// stop demo loop
                return;
            }

            _keyboard.IsWatchingDemo = true;
            _state.StaticData.demoplayback = true;
            _state.StaticData.state = cactive_t.ca_connected;
            _state.StaticData.forcetrack = 0;

            var s = reader.Object;
            c = 0;
            var neg = false;
            while ( true )
            {
                c = s.ReadByte( );
                if ( c == '\n' )
                    break;

                if ( c == '-' )
                    neg = true;
                else
                    _state.StaticData.forcetrack = _state.StaticData.forcetrack * 10 + ( c - '0' );
            }

            if ( neg )
                _state.StaticData.forcetrack = -_state.StaticData.forcetrack;
            // ZOID, fscanf is evil
            //	fscanf (_state.StaticData.demofile, "%i\n", &_state.StaticData.forcetrack);
        }

        /// <summary>
        /// CL_TimeDemo_f
        /// timedemo [demoname]
        /// </summary>
        private void TimeDemo_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            var c = msg.Parameters != null ? msg.Parameters.Length : 0;

            if ( c != 1 )
            {
                _logger.Print( "timedemo <demoname> : gets demo speeds\n" );
                return;
            }

            PlayDemo_f( msg );

            // _state.StaticData.td_starttime will be grabbed at the second frame of the demo, so
            // all the loading time doesn't get counted
            _state.StaticData.timedemo = true;
            _state.StaticData.td_startframe = _view.FrameCount;
            _state.StaticData.td_lastframe = -1;		// get a new message this frame
        }

        /// <summary>
        /// CL_GetMessage
        /// Handles recording and playback of demos, on top of NET_ code
        /// </summary>
        /// <returns></returns>
        private Int32 GetMessage( )
        {
            if ( _state.StaticData.demoplayback )
            {
                // decide if it is time to grab the next message
                if ( _state.StaticData.signon == ClientDef.SIGNONS )	// allways grab until fully connected
                {
                    if ( _state.StaticData.timedemo )
                    {
                        if ( _view.FrameCount == _state.StaticData.td_lastframe )
                            return 0;		// allready read this frame's message
                        _state.StaticData.td_lastframe = _view.FrameCount;
                        // if this is the second frame, grab the real td_starttime
                        // so the bogus time on the first frame doesn't count
                        if ( _view.FrameCount == _state.StaticData.td_startframe + 1 )
                            _state.StaticData.td_starttime = ( Single ) Time.Absolute;
                    }
                    else if ( _state.Data.time <= _state.Data.mtime[0] )
                    {
                        return 0;	// don't need another message yet
                    }
                }

                // get the next message
                var reader = ( ( DisposableWrapper<BinaryReader> ) _state.StaticData.demofile ).Object;
                var size = EndianHelper.LittleLong( reader.ReadInt32( ) );
                if ( size > QDef.MAX_MSGLEN )
                    Utilities.Error( "Demo message > MAX_MSGLEN" );

                _state.Data.mviewangles[1] = _state.Data.mviewangles[0];
                _state.Data.mviewangles[0].X = EndianHelper.LittleFloat( reader.ReadSingle( ) );
                _state.Data.mviewangles[0].Y = EndianHelper.LittleFloat( reader.ReadSingle( ) );
                _state.Data.mviewangles[0].Z = EndianHelper.LittleFloat( reader.ReadSingle( ) );

                _network.Message.FillFrom( reader.BaseStream, size );
                if ( _network.Message.Length < size )
                {
                    StopPlayback( );
                    return 0;
                }
                return 1;
            }

            Int32 r;
            while ( true )
            {
                r = _network.GetMessage( _state.StaticData.netcon );

                if ( r != 1 && r != 2 )
                    return r;

                // discard nop keepalive message
                if ( _network.Message.Length == 1 && _network.Message.Data[0] == ProtocolDef.svc_nop )
                    _logger.Print( "<-- server to client keepalive\n" );
                else
                    break;
            }

            if ( _state.StaticData.demorecording )
                WriteDemoMessage( );

            return r;
        }

        /// <summary>
        /// CL_FinishTimeDemo
        /// </summary>
        private void FinishTimeDemo( )
        {
            _state.StaticData.timedemo = false;

            // the first frame didn't count
            var frames = ( _view.FrameCount - _state.StaticData.td_startframe ) - 1;
            var time = ( Single ) Time.Absolute - _state.StaticData.td_starttime;
            if ( time == 0 )
                time = 1;
            _logger.Print( "{0} frames {1:F5} seconds {2:F2} fps\n", frames, time, frames / time );
        }

        /// <summary>
        /// CL_WriteDemoMessage
        /// Dumps the current net message, prefixed by the length and view angles
        /// </summary>
        private void WriteDemoMessage( )
        {
            var len = EndianHelper.LittleLong( _network.Message.Length );
            var writer = ( ( DisposableWrapper<BinaryWriter> ) _state.StaticData.demofile ).Object;
            writer.Write( len );
            writer.Write( EndianHelper.LittleFloat( _state.Data.viewangles.X ) );
            writer.Write( EndianHelper.LittleFloat( _state.Data.viewangles.Y ) );
            writer.Write( EndianHelper.LittleFloat( _state.Data.viewangles.Z ) );
            writer.Write( _network.Message.Data, 0, _network.Message.Length );
            writer.Flush( );
        }
    }
}
