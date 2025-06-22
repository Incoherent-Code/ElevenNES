using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESEmu.Rom {
   //If I ever find this insufficient it can be discontinued but for now this allows for compatibility
   public class iNESHeader(byte[] header) {
      protected byte[] _Header = header;
      /// <summary>
      /// The size of the Program Rom in 16KB Units. (Ammount of Pages)
      /// </summary>
      public virtual int ProgramRomSize => _Header[4];
      /// <summary>
      /// The size of the Character Rom in 8KB units. 0 indicates the use of CHR RAM.
      /// </summary>
      public virtual int CharacterRomSize => _Header[5];
      /// <summary>
      /// Whether or not the PPU is configured to use horizontal or vertical mirroring. CustomNameTable can indicate that there is something else going on.
      /// </summary>
      public bool HorizontallyMirrored = (header[6] & 1) != 0;
      /// <summary>
      /// Indicates that the cartridge contained battery backup or other persistent memory. Typically acroos 0x6000 thru 0x7FFF.
      /// </summary>
      public bool BatteryBackup = (header[6] & 0b00000010) != 0;
      /// <summary>
      /// 512 Bytes located before PRG Data.
      /// </summary>
      public bool HasTrainerData = (header[6] & 0b0000100) != 0;
      /// <summary>
      /// Indicates custom nametable mirroring based on mapper chip.
      /// </summary>
      public bool CustomNametableLayout = (header[6] & 0b00001000) != 0;
      /// <summary>
      /// ID of the mapper used with the NES Cartridge
      /// </summary>
      public virtual int MapperID => _Header[6] >> 4;

   }
}
