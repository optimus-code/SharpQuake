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

using SharpQuake.Framework;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Framework.World;
using SharpQuake.Game.Client;
using SharpQuake.Game.Rendering;
using SharpQuake.Sys;
using System;

namespace SharpQuake
{
	partial class client
	{
		// CL_Init
		public void Initialise( )
		{
			InitialiseMessageDelegates();
			InitInput( );

			if ( Cvars.Name == null )
			{
				Cvars.Name = _cvars.Add( "_cl_name", "player", ClientVariableFlags.Archive );
				Cvars.Color = _cvars.Add( "_cl_color", 0f, ClientVariableFlags.Archive );
				Cvars.ShowNet = _cvars.Add( "cl_shownet", 0 ); // can be 0, 1, or 2
				Cvars.NoLerp = _cvars.Add( "cl_nolerp", false );
				Cvars.LookSpring = _cvars.Add( "lookspring", false, ClientVariableFlags.Archive );
				Cvars.LookStrafe = _cvars.Add( "lookstrafe", false, ClientVariableFlags.Archive );
				Cvars.Sensitivity = _cvars.Add( "sensitivity", 3f, ClientVariableFlags.Archive );
				Cvars.MPitch = _cvars.Add( "m_pitch", 0.022f, ClientVariableFlags.Archive );
				Cvars.MYaw = _cvars.Add( "m_yaw", 0.022f, ClientVariableFlags.Archive );
				Cvars.MForward = _cvars.Add( "m_forward", 1f, ClientVariableFlags.Archive );
				Cvars.MSide = _cvars.Add( "m_side", 0.8f, ClientVariableFlags.Archive );
				Cvars.UpSpeed = _cvars.Add( "cl_upspeed", 200f );
				Cvars.ForwardSpeed = _cvars.Add( "cl_forwardspeed", 200f, ClientVariableFlags.Archive );
				Cvars.BackSpeed = _cvars.Add( "cl_backspeed", 200f, ClientVariableFlags.Archive );
				Cvars.SideSpeed = _cvars.Add( "cl_sidespeed", 350f );
				Cvars.MoveSpeedKey = _cvars.Add( "cl_movespeedkey", 2.0f );
				Cvars.YawSpeed = _cvars.Add( "cl_yawspeed", 140f );
				Cvars.PitchSpeed = _cvars.Add( "cl_pitchspeed", 150f );
				Cvars.AngleSpeedKey = _cvars.Add( "cl_anglespeedkey", 1.5f );
				Cvars.AnimationBlend = _cvars.Add( "cl_animationblend", false );
			}

			//
			// register our commands
			//
			_commands.Add( "cmd", ForwardToServer_f );
			_commands.Add( "entities", PrintEntities_f );
			_commands.Add( "disconnect", Disconnect_f );
			_commands.Add( "record", Record_f );
			_commands.Add( "stop", Stop_f );
			_commands.Add( "playdemo", PlayDemo_f );
			_commands.Add( "timedemo", TimeDemo_f );

            PrecacheSFX( );
        }

		// void	Cmd_ForwardToServer (void);
		// adds the current command line as a clc_stringcmd to the client message.
		// things like godmode, noclip, etc, are commands directed to the server,
		// so when they are typed in at the console, they will need to be forwarded.
		//
		// Sends the entire command line over to the server
		public void ForwardToServer_f( CommandMessage msg )
		{
			if ( _state.StaticData.state != cactive_t.ca_connected )
			{
				_logger.Print( $"Can't \"{msg.Name}\", not connected\n" );
				return;
			}

			if ( _state.StaticData.demoplayback )
				return;     // not really connected

			var writer = _state.StaticData.message;
			writer.WriteByte( ProtocolDef.clc_stringcmd );
			if ( !msg.Name.Equals( "cmd" ) )
			{
				writer.Print( msg.Name + " " );
			}
			if ( msg.HasParameters )
			{
				writer.Print( msg.StringParameters );
			}
			else
			{
				writer.Print( "\n" );
			}
		}

		/// <summary>
		/// CL_EstablishConnection
		/// </summary>
		public void EstablishConnection( String host )
		{
			if ( _state.StaticData.state == cactive_t.ca_dedicated )
				return;

			if ( _state.StaticData.demoplayback )
				return;

			Disconnect();

			_state.StaticData.netcon = _network.Connect( host );
			if ( _state.StaticData.netcon == null )
				_engine.Error( "CL_Connect: connect failed\n" );

			_logger.DPrint( "CL_EstablishConnection: connected to {0}\n", host );

			_state.StaticData.demonum = -1;           // not in the demo loop now
			_state.StaticData.state = cactive_t.ca_connected;
			_state.StaticData.signon = 0;             // need all the signon messages before playing
		}

