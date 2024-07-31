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
using SharpQuake.Framework;
using SharpQuake.Framework.Factories.IO;
using SharpQuake.Framework.IO;
using SharpQuake.Framework.Logging;
using SharpQuake.Framework.Mathematics;
using SharpQuake.Networking.Client;
using SharpQuake.Networking.Server;

namespace SharpQuake.Sys.Programs
{
    public class ProgramsEdict
    {
        private readonly IEngine _engine;
        private readonly IConsoleLogger _logger;
        private readonly CommandFactory _commands;
        private readonly ClientVariableFactory _cvars;
        private readonly ClientState _clientState;
        private readonly ProgramsState _state;
        private readonly ServerState _serverState;

        public ProgramsEdict( IEngine engine, IConsoleLogger logger, CommandFactory commands, 
            ClientVariableFactory cvars, ClientState clientState,
            ProgramsState state, ServerState serverState )
        {
            _engine = engine;
            _logger = logger;
            _commands = commands;
            _cvars = cvars;
            _clientState = clientState;
            _state = state;
            _serverState = serverState;

            // Temporary workaround - will fix later
            ProgramsWrapper.OnGetString += ( strId ) =>
            {
                return _state.GetString( strId );
            };

            _commands.Add( "edict", PrintEdict_f );
            _commands.Add( "edicts", PrintEdicts );
            _commands.Add( "edictcount", EdictCount );
            _commands.Add( "test5", Test5_f );

            if ( Cvars.NoMonsters == null )
            {
                Cvars.NoMonsters = _cvars.Add( "nomonsters", false );
                Cvars.GameCfg = _cvars.Add( "gamecfg", false );
                Cvars.Scratch1 = _cvars.Add( "scratch1", false );
                Cvars.Scratch2 = _cvars.Add( "scratch2", false );
                Cvars.Scratch3 = _cvars.Add( "scratch3", false );
                Cvars.Scratch4 = _cvars.Add( "scratch4", false );
                Cvars.SavedGameCfg = _cvars.Add( "savedgamecfg", false, ClientVariableFlags.Archive );
                Cvars.Saved1 = _cvars.Add( "saved1", false, ClientVariableFlags.Archive );
                Cvars.Saved2 = _cvars.Add( "saved2", false, ClientVariableFlags.Archive );
                Cvars.Saved3 = _cvars.Add( "saved3", false, ClientVariableFlags.Archive );
                Cvars.Saved4 = _cvars.Add( "saved4", false, ClientVariableFlags.Archive );
            }
        }

        /// <summary>
        /// ED_PrintEdicts
        /// </summary>
        /// <remarks>
        /// (For debugging, prints all the entities in the current server)
        /// </remarks>
        /// <param name="msg"></param>
        public void PrintEdicts( CommandMessage msg )
        {
            _logger.Print( "{0} entities\n", _serverState.Data.num_edicts );

            for ( var i = 0; i < _serverState.Data.num_edicts; i++ )
                _state.PrintNum( i );
        }

