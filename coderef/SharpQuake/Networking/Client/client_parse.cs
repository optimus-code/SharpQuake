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

using SharpQuake.Factories.Rendering.UI;
using SharpQuake.Framework;
using SharpQuake.Framework.Definitions;
using SharpQuake.Framework.IO;
using SharpQuake.Game.Client;
using SharpQuake.Game.Data.Models;
using SharpQuake.Game.World;
using SharpQuake.Sys;
using System;
using System.Collections.Generic;

// cl_parse.c

namespace SharpQuake
{
	partial class client
	{
		private const String ConsoleBar = "\n\n\u001D\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001F\n\n";

		private static String[] _SvcStrings = new String[]
		{
			"svc_bad",
			"svc_nop",
			"svc_disconnect",
			"svc_updatestat",
			"svc_version",		// [long] server version
	        "svc_setview",		// [short] entity number
	        "svc_sound",			// <see code>
	        "svc_time",			// [float] server time
	        "svc_print",			// [string] null terminated string
	        "svc_stufftext",		// [string] stuffed into client's console buffer
						        // the string should be \n terminated
	        "svc_setangle",		// [vec3] set the view angle to this absolute value

	        "svc_serverinfo",		// [long] version
						        // [string] signon string
						        // [string]..[0]model cache [string]...[0]sounds cache
						        // [string]..[0]item cache
	        "svc_lightstyle",		// [byte] [string]
	        "svc_updatename",		// [byte] [string]
	        "svc_updatefrags",	// [byte] [short]
	        "svc_clientdata",		// <shortbits + data>
	        "svc_stopsound",		// <see code>
	        "svc_updatecolors",	// [byte] [byte]
	        "svc_particle",		// [vec3] <variable>
	        "svc_damage",			// [byte] impact [byte] blood [vec3] from

	        "svc_spawnstatic",
			"OBSOLETE svc_spawnbinary",
			"svc_spawnbaseline",

			"svc_temp_entity",		// <variable>
	        "svc_setpause",
			"svc_signonnum",
			"svc_centerprint",
			"svc_killedmonster",
			"svc_foundsecret",
			"svc_spawnstaticsound",
			"svc_intermission",
			"svc_finale",			// [string] music [string] text
	        "svc_cdtrack",			// [byte] track [byte] looptrack
	        "svc_sellscreen",
			"svc_cutscene"
		};

		private Int32[] _BitCounts = new Int32[16]; // bitcounts
		private Object _MsgState; // used by KeepaliveMessage function
		private Single _LastMsg; // static float lastmsg from CL_KeepaliveMessage
		delegate void ProcessMessageDelegate( );

		private Dictionary<Int32, ProcessMessageDelegate> MessageDelegates;

		private void MessageCommandNoOperation( )
		{
		}

		private void MessageCommandServerTime( )
		{
			_state.Data.mtime[1] = _state.Data.mtime[0];
			_state.Data.mtime[0] = _network.Reader.ReadFloat();
		}

		private void MessageCommandClientData( )
		{
			var i = _network.Reader.ReadShort();
			ParseClientData( i );
		}

		private void MessageCommandVersion( )
		{
			var i = _network.Reader.ReadLong();

			if ( i != ProtocolDef.PROTOCOL_VERSION )
				_engine.Error( "CL_ParseServerMessage: Server is protocol {0} instead of {1}\n", i, ProtocolDef.PROTOCOL_VERSION );
		}

		private void MessageCommandDisconnect( )
		{
			_engine.EndGame( "Server disconnected\n" );
		}

		private void MessageCommandPrint( )
		{
			_logger.Print( _network.Reader.ReadString() );
		}

		private void MessageCommandCentrePrint( )
		{
			_screen.Elements.Enqueue( ElementFactory.CENTRE_PRINT, _network.Reader.ReadString() );
		}

		private void MessageCommandStuffText( )
		{
			_commands.Buffer.Append( _network.Reader.ReadString() );
		}

		private void MessageCommandDamage( )
		{
			_view.ParseDamage();
		}

		private void MessageCommandServerInfo( )
		{
			ParseServerInfo();
			_videoState.Data.recalc_refdef = true;   // leave intermission full screen
		}

		private void MessageCommandSetAngle( )
		{
			_state.Data.viewangles.X = _network.Reader.ReadAngle();
			_state.Data.viewangles.Y = _network.Reader.ReadAngle();
			_state.Data.viewangles.Z = _network.Reader.ReadAngle();
		}

		private void MessageCommandSetView( )
		{
			_state.Data.viewentity = _network.Reader.ReadShort();
		}

