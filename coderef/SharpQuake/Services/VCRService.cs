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

using SharpQuake.Framework.IO;
using SharpQuake.Framework;
using System.IO;
using System.Text;

using System;
using SharpQuake.Framework.Logging;

namespace SharpQuake.Services
{
    /// <summary>
    /// Service that deals with recording and playback of Quake VCR
    /// </summary>
    public class VCRService : IDisposable
    {
        private BinaryReader VcrReader
        {
            get;
            set;
        }

        private BinaryWriter VcrWriter
        {
            get;
            set;
        }

        public Stream ReadStream
        {
            get
            {
                return VcrReader?.BaseStream;
            }
        }

        public Stream WriteStream
        {
            get
            {
                return VcrWriter?.BaseStream;
            }
        }

        private readonly IConsoleLogger _logger;
        private readonly QuakeParameters _parameters;


        public VCRService( IConsoleLogger logger, QuakeParameters parameters )
        {
            _logger = logger;
            _parameters = parameters;
        }

        public void Initialise( )
        {
            if ( CommandLine.HasParam( "-playback" ) )
            {
                if ( CommandLine.Argc != 2 )
                    Utilities.Error( "No other parameters allowed with -playback\n" );

                Stream file = FileSystem.OpenRead( "quake.vcr" );
                if ( file == null )
                    Utilities.Error( "playback file not found\n" );

                VcrReader = new BinaryReader( file, Encoding.ASCII );
                var signature = VcrReader.ReadInt32( );  //Sys_FileRead(vcrFile, &i, sizeof(int));
                if ( signature != HostDef.VCR_SIGNATURE )
                    Utilities.Error( "Invalid signature in vcr file\n" );

                var argc = VcrReader.ReadInt32( ); // Sys_FileRead(vcrFile, &com_argc, sizeof(int));
                var argv = new String[argc + 1];
                argv[0] = _parameters.argv[0];

                for ( var i = 1; i < argv.Length; i++ )
                {
                    argv[i] = Utilities.ReadString( VcrReader );
                }
                CommandLine.Args = argv;
                _parameters.argv = argv;
            }

            var n = CommandLine.CheckParm( "-record" );
            if ( n != 0 )
            {
                Stream file = FileSystem.OpenWrite( "quake.vcr" ); // vcrFile = Sys_FileOpenWrite("quake.vcr");
                VcrWriter = new BinaryWriter( file, Encoding.ASCII );

                VcrWriter.Write( HostDef.VCR_SIGNATURE ); //  Sys_FileWrite(vcrFile, &i, sizeof(int));
                VcrWriter.Write( CommandLine.Argc - 1 );
                for ( var i = 1; i < CommandLine.Argc; i++ )
                {
                    if ( i == n )
                    {
                        Utilities.WriteString( VcrWriter, "-playback" );
                        continue;
                    }
                    Utilities.WriteString( VcrWriter, CommandLine.Argv( i ) );
                }
            }
        }

        /// <summary>
        /// Cleanup the VCR properties
        /// </summary>
        /// <remarks>
        /// (DI will handle this appropriately when the container
        /// is disposed.)
        /// </remarks>
        public void Dispose( )
        {
            if ( VcrWriter != null )
            {
                _logger.Print( "Closing vcrfile.\n" );
                VcrWriter.Close( );
                VcrWriter = null;
            }

            if ( VcrReader != null )
            {
                _logger.Print( "Closing vcrfile.\n" );
                VcrReader.Close( );
                VcrReader = null;
            }
        }

        public void Write( Int32 value )
        {
            VcrWriter.Write( value );
        }

        public void Write( Byte[] buffer )
        {
            VcrWriter.Write( buffer );
        }

        public void Write( Byte[] buffer, Int32 index, Int32 count )
        {
            VcrWriter.Write( buffer, index, count );
        }

        public Int32 ReadInt32()
        {
            return VcrReader.ReadInt32();
        }

        public void Read( Byte[] buffer, Int32 index, Int32 count )
        {
            VcrReader.Read( buffer, index, count );
        }
    }
}
