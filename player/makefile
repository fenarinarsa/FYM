DISKNAME = test.dsk

PYTHON = python.exe
ACME = acme.exe -f plain -o
LZ4 = lz4.exe
DIRECTWRITE = $(PYTHON) $(A2SDK)\bin\dw.py
TRANSAIR = $(PYTHON) $(A2SDK)\bin\transair.py
GENDSK = $(PYTHON) $(A2SDK)\bin\genDSK.py
APPLEWIN = $(APPLEWINPATH)\Applewin.exe
APPLECOMMANDER = C:\retrodev\bin\ac.jar
EMULATOR = $(APPLEWIN)

TUNE1_name = HAPPY
TUNE1 = tunes\DATA_happy.fym
TUNE2_name = DECADE
TUNE2 = tunes\DATA_decade.fym
TUNE3_name = LAP
TUNE3 = tunes\DATA_lap.fym
TUNE4_name = ROLLOUT
TUNE4 = tunes\DATA_rollout.fym

all: $(DISKNAME)

$(DISKNAME): main.b STARTUP.bas $(TUNE1) $(TUNE2) $(TUNE3) $(TUNE4)
	copy C:\jac\wudsn\Tools\template_prodos.dsk $(DISKNAME)
	java -jar $(APPLECOMMANDER) -bas $(DISKNAME) PLAY < STARTUP.bas
	java -jar $(APPLECOMMANDER) -p $(DISKNAME) $(TUNE1_name) BIN 0x2000 < $(TUNE1)
	java -jar $(APPLECOMMANDER) -p $(DISKNAME) $(TUNE2_name) BIN 0x2000 < $(TUNE2)
	java -jar $(APPLECOMMANDER) -p $(DISKNAME) $(TUNE3_name) BIN 0x2000 < $(TUNE3)
	java -jar $(APPLECOMMANDER) -p $(DISKNAME) $(TUNE4_name) BIN 0x2000 < $(TUNE4)
	java -jar $(APPLECOMMANDER) -p $(DISKNAME) FYM BIN 0x1000 < main.b
	copy lbl_main.txt $(APPLEWINPATH)\A2_USER1.SYM
#	$(TRANSAIR) $(DISKNAME)
	copy $(DISKNAME) $(APPLEWINPATH)\tmp.dsk
	$(EMULATOR) -d1 $(APPLEWINPATH)\tmp.dsk

main.b: main.a tools.a macros.a
	$(ACME) main.b main.a

clean:
	del *.b
	del lbl_*.txt

	