		private void MessageCommandLightStyle( )
		{
			var i = _network.Reader.ReadByte();

			if ( i >= QDef.MAX_LIGHTSTYLES )
				Utilities.Error( "svc_lightstyle > MAX_LIGHTSTYLES" );

			_state.LightStyle[i].map = _network.Reader.ReadString();
		}

		private void MessageCommandSound( )
		{
			ParseStartSoundPacket();
		}

		private void MessageCommandStopSound( )
		{
			var i = _network.Reader.ReadShort();
			_sound.StopSound( i >> 3, i & 7 );
		}

		private void MessageCommandUpdateName( )
		{
			_screen.Elements.SetDirty( ElementFactory.HUD );

			var i = _network.Reader.ReadByte();

			if ( i >= _state.Data.maxclients )
				_engine.Error( "CL_ParseServerMessage: svc_updatename > MAX_SCOREBOARD" );

			_state.Data.scores[i].name = _network.Reader.ReadString();
		}

		private void MessageCommandUpdateFrags( )
		{
			_screen.Elements.SetDirty( ElementFactory.HUD );

			var i = _network.Reader.ReadByte();

			if ( i >= _state.Data.maxclients )
				_engine.Error( "CL_ParseServerMessage: svc_updatefrags > MAX_SCOREBOARD" );

			_state.Data.scores[i].frags = _network.Reader.ReadShort();
		}

		private void MessageCommandUpdateColours( )
		{
			_screen.Elements.SetDirty( ElementFactory.HUD );

			var i = _network.Reader.ReadByte();

			if ( i >= _state.Data.maxclients )
				_engine.Error( "CL_ParseServerMessage: svc_updatecolors > MAX_SCOREBOARD" );

			_state.Data.scores[i].colors = _network.Reader.ReadByte();
			NewTranslation( i );
		}

		private void MessageCommandParticle( )
		{
			_renderer.World.Particles.ParseParticleEffect( _state.Data.time, _network.Reader );
		}

		private void MessageCommandSpawnBaseline( )
		{
			var i = _network.Reader.ReadShort();
			// must use CL_EntityNum() to force _state.Data.num_entities up
			ParseBaseline( EntityNum( i ) );
		}

		private void MessageCommandSpawnStatic( )
		{
			ParseStatic();
		}

		private void MessageCommandTempEntity( )
		{
			ParseTempEntity();
		}

		private void MessageCommandSetPause( )
		{
			_state.Data.paused = _network.Reader.ReadByte() != 0;

			if ( _state.Data.paused )
			{
				_sound.CDAudio.Pause();
			}
			else
			{
				_sound.CDAudio.Resume();
			}
		}

		private void MessageCommandSignOnNum( )
		{
			var i = _network.Reader.ReadByte();

			if ( i <= _state.StaticData.signon )
				_engine.Error( "Received signon {0} when at {1}", i, _state.StaticData.signon );

			_state.StaticData.signon = i;
			SignonReply();
		}

		private void MessageCommandKilledMonster( )
		{
			_state.Data.stats[QStatsDef.STAT_MONSTERS]++;
		}

		private void MessageCommandFoundSecret( )
		{
			_state.Data.stats[QStatsDef.STAT_SECRETS]++;
		}

		private void MessageCommandUpdateStat( )
		{
			var i = _network.Reader.ReadByte();

			if ( i < 0 || i >= QStatsDef.MAX_CL_STATS )
				Utilities.Error( "svc_updatestat: {0} is invalid", i );

			_state.Data.stats[i] = _network.Reader.ReadLong();
		}

		private void MessageCommandSpawnStaticSound( )
		{
			ParseStaticSound();
		}

		private void MessageCommandCDTrack( )
		{
			_state.Data.cdtrack = _network.Reader.ReadByte();
			_state.Data.looptrack = _network.Reader.ReadByte();

			if ( ( _state.StaticData.demoplayback || _state.StaticData.demorecording ) && ( _state.StaticData.forcetrack != -1 ) )
				_sound.CDAudio.Play( ( Byte ) _state.StaticData.forcetrack, true );
			else
				_sound.CDAudio.Play( ( Byte ) _state.Data.cdtrack, true );
		}

		private void MessageCommandIntermission( )
		{
			_state.Data.intermission = 1;
			_state.Data.completed_time = ( Int32 ) _state.Data.time;
			_videoState.Data.recalc_refdef = true;   // go to full screen
		}

