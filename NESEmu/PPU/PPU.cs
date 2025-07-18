using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NESEmu.Mappers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static NESEmu.NESEmulator;

namespace NESEmu.PPU {
   public class PPU(Mapper mapper) {
      private Mapper Cartridge = mapper;
      /// <summary>
      /// Object Attribute Memory. Basically an array of 64 structs with the following fields:
      /// byte Y_Coordinate,
      /// byte Tile_Number,
      /// byte Sprite_Attribute_Information,
      /// byte X_Coordinate
      /// </summary>
      /// <see cref="https://www.nesdev.org/wiki/PPU_OAM"/>
      private byte[] OAMem = new byte[256];
      /// <summary>
      /// 32 bytes that contain 4 background palettes and 4 sprite palettes. First color is set as transparent.
      /// </summary>
      /// <see cref="https://www.nesdev.org/wiki/PPU_palettes"/>
      private byte[] PaletteRam = new byte[32];
      /// <summary>
      /// Shared between PPUADDR and PPUSCROLL.
      /// PPU SCROLL: Determines whether or not writing to X or Y scroll register.
      /// PPU ADDR: Determines whether or not writing high or low byte of PPUADDR.
      /// Cleared on PPUSTATUS Read
      /// </summary>
      /// <see cref="https://www.nesdev.org/wiki/PPU_registers#Internal_registers"/>
      private bool WRegister = false;
      private byte ReadPPUBus(ushort address) {
         if (address > 0x3EFF) {
            address -= 0x3F00;
            return PaletteRam[address % 32];
         }
         return Cartridge.ReadValuePPU(address);
      }
      private void WritePPUBus(ushort address, byte value) {
         if (address > 0x3EFF) {
            address -= 0x3F00;
            PaletteRam[address % 32] = value;
            return;
         }
         Cartridge.WriteValuePPU(address, value);
      }

      //PPU Configuration
      //See: https://www.nesdev.org/wiki/PPU_registers#PPUCTRL
      public enum BaseNameTableAddress {
         Hex2000,
         Hex2400,
         Hex2800,
         Hex2C00
      }
      /// <summary>
      /// Effectively acts as the 9th bit to scroll the PPU 
      /// </summary>
      public BaseNameTableAddress BaseNameTable { get; set; } = BaseNameTableAddress.Hex2000;
      /// <summary>
      /// When false, PPUDATA is incrimented by 1 on write.
      /// When true, PPUDATA is incrimented by 32 on write.
      /// </summary>
      public bool PPUAccessIncriment { get; set; } = false;
      /// <summary>
      /// When true, uses pattern table at 0x1000 instead of 0x0000. This is ignored in 8x16 mode.
      /// </summary>
      public bool _8by8SpritePatternTable1 { get; set; } = false;
      /// <summary>
      /// Whether the pattern table is located at 0x1000 (if true) or 0x0000 (if false)
      /// </summary>
      public bool BackgroundPatternTable {  get; set; } = false;
      /// <summary>
      /// Whether or not sprites are 8x16 (if true) or 8x8
      /// </summary>
      public bool _8by16SpriteMode {  get; set; } = false;
      /// <summary>
      /// May be implimented at a later date. On a stock NES this should never be enabled.
      /// </summary>
      public bool PPUIsSlave { get; set; } = false;
      /// <summary>
      /// Whether or not the CPU recieves an NMI Interupt when VBlank is hit.
      /// </summary>
      public bool SendVBlank { get; set; } = false;
      private byte _PPUCTRL;
      /// <summary>
      /// Mimics the PPUCTRL Register found at 0x2000 on the CPU Bus
      /// </summary>
      public byte PPUCTRL { get => _PPUCTRL; set {
            _PPUCTRL = value;
            BaseNameTable = (BaseNameTableAddress)(value & 3);
            value >>= 2;
            PPUAccessIncriment = (value & 1) == 1; value >>= 1;
            _8by8SpritePatternTable1 = (value & 1) == 1; value >>= 1;
            BackgroundPatternTable = (value & 1) == 1; value >>= 1;
            _8by16SpriteMode = (value & 1) == 1; value >>= 1;
            PPUIsSlave = (value & 1) == 1; value >>= 1;
            SendVBlank = value == 1;
         } }
      public bool GrayscaleMode { get; set; } = false;
      public bool ShowBackgroundInFirst8Pixels { get; set; } = false;
      public bool ShowSpritesInFirst8Pixels { get; set; } = false;
      public bool DoBackgroundRendering { get; set; } = false;
      public bool DoSpriteRendering { get; set; } = false;
      public bool EmphasizeRed { get; set; } = false;
      public bool EmphasizeGreen { get; set; } = false;
      public bool EmphasizeBlue { get; set; } = false;

      private byte _PPUMASK;
      /// <summary>
      /// Mimics the PPUMASK Register found at 0x2001 on the CPU Bus
      /// </summary>
      public byte PPUMASK {
         get => _PPUMASK; set {
            _PPUMASK = value;
            GrayscaleMode = (value & 1) == 1; value >>= 1;
            ShowBackgroundInFirst8Pixels = (value & 1) == 1; value >>= 1;
            ShowSpritesInFirst8Pixels = (value & 1) == 1; value >>= 1;
            DoBackgroundRendering = (value & 1) == 1; value >>= 1;
            DoSpriteRendering = (value & 1) == 1; value >>= 1;
            EmphasizeRed = (value & 1) == 1; value >>= 1;
            EmphasizeGreen = (value & 1) == 1; value >>= 1;
            EmphasizeBlue = (value & 1) == 1;
         }
      }

