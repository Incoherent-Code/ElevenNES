using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESEmu.Rom {
   public class iNES1Header(byte[] header) : iNESHeader(header) {
      public bool ForVSUnisystem = (header[7] & 1) != 0;
      /// <summary>
      /// Indicates that there is 8KB of Hint Screen Data stored after CHR ROM
      /// </summary>
      public bool ForPlayChoice10 = (header[7] & 0b00000010) != 0;
      public override int MapperID => base.MapperID & (_Header[7] & 0b11110000);
      /// <summary>
      /// Can be used to specify the size of Program Ram on the cartridge. 8KB is inferred for compatibility. In 8KB Units.
      /// </summary>
      public virtual int ProgramRamSize => _Header[8];
      /// <summary>
      /// This is not in the roadmap for implimentation, but PAL consoles run at different speeds.
      /// </summary>
      public virtual bool IsPalRom => _Header[9] == 1; 
   }
}
