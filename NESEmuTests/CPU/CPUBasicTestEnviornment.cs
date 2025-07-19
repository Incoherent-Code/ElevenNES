using Microsoft.VisualStudio.TestPlatform.TestHost;
using NESEmu.CPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NESEmuTests.CPU {
   /// <summary>
   /// Creates a basic test enviornment where the entire memory bus is writable memory.
   /// </summary>
   public class CPUBasicTestEnviornment {
      public byte[] DRAM = new byte[65536];
      public NESEmu.CPU.CPU CPU = new();
      private bool initialized = false;
      public static CPUBasicTestEnviornment GetNew() {
         return new CPUBasicTestEnviornment();
      }
      public CPUBasicTestEnviornment() {
         CPU.WriteMemory = (address, data) => {
            DRAM[address] = data;
         };
         CPU.ReadMemory = (address) => { return DRAM[address]; };
         //Start Vector
         DRAM[0xFFFD] = 0x80;
      }
      /// <summary>
      /// Initialize a program at specified offset (default 0x8000). Sets up start vector.
      /// </summary>
      public CPUBasicTestEnviornment WithProgram(byte[] program, ushort offset = 0x8000) {
         for (int i = 0; i < program.Length; i++) {
            DRAM[offset + i] = program[i];
         }
         DRAM[0xFFFC] = (byte)offset;
         DRAM[0xFFFD] = (byte)(offset >> 8);
         return this;
      }
      public CPUBasicTestEnviornment WithIRQHandler(byte[] handler, ushort offset = 0x4000) {
         for (int i = 0; i < handler.Length; i++) {
            DRAM[offset + i] = handler[i];
         }
         DRAM[0xFFFE] = (byte)offset;
         DRAM[0xFFFF] = (byte)(offset >> 8);
         return this;
      }
      public CPUBasicTestEnviornment WithNMIHandler(byte[] handler, ushort offset = 0x6000) {
         for (int i = 0; i < handler.Length; i++) {
            DRAM[offset + i] = handler[i];
         }
         DRAM[0xFFFA] = (byte)offset;
         DRAM[0xFFFB] = (byte)(offset >> 8);
         return this;
      }
      /// <summary>
      /// Should only be used for debugging purposes.
      /// </summary>
      public CPUBasicTestEnviornment WithBreakPoints(CPUBreakpoint[] breaks) {
#if DEBUG
         foreach (var item in breaks) {
            CPU.Breakpoints.Add(item);
         }
#endif
         return this;
      }
      public CPUBasicTestEnviornment RunCycles(int cycles) {
         if (!initialized) {
            CPU.ExecuteCPUCyles(8);
            initialized = true;
         }
         CPU.ExecuteCPUCyles(cycles);
         return this;
      }
      public CPUBasicTestEnviornment TriggerNMI() {
         CPU.TriggerInteruptNMI(); 
         return this;
      }
      public CPUBasicTestEnviornment TriggerIRQ() {
         CPU.TriggerInteruptIRQ();
         return this;
      }
      public CPUBasicTestEnviornment AssertMemoryValue(ushort address, byte value, string message) {
         Assert.AreEqual(value, DRAM[address], message);
         return this;
      }
   }
}