		/// <summary>
		/// CL_NextDemo
		///
		/// Called to play the next demo in the demo loop
		/// </summary>
		public void NextDemo( )
		{
			if ( _state.StaticData.demonum == -1 )
				return;     // don't play demos

			_screen.BeginLoadingPlaque();

			if ( String.IsNullOrEmpty( _state.StaticData.demos[_state.StaticData.demonum] ) || _state.StaticData.demonum == ClientDef.MAX_DEMOS )
			{
				_state.StaticData.demonum = 0;
				if ( String.IsNullOrEmpty( _state.StaticData.demos[_state.StaticData.demonum] ) )
				{
					_logger.Print( "No demos listed with startdemos\n" );
					_state.StaticData.demonum = -1;
					return;
				}
			}

			_commands.Buffer.Insert( String.Format( "playdemo {0}\n", _state.StaticData.demos[_state.StaticData.demonum] ) );
			_state.StaticData.demonum++;
		}				

		// CL_Disconnect_f
		public void Disconnect_f( CommandMessage msg )
		{
			Disconnect();
			if ( _serverState.IsActive )
				_engine.ShutdownServer( false );
		}

		// CL_SendCmd
		public void SendCmd( )
		{
			if ( _state.StaticData.state != cactive_t.ca_connected )
				return;

			if ( _state.StaticData.signon == ClientDef.SIGNONS )
			{
				var cmd = new usercmd_t();

				// get basic movement from keyboard
				BaseMove( ref cmd );

                // allow mice or other external controllers to add to the move
                _mouse.Move( cmd );

				// send the unreliable message
				SendMove( ref cmd );
			}

			if ( _state.StaticData.demoplayback )
			{
				_state.StaticData.message.Clear();//    SZ_Clear (_state.StaticData.message);
				return;
			}

			// send the reliable message
			if ( _state.StaticData.message.IsEmpty )
				return;     // no message at all

			if ( !_network.CanSendMessage( _state.StaticData.netcon ) )
			{
				_logger.DPrint( "CL_WriteToServer: can't send\n" );
				return;
			}

			if ( _network.SendMessage( _state.StaticData.netcon, _state.StaticData.message ) == -1 )
				_engine.Error( "CL_WriteToServer: lost server connection" );

			_state.StaticData.message.Clear();
		}

		// CL_ReadFromServer
		//
		// Read all incoming data from the server
		public Int32 ReadFromServer( )
		{
			_state.Data.oldtime = _state.Data.time;
			_state.Data.time += Time.Delta;

			Int32 ret;
			do
			{
				ret = GetMessage();
				if ( ret == -1 )
					_engine.Error( "CL_ReadFromServer: lost server connection" );
				if ( ret == 0 )
					break;

				_state.Data.last_received_message = ( Single ) Time.Absolute;
				ParseServerMessage();
			} while ( ret != 0 && _state.StaticData.state == cactive_t.ca_connected );

			if ( Cvars.ShowNet.Get<Int32>() != 0 )
				_logger.Print( "\n" );

			//
			// bring the links up to date
			//
			RelinkEntities();
			UpdateTempEntities();

			return 0;
		}

		/// <summary>
		/// CL_Disconnect
		///
		/// Sends a disconnect message to the server
		/// This is also called on Host_Error, so it shouldn't cause any errors
		/// </summary>
		public void Disconnect( )
		{
			// stop sounds (especially looping!)
			_sound.StopAllSounds( true );

			// bring the console down and fade the colors back to normal
			//	SCR_BringDownConsole ();

			// if running a local server, shut it down
			if ( _state.StaticData.demoplayback )
				StopPlayback();
			else if ( _state.StaticData.state == cactive_t.ca_connected )
			{
				if ( _state.StaticData.demorecording )
					Stop_f( null );

				_logger.DPrint( "Sending clc_disconnect\n" );
				_state.StaticData.message.Clear();
				_state.StaticData.message.WriteByte( ProtocolDef.clc_disconnect );
				_network.SendUnreliableMessage( _state.StaticData.netcon, _state.StaticData.message );
				_state.StaticData.message.Clear();
				_network.Close( _state.StaticData.netcon );

				_state.StaticData.state = cactive_t.ca_disconnected;

				if ( _serverState.Data.active )
					_engine.ShutdownServer( false );
			}

			_state.StaticData.demoplayback = _state.StaticData.timedemo = false;
			_keyboard.IsWatchingDemo = _state.StaticData.demoplayback;
			_state.StaticData.signon = 0;
		}

