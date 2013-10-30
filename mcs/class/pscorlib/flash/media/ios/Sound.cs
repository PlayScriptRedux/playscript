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

//#define DEBUG_TRACE

#if PLATFORM_MONOTOUCH

using System;
using System.Collections.Generic;
using System.Diagnostics;

using MonoTouch.AVFoundation;
using MonoTouch.Foundation;
using MonoTouch.CoreFoundation;

using flash.events;

namespace flash.media {

	// We use a partial C# class for platform specific logic
	partial class Sound {

		private const int ReportNumberOfPlayersAbove = 10;
		private const int MaxNumberOfPlayingSounds = 3;

		private static List<Sound> sPlayingSounds = new List<Sound>(ReportNumberOfPlayersAbove);

		private AVAudioPlayer mPlayer;
		private SoundChannel mChannel;
		private static int sNextId = 0;
		private int mId;

		private static List<Action> sAsyncEventDispatcher = new List<Action>();
		private static object sSync = new object();

		// Consider if these are accessed on one thread only or multiple threads
		private static Dictionary<string, NSData> sCachedSoundData = new Dictionary<string, NSData>();
		private static Dictionary<string, List<AVAudioPlayer>> sRecycledAudioPlayers = new Dictionary<string, List<AVAudioPlayer>>();

		public SoundChannel Channel
		{
			get { return mChannel; }
		}

		public static void WarmCache(string[] soundFiles)
		{
			foreach (string soundFile in soundFiles)
			{
				string key = soundFile.ToLowerInvariant();
				if (sCachedSoundData.ContainsKey(soundFile))
				{
					continue;
				}
				NSUrl nsUrl = NSUrl.FromFilename(soundFile);
				NSData soundData = NSData.FromUrl(nsUrl);
				sCachedSoundData.Add(soundFile, soundData);
			}
		}

		[Conditional("DEBUG_TRACE")]
		private static void GeneralDebugTrace(string text)
		{
			Console.WriteLine("[Sound] " + text);
		}

		[Conditional("DEBUG_TRACE")]
		private void DebugTrace(string text)
		{
			Console.WriteLine("[Sound - " + mId + "] " + text);
		}

		private void Init()
		{
			mId = sNextId++;
		}

		private List<AVAudioPlayer> GetOrCreateRecycledAudioPlayerList()
		{
			List<AVAudioPlayer> audioPlayers;
			if (sRecycledAudioPlayers.TryGetValue(_url, out audioPlayers) == false)
			{
				audioPlayers = new List<AVAudioPlayer>();
				sRecycledAudioPlayers.Add(_url, audioPlayers);
			}
			return audioPlayers;
		}

		private AVAudioPlayer GetOrCreateAudioPlayer()
		{
			AVAudioPlayer audioPlayer;

			// Let's see if we can use a recycled audio player
			List<AVAudioPlayer> audioPlayers = GetOrCreateRecycledAudioPlayerList();
			int count = audioPlayers.Count;
			if (count != 0)
			{
				int lastIndex = count - 1;
				audioPlayer = audioPlayers[lastIndex];
				audioPlayers.RemoveAt(lastIndex);
			}
			else
			{
				// If not let's create one (assuming we have the corresponding NSData)
				NSData soundData;
				if (sCachedSoundData.TryGetValue(_url, out soundData) == false)
				{
					// We could not use a recycled audio player, and the sound has never been loaded.
					// Let's load it now (synchronously for the moment)
					NSUrl nsUrl = NSUrl.FromFilename(_url);
					soundData = NSData.FromUrl(nsUrl);			// TODO: Synchronous load! Replace this to asynchronous!

					sCachedSoundData.Add(_url, soundData);
					if (soundData == null)
					{
						// We could not load the data, does it exist.
						// We still store it on the cache for next time. And return the error state.
						return null;
					}
				}

				audioPlayer = AVAudioPlayer.FromData(soundData);
				audioPlayer.PrepareToPlay();

				// Decode error event is only needed for the sound that created the player
				// as subsequent sound will not have a decode error
				audioPlayer.DecoderError += OnDecodeError;
			}

			// FinishedPlaying event is specific to this sound, so register the event there
			audioPlayer.FinishedPlaying += OnFinishedPlaying;
			return audioPlayer;
		}

		void OnDecodeError(object sender, AVErrorEventArgs args)
		{
			lock (sSync)
			{
				sAsyncEventDispatcher.Add( () => { dispatchEvent(new IOErrorEvent(IOErrorEvent.IO_ERROR)); });
			}
		}

		void OnFinishedPlaying(object sender, AVStatusEventArgs args)
		{
			lock (sSync)
			{
				sAsyncEventDispatcher.Add( () => { mChannel.dispatchEvent(new Event(Event.SOUND_COMPLETE)); });
			}

			// We can't dispose the sound within the callback as it breaks the app state (callback from another thread?)
			// This will aynway automatically be taken care of next time a sound is played, and this player will be recycled.
		}

