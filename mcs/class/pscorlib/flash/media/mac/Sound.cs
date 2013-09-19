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
using System.Collections.Generic;
using flash.events;

#if PLATFORM_MONOMAC

namespace flash.media {

	// We use a partial C# class for platform specific logic
	partial class Sound {
			
		private SoundChannel channel = new SoundChannel();

		private void internalLoad(String url)
		{
			Console.WriteLine ("Sound internalLoad is not implemented for this platform");
		}

		private SoundChannel internalPlay(double startTime=0, int loops=0, SoundTransform sndTransform=null)
        {
			Console.WriteLine ("Sound internalPlay is not implemented for this platform");
			return channel;
        }

	}

}

#endif