		private void MessageCommandFinale( )
		{
			_state.Data.intermission = 2;
			_state.Data.completed_time = ( Int32 ) _state.Data.time;
			_videoState.Data.recalc_refdef = true;   // go to full screen
			_screen.Elements.Enqueue( ElementFactory.CENTRE_PRINT, _network.Reader.ReadString() );
		}

		private void MessageCommandCutScene( )
		{
			_state.Data.intermission = 3;
			_state.Data.completed_time = ( Int32 ) _state.Data.time;
			_videoState.Data.recalc_refdef = true;   // go to full screen
			_screen.Elements.Enqueue( ElementFactory.CENTRE_PRINT, _network.Reader.ReadString() );
		}

		private void MessageCommandSellScreen( )
		{
			_commands.ExecuteString( "help", CommandSource.Command );
		}

		protected void InitialiseMessageDelegates( )
		{
			MessageDelegates = new Dictionary<Int32, ProcessMessageDelegate>
			{
				{ ProtocolDef.svc_nop, MessageCommandNoOperation },
				{ ProtocolDef.svc_time, MessageCommandServerTime },
				{ ProtocolDef.svc_clientdata, MessageCommandClientData },
				{ ProtocolDef.svc_version, MessageCommandVersion },
				{ ProtocolDef.svc_disconnect, MessageCommandDisconnect },
				{ ProtocolDef.svc_print, MessageCommandPrint },
				{ ProtocolDef.svc_centerprint, MessageCommandCentrePrint },
				{ ProtocolDef.svc_stufftext, MessageCommandStuffText },
				{ ProtocolDef.svc_damage, MessageCommandDamage },
				{ ProtocolDef.svc_serverinfo, MessageCommandServerInfo },
				{ ProtocolDef.svc_setangle, MessageCommandSetAngle },
				{ ProtocolDef.svc_setview, MessageCommandSetView },
				{ ProtocolDef.svc_lightstyle, MessageCommandLightStyle },
				{ ProtocolDef.svc_sound, MessageCommandSound },
				{ ProtocolDef.svc_stopsound, MessageCommandStopSound },
				{ ProtocolDef.svc_updatename, MessageCommandUpdateName },
				{ ProtocolDef.svc_updatefrags, MessageCommandUpdateFrags },
				{ ProtocolDef.svc_updatecolors, MessageCommandUpdateColours },
				{ ProtocolDef.svc_particle, MessageCommandParticle },
				{ ProtocolDef.svc_spawnbaseline, MessageCommandSpawnBaseline },
				{ ProtocolDef.svc_spawnstatic, MessageCommandSpawnStatic },
				{ ProtocolDef.svc_temp_entity, MessageCommandTempEntity },
				{ ProtocolDef.svc_setpause, MessageCommandSetPause },
				{ ProtocolDef.svc_signonnum, MessageCommandSignOnNum },
				{ ProtocolDef.svc_killedmonster, MessageCommandKilledMonster },
				{ ProtocolDef.svc_foundsecret, MessageCommandFoundSecret },
				{ ProtocolDef.svc_updatestat, MessageCommandUpdateStat },
				{ ProtocolDef.svc_spawnstaticsound, MessageCommandSpawnStaticSound },
				{ ProtocolDef.svc_cdtrack, MessageCommandCDTrack },
				{ ProtocolDef.svc_intermission, MessageCommandIntermission },
				{ ProtocolDef.svc_finale, MessageCommandFinale },
				{ ProtocolDef.svc_cutscene, MessageCommandCutScene },
				{ ProtocolDef.svc_sellscreen, MessageCommandSellScreen }
			};
		}

		private void ProcessMessageCommand( Int32 cmd )
		{
			if ( MessageDelegates.ContainsKey( cmd ) )
				MessageDelegates[cmd]();
			else
				_engine.Error( "CL_ParseServerMessage: Illegible server message\n" );
		}

		/// <summary>
		/// CL_ParseServerMessage
		/// </summary>
		private void ParseServerMessage( )
		{
			// If recording demos, copy the message out
			if ( Cvars.ShowNet.Get<Int32>() == 1 )
				_logger.Print( "{0} ", _network.Message.Length );
			else if ( Cvars.ShowNet.Get<Int32>() == 2 )
				_logger.Print( "------------------\n" );

			_state.Data.onground = false;    // unless the server says otherwise

			// Parse the message
			_network.Reader.Reset();
			while ( true )
			{
				if ( _network.Reader.IsBadRead )
					_engine.Error( "CL_ParseServerMessage: Bad server message" );

				var cmd = _network.Reader.ReadByte();
				if ( cmd == -1 )
				{
					ShowNet( "END OF MESSAGE" );
					return; // end of message
				}

				// if the high bit of the command byte is set, it is a fast update
				if ( ( cmd & 128 ) != 0 )
				{
					ShowNet( "fast update" );
					ParseUpdate( cmd & 127 );
					continue;
				}

				ShowNet( _SvcStrings[cmd] );

				// other commands
				ProcessMessageCommand( cmd );
			}
		}

