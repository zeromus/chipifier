rem cleanup from before
del %1.*.dmw
del %1.small.wav

rem decrease volume to a range [16,15] and resample while we're at it
sox -D -v 0.00048829615 %1 -r 1860 %1.small.wav

rem produces myfile.wav.small.wav.0.dmw, etc. with the chip samples you need
chipifier %1.small.wav