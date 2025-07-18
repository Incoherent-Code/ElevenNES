using NESEmu.Rom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESEmu.Mappers {
   public class Mapper000_NROM(iNESHeader header, FileStream file) : Mapper(header, file) {
      public override byte ReadValueCPU(ushort location) {
         if (location < 0x8000)
            return 0;
         location -= 0x8000;
         if (_Header.ProgramRomSize == 1)
            return ProgramROM[location % 2048];
         else
            return ProgramROM[location];
      }

      public override byte ReadValuePPU(ushort location) {
         if (location < 0x2000)
            return ProgramROM[location];
         else if (location < 0x2FFF)
            return ReadVRAM((ushort)(location - 0x2000));
         else
            throw new ArgumentException("Further locations are unused / mapped to internal PPU registers.", nameof(location));
      }

      public override void WriteValueCPU(ushort location, byte value) {
         // NROM has no storage
      }

      public override void WriteValuePPU(ushort location, byte value) {
         // NROM has no storage
      }
   }
}
