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
      public byte[] WorkRam = new byte[2048];
      public byte[] VRAM = new byte[2048];
      public Mapper Cartridge;
        
      public delegate byte BusReadDelegate(ushort address);
      public delegate void BusWriteDelegate(ushort address, byte data);
      public NESEmulator(string filePath) {
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

         CPU.ReadMemory = (address) => {
            if (address < 0x1FFF) {
               return WorkRam[address % 2048];
            }
            else {
               return Cartridge.ReadValueCPU(address);
            }
         };

         CPU.WriteMemory = (address, data) => {
            if (address < 0x1FFF) {
               WorkRam[address % 2048] = data;
            }
            else {
               Cartridge.WriteValueCPU(address, data);
            }
         };
      }
      public void Update() {
         CPU.ExecuteCPUCyles(27552);
      }

      public void Draw() {
         CPU.ExecuteCPUCyles(2277);
      }
   }
}
