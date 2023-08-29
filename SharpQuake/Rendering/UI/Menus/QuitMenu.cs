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
using SharpQuake.Desktop;
using SharpQuake.Factories.Rendering.UI;
using SharpQuake.Framework;
using SharpQuake.Framework.IO.Input;
using SharpQuake.Sys;

namespace SharpQuake.Rendering.UI
{
    public class QuitMenu : BaseMenu
    {
        private BaseMenu _PrevMenu; // m_quit_prevstate;

        private readonly IEngine _engine;

        public QuitMenu( IEngine engine, IKeyboardInput keyboard, MenuFactory menus ) : base( "menu_quit", keyboard, menus )
        {
            _engine = engine;
        }

        public override void Show( )
        {
            if ( _menus.CurrentMenu == this )
                return;

            _keyboard.Destination = KeyDestination.key_menu;
            _PrevMenu = _menus.CurrentMenu;

            base.Show( );
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                case 'n':
                case 'N':
                    if ( _PrevMenu != null )
                        _PrevMenu.Show( );
                    else
                        _menus.CurrentMenu.Hide( );
                    break;

                case 'Y':
                case 'y':
                    _keyboard.Destination = KeyDestination.key_console;
                    _engine.Quit_f( null );
                    break;

                default:
                    break;
            }
        }

        public override void Draw( )
        {
            var textBoxWidth = 38 * _menus.UIScale;

            var width = _menus.Measure( "Quake is a trademark of Id Software, " ) / _menus.UIScale;
            width /= 2;

            width += 4;
            //width = (int)( width / 1.75 );

            var adorner = _menus.BuildAdorner( 21, 0, 0, padding: 0, width: width );

            var cW = _menus.CharacterAdvanceHeight( ) / 2;
            _menus.DrawTextBoxCalc( adorner.LeftPoint - 32 - cW, adorner.TopPoint - 32 - cW, adorner.Width / _menus.UIScale, adorner.TotalHeight + (cW * 2 ) + cW, scale: _menus.UIScale );

            adorner.PrintWhite( "Quake version 1.09 by id Software\n\n", Menus.TextAlignment.Left );

            adorner.PrintWhite( "Programming        Art \n", Menus.TextAlignment.Left );
            adorner.Print( " John Carmack       Adrian Carmack\n", Menus.TextAlignment.Left );
            adorner.Print( " Michael Abrash     Kevin Cloud\n", Menus.TextAlignment.Left );
            adorner.Print( " John Cash          Paul Steed\n", Menus.TextAlignment.Left );
            adorner.Print( " Dave 'Zoid' Kirsch\n", Menus.TextAlignment.Left );

            adorner.PrintWhite( "Design             Biz\n", Menus.TextAlignment.Left );
            adorner.Print( " John Romero        Jay Wilbur\n", Menus.TextAlignment.Left );
            adorner.Print( " Sandy Petersen     Mike Wilson\n", Menus.TextAlignment.Left );
            adorner.Print( " American McGee     Donna Jackson\n", Menus.TextAlignment.Left );
            adorner.Print( " Tim Willits        Todd Hollenshead\n", Menus.TextAlignment.Left );

            adorner.PrintWhite( "Support            Projects\n", Menus.TextAlignment.Left );
            adorner.Print( " Barrett Alexander  Shawn Green\n", Menus.TextAlignment.Left );

            adorner.PrintWhite( "Sound Effects\n", Menus.TextAlignment.Left );
            adorner.Print( " Trent Reznor and Nine Inch Nails\n\n", Menus.TextAlignment.Left );

            adorner.PrintWhite( "Quake is a trademark of Id Software,\n", Menus.TextAlignment.Left );
            adorner.PrintWhite( "inc., (c)1996 Id Software, inc. All\n", Menus.TextAlignment.Left );
            adorner.PrintWhite( "rights reserved. NIN logo is a\n", Menus.TextAlignment.Left );
            adorner.PrintWhite( "registered trademark licensed to\n", Menus.TextAlignment.Left );
            adorner.PrintWhite( "Nothing Interactive, Inc. All rights\n", Menus.TextAlignment.Left );
            adorner.PrintWhite( "reserved. Press y to exit\n", Menus.TextAlignment.Left );

            //_menus.PrintWhite( 16, 12, "  Quake version 1.09 by id Software\n\n" );
            //_menus.PrintWhite( 16, 28, "Programming        Art \n" );
            //_menus.Print( 16, 36, " John Carmack       Adrian Carmack\n" );
            //_menus.Print( 16, 44, " Michael Abrash     Kevin Cloud\n" );
            //_menus.Print( 16, 52, " John Cash          Paul Steed\n" );
            //_menus.Print( 16, 60, " Dave 'Zoid' Kirsch\n" );
            //_menus.PrintWhite( 16, 68, "Design             Biz\n" );
            //_menus.Print( 16, 76, " John Romero        Jay Wilbur\n" );
            //_menus.Print( 16, 84, " Sandy Petersen     Mike Wilson\n" );
            //_menus.Print( 16, 92, " American McGee     Donna Jackson\n" );
            //_menus.Print( 16, 100, " Tim Willits        Todd Hollenshead\n" );
            //_menus.PrintWhite( 16, 108, "Support            Projects\n" );
            //_menus.Print( 16, 116, " Barrett Alexander  Shawn Green\n" );
            //_menus.PrintWhite( 16, 124, "Sound Effects\n" );
            //_menus.Print( 16, 132, " Trent Reznor and Nine Inch Nails\n\n" );
            //_menus.PrintWhite( 16, 140, "Quake is a trademark of Id Software,\n" );
            //_menus.PrintWhite( 16, 148, "inc., (c)1996 Id Software, inc. All\n" );
            //_menus.PrintWhite( 16, 156, "rights reserved. NIN logo is a\n" );
            //_menus.PrintWhite( 16, 164, "registered trademark licensed to\n" );
            //_menus.PrintWhite( 16, 172, "Nothing Interactive, Inc. All rights\n" );
            //_menus.PrintWhite( 16, 180, "reserved. Press y to exit\n" );
        }
    }
}