		private void unload()
		{
			CheckInPlayingList(false);

			if (mPlayer != null)
			{
				mPlayer.FinishedPlaying -= OnFinishedPlaying;		// Remove the event for this sound, another sound will register the event for this player

				// For the moment, this sound does not need a player anymore (but another sound might need the same later, so we recycle it).
				GetOrCreateRecycledAudioPlayerList().Add(mPlayer);
				mPlayer = null;
			}
		}

		private static void RemoveUnusedAudioPlayers()
		{
			CheckValidity();

			// Remove all the sounds that are done playing
			int newCount = sPlayingSounds.Count;
			for (int i = newCount - 1 ; i  >= 0 ; --i)
			{
				Sound sound = sPlayingSounds[i];
				if (sound.mPlayer.Playing == false)
				{
					sound.DebugTrace("Remove finished sound.");

					// We can remove this player as it is not playing anymore
					// Swap it with last available sound (has already been parsed so we know we can keep it)
					if (i != newCount - 1)
					{
						sPlayingSounds[i] = sPlayingSounds[newCount - 1];
					}
					--newCount;
					sPlayingSounds.RemoveAt(newCount);

					sound.unload();
				}
				else
				{
					sound.DebugTrace("Still playing.");
				}
			}

			Debug.Assert(sPlayingSounds.Count == newCount);
			CheckValidity();
		}

		[Conditional("DEBUG")]
		private static void CheckValidity()
		{
			HashSet<int> ids = new HashSet<int>();
			for (int i = 0 ; i < sPlayingSounds.Count ; ++i)
			{
				bool added = ids.Add(sPlayingSounds[i].mId);
				Debug.Assert(added);
			}
		}

		[Conditional("DEBUG")]
		private void CheckInPlayingList(bool expectInPlayingList)
		{
			bool inPlayingList = false;
			for (int i = 0 ; i < sPlayingSounds.Count ; ++i)
			{
				if (sPlayingSounds[i].mId == mId)
				{
					inPlayingList = true;
					break;
				}
			}
			Debug.Assert(inPlayingList == expectInPlayingList);
		}

		private bool IsInPlayingList()
		{
			for (int i = 0 ; i < sPlayingSounds.Count ; ++i)
			{
				if (sPlayingSounds[i].mId == mId)
				{
					return true;
				}
			}
			return false;
		}

		private void internalLoad(String url)
		{
			_url = url;

			mPlayer = GetOrCreateAudioPlayer();
			if (null == mPlayer)
			{
				return;
			}

			mChannel = new SoundChannel();
			mChannel.Player = mPlayer;

			dispatchEvent(new Event(Event.COMPLETE));			// Send this when the sound has been loaded, however we should do this asynchrnously as much as possible...
		}

		private SoundChannel internalPlay(double startTime = 0, int loops = 0, SoundTransform sndTransform = null)
        {
			// Instead of doing this every frame, we only do it when we play a new sound (it is less often)
			// This still gives us opportunity to recycle audio players before a new one is needed
			RemoveUnusedAudioPlayers();

			if (null == mPlayer && _url != null)
				internalLoad (_url);
			if (null == mPlayer)
			{
				DebugTrace("Could not load sound " + (_url != null ? _url : "<unknown>"));
				return null;			// If we could not still load it, probably should add an error message
			}

			DebugTrace("Play sound " + _url);

			if (sPlayingSounds.Count >= MaxNumberOfPlayingSounds)
			{
				// Too many sounds, if that's mp3, we assume it is music, and in that case we don't skip this one...
				if (_url.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase) == false)
				{
					if (IsInPlayingList() == false)
					{
						DebugTrace("    Unloaded.");
						unload();		// We can safely unload it if it is not in the playlist already
					}
					DebugTrace("    Skipped.");
					return null;
				}
			}

			// We add it to the playing sounds only if it does not actually plays
			if (IsInPlayingList() == false)
			{
				// Not already in the list, add it and play it
				Debug.Assert(mPlayer.Playing == false);
				CheckInPlayingList(false);
				sPlayingSounds.Add(this);
				CheckInPlayingList(true);

				if (sPlayingSounds.Count >= ReportNumberOfPlayersAbove)
				{
					GeneralDebugTrace(sPlayingSounds.Count.ToString() + " sounds playing simultaneously.");
				}
			}

			mPlayer.NumberOfLoops = (loops > 0) ? loops : 0;

			if (mPlayer.Playing == false)
			{
				//DispatchQueue.DefaultGlobalQueue.DispatchAsync (() => {
				if (startTime > 0)
					mPlayer.PlayAtTime(startTime);
				else
					mPlayer.Play();
				//});
			}
			else
			{
				DebugTrace("    Was already playing.");
			}
	
			return mChannel;
        }

		public static void AsyncDispatchEvents()
		{
			int count = 0;
			Action[] actions = null;
			lock (sSync)
			{
				count = sAsyncEventDispatcher.Count;
				if (count != 0)
				{
					actions = sAsyncEventDispatcher.ToArray();
					sAsyncEventDispatcher.Clear();
				}
			}

			for (int i = 0 ; i < count ; ++i)
			{
				actions[i]();
			}
		}
	}

}

#endif
