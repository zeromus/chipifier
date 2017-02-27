SET /P CHIPRATE=Enter new sample rate in Hz: 
IF %CHIPRATE% EQU 0 GOTO :END

SET /A UserInputVal="%CHIPRATE%"*1
IF %UserInputVal% EQU 0 GOTO END

del %1.*.dmw
del %1.CHIPIFIED.wav
del %1.CHIPIFIED.wav.txt

sox -D %1 -c 1 -r "%CHIPRATE%" "%~1.TMP1.wav"
sox -D "%~1.TMP1.wav" "%~1.TMP2.wav" norm
sox -D -v 0.00048829615 "%~1.TMP2.wav" "%~1.CHIPIFIED.wav"

chipifier "%~1.CHIPIFIED.wav"

del %1.TMP*.wav

:END
