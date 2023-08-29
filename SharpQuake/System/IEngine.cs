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
using System;

namespace SharpQuake.Sys
{
    /// <summary>
    /// Interface for core engine class
    /// </summary>
    public interface IEngine
    {
        Boolean IsDeveloper
        {
            get;
        }

        Boolean IsDedicated
        {
            get;
        }

        Boolean IsDisposing
        {
            get;
        }

        Boolean NoClipAngleHack
        {
            get;
            set;
        }

        SystemInformation System
        {
            get;
        }

        /// <summary>
        /// Returns the dynamic mode of the engine.
        /// </summary>
        /// <remarks>
        /// (E.g. Whether it's a dedicated server, listen server or client.)
        /// </remarks>
        EngineMode Mode
        {
            get;
        }

        GameKind Game
        {
            get;
        }

        public TService Get<TService>( ) where TService : class;
        public object Get( Type type );

        /// <summary>
        /// host_Error
        /// This shuts down both the client and server
        /// </summary>
        void Error( String error, params Object[] args );

        /// <summary>
        /// host_EndGame
        /// </summary>
        void EndGame( String message, params Object[] args );

        /// <summary>
        /// Host_ShutdownServer
        /// This only happens at the end of a game, not between levels
        /// </summary>
        void ShutdownServer( Boolean crash );
        void Quit( );
        void Quit_f( CommandMessage msg );
        void Dispose( );
    }
}
