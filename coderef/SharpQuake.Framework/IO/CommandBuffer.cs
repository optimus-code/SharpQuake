﻿/// <copyright>
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
using System.Text;
using SharpQuake.Framework.Factories.IO;

namespace SharpQuake.Framework.IO
{
    //Any number of commands can be added in a frame, from several different sources.
    //Most commands come from either keybindings or console line input, but remote
    //servers can also send across commands and entire text files can be execed.

    //The + command line options are also added to the command buffer.

    //The game starts with a Cbuf_AddText ("exec quake.rc\n"); Cbuf_Execute ();

    public class CommandBuffer // Cbuf
    {
        private CommandFactory Commands
        {
            get;
            set;
        }

        private StringBuilder Buffer
        {
            get;
            set;
        }

        private Boolean Wait
        {
            get;
            set;
        }

        public CommandBuffer( CommandFactory commands )
        {
            Commands = commands;
            Buffer = new StringBuilder( 8192 );
        }

        // Cbuf_AddText()
        // as new commands are generated from the console or keybindings,
        // the text is added to the end of the command buffer.
        public void Append( String text )
        {
            if ( String.IsNullOrEmpty( text ) )
                return;
            
            if ( Buffer.Length + text.Length > Buffer.Capacity )
                ConsoleWrapper.Print( "Cbuf.AddText: overflow!\n" );
            else
                Buffer.Append( text );
        }

        // Cbuf_InsertText()
        // when a command wants to issue other commands immediately, the text is
        // inserted at the beginning of the buffer, before any remaining unexecuted
        // commands.
        // Adds command text immediately after the current command
        // ???Adds a \n to the text
        // FIXME: actually change the command buffer to do less copying
        public void Insert( String text )
        {
            Buffer.Insert( 0, text );
        }

        // Cbuf_Execute()
        // Pulls off \n terminated lines of text from the command buffer and sends
        // them through Cmd_ExecuteString.  Stops when the buffer is empty.
        // Normally called once per frame, but may be explicitly invoked.
        // Do not call inside a command function!
        public void Execute( )
        {
            while ( Buffer.Length > 0 )
            {
                var text = Buffer.ToString( );

                // find a \n or ; line break
                Int32 quotes = 0, i;
                for ( i = 0; i < text.Length; i++ )
                {
                    if ( text[i] == '"' )
                        quotes++;

                    if ( ( ( quotes & 1 ) == 0 ) && ( text[i] == ';' ) )
                        break;  // don't break if inside a quoted string

                    if ( text[i] == '\n' )
                        break;
                }

                var line = text.Substring( 0, i ).TrimEnd( '\n', ';' );

                // delete the text from the command buffer and move remaining commands down
                // this is necessary because commands (exec, alias) can insert data at the
                // beginning of the text buffer

                if ( i == Buffer.Length )
                    Buffer.Length = 0;
                else
                    Buffer.Remove( 0, i + 1 );

                // execute the command line
                if ( !String.IsNullOrEmpty( line ) )
                {
                    Commands.ExecuteString( line, CommandSource.Command );

                    if ( Wait )
                    {
                        // skip out while text still remains in buffer, leaving it
                        // for next frame
                        Wait = false;
                        break;
                    }
                }
            }
        }

        // Cmd_Wait_f
        // Causes execution of the remainder of the command buffer to be delayed until
        // next frame.  This allows commands like:
        // bind g "impulse 5 ; +attack ; wait ; -attack ; impulse 2"
        public void Wait_f( CommandMessage msg )
        {
            Wait = true;
        }
    }
}
