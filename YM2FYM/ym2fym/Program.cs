// ym2fym
// by fenarinarsa 2019-2020
//
// This file is part of FYM.
//
// FYM is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// FYM is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with FYM.  If not, see<https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;

namespace ym2fym
{
    class Program
    {
        // This converter takes an UNCOMPRESSED .ym file
        // and outputs a .fym to be player on Apple II (Mockingboard) with the player provided in Latecomer or MockingboardYM (2019).
        // It supports only YM3, YM5 and YM6 and only 3 channels.
        // Don't forget to unpack the YM file before feeding it to YMOptimize (easily done by adding .lha to the file and opening it with an archiver)
        //
        //
        // ------------- What is the FYM format? -------------
        //
        // It's a file format I created as a test to see if it would make files smaller than MYM.
        // It seems to be the case, and it's using RLE compression only.
        //
        // The FYM file format consists of a "partition" that contains indexes to individual register patterns.
        // 1 byte = pattern_length
        //    followed by the "partition"
        // For each pattern:
        // 14 bytes = HIGH BYTE of offset to the 14 patterns for AY register 0, register 1, register 2...
        // 14 bytes = LOW BYTE of offset to the 14 patterns for AY register 0, register 1, register 2...
        // (n times)
        // 
        // 1 byte = 0x00 (end of partition)
        // n bytes = filler to the next 256b page
        // n bytes = register patterns
        // <end of file>
        //
        // Register patterns are compressed in RLE
        // They NEVER overlap from one 256b page to another.
        //
        // How to play that:
        // First the player has to relocate the file, meaning if the file is loaded at 0x2000 for instance,
        //  it needs to add 0x20 to every HIGH BYTE of the offsets
        //
        // Then it starts by getting the 14 first offsets to get the addresses of the 14 patterns, each for 1 AY register
        // And starts unpacking the RLE for each register.
        // All the patterns end after <patterns_length> unpacked bytes (first byte in file)
        // Then it's back to the offsets to find the following 14 patterns.
        //
        // The file can be optimized quite easily, some ideas:
        // - Put the patterns before the partition to avoid putting a filler between both.
        // - Not recording every register, some tunes don't use every registers.
        //
        // Also, specifically for the demo "Latecomer", I edited the partition to cut some parts of the song.
        // but it didn't make it smaller, it's just that the partition is smaller and the filler is bigger. This could be done better.
        // I didn't have time to make a better version at the party.
        //
        // ------------- How does work the converter ? ---------------
        // It uses a bruteforce analysis.
        // It tries all combinations of <pattern_length> from 16 to 128 and estimates the final FYM filesize.
        // It's the fastest way because smaller patterns make the partition bigger, of course.
        // It turned out most of times the converter finds either pattern_length = original partition pattern length
        // or pattern_length = half the original partition pattern length
        //
        // In addition pattern_length always ends as binary value, for instance 16,32,48,64,96...
        // Which seems right since the original trackers often use that kind of values for their pattern lengths.
        //
        // In the case of latecomer the detected pattern length is 96 and fits perfectly 4 beats of the song, which allowed me to edit it very easily.
        //

