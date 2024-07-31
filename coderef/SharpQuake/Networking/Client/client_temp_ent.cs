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
using SharpQuake.Framework.IO.Sound;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Game.Data.Models;
using SharpQuake.Game.World;
using System;

// cl_tent.c

namespace SharpQuake
{
	partial class client
	{
		private SoundEffect_t _SfxWizHit; // cl_sfx_wizhit
		private SoundEffect_t _SfxKnigtHit; // cl_sfx_knighthit
		private SoundEffect_t _SfxTink1; // cl_sfx_tink1
		private SoundEffect_t _SfxRic1; // cl_sfx_ric1
		private SoundEffect_t _SfxRic2; // cl_sfx_ric2
		private SoundEffect_t _SfxRic3; // cl_sfx_ric3
		private SoundEffect_t _SfxRExp3; // cl_sfx_r_exp3

		// CL_InitTEnts
		private void PrecacheSFX( )
        {
            _sound.PrecacheAmbientSFX( );

            _SfxWizHit = _sound.PrecacheSound( "wizard/hit.wav" );
			_SfxKnigtHit = _sound.PrecacheSound( "hknight/hit.wav" );
			_SfxTink1 = _sound.PrecacheSound( "weapons/tink1.wav" );
			_SfxRic1 = _sound.PrecacheSound( "weapons/ric1.wav" );
			_SfxRic2 = _sound.PrecacheSound( "weapons/ric2.wav" );
			_SfxRic3 = _sound.PrecacheSound( "weapons/ric3.wav" );
			_SfxRExp3 = _sound.PrecacheSound( "weapons/r_exp3.wav" );
		}

		// CL_UpdateTEnts
		private void UpdateTempEntities( )
		{
            _state.NumTempEntities = 0;

			// update lightning
			for ( var i = 0; i < ClientDef.MAX_BEAMS; i++ )
			{
				var b = _state.Beams[i];
				if ( b.model == null || b.endtime < _state.Data.time )
					continue;

				// if coming from the player, update the start position
				if ( b.entity == _state.Data.viewentity )
				{
					b.start = _state.Entities[_state.Data.viewentity].origin;
				}

				// calculate pitch and yaw
				var dist = b.end - b.start;
				Single yaw, pitch, forward;

				if ( dist.Y == 0 && dist.X == 0 )
				{
					yaw = 0;
					if ( dist.Z > 0 )
						pitch = 90;
					else
						pitch = 270;
				}
				else
				{
					yaw = ( Int32 ) ( Math.Atan2( dist.Y, dist.X ) * 180 / Math.PI );
					if ( yaw < 0 )
						yaw += 360;

					forward = ( Single ) Math.Sqrt( dist.X * dist.X + dist.Y * dist.Y );
					pitch = ( Int32 ) ( Math.Atan2( dist.Z, forward ) * 180 / Math.PI );
					if ( pitch < 0 )
						pitch += 360;
				}

				// add new entities for the lightning
				var org = b.start;
				var d = MathLib.Normalize( ref dist );
				while ( d > 0 )
				{
					var ent = NewTempEntity();
					if ( ent == null )
						return;

					ent.origin = org;
					ent.model = b.model;
					ent.angles.X = pitch;
					ent.angles.Y = yaw;
					ent.angles.Z = MathLib.Random() % 360;

					org += dist * 30;
					// Uze: is this code bug (i is outer loop variable!!!) or what??????????????
					//for (i=0 ; i<3 ; i++)
					//    org[i] += dist[i]*30;
					d -= 30;
				}
			}
		}

		/// <summary>
		/// CL_NewTempEntity
		/// </summary>
		private Entity NewTempEntity( )
		{
			return _state.NewTempEntity( _videoState.Data.colormap );
		}