		// CL_PrintEntities_f
		private void PrintEntities_f( CommandMessage msg )
		{
			for ( var i = 0; i < _state.Data.num_entities; i++ )
			{
				var ent = _state.Entities[i];
				_logger.Print( "{0:d3}:", i );
				if ( ent.model == null )
				{
					_logger.Print( "EMPTY\n" );
					continue;
				}
				_logger.Print( "{0}:{1:d2}  ({2}) [{3}]\n", ent.model.Name, ent.frame, ent.origin, ent.angles );
			}
		}

		/// <summary>
		/// CL_RelinkEntities
		/// </summary>
		private void RelinkEntities( )
		{
			// determine partial update time
			var frac = LerpPoint();

            _state.NumVisEdicts = 0;

			//
			// interpolate player info
			//
			_state.Data.velocity = _state.Data.mvelocity[1] + frac * ( _state.Data.mvelocity[0] - _state.Data.mvelocity[1] );

			if ( _state.StaticData.demoplayback )
			{
				// interpolate the angles
				var angleDelta = _state.Data.mviewangles[0] - _state.Data.mviewangles[1];
				MathLib.CorrectAngles180( ref angleDelta );
				_state.Data.viewangles = _state.Data.mviewangles[1] + frac * angleDelta;
			}

			var bobjrotate = MathLib.AngleMod( 100 * _state.Data.time );

			// start on the entity after the world
			for ( var i = 1; i < _state.Data.num_entities; i++ )
			{
				var ent = _state.Entities[i];
				if ( ent.model == null )
				{
					// empty slot
					if ( ent.forcelink )
						_renderer.World.Entities.RemoveEfrags( ent ); // just became empty
					continue;
				}

				// if the object wasn't included in the last packet, remove it
				if ( ent.msgtime != _state.Data.mtime[0] )
				{
					ent.model = null;
					continue;
				}

				var oldorg = ent.origin;

				if ( ent.forcelink )
				{
					// the entity was not updated in the last message
					// so move to the final spot
					ent.origin = ent.msg_origins[0];
					ent.angles = ent.msg_angles[0];
				}
				else
				{
					// if the delta is large, assume a teleport and don't lerp
					var f = frac;
					var delta = ent.msg_origins[0] - ent.msg_origins[1];
					if ( Math.Abs( delta.X ) > 100 || Math.Abs( delta.Y ) > 100 || Math.Abs( delta.Z ) > 100 )
						f = 1; // assume a teleportation, not a motion

					// interpolate the origin and angles
					ent.origin = ent.msg_origins[1] + f * delta;
					var angleDelta = ent.msg_angles[0] - ent.msg_angles[1];
					MathLib.CorrectAngles180( ref angleDelta );
					ent.angles = ent.msg_angles[1] + f * angleDelta;
				}

				// rotate binary objects locally
				if ( ent.model.Flags.HasFlag( EntityFlags.Rotate ) )
					ent.angles.Y = bobjrotate;

				if ( ( ent.effects & EntityEffects.EF_BRIGHTFIELD ) != 0 )
					_renderer.World.Particles.EntityParticles( _state.Data.time, ent.origin );

				if ( ( ent.effects & EntityEffects.EF_MUZZLEFLASH ) != 0 )
				{
					var dl = _state.AllocDlight( i );
					dl.origin = ent.origin;
					dl.origin.Z += 16;
					Vector3 fv, rv, uv;
					MathLib.AngleVectors( ref ent.angles, out fv, out rv, out uv );
					dl.origin += fv * 18;
					dl.radius = 200 + ( MathLib.Random() & 31 );
					dl.minlight = 32;
					dl.die = ( Single ) _state.Data.time + 0.1f;
				}
				if ( ( ent.effects & EntityEffects.EF_BRIGHTLIGHT ) != 0 )
				{
					var dl = _state.AllocDlight( i );
					dl.origin = ent.origin;
					dl.origin.Z += 16;
					dl.radius = 400 + ( MathLib.Random() & 31 );
					dl.die = ( Single ) _state.Data.time + 0.001f;
				}
				if ( ( ent.effects & EntityEffects.EF_DIMLIGHT ) != 0 )
				{
					var dl = _state.AllocDlight( i );
					dl.origin = ent.origin;
					dl.radius = 200 + ( MathLib.Random() & 31 );
					dl.die = ( Single ) _state.Data.time + 0.001f;
				}

				if ( ent.model.Flags.HasFlag( EntityFlags.Gib ) )
					_renderer.World.Particles.RocketTrail( _state.Data.time, ref oldorg, ref ent.origin, 2 );
				else if ( ent.model.Flags.HasFlag( EntityFlags.ZomGib ) )
					_renderer.World.Particles.RocketTrail( _state.Data.time, ref oldorg, ref ent.origin, 4 );
				else if ( ent.model.Flags.HasFlag( EntityFlags.Tracer ) )
					_renderer.World.Particles.RocketTrail( _state.Data.time, ref oldorg, ref ent.origin, 3 );
				else if ( ent.model.Flags.HasFlag( EntityFlags.Tracer2 ) )
					_renderer.World.Particles.RocketTrail( _state.Data.time, ref oldorg, ref ent.origin, 5 );
				else if ( ent.model.Flags.HasFlag( EntityFlags.Rocket ) )
				{
					_renderer.World.Particles.RocketTrail( _state.Data.time, ref oldorg, ref ent.origin, 0 );
					var dl = _state.AllocDlight( i );
					dl.origin = ent.origin;
					dl.radius = 200;
					dl.die = ( Single ) _state.Data.time + 0.01f;
				}
				else if ( ent.model.Flags.HasFlag( EntityFlags.Grenade ) )
					_renderer.World.Particles.RocketTrail( _state.Data.time, ref oldorg, ref ent.origin, 1 );
				else if ( ent.model.Flags.HasFlag( EntityFlags.Tracer3 ) )
					_renderer.World.Particles.RocketTrail( _state.Data.time, ref oldorg, ref ent.origin, 6 );

				ent.forcelink = false;

				if ( i == _state.Data.viewentity && !_chaseView.IsActive )
					continue;

				if ( _state.NumVisEdicts < ClientDef.MAX_VISEDICTS )
				{
					_state.VisEdicts[_state.NumVisEdicts] = ent;
                    _state.NumVisEdicts++;
				}
			}
		}

