using NESEmu.Rom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESEmu.Mappers {
   public enum VRAMMirroring {
      Horizontal,
      Vertical,
      Custom
   }
   public abstract class Mapper {
      protected readonly iNESHeader _Header;
      protected byte[] ProgramROM;
      protected byte[] CharacterROM;
      protected byte[] ProgramRAM;
      //Since the Cartridge Maps the VRAM using PPU A10 / A11, we will define VRAM on the Mapper
      protected byte[] VRAM = new byte[2048];
      /// <summary>
      /// Reads VRAM, accounting for nametable mirroring as configured by VRAMMirroringState. Can be overwritten by the mapper.
      /// </summary>
      protected virtual byte ReadVRAM(ushort address) {
         ArgumentOutOfRangeException.ThrowIfGreaterThan(address, 4095, nameof(address));
         //Nametables / Attribute tables 1 - 4 
         if (address < 1024)
            return VRAM[address];
         else if (address < 2048)
            return VRAM[(VRAMMirroringState == VRAMMirroring.Horizontal) ? address - 1024 : address];
         else if (address < 3072)
            return VRAM[(VRAMMirroringState == VRAMMirroring.Horizontal) ? address - 1024 : address - 2048];
         else
            return VRAM[address - 2048];
      }
      /// <summary>
      /// This is the State of VRAM Mirroring on the cartridge. Some use Horizontal, Vertical, Can Alternate, or have custom logic.
      /// See https://www.nesdev.org/wiki/Mirroring#Nametable_Mirroring
      /// </summary>
      protected VRAMMirroring VRAMMirroringState;
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
      /// This is the value that will be accessed externally in order to save and load cartridge data like saves.
      /// </summary>
      public virtual byte[] SaveData { get => ProgramRAM; set => ProgramRAM = value; }
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
         file.Read(CharacterROM, 0, CHROMBytes);
         if (Header.BatteryBackup) {
            //TODO: Proper Versatile Implimentation
            ProgramRAM = new byte[8192];
         }
         else {
            ProgramRAM = [];
         }

         if (Header.CustomNametableLayout)
            VRAMMirroringState = VRAMMirroring.Custom;
         else
            VRAMMirroringState = (Header.HorizontallyMirrored) ? VRAMMirroring.Horizontal : VRAMMirroring.Vertical;
      }
   }
}