        /// <summary>
        /// ED_LoadFromFile
        /// The entities are directly placed in the array, rather than allocated with
        /// ED_Alloc, because otherwise an error loading the map would have entity
        /// number references out of order.
        ///
        /// Creates a server's entity / program execution context by
        /// parsing textual entity definitions out of an ent file.
        ///
        /// Used for both fresh maps and savegame loads.  A fresh map would also need
        /// to call ED_CallSpawnFunctions () to let the objects initialize themselves.
        /// </summary>
        public void LoadFromFile( String data )
        {
            MemoryEdict ent = null;
            var inhibit = 0;
            _state.GlobalStruct.time = ( Single ) _serverState.Data.time;

            // parse ents
            while ( true )
            {
                // parse the opening brace
                data = Tokeniser.Parse( data );
                if ( data == null )
                    break;

                if ( Tokeniser.Token != "{" )
                    Utilities.Error( "ED_LoadFromFile: found {0} when expecting {", Tokeniser.Token );

                if ( ent == null )
                    ent = _serverState.EdictNum( 0 );
                else
                    ent = _serverState.AllocEdict( );
                data = ParseEdict( data, ent );

                // remove things from different skill levels or deathmatch
                if ( Cvars.Deathmatch.Get<Int32>( ) != 0 )
                {
                    if ( ( ( Int32 ) ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_DEATHMATCH ) != 0 )
                    {
                        _serverState.FreeEdict( ent );
                        inhibit++;
                        continue;
                    }
                }
                else if ( ( _serverState.CurrentSkill == 0 && ( ( Int32 ) ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_EASY ) != 0 ) ||
                    ( _serverState.CurrentSkill == 1 && ( ( Int32 ) ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_MEDIUM ) != 0 ) ||
                    ( _serverState.CurrentSkill >= 2 && ( ( Int32 ) ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_HARD ) != 0 ) )
                {
                    _serverState.FreeEdict( ent );
                    inhibit++;
                    continue;
                }

                //
                // immediately call spawn function
                //
                if ( ent.v.classname == 0 )
                {
                    _logger.Print( "No classname for:\n" );
                    _state.Print( ent );
                    _serverState.FreeEdict( ent );
                    continue;
                }

                // look for the spawn function
                var func = _state.IndexOfFunction( _state.GetString( ent.v.classname ) );
                if ( func == -1 )
                {
                    _logger.Print( "No spawn function for:\n" );
                    _state.Print( ent );
                    _serverState.FreeEdict( ent );
                    continue;
                }

                _state.GlobalStruct.self = _serverState.EdictToProg( ent );
                _state.OnExecute?.Invoke( func );
            }

            _logger.DPrint( "{0} entities inhibited\n", inhibit );
        }

        /// <summary>
        /// ED_ParseEdict
        /// Parses an edict out of the given string, returning the new position
        /// ed should be a properly initialized empty edict.
        /// Used for initial level load and for savegames.
        /// </summary>
        public String ParseEdict( String data, MemoryEdict ent )
        {
            var init = false;

            // clear it
            if ( ent != _serverState.Data.edicts[0] )	// hack
                ent.Clear( );

            // go through all the dictionary pairs
            Boolean anglehack;
            while ( true )
            {
                // parse key
                data = Tokeniser.Parse( data );
                if ( Tokeniser.Token.StartsWith( "}" ) )
                    break;

                if ( data == null )
                    Utilities.Error( "ED_ParseEntity: EOF without closing brace" );

                var token = Tokeniser.Token;

                // anglehack is to allow QuakeEd to write single scalar angles
                // and allow them to be turned into vectors. (FIXME...)
                if ( token == "angle" )
                {
                    token = "angles";
                    anglehack = true;
                }
                else
                    anglehack = false;

                // FIXME: change light to _light to get rid of this hack
                if ( token == "light" )
                    token = "light_lev";	// hack for single light def

                var keyname = token.TrimEnd( );

                // parse value
                data = Tokeniser.Parse( data );
                if ( data == null )
                    Utilities.Error( "ED_ParseEntity: EOF without closing brace" );

                if ( Tokeniser.Token.StartsWith( "}" ) )
                    Utilities.Error( "ED_ParseEntity: closing brace without data" );

                init = true;

                // keynames with a leading underscore are used for utility comments,
                // and are immediately discarded by quake
                if ( keyname[0] == '_' )
                    continue;

                var key = _state.FindField( keyname );
                if ( key == null )
                {
                    _logger.Print( "'{0}' is not a field\n", keyname );
                    continue;
                }

                token = Tokeniser.Token;
                if ( anglehack )
                {
                    token = "0 " + token + " 0";
                }

                if ( !ParsePair( ent, key, token ) )
                    _engine.Error( "ED_ParseEdict: parse error" );
            }

            if ( !init )
                ent.free = true;

            return data;
        }

        public Single GetEdictFieldFloat( MemoryEdict ed, String field, Single defValue = 0 )
        {
            var def = _state.CachedSearch( ed, field );
            if ( def == null )
                return defValue;

            return ed.GetFloat( def.ofs );
        }

        public Boolean SetEdictFieldFloat( MemoryEdict ed, String field, Single value )
        {
            var def = _state.CachedSearch( ed, field );
            if ( def != null )
            {
                ed.SetFloat( def.ofs, value );
                return true;
            }
            return false;
        }