		private void ShowNet( String s )
		{
			if ( Cvars.ShowNet.Get<Int32>() == 2 )
				_logger.Print( "{0,3}:{1}\n", _network.Reader.Position - 1, s );
		}

		/// <summary>
		/// CL_ParseUpdate
		///
		/// Parse an entity update message from the server
		/// If an entities model or origin changes from frame to frame, it must be
		/// relinked.  Other attributes can change without relinking.
		/// </summary>
		private void ParseUpdate( Int32 bits )
		{
			Int32 i;

			if ( _state.StaticData.signon == ClientDef.SIGNONS - 1 )
			{
				// first update is the final signon stage
				_state.StaticData.signon = ClientDef.SIGNONS;
				SignonReply();
			}

			if ( ( bits & ProtocolDef.U_MOREBITS ) != 0 )
			{
				i = _network.Reader.ReadByte();
				bits |= ( i << 8 );
			}

			Int32 num;

			if ( ( bits & ProtocolDef.U_LONGENTITY ) != 0 )
				num = _network.Reader.ReadShort();
			else
				num = _network.Reader.ReadByte();

			var ent = EntityNum( num );
			for ( i = 0; i < 16; i++ )
				if ( ( bits & ( 1 << i ) ) != 0 )
					_BitCounts[i]++;

			var forcelink = false;
			if ( ent.msgtime != _state.Data.mtime[1] )
				forcelink = true;   // no previous frame to lerp from

			ent.msgtime = _state.Data.mtime[0];
			Int32 modnum;
			if ( ( bits & ProtocolDef.U_MODEL ) != 0 )
			{
				modnum = _network.Reader.ReadByte();
				if ( modnum >= QDef.MAX_MODELS )
					_engine.Error( "CL_ParseModel: bad modnum" );
			}
			else
				modnum = ent.baseline.modelindex;

			var model = _state.Data.model_precache[modnum];
			if ( model != ent.model )
			{
				ent.model = model;
				// automatic animation (torches, etc) can be either all together
				// or randomized
				if ( model != null )
				{
					if ( model.SyncType == SyncType.ST_RAND )
						ent.syncbase = ( Single ) ( MathLib.Random() & 0x7fff ) / 0x7fff;
					else
						ent.syncbase = 0;
				}
				else
					forcelink = true;   // hack to make null model players work

				if ( num > 0 && num <= _state.Data.maxclients )
					_renderer.TranslatePlayerSkin( num - 1 );
			}

			if ( ( bits & ProtocolDef.U_FRAME ) != 0 )
				ent.frame = _network.Reader.ReadByte();
			else
				ent.frame = ent.baseline.frame;

			if ( ( bits & ProtocolDef.U_COLORMAP ) != 0 )
				i = _network.Reader.ReadByte();
			else
				i = ent.baseline.colormap;
			if ( i == 0 )
				ent.colormap = _videoState.Data.colormap;
			else
			{
				if ( i > _state.Data.maxclients )
					Utilities.Error( "i >= _state.Data.maxclients" );
				ent.colormap = _state.Data.scores[i - 1].translations;
			}

			Int32 skin;
			if ( ( bits & ProtocolDef.U_SKIN ) != 0 )
				skin = _network.Reader.ReadByte();
			else
				skin = ent.baseline.skin;
			if ( skin != ent.skinnum )
			{
				ent.skinnum = skin;
				if ( num > 0 && num <= _state.Data.maxclients )
					_renderer.TranslatePlayerSkin( num - 1 );
			}

			if ( ( bits & ProtocolDef.U_EFFECTS ) != 0 )
				ent.effects = _network.Reader.ReadByte();
			else
				ent.effects = ent.baseline.effects;

			// shift the known values for interpolation
			ent.msg_origins[1] = ent.msg_origins[0];
			ent.msg_angles[1] = ent.msg_angles[0];

			if ( ( bits & ProtocolDef.U_ORIGIN1 ) != 0 )
				ent.msg_origins[0].X = _network.Reader.ReadCoord();
			else
				ent.msg_origins[0].X = ent.baseline.origin.x;
			if ( ( bits & ProtocolDef.U_ANGLE1 ) != 0 )
				ent.msg_angles[0].X = _network.Reader.ReadAngle();
			else
				ent.msg_angles[0].X = ent.baseline.angles.x;

			if ( ( bits & ProtocolDef.U_ORIGIN2 ) != 0 )
				ent.msg_origins[0].Y = _network.Reader.ReadCoord();
			else
				ent.msg_origins[0].Y = ent.baseline.origin.y;
			if ( ( bits & ProtocolDef.U_ANGLE2 ) != 0 )
				ent.msg_angles[0].Y = _network.Reader.ReadAngle();
			else
				ent.msg_angles[0].Y = ent.baseline.angles.y;

			if ( ( bits & ProtocolDef.U_ORIGIN3 ) != 0 )
				ent.msg_origins[0].Z = _network.Reader.ReadCoord();
			else
				ent.msg_origins[0].Z = ent.baseline.origin.z;
			if ( ( bits & ProtocolDef.U_ANGLE3 ) != 0 )
				ent.msg_angles[0].Z = _network.Reader.ReadAngle();
			else
				ent.msg_angles[0].Z = ent.baseline.angles.z;

			if ( ( bits & ProtocolDef.U_NOLERP ) != 0 )
				ent.forcelink = true;

			if ( forcelink )
			{   // didn't have an update last message
				ent.msg_origins[1] = ent.msg_origins[0];
				ent.origin = ent.msg_origins[0];
				ent.msg_angles[1] = ent.msg_angles[0];
				ent.angles = ent.msg_angles[0];
				ent.forcelink = true;
			}
		}

