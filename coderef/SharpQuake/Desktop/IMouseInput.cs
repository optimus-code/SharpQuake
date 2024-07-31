﻿/// <copyright>
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
using SharpQuake.Framework.Mathematics;
using System;

namespace SharpQuake.Desktop
{
    /// <summary>
    /// Generic interface for mouse input
    /// </summary>
    public interface IMouseInput
    {
        Func<Boolean> OnCheckMouseActive
        {
            get;
            set;
        }

        /// <summary>
        /// Stores mouse sensitivity
        /// </summary>
        Single Sensitivity
        {
            get;
        }

        /// <summary>
        /// Event used to subscribe to mouse movements
        /// </summary>
        Action<usercmd_t> OnMouseMove
        {
            get;
            set;
        }

        Boolean IsActive
        {
            get;
        }

        Vector2 Mouse // mouse_x, mouse_y
        {
            get;
        }
        void Initialise( );
        void Dispose( );
        void MouseEvent( Int32 mstate );
        void ShowMouse( );
        void HideMouse( );
        void ActivateMouse( );
        void DeactivateMouse( );
        void ClearStates( );
        void Move( usercmd_t cmd );

        [Obsolete( "Need to add Game Pad support")]
        void Commands( );
    }
}