        static void Main(string[] args)
        {
            
            // uncompressed YM file
            //String inputfile = @"S:\Emulateurs\Atari ST\stsound\YM\Lary.Emmanuel (Lap)\NextCharts\LAP22_33_upk.YM";
            //String inputfile = @"S:\Emulateurs\Atari ST\stsound\YM\Hippel.Jochen (Mad Max)\Union Demo\Alloy Run_upk.ym";

            String inputfile = @"S:\Emulateurs\Atari ST\stsound\YM\Hippel.Jochen (Mad Max)\Rollout\ROLLOUT2_upk.YM";
            //String inputfile = @"S:\Emulateurs\Atari ST\stsound\YM\Hippel.Jochen (Mad Max)\Ooh Crickey\Crickey Loader_upk.ym";
            //String inputfile = @"S:\Emulateurs\Atari ST\stsound\YM\Seemann.Frank (Tao)\Just Buggin (Normal)\Just feel it1_upk.ym";
            //String inputfile = @"S:\Emulateurs\Apple II\dev\Perso\MockinboardYM\main\happy.ym";
            String outputfile = @"S:\Emulateurs\Apple II\dev\Perso\MockinboardYM\main\DATA_rollout.fym";

            FileInfo fi = new FileInfo(inputfile);
            String fname = fi.Name;
            MD5 md5hash = MD5.Create();

            Byte[] ym = File.ReadAllBytes(inputfile);
            int nb_frames = 0;
            bool interleaved = true;
            int ymsize = 14;

            // ZX Spectrum: 1773.4f
     //        Atari ST: 2000.0f
            Double inputAYFrequency = 2000.0f;
            // CPC: 1000.0f
            // Apple II NTSC: 1023.0f
            // Apple II PAL: 1017.0f
            Double outputAYfrequency = 1017.0f;

            // Detecting YM format

            String format = Char.ConvertFromUtf32(ym[0]) + Char.ConvertFromUtf32(ym[1]) + Char.ConvertFromUtf32(ym[2]) + Char.ConvertFromUtf32(ym[3]);
            Console.WriteLine("{1} ({0})", format, fname);

            int start = 34;

            if (format == "YM6!" || format == "YM5!")
            {
                nb_frames = (ym[12] << 24) + (ym[13] << 16) + (ym[14] << 8) + ym[15];
                for (int i = 0; i < 3; i++) { while (ym[start] != 0) start++; start++; }
                ymsize = 16;
            }
            if (format == "YM5!")
            {
                ymsize = 14;
            }
            if (format == "YM3!")
            {
                interleaved = true;
                nb_frames = (ym.Length - 4) / 14;
                start = 4;
            }

            Console.WriteLine("Song length: {0} frames", nb_frames);

            Console.WriteLine("Start of song: byte #{0}", start);

            int seqsize = 24; // nb of frames for each sequence
            int bestsize = ym.Length;

           // FileStream fs = new FileStream("DATA_rle", FileMode.Create);

            // Reading all register values into an array
            byte[,] buf = new byte[16, nb_frames];
            int offset = 0x2000;
            for (int register = 0; register < 16; register++)
            {
                if (interleaved)
                {
                    for (int off = 0; off < nb_frames; off++) buf[register, off] = ym[start + (register * nb_frames) + off];
                } else {
                    for (int off = 0; off < nb_frames; off++) buf[register, off] = ym[start + off * ymsize + register];
                }
            }

            // YM/AY registers 
            //      7 6 5 4 3 2 1 0
            //  r0: X X X X X X X X Period voice A
            //  r1: - - - - X X X X " " "
            //  r2: X X X X X X X X Period voice B
            //  r3: - - - - X X X X " " "
            //  r4: X X X X X X X X Period voice C
            //  r5: - - - - X X X X " " "
            //  r6: - - - X X X X X Noise period
            //  r7: X X X X X X X X Mixer control
            //  r8: - - - X X X X X Volume voice A
            //  r9: - - - X X X X X Volume voice B
            // r10: - - - X X X X X Volume voice C
            // r11: X X X X X X X X Waveform period
            // r12: X X X X X X X X " "
            // r13: - - - - X X X X Waveform shape

            // YM4 special registers 
            //      7 6 5 4 3 2 1 0
            //  r1: - - X X - - - - Timer-Synth enable voice number
            //  r3: - - X X - - - - Digi-drum enable voice number
            //  r5: X X X X - - - - Vmax volume for Timer-Synth
            //  r6: X X X - - - - - Timer Predivisor for Timer-Synth
            //  r8: X X X - - - - - Timer Predivisor for Digi-drum
            // r14: X X X X X X X X Timer Count for Timer-Synth
            // r15: X X X X X X X X Timer Count for Digi-drum

            // YM6 special registers 
            //      7 6 5 4 3 2 1 0
            //  r1: T T V V - - - - V = Special Effect 1 Voice / T = Special Effect 1 Type
            //  r3: T T V V - - - - V = Special Effect 2 Voice / T = Special Effect 2 Type
            //  r5: X X X X - - - - Vmax volume for Timer-Synth
            //  r6: X X X - - - - - Timer Predivisor for Special Effect 1
            //  r8: X X X - - - - - Timer Predivisor for Special Effect 2 (if no digidrum)
            // if digi-drums :
            //  r8: - - X X X X X X Digidrum sample number voice A 
            //  r9: - - X X X X X X Digidrum sample number voice B
            // r10: - - X X X X X X Digidrum sample number voice C
            // if sync buzzer :
            //  r8: - - - - X X X X Sync buzzer env shape voice A 
            //  r9: - - - - X X X X Sync buzzer env shape voice B 
            // r10: - - - - X X X X Sync buzzer env shape voice C 
            // r14: X X X X X X X X Timer Count for Special Effect 1
            // r15: X X X X X X X X Timer Count for Special Effect 2
            //
            // Special Effect Type : 00 SID Voice, 01 Digidrum, 10 Sinus SID (TAO), 11 Sync buzzer

            // Changing the AY tones to fit the MB frequency
            // ST is 2Mhz, PAL MB is 1,017Mhz, NTSC MB is 1,023Mhz
            double tone;
            
            for (int i = 0; i < nb_frames; i++)
            {
               // if (buf[13, i] != 255) {
                //    Console.WriteLine("{0:X} {1:X} {2:X} {3:X}", buf[0, i], buf[1, i], buf[11, i], buf[12, i]);
               // }
                
                for (int register = 0; register < 6; register += 2)
                {
                    tone = Math.Round((buf[register + 1, i] * 256 + buf[register, i]) * (outputAYfrequency / inputAYFrequency));
                    buf[register + 1, i] = (byte)(int)(tone / 256);
                    buf[register, i] = (byte)(int)(tone % 256);
                }
                tone = Math.Round(buf[6, i] * (outputAYfrequency / inputAYFrequency));
                buf[6, i] = (byte)(int)tone;

                tone = Math.Round((buf[12, i] * 256 + buf[11, i]) * (outputAYfrequency / inputAYFrequency));
                buf[12, i] = (byte)(int)(tone / 256);
                buf[11, i] = (byte)(int)(tone % 256);


                if (buf[14, i] != 0 || buf[15, i] != 0 || (buf[1,i] & 0xF0) !=0 || (buf[3, i] & 0xF0) != 0 || (buf[5, i] & 0xF0) != 0)
                    Console.WriteLine("Warning SPECIAL EFFECT on frame {0}", i);
            }


            // (TEMP YM for testing)
            // Writing back register values into the original array
            for (int register = 0; register < 16; register++) {
                if (interleaved) {
                    for (int off = 0; off < nb_frames; off++) ym[start + (register * nb_frames) + off] = buf[register, off];
                } else {
                    for (int off = 0; off < nb_frames; off++) ym[start + off * ymsize + register] = buf[register, off];
                }
            }
            // 1000000 = 0x000F4240   2000000 = 0x001E8480
            // 1023000 = 0x000F9C18
            ym[22] = 0x00;
            ym[23] = 0x0F;
            ym[24] = 0x42;
            ym[25] = 0x40;

            ym[22] = 0x00;
            ym[23] = 0x0F;
            ym[24] = 0x9c;
            ym[25] = 0x18;

            ym[22] = 0x00;
            ym[23] = 0x0F;
            ym[24] = 0x84;
            ym[25] = 0xA8;

            File.WriteAllBytes(inputfile + ".TESTING.ym", ym);


            // --------------------------------------------------
            // partitioned RLE version

            Dictionary<String, byte[]> sequences = new Dictionary<string, byte[]>();
            Dictionary<String, String> reftopacked = new Dictionary<string, string>();

            // Looking for the best sequence size (brute force test)
            String[][] partition = new String[14][];
            int final_seqsize = 0;
            for (seqsize = 16; seqsize < 128; seqsize++)
            {

                int nb_seq = nb_frames / seqsize;
                int total_seq = 0;

                byte[] buffer = new byte[seqsize];

                // To deduplicate patterns a string hexa representation is used as key
                Dictionary<String, byte[]> seqtest = new Dictionary<string, byte[]>();

                String hash;

                List<String>[] parttest = new List<String>[14];

                for (int register = 0; register < 14; register++)

                {
                    parttest[register] = new List<String>();
                    // Testing sequence for that register
                    for (int seq = 0; seq < nb_seq; seq++)
                    {
                        total_seq++;

                        for (int off = 0; off < seqsize; off++) buffer[off] = buf[register, seq * seqsize + off];


                        StringBuilder sBuilder = new StringBuilder();
                        for (int i = 0; i < buffer.Length; i++) { sBuilder.Append(buffer[i].ToString("x2")); }
                        hash = sBuilder.ToString();
                        parttest[register].Add(hash); // populate partition

                        if (!seqtest.ContainsKey(hash))
                        {
                            byte[] tmp = new byte[seqsize];
                            Array.Copy(buffer, tmp, seqsize);
                            seqtest.Add(hash, tmp);
                        }
                    }
                }

                int sequcount = seqtest.Count();
                // Final size is the size of all unique sequences + 2*number of sequences for the partition pointers + sequence size (1 byte) + end pointer mark (0x00, 1 byte)
                int est_size = sequcount * seqsize + total_seq * 2 + 2;
                int packed_est_size = total_seq * 2 + 2;
                foreach (String s in seqtest.Keys)
                {
                    byte[] packbits = new byte[seqsize + 1];
                    int max = Packbits.packbits(seqtest[s], packbits);
                    packed_est_size += max;
                }

                if (packed_est_size < bestsize)
                {
                    // This version is smaller than all the previous ones

                    // save partition
                    for (int i = 0; i < 14; i++) partition[i] = parttest[i].ToArray<String>();
                    bestsize = packed_est_size;
                    reftopacked = new Dictionary<string, string>();
                    sequences = new Dictionary<string, byte[]>();
                    final_seqsize = seqsize;
                    //int packed_est_size = total_seq * 2;

                    foreach (String s in seqtest.Keys)
                    {
                        //Console.WriteLine(s);
                        byte[] packbits = new byte[seqsize + 1];
                        int max = Packbits.packbits(seqtest[s], packbits);
                        //packbits[max] = 0x80; // unused since the end of RLE is detected through the pattern size variable at the start of file
                        //max++;
                        StringBuilder sBuilder = new StringBuilder();
                        for (int i = 0; i < max; i++) { sBuilder.Append(packbits[i].ToString("x2")); }
                        String spacked = sBuilder.ToString();

                        // save sequence
                        byte[] packed = new byte[max];
                        Array.Copy(packbits, packed, max);
                        sequences.Add(spacked, packed);
                        reftopacked.Add(s, spacked);

                        //packed_est_size += max;
                        Console.WriteLine("{0} (packed)", spacked);
                    }
                    Console.WriteLine("File: {0} ({2})  Tested sequence size: {1} bytes", fname, seqsize, format);
                    Console.WriteLine("Total sequences: {0}   Unique sequences: {1} ", total_seq, sequcount);
                    Console.WriteLine("Estimated size: {0} bytes ({1}% of original size)", est_size, (est_size * 100) / ym.Length);
                    Console.WriteLine("Estimated packed size: {0} bytes ({1}% of original size)", packed_est_size, (packed_est_size * 100) / ym.Length);
                }
            }
            fini:
            // making the final partition
            List<String> sorted_packed_sequences = new List<string>();

            // we now try to pack all the sequences in 256-bytes pages without overlapping, that is 512 characters string
            // this is a bin packing problem, I use a crude algorithm so not optimal but easy to implement and gives very good results.

            // get the sequences in reverse order of their length (bigger>smaller)
            foreach (String s in sequences.Keys.OrderBy(str => str.Length))
            {
                sorted_packed_sequences.Insert(0, s);

            }
            
            List<String> pages = new List<String>();
            List<byte[]> pages_byte = new List<byte[]>();
            bool ok = false;
            Dictionary<String, int> seqlocations_L = new Dictionary<string, int>();
            Dictionary<String, int> seqlocations_H = new Dictionary<string, int>();
            int seqnum = 0;
            foreach (String seq in sorted_packed_sequences)
            {
                ok = false;
                for (int i = 0; i < pages.Count && !ok; i++)
                {
                    if (512 - pages[i].Length >= seq.Length)
                    {
                        seqlocations_H.Add(seq, i);
                        seqlocations_L.Add(seq, pages[i].Length / 2);

                        Array.Copy(sequences[seq], 0, pages_byte[i], pages[i].Length / 2, seq.Length / 2);

                        pages[i] = pages[i] + seq;

                        ok = true;
                    }
                }
                if (!ok)
                {
                    seqlocations_H.Add(seq, pages.Count);
                    seqlocations_L.Add(seq, 0);
                    pages.Add(seq);
                    byte[] new_page = new byte[256];
                    Array.Copy(sequences[seq], 0, new_page, 0, seq.Length / 2);
                    pages_byte.Add(new_page);
                }
                seqnum++;
            }

            for (int i = 0; i < pages.Count; i++)
            {
                Console.WriteLine("page {0} length {1}", i, pages[i].Length / 2);
            }
            Console.WriteLine("{0} sequences packed in {1} pages", sorted_packed_sequences.Count, pages.Count);



            // Generating final .fym file


            // ***** only for latecomer ** (partition edit)
            //int[] final_partition = new int[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0xa,0xb,0xc,0xd,0xe,0xf, // intro
            //                                   0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, // scrolltext
            //                                   0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, // dotwaves
            //                                   0x38, 0x39, 0x3a, 0x3b, 0x3c, 0x3d, 0x3e, 0x3f, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, // greetings
            //                                   0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,0x59,0x5a,0x5b,0x5c,0x5d,0x5e,0x5f, 0x60 };
            // For regular .fym we keep the full partition
            int[] final_partition = new int[partition[0].Length];
            for (int pattern=0; pattern< partition[0].Length; pattern++)
            { final_partition[pattern] = pattern; }

            int total_size = 1 // sequence size
                + partition[0].Length * 28 // partition size
                + 1;  // 0x00
            total_size = (total_size / 256) * 256 + (total_size % 256 == 0 ? 0 : 256); // round to 256 bytes
            int sequences_start = total_size;
            total_size += pages.Count * 256;
            Console.WriteLine("File size: {0} bytes", total_size);

            byte[] final_file = new byte[total_size];
            final_file[0] = (byte)final_seqsize;
            // for(int partline=0; partline<partition[0].Length; partline++)
             for(int pattern=0; pattern< final_partition.Length; pattern++)
            {
                int partline = final_partition[pattern];
                for (int register=0; register<14; register++)
                {
                    String seq = reftopacked[partition[register][partline]];
                    // pointer low byte
                    int low_byte = seqlocations_L[seq];
                    // pointer high byte
                    int high_byte = (sequences_start / 256) + seqlocations_H[seq];

                    final_file[1 + pattern * 28 + register] = (byte)(high_byte);
                    final_file[1 + pattern * 28 + 14 + register] = (byte)(low_byte);
                    Trace.Write(high_byte.ToString("x2") + low_byte.ToString("x2") +" ");
                }
                Trace.WriteLine("");
            }
            for (int page=0; page<pages.Count; page++)
            {
                Array.Copy(pages_byte[page], 0, final_file, sequences_start + page * 256, 256);
            }
            File.WriteAllBytes(outputfile, final_file);

            Console.ReadKey();

        }
        
    }
}