		/// <summary>
		/// CL_ParseClientdata
		/// Server information pertaining to this client only
		/// </summary>
		private void ParseClientData( Int32 bits )
		{
			if ( ( bits & ProtocolDef.SU_VIEWHEIGHT ) != 0 )
				_state.Data.viewheight = _network.Reader.ReadChar();
			else
				_state.Data.viewheight = ProtocolDef.DEFAULT_VIEWHEIGHT;

			if ( ( bits & ProtocolDef.SU_IDEALPITCH ) != 0 )
				_state.Data.idealpitch = _network.Reader.ReadChar();
			else
				_state.Data.idealpitch = 0;

			_state.Data.mvelocity[1] = _state.Data.mvelocity[0];
			for ( var i = 0; i < 3; i++ )
			{
				if ( ( bits & ( ProtocolDef.SU_PUNCH1 << i ) ) != 0 )
					MathLib.SetComp( ref _state.Data.punchangle, i, _network.Reader.ReadChar() );
				else
					MathLib.SetComp( ref _state.Data.punchangle, i, 0 );
				if ( ( bits & ( ProtocolDef.SU_VELOCITY1 << i ) ) != 0 )
					MathLib.SetComp( ref _state.Data.mvelocity[0], i, _network.Reader.ReadChar() * 16 );
				else
					MathLib.SetComp( ref _state.Data.mvelocity[0], i, 0 );
			}

			// [always sent]	if (bits & SU_ITEMS)
			var i2 = _network.Reader.ReadLong();

			if ( _state.Data.items != i2 )
			{   // set flash times
				_screen.Elements.SetDirty( ElementFactory.HUD );
				for ( var j = 0; j < 32; j++ )
					if ( ( i2 & ( 1 << j ) ) != 0 && ( _state.Data.items & ( 1 << j ) ) == 0 )
						_state.Data.item_gettime[j] = ( Single ) _state.Data.time;
				_state.Data.items = i2;
			}

			_state.Data.onground = ( bits & ProtocolDef.SU_ONGROUND ) != 0;
			_state.Data.inwater = ( bits & ProtocolDef.SU_INWATER ) != 0;

			if ( ( bits & ProtocolDef.SU_WEAPONFRAME ) != 0 )
				_state.Data.stats[QStatsDef.STAT_WEAPONFRAME] = _network.Reader.ReadByte();
			else
				_state.Data.stats[QStatsDef.STAT_WEAPONFRAME] = 0;

			if ( ( bits & ProtocolDef.SU_ARMOR ) != 0 )
				i2 = _network.Reader.ReadByte();
			else
				i2 = 0;
			if ( _state.Data.stats[QStatsDef.STAT_ARMOR] != i2 )
			{
				_state.Data.stats[QStatsDef.STAT_ARMOR] = i2;
				_screen.Elements.SetDirty( ElementFactory.HUD );
			}

			if ( ( bits & ProtocolDef.SU_WEAPON ) != 0 )
				i2 = _network.Reader.ReadByte();
			else
				i2 = 0;
			if ( _state.Data.stats[QStatsDef.STAT_WEAPON] != i2 )
			{
				_state.Data.stats[QStatsDef.STAT_WEAPON] = i2;
				_screen.Elements.SetDirty( ElementFactory.HUD );
			}

			i2 = _network.Reader.ReadShort();
			if ( _state.Data.stats[QStatsDef.STAT_HEALTH] != i2 )
			{
				_state.Data.stats[QStatsDef.STAT_HEALTH] = i2;
				_screen.Elements.SetDirty( ElementFactory.HUD );
			}

			i2 = _network.Reader.ReadByte();
			if ( _state.Data.stats[QStatsDef.STAT_AMMO] != i2 )
			{
				_state.Data.stats[QStatsDef.STAT_AMMO] = i2;
				_screen.Elements.SetDirty( ElementFactory.HUD );
			}

			for ( i2 = 0; i2 < 4; i2++ )
			{
				var j = _network.Reader.ReadByte();
				if ( _state.Data.stats[QStatsDef.STAT_SHELLS + i2] != j )
				{
					_state.Data.stats[QStatsDef.STAT_SHELLS + i2] = j;
					_screen.Elements.SetDirty( ElementFactory.HUD );
				}
			}

			i2 = _network.Reader.ReadByte();

			// Change
			if ( Engine.Common.GameKind == GameKind.StandardQuake )
			{
				if ( _state.Data.stats[QStatsDef.STAT_ACTIVEWEAPON] != i2 )
				{
					_state.Data.stats[QStatsDef.STAT_ACTIVEWEAPON] = i2;
					_screen.Elements.SetDirty( ElementFactory.HUD );
				}
			}
			else
			{
				if ( _state.Data.stats[QStatsDef.STAT_ACTIVEWEAPON] != ( 1 << i2 ) )
				{
					_state.Data.stats[QStatsDef.STAT_ACTIVEWEAPON] = ( 1 << i2 );
					_screen.Elements.SetDirty( ElementFactory.HUD );
				}
			}
		}

