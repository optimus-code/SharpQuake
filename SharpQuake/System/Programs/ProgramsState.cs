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
using SharpQuake.Framework.Logging;
using SharpQuake.Networking.Server;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpQuake.Sys.Programs
{
    public class ProgramsState
    {
        public Int32 EdictSize
        {
            get;
            private set;
        }

        public Int32 CRC
        {
            get;
            private set;
        }

        public GlobalVariables GlobalStruct
        {
            get;
            private set;
        }

        public Program Data
        {
            get;
            private set;
        }

        public ProgramFunction[] Functions
        {
            get;
            private set;
        }

        public String Strings
        {
            get;
            private set;
        }

        public ProgramDefinition[] GlobalDefs
        {
            get;
            private set;
        }

        public ProgramDefinition[] FieldDefs
        {
            get;
            private set;
        }

        public Statement[] Statements
        {
            get;
            private set;
        }

        public Single[] Globals
        {
            get;
            private set;
        }

        private GCHandle HGlobalStruct
        {
            get;
            set;
        }

        private GCHandle HGlobals
        {
            get;
            set;
        }

        public Int64 GlobalStructAddr
        {
            get;
            private set;
        }

        public Int64 GlobalsAddr
        {
            get;
            private set;
        }

        private struct gefv_cache
        {
            public ProgramDefinition pcache;
            public String field;// char	field[MAX_FIELD_LEN];
        }

        private const Int32 GEFV_CACHESIZE = 2;

        private gefv_cache[] GefvCache
        {
            get;
            set;
        } = new gefv_cache[GEFV_CACHESIZE]; // gefvCache

        private Int32 GefvPos
        { 
            get;
            set;
        }

        public List<String> DynamicStrings
        {
            get;
            private set;
        } = new List<String>( 512 );

        public Int32 TemporaryString
        {
            get;
            set;
        } = -1;

        // pr_argc
        public Int32 ArgC
        {
            get;
            set;
        }

        // pr_xfunction
        public ProgramFunction xFunction
        {
            get;
            set;
        }

        // pr_depth
        public Int32 Depth
        {
            get;
            set;
        }

        public const Int32 MAX_STACK_DEPTH = 32;
        public const Int32 LOCALSTACK_SIZE = 2048;

        // pr_trace
        public ProgramStack[] Stack
        {
            get;
            private set;
        } = new ProgramStack[MAX_STACK_DEPTH]; // pr_stack

        public Int32[] LocalStack
        {
            get;
            private set;
        } = new Int32[LOCALSTACK_SIZE]; // localstack

        // localstack_used
        public Int32 LocalStackUsed
        {
            get;
            set;
        }

        // pr_xstatement
        public Int32 xStatement
        {
            get;
            set;
        }

        public Boolean Trace
        {
            get;
            set;
        }

        public Action<int> OnExecute
        {
            get;
            set;
        }

        public Action OnReset
        {
            get;
            set;
        }

        public Action<int> OnExecuteBuiltIn
        {
            get;
            set;
        }

        public Int32 BuiltInCount
        {
            get;
            set;
        }

        private static Int32[] TYPE_SIZE = new Int32[8] // type_size
        {
            1, sizeof(Int32)/4, 1, 3, 1, 1, sizeof(Int32)/4, IntPtr.Size/4
        };

        private readonly IConsoleLogger _logger;
        private readonly ServerState _serverState;

        public ProgramsState( IConsoleLogger logger, ServerState serverState )
        {
            _logger = logger;
            _serverState = serverState;
        }

        /// <summary>
        /// Load the progs.dat file (PR_LoadProgs)
        /// </summary>
        public void Load( )
        {
            Reset( );
            Parse( FileSystem.LoadFile( "progs.dat" ) );            
        }

        private void Parse( Byte[] buffer )
        {
            if ( buffer == null )
                Utilities.Error( "PR_LoadProgs: couldn't find Programs.dat" );

            Data = Utilities.BytesToStructure<Program>( buffer, 0 );

            if ( Data == null )
                Utilities.Error( "PR_LoadProgs: couldn't load Programs.dat" );

            _logger.DPrint( "Programs occupy {0}K.\n", buffer.Length / 1024 );

            CRC = Utilities.CalculateCrc32( buffer );

            // byte swap the header
            Data.SwapBytes( );

            CheckHeader( );
            ParseFunctions( buffer );
            ParseStrings( buffer );
            ParseGlobalDefinitions( buffer );
            ParseFieldDefinitions( buffer );
            ParseStatements( buffer );
            SwapBytesBigEndian( buffer );
            ParseGlobalStruct( buffer );
            ParseGlobals( buffer );
            ParseEdictSize( );
            PinHandles( );
        }

        /// <summary>
        /// Flush the non-C variable lookup cache
        /// </summary>
        private void FlushVariableLookupCache( )
        {
            // flush the non-C variable lookup cache
            for ( var i = 0; i < GEFV_CACHESIZE; i++ )
                GefvCache[i].field = null;
        }

        private void Reset( )
        {
            UnpinHandles( );
            OnReset?.Invoke( );
            DynamicStrings.Clear( );
            FlushVariableLookupCache( );
        }

        private void CheckHeader( )
        {
            if ( Data.version != ProgramDef.PROG_VERSION )
                Utilities.Error( "progs.dat has wrong version number ({0} should be {1})", Data.version, ProgramDef.PROG_VERSION );

            if ( Data.crc != ProgramDef.PROGHEADER_CRC )
                Utilities.Error( "progs.dat system vars have been modified, progdefs.h is out of date" );
        }

        private void ParseFunctions( Byte[] buffer )
        {
            Functions = new ProgramFunction[Data.numfunctions];
            var offset = Data.ofs_functions;

            for ( var i = 0; i < Functions.Length; i++, offset += ProgramFunction.SizeInBytes )
            {
                Functions[i] = Utilities.BytesToStructure<ProgramFunction>( buffer, offset );
                Functions[i].SwapBytes( );
            }
        }

        private void ParseStrings( Byte[] buffer )
        {
            var offset = Data.ofs_strings;
            var str0 = offset;

            for ( var i = 0; i < Data.numstrings; i++, offset++ )
            {
                // count string length
                while ( buffer[offset] != 0 )
                    offset++;
            }

            var length = offset - str0;
            Strings = Encoding.ASCII.GetString( buffer, str0, length );
        }

        private void ParseGlobalDefinitions( Byte[] buffer )
        {
            GlobalDefs = new ProgramDefinition[Data.numglobaldefs];
            var offset = Data.ofs_globaldefs;

            for ( var i = 0; i < GlobalDefs.Length; i++, offset += ProgramDefinition.SizeInBytes )
            {
                GlobalDefs[i] = Utilities.BytesToStructure<ProgramDefinition>( buffer, offset );
                GlobalDefs[i].SwapBytes( );
            }
        }

        private void ParseFieldDefinitions( Byte[] buffer )
        {
            FieldDefs = new ProgramDefinition[Data.numfielddefs];
            var offset = Data.ofs_fielddefs;

            for ( var i = 0; i < FieldDefs.Length; i++, offset += ProgramDefinition.SizeInBytes )
            {
                FieldDefs[i] = Utilities.BytesToStructure<ProgramDefinition>( buffer, offset );
                FieldDefs[i].SwapBytes( );

                if ( ( FieldDefs[i].type & ProgramDef.DEF_SAVEGLOBAL ) != 0 )
                    Utilities.Error( "PR_LoadProgs: pr_fielddefs[i].type & DEF_SAVEGLOBAL" );
            }
        }

        private void ParseStatements( Byte[] buffer )
        {
            Statements = new Statement[Data.numstatements];
            var offset = Data.ofs_statements;

            for ( var i = 0; i < Statements.Length; i++, offset += Statement.SizeInBytes )
            {
                Statements[i] = Utilities.BytesToStructure<Statement>( buffer, offset );
                Statements[i].SwapBytes( );
            }
        }
        
        private void SwapBytesBigEndian( Byte[] buffer )
        {
            // Swap bytes inplace if needed
            if ( !BitConverter.IsLittleEndian )
            {
                var offset = Data.ofs_globals;

                for ( var i = 0; i < Data.numglobals; i++, offset += 4 )
                    SwapHelper.Swap4b( buffer, offset );
            }
        }

        private void ParseGlobalStruct( Byte[] buffer )
        {
            GlobalStruct = Utilities.BytesToStructure<GlobalVariables>( buffer, Data.ofs_globals );
        }

        public void ParseGlobals( Byte[] buffer )
        {
            Globals = new Single[Data.numglobals - GlobalVariables.SizeInBytes / 4];
            Buffer.BlockCopy( buffer, Data.ofs_globals + GlobalVariables.SizeInBytes, Globals, 0, Globals.Length * 4 );
        }

        private void ParseEdictSize()
        {
            EdictSize = Data.entityfields * 4 + Edict.SizeInBytes - EntVars.SizeInBytes;
            ProgramDef.EdictSize = EdictSize;
        }

        private void UnpinHandles()
        {
            if ( HGlobals.IsAllocated )
            {
                HGlobals.Free( );
                GlobalsAddr = 0;
            }
            if ( HGlobalStruct.IsAllocated )
            {
                HGlobalStruct.Free( );
                GlobalStructAddr = 0;
            }
        }

        private void PinHandles()
        {
            HGlobals = GCHandle.Alloc( Globals, GCHandleType.Pinned );
            GlobalsAddr = HGlobals.AddrOfPinnedObject( ).ToInt64( );

            HGlobalStruct = GCHandle.Alloc( GlobalStruct, GCHandleType.Pinned );
            GlobalStructAddr = HGlobalStruct.AddrOfPinnedObject( ).ToInt64( );
        }

        private Boolean IsStaticString( Int32 stringId, out Int32 offset )
        {
            offset = stringId & 0xFFFFFF;
            return ( ( stringId >> 24 ) & 1 ) == 0;
        }

        public String GetString( Int32 strId )
        {
            if ( IsStaticString( strId, out var offset ) )
            {
                var i0 = offset;
                while ( offset < Strings.Length && Strings[offset] != 0 )
                    offset++;

                var length = offset - i0;

                if ( length > 0 )
                    return Strings.Substring( i0, length );
            }
            else
            {
                if ( offset < 0 || offset >= DynamicStrings.Count )
                {
                    throw new ArgumentException( "Invalid string id!" );
                }
                return DynamicStrings[offset];
            }

            return String.Empty;
        }

        public Boolean SameName( Int32 name1, String name2 )
        {
            var offset = name1;
            if ( offset + name2.Length > Strings.Length )
                return false;

            for ( var i = 0; i < name2.Length; i++, offset++ )
                if ( Strings[offset] != name2[i] )
                    return false;

            if ( offset < Strings.Length && Strings[offset] != 0 )
                return false;

            return true;
        }

        public Int32 IndexOfFunction( String name )
        {
            for ( var i = 0; i < Functions.Length; i++ )
            {
                if ( SameName( Functions[i].s_name, name ) )
                    return i;
            }

            return -1;
        }

        public Int32 IndexOfField( String name )
        {
            for ( var i = 0; i < FieldDefs.Length; i++ )
            {
                if ( SameName( FieldDefs[i].s_name, name ) )
                    return i;
            }
            return -1;
        }

        public Int32 IndexOfField( Int32 ofs )
        {
            for ( var i = 0; i < FieldDefs.Length; i++ )
            {
                if ( FieldDefs[i].ofs == ofs )
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// ED_FieldAtOfs
        /// </summary>
        public ProgramDefinition FindField( Int32 ofs )
        {
            var i = IndexOfField( ofs );

            if ( i != -1 )
                return FieldDefs[i];

            return null;
        }

        /// <summary>
        /// ED_FindField
        /// </summary>
        public ProgramDefinition FindField( String name )
        {
            var i = IndexOfField( name );

            if ( i != -1 )
                return FieldDefs[i];

            return null;
        }

        /// <summary>
        /// Returns true if ofs is inside GlobalStruct or false if ofs is in _Globals
        /// Out parameter offset is set to correct offset inside either GlobalStruct or _Globals
        /// </summary>
        public Boolean IsGlobalStruct( Int32 ofs, out Int32 offset )
        {
            if ( ofs < GlobalVariables.SizeInBytes >> 2 )
            {
                offset = ofs;
                return true;
            }
            offset = ofs - ( GlobalVariables.SizeInBytes >> 2 );
            return false;
        }

        /// <summary>
        /// ED_FindGlobal
        /// </summary>
        public ProgramDefinition FindGlobal( String name )
        {
            for ( var i = 0; i < GlobalDefs.Length; i++ )
            {
                var def = GlobalDefs[i];

                if ( name == GetString( def.s_name ) )
                    return def;
            }

            return null;
        }

        /// <summary>
        /// Mimics G_xxx macros
        /// But globals are split too, so we must check offset and choose
        /// GlobalStruct or _Globals
        /// </summary>
        public unsafe void* Get( Int32 offset )
        {
            Int32 offset1;
            if ( IsGlobalStruct( offset, out offset1 ) )
            {
                return ( Int32* ) GlobalStructAddr + offset1;
            }
            return ( Int32* ) GlobalsAddr + offset1;
        }

        public unsafe void Set( Int32 offset, Int32 value )
        {
            if ( offset < GlobalVariables.SizeInBytes >> 2 )
            {
                *( ( Int32* ) GlobalStructAddr + offset ) = value;
            }
            else
            {
                *( ( Int32* ) GlobalsAddr + offset - ( GlobalVariables.SizeInBytes >> 2 ) ) = value;
            }
        }

        public unsafe Int32 GetInt32( Int32 offset )
        {
            return *( ( Int32* ) Get( offset ) );
        }

        public ProgramDefinition CachedSearch( MemoryEdict ed, String field )
        {
            ProgramDefinition def = null;
            for ( var i = 0; i < GEFV_CACHESIZE; i++ )
            {
                if ( field == GefvCache[i].field )
                {
                    def = GefvCache[i].pcache;
                    return def;
                }
            }

            def = FindField( field );

            GefvCache[GefvPos].pcache = def;
            GefvCache[GefvPos].field = field;
            GefvPos ^= 1;

            return def;
        }

        private Int32 MakeStringID( Int32 index, Boolean isStatic )
        {
            return ( ( isStatic ? 0 : 1 ) << 24 ) + ( index & 0xFFFFFF );
        }

        public Int32 AllocString( )
        {
            var id = DynamicStrings.Count;
            DynamicStrings.Add( String.Empty );
            return MakeStringID( id, false );
        }

        public void SetString( Int32 id, String value )
        {
            if ( IsStaticString( id, out var offset ) )
                throw new ArgumentException( "Static strings are read-only!" );

            if ( offset < 0 || offset >= DynamicStrings.Count )
                throw new ArgumentException( "Invalid string id!" );

            DynamicStrings[offset] = value;
        }

        /// <summary>
        /// Like ED_NewString but returns string id (string_t)
        /// </summary>
        public Int32 NewString( String s )
        {
            var id = AllocString( );
            var sb = new StringBuilder( s.Length );
            var len = s.Length;
            for ( var i = 0; i < len; i++ )
            {
                if ( s[i] == '\\' && i < len - 1 )
                {
                    i++;
                    if ( s[i] == 'n' )
                        sb.Append( '\n' );
                    else
                        sb.Append( '\\' );
                }
                else
                    sb.Append( s[i] );
            }
            SetString( id, sb.ToString( ) );
            return id;
        }

        public Int32 StringOffset( String value )
        {
            var tmp = '\0' + value + '\0';
            var offset = Strings.IndexOf( tmp, StringComparison.Ordinal );

            if ( offset != -1 )
                return MakeStringID( offset + 1, true );

            for ( var i = 0; i < DynamicStrings.Count; i++ )
            {
                if ( DynamicStrings[i] == value )
                    return MakeStringID( i, false );
            }
            return -1;
        }

        /// <summary>
        /// ED_GlobalAtOfs
        /// </summary>
        public ProgramDefinition GlobalAtOfs( Int32 ofs )
        {
            for ( var i = 0; i < GlobalDefs.Length; i++ )
            {
                var def = GlobalDefs[i];
                if ( def.ofs == ofs )
                    return def;
            }
            return null;
        }

        public Int32 SetTempString( String value )
        {
            if ( TemporaryString == -1 )
                TemporaryString = NewString( value );
            else
                SetString( TemporaryString, value );

            return TemporaryString;
        }


        /// <summary>
        /// PR_ValueString
        /// </summary>
        public unsafe String ValueString( EdictType type, void* val )
        {
            String result;
            type &= ( EdictType ) ~ProgramDef.DEF_SAVEGLOBAL;

            switch ( type )
            {
                case EdictType.ev_string:
                    result = GetString( *( Int32* ) val );
                    break;

                case EdictType.ev_entity:
                    result = "entity " + _serverState.NumForEdict( _serverState.ProgToEdict( *( Int32* ) val ) );
                    break;

                case EdictType.ev_function:
                    var f = Functions[*( Int32* ) val];
                    result = GetString( f.s_name ) + "()";
                    break;

                case EdictType.ev_field:
                    var def = FindField( *( Int32* ) val );
                    result = "." + GetString( def.s_name );
                    break;

                case EdictType.ev_void:
                    result = "void";
                    break;

                case EdictType.ev_float:
                    result = ( *( Single* ) val ).ToString( "F1", CultureInfo.InvariantCulture.NumberFormat );
                    break;

                case EdictType.ev_vector:
                    result = String.Format( CultureInfo.InvariantCulture.NumberFormat,
                        "{0,5:F1} {1,5:F1} {2,5:F1}", ( ( Single* ) val )[0], ( ( Single* ) val )[1], ( ( Single* ) val )[2] );
                    break;

                case EdictType.ev_pointer:
                    result = "pointer";
                    break;

                default:
                    result = "bad type " + type.ToString( );
                    break;
            }

            return result;
        }

        /// <summary>
        /// PR_UglyValueString
        /// Returns a string describing *data in a type specific manner
        /// Easier to parse than PR_ValueString
        /// </summary>
        public unsafe String UglyValueString( EdictType type, EVal* val )
        {
            type &= ( EdictType ) ~ProgramDef.DEF_SAVEGLOBAL;
            String result;

            switch ( type )
            {
                case EdictType.ev_string:
                    result = GetString( val->_string );
                    break;

                case EdictType.ev_entity:
                    result = _serverState.NumForEdict( _serverState.ProgToEdict( val->edict ) ).ToString( );
                    break;

                case EdictType.ev_function:
                    var f = Functions[val->function];
                    result = GetString( f.s_name );
                    break;

                case EdictType.ev_field:
                    var def = FindField( val->_int );
                    result = GetString( def.s_name );
                    break;

                case EdictType.ev_void:
                    result = "void";
                    break;

                case EdictType.ev_float:
                    result = val->_float.ToString( "F6", CultureInfo.InvariantCulture.NumberFormat );
                    break;

                case EdictType.ev_vector:
                    result = String.Format( CultureInfo.InvariantCulture.NumberFormat,
                        "{0:F6} {1:F6} {2:F6}", val->vector[0], val->vector[1], val->vector[2] );
                    break;

                default:
                    result = "bad type " + type.ToString( );
                    break;
            }

            return result;
        }

        /// <summary>
        /// PR_GlobalString
        /// Returns a string with a description and the contents of a global,
        /// padded to 20 field width
        /// </summary>
        public unsafe String GlobalString( Int32 ofs )
        {
            var line = String.Empty;
            var val = Get( ofs );// (void*)&pr_globals[ofs];
            var def = GlobalAtOfs( ofs );
            if ( def == null )
                line = String.Format( "{0}(???)", ofs );
            else
            {
                var s = ValueString( ( EdictType ) def.type, val );
                line = String.Format( "{0}({1}){2} ", ofs, GetString( def.s_name ), s );
            }

            line = line.PadRight( 20 );

            return line;
        }

        /// <summary>
        /// PR_GlobalStringNoContents
        /// </summary>
        public String GlobalStringNoContents( Int32 ofs )
        {
            var line = String.Empty;
            var def = GlobalAtOfs( ofs );
            if ( def == null )
                line = String.Format( "{0}(???)", ofs );
            else
                line = String.Format( "{0}({1}) ", ofs, GetString( def.s_name ) );

            line = line.PadRight( 20 );

            return line;
        }

        /// <summary>
        /// ED_Print
        /// For debugging
        /// </summary>
        public unsafe void Print( MemoryEdict ed )
        {
            if ( ed.free )
            {
                _logger.Print( "FREE\n" );
                return;
            }

            _logger.Print( "\nEDICT {0}:\n", _serverState.NumForEdict( ed ) );

            for ( var i = 1; i < Data.numfielddefs; i++ )
            {
                var d = FieldDefs[i];
                var name = GetString( d.s_name );

                if ( name.Length > 2 && name[name.Length - 2] == '_' )
                    continue; // skip _x, _y, _z vars

                var type = d.type & ~ProgramDef.DEF_SAVEGLOBAL;
                Int32 offset;
                if ( ed.IsV( d.ofs, out offset ) )
                {
                    fixed ( void* ptr = &ed.v )
                    {
                        var v = ( Int32* ) ptr + offset;
                        if ( IsEmptyField( type, v ) )
                            continue;

                        _logger.Print( "{0,15} ", name );
                        _logger.Print( "{0}\n", ValueString( ( EdictType ) d.type, ( void* ) v ) );
                    }
                }
                else
                {
                    fixed ( void* ptr = ed.fields )
                    {
                        var v = ( Int32* ) ptr + offset;
                        if ( IsEmptyField( type, v ) )
                            continue;

                        _logger.Print( "{0,15} ", name );
                        _logger.Print( "{0}\n", ValueString( ( EdictType ) d.type, ( void* ) v ) );
                    }
                }
            }
        }

        public unsafe Boolean IsEmptyField( Int32 type, Int32* v )
        {
            for ( var j = 0; j < TYPE_SIZE[type]; j++ )
                if ( v[j] != 0 )
                    return false;

            return true;
        }

        /// <summary>
        /// ED_PrintNum
        /// </summary>
        public void PrintNum( Int32 ent )
        {
            Print( _serverState.EdictNum( ent ) );
        }
    }
}
