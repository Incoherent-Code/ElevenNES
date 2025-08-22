using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESEmu.Rom {
   public class NES2Header(byte[] header) : iNES1Header(header) {
      public bool IsExtendedConsoleType => ForVSUnisystem && ForPlayChoice10;
      public override int MapperID => base.MapperID | ((_Header[8] & 0x0F) << 8);
      public int SubbmapperID = header[8] >> 4;
      public override int ProgramRomSize => base.ProgramRomSize | ((_Header[9] & 0x0F) << 8);
      public override int CharacterRomSize => base.CharacterRomSize | ((_Header[9] & 0xF0) << 4);
      public override int ProgramRamSize => (64 << (_Header[10] & 0x0F)) / 8192;
      public int EEPROMSize => (64 << (_Header[10] >> 4)) / 8192;
      public override bool IsPalRom => (_Header[12] & 0x03) == 1;
      public SystemTimingType SystemTimingType = (SystemTimingType)(header[12] & 0x03);
      //TODO: The rest of this
      //https://www.nesdev.org/wiki/NES_2.0
   }
   public enum SystemTimingType {
      NTSC_NES = 0,
      PAL_NES = 1,
      Multiregion = 2,
      DENDY = 3
   }
}
