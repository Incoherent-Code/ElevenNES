using NESEmu.Rom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESEmu.Mappers {
   public abstract class Mapper {
      protected readonly iNESHeader _Header;
      protected byte[] ProgramROM;
      protected byte[] CharacterROM;
      protected byte[] ProgramRAM;
      /// <summary>
      /// Read a byte of data from the cartridge. Can also be registers from the mapper chip itself.
      /// </summary>
      public abstract byte ReadValueCPU(ushort location);
      /// <summary>
      /// Write a byte to the Program ROM. Usually used for configuring bank switching or saving to PRAM.
      /// </summary>
      public abstract void WriteValueCPU(ushort location, byte value);
      /// <summary>
      /// Read a value from the CHROM for the PPU to use.
      /// </summary>
      public abstract byte ReadValuePPU(ushort location);
      /// <summary>
      /// If the PPU sends a write request to the CRAM. Not sure how often this is actually used.
      /// </summary>
      public abstract void WriteValuePPU(ushort location, byte value);
      /// <summary>
      /// Provided by the NES Emulator to interupt the CPU.
      /// </summary>
      public Action InteruptCPU { get; set; } = delegate { };
      protected Mapper(iNESHeader Header, FileStream file) {
         _Header = Header;
         var StartingPos = 16;
         if (Header.HasTrainerData)
            StartingPos += 512;
         file.Seek(StartingPos, SeekOrigin.Begin);
         var PGMBytes = Header.ProgramRomSize * 16384;
         ProgramROM = new byte[PGMBytes];
         file.Read(ProgramROM, 0, PGMBytes);
         var CHROMBytes = Header.CharacterRomSize * 8192;
         CharacterROM = new byte[CHROMBytes];
         file.Read(ProgramRAM, 0, CHROMBytes);
         if (Header.BatteryBackup) {
            //TODO: Proper Versatile Implimentation
            ProgramRAM = new byte[8192];
         }
         else {
            ProgramRAM = [];
         }
      }
   }
}