      public bool SpriteOverflow { get; set; } = false;
      public bool SpriteZeroHit { get; set; } = false;
      public bool IsInVBlank { get; set; } = false;
      /// <summary>
      /// Simulates a 0x2002 Read
      /// TODO: PPU Open Bus
      /// </summary>
      /// <see cref="https://www.nesdev.org/wiki/PPU_registers#PPUSTATUS"/>
      public byte PPUSTATUSRead() {
         int val = 0;
         val += SpriteOverflow ? 0b00100000 : 0;
         val += SpriteZeroHit ? 0b01000000 : 0;
         val += IsInVBlank ? 0b10000000 : 0;
         //IsInBlank Flag clears on Read
         IsInVBlank = false;
         //w register clears on read
         WRegister = false;
         return (byte)val;
      }
      /// <summary>
      /// PPU register that controls where OAMDATA is pointing. Often set to zero and using OAMDMA instead of OAMDATA Register
      /// </summary>
      /// <see cref="https://www.nesdev.org/wiki/PPU_registers#OAMADDR"/>
      public byte OAMADDR { get; set; } = 0;
      private byte OAMDATABuffer = 0;
      /// <summary>
      /// PPU register that allows access to the internal OAM memory using the address in OAMDATA
      /// </summary>
      /// <see cref="https://www.nesdev.org/wiki/PPU_registers#OAMDATA"/>
      public byte OAMDATARead() {
         var output = OAMDATABuffer;
         OAMDATABuffer = OAMem[OAMADDR];
         return output;
      }
      /// <summary>
      /// PPU register that allows access to the internal OAM memory using the address in OAMDATA
      /// </summary>
      /// <see cref="https://www.nesdev.org/wiki/PPU_registers#OAMDATA"/>
      public void OAMDATAWrite(byte value) {
         OAMem[OAMADDR] = value;
      }
      /// <summary>
      /// Simulates an OAMDMA Write (can take 513 or 514 cpu cycles depending on odd cycle)
      /// </summary>
      /// <param name="bytes">Byte Array to read from</param>
      /// <param name="offset">Offset of that byte array to read from</param>
      /// <see cref="https://www.nesdev.org/wiki/PPU_registers#OAMDMA"/>
      public void OAMDMAWrite(byte[] bytes, int offset) {
         for (int i = 0; i < 256; i++) {
            OAMem[i] = bytes[offset + i];
         }
      }
      //These values have a simulated 9th bit in PPUCTRL but will be emulated using base nametable address
      private byte XScroll = 0;
      private byte YScroll = 0;
      /// <summary>
      /// Alternates between writing to the XScroll and the YScroll values
      /// </summary>
      /// <param name="value">Value to write to PPU Scroll</param>
      public void PPUSCROLLWrite(byte value) {
         if (WRegister)
            YScroll = value;
         else
            XScroll = value;
         
         WRegister = !WRegister;
      }
      private int PPUWriteAddress = 0;
      /// <summary>
      /// VRAM Address that PPUDATA points to. Alternates between high and low byte.
      /// </summary>
      /// <param name="value">Value to write to.</param>
      /// <see cref="https://www.nesdev.org/wiki/PPU_registers#PPUADDR"/>
      public void PPUADDRWrite(byte value) {
         //If Writing low value
         if (WRegister) {
            PPUWriteAddress &= 0xFF00;
            PPUWriteAddress |= value;
         }
         //If writing high value
         else {
            PPUWriteAddress &= 0xFF;
            PPUWriteAddress |= (value & 0x3F) << 8;
         }
         WRegister = !WRegister;
      }
      private byte PPUDATABuffer = 0;
      public byte PPUDATARead() {
         var output = PPUDATABuffer;
         PPUDATABuffer = ReadPPUBus((ushort)PPUWriteAddress);
         PPUWriteAddress += PPUAccessIncriment ? 32 : 1;
         return output;
      }
      public void PPUDATAWrite(byte value) {
         WritePPUBus((ushort)PPUWriteAddress, value);
         PPUWriteAddress += PPUAccessIncriment ? 32 : 1;
      }
      private Color BackdropColor => NTSCPalette.GetColor(PaletteRam[0]);
      private Color[] GetPalette(int index) {
         var output = new Color[4];
         for (int i = 0; i < 4; i++) {
            output[i] = NTSCPalette.GetColor(PaletteRam[index + i]);
         }
         return output;
      }
      public void Draw(GraphicsDevice graphics, SpriteBatch spriteBatch) {
         graphics.Clear(BackdropColor);
      }
      /// <summary>
      /// Renders sprites using graphics and spriteBatch
      /// </summary>
      /// <param name="priority">If true, sprites set to behind background will be rendered. False will render sprites set to the front.</param>
      private void RenderSprites(GraphicsDevice graphics, SpriteBatch spriteBatch, int priority) {
         for (int i = 0; i < 64; i++) {
            
         }
      }
      private Texture2D LoadFromTile8by8(GraphicsDevice graphics, int paletteIndex, ushort address) {
         var output = new Texture2D(graphics, 8, 8);
         var data = new Color[64];
         var palette = GetPalette(paletteIndex);

         long leftPlane = 0;
         for (var i = 0; i < 8; i++) { //Read Left Bitplane from PPU
            leftPlane = (leftPlane << 8) + Cartridge.ReadValuePPU((ushort)(address + i));
         }

         long rightPlane = 0;
         for (var i = 0; i < 8; i++) { //Read Right Bitplane from PPU
            rightPlane = (rightPlane << 8) + Cartridge.ReadValuePPU((ushort)(address + i + 8));
         }

         for (var i = 63; i >= 0; i++) {
            data[i] = palette[((rightPlane & 1) << 1) + (leftPlane & 1)];
            leftPlane >>= 1;
            rightPlane >>= 1;
         }

         output.SetData(data);
         return output;
      }
   }
}