        /// <summary>
        /// ED_WriteGlobals
        /// </summary>
        public unsafe void WriteGlobals( StreamWriter writer )
        {
            writer.WriteLine( "{" );
            for ( var i = 0; i < _state.Data.numglobaldefs; i++ )
            {
                var def = _state.GlobalDefs[i];
                var type = ( EdictType ) def.type;
                if ( ( def.type & ProgramDef.DEF_SAVEGLOBAL ) == 0 )
                    continue;

                type &= ( EdictType ) ~ProgramDef.DEF_SAVEGLOBAL;

                if ( type != EdictType.ev_string && type != EdictType.ev_float && type != EdictType.ev_entity )
                    continue;

                writer.Write( "\"" );
                writer.Write( _state.GetString( def.s_name ) );
                writer.Write( "\" \"" );
                writer.Write( _state.UglyValueString( type, ( EVal* ) _state.Get( def.ofs ) ) );
                writer.WriteLine( "\"" );
            }
            writer.WriteLine( "}" );
        }

        /// <summary>
        /// ED_Write
        /// </summary>
        public unsafe void WriteEdict( StreamWriter writer, MemoryEdict ed )
        {
            writer.WriteLine( "{" );

            if ( ed.free )
            {
                writer.WriteLine( "}" );
                return;
            }

            for ( var i = 1; i < _state.Data.numfielddefs; i++ )
            {
                var d = _state.FieldDefs[i];
                var name = _state.GetString( d.s_name );
                if ( name != null && name.Length > 2 && name[name.Length - 2] == '_' )// [strlen(name) - 2] == '_')
                    continue;	// skip _x, _y, _z vars

                var type = d.type & ~ProgramDef.DEF_SAVEGLOBAL;
                Int32 offset1;
                if ( ed.IsV( d.ofs, out offset1 ) )
                {
                    fixed ( void* ptr = &ed.v )
                    {
                        var v = ( Int32* ) ptr + offset1;
                        if ( _state.IsEmptyField( type, v ) )
                            continue;

                        writer.WriteLine( "\"{0}\" \"{1}\"", name, _state.UglyValueString( ( EdictType ) d.type, ( EVal* ) v ) );
                    }
                }
                else
                {
                    fixed ( void* ptr = ed.fields )
                    {
                        var v = ( Int32* ) ptr + offset1;
                        if ( _state.IsEmptyField( type, v ) )
                            continue;

                        writer.WriteLine( "\"{0}\" \"{1}\"", name, _state.UglyValueString( ( EdictType ) d.type, ( EVal* ) v ) );
                    }
                }
            }

            writer.WriteLine( "}" );
        }

        /// <summary>
        /// ED_ParseGlobals
        /// </summary>
        public void ParseGlobals( String data )
        {
            while ( true )
            {
                // parse key
                data = Tokeniser.Parse( data );
                if ( Tokeniser.Token.StartsWith( "}" ) )
                    break;

                if ( String.IsNullOrEmpty( data ) )
                    Utilities.Error( "ED_ParseEntity: EOF without closing brace" );

                var keyname = Tokeniser.Token;

                // parse value
                data = Tokeniser.Parse( data );
                if ( String.IsNullOrEmpty( data ) )
                    Utilities.Error( "ED_ParseEntity: EOF without closing brace" );

                if ( Tokeniser.Token.StartsWith( "}" ) )
                    Utilities.Error( "ED_ParseEntity: closing brace without data" );

                var key = _state.FindGlobal( keyname );
                if ( key == null )
                {
                    _logger.Print( "'{0}' is not a global\n", keyname );
                    continue;
                }

                if ( !ParseGlobalPair( key, Tokeniser.Token ) )
                    _engine.Error( "ED_ParseGlobals: parse error" );
            }
        }


        private void Test5_f( CommandMessage msg )
        {
            var p = _clientState.ViewEntity;
            if ( p == null )
                return;

            var org = p.origin;

            for ( var i = 0; i < _serverState.Data.edicts.Length; i++ )
            {
                var ed = _serverState.Data.edicts[i];

                if ( ed.free )
                    continue;

                Vector3 vmin, vmax;
                MathLib.Copy( ref ed.v.absmax, out vmax );
                MathLib.Copy( ref ed.v.absmin, out vmin );

                if ( org.X >= vmin.X && org.Y >= vmin.Y && org.Z >= vmin.Z &&
                    org.X <= vmax.X && org.Y <= vmax.Y && org.Z <= vmax.Z )
                {
                    _logger.Print( "{0}\n", i );
                }
            }
        }

