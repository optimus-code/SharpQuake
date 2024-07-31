/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
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

namespace SharpQuake.Framework.Logging
{
    /// <summary>
    /// Interface for core logging component
    /// </summary>
    public interface IConsoleLogger
    {
        /// <summary>
        /// Is the console forced open?
        /// </summary>
        /// <remarks>
        /// (Due to errors etc.)
        /// </remarks>
        Boolean IsForcedUp
        {
            get;
        }

        /// <summary>
        /// Callback for handling the visual aspect of console printing
        /// </summary>
        Action<String> OnPrint
        {
            get;
            set;
        }

        /// <summary>
        /// Print a message to the console
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void Print( String format, params Object[] args );

        /// <summary>
        /// Con_DPrintf
        /// </summary>
        /// <remarks>
        /// A Con_Printf that only shows up if the "developer" cvar is set
        /// </remarks>
        /// <param name="fmt"></param>
        /// <param name="args"></param>
        void DPrint( String format, params Object[] args );

        void Dispose( );
    }
}
