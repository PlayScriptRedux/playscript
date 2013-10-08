// Copyright 2013 Zynga Inc.
//	
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//		
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.

using System;

#if PLATFORM_MONOMAC
using MonoMac.OpenGL;
#elif PLATFORM_MONOTOUCH
using OpenTK.Graphics.ES20;
#elif PLATFORM_MONODROID
using OpenTK.Graphics.ES20;
using ErrorCode = OpenTK.Graphics.ES20.All;
#endif

namespace flash.display3D {

	public class GLUtils {

		public static void CheckGLError()
		{
			#if PLATFORM_MONOTOUCH || PLATFORM_MONODROID
			ErrorCode error = GL.GetError ();
			if (error != 0) {
				System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();

				throw new InvalidOperationException("Error calling openGL api. Error: " + error + "\n" + trace.ToString());
			}
			#endif
		}

	}
}
