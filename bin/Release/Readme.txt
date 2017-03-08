Made with .net 4.0

Prebuilt binary is in bin/Release

Chipifier converts wav files to samples suitable for use in PCE chiptuning in Deflemask.

Make sure SoX is installed first (into %PATH% or into bin/Release)

Look in the bin/Release folder and drag any wave file onto chipify.bat

It will be downsampled (you must enter the new sample rate) and automatically split into Deflemask DMW wavetable files.

A text file will also be generated, with each waveform's decimal values logged to a separate line.
