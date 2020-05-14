# FYM
Fast YM tools  

Tools for creating and replaying FYM files on 6502 computers.  
The provided player works on Apple II with a Mockingboard (2x AY-3-8910).  

FYM is a format based on the YM format by Arnaud Carré (Leonard). It gives a very good final size ratio without using any form of LZ* compression. The ratio is almost what you can get with the MYM format.

For a complete explanation of how the file format works, please read the associated article here:  
https://www.fenarinarsa.com/?p=1454   

**Twitter**  
https://twitter.com/fenarinarsa  

**Mastodon**  
https://shelter.moe/@fenarinarsa


# Requirements

Apple II with a Mockingboard  


# Contents

- player  
Contains the player in 6502 assembler code for Apple II.

- YM2FYM  
YM to FYM converter in C#


# Build instructions

You need the following tools:  
- acme cross-compiler  
- make (the GNU tool)  

## acme

https://sourceforge.net/projects/acme-crossass/
Add acme's path to the environment PATH variable. 

## make

The fastest way to install make on Windows is to install chocolatey:  
https://chocolatey.org/  
Then open a shell as administrator and type:  
`choco install make`

## Build

Please change the needed paths in the makefile. You also need to provide a ProDos DSK file.

To build the demo player, open a shell, go to the "player" folder and type:  
`make`

You will get a DSK file that works on any Apple II emulator.

