using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NESEmu.CPU;
using NESEmu.Mappers;
using NESEmu.Rom;
using System;
using System.IO;

namespace NESEmu {
   public class NESEmulator {
      public CPU.CPU CPU = new CPU.CPU();
      public PPU.PPU PPU;
      public byte[] WorkRam = new byte[2048];
      public Mapper Cartridge;
      public string CurrentGamePath;

      private ShiftRegister8Bit Controller1Register = new();
      private ShiftRegister8Bit Controller2Register = new();
      /// <summary>
      /// Function to get the controller's value. Bits should be in this order (highest to lowest bit):
      /// DPAD_RIGHT, DPAD_LEFT, DPAD_DOWN, DPAD_UP
      /// START, SELECT, B, A
      /// </summary>
      public Func<byte> GetController1 { get => Controller1Register.Input; set => Controller1Register.Input = value; }
      /// <summary>
      /// Function to get the controller's value. Bits should be in this order (highest to lowest bit):
      /// DPAD_RIGHT, DPAD_LEFT, DPAD_DOWN, DPAD_UP
      /// START, SELECT, B, A
      /// </summary>
      public Func<byte> GetController2 { get => Controller2Register.Input; set => Controller2Register.Input = value; }
        
      public delegate byte BusReadDelegate(ushort address);
      public delegate void BusWriteDelegate(ushort address, byte data);
      public NESEmulator(string filePath) {
         CurrentGamePath = filePath;
         using (var nesFile = File.OpenRead(filePath)) {
            //This isnt reading the header info yet, Rather it is identifying the file
            //See: https://www.nesdev.org/wiki/INES
            var header = new byte[16];
            nesFile.Read(header, 0, 16);
            if (!(header[0] == 'N' && header[1] == 'E' && header[2] == 'S' && header[3] == 0x1A))
               throw new NotSupportedException("ROM is not in a supported format. iNES / NES 2.0 format only.");
            iNESHeader parsedHeader;
            if ((header[7] & header[12]) == 8) {
               //Probably iNES 2.0
               parsedHeader = new NES2Header(header);
            }
            if ((header[7] & header[12]) == 0 && header[12] == 0 && header[13] == 0 && header[14] == 0 && header[15] == 0) {
               //Probably iNES 1.0
               parsedHeader = new iNES1Header(header);
            }
            else {
               parsedHeader = new iNESHeader(header);
            }
            Cartridge = MapperFactory.CreateMapper(parsedHeader, nesFile);
            Cartridge.InteruptCPU = () => CPU.TriggerInteruptIRQ();
         }

         PPU = new PPU.PPU(Cartridge);

         CPU.ReadMemory = (address) => {
            if (address <= 0x1FFF) {
               return WorkRam[address % 2048];
            }
            else if (address == 0x2002) {
               return PPU.PPUSTATUSRead();
            }
            else if (address == 0x2004) {
               return PPU.OAMDATARead();
            }
            else if (address == 0x2007) {
               return PPU.PPUDATARead();
            }
            else if (address == 0x4016) {
               return (byte)(Controller1Register.ReadWithPulse() ? 1 : 0);
            }
            else if (address == 0x4017) {
               return (byte)(Controller2Register.ReadWithPulse() ? 1 : 0);
            }
            else {
               return Cartridge.ReadValueCPU(address);
            }
         };

         CPU.WriteMemory = (address, data) => {
            if (address <= 0x1FFF) {
               WorkRam[address % 2048] = data;
            }
            //PPU Registers
            else if (address == 0x2000) {
               PPU.PPUCTRL = data;
            }
            else if (address == 0x2001) {
               PPU.PPUMASK = data;
            }
            else if (address == 0x2003) {
               PPU.OAMADDR = data;
            }
            else if (address == 0x2004) {
               PPU.OAMDATAWrite(data);
            }
            else if (address == 0x2005) {
               PPU.PPUSCROLLWrite(data);
            }
            else if (address == 0x2006) {
               PPU.PPUADDRWrite(data);
            }
            else if (address == 0x2007) {
               PPU.PPUDATAWrite(data);
            }
            else if (address == 0x4014) {
               CPU.DelegateCycles(CPU.CycleCount % 2 == 0 ? 513 : 514, () => {
                  PPU.OAMDMAWrite(WorkRam, data << 8);
               });
            }
            else if (address == 0x4016) {
               //The latching of the controller inputs are bound together
               if ((data & 1) == 1) {
                  Controller1Register.PullLatchHIGH();
                  Controller2Register.PullLatchHIGH();
               }
               else {
                  Controller1Register.PullLatchLOW();
                  Controller2Register.PullLatchLOW();
               }
            }
            else {
               Cartridge.WriteValueCPU(address, data);
            }
         };
      }
      public void Update() {
         CPU.ExecuteCPUCyles(27552);
      }

      public void Draw(GraphicsDevice Graphics, SpriteBatch Batch) {
         PPU.Draw(Graphics, Batch);
         if (PPU.SendVBlank)
            CPU.TriggerInteruptNMI();
         CPU.ExecuteCPUCyles(1);
         PPU.IsInVBlank = true;
         CPU.ExecuteCPUCyles(2276);
         PPU.IsInVBlank = false;
      }
   }
}
