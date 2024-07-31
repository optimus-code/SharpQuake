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

using SharpQuake.Desktop;
using SharpQuake.Factories.Rendering.UI;
using SharpQuake.Framework;
using SharpQuake.Framework.Factories.IO;
using SharpQuake.Framework.IO.Input;
using SharpQuake.Game.Client;
using SharpQuake.Networking.Client;
using SharpQuake.Rendering.UI.Menus;
using SharpQuake.Sys;
using System;

namespace SharpQuake.Rendering.UI
{
	/// <summary>
	/// MainMenu
	/// </summary>
	public class MainMenu : BaseMenu
	{
		private const Int32 MAIN_ITEMS = 5;
		private Int32 _SaveDemoNum;

		private readonly ClientState _clientState;
        private readonly ClientVariableFactory _cvars;
        private readonly snd _sound;
        private readonly PictureFactory _pictures;
		private readonly client _client;

        public MainMenu( IKeyboardInput keyboard, MenuFactory menus, ClientState clientState,
			snd sound, PictureFactory pictures, client client, ClientVariableFactory cvars ) : base( "menu_main", keyboard, menus )
		{
			_clientState = clientState;
			_sound = sound;
			_pictures = pictures;
			_client = client;
			_cvars = cvars;
            Cvars.NewUI = _cvars.Add( "r_newui", true );
        }

		public override void Show( )
		{
            if ( _keyboard.Destination != KeyDestination.key_menu )
			{
				_SaveDemoNum = _clientState.StaticData.demonum;
                _clientState.StaticData.demonum = -1;
			}

			base.Show( );
		}

		/// <summary>
		/// M_Main_Key
		/// </summary>
		public override void KeyEvent( Int32 key )
		{
			switch ( key )
			{
				case KeysDef.K_ESCAPE:
					//Host.Keyboard.Destination = keydest_t.key_game;
					_menus.CurrentMenu.Hide();
                    _clientState.StaticData.demonum = _SaveDemoNum;

					if ( _clientState.StaticData.demonum != -1 && !_clientState.StaticData.demoplayback && _clientState.StaticData.state != cactive_t.ca_connected )
						_client.NextDemo();
					break;

				case KeysDef.K_DOWNARROW:
					_sound.LocalSound( "misc/menu1.wav" );
					if ( ++Cursor >= MAIN_ITEMS )
						Cursor = 0;
					break;

				case KeysDef.K_UPARROW:
					_sound.LocalSound( "misc/menu1.wav" );
					if ( --Cursor < 0 )
						Cursor = MAIN_ITEMS - 1;
					break;

				case KeysDef.K_ENTER:
					_menus.EnterSound = true;

					switch ( Cursor )
					{
						case 0:
                            _menus.Show( "menu_singleplayer" );
							break;

						case 1:
                            _menus.Show( "menu_multiplayer" );
							break;

						case 2:
                            _menus.Show( "menu_options" );
							break;

						case 3:
                            _menus.Show( "menu_help" );
							break;

						case 4:
                            _menus.Show( "menu_quit" );
							break;
					}
					break;
			}
		}

        private void DrawPlaque( MenuAdorner adorner )
        {
            var scale = _menus.UIScale;

            //_menus.DrawTransPic( 16 * scale, 4 * scale, _pictures.Cache( "gfx/qplaque.lmp", "GL_NEAREST" ), scale: scale );
            var logoPic = _pictures.Cache( "gfx/qplaque.lmp", "GL_NEAREST" );
            _menus.DrawTransPic( adorner.LeftPoint - ( logoPic.Width * _menus.UIScale ), adorner.MidPointY - ( ( logoPic.Height * scale ) / 2 ), logoPic, scale: scale );

            var p = _pictures.Cache( "gfx/ttl_main.lmp", "GL_NEAREST" );
            _menus.DrawPic( adorner.MidPointX - ( ( p.Width * scale ) / 2 ), 4 * scale, p, scale: scale );
        }

		static String[] OPTIONS = new [] 
		{
            "Singleplayer",
            "Multiplayer",
            "Options",
            "Help/Ordering",
            "Quit"
        };

        public override void Draw( )
		{
			var scale = _menus.UIScale;
            var adorner = _menus.BuildAdorner( OPTIONS.Length, 0, 0, width: 152, isBigFont: true );

			DrawPlaque( adorner );

			//_menus.DrawTransPic( 16 * scale, 4 * scale, _pictures.Cache( "gfx/qplaque.lmp", "GL_NEAREST" ), scale: scale );
			//var p = _pictures.Cache( "gfx/ttl_main.lmp", "GL_NEAREST" );
			//_menus.DrawPic( ( ( 320 * scale ) - ( p.Width * scale ) ) / 2, 4 * scale, p, scale: scale );
			//_menus.DrawTransPic( 72 * scale, 32 * scale, _pictures.Cache( "gfx/mainmenu.lmp", "GL_NEAREST" ), scale: scale );

			if ( Cvars.NewUI.Get<Boolean>( ) )
			{
				for ( var i = 0; i < OPTIONS.Length; i++ )
				{
					var text = OPTIONS[i];

					if ( Cursor == i )
                        adorner.PrintWhite( text, TextAlignment.Centre );
					else
						adorner.Print( text, TextAlignment.Centre );
                }
            }
			else
            {
                var options = _pictures.Cache( "gfx/mainmenu.lmp", "GL_NEAREST" );
                var optionsY = adorner.MidPointY - ( ( options.Height * scale ) / 2 );
                var optionsX = adorner.MidPointX - ( ( options.Width * scale ) / 2 );

                _menus.DrawTransPic( optionsX, optionsY, options, scale: scale );

                var f = ( Int32 ) ( Time._Time * 10 ) % 6;
                //_menus.DrawTransPic( 54 * scale, ( 32 + Cursor * 20 ) * scale, _pictures.Cache( String.Format( "gfx/menudot{0}.lmp", f + 1 ), "GL_NEAREST" ), scale: scale );

                var cursorPic = _pictures.Cache( String.Format( "gfx/menudot{0}.lmp", f + 1 ), "GL_NEAREST" );

                _menus.DrawTransPic( adorner.LeftPoint + ( ( cursorPic.Width * scale ) / 2 ), optionsY + ( 20 * Cursor * scale ), cursorPic, scale: scale );
            }

        }
    }
}
