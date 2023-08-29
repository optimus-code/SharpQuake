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

using SharpQuake.Framework.Factories.IO;
using SharpQuake.Framework.IO;
using System;
using System.IO;
using System.Text;

namespace SharpQuake.Services
{
    /// <summary>
    /// Functions for scanning and interacting with Save data
    /// </summary>
    public class SaveFileService
    {
        public const Int32 MAX_SAVEGAMES = 12;

        public (string Name, bool IsLoadable)[] Files
        {
            get;
            private set;
        } = new (string Name, bool IsLoadable)[MAX_SAVEGAMES];

        private readonly CommandFactory _commands;
        private readonly Scr _screen;

        public SaveFileService( CommandFactory commands, Scr screen )
        {
            _commands = commands;
            _screen = screen;
        }

        /// <summary>
		/// M_ScanSaves
		/// </summary>
		public void Update( )
        {
            for ( var i = 0; i < MAX_SAVEGAMES; i++ )
            {
                Files[i] = ("--- UNUSED SLOT ---", false);

                var name = String.Format( "{0}/s{1}.sav", FileSystem.GameDir, i );
                var fs = FileSystem.OpenRead( name );

                if ( fs == null )
                    continue;

                using ( var reader = new StreamReader( fs, Encoding.ASCII ) )
                {
                    var version = reader.ReadLine( );

                    if ( version == null )
                        continue;

                    var info = reader.ReadLine( );

                    if ( info == null )
                        continue;

                    info = info
                        .TrimEnd( '\0', '_' )
                        .Replace( '_', ' ' );

                    if ( !String.IsNullOrEmpty( info ) )
                        Files[i] = (info, true);
                }
            }
        }

        public void Load( Int32 index )
        {
            // Host_Loadgame_f can't bring up the loading plaque because too much
            // stack space has been used, so do it now
            _screen.BeginLoadingPlaque( );

            // issue the load command
            _commands.Buffer.Append( String.Format( "load s{0}\n", index ) );
        }

        public void Save( Int32 index )
        {
            _commands.Buffer.Append( String.Format( "save s{0}\n", index ) );
        }
    }
}
