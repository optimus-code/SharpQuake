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
using SharpQuake.Game.Networking.Server;
using System;

namespace SharpQuake.Networking.Server
{
    public class ServerState
    {
        public Boolean IsActive
        {
            get
            {
                return Data.active;
            }
        }

        public Boolean IsLoading
        {
            get
            {
                return Data.state == server_state_t.Loading;
            }
        }

        // SV
        public server_t Data
        {
            get;
            private set;
        }

        // SVS
        public server_static_t StaticData
        {
            get;
            private set;
        }
        public Int32 CurrentSkill
        {
            get;
            set;
        }

        /// <summary>
        /// Can NOT be array indexed, because
        /// edict_t is variable sized, but can
        /// be used to reference the world ent
        /// </summary>
        public MemoryEdict[] Edicts
        {
            get;
            private set;
        }


        public MemoryEdict Player
        {
            get;
            set;
        }

        public Action OnProgramsChangeYaw
        {
            get;
            set;
        }

        public ServerState()
        {
            Data = new server_t( );
            StaticData = new server_static_t( );
        }

        /// <summary>
        /// EDICT_TO_PROG(e)
        /// </summary>
        public Int32 EdictToProg( MemoryEdict e )
        {
            return Array.IndexOf( Data.edicts, e ); // todo: optimize this
        }

        /// <summary>
        /// EDICT_NUM
        /// </summary>
        public MemoryEdict EdictNum( Int32 n )
        {
            if ( n < 0 || n >= Data.max_edicts )
                Utilities.Error( "EDICT_NUM: bad number {0}", n );

            return Data.edicts[n];
        }

        /// <summary>
        /// PROG_TO_EDICT(e)
        /// Offset in bytes!
        /// </summary>
        public MemoryEdict ProgToEdict( Int32 e )
        {
            if ( e < 0 || e > Data.edicts.Length )
                Utilities.Error( "ProgToEdict: Bad prog!" );

            return Data.edicts[e];
        }

        /// <summary>
        /// NUM_FOR_EDICT
        /// </summary>
        public Int32 NumForEdict( MemoryEdict e )
        {
            var i = Array.IndexOf( Data.edicts, e ); // todo: optimize this

            if ( i < 0 )
                Utilities.Error( "NUM_FOR_EDICT: bad pointer" );

            return i;
        }

        /// <summary>
        /// ED_Alloc
        /// Either finds a free edict, or allocates a new one.
        /// Try to avoid reusing an entity that was recently freed, because it
        /// can cause the client to think the entity morphed into something else
        /// instead of being removed and recreated, which can cause interpolated
        /// angles and bad trails.
        /// </summary>
        public MemoryEdict AllocEdict( )
        {
            MemoryEdict e;
            Int32 i;
            for ( i = StaticData.maxclients + 1; i < Data.num_edicts; i++ )
            {
                e = EdictNum( i );

                // the first couple seconds of server time can involve a lot of
                // freeing and allocating, so relax the replacement policy
                if ( e.free && ( e.freetime < 2 || Data.time - e.freetime > 0.5 ) )
                {
                    e.Clear( );
                    return e;
                }
            }

            if ( i == QDef.MAX_EDICTS )
                Utilities.Error( "ED_Alloc: no free edicts" );

            Data.num_edicts++;
            e = EdictNum( i );
            e.Clear( );

            return e;
        }

        /// <summary>
        /// ED_Free
        /// Marks the edict as free
        /// FIXME: walk all entities and NULL out references to this entity
        /// </summary>
        public void FreeEdict( MemoryEdict ed )
        {
            UnlinkEdict( ed );		// unlink from world bsp

            ed.free = true;
            ed.v.model = 0;
            ed.v.takedamage = 0;
            ed.v.modelindex = 0;
            ed.v.colormap = 0;
            ed.v.skin = 0;
            ed.v.frame = 0;
            ed.v.origin = default( Vector3f );
            ed.v.angles = default( Vector3f );
            ed.v.nextthink = -1;
            ed.v.solid = 0;

            ed.freetime = ( Single ) Data.time;
        }

        /// <summary>
		/// SV_UnlinkEdict
		/// call before removing an entity, and before trying to move one,
		/// so it doesn't clip against itself
		/// flags ent->v.modified
		/// </summary>
		public void UnlinkEdict( MemoryEdict ent )
        {
            if ( ent.area.Prev == null )
                return;     // not linked in anywhere

            ent.area.Remove( );  //RemoveLink(&ent->area);
                                 //ent->area.prev = ent->area.next = NULL;
        }
    }
}
