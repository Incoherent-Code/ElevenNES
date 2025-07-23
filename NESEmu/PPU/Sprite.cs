using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESEmu.PPU {
   public struct Sprite {
      public byte x;
      public byte y;
      /// <summary>
      /// In 8x8 Mode, this is the tile number from the PPU selected pattern table.
      /// In 8x16 Mode, The first bit from the right selects base pattern table, and other 7 bytes dictate sprite index.
      /// </summary>
      /// <see cref="https://www.nesdev.org/wiki/PPU_OAM"/>
      public byte tileNumber;
      /// <summary>
      /// 2 bit number; (Palettes 4-7 are for sprites on the NES)
      /// </summary>
      public int palette;
      public bool FlipHorizontal;
      public bool FlipVertical;
      public bool IsBehindBackground;
      public Vector2 Location => new Vector2(x, y);
      public Sprite(byte[] arr, int offset) {
         y = arr[offset];
         tileNumber = arr[offset + 1];
         var metadata = arr[offset + 2];
         palette = metadata & 3;
         IsBehindBackground = (metadata & 0b00100000) != 0;
         FlipHorizontal = (metadata & 0b01000000) != 0;
         FlipVertical = (metadata & 0b10000000) != 0;
         x = arr[offset + 3];
      }
   }
}