		/// <summary>
		/// CL_ParseServerInfo
		/// </summary>
		private void ParseServerInfo( )
		{
			_logger.DPrint( "Serverinfo packet received.\n" );

			//
			// wipe the client_state_t struct
			//
			ClearState();

			// parse protocol version number
			var i = _network.Reader.ReadLong();
			if ( i != ProtocolDef.PROTOCOL_VERSION )
			{
				_logger.Print( "Server returned version {0}, not {1}", i, ProtocolDef.PROTOCOL_VERSION );
				return;
			}

			// parse maxclients
			_state.Data.maxclients = _network.Reader.ReadByte();
			if ( _state.Data.maxclients < 1 || _state.Data.maxclients > QDef.MAX_SCOREBOARD )
			{
				_logger.Print( "Bad maxclients ({0}) from server\n", _state.Data.maxclients );
				return;
			}
			_state.Data.scores = new scoreboard_t[_state.Data.maxclients];// Hunk_AllocName (_state.Data.maxclients*sizeof(*_state.Data.scores), "scores");
			for ( i = 0; i < _state.Data.scores.Length; i++ )
				_state.Data.scores[i] = new scoreboard_t();

			// parse gametype
			_state.Data.gametype = _network.Reader.ReadByte();

			// parse signon message
			var str = _network.Reader.ReadString();
			_state.Data.levelname = Utilities.Copy( str, 40 );

			// seperate the printfs so the server message can have a color
			_logger.Print( ConsoleBar );
			_logger.Print( "{0}{1}\n", ( Char ) 2, str );

			//
			// first we go through and touch all of the precache data that still
			// happens to be in the cache, so precaching something else doesn't
			// needlessly purge it
			//

			// precache models
			Array.Clear( _state.Data.model_precache, 0, _state.Data.model_precache.Length );
			Int32 nummodels;
			var model_precache = new String[QDef.MAX_MODELS];
			for ( nummodels = 1; ; nummodels++ )
			{
				str = _network.Reader.ReadString();
				if ( String.IsNullOrEmpty( str ) )
					break;

				if ( nummodels == QDef.MAX_MODELS )
				{
					_logger.Print( "Server sent too many model precaches\n" );
					return;
				}
				model_precache[nummodels] = str;
				_models.TouchModel( str );
			}

			// precache sounds
			Array.Clear( _state.Data.sound_precache, 0, _state.Data.sound_precache.Length );
			Int32 numsounds;
			var sound_precache = new String[QDef.MAX_SOUNDS];
			for ( numsounds = 1; ; numsounds++ )
			{
				str = _network.Reader.ReadString();
				if ( String.IsNullOrEmpty( str ) )
					break;
				if ( numsounds == QDef.MAX_SOUNDS )
				{
					_logger.Print( "Server sent too many sound precaches\n" );
					return;
				}
				sound_precache[numsounds] = str;
				_sound.TouchSound( str );
			}

			//
			// now we try to load everything else until a cache allocation fails
			//
			for ( i = 1; i < nummodels; i++ )
			{
				var name = model_precache[i];
				var n = name.ToLower();
				var type = ModelType.Sprite;

				if ( n.StartsWith( "*" ) && !n.Contains( ".mdl" ) || n.Contains( ".bsp" ) )
					type = ModelType.Brush;
				else if ( n.Contains( ".mdl" ) )
					type = ModelType.Alias;
				else
					type = ModelType.Sprite;

				if ( name == "progs/player.mdl" )
				{

				}
				_state.Data.model_precache[i] = _models.ForName( name, false, type, false );
				if ( _state.Data.model_precache[i] == null )
				{
					_logger.Print( "Model {0} not found\n", name );
					return;
				}
				KeepaliveMessage();
			}

			_sound.BeginPrecaching();
			for ( i = 1; i < numsounds; i++ )
			{
				_state.Data.sound_precache[i] = _sound.PrecacheSound( sound_precache[i] );
				KeepaliveMessage();
			}
			_sound.EndPrecaching();

			// local state
			_state.Data.worldmodel = ( BrushModelData ) _state.Data.model_precache[1];
            _state.Entities[0].model = _state.Data.model_precache[1];

			_renderer.World.NewMap();

			_engine.NoClipAngleHack = false; // noclip is turned off at start

			// EWWWW - GET RID OF THIS ASAP!!
			GC.Collect();
		}