		/// <summary>
		/// CL_SignonReply
		///
		/// An svc_signonnum has been received, perform a client side setup
		/// </summary>
		private void SignonReply( )
		{
			_logger.DPrint( "CL_SignonReply: ^0{0}\n", _state.StaticData.signon );

			switch ( _state.StaticData.signon )
			{
				case 1:
					_state.StaticData.message.WriteByte( ProtocolDef.clc_stringcmd );
					_state.StaticData.message.WriteString( "prespawn" );
					break;

				case 2:
					_state.StaticData.message.WriteByte( ProtocolDef.clc_stringcmd );
					_state.StaticData.message.WriteString( String.Format( "name \"{0}\"\n", Cvars.Name.Get<String>() ) );

					_state.StaticData.message.WriteByte( ProtocolDef.clc_stringcmd );
					_state.StaticData.message.WriteString( String.Format( "color {0} {1}\n", ( ( Int32 ) Cvars.Color.Get<Single>() ) >> 4, ( ( Int32 ) Cvars.Color.Get<Single>() ) & 15 ) );

					_state.StaticData.message.WriteByte( ProtocolDef.clc_stringcmd );
					_state.StaticData.message.WriteString( "spawn " + _state.StaticData.spawnparms );
					break;

				case 3:
					_state.StaticData.message.WriteByte( ProtocolDef.clc_stringcmd );
					_state.StaticData.message.WriteString( "begin" );
					_cache.Report();    // print remaining memory
					break;

				case 4:
					_screen.EndLoadingPlaque();     // allow normal screen updates
					break;
			}
		}

		/// <summary>  
		/// CL_ClearState
		/// </summary>
		private void ClearState( )
		{
			if ( !_serverState.Data.active )
                _memoryHandler.ClearMemory();

			_state.Clear( );
		}

		/// <summary>
		/// CL_LerpPoint
		/// Determines the fraction between the last two messages that the objects
		/// should be put at.
		/// </summary>
		private Single LerpPoint( )
		{
			var f = _state.Data.mtime[0] - _state.Data.mtime[1];

			if ( f == 0 || Cvars.NoLerp.Get<Boolean>() ||
				_state.StaticData.timedemo || _serverState.IsActive )
			{
				_state.Data.time = _state.Data.mtime[0];
				return 1;
			}

			if ( f > 0.1 )
			{   // dropped packet, or start of demo
				_state.Data.mtime[1] = _state.Data.mtime[0] - 0.1;
				f = 0.1;
			}

			var frac = ( _state.Data.time - _state.Data.mtime[1] ) / f;

			if ( frac < 0 )
			{
				if ( frac < -0.01 )
				{
					_state.Data.time = _state.Data.mtime[1];
				}
				frac = 0;
			}
			else if ( frac > 1 )
			{
				if ( frac > 1.01 )
				{
					_state.Data.time = _state.Data.mtime[0];
				}
				frac = 1;
			}

			return ( Single ) frac;
		}
	}
}