        /// <summary>
        /// ED_PrintEdict_f
        /// For debugging, prints a single edict
        /// </summary>
        private void PrintEdict_f( CommandMessage msg )
        {
            var i = MathLib.atoi( msg.Parameters[0] );

            if ( i >= _serverState.Data.num_edicts )
            {
                _logger.Print( "Bad edict number\n" );
                return;
            }

            _state.PrintNum( i );
        }

        // ED_Count
        //
        // For debugging
        private void EdictCount( CommandMessage msg )
        {
            Int32 active = 0, models = 0, solid = 0, step = 0;

            for ( var i = 0; i < _serverState.Data.num_edicts; i++ )
            {
                var ent = _serverState.EdictNum( i );
                if ( ent.free )
                    continue;
                active++;
                if ( ent.v.solid != 0 )
                    solid++;
                if ( ent.v.model != 0 )
                    models++;
                if ( ent.v.movetype == Movetypes.MOVETYPE_STEP )
                    step++;
            }

            _logger.Print( "num_edicts:{0}\n", _serverState.Data.num_edicts );
            _logger.Print( "active    :{0}\n", active );
            _logger.Print( "view      :{0}\n", models );
            _logger.Print( "touch     :{0}\n", solid );
            _logger.Print( "step      :{0}\n", step );
        }

        /// <summary>
        /// Since memory block containing original edict_t plus additional data
        /// is split into two fiels - edict_t.v and edict_t.fields we must check key.ofs
        /// to choose between thistwo parts.
        /// Warning: Key offset is in integers not bytes!
        /// </summary>
        private unsafe Boolean ParsePair( MemoryEdict ent, ProgramDefinition key, String s )
        {
            Int32 offset1;
            if ( ent.IsV( key.ofs, out offset1 ) )
            {
                fixed ( EntVars* ptr = &ent.v )
                {
                    return ParsePair( ( Int32* ) ptr + offset1, key, s );
                }
            }
            else
                fixed ( Single* ptr = ent.fields )
                {
                    return ParsePair( ptr + offset1, key, s );
                }
        }

        /// <summary>
        /// ED_ParseEpair
        /// Can parse either fields or globals returns false if error
        /// Uze: Warning! value pointer is already with correct offset (value = base + key.ofs)!
        /// </summary>
        private unsafe Boolean ParsePair( void* value, ProgramDefinition key, String s )
        {
            var d = value;// (void *)((int *)base + key->ofs);

            switch ( ( EdictType ) ( key.type & ~ProgramDef.DEF_SAVEGLOBAL ) )
            {
                case EdictType.ev_string:
                    *( Int32* ) d = _state.NewString( s );// - pr_strings;
                    break;

                case EdictType.ev_float:
                    *( Single* ) d = MathLib.atof( s );
                    break;

                case EdictType.ev_vector:
                    var vs = s.Split( ' ' );
                    ( ( Single* ) d )[0] = MathLib.atof( vs[0] );
                    ( ( Single* ) d )[1] = ( vs.Length > 1 ? MathLib.atof( vs[1] ) : 0 );
                    ( ( Single* ) d )[2] = ( vs.Length > 2 ? MathLib.atof( vs[2] ) : 0 );
                    break;

                case EdictType.ev_entity:
                    *( Int32* ) d = _serverState.EdictToProg( _serverState.EdictNum( MathLib.atoi( s ) ) );
                    break;

                case EdictType.ev_field:
                    var f = _state.IndexOfField( s );
                    if ( f == -1 )
                    {
                        _logger.Print( "Can't find field {0}\n", s );
                        return false;
                    }
                    *( Int32* ) d = _state.GetInt32( _state.FieldDefs[f].ofs );
                    break;

                case EdictType.ev_function:
                    var func = _state.IndexOfFunction( s );
                    if ( func == -1 )
                    {
                        _logger.Print( "Can't find function {0}\n", s );
                        return false;
                    }
                    *( Int32* ) d = func;// - pr_functions;
                    break;

                default:
                    break;
            }
            return true;
        }

        private unsafe Boolean ParseGlobalPair( ProgramDefinition key, String value )
        {
            if ( _state.IsGlobalStruct( key.ofs, out var offset ) )
            {
                return ParsePair( ( Single* ) _state.GlobalStructAddr + offset, key, value );
            }
            return ParsePair( ( Single* ) _state.GlobalsAddr + offset, key, value );
        }
    }
}