		// CL_ParseStartSoundPacket
		private void ParseStartSoundPacket( )
		{
			var field_mask = _network.Reader.ReadByte();
			Int32 volume;
			Single attenuation;

			if ( ( field_mask & ProtocolDef.SND_VOLUME ) != 0 )
				volume = _network.Reader.ReadByte();
			else
				volume = snd.DEFAULT_SOUND_PACKET_VOLUME;

			if ( ( field_mask & ProtocolDef.SND_ATTENUATION ) != 0 )
				attenuation = _network.Reader.ReadByte() / 64.0f;
			else
				attenuation = snd.DEFAULT_SOUND_PACKET_ATTENUATION;

			var channel = _network.Reader.ReadShort();
			var sound_num = _network.Reader.ReadByte();

			var ent = channel >> 3;
			channel &= 7;

			if ( ent > QDef.MAX_EDICTS )
				_engine.Error( "CL_ParseStartSoundPacket: ent = {0}", ent );

			var pos = _network.Reader.ReadCoords();
			_sound.StartSound( ent, channel, _state.Data.sound_precache[sound_num], ref pos, volume / 255.0f, attenuation );
		}

		// CL_NewTranslation
		private void NewTranslation( Int32 slot )
		{
			if ( slot > _state.Data.maxclients )
				Utilities.Error( "CL_NewTranslation: slot > _state.Data.maxclients" );

			var dest = _state.Data.scores[slot].translations;
			var source = _videoState.Data.colormap;
			Array.Copy( source, dest, dest.Length );

			var top = _state.Data.scores[slot].colors & 0xf0;
			var bottom = ( _state.Data.scores[slot].colors & 15 ) << 4;

			_renderer.TranslatePlayerSkin( slot );

			for ( Int32 i = 0, offset = 0; i < VideoDef.VID_GRADES; i++ )//, dest += 256, source+=256)
			{
				if ( top < 128 )    // the artists made some backwards ranges.  sigh.
					Buffer.BlockCopy( source, offset + top, dest, offset + render.TOP_RANGE, 16 );  //memcpy (dest + Render.TOP_RANGE, source + top, 16);
				else
					for ( var j = 0; j < 16; j++ )
						dest[offset + render.TOP_RANGE + j] = source[offset + top + 15 - j];

				if ( bottom < 128 )
					Buffer.BlockCopy( source, offset + bottom, dest, offset + render.BOTTOM_RANGE, 16 ); // memcpy(dest + Render.BOTTOM_RANGE, source + bottom, 16);
				else
					for ( var j = 0; j < 16; j++ )
						dest[offset + render.BOTTOM_RANGE + j] = source[offset + bottom + 15 - j];

				offset += 256;
			}
		}

