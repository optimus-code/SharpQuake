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
using SharpQuake.Framework.Logging;
using System;

namespace SharpQuake.Networking.Server
{
    public class ServerSound
    {
        private readonly IConsoleLogger _logger;
        private readonly ServerState _state;

        public ServerSound( IConsoleLogger logger, ServerState state )
        {
            _logger = logger;
            _state = state;
        }

        /// <summary>
        /// SV_StartSound
        /// Each entity can have eight independant sound sources, like voice,
        /// weapon, feet, etc.
        ///
        /// Channel 0 is an auto-allocate channel, the others override anything
        /// allready running on that entity/channel pair.
        ///
        /// An attenuation of 0 will play full volume everywhere in the level.
        /// Larger attenuations will drop off.  (max 4 attenuation)
        /// </summary>
        public void StartSound( MemoryEdict entity, Int32 channel, String sample, Int32 volume, Single attenuation )
        {
            if ( volume < 0 || volume > 255 )
                Utilities.Error( "SV_StartSound: volume = {0}", volume );

            if ( attenuation < 0 || attenuation > 4 )
                Utilities.Error( "SV_StartSound: attenuation = {0}", attenuation );

            if ( channel < 0 || channel > 7 )
                Utilities.Error( "SV_StartSound: channel = {0}", channel );

            if ( _state.Data.datagram.Length > QDef.MAX_DATAGRAM - 16 )
                return;

            // find precache number for sound
            Int32 sound_num;
            for ( sound_num = 1; sound_num < QDef.MAX_SOUNDS && _state.Data.sound_precache[sound_num] != null; sound_num++ )
                if ( sample == _state.Data.sound_precache[sound_num] )
                    break;

            if ( sound_num == QDef.MAX_SOUNDS || String.IsNullOrEmpty( _state.Data.sound_precache[sound_num] ) )
            {
                _logger.Print( "SV_StartSound: {0} not precacheed\n", sample );
                return;
            }

            var ent = _state.NumForEdict( entity );

            channel = ( ent << 3 ) | channel;

            var field_mask = 0;
            if ( volume != snd.DEFAULT_SOUND_PACKET_VOLUME )
                field_mask |= ProtocolDef.SND_VOLUME;
            if ( attenuation != snd.DEFAULT_SOUND_PACKET_ATTENUATION )
                field_mask |= ProtocolDef.SND_ATTENUATION;

            // directed messages go only to the entity the are targeted on
            _state.Data.datagram.WriteByte( ProtocolDef.svc_sound );
            _state.Data.datagram.WriteByte( field_mask );
            if ( ( field_mask & ProtocolDef.SND_VOLUME ) != 0 )
                _state.Data.datagram.WriteByte( volume );
            if ( ( field_mask & ProtocolDef.SND_ATTENUATION ) != 0 )
                _state.Data.datagram.WriteByte( ( Int32 ) ( attenuation * 64 ) );
            _state.Data.datagram.WriteShort( channel );
            _state.Data.datagram.WriteByte( sound_num );
            Vector3f v;
            MathLib.VectorAdd( ref entity.v.mins, ref entity.v.maxs, out v );
            MathLib.VectorMA( ref entity.v.origin, 0.5f, ref v, out v );
            _state.Data.datagram.WriteCoord( v.x );
            _state.Data.datagram.WriteCoord( v.y );
            _state.Data.datagram.WriteCoord( v.z );
        }
    }
}
