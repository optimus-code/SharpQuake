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

// From https://github.com/opentk/LearnOpenTK

using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SharpQuake.Renderer.OpenGL
{
	// A simple class meant to help create shaders.
	public class Shader
    {
        public readonly int Handle;

        private readonly Dictionary<string, int> _uniformLocations;


        // This is how you create a simple shader.
        // Shaders are written in GLSL, which is a language very similar to C in its semantics.
        // The GLSL source is compiled *at runtime*, so it can optimize itself for the graphics card it's currently being used on.
        // A commented example of GLSL can be found in shader.vert
        public Shader( string vertexShaderSource, string fragShaderSource )
        {
            // There are several different types of shaders, but the only two you need for basic rendering are the vertex and fragment shaders.
            // The vertex shader is responsible for moving around vertices, and uploading that data to the fragment shader.
            //   The vertex shader won't be too important here, but they'll be more important later.
            // The fragment shader is responsible for then converting the vertices to "fragments", which represent all the data OpenGL needs to draw a pixel.
            //   The fragment shader is what we'll be using the most here.

            // Load vertex shader and compile
            // LoadSource is a simple function that just loads all text from the file whose path is given.
            var shaderSource = vertexShaderSource;//LoadSource( vertPath );

            // GL.CreateShader will create an empty shader (obviously). The ShaderType enum denotes which type of shader will be created.
            var vertexShader = GL.CreateShader( ShaderType.VertexShader );

            // Now, bind the GLSL source code
            GL.ShaderSource( vertexShader, shaderSource );

            // And then compile
            CompileShader( vertexShader );


            // We do the same for the fragment shader
            shaderSource = fragShaderSource;//LoadSource( fragShaderSource );
            var fragmentShader = GL.CreateShader( ShaderType.FragmentShader );
            GL.ShaderSource( fragmentShader, shaderSource );
            CompileShader( fragmentShader );


            // These two shaders must then be merged into a shader program, which can then be used by OpenGL.
            // To do this, create a program...
            Handle = GL.CreateProgram( );

            // Attach both shaders...
            GL.AttachShader( Handle, vertexShader );
            GL.AttachShader( Handle, fragmentShader );

            // And then link them together.
            LinkProgram( Handle );

            // When the shader program is linked, it no longer needs the individual shaders attacked to it; the compiled code is copied into the shader program.
            // Detach them, and then delete them.
            GL.DetachShader( Handle, vertexShader );
            GL.DetachShader( Handle, fragmentShader );
            GL.DeleteShader( fragmentShader );
            GL.DeleteShader( vertexShader );

            // The shader is now ready to go, but first, we're going to cache all the shader uniform locations.
            // Querying this from the shader is very slow, so we do it once on initialization and reuse those values
            // later.

            // First, we have to get the number of active uniforms in the shader.
            GL.GetProgram( Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms );

            // Next, allocate the dictionary to hold the locations.
            _uniformLocations = new Dictionary<string, int>( );

            // Loop over all the uniforms,
            for ( var i = 0; i < numberOfUniforms; i++ )
            {
                // get the name of this uniform,
                var key = GL.GetActiveUniform( Handle, i, out _, out _ );

                // get the location,
                var location = GL.GetUniformLocation( Handle, key );

                // and then add it to the dictionary.
                _uniformLocations.Add( key, location );
            }
        }


        private static void CompileShader( int shader )
        {
            // Try to compile the shader
            GL.CompileShader( shader );

            // Check for compilation errors
            GL.GetShader( shader, ShaderParameter.CompileStatus, out var code );
            if ( code != ( int ) All.True )
            {
                // We can use `GL.GetShaderInfoLog(shader)` to get information about the error.
                throw new Exception( $"Error occurred whilst compiling Shader({shader})" );
            }
        }

        private static void LinkProgram( int program )
        {
            // We link the program
            GL.LinkProgram( program );

            // Check for linking errors
            GL.GetProgram( program, GetProgramParameterName.LinkStatus, out var code );
            if ( code != ( int ) All.True )
            {
                // We can use `GL.GetProgramInfoLog(program)` to get information about the error.
                throw new Exception( $"Error occurred whilst linking Program({program})" );
            }
        }


        // A wrapper function that enables the shader program.
        public void Use( )
        {
            GL.UseProgram( Handle );
        }


        // The shader sources provided with this project use hardcoded layout(location)-s. If you want to do it dynamically,
        // you can omit the layout(location=X) lines in the vertex shader, and use this in VertexAttribPointer instead of the hardcoded values.
        public int GetAttribLocation( string attribName )
        {
            return GL.GetAttribLocation( Handle, attribName );
        }

        public int GetUniformLocation( string name )
        {
            return GL.GetUniformLocation( Handle, name );
        }
        // Uniform setters
        // Uniforms are variables that can be set by user code, instead of reading them from the VBO.
        // You use VBOs for vertex-related data, and uniforms for almost everything else.

        // Setting a uniform is almost always the exact same, so I'll explain it here once, instead of in every method:
        //     1. Bind the program you want to set the uniform on
        //     2. Get a handle to the location of the uniform with GL.GetUniformLocation.
        //     3. Use the appropriate GL.Uniform* function to set the uniform.

        /// <summary>
        /// Set a uniform int on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetInt( string name, int data )
        {
            GL.UseProgram( Handle );
            GL.Uniform1( _uniformLocations[name], data );
        }

        /// <summary>
        /// Set a uniform float on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetFloat( string name, float data )
        {
            GL.UseProgram( Handle );
            GL.Uniform1( _uniformLocations[name], data );
        }

        /// <summary>
        /// Set a uniform Matrix4 on this shader
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        /// <remarks>
        ///   <para>
        ///   The matrix is transposed before being sent to the shader.
        ///   </para>
        /// </remarks>
        public void SetMatrix4( string name, Matrix4 data )
        {
            GL.UseProgram( Handle );
            GL.UniformMatrix4( _uniformLocations[name], true, ref data );
        }

        /// <summary>
        /// Set a uniform Vector3 on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetVector3( string name, Vector3 data )
        {
            GL.UseProgram( Handle );
            GL.Uniform3( _uniformLocations[name], data );
        }
    }
}