		/// <summary>
		/// CL_ParseTEnt
		/// </summary>
		private void ParseTempEntity( )
		{
			Vector3 pos;
			dlight_t dl;
			var type = _network.Reader.ReadByte();
			switch ( type )
			{
				case ProtocolDef.TE_WIZSPIKE:           // spike hitting wall
					pos = _network.Reader.ReadCoords();
					_renderer.World.Particles.RunParticleEffect( _state.Data.time, ref pos, ref Utilities.ZeroVector, 20, 30 );
					_sound.StartSound( -1, 0, _SfxWizHit, ref pos, 1, 1 );
					break;

				case ProtocolDef.TE_KNIGHTSPIKE:            // spike hitting wall
					pos = _network.Reader.ReadCoords();
					_renderer.World.Particles.RunParticleEffect( _state.Data.time, ref pos, ref Utilities.ZeroVector, 226, 20 );
					_sound.StartSound( -1, 0, _SfxKnigtHit, ref pos, 1, 1 );
					break;

				case ProtocolDef.TE_SPIKE:          // spike hitting wall
					pos = _network.Reader.ReadCoords();
#if GLTEST
                    Test_Spawn (pos);
#else
					_renderer.World.Particles.RunParticleEffect( _state.Data.time, ref pos, ref Utilities.ZeroVector, 0, 10 );
#endif
					if ( ( MathLib.Random() % 5 ) != 0 )
						_sound.StartSound( -1, 0, _SfxTink1, ref pos, 1, 1 );
					else
					{
						var rnd = MathLib.Random() & 3;
						if ( rnd == 1 )
							_sound.StartSound( -1, 0, _SfxRic1, ref pos, 1, 1 );
						else if ( rnd == 2 )
							_sound.StartSound( -1, 0, _SfxRic2, ref pos, 1, 1 );
						else
							_sound.StartSound( -1, 0, _SfxRic3, ref pos, 1, 1 );
					}
					break;

				case ProtocolDef.TE_SUPERSPIKE:         // super spike hitting wall
					pos = _network.Reader.ReadCoords();
					_renderer.World.Particles.RunParticleEffect( _state.Data.time, ref pos, ref Utilities.ZeroVector, 0, 20 );

					if ( ( MathLib.Random() % 5 ) != 0 )
						_sound.StartSound( -1, 0, _SfxTink1, ref pos, 1, 1 );
					else
					{
						var rnd = MathLib.Random() & 3;
						if ( rnd == 1 )
							_sound.StartSound( -1, 0, _SfxRic1, ref pos, 1, 1 );
						else if ( rnd == 2 )
							_sound.StartSound( -1, 0, _SfxRic2, ref pos, 1, 1 );
						else
							_sound.StartSound( -1, 0, _SfxRic3, ref pos, 1, 1 );
					}
					break;

				case ProtocolDef.TE_GUNSHOT:            // bullet hitting wall
					pos = _network.Reader.ReadCoords();
					_renderer.World.Particles.RunParticleEffect( _state.Data.time, ref pos, ref Utilities.ZeroVector, 0, 20 );
					break;

				case ProtocolDef.TE_EXPLOSION:          // rocket explosion
					pos = _network.Reader.ReadCoords();
					_renderer.World.Particles.ParticleExplosion( _state.Data.time, ref pos );
					dl = _state.AllocDlight( 0 );
					dl.origin = pos;
					dl.radius = 350;
					dl.die = ( Single ) _state.Data.time + 0.5f;
					dl.decay = 300;
					_sound.StartSound( -1, 0, _SfxRExp3, ref pos, 1, 1 );
					break;

				case ProtocolDef.TE_TAREXPLOSION:           // tarbaby explosion
					pos = _network.Reader.ReadCoords();
					_renderer.World.Particles.BlobExplosion( _state.Data.time, ref pos );
					_sound.StartSound( -1, 0, _SfxRExp3, ref pos, 1, 1 );
					break;

				case ProtocolDef.TE_LIGHTNING1:             // lightning bolts
					ParseBeam( _models.ForName( "progs/bolt.mdl", true, ModelType.Alias, false ) );
					break;

				case ProtocolDef.TE_LIGHTNING2:             // lightning bolts
					ParseBeam( _models.ForName( "progs/bolt2.mdl", true, ModelType.Alias, false ) );
					break;

				case ProtocolDef.TE_LIGHTNING3:             // lightning bolts
					ParseBeam( _models.ForName( "progs/bolt3.mdl", true, ModelType.Alias, false ) );
					break;

				// PGM 01/21/97
				case ProtocolDef.TE_BEAM:               // grappling hook beam
					ParseBeam( _models.ForName( "progs/beam.mdl", true, ModelType.Alias, false ) );
					break;
				// PGM 01/21/97

				case ProtocolDef.TE_LAVASPLASH:
					pos = _network.Reader.ReadCoords();
					_renderer.World.Particles.LavaSplash( _state.Data.time, ref pos );
					break;

				case ProtocolDef.TE_TELEPORT:
					pos = _network.Reader.ReadCoords();
					_renderer.World.Particles.TeleportSplash( _state.Data.time, ref pos );
					break;

				case ProtocolDef.TE_EXPLOSION2:             // color mapped explosion
					pos = _network.Reader.ReadCoords();
					var colorStart = _network.Reader.ReadByte();
					var colorLength = _network.Reader.ReadByte();
					_renderer.World.Particles.ParticleExplosion( _state.Data.time, ref pos, colorStart, colorLength );
					dl = _state.AllocDlight( 0 );
					dl.origin = pos;
					dl.radius = 350;
					dl.die = ( Single ) _state.Data.time + 0.5f;
					dl.decay = 300;
					_sound.StartSound( -1, 0, _SfxRExp3, ref pos, 1, 1 );
					break;

				default:
					Utilities.Error( "CL_ParseTEnt: bad type" );
					break;
			}
		}

		/// <summary>
		/// CL_ParseBeam
		/// </summary>
		private void ParseBeam( ModelData m )
		{
			var ent = _network.Reader.ReadShort();

			var start = _network.Reader.ReadCoords();
			var end = _network.Reader.ReadCoords();

			// override any beam with the same entity
			for ( var i = 0; i < ClientDef.MAX_BEAMS; i++ )
			{
				var b = _state.Beams[i];
				if ( b.entity == ent )
				{
					b.entity = ent;
					b.model = m;
					b.endtime = ( Single ) ( _state.Data.time + 0.2 );
					b.start = start;
					b.end = end;
					return;
				}
			}

			// find a free beam
			for ( var i = 0; i < ClientDef.MAX_BEAMS; i++ )
			{
				var b = _state.Beams[i];
				if ( b.model == null || b.endtime < _state.Data.time )
				{
					b.entity = ent;
					b.model = m;
					b.endtime = ( Single ) ( _state.Data.time + 0.2 );
					b.start = start;
					b.end = end;
					return;
				}
			}
			_logger.Print( "beam list overflow!\n" );
		}
	}
}