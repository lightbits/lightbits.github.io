@echo off
echo %~n1

C:/Programming/miktex/miktex/bin/latex %1
C:/Programming/miktex/miktex/bin/dvipng -T tight %~n1.dvi
del %~n1.aux > NUL
del %~n1.log > NUL
del %~n1.dvi > NUL

