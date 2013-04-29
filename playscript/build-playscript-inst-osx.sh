PLAYSCRIPT_MONO=".."
PLAYSCRIPT_MONO_INST="../../playscript-mono-inst"
PLAYSCRIPT_OUT="../../playscript-osx"

echo "Building playscript OSX install folder at $PLAYSCRIPT_OUT"

if [ -d "$PLAYSCRIPT_OUT" ]; then
	rm -rf "$PLAYSCRIPT_OUT"
fi
mkdir "$PLAYSCRIPT_OUT"
cp -r "$PLAYSCRIPT_MONO/playscript/template_osx/" "$PLAYSCRIPT_OUT"
cp "$PLAYSCRIPT_MONO_INST/lib/mono/4.5/mcs.exe" "$PLAYSCRIPT_OUT/bin/playc.exe"
cp "$PLAYSCRIPT_MONO_INST/lib/mono/4.0/pscorlib.dll" "$PLAYSCRIPT_OUT/lib"
cp "$PLAYSCRIPT_MONO_INST/lib/mono/4.0/pscorlib_aot.dll" "$PLAYSCRIPT_OUT/lib"
cp "$PLAYSCRIPT_MONO_INST/lib/mono/4.0/PlayScript.Dynamic.dll" "$PLAYSCRIPT_OUT/lib"
cp "$PLAYSCRIPT_MONO_INST/lib/mono/4.0/PlayScript.Dynamic_aot.dll" "$PLAYSCRIPT_OUT/lib"
cp "$PLAYSCRIPT_MONO_INST/lib/mono/4.0/Mono.PlayScript.dll" "$PLAYSCRIPT_OUT/lib"
cp -r "$PLAYSCRIPT_MONO/mcs/class/pscorlib" "$PLAYSCRIPT_OUT/src"
cp -r "$PLAYSCRIPT_MONO/mcs/class/pscorlib_aot" "$PLAYSCRIPT_OUT/src"
cp -r "$PLAYSCRIPT_MONO/mcs/class/PlayScript.Dynamic" "$PLAYSCRIPT_OUT/src"
cp -r "$PLAYSCRIPT_MONO/mcs/class/PlayScript.Dynamic_aot" "$PLAYSCRIPT_OUT/src"
cp -r "$PLAYSCRIPT_MONO/mcs/class/Mono.PlayScript" "$PLAYSCRIPT_OUT/src"

echo "PlayScript OSX install folder complete."
