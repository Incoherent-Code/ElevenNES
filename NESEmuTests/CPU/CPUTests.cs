using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NESEmu.CPU;
using static NESEmu.CPU._6502OPCode;

namespace NESEmuTests.CPU {
   [TestClass]
   public sealed class CPUTests {
      /// <summary>
      /// This test is based on the video by Ben Eater showcasing the 6502 clock init cycles.
      /// https://youtu.be/LnzuMJLZRdU?si=uq2lvz8GXYMqCg_0
      /// Relevant for this test is 21:44
      /// </summary>
      [TestMethod]
      public void BasicNopCycleAccurateTest() {
         var cpu = new NESEmu.CPU.CPU();
         ushort lastReadAddress = 0;
         cpu.ReadMemory = (ushort address) => {
            lastReadAddress = address;
            return 0xEA;
         };
         //Note: CPU starts execution after 9 cycles, so during the 10th cycle is when it should read the value
         //Odly enough, the 6502 in the video executes in only one cycle. Not sure but we will assume the cpu only takes 8 cycles to execute for now
         cpu.ExecuteCPUCyles(9);
         Assert.AreEqual(0xEAEA, lastReadAddress, "CPU should initialize and start execution at 0xEAEA.");
         cpu.ExecuteCPUCyles(2);
         Assert.AreEqual(0xEAEB, lastReadAddress, "CPU should just keep incrimenting with nop command every 2 cycles.");
         cpu.ExecuteCPUCyles(1);
         Assert.AreEqual(0xEAEB, lastReadAddress, "CPU shouldn't read again during the next cycle. May indicate an off by one cycle execution.");
         cpu.ExecuteCPUCyles(512);
         Assert.AreEqual(0xEBEB, lastReadAddress, "CPU should just keep counting at a rate of 1 per 2 cycles");

         //A reset from the reset pin should only take 6 cpu cycles.
         cpu.ResetCPU();
         cpu.ExecuteCPUCyles(9);
         Assert.AreEqual(0xEAEB, lastReadAddress, "CPU should take 6 cycles to warm reset.");
      }
      [TestMethod]
      public void ArithmeticTest() {
         CPUBasicTestEnviornment.GetNew()
            .WithProgram([
               LDA_Imm, 24,
               STA_ZerX, 0, //24 at 0x0000
               INX,
               ASL_Acc,
               STA_ZerX, 0, //48 at 0x0001
               INX,
               SEC,
               SBC_Abs, 0, 0,
               STA_ZerX, 0, //24 at 0x0002
               INX,
               ADC_Imm, 76,
               LSR_Acc,
               STA_ZerX, 0, //50 at 0x0003
               INX,
               ORA_Imm, 128,
               STA_ZerX, 0, //178 at 0x0004
               INX,
               ADC_Imm, 100,
               STA_ZerX, 0, //23 at 0x0005 (Carry Test)
               INX,
               NOP,
               BCC_Rel, 253, //Infinite loop if carry is set
               LDA_Imm, 1,
               STA_Abs, 05, 00,
               NOP
               ])
            .runCycles(255)
            .AssertMemoryValue(0x0000, 24, "STA Failed")
            .AssertMemoryValue(0x0001, 48, "ASL Failed")
            .AssertMemoryValue(0x0002, 24, "SBC Failed")
            .AssertMemoryValue(0x0003, 50, "ADC or LSR Failed")
            .AssertMemoryValue(0x0004, 178, "ORA Failed")
            .AssertMemoryValue(0x0005, 23, "Carry Test Failed");
      }
   }
}
