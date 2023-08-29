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

using SharpQuake.Sys;
using System;

namespace SharpQuake.Rendering.UI.Elements
{
    public class Crosshair : BaseUIElement
    {
        private Boolean ShowCrosshair
        {
            get
            {
                return Cvars.Crosshair.Get<Single>( ) > 0;
            }
        }

        private readonly Scr _screen;
        private readonly Drawer _drawer;

        public Crosshair( Scr screen, Drawer drawer )
        {
            _screen = screen;
            _drawer = drawer;
        }

        public override void Draw( )
        {
            base.Draw( );

            if ( !IsVisible || !HasInitialised )
                return;

            if ( ShowCrosshair )
                _drawer.DrawCharacter( _screen.VRect.x + _screen.VRect.width / 2, _screen.VRect.y + _screen.VRect.height / 2, '+' );
        }
    }
}
