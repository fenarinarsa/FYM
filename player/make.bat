copy C:\jac\wudsn\Tools\template_prodos.dsk debug.dsk
java -jar C:\jac\wudsn\Tools\ac.jar -bas debug.dsk STARTUP < HELLO.bas
java -jar C:\jac\wudsn\Tools\ac.jar -p debug.dsk UNION BIN 0x2000 < DATA_union.fym
java -jar C:\jac\wudsn\Tools\ac.jar -p debug.dsk HAPPY BIN 0x2000 < DATA_happy.fym
java -jar C:\jac\wudsn\Tools\ac.jar -p debug.dsk DONT BIN 0x2000 < DATA_dont.fym
java -jar C:\jac\wudsn\Tools\ac.jar -p debug.dsk HAPPYW BIN 0x2000 < DATA_HappyW.fym
java -jar C:\jac\wudsn\Tools\ac.jar -dos debug.dsk FYM BIN < "%1"
C:\jac\wudsn\Tools\EMU\AppleWin\Applewin.exe -d1 debug.dsk
rem --- optional copy to floppy emu sd card
rem copy debug.dsk e:\ym.dsk