		/// <summary>
		/// CL_EntityNum
		///
		/// This error checks and tracks the total number of entities
		/// </summary>
		/// <param name="num"></param>
		/// <returns></returns>
		private Entity EntityNum( Int32 num )
		{
			if ( num >= _state.Data.num_entities )
			{
				if ( num >= QDef.MAX_EDICTS )
					_engine.Error( "CL_EntityNum: %i is an invalid number", num );
				while ( _state.Data.num_entities <= num )
				{
                    _state.Entities[_state.Data.num_entities].colormap = _videoState.Data.colormap;
					_state.Data.num_entities++;
				}
			}

			return _state.Entities[num];
		}

		/// <summary>
		/// CL_ParseBaseline
		/// </summary>
		/// <param name="ent"></param>
		private void ParseBaseline( Entity ent )
		{
			ent.baseline.modelindex = _network.Reader.ReadByte();
			ent.baseline.frame = _network.Reader.ReadByte();
			ent.baseline.colormap = _network.Reader.ReadByte();
			ent.baseline.skin = _network.Reader.ReadByte();
			ent.baseline.origin.x = _network.Reader.ReadCoord();
			ent.baseline.angles.x = _network.Reader.ReadAngle();
			ent.baseline.origin.y = _network.Reader.ReadCoord();
			ent.baseline.angles.y = _network.Reader.ReadAngle();
			ent.baseline.origin.z = _network.Reader.ReadCoord();
			ent.baseline.angles.z = _network.Reader.ReadAngle();
		}

		/// <summary>
		/// CL_ParseStatic
		/// </summary>
		private void ParseStatic( )
		{
			var i = _state.Data.num_statics;

			if ( i >= ClientDef.MAX_STATIC_ENTITIES )
				_engine.Error( "Too many static entities" );

			var ent = _state.StaticEntities[i];
			_state.Data.num_statics++;
			ParseBaseline( ent );

			// copy it to the current state
			ent.model = _state.Data.model_precache[ent.baseline.modelindex];
			ent.frame = ent.baseline.frame;
			ent.colormap = _videoState.Data.colormap;
			ent.skinnum = ent.baseline.skin;
			ent.effects = ent.baseline.effects;
			ent.origin = Utilities.ToVector( ref ent.baseline.origin );
			ent.angles = Utilities.ToVector( ref ent.baseline.angles );
			_renderer.World.Entities.AddEfrags( ent );
		}

		/// <summary>
		/// CL_ParseStaticSound
		/// </summary>
		private void ParseStaticSound( )
		{
			var org = _network.Reader.ReadCoords();
			var sound_num = _network.Reader.ReadByte();
			var vol = _network.Reader.ReadByte();
			var atten = _network.Reader.ReadByte();

			_sound.StaticSound( _state.Data.sound_precache[sound_num], ref org, vol, atten );
		}

		/// <summary>
		/// CL_KeepaliveMessage
		/// When the client is taking a long time to load stuff, send keepalive messages
		/// so the server doesn't disconnect.
		/// </summary>
		private void KeepaliveMessage( )
		{
			if ( _serverState.IsActive )
				return; // no need if server is local

			if ( _state.StaticData.demoplayback )
				return;

			// read messages from server, should just be nops
			_network.Message.SaveState( ref _MsgState );

			Int32 ret;
			do
			{
				ret = GetMessage();
				switch ( ret )
				{
					default:
						_engine.Error( "CL_KeepaliveMessage: CL_GetMessage failed" );
						break;

					case 0:
						break;  // nothing waiting

					case 1:
						_engine.Error( "CL_KeepaliveMessage: received a message" );
						break;

					case 2:
						if ( _network.Reader.ReadByte() != ProtocolDef.svc_nop )
							_engine.Error( "CL_KeepaliveMessage: datagram wasn't a nop" );
						break;
				}
			} while ( ret != 0 );

			_network.Message.RestoreState( _MsgState );

			// check time
			var time = ( Single ) Timer.GetFloatTime();
			if ( time - _LastMsg < 5 )
				return;

			_LastMsg = time;

			// write out a nop
			_logger.Print( "--> client to server keepalive\n" );

			_state.StaticData.message.WriteByte( ProtocolDef.clc_nop );
			_network.SendMessage( _state.StaticData.netcon, _state.StaticData.message );
			_state.StaticData.message.Clear();
		}
	}
}