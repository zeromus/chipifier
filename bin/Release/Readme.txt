Made with .net 4.0

Chipifier converts WAV files to wavetable files suitable for use in Deflemask for PC-Engine chiptuning.

Make sure SoX.exe and all of its libraries are installed into %PATH% or into Chipify root folder.

Also make sure the WAV file you're inputting into Chipify is in Chipify root folder.

Drag any standard WAV file onto Chipify.bat and enter the new sample rate* (in Hz) you want to resample to.

It will be downsampled and automatically split into Deflemask compatible DMW wavetable files.

A text file will also be generated with each waveform's decimal values logged to a separate line.

A WAV file named filename.wav.CHIPIFIED.wav will also be generated.  This is the processed/downsampled WAV before being split.


* To determine the sample rate you need to resample the input WAV to, if you're ripping individual waveforms, you will first need to determine how long (how many samples) one exact loop for that waveform is in the original WAV file.  This can be done in most decent WAV editors by simply zooming way in and selecting the waveform you want.  You will also need to know the sample rate in Hz of the original WAV file.  Once you have those two numbers, you would use a simple algebraic equation of ((32 / N) * Y), where N = the number of samples in the exact loop, and Y = the sample rate of the entire input file (in Hz).  The answer to that equation will give you the sample rate that needs to be input to Chipify.
